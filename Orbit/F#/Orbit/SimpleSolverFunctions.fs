namespace Orbit.Solver

open System.Diagnostics

module SimpleFunctions =
    open Orbit
    open Helpers

    let solve<'T when 'T: equality> { initData = initData; generators = generators } =
        let foundSoFar = MutableSet(initData)
        let rec helper current =
            if Seq.isEmpty current then
                foundSoFar :> seq<'T>
            else
                let nCurrent = 
                    current 
                    |> Seq.collect generators 
                    |> Seq.filter (MutableSet.add foundSoFar)
                    |> Seq.toArray
                helper nCurrent
        let timer = Stopwatch.StartNew()
        helper <| Seq.toArray initData, timer.ElapsedMilliseconds

    open System.Linq
    let solveWithPLinq<'T when 'T: equality> { initData = initData; generators = generators } =
        let foundSoFar = MutableSet(initData)
        let rec helper current =
            if Seq.isEmpty (current:seq<'T>) then
                foundSoFar :> seq<'T>
            else
                let nCurrent = 
                    current  
                        .AsParallel()                       
                        .SelectMany(generators)
                        .Where(not << MutableSet.contains foundSoFar)
                        .Distinct()
                        .ToList()
                MutableSet.unionWith foundSoFar nCurrent
                helper nCurrent
        let timer = Stopwatch.StartNew()
        helper initData, timer.ElapsedMilliseconds

    let solveWithPLinq2<'T when 'T: equality> M { initData = initData; generators = generators } =
        let foundSoFar = ConcurrentSet.create M 1000000
        let rec helper current =
            if Seq.isEmpty (current:seq<'T>) then
                foundSoFar.Keys :> seq<_>
            else
                let nCurrent = 
                    current  
                        .AsParallel()
                        .WithDegreeOfParallelism(M)
                        .SelectMany(generators)
                        .Where(ConcurrentSet.add foundSoFar)
                        .ToList()
                helper nCurrent
        let timer = Stopwatch.StartNew()
        for x in initData do ConcurrentSet.add foundSoFar x |> ignore
        helper initData, timer.ElapsedMilliseconds

    type Parallel = System.Threading.Tasks.Parallel
    let solveWithParallelForEach<'T when 'T: equality> M { initData = initData; generators = generators } =
        let foundSoFar = ConcurrentSet.create M 1000000
        let rec helper current =
            if Seq.isEmpty (current:seq<'T>) then
                foundSoFar.Keys :> seq<_>
            else
                let res = System.Collections.Concurrent.ConcurrentBag<'T>()
                Parallel.ForEach(current,
                    generators
                    >> Seq.filter (ConcurrentSet.add foundSoFar)
                    >> Seq.iter res.Add
                ) |> ignore
                helper res
        let timer = Stopwatch.StartNew()
        for x in initData do ConcurrentSet.add foundSoFar x |> ignore
        helper initData, timer.ElapsedMilliseconds