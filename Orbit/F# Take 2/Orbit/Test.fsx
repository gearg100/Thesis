#r @".\bin\Release\Orbit.exe"

open Orbit.Main
open Orbit.Benchmarks

let inline (|Pair|) (pair:System.Collections.Generic.KeyValuePair<_,_>) = pair.Key, fst pair.Value, snd pair.Value

for Pair(_, name, solve) in solvers 8 5000 do
    let transformer (i:int) = int64 i
    let res, timeElapsed = solve <| Fibonaccis.definition transformer
    printfn "Result Length for %s: %d" name <| Seq.length res