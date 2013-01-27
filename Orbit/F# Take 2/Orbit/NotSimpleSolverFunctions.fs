namespace Orbit.Solver

open System.Diagnostics

module NotSimpleFunctions = 
    open Orbit

    open Helpers
    open System.Threading
    open System.Threading.Tasks
    let solveWithAgentTasks<'T when 'T: equality> (M,G) =
        let scheduler = Helpers.CustomTaskSchedulers.LimitedConcurrencyLevelTaskScheduler M
        fun { initData = initData; generators = generators } ->
            use flag = new ManualResetEventSlim(false)
            let foundSoFar = MSet<'T>()
            let workPile = Agent.Start(fun inbox ->
                let remaining = ref 0
                let rec loop() = async {
                    let! data = inbox.Receive()
                    let data = data |> Seq.filter (not << contains foundSoFar) |> Array.ofSeq
                    unionWith foundSoFar data
                    let jobs = ref -1
                    for chunk in data |> Seq.distinct |> Seq.chunked G do
                        Task.Factory.StartNew(
                            (fun () ->
                                Seq.collect generators chunk
                                |> Array.ofSeq
                                |> inbox.Post),
                            CancellationToken.None,
                            TaskCreationOptions.HideScheduler ||| TaskCreationOptions.DenyChildAttach,
                            scheduler
                        )
                        |> ignore
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
                        |> Array.ofSeq
                        |> Agent.post inbox
                        return! loop()
                    |Stop ->
                        ()
                }
            loop()
        
        let workerPileLogic foundSoFar M G flag workers inbox  =
            let remaining = ref 0
            let i = ref -1
            let rec loop() = async {
                let! data = Agent.receive inbox
                let data = 
                    data 
                    |> Seq.filter (not << contains foundSoFar) 
                    |> Array.ofSeq
                unionWith foundSoFar data
                let jobs = ref -1
                for chunk in data |> Seq.distinct |> Seq.chunked G do
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
    let solveWithAgentWorkers<'T when 'T:equality> (M,G) { initData = initData; generators = generators } =
        use flag = new ManualResetEventSlim()
        let foundSoFar = MSet<'T>()
        let workPile = Agent.start <| fun inbox ->
            let workers = Array.init M <| fun  _ -> Agent.start(workerLogic generators inbox)
            workerPileLogic foundSoFar M G flag workers inbox  
        let timer = Stopwatch.StartNew()    
        Agent.post workPile (Array.ofSeq initData)
        ManualResetEventSlim.wait flag        
        foundSoFar :> seq<_>, timer.ElapsedMilliseconds

