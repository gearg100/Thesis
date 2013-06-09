namespace Orbit.Agent
    
module Aggregator = 
    open Orbit.Types
    open System.Linq
    open Helpers
    type internal AggregatorMessage<'T when 'T : comparison> =
        |Store of seq<'T>
        |Fetch of AsyncReplyChannel<Set<'T>>
        |Stop

    let inline internal actorBody<'T when 'T : comparison> 
        (mapper: IMapper<'T> ref) =
        new Agent<AggregatorMessage<'T>>(fun inbox -> 
            let hashset = HashSet<'T>()
            let rec loop () = 
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    |Store(set) ->
                        let filteredset = 
                            HashSet<_>(set |> Seq.filter (not << hashset.Contains))
                        (!mapper).Map filteredset                    
                        hashset.UnionWith filteredset                    
                        return! loop()
                    |Fetch channel->
                        channel.Reply <| Set.ofSeq hashset
                        return! loop()
                    |Stop ->
                        ()
                }
            loop()
        )

    type Aggregator<'T when 'T:comparison>
        (   
            coordinator: ICoordinator,
            nOfWorkers:int
        ) =
        let dependency = ref Unchecked.defaultof<IMapper<'T>>
        let workers = Array.init nOfWorkers <| fun _ -> actorBody dependency
        interface IAggregator<'T> with
            member x.Store data =
                let mutable acc = 0
                for group in data.GroupBy(System.Func<_,_>(indexOf nOfWorkers)) do
                    acc <- acc + 1
                    workers.[group.Key].Post <| Store(group)
                coordinator.Add <| acc - 1
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
                for worker in workers do worker.Post Stop
            member x.Dispose() =
                dependency := Unchecked.defaultof<IMapper<'T>>



