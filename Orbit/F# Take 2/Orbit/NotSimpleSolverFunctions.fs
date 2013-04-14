namespace Orbit.Solver

open System.Diagnostics

module NotSimpleFunctions = 
    open Orbit

    open Helpers
    open System.Threading
    open System.Threading.Tasks
    let solveWithAgentTasks<'T when 'T: equality> M G =
        let scheduler = Helpers.CustomTaskSchedulers.LimitedConcurrencyLevelTaskScheduler M
        fun { initData = initData; generators = generators } ->
            use flag = new ManualResetEventSlim(false)
            let foundSoFar = MSet<'T>()
            let workPile = Agent.Start(fun inbox ->
                let remaining = ref 1
                let rec loop() = async {
                    let! data = inbox.Receive()
                    let data = data |> Array.filter (not << contains foundSoFar)
                    unionWith foundSoFar data
                    let jobs = ref -1
                    for chunk in data |> Seq.chunked_opt_2 G do
                        Task.Factory.StartNew(fun _ ->
                            Seq.collect generators chunk
                            |> Seq.distinct
                            |> Array.ofSeq
                            |> inbox.Post
                        ) |> ignore
                        incr jobs
                    remaining := !remaining + !jobs
                    if (!remaining = 0 && !jobs = -1) then
                        ManualResetEventSlim.set flag
                    else 
                        return! loop()
                }
                loop()
            )
            let timer = Stopwatch.StartNew()
            Agent.post workPile (Array.ofSeq initData)
            ManualResetEventSlim.wait flag
            foundSoFar :> seq<_>, timer.ElapsedMilliseconds

    let solveWithAgentConcurrentDictionary<'T when 'T: equality> M G { initData = initData; generators = generators } = 
        use flag = new ManualResetEventSlim()
        let foundSoFar = System.Collections.Concurrent.ConcurrentDictionary<'T, obj>(M, 5000000)
        let workPile = Agent.start <| fun inbox ->
            let remaining = ref 1
            let rec loop() = async {
                let! data = Agent.receive inbox
                let jobs = ref -1
                for chunk in data do
                    Task.Factory.StartNew(fun _ ->
                        Seq.collect generators chunk
                        |> Seq.filter (fun x -> foundSoFar.TryAdd(x, null))
                        |> Seq.chunked G 
                        |> Seq.toArray
                        |> Agent.post inbox
                    ) |> ignore
                    incr jobs
                remaining := !remaining + !jobs
                if (!remaining = 0 && !jobs = -1) then
                    ManualResetEventSlim.set flag |> ignore
                else
                    return! loop()
            }
            loop()
        let timer = Stopwatch.StartNew()
        Agent.post workPile (initData |> Seq.chunked G |> Seq.toArray)
        ManualResetEventSlim.wait flag
        timer.Stop()
        foundSoFar.Keys :> seq<_>, timer.ElapsedMilliseconds

    module private AgentSystem = 

        type MapperMessage<'T> =
        |Job of seq<'T>
        |Stop
        
        let workerLogic generators inbox workerInbox = 
            let rec loop () = 
                async {
                    let! msg = Agent.receive workerInbox
                    match msg with
                    |Job chunk ->
                        Seq.collect generators chunk
                        |> Seq.distinct
                        |> Array.ofSeq
                        |> Agent.post inbox
                        return! loop()
                    |Stop ->
                        ()
                }
            loop()
        
        let workerPileLogic foundSoFar M G flag workers inbox  =
            let remaining = ref 1
            let i = ref -1
            let rec loop() = async {
                let! data = Agent.receive inbox
                let data = data |> Array.filter (not << contains foundSoFar)
                unionWith foundSoFar data
                let jobs = ref -1
                for chunk in data |> Seq.chunked G |> Seq.toArray do
                    (Array.get workers <| (incSafe i)%M, Job chunk) 
                    ||> Agent.post
                    incr jobs
                remaining := !remaining + !jobs
                if (!remaining = 0 && !jobs = -1) then
                    ManualResetEventSlim.set flag
                    Array.iter (fun worker -> Agent.post worker Stop) workers 
                else 
                    return! loop()
            }
            loop()
            
    open AgentSystem    
    let solveWithAgentWorkers<'T when 'T:equality> M G { initData = initData; generators = generators } =
        use flag = new ManualResetEventSlim()
        let foundSoFar = MSet<'T>()
        let workPile = Agent.start <| fun inbox ->
            let workers = Array.init M <| fun  _ -> Agent.start(workerLogic generators inbox)
            workerPileLogic foundSoFar M G flag workers inbox  
        let timer = Stopwatch.StartNew()    
        Agent.post workPile (Array.ofSeq initData)
        ManualResetEventSlim.wait flag        
        foundSoFar :> seq<_>, timer.ElapsedMilliseconds

