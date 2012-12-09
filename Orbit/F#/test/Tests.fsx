#r "../target/Debug/Common.dll"
#load 
    "../src/AgentImplementation/Mapper.fs"
    "../src/AgentImplementation/Aggregator.fs"
    "../src/OrbitLogic.fs"
    "../src/BenchMarks/OrbitBench.fs"
        

open System 
open System.Linq

open Orbit
open Orbit.Types
open Orbit.Logic
open Orbit.Benchmarks
open Orbit.Agent.Mapper
open Orbit.Agent.Aggregator

open System
open System.Diagnostics
open System.Threading
open Helpers

let inline onComplete<'T when 'T:comparison> 
    (flag:CountdownEvent) (timer:Stopwatch) (result: seq<'T> ref) (a:IAggregator<'T>) = 
    async {
        let tock = timer.Stop()
        let! set = a.FetchResults()
        result := set |> Set.toSeq
        try 
            flag.Signal() |> ignore
        with 
        | _ -> printfn "Already signaled"
        printfn "Time Elapsed: %d ms" timer.ElapsedMilliseconds
    }
    |> Async.RunSynchronously


open SimpleInt
let M, N, G = 1, 1, 3000

[<AutoOpen>]
module ``Simple Mapper Test`` =  
    let mutable remaining = 1
    let chunkFunc (sq: seq<'T>) =
            Interlocked.Decrement(&remaining) |> ignore
            if not <| Seq.isEmpty sq then
                Seq.chunked G sq
            else
                Seq.empty
    let test<'T when 'T:comparison> (funcs:seq<'T->'T>) integers=
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        use mapper = new Mapper<'T>(M, mapF funcs, chunkFunc) :> IMapper<_>
        let result = ref Seq.empty
        let aggregator = {
            new IAggregator<_> with
                member x.Store data =
                    result := data
                    flag.Signal()|> ignore
                member x.FetchResults() = 
                    raise <| NotImplementedException()
                member x.Config(_) = raise <| NotImplementedException()
                member x.Start() = raise <| NotImplementedException()
                member x.Stop() = raise <| NotImplementedException()
                member x.Dispose() = raise <| NotImplementedException()
        }
        mapper.Config aggregator
        mapper.Start()
        mapper.Map integers
        flag.Wait()
        printfn "%A" (!result |> Seq.toList)

Console.WriteLine "Simple Mapper Test"
test (funcs 21) (integers)
Console.WriteLine()

[<AutoOpen>]
module ``Simple Aggregator Test`` = 
    let mutable remaining = 1
    let groupFunc (sq: seq<'T>) =
        let chunks = Seq.groupBy (indexOf N) sq
        Interlocked.Add(&remaining, Seq.length chunks) |> ignore
        chunks
    let test<'T when 'T:comparison> data=
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        let result = ref Seq.empty
        let mapper = {
            new IMapper<_> with
                member x.Map data = 
                    result := data
                    flag.Signal() |> ignore
                member x.Config(_) = raise <| NotImplementedException()
                member x.Start() = raise <| NotImplementedException()
                member x.Stop() = raise <| NotImplementedException()
                member x.Dispose() = raise <| NotImplementedException()
        }
        use aggregator = new Aggregator<'T>(N,groupFunc) :> IAggregator<_>
        aggregator.Config mapper
        aggregator.Start()
        aggregator.Store data
        flag.Wait()
        printfn "%A" (!result |> Seq.toList)

Console.WriteLine "Simple Aggregator Test"
test integers
Console.WriteLine()

[<AutoOpen>]
module ``Mapper -> Aggregator Test`` =
    let mutable remaining = 1
    let chunkFunc (sq: seq<'T>) =
            Interlocked.Decrement(&remaining) |> ignore
            if not <| Seq.isEmpty sq then
                Seq.chunked G sq
            else
                Seq.empty
    let groupFunc (sq: seq<'T>) =
        let chunks = Seq.groupBy (indexOf N) sq
        Interlocked.Add(&remaining, Seq.length chunks) |> ignore
        chunks
    let test<'T when 'T:comparison> funcs integers =        
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        let result = ref Seq.empty
        use mapper = new Mapper<'T>(M, mapF funcs, chunkFunc) :> IMapper<_>
        use aggregator = new Aggregator<'T>(N,groupFunc) :> IAggregator<_>
        let mapper' = {
            new IMapper<_> with
                member x.Map data = 
                    result := data
                    flag.Signal() |> ignore
                member x.Config(_) = raise <| NotImplementedException()
                member x.Start() = raise <| NotImplementedException()
                member x.Stop() = raise <| NotImplementedException()
                member x.Dispose() = raise <| NotImplementedException()
        }
        mapper.Config aggregator
        aggregator.Config mapper'

        mapper.Start()
        aggregator.Start()

        mapper.Map integers

        flag.Wait()
        printfn "%A" (!result |> Seq.toList)

Console.WriteLine "Mapper -> Aggregator Pipeline Test"
test (funcs 20) (integers)
Console.WriteLine()

Console.WriteLine "Orbit System Test with FibonaccisBigInt Benchmarks"
open FibonaccisLong

[<AutoOpen>]
module MapperAggregatorFeedBackTest =
    type Msg = Add of int | Print
    let mutable remaining = 1
    let count = ref 0
    let printer = Agent.Start(fun inbox -> 
        let rec loop() = async{
            let! msg = inbox.Receive()
            match msg with
            | Add n -> count := !count + n
            | Print -> printfn "%d" !count
            return! loop()
        }
        loop()
    )
    let chunkFunc G onFinish (sq: seq<'T>)=
        Interlocked.Decrement(&remaining) |> ignore
        if not <| Seq.isEmpty sq then
            //printer.Post <| Add (Seq.length sq)
            let chunked = Seq.chunked G sq
            Interlocked.Add(&remaining, Seq.length chunked) |> ignore
            chunked
        else
            if remaining = 0 then onFinish()
            //printer.Post Print
            Seq.empty
    let groupFunc N (sq: seq<'T>) =
        let chunks = Seq.groupBy (indexOf N) sq
        Interlocked.Add(&remaining, Seq.length chunks - 1) |> ignore
        chunks
    let test<'T when 'T:comparison> M N G funcs integers =
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        let result = ref Seq.empty
        count := 0
        let aggregator = new Aggregator<'T>(N,groupFunc N) :> IAggregator<_>
        let mapper = new Mapper<'T>(M, mapF funcs, chunkFunc G (fun () -> onComplete flag timer result aggregator)) :> IMapper<_>
        mapper.Config aggregator
        aggregator.Config mapper

        mapper.Start()
        aggregator.Start()

        aggregator.Store (integers)

        flag.Wait()
        printfn "%A" <| (!result).Count()
        

Console.WriteLine()
let test M N G = 
    test M N G (funcs 1000871L) (integers)

for m in [1;2;4;8;16] do
    printfn "(M, N, G) = (%d, 1, 3000)" m
    test m 1 G

for m, n in [(2,2);(4,2);(6,3);(8,2);(16,2);(16,4)] do
    printfn "(M, N, G) = (%d, %d, 3000)" m n
    test m n G