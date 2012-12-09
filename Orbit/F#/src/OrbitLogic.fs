namespace Orbit
open System.Linq
open Orbit.Types

module Logic =
    let inline indexOf N el = (hash el)%N
    let inline mapF<'T when 'T:equality> (funcs: seq<'T->'T>) sequence= 
        (funcs).SelectMany(fun f -> Seq.map f sequence)
    let inline aggregateF<'T when 'T :comparison> (mapper:IMapper<'T>) sequence = 
        mapper.Map sequence

