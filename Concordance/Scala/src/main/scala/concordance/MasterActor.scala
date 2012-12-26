package concordance

import akka.{actor,event,dispatch}
import actor.{Actor, ActorRef}
import concurrent.{Future, ExecutionContext}
import event.Logging
import collection.mutable.HashMap
import collection.IndexedSeq

case object Start
case object Stop
case class Finished(i:Int, res: HashMap[List[String],Data])

abstract class MasterActor(
    S:Int,
    G:Int, M:Int, N:Int,
    words : IndexedSeq[String], 
    worker : Int => WorkerActor,
    onFinish: List[HashMap[List[String],Data]] => Unit
) extends Actor with Mode{
  
  var results = List.empty[HashMap[List[String],Data]]
  
  val workers = Array.tabulate(N){
    import akka.actor.Props
    i => context.actorOf(Props(
        worker(i)
      ),"worker"+i)
  }

  private var remain = N
  
  @inline
  final def indexOf[A](str:A, N: Int) = math.abs(str.## % N) // optimal?
  import ChunkedIterator._
  override protected def receive = {
    case Start =>
      val it = new ConcordanceIterator(words, S, 0, words.length-1)
      //val it = toConcordanceList(words, S).iterator
      implicit val ctx = context.dispatcher
      it.grouped(G)
        .grouped(G)
        .foldLeft(Future {}){ (acc: Future[Unit], chunk) =>
        acc.flatMap({
          _ => Future.traverse(chunk.toIterable)(g =>
            Future {
              g.toIterable groupBy (t => indexOf(t._1, N)) foreach {
                t => workers(t._1) ! t._2
              }
            }
          ) map (_ => ())
        })
      } map { 
        case _ =>
          log("all sent")
          workers foreach (_ ! Stop) 
      } 
      
    case Finished(i,res) =>      
      log("master received \n" + res.mkString("\n"))
      results ::= res
      context stop sender
      remain -= 1
      if (remain == 0){
        onFinish(results)
        log("All finished")
      }
  }
}