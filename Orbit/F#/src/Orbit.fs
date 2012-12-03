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
open FibonaccisLong

module Program =
    type Mapper = Orbit.Tasks.Mapper.Mapper<TElem,TElem>
    type Aggregator = Orbit.Agent.Aggregator.Aggregator<TElem>
    let run M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs 1000871L
        let result = ref None
        let timer = Stopwatch.StartNew()
        use mapper = new Mapper(M, G, mapF funcs, onComplete flag timer result)
        let aggregator = new Aggregator(N)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
        flag.Wait()
        result.Value.Value
    
    [<EntryPoint>]
    let main _ =
        let (time, result) = run 4 2 3000
        printfn "Result: %d" result
        printfn "Time Elapsed: %d ms" time
        Console.WriteLine("Press <Enter> to exit")
        Console.ReadLine() |> ignore
        0


