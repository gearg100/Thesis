#r @".\bin\Release64\Orbit.exe"

open Orbit.Main
open Orbit.Benchmarks

let (|Pair|) (pair:System.Collections.Generic.KeyValuePair<_,_>) = pair.Key, fst pair.Value, snd pair.Value

let inline run i = 
    let solvers = solvers 8 5000
    let (name, solve) = solvers.[i]
    let transformer (i:int) = int64 i
    let res, timeElapsed = solve <| Fibonaccis.definition transformer
    printfn "Result Length for %s: %d" name <| Seq.length res

do
    for i = 1 to 10 do
    for Pair(j, name, solve) in solvers 8 200 do
        if j > 1 then
            let transformer (i:int) = int64 i
            printf "%s: " name
            let res, timeElapsed = solve <| Simple.definition transformer 200000 10000 8
            printfn "%d | %d ms" (Seq.length res) timeElapsed

let def = Simple.definition (fun i -> int64 i) 500000 20000 10
let l1, t1 =
    let s, t = Orbit.Solver.SimpleFunctions.solve def
    Seq.length s, t
let l2, t2 = 
    let s, t =  Orbit.Solver.NotSimpleFunctions.solveWithAgentConcurrentDictionary 8 200 def
    Seq.length s, t