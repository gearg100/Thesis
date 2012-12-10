namespace Orbit.Task

module Mapper = 
    open Orbit.Types

    open Helpers
    open System.Threading
    open System.Threading.Tasks

    type Mapper<'T when 'T : comparison>
        (
            coordinator: ICoordinator,
            nOfWorkers:int, chunkLimit:int,
            func : seq<'T> -> seq<'T>
        ) =
        let dependency = ref Unchecked.defaultof<IAggregator<'T>>
        let mutable i = 1
        let mutable remaining = 0
        interface IMapper<'T> with
            member x.Map data = 
                let mutable acc = 0
                for chunk in Seq.chunked chunkLimit data do 
                    acc <- acc + 1
                    Task.Factory.StartNew(fun () -> 
                        func chunk
                        |> (!dependency).Store
                    ) |> ignore
                coordinator.Add <| acc - 1

            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                ()
            member x.Stop () =
                ()
            member x.Dispose() =
                dependency := Unchecked.defaultof<IAggregator<'T>> 