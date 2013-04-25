namespace Orbit.Solver

open System.Diagnostics

module NotSimpleFunctions = 
    open Orbit
    open Helpers

    let agentLogic_ chunkAndSend G inbox = 
        let foundSoFar = MutableSet<'T>()
        let rec start() = 
            async {
                let! Start(initData, replyChannel) = Agent.receive inbox
                return! loop replyChannel 0 [initData] (Array.length initData)
            }
        and loop replyChannel remaining (acc:'T array list) count = async {
            if (remaining = 0 && count > 0) || count >= G then
                let jobs = chunkAndSend (Seq.collect id acc) (Seq.chunked G) inbox
                return! loop replyChannel (remaining + jobs) [] 0
            elif remaining = 0 then
                AsyncReplyChannel.reply replyChannel <| upcast foundSoFar
            else
                let! Result data = inbox.Receive()
                let data = data |> Array.filter (not << contains foundSoFar)
                MutableSet.unionWith foundSoFar data
                let size = Array.length data
                if count + size < G then
                    let nacc = if Array.isEmpty data then acc else data::acc
                    return! loop replyChannel (remaining - 1) nacc (count + size)
                else
                    let jobs = chunkAndSend (data::acc |> Seq.collect id) (Seq.chunked G) inbox
                    return! loop replyChannel (remaining + jobs - 1) [] 0
        }
        start()

    let agentLogic chunkAndSend G inbox = 
        let foundSoFar = MutableSet<'T>()
        let rec start() = 
            async {
                let! Start(initData, replyChannel) = Agent.receive inbox
                MutableSet.unionWith foundSoFar initData
                let jobs = chunkAndSend initData (Seq.chunked G) inbox
                return! loop replyChannel jobs
            }
        and loop replyChannel remaining = async {
            try
                let! Result data = inbox.Receive()
                let data = data |> Array.filter (not << contains foundSoFar)
                MutableSet.unionWith foundSoFar data
                let jobs = chunkAndSend data (Seq.chunked_opt_2 G) inbox
                if remaining > 1 || jobs > 0 then
                    return! loop replyChannel (remaining + jobs - 1)
                else
                    AsyncReplyChannel.reply replyChannel <| upcast foundSoFar               
            with _ ->
                return! loop replyChannel remaining
        }
        start()

    let solveWithAgentAsyncs<'T when 'T: equality> G 
        { initData = (initData:seq<'T>); generators = generators } = 
        let chunkAndSend data chunker inbox = 
            let mutable jobs = 0
            for chunk in data |> chunker do
                Async.Start <| async {
                    Seq.collect generators chunk |> Seq.distinct
                    |> Array.ofSeq
                    |> Result |> Agent.post inbox 
                }
                jobs <- jobs + 1
            jobs
        let workPile = Agent.start <| agentLogic chunkAndSend G
        async {
            let timer = Stopwatch.StartNew()
            let! result = Agent.postAndAsyncReply workPile (fun channel -> Start(Array.ofSeq initData, channel))
            return result, timer.ElapsedMilliseconds
        } |> Async.RunSynchronously

    let solveWithAgentTasks<'T when 'T: equality> G 
        { initData = (initData:seq<'T>); generators = generators } =
        let chunkAndSend data chunker inbox = 
            let mutable jobs = 0
            for chunk in data |> chunker do
                System.Threading.Tasks.Task.Factory.StartNew(fun _ ->
                    Seq.collect generators chunk |> Seq.distinct
                    |> Array.ofSeq
                    |> Result |> Agent.post inbox 
                ) |> ignore
                jobs <- jobs + 1
            jobs
        let workPile = Agent.start <| agentLogic chunkAndSend G
        async {
            let timer = Stopwatch.StartNew()
            let! result = Agent.postAndAsyncReply workPile (fun channel -> Start(Array.ofSeq initData, channel))
            return result, timer.ElapsedMilliseconds
        } |> Async.RunSynchronously
   
    let solveWithAgentWorkers<'T when 'T:equality> M G 
        { initData = (initData:seq<'T>); generators = generators } =
        let chunkAndSend workers i data chunker inbox =
            let mutable jobs = 0
            for chunk in data |> chunker do
                (Array.get workers <| (incSafe i)%M, chunk) 
                ||> Agent.post
                jobs <- jobs + 1
            jobs
        let workPile = Agent.start <| fun inbox ->
            let workers = Array.init M <| fun _ -> Agent.start(fun workerInbox ->
                let rec loop () = 
                    async {
                        let! chunk = Agent.receive workerInbox
                        Seq.collect generators chunk
                        |> Seq.distinct
                        |> Array.ofSeq
                        |> Result
                        |> Agent.post inbox
                        return! loop()
                    }
                loop()
            )
            let i = ref -1
            agentLogic (chunkAndSend workers i) G inbox 
        async {
            let timer = Stopwatch.StartNew()
            let! result = Agent.postAndAsyncReply workPile (fun channel -> Start(Array.ofSeq initData, channel))
            return result, timer.ElapsedMilliseconds
        } |> Async.RunSynchronously

//    open System.Threading
//    open System.Threading.Tasks
//    let solveWithAgentConcurrentDictionary<'T when 'T: equality> M G 
//        { initData = initData; generators = generators } = 
//        use flag = new ManualResetEventSlim()
//        let foundSoFar = System.Collections.Concurrent.ConcurrentDictionary<'T, obj>(M, 5000000)
//        let workPile = Agent.start <| fun inbox ->
//            let remaining = ref 1
//            let rec loop() = async {
//                let! data = Agent.receive inbox
//                let jobs = ref -1
//                for chunk in data do
//                    Task.Factory.StartNew(fun _ ->
//                        Seq.collect generators chunk
//                        |> Seq.filter (fun x -> foundSoFar.TryAdd(x, null))
//                        |> Seq.chunked G 
//                        |> Seq.toArray
//                        |> Agent.post inbox
//                    ) |> ignore
//                    incr jobs
//                remaining := !remaining + !jobs
//                if (!remaining = 0 && !jobs = -1) then
//                    ManualResetEventSlim.set flag |> ignore
//                else
//                    return! loop()
//            }
//            loop()
//        let timer = Stopwatch.StartNew()
//        Agent.post workPile (initData |> Seq.chunked G |> Seq.toArray)
//        ManualResetEventSlim.wait flag
//        timer.Stop()
//        foundSoFar.Keys :> seq<_>, timer.ElapsedMilliseconds

    let agentLogic2 chunkAndSend M G (inbox:Agent<Message<'T>>) = 
        let foundSoFar = ConcurrentSet.create M 100000
        let rec start() = 
            async {
                let! Start(initData, replyChannel) = Agent.receive inbox
                return! loop replyChannel 0 [initData] (Array.length initData)
            }
        and loop replyChannel remaining (acc:'T array list) count = async {
            if (remaining = 0 && count > 0) || count >= G then
                let jobs = chunkAndSend foundSoFar (Seq.collect id acc) (Seq.chunked G) inbox
                return! loop replyChannel (remaining + jobs) [] 0
            elif remaining = 0 then
                AsyncReplyChannel.reply replyChannel <| upcast foundSoFar.Keys
            else
                let! Result data = inbox.Receive()
                let size = Array.length data
                if count + size < G then
                    let nacc = if Array.isEmpty data then acc else data::acc
                    return! loop replyChannel (remaining - 1) nacc (count + size)
                else
                    let jobs = chunkAndSend foundSoFar (data::acc |> Seq.collect id) (Seq.chunked G) inbox
                    return! loop replyChannel (remaining + jobs - 1) [] 0
        }
        start()

    open System.Threading
    open System.Threading.Tasks
    let solveWithAgentConcurrentDictionary<'T when 'T: equality> M G 
        { initData = initData; generators = generators } = 
        let chunkAndSend foundSoFar (data:seq<'T>) chunker inbox = 
            let mutable jobs = 0
            for chunk in data |> chunker do
                Task.Factory.StartNew(fun _ ->
                    Seq.collect generators chunk
                    |> Seq.filter (ConcurrentSet.add foundSoFar)
                    |> Seq.toArray
                    |> Result
                    |> Agent.post inbox
                ) |> ignore
                jobs <- jobs + 1
            jobs
        let workPile = Agent.start <| agentLogic2 chunkAndSend M G
        async {
            let timer = Stopwatch.StartNew()
            let! result = Agent.postAndAsyncReply workPile (fun channel -> Start(Array.ofSeq initData, channel))
            return result, timer.ElapsedMilliseconds
        } |> Async.RunSynchronously