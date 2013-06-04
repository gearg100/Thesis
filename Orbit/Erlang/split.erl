-module(split).
-export([split/2, split2/3]).

split_helper([], _ChunkSize, _Left, [], Result, Count) -> 
  {Count, Result};
split_helper([], _ChunkSize, _Left, Acc, Result, Count) -> 
  {Count+1, [Acc|Result]};
split_helper(List, ChunkSize, 0, Acc, ResultAcc, Count) ->
  split_helper(List, ChunkSize, ChunkSize, [], [Acc|ResultAcc], Count + 1);
split_helper([H|T], ChunkSize, Left, Acc, Result, Count) ->
  split_helper(T, ChunkSize, Left - 1, [H|Acc], Result, Count).

split(0, _List) -> [];
split(ChunkSize, List) ->
  split_helper(List, ChunkSize, ChunkSize, [], [], 0).

split_helper2([], _ChunkSize, _Left, [], Count, _Func) -> 
  Count;
split_helper2([], _ChunkSize, _Left, Acc, Count, Func) -> 
  Func(Acc),
  Count+1;
split_helper2(List, ChunkSize, 0, Acc, Count, Func) ->
  Func(Acc),
  split_helper2(List, ChunkSize, ChunkSize, [], Count + 1, Func);
split_helper2([H|T], ChunkSize, Left, Acc, Count, Func) ->
  split_helper2(T, ChunkSize, Left - 1, [H|Acc], Count, Func).

split2(0, _List, _Func) -> [];
split2(ChunkSize, List, Func) ->
  split_helper2(List, ChunkSize, ChunkSize, [], 0, Func).