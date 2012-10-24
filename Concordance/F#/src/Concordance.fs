// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
namespace  Concordance
open System
open System.IO
open System.Linq
open Microsoft.FSharp.Collections

type Data() =
    let mutable n = 0
    let lst = ResizeArray<int>()
    member x.Count = n
    member x.Positions = Seq.toList lst
    member x.IncCount value = n <- n + value
    member internal x.List = lst
    override x.ToString() = 
         lst.Aggregate(
            new System.Text.StringBuilder("(" + string n + ", ["), 
            fun builder current -> builder.Append (string current + "; ")
         ).ToString() + "])"
    static member (+=) (data : Data,n :int) = 
        data.IncCount 1
        data.List.Add n
    static member (++=) (data : Data, data2 : Data) =
        data.IncCount data2.Count
        data.List.AddRange data2.List


type ConcordanceMap<'a when 'a : equality>() =
    inherit System.Collections.Generic.Dictionary<'a, Data>()
    member x.Item2 
        with get(index) = 
            let def = Data()
            let mutable value = def
            if x.TryGetValue(index, &value) then
                value
            else 
                x.Item2(index) <- def
                def  
        and set index value =
            x.Item(index) <- value      

module Agents = 
    open Concordance.SeqTransformation
    open Concordance
    type Message1<'a when 'a : equality> =
        |Start
        |Finished of int * ConcordanceMap<'a>

    type Message2<'a> =
        |Stop
        |Work of ('a * int) seq

    let worker (master:MailboxProcessor<_>) i =
        MailboxProcessor.Start(fun inbox ->
            let map = ConcordanceMap()
            let rec loop ()= async {
                try
                    let! msg = inbox.Receive()
                    match msg with
                    |Work(bulk) ->
                        for (seq, n) in bulk do
                            map.Item2(seq) += n
                        //printfn "%A done" (Seq.toList bulk)
                        return! loop()
                    |Stop ->
                        //printfn "Stopped %d" i
                        master.Post <| Finished(i,map)
                with e ->
                    printfn "%s" e.Message
            }
            loop()
        )        

    let inline indexOf N (str,_) = (hash str) % N |> abs //(hash lst) &&& (N-1)  //optimal??

    let master S G M N input (worker: MailboxProcessor<_> -> int -> MailboxProcessor<_>)onComplete =
        MailboxProcessor.Start(fun inbox ->
            let workers = Array.init N <| worker inbox
            let res = ResizeArray(N)
            let rec finishing j = async {
                try
                    let! msg = inbox.Receive()
                    match msg with
                    |Finished (i, map) ->
                        res.Add(map)
                        //printfn "%d finished with %d keys" i map.Count
                        if j > 0 then 
                            return! finishing (j - 1)
                        else
                            onComplete(res)
                    | _ -> printfn "%A : unknown" msg
                with e ->
                    printfn "%s" e.Message
                    
            }
            let start() = async {
                let! msg = inbox.Receive()
                try 
                    match msg with
                    |Start ->
                        let inline func (sq:#seq<_>) = 
                            try
                                for group in sq.GroupBy(Func<_,_>(indexOf N)) do
                                    workers.[group.Key].Post <| Work group
                            with 
                            | e-> 
                                printfn "Error: %s" e.Message
                        sequences input S
                        |> Seq.chunked G
                        |> PSeq.``chunked iter(PLINQ)`` M func
                        workers
                        |> Seq.iter (fun worker -> worker.Post Stop)
                        return! finishing (N - 1)
                    | _ -> printfn "%A : unknown" msg
                with e -> printfn "Error"
            }       
            start()
        )         

module Execution = 
    open Concordance.Helpers
    open Agents
    let inline run T file S G M N = 
        print("\nReading from file... ")
        let words = time T <| fun () -> readFile file |> Seq.toList
        print("\nBuilding HashMap... ")
        let result = time T  <| fun () ->
            let r: ResizeArray<ConcordanceMap<_>> ref= ref null
            let latch = new System.Threading.ManualResetEvent(false)
            let master = master S G M N words worker <| fun res ->
                r := res
                latch.Set()|> ignore            
            master.Post Start
            latch.WaitOne()|> ignore
            r
        Console.WriteLine ("\nTotal items in HashMap: " + result.Value.Sum(fun (map:ConcordanceMap<_>) -> map.Values.Sum(fun (v:Data) -> v.Count)).ToString())








       