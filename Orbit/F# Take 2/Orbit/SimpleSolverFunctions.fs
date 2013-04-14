namespace Orbit.Solver

open System.Diagnostics

module SimpleFunctions =
    open Orbit

    let solve<'T when 'T: equality> { initData = initData; generators = generators } =
        let foundSoFar = MSet<'T>(initData)
        let rec helper current =
            if Seq.isEmpty current then
                foundSoFar :> seq<'T>
            else
                let nCurrent = 
                    current 
                    |> Seq.collect generators 
                    |> Seq.filter foundSoFar.Add
                    |> Seq.toArray
                helper nCurrent
        let timer = Stopwatch.StartNew()
        helper <| Seq.toArray initData, timer.ElapsedMilliseconds

    open System.Linq
    let solveWithPLinq<'T when 'T: equality> { initData = initData; generators = generators } =
        let foundSoFar = MSet<'T>(initData)
        let rec helper current =
            if Seq.isEmpty (current:seq<'T>) then
                foundSoFar :> seq<'T>
            else
                let nCurrent = 
                    current  
                        .AsParallel()
                        .SelectMany(generators)
                        .Where(not << foundSoFar.Contains)
                        .Distinct()
                        .ToList()
                foundSoFar.UnionWith nCurrent
                helper nCurrent
        let timer = Stopwatch.StartNew()
        helper initData, timer.ElapsedMilliseconds

    let solveWithPLinq2<'T when 'T: equality> M { initData = initData; generators = generators } =
        let foundSoFar = System.Collections.Concurrent.ConcurrentDictionary<'T,obj>(M, 5000000)
        let rec helper current =
            if Seq.isEmpty (current:seq<'T>) then
                foundSoFar.Keys :> seq<_>
            else
                let nCurrent = 
                    current  
                        .AsParallel()
                        .SelectMany(generators)
                        .Where(fun x -> foundSoFar.TryAdd(x, null))
                        .ToList()
                helper nCurrent
        let timer = Stopwatch.StartNew()
        for x in initData do foundSoFar.TryAdd(x, null) |> ignore
        helper initData, timer.ElapsedMilliseconds

    open Helpers
    open System.Threading
    let solveWithAgentAsyncs<'T when 'T: equality> G { initData = initData; generators = generators } = 
        use flag = new ManualResetEventSlim()
        let foundSoFar = MSet<'T>()
        let workPile = Agent.start <| fun inbox ->
            let remaining = ref 1
            let rec loop() = async {
                let! data = Agent.receive inbox
                let data = data |> Array.filter (not << contains foundSoFar)
                unionWith foundSoFar data
                let jobs = ref -1
                for chunk in data |> Seq.chunked_opt_2 G do
                    Async.Start <| async {
                        Seq.collect generators chunk
                        |> Seq.distinct
                        |> Array.ofSeq
                        |> Agent.post inbox
                    }
                    incr jobs
                remaining := !remaining + !jobs
                if (!remaining = 0 && !jobs = -1) then
                    ManualResetEventSlim.set flag |> ignore
                else
                    return! loop()
            }
            loop()
        let timer = Stopwatch.StartNew()
        Agent.post workPile (Array.ofSeq initData)
        ManualResetEventSlim.wait flag
        timer.Stop()
        foundSoFar :> seq<_>, timer.ElapsedMilliseconds