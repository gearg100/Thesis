namespace Orbit.Solver

open System.Diagnostics

module NotSimpleFunctions = 
    open Orbit
    open Helpers
    type Task = System.Threading.Tasks.Task

    let agentLogicMutableSet chunkAndSend G inbox = 
        let foundSoFar = MutableSet<'T>()
        let rec start() = 
            async {
                let! Start(initData, replyChannel) = Agent.receive inbox
                MutableSet.unionWith foundSoFar initData
                let jobs = chunkAndSend inbox (Seq.chunked_opt_2 G) initData
                return! loop replyChannel jobs
            }
        and loop replyChannel remaining = async {
            let! Result data = inbox.Receive()
            let data = data |> Array.filter (not << contains foundSoFar)
            MutableSet.unionWith foundSoFar data
            let jobs = chunkAndSend inbox (Seq.chunked_opt_2 G) data
            if remaining > 1 || jobs > 0 then
                return! loop replyChannel (remaining + jobs - 1)
            else
                AsyncReplyChannel.reply replyChannel <| upcast foundSoFar
        }
        start()

    let agentLogicConcurrentSet (foundSoFar:ConcurrentSet<'T>) chunkAndSend M G inbox =       
        let rec start() = 
            async {
                let! Start(initData, replyChannel) = Agent.receive inbox
                let jobs = chunkAndSend inbox (Seq.chunked_opt_2 G) initData
                return! loop replyChannel jobs
            }
        and loop replyChannel remaining = async {
            let! Result data = inbox.Receive()
            let jobs = chunkAndSend inbox (Seq.chunked_opt_2 G)  data
            if remaining > 1 || jobs > 0 then
                return! loop replyChannel (remaining + jobs - 1)
            else
                AsyncReplyChannel.reply replyChannel <| upcast foundSoFar.Keys
        }
        start()

    let timedRun workPile initData = 
        async {
            let timer = Stopwatch.StartNew()
            let! result = Agent.postAndAsyncReply workPile (fun channel -> Start(Array.ofSeq initData, channel))
            return result, timer.ElapsedMilliseconds
        } |> Async.RunSynchronously

    let logicWithMutableSet generators coordinator chunk = 
        Seq.collect generators chunk 
        |> Seq.distinct
        |> Array.ofSeq
        |> Result 
        |> Agent.post coordinator 

    let logicWithConcurrentDictionary generators foundSoFar coordinator chunk =
        Seq.collect generators chunk
        |> Seq.filter (ConcurrentSet.add foundSoFar)
        |> Seq.toArray
        |> Result
        |> Agent.post coordinator

    let genericChunkAndSendComputations sendLogic chunker data= 
        let mutable jobs = 0        
        for chunk in data |> chunker do
            sendLogic chunk                
            jobs <- jobs + 1
        jobs

    let genericChunkAndSendToWorkers M workers i chunker data =
        let mutable jobs = 0
        for chunk in data |> chunker do
            (Array.get workers (!i%M), chunk)
            ||> Agent.post
            incr i; jobs <- jobs + 1
        jobs

    let solveWithAgentAsyncs<'T when 'T: equality> G 
        { initData = (initData:seq<'T>); generators = generators } = 
        let chunkAndSend inbox = 
            let logic = logicWithMutableSet generators inbox
            genericChunkAndSendComputations <| fun chunk -> Async.Start <| async { logic chunk }
        let workPile = Agent.start <| agentLogicMutableSet chunkAndSend G
        timedRun workPile initData

    let solveWithAgentTasks<'T when 'T: equality> G 
        { initData = (initData:seq<'T>); generators = generators } =
        let chunkAndSend inbox = 
            let logic = logicWithMutableSet generators inbox
            genericChunkAndSendComputations <| fun chunk -> Task.Factory.StartNew(fun _ -> logic chunk) |> ignore
        let workPile = Agent.start <| agentLogicMutableSet chunkAndSend G
        timedRun workPile initData
   
    let solveWithAgentWorkers<'T when 'T:equality> M G 
        { initData = (initData:seq<'T>); generators = generators } =
        let i = ref 0
        let chunkAndSend workers _  = genericChunkAndSendToWorkers M workers i
        let workPile = Agent.start <| fun inbox ->
            let workers = Array.init M <| fun _ -> Agent.start(fun workerInbox ->
                let rec loop () = async {
                    let! chunk = Agent.receive workerInbox
                    logicWithMutableSet generators inbox chunk
                    return! loop()
                }
                loop()
            )
            agentLogicMutableSet (chunkAndSend workers) G inbox 
        timedRun workPile initData   

    let solveWithAgentWorkersAndConcurrentSet<'T when 'T:equality> M G 
        { initData = (initData:seq<'T>); generators = generators } =
        let i = ref 0
        let chunkAndSend workers _  = 
            genericChunkAndSendToWorkers M workers i
        let workPile = Agent.start <| fun inbox ->
            let foundSoFar = ConcurrentSet.create M 100000
            let workers = Array.init M <| fun _ -> Agent.start(fun workerInbox ->
                let rec loop () = async {
                    let! chunk = Agent.receive workerInbox
                    logicWithConcurrentDictionary generators foundSoFar inbox chunk
                    return! loop()
                }
                loop()
            )            
            agentLogicConcurrentSet foundSoFar (chunkAndSend workers) M G inbox 
        timedRun workPile initData

    let solveWithAgentConcurrentDictionary<'T when 'T: equality> M G 
        { initData = initData; generators = generators } = 
        let chunkAndSend foundSoFar inbox = 
            let logic = logicWithConcurrentDictionary generators foundSoFar inbox
            genericChunkAndSendComputations <| fun chunk -> Task.Factory.StartNew(fun _ -> logic chunk) |> ignore
        let workPile = Agent.start <| fun inbox ->
            let foundSoFar = ConcurrentSet.create<'T> M 100000
            agentLogicConcurrentSet foundSoFar (chunkAndSend foundSoFar) M G inbox
        timedRun workPile initData