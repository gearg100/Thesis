namespace Orbit.Task

module Mapper = 
    open Orbit.Types

    open Helpers
    open System.Threading
    open System.Threading.Tasks

    type Mapper<'TSource, 'TResult when 'TResult : comparison>
        (
            nOfWorkers:int, grainsize:int,
            func : 'TSource seq -> seq<'TResult>,
            onComplete : IAggregator<'TResult> -> unit
        ) =
        let dependency = ref Unchecked.defaultof<IAggregator<'TResult>>
        let mutable i = 1
        let mutable remaining = 0
        interface IMapper<'TSource> with
            member x.Map data =
                Interlocked.Increment &remaining |> ignore
                if not <| Seq.isEmpty data then 
                    data
                    |> Seq.chunked grainsize
                    |> Seq.iter (fun chunk -> 
                        Interlocked.Increment &i |> ignore
                        async {
                            chunk
                            |> func
                            |> (!dependency).Store    
                        } 
                        |> Async.Start
                    )
                elif i = remaining then
                    onComplete !dependency
        interface IDependent<IAggregator<'TResult>> with
            member x.Config dependency' = 
                dependency := dependency'
            member x.Start () = 
                ()
            member x.Stop () =
                ()
            member x.Dispose() =
                dependency := Unchecked.defaultof<IAggregator<'TResult>> 