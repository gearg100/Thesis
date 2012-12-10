module Helpers

let inline indexOf N num = (abs (hash num) % N) |> abs

[<RequireQualifiedAccess>]
module Seq =
    open System.Linq
    open System.Collections.Generic 
    type internal ChunkedEnumerator<'a>(inSeq:seq<'a>,chunkSize:int) =
        let mutable s = inSeq.GetEnumerator()
        let mutable current = Unchecked.defaultof<_>
        interface System.Collections.IEnumerator with
            member x.MoveNext() = 
                let b = s.MoveNext()
                if b then 
                    current <-
                        let lst = List<_>()
                        lst.Add s.Current
                        let mutable i = chunkSize - 1
                        while i > 0 && s.MoveNext() do
                            lst.Add s.Current
                            i <- i - 1
                        lst
                else
                    current <- Unchecked.defaultof<_>
                b                    
            member x.Current = (x :> IEnumerator<_>).Current :> obj
            member x.Reset() = 
                s <- inSeq.GetEnumerator()
                current <- Unchecked.defaultof<_>
        interface System.Collections.Generic.IEnumerator<seq<'a>> with
            member x.Current = current :> _
        interface System.IDisposable with
            member x.Dispose() =
                s <- Unchecked.defaultof<_>
                current <- Unchecked.defaultof<_>
    let chunked<'a> (chunkSize:int) sq :seq<_> = {
        new IEnumerable<seq<'a>> with
            member x.GetEnumerator() = new ChunkedEnumerator<'a>(sq, chunkSize) :> System.Collections.Generic.IEnumerator<_>
            member x.GetEnumerator() = new ChunkedEnumerator<'a>(sq, chunkSize) :> System.Collections.IEnumerator
    }