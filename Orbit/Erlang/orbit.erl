-module(orbit).
-include("definition.hrl").
-export([repeat/2, timedRun/3, run/0, main/1]).

% Timed Run
timedRun(Fun, Args) ->
  {T, Xs} = timer:tc(Fun, [Args]),
  {length(Xs), T div 1000}.

timedRun(Module, Fun, Args) ->
  {T, Xs} = timer:tc(Module, Fun, [Args]),
  {length(Xs), T div 1000}.

toIntegerOrElse(Str, Default) ->
  case string:to_integer(Str) of
    {error,_} -> Default;
    {N, _} -> N
  end.

solveFuncs(M, G) ->
	dict:from_list([
		{1, fun(X) -> solve:solve(X) end},
		%{2, fun(X) -> solve:solve_conc(X, G, fun solve:solve_conc_helper/4) end}, 
		{2, fun(X) -> solve:solve_conc(X, G, fun solve:solve_conc_helper2/4) end},
		{3, fun(X) -> solve:solve_conc(X, G, fun solve:solve_conc_helper3/4) end},
    {4 ,fun(X) -> solve:solve_conc_with_workers(X, M, G) end}
	]).

repeat(Body, 1) -> 
  Body();
repeat(Body, N)->
  Body(),
  repeat(Body, N-1).

run() ->
  Times = toIntegerOrElse(io:get_line('nOfTimes each test will run (default = 10): '),10), io:nl(),
  M = toIntegerOrElse(io:get_line('Give me nOfMappers (default = schedulers_online): '), erlang:system_info(schedulers_online)),io:nl(),
  G = toIntegerOrElse(io:get_line('Give me chunkSize (default = 1000): '), 1000),io:nl(),
  io:fwrite("Choose Implementation from [\n"),
  io:fwrite("    1 -> Sequential,\n"),
  io:fwrite("    2 -> Concurrent Tasks with Sequential Ets,\n"),
  io:fwrite("    3 -> Concurrent Tasks with Concurrent Ets \n"),
  io:fwrite("    4 -> Concurrent with Workers and Concurrent Ets\n"),
  Implementation = toIntegerOrElse(io:get_line("] (default = 1): "),1),
  Map = solveFuncs(M, G),
  L = toIntegerOrElse(io:get_line("L (default = 200000): "),200000),io:nl(),
  D = toIntegerOrElse(io:get_line("D (default = 10000): "),10000),io:nl(),
  F = toIntegerOrElse(io:get_line("F (default = 8): "),8),io:nl(),
  io:format("Schedulers = ~p, Times = ~p, M = ~p, G = ~p, Implementation = ~p, L = ~p, D = ~p, F = ~p", [erlang:system_info(schedulers_online),Times, M, G, Implementation, L, D, F]),
  repeat(fun() ->
    {Res, T} = timedRun(dict:fetch(Implementation, Map), benchmarks:simple(L,D,F)),
    io:format("\nResult: ~p - Time Elapsed: ~p ms",[Res,T]) 
  end, Times).


main(_Args) -> run().