namespace Orbit.Types
open System
open System.Linq

type Agent<'T> = MailboxProcessor<'T>
type Task = System.Threading.Tasks.Task
type HashSet<'T> = System.Collections.Generic.HashSet<'T>
type groupedSeq<'T> = seq<int*seq<'T>>

type IDependent<'TDependency> =
    inherit IDisposable
    abstract member Config: 'TDependency -> unit
    abstract member Start: unit -> unit
    abstract member Stop: unit -> unit

type IMapper<'T when 'T: comparison> =
    inherit IDependent<IAggregator<'T>>
    abstract member Map : seq<'T> -> unit
and IAggregator<'T when 'T: comparison> =
    inherit IDependent<IMapper<'T>>
    abstract member Store : seq<'T> -> unit
    abstract member FetchResults: unit -> Async<Set<'T>>

type chunkFunction<'T> = seq<'T> -> seq<seq<'T>>
type groupFunction<'T> = seq<'T> -> groupedSeq<'T>