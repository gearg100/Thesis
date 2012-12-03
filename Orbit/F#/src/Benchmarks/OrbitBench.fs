namespace Orbit.Benchmarks

module FibonaccisLong=
    type TElem = int64
    let fib n =
        let rec fib_tr n b a =
            if n = 0L then a else fib_tr (n-1L) (a+b) b
        fib_tr (abs n) 1L 0L

    let s lst n = List.fold (fun acc el -> if el > n then acc + 1L else acc) 0L lst 

    let p lst n = List.fold (fun acc el -> acc*n + el) 0L lst     

    let f1 n x = (fib (p [1L;0L] (x%16L)) + p [1L;0L] x) % n
    let f2 n x = (fib (p [1L;5L] (x%16L)) + p [2L;5L;-1L] x) % n
    let f3 n x = (fib (p [1L;10L] (x%16L)) + p [-1L;0L;8L;0L] x) % n
    let f4 n x = (fib (p [8L;3L] (s [0L;49L;98L;100L] (x%100L))) + p [-1L] x) % n
    let f5 n x = (fib (p [10L;0L] (s [0L;900L;999L;1000L] (x%1000L))) + p [1L] x) % n

    let funcs n= [f1 n;f2 n;f3 n;f4 n;f5 n]

    let integers = [1L;3L;5L;6L;8L;56L;235L;543L]

module FibonaccisBigInt=
    type TElem = bigint
    let fib n =
        let rec fib_tr n b a =
            if n = 0I then a else fib_tr (n-1I) (a+b) b
        fib_tr (abs n) 1I 0I

    let s lst n = List.fold (fun acc el -> if el > n then acc + 1I else acc) 0I lst 

    let p lst n = List.fold (fun acc el -> acc*n + el) 0I lst     

    let f1 n x = (fib (p [1I;0I] (x%16I)) + p [1I;0I] x) % n
    let f2 n x = (fib (p [1I;5I] (x%16I)) + p [2I;5I;-1I] x) % n
    let f3 n x = (fib (p [1I;10I] (x%16I)) + p [-1I;0I;8I;0I] x) % n
    let f4 n x = (fib (p [8I;3I] (s [0I;49I;98I;100I] (x%100I))) + p [-1I] x) % n
    let f5 n x = (fib (p [10I;0I] (s [0I;900I;999I;1000I] (x%1000I))) + p [1I] x) % n

    let funcs n= [f1 n;f2 n;f3 n;f4 n;f5 n]

    let integers = [1I;3I;5I;6I;8I;56I;235I;543I]

module SimpleInt =
    type TElem = int
    let funcs n =
        [
            fun i -> (i + 1)%n
            fun i -> (i * 2)%n
            fun i -> (i * 7)%n
            fun i -> (i * 31)%n
            fun i -> (i * 51)%n
            fun i -> (i * 67)%n
        ] 
    let integers =
        [1;3;5;7;9;10;20]