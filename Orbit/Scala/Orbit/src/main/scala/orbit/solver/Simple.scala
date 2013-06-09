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
          .filterNot(results.contains)
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

  def solveParSeq(problemDef: Definition, M: Int): (Set[problemDef.T], Long) = {
    import problemDef._, collection.parallel.ForkJoinTaskSupport, concurrent.forkjoin.ForkJoinPool
    val initDataPar = initData.par; initDataPar.tasksupport = new ForkJoinTaskSupport(new ForkJoinPool(M))
    timedRun { simpleLogic(problemDef)(initDataPar, initData.to[Set]) }
  }

  def solveParSeqWithConcurrentMap(problemDef: Definition, M: Int): (Set[problemDef.T], Long) = {
    import problemDef._, collection.parallel.ForkJoinTaskSupport, concurrent.forkjoin.ForkJoinPool
    val initDataPar = initData.par; initDataPar.tasksupport = new ForkJoinTaskSupport(new ForkJoinPool(M))
    val results = cMap[T]
    def helper(currentSeq: GenSeq[T]) {
      val nFilteredSeq = currentSeq flatMap { generators(_) filter (results.putIfAbsent(_, ()).isEmpty) }
      if (!nFilteredSeq.isEmpty) helper(nFilteredSeq)
    }
    timedRun { helper(initDataPar); results.keySet }
  }

  def solveFuture(problemDef: Definition, M: Int, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._
    import concurrent._, duration.Duration.Inf
    implicit val executionContext =
      ExecutionContext.fromExecutorService(new scala.concurrent.forkjoin.ForkJoinPool(M))
    val results = cMap[T]
    def helper(currentSeq: Seq[T]): Future[Set[T]] =
      if (currentSeq.isEmpty) { Future(results.keySet) }
      else {
        for {
          nSeqIterator <- Future.traverse(currentSeq grouped G) { chunk =>
            Future { chunk flatMap (generators(_) filter (results.putIfAbsent(_, ()).isEmpty)) }
          }
          res <- helper(nSeqIterator.flatten.toSeq)
        } yield res
      }
    timedRun { Await.result(helper(initData), Inf) }
  }

}