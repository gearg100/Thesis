namespace Orbit.Master

open Helpers
open System
open System.Threading

open Orbit.Types   

type Master<'T when 'T:comparison>
    (
        M, N, chunkSize:int, 
        mapperF: int -> (seq<'T> -> seq<seq<'T>>) -> IMapper<'T>, 
        aggregatorF: int -> (seq<'T> -> groupedSeq<'T>) -> IAggregator<'T>,
        onComplete: IAggregator<'T> -> unit
    ) =

    let mutable remaining = 0
    let chunkFunc aggregator (sq: seq<'T>) =
        Interlocked.Decrement(&remaining) |> ignore
        if not <| Seq.isEmpty sq then
            let chunked = Seq.chunked chunkSize sq
            Interlocked.Add(&remaining, Seq.length chunked) |> ignore
            chunked
        else
            if remaining = 0 then onComplete aggregator
            Seq.empty
    let groupFunc (sq: seq<'T>) =
        let chunks = Seq.groupBy (indexOf N) sq
        Interlocked.Add(&remaining, Seq.length chunks - 1) |> ignore
        chunks

    let aggregator = aggregatorF N groupFunc      
    let mapper = mapperF M (chunkFunc aggregator)
    do
        mapper.Config aggregator
        aggregator.Config mapper

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

