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

  private sealed trait Message[T]
  private case class Result[T](data: Seq[T])
  private case class Start[T](data: Seq[T], promise: Promise[Set[T]])

  def solveImmutableSet(problemDef: Definition, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._
    def chunkAndSend(data: Seq[T], coordinator: ActorRef): Int = {
      var jobs = 0
      for (chunk <- data.grouped(G)) {
        import akka.pattern.pipe, concurrent.Future
        Future { Result(chunk.flatMap(x => generators(x))) } pipeTo coordinator
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
          val typedData = data.asInstanceOf[Seq[T]]
          val filteredData = typedData.filterNot(foundSoFar contains)
          foundSoFar ++= filteredData
          val jobs = chunkAndSend(filteredData, self)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar)
      }

      become {
        case Start(data, promise) =>
          val typedData = data.asInstanceOf[Seq[T]]
          val typedPromise = promise.asInstanceOf[Promise[Set[T]]]
          foundSoFar ++= typedData
          become(loop(typedPromise))
          val jobs = chunkAndSend(typedData, self)
          remaining += jobs
      }
    })
    val resultPromise = concurrent.promise[Set[T]]
    timedRun {
      a ! Start(initData, resultPromise)
      val res = Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
      system.shutdown()
      res
    }
  }

  def solveMutableSet(problemDef: Definition, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._
    def chunkAndSend(data: Seq[T], coordinator: ActorRef): Int = {
      var jobs = 0
      for (chunk <- data.grouped(G)) {
        import akka.pattern.pipe, concurrent.Future
        Future { Result(chunk.flatMap(generators(_))) } pipeTo coordinator
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
          val typedData = data.asInstanceOf[Seq[T]]
          val filteredData = typedData.filterNot(foundSoFar contains)
          foundSoFar ++= filteredData
          val jobs = chunkAndSend(filteredData, self)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar)
      }

      become {
        case Start(data, promise) =>
          val typedData = data.asInstanceOf[Seq[T]]
          val typedPromise = promise.asInstanceOf[Promise[Set[T]]]
          foundSoFar ++= typedData
          become(loop(typedPromise))
          val jobs = chunkAndSend(typedData, self)
          remaining += jobs
      }
    })
    val resultPromise = concurrent.promise[Set[T]]
    timedRun {
      a ! Start(initData, resultPromise)
      val res = Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
      system.shutdown()
      res
    }
  }

  def solveConcurrentMap(problemDef: Definition, G: Int): (Set[problemDef.T], Long) = {
    import problemDef._, collection.concurrent.TrieMap

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
          val typedData = data.asInstanceOf[Seq[T]]
          val jobs = chunkAndSend(typedData, self)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar.keySet)
      }

      become {
        case Start(data, promise) =>
          val typedData = data.asInstanceOf[Seq[T]]
          val typedPromise = promise.asInstanceOf[Promise[Set[T]]]
          foundSoFar ++= typedData.toIterable.map((_, ()))
          become(loop(typedPromise))
          val jobs = chunkAndSend(typedData, self)
          remaining += jobs
      }
    })
    val resultPromise = concurrent.promise[Set[T]]
    timedRun {
      a ! Start(initData, resultPromise)
      val res = Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
      system.shutdown()
      res
    }
  }
}