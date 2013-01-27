namespace Orbit

//open Orbit.Solver
//open Orbit.Benchmarks
//
//module Main =
//    let inline solvers() = [ 
//        //SimpleSolver<'T>() :> OrbitSolver<_> 
//        //SimpleParSolver<'T>(8) :> OrbitSolver<_>
//        SimpleAgentSolver<'T>(8, 3000) :> OrbitSolver<_>
//    ]
//
//    [<EntryPoint>]
//    let main argv = 
//        let M = 8
//        let transformer (i:int) = bigint i
//        for solver in solvers() do 
//            let timer = System.Diagnostics.Stopwatch.StartNew()
//            let res = solver.Solve(Fibonaccis.definition transformer) 
//            timer.Stop()
//            res
//            |> Seq.length
//            |> printfn "%d %d" timer.ElapsedMilliseconds
//        System.Console.ReadLine() |> ignore   
//        0 // return an integer exit code

open Orbit.Solver
open Orbit.Benchmarks

open SimpleFunctions
open NotSimpleFunctions

module Main=
    let solvers M G = [ 
        solve
        solveWithPLinq(M)
        solveWithAgentAsyncs(G)
        solveWithAgentTasks(M,G)
        solveWithAgentWorkers(M,G)
    ]

    [<EntryPoint>]
    let main argv = 
        let M, G = 8, 5000
        let transformer (i:int) = int64 i
        for solve in solvers M G do 
            let res,timeElapsed = solve (Fibonaccis.definition transformer) 
            res
            |> Seq.length
            |> printfn "%d %d" timeElapsed
        System.Console.ReadLine() |> ignore   
        0 // return an integer exit code