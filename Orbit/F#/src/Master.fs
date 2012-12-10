namespace Orbit.Master

open Helpers
open System
open System.Linq
open System.Threading

open Orbit.Types 

type internal Coordinator(onComplete: unit -> unit) =
    let actor = Agent<_>.Start(fun inbox ->
        let remaining = ref 1
        let rec loop() = async {
            let! n = inbox.Receive()
            remaining := !remaining + n
            if !remaining = 0 && n = -1 then 
                onComplete()
            return! loop()                
        }
        loop()
    )  
    member x.Start() = actor.Start()

    interface ICoordinator with
        member x.Add n = actor.Post n
        member x.Dispose() = (actor:>IDisposable).Dispose()

type Coordinator2(onComplete: unit -> unit) =
    let mutable remaining = 1
    interface ICoordinator with
        member x.Add n = 
            Interlocked.Add(&remaining,n) |> ignore
            if remaining = 0 && n = -1 then 
                onComplete()
        member x.Dispose() = ()

type Master<'T when 'T:comparison>
    (
        M, N, chunkSize:int, 
        mapperF: int -> ICoordinator -> IMapper<'T>, 
        aggregatorF: int -> ICoordinator -> IAggregator<'T>,
        onComplete: IAggregator<'T> -> unit
    ) as this =
    let coordinator = new Coordinator2(fun () -> onComplete this.Aggregator) :> ICoordinator
    
    let aggregator = aggregatorF N coordinator :> IAggregator<_>
    let mapper = mapperF M coordinator :> IMapper<_>
    do
        mapper.Config aggregator
        aggregator.Config mapper

    member x.Aggregator = aggregator

    member x.StartBenchmark(initData : seq<'T>) =
        mapper.Start()
        aggregator.Start()
        aggregator.Store initData
    member x.FetchResults() = 
        aggregator.FetchResults()

    interface IDisposable with
        member x.Dispose() =
            mapper.Dispose()
            aggregator.Dispose()
            coordinator.Dispose()

