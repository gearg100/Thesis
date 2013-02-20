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

for i = 1 to 20 do
for Pair(j, name, solve) in solvers 8 5000 do
    if j > 2 then
        let transformer (i:int) = int64 i
        let res, timeElapsed = solve <| Fibonaccis.definition transformer
        printfn "Result Length for %s: %d" name <| Seq.length res