package orbit.solver
import util.Helper.timedRun
import orbit.Definition
import collection.{ Set, GenSeq }

class Simple(sets: orbit.util.SetProvider) {
  import sets._

  @inline private def simpleLogic(p: Definition)(seq: GenSeq[p.T], results: Set[p.T]): Set[p.T] = {
    import p._
    def helper(currentSeq: GenSeq[T], results: Set[T]): Set[T] = {
      val nFilteredSeq =
        currentSeq
          .flatMap(generators(_))
          .filterNot(results contains)
          .distinct
      if (nFilteredSeq.isEmpty) results
      else helper(nFilteredSeq, results ++ nFilteredSeq)
    }
    helper(seq, results)
  }

  def solve(problemDef: Definition): (Set[problemDef.T], Long) = {
    import problemDef._
    timedRun { simpleLogic(problemDef)(initData, iSet ++ initData) }
  }

  def solve2(problemDef: Definition): (Set[problemDef.T], Long) = {
    import problemDef._
    timedRun { simpleLogic(problemDef)(initData, mSet ++ initData) }
  }

  def solveParSeq(problemDef: Definition): (Set[problemDef.T], Long) = {
    import problemDef._
    timedRun { simpleLogic(problemDef)(initData.par, initData.to[Set]) }
  }

  def solveParSeqWithConcurrentMap(problemDef: Definition): (Set[problemDef.T], Long) = {
    import problemDef._
    val results = cMap[T]
    def helper(currentSeq: GenSeq[T]) {
      val nFilteredSeq =
        currentSeq flatMap {
          generators(_) filter (results.putIfAbsent(_, ()).isEmpty)
        }
      if (!nFilteredSeq.isEmpty) helper(nFilteredSeq)
    }
    timedRun { helper(initData.par); results.keySet }
  }

  def solveFuture(problemDef: Definition): (Set[problemDef.T], Long) = {
    import problemDef._
    import concurrent._, duration.Duration.Inf, ExecutionContext.Implicits.global
    val G = 20
    def helper(currentSeq: Seq[T], results: Set[T]): Future[Set[T]] =
      if (currentSeq.isEmpty) { Future(results) }
      else {
        for {
          nSeqIterator <- Future.traverse(currentSeq.grouped(G)) { chunk =>
            Future { chunk.flatMap(generators(_)).filterNot(results.contains) }
          }
          res <- {
            val nFilteredSet = (Set.empty[T] /: nSeqIterator)(_ ++ _)
            helper(nFilteredSet.toSeq, results ++ nFilteredSet)
          }
        } yield res
      }
    timedRun { Await.result(helper(initData, initData.to[Set]), Inf) }
  }

}