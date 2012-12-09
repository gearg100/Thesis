namespace Orbit.Task

module Mapper = 
    open Orbit.Types

    open Helpers
    open System.Threading
    open System.Threading.Tasks

    type Mapper<'T when 'T : comparison>
        (
            nOfWorkers:int,
            func : seq<'T> -> seq<'T>,
            chunkFunc : seq<'T> -> seq<seq<'T>>
        ) =
        let dependency = ref Unchecked.defaultof<IAggregator<'T>>
        let mutable i = 1
        let mutable remaining = 0
        interface IMapper<'T> with
            member x.Map data = 
                for chunk in chunkFunc data do 
                    Task.Factory.StartNew(fun () -> 
                        func chunk
                        |> (!dependency).Store
                    ) |> ignore
                     
        interface IDependent<IAggregator<'T>> with
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                ()
            member x.Stop () =
                ()
            member x.Dispose() =
                dependency := Unchecked.defaultof<IAggregator<'T>> 