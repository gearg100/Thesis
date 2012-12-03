namespace Orbit.Agent
    
module Aggregator = 
    open Orbit.Types
    open System.Linq

    type internal AggregatorMessage<'T when 'T : comparison> =
        //|Store of seq<'T>
        |Store of AsyncReplyChannel<seq<'T>>*seq<'T>
        |Fetch of AsyncReplyChannel<Set<'T>>
        |Stop of AsyncReplyChannel<unit>



    let inline internal actorBody<'T when 'T : comparison> 
        (mapper: IMapper<'T> ref) =
        new Agent<AggregatorMessage<'T>>(fun inbox -> 
            let hashset = HashSet<'T>()
            let rec loop () = 
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    //|Store(set) ->
                    |Store(channel, set) ->
                        let filteredset = 
                            HashSet<_>(set |> Seq.filter (not << hashset.Contains))                           
                        hashset.UnionWith filteredset 
                        //mapper.Value.Map filteredset                    
                        channel.Reply <| filteredset
                        return! loop()
                    |Fetch channel->
                        channel.Reply <| Set.ofSeq hashset
                        return! loop()
                    |Stop channel->
                        channel.Reply()
                }
            loop()
        )
    open System.Threading.Tasks
    type Aggregator<'T when 'T:comparison>(nOfWorkers:int) =
        let dependency = ref Unchecked.defaultof<IMapper<'T>>
        let workers = Array.init nOfWorkers (fun _ -> actorBody dependency)
        interface IAggregator<'T> with
            member x.Store data =
                let inline indexOf N el = (hash el |> abs)%N
//                data
//                |> Seq.groupBy (indexOf nOfWorkers)
//                |> Seq.iter (fun (i,chunk)-> workers.[i].Post <| Store chunk)
                data
                |> Seq.groupBy (indexOf nOfWorkers)
                |> Seq.map (fun (i,chunk)-> 
                    workers.[i].PostAndAsyncReply <| fun channel -> Store(channel, chunk)
                    |> Async.StartAsTask
                )
                |> Task.WhenAll
                |> fun task -> task.ContinueWith(fun (task:Task<_>) -> 
                    task.Result
                    |> Seq.concat
                    |> dependency.Value.Map
                )
                |> ignore
//                async {
//                    let! result = 
//                        data
//                        |> Seq.groupBy (indexOf nOfWorkers)
//                        |> Seq.map (fun (i,chunk)->async { return! workers.[i].PostAndAsyncReply <| fun channel -> Store(channel, chunk)}) 
//                        |> Async.Parallel
//                    result
//                    |> Seq.concat
//                    |> dependency.Value.Map
//                }
//                |> Async.Start
            member x.FetchResults () = async {
                let! sets = 
                    workers 
                    |> Array.map (fun worker -> worker.PostAndAsyncReply Fetch)
                    |> Async.Parallel
                return Set.unionMany sets
            }
        interface IDependent<IMapper<'T>> with
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                workers |> Seq.iter (fun worker-> worker.Start()) 
            member x.Stop () =
                workers 
                |> Seq.map (fun worker-> worker.PostAndAsyncReply Stop)
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
            member x.Dispose() =
                dependency := Unchecked.defaultof<IMapper<'T>>



