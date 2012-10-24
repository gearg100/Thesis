package concordance.test

import scala.collection.mutable.HashMap
import java.util.concurrent.CountDownLatch
import concordance.{Data,MasterActor,WorkerActor,Mode,Debug,Start}

object SystemTest extends App {
  import akka.actor.{ActorRef, Props, ActorSystem}
  import common._
  
  type Mode = Debug
  
  print("Give the times for the execution to be repeated and press <Enter>: [default = 1] ")
  implicit val T = readLine().tryParseInt.getOrElse(1)
  
  print("Give maximum length of word sequences and press <Enter>: [default = 3] ")
  val S = readLine().tryParseInt.getOrElse(3)  
  print("Give grainsize and press <Enter>: [default = 3000] ")
  val G = readLine().tryParseInt.getOrElse(3000)
  print("Give number of senders and press <Enter>: [default = 2] ")
  val M = readLine().tryParseInt.getOrElse(2)
  print("Give number of hashtables and press <Enter>: [default = availableProcessors/2] ")
  val N = readLine().tryParseInt.getOrElse(Runtime.getRuntime.availableProcessors/2)
  print("Give input filename: [default = ./test.txt] ")
  val file = {
    val filename = readLine()
    if (filename.isEmpty) """test.txt""" else filename
  }
  //println("%d %d %d %d %d".format(T,S,G,M,N))
  print("\nReading from file... ")
  val words = time { 
    getWords(file) 
  }
  
  print("\nBuilding HashMap... ")
  val result = time {
    val latch = new CountDownLatch(1)
    var result : List[HashMap[_,Data]] = null
  
    val system = ActorSystem("ConcordanceSystem")
    val master = system.actorOf(
      Props(
        new MasterActor(
          S,
          G, M, N,
          words,
          i => new WorkerActor(i) with Mode,
          res => {
            result = res
            latch.countDown()            
            system.shutdown()
          }
        ) with Mode
      ),"master")
      
    master ! Start   
  
    latch.await()
    
    result
  }
  
  println("\nTotal items in HashMap: " + result.map(_.values.map(_.n).sum).sum)
}