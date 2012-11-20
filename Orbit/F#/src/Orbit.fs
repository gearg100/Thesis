namespace Orbit

open System.Linq
open Orbit
open Mapper
open Aggregator
open Orbit.Types
open System
open System.Diagnostics
open System.Threading
open Orbit.Logic

[<AutoOpen>]
module TestData =
    let inline onComplete<'T when 'T:comparison> 
        (flag:CountdownEvent) (timer:Stopwatch) (result: int ref) (a:IAggregator<'T>) = 
        async {
            let tock = timer.Stop()
            let! set = a.FetchResults()
            result := set.Count
            flag.Signal()
            |> ignore
            printfn "Time Elapsed: %d ms" timer.ElapsedMilliseconds
        }
        |> Async.Start

open Orbit.Benchmarks
open FibonaccisBigInt

module Program =
    let run M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs 1000871I
        let result = ref -1
        let timer = System.Diagnostics.Stopwatch.StartNew()
        use mapper = new Mapper<_, _>(M, G, mapF funcs, onComplete flag timer result)
        let aggregator = new Aggregator<_>(N,aggregateF)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
        flag.Wait()
        !result
    
    [<EntryPoint>]
    let main _ =
        run 4 2 3000
        |> printfn "%d"
        Console.WriteLine("Press any key to exit")
        Console.ReadKey() |> ignore
        0


