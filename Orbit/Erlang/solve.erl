-module(solve).
-export([solve/1, solve_conc/3, solve_conc_helper/4, solve_conc_helper2/4, solve_conc_helper3/4, solve_conc_with_workers/3, create_workers_under_rooter/2]).

%sequential
solve_helper([], _Generators) -> 
  ets:match(hashset, '$1');
solve_helper(Current, Generators) ->
  NCurrent = lists:flatmap(Generators, Current),
  NCurrentDistinct = lists:usort(NCurrent),
  NCurrentFilteredDistinct = lists:foldl(fun(Elem,Acc)->
      case ets:insert_new(hashset, {Elem}) of
          false -> Acc; true -> [Elem|Acc]
      end
  end,[],NCurrentDistinct),
  solve_helper(NCurrentFilteredDistinct, Generators).

solve({InitData, Generators}) ->
  ets:new(hashset, [set, named_table, public]),
  Result = solve_helper(InitData, Generators),
  ets:delete(hashset),
  Result.

solve_conc({InitData, Generators}, G, Func) ->
  ets:new(hashset, [set, named_table, public, {read_concurrency, true}, {write_concurrency, true}]),
  Master = self(),
  Coordinator = spawn(fun() -> Func(Master, Generators, G, 1) end),
  Coordinator ! InitData,
  receive finish -> ok end,
  Result = ets:match(hashset, '$1'),
  ets:delete(hashset),
  Result.

% concurrent 1
solve_conc_helper(Master, _Generators, _G, 0) ->
  Master ! finish;
solve_conc_helper(Master, Generators, G, Remaining) ->
  receive Current ->
    FilteredCurrent = lists:foldl(fun(Elem,Acc)->
      case ets:insert_new(hashset, {Elem}) of
        false -> Acc; true -> [Elem|Acc]
      end
    end,[],Current),
    {Count, Chunks} = split:split(G, FilteredCurrent),
    Coordinator = self(),
    lists:foreach(fun(Chunk) -> 
      spawn(fun()->
        NCurrent = lists:flatmap(Generators, Chunk),
        Coordinator ! lists:usort(NCurrent)
      end)
    end,Chunks), 
    solve_conc_helper(Master, Generators, G, Remaining + Count - 1) 
  end.

%concurrent 2
solve_conc_helper2(Master, _Generators, _G, 0) ->
  Master ! finish;
solve_conc_helper2(Master, Generators, G, Remaining) ->
  receive Current ->
    FilteredCurrent = lists:foldl(fun(Elem,Acc)->
      case ets:insert_new(hashset, {Elem}) of
        false -> Acc; true -> [Elem|Acc]
      end
    end,[],Current),
    Coordinator = self(),
    Count = split:split2(G, FilteredCurrent, fun(Chunk) -> 
      spawn(fun()->
        NCurrent = lists:flatmap(Generators, Chunk),
        Coordinator ! lists:usort(NCurrent)
      end)
    end), 
    solve_conc_helper2(Master, Generators, G, Remaining + Count - 1) 
  end.

%concurrent 3
solve_conc_helper3(Master, _Generators, _G, 0) ->
  Master ! finish;
solve_conc_helper3(Master, Generators, G, Remaining) ->
  receive Current ->
    Coordinator = self(),
    Count = split:split2(G, Current,fun(Chunk) -> 
      spawn(fun()->
        NCurrent = lists:flatmap(
          fun(C) -> 
            R = Generators(C), 
            lists:filter(fun(X) -> ets:insert_new(hashset, {X}) end, R)
          end, Chunk
        ),
        Coordinator ! NCurrent
      end)
    end), 
    solve_conc_helper3(Master, Generators, G, Remaining + Count - 1) 
  end.

%workers
conc_worker(Coordinator, Generators) -> 
  receive 
    stop -> ok;
    Chunk ->
      NCurrent = lists:flatmap(fun(C) -> 
          R = Generators(C), 
          lists:filter(fun(X) -> ets:insert_new(hashset, {X}) end, R)
        end, Chunk),
      Coordinator ! NCurrent,
      conc_worker(Coordinator, Generators)
  end.

round_robin_router(Workers) ->
  receive 
    stop ->
      lists:foreach(fun(Pid) -> Pid ! stop end, queue:to_list(Workers));
    Msg ->
      {{value, Pid}, NWorkers} = queue:out(Workers),
      Pid ! Msg,
      round_robin_router(queue:in(Pid, NWorkers))
  end.

create_workers_under_rooter(M, Func) ->
  spawn(fun() ->
    Workers = [ spawn(fun() -> Func(I) end) || I <- lists:seq(1, M) ],
    round_robin_router(queue:from_list(Workers))
  end).

%logic 
solve_conc_workers_helper(Master, Workers, _G, 0) ->
  Master ! finish,
  Workers ! stop;  
solve_conc_workers_helper(Master, Workers, G, Remaining) ->
  receive Current ->
    Count = split:split2(G, Current, fun(Chunk) -> Workers ! Chunk end),
    solve_conc_workers_helper(Master, Workers, G, Remaining + Count - 1) 
  end.

solve_conc_with_workers({InitData, Generators}, M, G) ->
  ets:new(hashset, [set, named_table, public, {read_concurrency, true}, {write_concurrency, true}]),
  Master = self(),
  Coordinator = spawn(fun() -> 
    Coordinator = self(),
    Workers = create_workers_under_rooter(M, fun(_I)-> conc_worker(Coordinator, Generators) end),
    solve_conc_workers_helper(Master, Workers, G, 1) 
  end),
  Coordinator ! InitData,
  receive finish -> ok end,
  Result = ets:match(hashset, '$1'),
  ets:delete(hashset),
  Result.