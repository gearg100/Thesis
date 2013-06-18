-module(benchmarks).
-include("definition.hrl").
-export([definition/1,fib/1,foo/2,simple/3, delay/1]).


fib_aux(0, _Y, X) -> X;
fib_aux(N, Y, X) ->
  fib_aux(N - 1, X+Y, Y).

fib(N) -> fib_aux(abs(N), 1, 0).

%% mixing polynomials (up to degree 3)
p(A0,_N)          -> A0.
p(A1,A0, N)       -> A1 * N + p(A0, N).
p(A2,A1,A0, N)    -> A2 * N * N + p(A1,A0, N).
p(A3,A2,A1,A0, N) -> A3 * N * N * N + p(A2,A1,A0, N).

%% step functions (up to 4 steps)
s(B0, N)          -> if N < B0 -> 0; true -> 1 end.
s(B0,B1, N)       -> if N < B0 -> 0; true -> 1 + s(B1, N) end.
s(B0,B1,B2, N)    -> if N < B0 -> 0; true -> 1 + s(B1,B2, N) end.
s(B0,B1,B2,B3, N) -> if N < B0 -> 0; true -> 1 + s(B1,B2,B3, N) end.

r(X,Y) -> X rem Y.
  
f1(X) -> fib(p(1,0, r(X, 16)) + p(1,0, X)).
f2(X) -> fib(p(1,5, r(X, 16)) + p(2,5,-1, X)).
f3(X) -> fib(p(1,10, r(X, 16)) + p(-1,0,8,0, X)).
f4(X) -> fib(p(8,3, s(0,49,98,100, r(X, 100))) + p(-1,X)).
f5(X) -> fib(p(10,0, s(0,900,999,1000, r(X, 1000))) + p(1,X)).

foo(X, N) -> [r(f1(X), N), r(f2(X), N), r(f3(X), N), r(f4(X), N), r(f5(X), N)].

definition(N) -> #definition{ init_data=[1,3,5,6,8,56,235,543], generators = fun(X) -> foo(X, N) end }.

delay(D) -> delay(D, 0, 2*D).
delay(0, X, Y) -> X - Y;
delay(D, X, Y) -> delay(D - 1, X + 1, Y - 1).

simple(L,D,F) -> 
    List = lists:map(
        fun(X) -> fun(Y) -> Y * X rem L + delay(D) end end,
        element(1,lists:split(F, [2,3,5,7,11,13,17,23,29,31]))
    ),
    #definition{
    	init_data = [1,2,3,4,5,6,7,8,9], 
    	generators = fun(X) -> 
            lists:map(fun(Fun) -> Fun(X) end, List)
    	end
    }.