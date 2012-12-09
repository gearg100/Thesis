namespace Orbit.Agent

module Mapper =
    open Orbit.Types 
    open System.Linq

    type internal MapperMessage<'T> =
        |Job of seq<'T>
        |Stop of AsyncReplyChannel<unit>

    let inline internal actorBody<'T,'TResult when 'TResult: comparison> 
        (reducer:IAggregator<'TResult> ref) 
        (func: 'T seq -> seq<'TResult>)
        = new Agent<MapperMessage<'T>>(fun inbox ->
        let rec loop () = 
            async {
                let! msg = inbox.Receive()
                match msg with
                |Job set ->
                    let res = func set
                    (!reducer).Store <| res
                    return! loop()
                |Stop channel ->
                    channel.Reply ()
            }
        loop ()
    )

    open Helpers
    open System.Threading
    open System.Threading.Tasks
    type Mapper<'T when 'T: comparison>
        (
            nOfWorkers:int, 
            func : 'T seq -> seq<'T>,
            chunkFunc: seq<'T> -> seq<seq<'T>>
        ) =
        let dependency = ref Unchecked.defaultof<IAggregator<'T>>
        let workers = Array.init nOfWorkers (fun _ -> actorBody dependency func)
        let mutable i = 0
        interface IMapper<'T> with 
            member x.Map data =
                for chunk in chunkFunc data do
                    workers.[(Interlocked.Increment &i)%nOfWorkers].Post <| Job chunk           
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                for worker in workers do worker.Start()
            member x.Stop () =
                workers 
                |> Seq.map (fun worker-> worker.PostAndAsyncReply Stop)
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
            member x.Dispose() =
                dependency := Unchecked.defaultof<IAggregator<'T>>
            
