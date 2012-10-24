// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
namespace  Concordance
open System
open System.IO
open System.Linq
open Microsoft.FSharp.Collections

type Data() =
    let mutable n = 0
    let lst = System.Collections.Generic.List<int>()
    member x.Count 
        with get() = n
        and internal set(value) = 
            n <- n+1
    member x.Positions 
        with get() = Seq.toList lst
    member internal x.List = lst
    override x.ToString() = 
         lst.Aggregate(
            new System.Text.StringBuilder("(" + string n + ", ["), 
            fun builder current -> builder.Append (string current + "; ")
         ).ToString() + "])"
    static member (+=) (data : Data,n :int) = 
        data.Count <- data.Count + 1
        data.List.Add n
    static member (++=) (data : Data, data2 : Data) =
        data.Count <- data.Count + data2.Count
        data.List.AddRange data2.List


type ConcordanceMap() =
    inherit System.Collections.Generic.Dictionary<string list, Data>()
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
    type Message1 =
        |Start
        |Finished of int * ConcordanceMap

    type Message2 =
        |Stop
        |Work of (string list * int) seq

    let worker (master:MailboxProcessor<_>) i =
        MailboxProcessor.Start(fun inbox ->
            let map = ConcordanceMap()
            let rec loop ()= async {
                let! msg = inbox.Receive()
                match msg with
                |Work(bulk) ->
                    for (seq, n) in bulk do
                        map.Item2(seq) += n
                    //printfn "%A done" (Seq.toList bulk)
                    return! loop()
                |Stop ->
                    //printfn "received Stop"
                    //printfn "Stopped %d" i
                    master.Post <| Finished(i,map)
            }
            loop()
        )        

    let inline indexOf (lst,_)  N = (abs (hash lst)) % N//(hash lst) &&& (N-1)     

    let master S G M N input (worker: MailboxProcessor<_> -> int -> MailboxProcessor<_>)onComplete =
        MailboxProcessor.Start(fun inbox ->
            let workers = Array.init N <| worker inbox
            let res = ResizeArray(N)
            let rec finishing j = async {
                let! msg = inbox.Receive()
                match msg with
                |Finished (i, map) ->
                    res.Add(map)
                    //printfn "%d finished with %d keys" i map.Count
                    //Seq.iter (fun pair -> printfn "%A" pair) map
                    if j > 0 then 
                        return! finishing (j - 1)
                    else
                        onComplete(res)
                | _ -> printfn "%A : unknown" msg
            }
            let start() = async {
                let! msg = inbox.Receive()
                try 
                    match msg with
                    |Start ->
                        sequences input S
                        |> Seq.chunked G
                        |> Seq.chunked M
                        |> Seq.iter (fun chunk -> 
                            let inline func (seq:#seq<_>) = 
                                let bulks = seq.GroupBy (fun seq -> indexOf seq N)
                                try
                                    for group in bulks do
                                        workers.[group.Key].Post <| Work group
                                with 
                                | e-> 
                                    printfn "Error: %s" e.Message
                            // Using TPL
                            let factory = Threading.Tasks.Task.Factory
                            let tasks = chunk.Select(fun it -> factory.StartNew(fun () -> func(it))).ToArray()
                            System.Threading.Tasks.Task.WaitAll(tasks)
                            //Using PLinq
                            //chunk.AsParallel().WithDegreeOfParallelism(M).ForAll(fun el -> func el)
                        )
                        workers
                        |> Seq.iter (fun worker -> worker.Post Stop)
                        //printfn "All Stop sent"
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
            let r: ResizeArray<ConcordanceMap> ref= ref null
            let latch = new System.Threading.ManualResetEvent(false)
            let master = master S G M N words worker <| fun res ->
                r := res
                latch.Set()|> ignore            
            master.Post Start
            latch.WaitOne()|> ignore
            r
        Console.WriteLine ("\nTotal items in HashMap: " + result.Value.Sum(fun (map:ConcordanceMap) -> map.Values.Sum(fun (v:Data) -> v.Count)).ToString())








       