namespace Orbit.Tasks
    
module Aggregator = 
    open Orbit.Types
    open System.Threading.Tasks
    open System.Collections.Concurrent
    [<Literal>]
    let defaultCapacity = 100000
    type Aggregator<'T when 'T:comparison>(nOfWorkers:int) =
        let dependency = ref Unchecked.defaultof<IMapper<'T>>
        let hashset = ConcurrentDictionary<'T, unit>(nOfWorkers,defaultCapacity)
        interface IAggregator<'T> with
            member x.Store data =
                let res = 
                    data
                    |> Seq.filter (hashset.ContainsKey>>not)
                    |> Seq.cache
                res
                |> Seq.iter (fun i -> hashset.TryAdd(i,())|>ignore)
                (!dependency).Map res
            member x.FetchResults () = async {
                return hashset.Keys |> Set.ofSeq
            }
        interface IDependent<IMapper<'T>> with
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                () 
            member x.Stop () =
                ()
            member x.Dispose() =
                dependency := Unchecked.defaultof<IMapper<'T>>



