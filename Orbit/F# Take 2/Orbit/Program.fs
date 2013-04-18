﻿namespace Orbit

open System
open Orbit.Solver
open Orbit.Benchmarks

open SimpleFunctions
open NotSimpleFunctions

module Main=
    let solvers M G = 
        dict [ 
            1, ("Sequential", solve)
            2, ("PLinq", solveWithPLinq)
            22, ("PLinq 2", solveWithPLinq2 M)
            3, ("Async Workflows", solveWithAgentAsyncs G)
            4, ("Tasks", solveWithAgentTasks G)
            5, ("Agents", solveWithAgentWorkers M G)
            6, ("Concurrent Dictionary", solveWithAgentConcurrentDictionary M G)
        ]

    [<EntryPoint>]
    let main argv = 
        Console.Write("Choose mode [1 -> int64, 2 -> bigint] (default = int64): ")
        let mode = 
            let flag, number = Console.ReadLine() |> Int32.TryParse
            if flag then number else 1
        Console.Write("Give me nOfMappers (default = ProcessorCount): ")
        let M = 
            let flag, number = Console.ReadLine() |> Int32.TryParse
            if flag then number else Environment.ProcessorCount
        Console.Write("Give me chunkSize (default = 3000): ")
        let G = 
            let flag, number = Console.ReadLine() |> Int32.TryParse
            if flag then number else 3000
        Console.Write("""Choose Implementation from [
    1 -> Sequential, 
    2 -> PLinq, 
    22 -> PLinq 2, 
    3 -> Async Workflows(nOfMappers = ProcessorCount),
    4 -> Tasks, 
    5 -> Agents,
    6 -> Concurrent Dictionary
] (default = 1): """  )
        let implementation = try int <| Console.ReadLine() with _ -> 1

        Console.Write("Give me l,d,f (space separated on the same line, f <= 10, default values = 200000,10000,8): ")
        let (l,d,f) = 
            try
                Console.ReadLine().Split(' ')
                |> Array.map(fun x -> x.Trim())
                |> fun ([|l;d;f|]) -> (Int32.Parse l, Int32.Parse d, Int32.Parse f)
            with _ -> 
                100000, 20000, 10
        Console.WriteLine() 
        match mode with
        | 1-> 
            let transformer (i:int) = int64 i
            let _, solve = (solvers M G).[implementation] 
            let res, timeElapsed = solve (Simple.definition transformer l d f)
            printfn "Result: %d - Time Elapsed: %d ms" (Seq.length res) timeElapsed
        | 2 ->
            let transformer (i:int) = bigint i
            let _, solve = (solvers M G).[implementation] 
            let res, timeElapsed = solve (Simple.definition transformer l d f)
            printfn "Result: %d - Time Elapsed: %d ms" (Seq.length res) timeElapsed
        | _ ->
            printfn "invalid mode"        
        Console.ReadLine() |> ignore   
        0 // return an integer exit code 