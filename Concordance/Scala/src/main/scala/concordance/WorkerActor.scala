package concordance

import akka.actor.Actor
import akka.event.Logging
import collection.mutable.HashMap

abstract class WorkerActor(i:Int) extends Actor with Mode {
  val hashTable = new HashMap[List[String],Data]

  override def receive = {      
    case seqs : Seq[(List[String],Int)] =>
      //println("worker received " + seqs)
      for( (seq,pos) <- seqs)
        hashTable.get(seq) match {
          case None => hashTable += seq -> new Data(1,List(pos))
          case Some(data) => data += pos
        }
    case Stop =>
      sender ! Finished(i,hashTable)
    case _ =>
      println("Error")
  }
}