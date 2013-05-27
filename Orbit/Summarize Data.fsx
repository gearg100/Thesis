open System
open System.IO

Console.Write("Give Data File (must end with txt): ")
let file = Console.ReadLine() |> fun str -> str.Trim('"') |> fun file -> if Path.IsPathRooted(file) then file else Path.GetFullPath(file)
if Path.GetExtension(file) <> ".txt" then failwithf "Invalid File Extension: %s" (Path.GetExtension(file))

let lines = File.ReadLines(file)
let header = Seq.head lines

let summarizedLines =   
    lines
    |> Seq.skip 1
    |> Seq.map (fun str -> str.Split(',') |> Array.map (fun str -> str.Trim()))
    |> Seq.groupBy (fun ar -> Array.sub ar 0 (ar.Length - 2))
    |> Seq.map (fun (k, vs) ->        
        k |> Seq.reduce (fun acc el -> acc + "," + el), vs |> Seq.map (fun v -> v.[v.Length - 2], v.[v.Length - 1])
    )
    |> Seq.toArray
    |> Seq.map (fun (k, group) -> k, group |> Seq.minBy (snd>>int64))
    |> Seq.map (fun (k, (res, minTime)) -> k + "," + res + "," + minTime)

File.AppendAllLines(
    Path.Combine(
        Path.GetDirectoryName(file), 
        Path.GetFileNameWithoutExtension(file) + "_minimums.txt"
    ), 
    seq { yield header; yield! summarizedLines }
)