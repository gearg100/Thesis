﻿namespace Orbit

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
            do! Async.Sleep(1000)
            let! set = a.FetchResults()
            result := Some(timer.ElapsedMilliseconds, set.Count)
            try
                flag.Signal() |> ignore
            with
            | _ -> Console.WriteLine("already signaled")            
        }
        |> Async.Start

open Orbit.Benchmarks

#if BigInt
open FibonaccisBigInt
#else
open FibonaccisLong
#endif
open Orbit.Master



module Program =
    type MapperA<'T when 'T: comparison> = Orbit.Agent.Mapper.Mapper<'T>
    type AggregatorA<'T when 'T: comparison> = Orbit.Agent.Aggregator.Aggregator<'T>
    type MapperT<'T when 'T: comparison> = Orbit.Task.Mapper.Mapper<'T>
    type AggregatorT<'T when 'T: comparison> = Orbit.Task.Aggregator.Aggregator<'T>

#if BigInt
    let inp = 1000871I
#else
    let inp = 1000871L
#endif  

    let runAA M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs inp
        let result = ref None
        let timer = Stopwatch()
        let mapperF M coordinator = new MapperA<_>(coordinator, M, G, mapF funcs) :> IMapper<_>
        let aggregatorF N coordinator = new AggregatorA<_>(coordinator, N) :> IAggregator<_>
        use master = new Master<_>(M,N,G, mapperF, aggregatorF, onComplete flag timer result)
        timer.Start()
        master.StartBenchmark integers
        flag.Wait()
        result.Value.Value

    let runTT M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs inp
        let result = ref None
        let timer = Stopwatch()
        let mapperF M coordinator = new MapperT<_>(coordinator, M, G, mapF funcs) :> IMapper<_>
        let aggregatorF N coordinator = new AggregatorT<_>(coordinator, N) :> IAggregator<_>
        use master = new Master<_>(M,N,G, mapperF, aggregatorF, onComplete flag timer result)
        timer.Start()
        master.StartBenchmark integers
        flag.Wait()
        result.Value.Value

    let runTA M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs inp
        let result = ref None
        let timer = Stopwatch()
        let mapperF M coordinator = new MapperT<_>(coordinator, M, G, mapF funcs) :> IMapper<_>
        let aggregatorF N coordinator = new AggregatorA<_>(coordinator, N) :> IAggregator<_>
        use master = new Master<_>(M,N,G, mapperF, aggregatorF, onComplete flag timer result)
        timer.Start()
        master.StartBenchmark integers
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
        Console.WriteLine()
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


