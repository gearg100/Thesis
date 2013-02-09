namespace Orbit

open System
open Orbit.Solver
open Orbit.Benchmarks

open SimpleFunctions
open NotSimpleFunctions

module Main=
    let solvers M G = 
        dict [ 
            1, ("Sequential", solve)
            2, ("PLinq", solveWithPLinq M)
            3, ("Async Workflows", solveWithAgentAsyncs G)
            4, ("Tasks", solveWithAgentTasks M G)
            5, ("Agents", solveWithAgentWorkers M G)
        ]

    [<EntryPoint>]
    let main argv = 
        Console.Write("Choose mode [1 -> int64, 2 -> bigint] (default = int64):")
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
    3 -> Async Workflows(nOfMappers = ProcessorCount), 
    4 -> Tasks, 
    5 -> Agents
]: """  )
        let choice = int <| Console.ReadLine()
        Console.WriteLine() 
        match mode with
        | 1-> 
            let transformer (i:int) = int64 i
            let _, solve = (solvers M G).[choice] 
            let res, timeElapsed = solve (Fibonaccis.definition transformer)
            printfn "Result: %d - Time Elapsed: %d ms" (Seq.length res) timeElapsed
        | 2 ->
            let transformer (i:int) = bigint i
            let _, solve = (solvers M G).[choice] 
            let res, timeElapsed = solve (Fibonaccis.definition transformer)
            printfn "Result: %d - Time Elapsed: %d ms" (Seq.length res) timeElapsed
        | _ ->
            printfn "invalid mode"        
        Console.ReadLine() |> ignore   
        0 // return an integer exit code 