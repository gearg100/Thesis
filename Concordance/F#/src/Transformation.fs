namespace Concordance

open System.Linq

module SeqTransformation =
    let sequences (words:_ list) (chunkLimit:int) =
        let size = words.Length
        let rec init pos current = seq {
            for i = 1 to chunkLimit-pos-1 do 
                yield (current |> Seq.take i |> Seq.toList, pos)
            if not <| Seq.isEmpty current then 
                yield! init (pos+1) (List.tail current)
            else 
                yield! main 0 words
            }    
        and main pos (rest:_ list) = 
            let rec subSeqs p (current: _ list) = seq {                         
                match current with
                | [] -> yield! main (pos+1) (List.tail rest)
                | _ :: t -> 
                    yield (current,p)
                    yield! subSeqs (p+1) t
                }
            seq {
                if pos < size - chunkLimit + 1 then
                    yield! subSeqs pos <| (rest.Take chunkLimit |> Seq.toList)
            }
        init 0 (words.Take (chunkLimit - 1) |> Seq.toList)
           
    let sequences2 (words : _ list) chunkLimit = 
        let size = words.Length
        let acc = new ResizeArray<_>(words.Length * chunkLimit)
        let rec init pos current len =
            for i = 1 to len do 
                acc.Add(current |> Seq.take i |> Seq.toList, pos)
            if not <| Seq.isEmpty current then 
                init (pos+1) (List.tail current) (len-1)
            else 
                main 0 words
        and main pos (rest:_ list) = 
            let rec subSeqs p (current: _ list) =                          
                match current with
                | [] -> main (pos+1) (List.tail rest)
                | _ :: t -> 
                    acc.Add(current,p)
                    subSeqs (p+1) t
            if pos < size - chunkLimit + 1 then
                subSeqs pos (rest |> Seq.take chunkLimit |> Seq.toList)
        init 0 (words.Take (chunkLimit - 1) |> Seq.toList) (chunkLimit - 1)
        acc

    let sequences3 (words : _ list) chunkLimit func = 
        let size = words.Length
        let rec init pos current len =
            for i = 1 to len do 
                func(current |> Seq.take i |> Seq.toList, pos)
            if not <| Seq.isEmpty current then 
                init (pos+1) (List.tail current) (len-1)
            else 
                main 0 words
        and main pos (rest:_ list) = 
            let rec subSeqs p (current: _ list) =                          
                match current with
                | [] -> main (pos+1) (List.tail rest)
                | _ :: t -> 
                    func(current,p)
                    subSeqs (p+1) t
            if pos < size - chunkLimit + 1 then
                subSeqs pos (rest |> Seq.take chunkLimit |> Seq.toList)
        init 0 (words.Take (chunkLimit - 1) |> Seq.toList) (chunkLimit - 1)