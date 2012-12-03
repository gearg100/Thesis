namespace Orbit.Types
open System
open System.Linq

type Agent<'T> = MailboxProcessor<'T>
type Task = System.Threading.Tasks.Task
type HashSet<'T> = System.Collections.Generic.HashSet<'T>

type IMapper<'T> =
    abstract member Map : seq<'T> -> unit
type IAggregator<'T when 'T: comparison> =
    abstract member Store : seq<'T> -> unit
    abstract member FetchResults: unit -> Async<Set<'T>>

type IDependent<'TDependency> =
    inherit IDisposable
    abstract member Config: 'TDependency -> unit
    abstract member Start: unit -> unit
    abstract member Stop: unit -> unit
