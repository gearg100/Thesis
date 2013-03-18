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
                    |> Seq.filter (not << foundSoFar.Contains) 
                    |> Seq.distinct
                    |> Seq.toArray
                foundSoFar.UnionWith nCurrent
                helper nCurrent
        let timer = Stopwatch.StartNew()
        helper <| Seq.toArray initData, timer.ElapsedMilliseconds

    open System.Linq
    let solveWithPLinq<'T when 'T: equality> M { initData = initData; generators = generators } =
        let foundSoFar = MSet<'T>(initData)
        let rec helper current =
            if Seq.isEmpty (current:seq<'T>) then
                foundSoFar :> seq<'T>
            else
                let nCurrent = 
                    current  
                        .AsParallel()
                        .WithDegreeOfParallelism(M)
                        .SelectMany(generators)
                        .Where(not << foundSoFar.Contains)
                        .Distinct()
                        .ToList()
                foundSoFar.UnionWith nCurrent
                helper nCurrent
        let timer = Stopwatch.StartNew()
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
                for chunk in data |> Seq.distinct |> Seq.chunked G |> Seq.toArray do
                    Async.Start <| async {
                        Seq.collect generators chunk
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
        foundSoFar :> seq<_>, timer.ElapsedMilliseconds