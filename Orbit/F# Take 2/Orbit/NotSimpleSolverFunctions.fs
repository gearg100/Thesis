namespace Orbit.Solver

open System.Diagnostics

module NotSimpleFunctions = 
    open Orbit
    open Helpers

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
            let! Result data = inbox.Receive()
            let data = data |> Array.filter (not << contains foundSoFar)
            MutableSet.unionWith foundSoFar data
            let jobs = chunkAndSend data (Seq.chunked_opt_2 G) inbox
            if remaining > 1 || jobs > 0 then
                return! loop replyChannel (remaining + jobs - 1)
            else
                AsyncReplyChannel.reply replyChannel <| upcast foundSoFar
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

    type Task = System.Threading.Tasks.Task
    let solveWithAgentTasks<'T when 'T: equality> G 
        { initData = (initData:seq<'T>); generators = generators } =
        let chunkAndSend data chunker inbox = 
            let mutable jobs = 0
            for chunk in data |> chunker do
                Task.Factory.StartNew(fun _ ->
                    Seq.collect generators chunk 
                    |> Seq.distinct
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

    let agentLogic2 chunkAndSend M G (inbox:Agent<Message<'T>>) = 
        let foundSoFar = ConcurrentSet.create M 100000
        let rec start() = 
            async {
                let! Start(initData, replyChannel) = Agent.receive inbox
                let jobs = chunkAndSend foundSoFar initData (Seq.chunked_opt_2 G) inbox
                return! loop replyChannel jobs
            }
        and loop replyChannel remaining = async {
            let! Result data = inbox.Receive()
            let jobs = chunkAndSend foundSoFar data (Seq.chunked_opt_2 G) inbox
            if remaining > 1 || jobs > 0 then
                return! loop replyChannel (remaining + jobs - 1)
            else
                AsyncReplyChannel.reply replyChannel <| upcast foundSoFar.Keys
        }
        start()

    open System.Threading; open System.Threading.Tasks
    let solveWithAgentConcurrentDictionary<'T when 'T: equality> M G 
        { initData = initData; generators = generators } = 
        let chunkAndSend foundSoFar (data:array<'T>) chunker inbox = 
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