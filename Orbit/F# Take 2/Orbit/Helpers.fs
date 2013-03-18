module Helpers

type Agent<'T> = MailboxProcessor<'T>
module Agent = 
    let start func = Agent.Start func
    let post (inbox:MailboxProcessor<_>) message = inbox.Post message
    let postAndAsyncReply (inbox:MailboxProcessor<_>) messageFunc = inbox.PostAndAsyncReply messageFunc
    let receive (inbox:MailboxProcessor<_>) = inbox.Receive()

module AsyncReplyChannel = 
    let reply (replyChannel:AsyncReplyChannel<_>) data = replyChannel.Reply data

open System.Threading
open System.Collections.Generic
module ManualResetEventSlim =
    let set (flag: ManualResetEventSlim) = flag.Set()
    let wait (flag: ManualResetEventSlim) = flag.Wait()

let contains (collection: System.Collections.Generic.ICollection<_>) data = collection.Contains data
let unionWith (set:HashSet<_>) seq = set.UnionWith seq
let incSafe (i:int ref):int = Interlocked.Increment i

[<RequireQualifiedAccess>]
module Seq =
    open System.Linq
    open System.Collections.Generic 
    type internal ChunkedEnumerator<'a>(inSeq:seq<'a>,chunkSize:int) =
        let mutable s = inSeq.GetEnumerator()
        let mutable current = Unchecked.defaultof<_>
        interface System.Collections.IEnumerator with
            member x.MoveNext() = 
                let b = s.MoveNext()
                if b then 
                    current <-
                        let lst = List<_>(chunkSize)
                        lst.Add s.Current
                        let mutable i = chunkSize - 1
                        while i > 0 && s.MoveNext() do
                            lst.Add s.Current
                            i <- i - 1
                        lst
                else
                    current <- Unchecked.defaultof<_>
                b                    
            member x.Current = (x :> IEnumerator<_>).Current :> obj
            member x.Reset() = 
                s <- inSeq.GetEnumerator()
                current <- Unchecked.defaultof<_>
        interface System.Collections.Generic.IEnumerator<seq<'a>> with
            member x.Current = current :> _
        interface System.IDisposable with
            member x.Dispose() =
                s <- Unchecked.defaultof<_>
                current <- Unchecked.defaultof<_>
    let chunked (chunkSize:int) sq :seq<_> = {
        new IEnumerable<seq<'a>> with
            member x.GetEnumerator() = new ChunkedEnumerator<'a>(sq, chunkSize) :> System.Collections.Generic.IEnumerator<_>
            member x.GetEnumerator() = new ChunkedEnumerator<'a>(sq, chunkSize) :> System.Collections.IEnumerator
    }

module CustomTaskSchedulers =
    open System
    open System.Threading
    open System.Threading.Tasks
    open System.Collections.Concurrent

    // translated from http://code.msdn.microsoft.com/Samples-for-Parallel-b4b76364/sourcecode?fileId=44488&pathId=1111204181
    /// <summary> 
    /// Provides a task scheduler that ensures a maximum concurrency level while 
    /// running on top of the ThreadPool. 
    /// </summary> 
    type LimitedConcurrencyLevelTaskScheduler(maxDegreeOfParallelism) =
        inherit TaskScheduler()
        /// <summary>Whether the current thread is processing work items.</summary> 
        [<ThreadStatic; DefaultValue>] 
        static val mutable private currentThreadIsProcessingItems: bool
        /// <summary>The list of tasks to be executed.</summary>
        let tasks = ConcurrentQueue<Task>()// protected by lock tasks
        /// <summary>Whether the scheduler is currently processing work items.</summary> 
        let mutable delegatesQueuedOrRunning = 0 //protected by lock(tasks)
        do if maxDegreeOfParallelism < 1 then
            raise <| ArgumentOutOfRangeException("maxDegreeOfParallelism")

        /// <summary> 
        /// Informs the ThreadPool that there's work to be executed for this scheduler. 
        /// </summary> 
        member private x.notifyThreadPoolOfPendingWork() =
            let rec loop() =
                // Get the next item from the queue                     
                let mutable item : Task = null
                if tasks.TryDequeue(&item) then
                    // Execute the task we pulled out of the queue 
                    x.TryExecuteTask item |> ignore
                    loop()
                else
                    // When there are no more items to be processed, 
                    // note that we're done processing, and get out.
                    delegatesQueuedOrRunning <- delegatesQueuedOrRunning - 1   
            ThreadPool.UnsafeQueueUserWorkItem <| (WaitCallback(fun _ ->
                // Note that the current thread is now processing work items. 
                // This is necessary to enable inlining of tasks into this thread.
                LimitedConcurrencyLevelTaskScheduler.currentThreadIsProcessingItems <- true
                try
                    // Process all available items in the queue. 
                    loop()
                finally
                    // We're done processing items on the current thread
                    LimitedConcurrencyLevelTaskScheduler.currentThreadIsProcessingItems <- false
            ), null)
            |> ignore

        /// <summary>Queues a task to the scheduler.</summary> 
        /// <param name="task">The task to be queued.</param>
        override x.QueueTask (task:Task) =
            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another.
            tasks.Enqueue task
            if delegatesQueuedOrRunning < maxDegreeOfParallelism then
                delegatesQueuedOrRunning <- delegatesQueuedOrRunning + 1
                x.notifyThreadPoolOfPendingWork()

        member private x.TryExecuteTask task = base.TryExecuteTask task

        /// <summary>Attempts to execute the specified task on the current thread.</summary> 
        /// <param name="task">The task to be executed.</param> 
        /// <param name="taskWasPreviouslyQueued"></param> 
        /// <returns>Whether the task could be executed on the current thread.</returns> 
        override x.TryExecuteTaskInline(task, taskWasPreviouslyQueued) =
            // If this thread isn't already processing a task, we don't support inlining 
            LimitedConcurrencyLevelTaskScheduler.currentThreadIsProcessingItems &&
            // If the task was previously queued, remove it from the queue & try to run it
            x.TryDequeue task && base.TryExecuteTask task

        /// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary> 
        /// <param name="task">The task to be removed.</param> 
        /// <returns>Whether the task could be found and removed.</returns> 
        override x.TryDequeue task =
            raise <| NotSupportedException()

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary> 
        override x.MaximumConcurrencyLevel = maxDegreeOfParallelism

        /// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary> 
        /// <returns>An enumerable of the tasks currently scheduled.</returns> 
        override x.GetScheduledTasks() =
            tasks.ToArray() :> seq<_>
            
        

