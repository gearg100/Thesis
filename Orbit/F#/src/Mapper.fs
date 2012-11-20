namespace Orbit

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
    open System.Diagnostics
    open System.Threading
    open System.Threading.Tasks
    type Mapper<'TSource, 'TResult when 'TSource : equality and 'TResult: comparison>
        (
            nOfWorkers:int, grainSize:int, 
            func : 'TSource seq -> seq<'TResult>,
            onComplete: IAggregator<'TResult> -> unit
        ) =
        let dependency = ref Unchecked.defaultof<IAggregator<'TResult>>
        let workers = Array.init nOfWorkers (fun _ -> actorBody dependency func)
        let mutable i = 1
        let mutable remaining = 0
        interface IMapper<'TSource> with 
            member x.Map data =
                Interlocked.Increment &remaining |> ignore
                if not <| Seq.isEmpty data then 
                    for chunk in data |> Seq.chunked grainSize do
                        workers.[(Interlocked.Increment &i)%nOfWorkers].Post <| Job chunk
                elif i = remaining then 
                    onComplete !dependency           
        interface IDependent<IAggregator<'TResult>> with
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                workers |> Seq.iter (fun worker-> worker.Start())
            member x.Stop () =
                workers 
                |> Seq.map (fun worker-> worker.PostAndAsyncReply Stop)
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
            member x.Dispose() =
                dependency := Unchecked.defaultof<IAggregator<'TResult>>
            
