namespace Orbit.Task
    
module Aggregator = 
    open Orbit.Types
    open System.Threading.Tasks
    open System.Collections.Concurrent
    [<Literal>]
    let private defaultCapacity = 100000
    type Aggregator<'T when 'T:comparison>
        (
            coordinator: ICoordinator,
            nOfWorkers:int
        ) =
        let dependency = ref Unchecked.defaultof<IMapper<'T>>
        let hashset = ConcurrentDictionary<'T, unit>(nOfWorkers,defaultCapacity)
        interface IAggregator<'T> with
            member x.Store data =
                let res = 
                    HashSet<'T>(Seq.filter (hashset.ContainsKey>>not) data)
                res
                |> Seq.iter (fun i -> hashset.TryAdd(i,())|>ignore)
                (!dependency).Map res
            member x.FetchResults () = async {
                return hashset.Keys |> Set.ofSeq
            }

            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                () 
            member x.Stop () =
                ()
            member x.Dispose() =
                dependency := Unchecked.defaultof<IMapper<'T>>



