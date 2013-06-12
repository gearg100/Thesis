package orbit.solver

import util.Helper.timedRun
import akka.actor._
import orbit.Definition
import collection.Set
import scala.concurrent.Promise
import akka.routing.RoundRobinRouter
import scala.concurrent.Await

class AkkaSystem(sets: orbit.util.SetProvider) {
  import sets._

  def solveActorWorkers(problemDef: Definition, M: Int, G: Int) = {
    import problemDef._
    val helpers = new OrbitAkkaHelpers[T]; import helpers._

    class Worker extends Actor {
      def receive = {
        case Job(chunk) =>
          sender ! Result(chunk.flatMap(generators(_)).distinct)
      }
    }

    class Coordinator extends Actor {
      var foundSoFar = iSet[T]
      val workers = context.actorOf {
        Props(new Worker).withRouter(RoundRobinRouter(M))
      }

      var remaining = 0

      def loop(replyPromise: Promise[Set[T]]): Receive = {
        case Result(data) =>
          val filteredData = data.filterNot(foundSoFar.contains)
          foundSoFar ++= filteredData
          val jobs = genericChunkAndSend(filteredData, G, workers)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar)
      }

      def receive = {
        case Start(data, promise) =>
          foundSoFar ++= data
          context.become(loop(promise))
          val jobs = genericChunkAndSend(data, G, workers)
          remaining += jobs
      }
    }
    implicit val system = ActorSystem("system")
    val coordinator = system.actorOf(Props(new Coordinator))
    val resultPromise = concurrent.promise[Set[T]]
    val res = timedRun {
      coordinator ! Start(initData, resultPromise)
      Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
    }
    system.shutdown()
    res
  }

  def solveActorWorkersConcurrentMap(problemDef: Definition, M: Int, G: Int) = {
    import problemDef._
    val helpers = new OrbitAkkaHelpers[T]; import helpers._

    class Worker(map: collection.concurrent.Map[T, Unit]) extends Actor {
      def receive = {
        case Job(chunk) =>
          sender ! Result(chunk.flatMap {
            generators(_).filter(map.putIfAbsent(_, ()).isEmpty)
          })
      }
    }

    class Coordinator extends Actor {
      val foundSoFar = cMap[T]
      val workers = context actorOf {
        Props(new Worker(foundSoFar)).withRouter(RoundRobinRouter(nrOfInstances = M))
      }

      var remaining = 0

      def loop(replyPromise: Promise[Set[T]]): Receive = {
        case Result(data) =>
          val jobs = genericChunkAndSend(data, G, workers)
          if (remaining > 1 || jobs > 0)
            remaining += jobs - 1
          else replyPromise.success(foundSoFar.keySet)
      }

      def receive = {
        case Start(data, promise) =>
          foundSoFar ++= data.map((_, ()))
          context.become(loop(promise))
          val jobs = genericChunkAndSend(data, G, workers)
          remaining += jobs
      }
    }
    implicit val system = ActorSystem("system")
    val coordinator = system.actorOf(Props(new Coordinator))
    val resultPromise = concurrent.promise[Set[T]]
    val res = timedRun {
      coordinator ! Start(initData, resultPromise)
      Await.result(resultPromise.future, concurrent.duration.Duration.Inf)
    }
    system.shutdown()
    res
  }

  class OrbitAkkaHelpers[T] {
    case class Job(chunk: Seq[T])
    case class Result(data: Seq[T])
    case class Start(data: Seq[T], promise: Promise[Set[T]])

    def genericChunkAndSend(data: Seq[T], G: Int, router: ActorRef) = {
      var jobs = 0
      for (chunk <- data.grouped(G)) yield {
        router ! Job(chunk)
        jobs += 1
      }
      jobs
    }
  }
}