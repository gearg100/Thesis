namespace Orbit

open System.Linq
open Orbit
open Orbit.Types
open System
open System.Diagnostics
open System.Threading
open Orbit.Logic

[<AutoOpen>]
module TestData =
    let inline onComplete<'T when 'T:comparison> 
        (flag:CountdownEvent) (timer:Stopwatch) (result: (int64*int) option ref) (a:IAggregator<'T>) = 
        async {
            timer.Stop()
            let! set = a.FetchResults()
            result := Some(timer.ElapsedMilliseconds, set.Count)
            flag.Signal()
            |> ignore
        }
        |> Async.Start

open Orbit.Benchmarks

#if BigInt
open FibonaccisBigInt
#else
open FibonaccisLong
#endif

module Program =
    type MapperA = Orbit.Task.Mapper.Mapper<TElem,TElem>
    type AggregatorA = Orbit.Task.Aggregator.Aggregator<TElem>
    type MapperT = Orbit.Task.Mapper.Mapper<TElem,TElem>
    type AggregatorT = Orbit.Task.Aggregator.Aggregator<TElem>

    let inp = 1000871L

    let runAA M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs inp
        let result = ref None
        let timer = Stopwatch.StartNew()
        use mapper = new MapperA(M, G, mapF funcs, onComplete flag timer result)
        use aggregator = new AggregatorA(N)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
        flag.Wait()
        result.Value.Value

    let runTT M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs inp
        let result = ref None
        let timer = Stopwatch.StartNew()
        use mapper = new MapperT(M, G, mapF funcs, onComplete flag timer result)
        use aggregator = new AggregatorT(N)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
        flag.Wait()
        result.Value.Value

    let runTA M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs inp
        let result = ref None
        let timer = Stopwatch.StartNew()
        use mapper = new MapperT(M, G, mapF funcs, onComplete flag timer result)
        use aggregator = new AggregatorA(N)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
        flag.Wait()
        result.Value.Value
    
    [<EntryPoint>]
    let main _ =
        Console.Write("Give me nOfMappers: ")
        let M = int <| Console.ReadLine()
        Console.Write("Give me levelOfParallelism for hashset: ")
        let N = int <| Console.ReadLine()
        Console.Write("Give me chunkSize: ")
        let G = int <| Console.ReadLine()
        Console.Write("Choose Implementation (AA, TT or TA): ")
        let m = Console.ReadLine()
        let (time, result) = 
            match m.ToUpper() with 
            |"AA" -> runAA M N G
            |"TT" -> runTT M N G
            |"TA" -> runTA M N G
            |_ -> failwith "Invalid Implementation"
        printfn "Result: %d" result
        printfn "Time Elapsed: %d ms" time
        Console.WriteLine("Press <Enter> to exit")
        Console.ReadLine() |> ignore
        0


