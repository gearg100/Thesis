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
open Orbit.Benchmarks.FibonaccisLong

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

[<TestClass>]
type ``Fibonacci Test with int64``() = 
    let ``Fibonacci Test with int64`` M N G = 
        use flag = new CountdownEvent(1)
        let funcs = funcs 1000871L
        let result = ref -1
        let timer = System.Diagnostics.Stopwatch.StartNew()
        use mapper = new Mapper<_, _>(M, G, mapF funcs, onComplete flag timer result)
        let aggregator = new Aggregator<_>(N)
        (mapper:>IDependent<_>).Config aggregator
        (aggregator:>IDependent<_>).Config mapper

        (mapper:>IDependent<_>).Start()
        (aggregator:>IDependent<_>).Start()
        (aggregator:>IAggregator<_>).Store (integers)
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