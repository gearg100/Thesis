namespace Concordance
open System
open System.IO

module Helpers = 
    let inline readFile file =
        let buffer = ResizeArray<_>()
        let inline mapf line = System.Text.RegularExpressions.Regex("\s+").Split(line)
        let inline addWords (line:string) = buffer.AddRange <| mapf (line.Trim())
        File.ReadLines(file)
        |> Seq.iter addWords
        buffer

    let inline print (str:string) = Console.Write str
    let inline readLine() = Console.ReadLine()

    let inline check N =
        let mutable power = 1
        while power < N do power <- power <<< 1
        power = N

    let inline tryParseInt str = 
        let mutable num :int = 0
        if Int32.TryParse(str, &num) then Some num
        else None
    let inline getOrElse num = function
        | Some n -> n
        | None -> num

    let inline time<'a> T (func: unit -> 'a) = 
        let watch = System.Diagnostics.Stopwatch.StartNew()
        let res = func()
        watch.Stop()
        for i = 2 to T do
            watch.Start()
            let _ = func()
            watch.Stop()
        printfn "Elapsed Time : %dms" <| watch.ElapsedMilliseconds/(int64 T)
        res

[<RequireQualifiedAccess>]
module Seq =
    open System.Linq
    open System.Collections.Generic 
    let inline chunked<'a> (chunkSize:int) sq =
        let rec helper (enumerator:IEnumerator<'a>) = seq {
            let sq = //seq {
                let list = ResizeArray<_>(chunkSize)
                let mutable i = 0
                while i < chunkSize && enumerator.MoveNext() do
                    //yield enumerator.Current
                    list.Add enumerator.Current
                    i <- i + 1
                list
            //}
            if not <| Seq.isEmpty sq then
                yield sq
                yield! helper enumerator
        }
        helper <| (sq:>seq<_>).GetEnumerator()