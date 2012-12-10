namespace Orbit.Agent

module Mapper =
    open Orbit.Types 
    open System.Linq

    type internal MapperMessage<'T> =
        |Job of seq<'T>
        |Stop

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
                |Stop ->
                    ()
            }
        loop ()
    )

    open Helpers
    open System.Threading
    open System.Threading.Tasks
    type Mapper<'T when 'T: comparison>
        (
            coordinator: ICoordinator,
            nOfWorkers:int, chunkLimit:int,
            func : 'T seq -> seq<'T>
        ) =
        let dependency = ref Unchecked.defaultof<IAggregator<'T>>
        let workers = Array.init nOfWorkers <| fun _ -> actorBody dependency func
        let mutable i = 0
        interface IMapper<'T> with 
            member x.Map data =
                let mutable acc = 0
                for chunk in Seq.chunked chunkLimit data do
                    acc <- acc + 1
                    workers.[(Interlocked.Increment &i)%nOfWorkers].Post <| Job chunk    
                coordinator.Add <| acc - 1
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                for worker in workers do worker.Start()
            member x.Stop () =
                for worker in workers do worker.Post Stop
            member x.Dispose() =
                dependency := Unchecked.defaultof<IAggregator<'T>>
            
