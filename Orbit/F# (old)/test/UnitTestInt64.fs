namespace Orbit.UnitTest

open System
open System.Linq
open System.Threading
open System.Diagnostics
open Microsoft.VisualStudio.TestTools.UnitTesting

open Orbit
open Orbit.Types
open Orbit.Agent
open Aggregator
open Mapper
open Orbit.Logic
open Orbit.Benchmarks

open Orbit.Master

[<AutoOpen>]
module TestData =
    let inline onComplete<'T when 'T:comparison> 
        (flag:CountdownEvent) (timer:Stopwatch) (result: int ref) (a:IAggregator<'T>) = 
        async {
            let tock = timer.Stop()
            do! Async.Sleep(1000)
            let! set = a.FetchResults()
            result := set.Count
            try
                flag.Signal() |> ignore
            with
            |_ -> printfn "already signaled"
            printfn "Time Elapsed: %d ms" timer.ElapsedMilliseconds
        }
        |> Async.Start

[<TestClass>]
type ``Fibonacci Test with int64``() = 
    let ``Fibonacci Test with int64`` M N G = 
        let flag = new CountdownEvent(1)
        let funcs,integers = Fibonaccis.definition int64 1000871
        let result = ref -1
        let timer = Stopwatch()
        let mapperF M coordinator = new Mapper<_>(coordinator, M, G, funcs) :> IMapper<int64>
        let aggregatorF N coordinator = new Aggregator<_>(coordinator, N) :> IAggregator<int64>
        use master = new Master<_>(M,N,G, mapperF, aggregatorF, onComplete flag timer result)
        timer.Start()
        master.StartBenchmark(integers)
        flag.Wait()
        Assert.AreEqual(1801462, !result)
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (1, 1, 3000)`` () =
        ``Fibonacci Test with int64`` 1 1 3000
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (2, 1, 3000)`` () =
        ``Fibonacci Test with int64`` 2 1 3000
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (4, 1, 3000)`` () =
        ``Fibonacci Test with int64`` 4 1 3000
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (4, 2, 3000)`` () =
        ``Fibonacci Test with int64`` 4 2 3000
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (8, 1, 3000)`` () =
        ``Fibonacci Test with int64`` 8 1 3000
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (8, 2, 3000)`` () =
        ``Fibonacci Test with int64`` 8 2 3000        
    [<TestMethod>]
    member x.``Fibonacci Test with int64, (M,N,G) = (5, 3, 3000)`` () =
        ``Fibonacci Test with int64`` 5 3 3000