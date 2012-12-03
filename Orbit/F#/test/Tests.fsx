#load 
    "..\src\Helpers.fs"
    "..\src\OrbitTypes.fs"
    "..\src\Mapper.fs"
    "..\src\Aggregator.fs"
    "..\src\OrbitLogic.fs"
    "..\src\OrbitBench.fs"
        

open System 
open System.Linq

open Orbit
open Orbit.Types
open Orbit.Logic
open Orbit.Benchmarks
open Mapper
open Aggregator

open System
open System.Diagnostics
open System.Threading

let inline onComplete<'T when 'T:comparison> 
    (flag:CountdownEvent) (timer:Stopwatch) (result: seq<'T> ref) (a:IAggregator<'T>) = 
    async {
        let tock = timer.Stop()
        let! set = a.FetchResults()
        result := set |> Set.toSeq
        flag.Signal() |> ignore
        printfn "Time Elapsed: %d ms" timer.ElapsedMilliseconds
    }
    |> Async.Start

open SimpleInt
let M, N, G = 1, 1, 3000

[<AutoOpen>]
module ``Simple Mapper Test`` =    
    let test<'T when 'T:comparison> (funcs:seq<'T->'T>) integers=
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        use mapper = new Mapper<'T, 'T>(M, G, mapF funcs, ignore)
        let result = ref Seq.empty
        let aggregator = {
            new IAggregator<_> with
                member x.Store data =
                    result := data
                    flag.Signal()|> ignore
                member x.FetchResults() = 
                    raise <| NotImplementedException()
        }
        (mapper:>IDependent<_>).Config aggregator
        (mapper:>IDependent<_>).Start()
        (mapper:>IMapper<_>).Map integers
        flag.Wait()
        printfn "%A" (!result |> Seq.toList)

Console.WriteLine "Simple Mapper Test"
test (funcs 21) (integers)
Console.WriteLine()

[<AutoOpen>]
module ``Simple Aggregator Test`` = 
    let test<'T when 'T:comparison> data=
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        let result = ref Seq.empty
        let mapper = {
            new IMapper<_> with
                member x.Map data = 
                    result := data
                    flag.Signal() |> ignore
        }
        use aggregator = new Aggregator<'T>(N,aggregateF)
        (aggregator:>IDependent<_>).Config mapper
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store data
        flag.Wait()
        printfn "%A" (!result |> Seq.toList)

Console.WriteLine "Simple Aggregator Test"
test integers
Console.WriteLine()

[<AutoOpen>]
module ``Mapper -> Aggregator Test`` =
    let test<'T when 'T:comparison> funcs integers =        
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        let result = ref Seq.empty
        use mapper = new Mapper<'T, 'T>(M, G, mapF funcs, onComplete flag timer result)
        use aggregator = new Aggregator<'T>(N,aggregateF)
        let mapper' = {
            new IMapper<_> with
                member x.Map data = 
                    result := data
                    flag.Signal() |> ignore
        }
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper'

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()

        (mapper:>IMapper<_>).Map integers

        flag.Wait()
        printfn "%A" (!result |> Seq.toList)

Console.WriteLine "Mapper -> Aggregator Pipeline Test"
test (funcs 20) (integers)
Console.WriteLine()

Console.WriteLine "Orbit System Test with FibonaccisBigInt Benchmarks"
open FibonaccisBigInt

[<AutoOpen>]
module MapperAggregatorFeedBackTest =
    let test<'T when 'T:comparison> M N G funcs integers =
        use flag = new CountdownEvent(1)
        let timer = Stopwatch.StartNew()
        let result = ref Seq.empty
        use mapper = new Mapper<'T, 'T>(M, G, mapF funcs, onComplete flag timer result)
        use aggregator = new Aggregator<'T>(N,aggregateF)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
        
        flag.Wait()
        printfn "%A" <| (!result).Count()

Console.WriteLine()
let test M N G = 
    test M N G (funcs 1000871I) (integers)

for m in [1;2;4;8;16] do
    printfn "(M, N, G) = (%d, 1, 3000)" m
    test m 1 G

for m, n in [(2,2);(4,2);(6,3);(8,2);(16,2);(16,4)] do
    printfn "(M, N, G) = (%d, %d, 3000)" m n
    test m n G