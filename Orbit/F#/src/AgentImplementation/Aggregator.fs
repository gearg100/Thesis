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
                    |Store(channel, set) ->
                        let filteredset = 
                            HashSet<_>(set |> Seq.filter (not << hashset.Contains))                           
                        hashset.UnionWith filteredset                    
                        channel.Reply filteredset
                        return! loop()
                    |Fetch channel->
                        channel.Reply <| Set.ofSeq hashset
                        return! loop()
                    |Stop channel->
                        channel.Reply()
                }
            loop()
        )

    type Aggregator<'T when 'T:comparison>
        (   
            nOfWorkers:int, 
            groupFunc: seq<'T> -> groupedSeq<'T>
        ) =
        let dependency = ref Unchecked.defaultof<IMapper<'T>>
        let workers = Array.init nOfWorkers (fun _ -> actorBody dependency)
        interface IAggregator<'T> with
            member x.Store data =
                for (i,group) in groupFunc data do
                    async {
                        let! rep = workers.[i].PostAndAsyncReply <| fun channel -> Store(channel, group)
                        dependency.Value.Map rep
                    }
                    |> Async.Start
            member x.FetchResults () = async {
                let! sets = 
                    workers 
                    |> Array.map (fun worker -> worker.PostAndAsyncReply Fetch)
                    |> Async.Parallel
                return Set.unionMany sets
            }
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                for worker in workers do worker.Start() 
            member x.Stop () =
                workers 
                |> Seq.map (fun worker-> worker.PostAndAsyncReply Stop)
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
            member x.Dispose() =
                dependency := Unchecked.defaultof<IMapper<'T>>



