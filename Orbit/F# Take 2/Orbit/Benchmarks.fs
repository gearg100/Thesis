namespace Orbit.Benchmarks
open Orbit
module Fibonaccis =
    let inline definition transform = 
        let funcs = 
            let zero = transform 0
            let one = transform 1
            let n = transform 1000871
            let fib n =
                let rec fib_tr n b a =
                    if n = zero then a else fib_tr (n - one) (a+b) b
                fib_tr (abs n) one zero

            let s lst n = List.fold (fun acc el -> if el > n then acc + one else acc) zero lst 

            let p lst n = List.fold (fun acc el -> acc * n + el) zero lst     

            let modulo = transform 16
            let transformList = List.map transform 

            let f1 n x = (fib (p (transformList [1;0]) (x % modulo)) + p (transformList [1;0]) x) % n
            let f2 n x = (fib (p (transformList [1;5]) (x % modulo)) + p (transformList [2;5;-1]) x) % n
            let f3 n x = (fib (p (transformList [1;10]) (x % modulo)) + p (transformList [-1;0;8;0]) x) % n
            let f4 n x = (fib (p (transformList [8;3]) (s (transformList [0;49;98;100]) (x% (transform 100)))) + p [-one] x) % n
            let f5 n x = (fib (p (transformList [10;0]) (s (transformList [0;900;999;1000]) (x% (transform 1000)))) + p [one] x) % n            

            fun x -> [f1 n x;f2 n x;f3 n x;f4 n x;f5 n x] :> seq<_>

        let integers = List.map transform [1;3;5;6;8;56;235;543]

        { generators = funcs; initData = integers}

module Simple =
    let inline definition transform l d f =
        let delay i = 
            let mutable n = 0
            let mutable k = 2*i
            while n < k do 
                n <- n + 1; k <- k - 1
            n - k
        let array = 
            [2;3;5;7;11;13;17;23;29;31]
            |> Seq.take f 
            |> Seq.map transform
            |> Seq.map (fun i x -> transform (delay d) + abs ((x * i) % transform l))
            |> Seq.toArray
        {
            initData = List.map transform [1;2;3;4;5;6;7;8;9]
            generators = fun x -> Seq.map (fun f -> f x) array
        }