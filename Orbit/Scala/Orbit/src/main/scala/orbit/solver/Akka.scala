package orbit.solver

import util.Helper.timedRun
import orbit.Definition
import akka.actor.ActorDSL._
import akka.actor.{ ActorSystem, ActorRef }
import concurrent.{ Promise, Await }
import concurrent.ExecutionContext.Implicits.global
import akka.actor.ActorLogging
import collection.Set

class Akka(sets: orbit.util.SetProvider) {
  import sets._

  def solveImmutableSet(problemDef: Definition, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._

    case class Result(data: Seq[T])
    case class Start(data: Seq[T], promise: Promise[Set[T]])

    def chunkAndSend(data: Seq[T], coordinator: ActorRef): Int = {
      var jobs = 0
      for (chunk <- data.grouped(G)) {
        import akka.pattern.pipe, concurrent.future
        future { Result(chunk.flatMap(generators(_)).distinct) } pipeTo coordinator
        jobs += 1
      }
      jobs
    }
    implicit val system = ActorSystem("system")
    val a = actor("coordinator")(new Act {
      var foundSoFar = iSet[T]
      var remaining = 0

      def loop(replyPromise: Promise[Set[T]]): Receive = {
        case Result(data) =>
          val filteredData = data.filterNot(foundSoFar.contains)
          foundSoFar ++= filteredData
          val jobs = chunkAndSend(filteredData, self)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar)
      }

      become {
        case Start(data, promise) =>
          foundSoFar ++= data
          become(loop(promise))
          val jobs = chunkAndSend(data, self)
          remaining += jobs
      }
    })
    val resultPromise = concurrent.promise[Set[T]]
    val res = timedRun {
      a ! Start(initData, resultPromise)
      Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
    }
    system.shutdown()
    res
  }

  def solveMutableSet(problemDef: Definition, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._
    case class Result(data: Seq[T])
    case class Start(data: Seq[T], promise: Promise[Set[T]])

    def chunkAndSend(data: Seq[T], coordinator: ActorRef): Int = {
      var jobs = 0
      for (chunk <- data.grouped(G)) {
        import akka.pattern.pipe, concurrent.Future
        Future { Result(chunk.flatMap(generators(_)).distinct) } pipeTo coordinator
        jobs += 1
      }
      jobs
    }
    implicit val system = ActorSystem("system")
    val a = actor("coordinator")(new Act {
      val foundSoFar = mSet[T]
      var remaining = 0

      def loop(replyPromise: Promise[Set[T]]): Receive = {
        case Result(data) =>
          val filteredData = data.filterNot(foundSoFar.contains)
          foundSoFar ++= filteredData
          val jobs = chunkAndSend(filteredData, self)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar)
      }

      become {
        case Start(data, promise) =>
          foundSoFar ++= data
          become(loop(promise))
          val jobs = chunkAndSend(data, self)
          remaining += jobs
      }
    })
    val resultPromise = concurrent.promise[Set[T]]
    val res = timedRun {
      a ! Start(initData, resultPromise)
      Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
    }
    system.shutdown()
    res
  }

  def solveConcurrentMap(problemDef: Definition, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._, collection.concurrent.TrieMap

    case class Result(data: Seq[T])
    case class Start(data: Seq[T], promise: Promise[Set[T]])

    implicit val system = ActorSystem("system")
    val a = actor("coordinator")(new Act {
      val foundSoFar = cMap[T]
      def chunkAndSend(data: Seq[T], coordinator: ActorRef): Int = {
        var jobs = 0
        for (chunk <- data.grouped(G)) {
          import akka.pattern.pipe, concurrent.Future
          Future {
            Result(chunk.flatMap {
              generators(_).filter(foundSoFar.putIfAbsent(_, ()).isEmpty)
            })
          } pipeTo coordinator
          jobs += 1
        }
        jobs
      }
      var remaining = 0

      def loop(replyPromise: Promise[Set[T]]): Receive = {
        case Result(data) =>
          val jobs = chunkAndSend(data, self)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar.keySet)
      }

      become {
        case Start(data, promise) =>
          foundSoFar ++= data.map((_, ()))
          become(loop(promise))
          val jobs = chunkAndSend(data, self)
          remaining += jobs
      }
    })
    val resultPromise = concurrent.promise[Set[T]]
    val res = timedRun {
      a ! Start(initData, resultPromise)
      Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
    }
    system.shutdown()
    res
  }
}