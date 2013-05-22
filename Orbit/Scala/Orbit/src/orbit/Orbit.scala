package orbit
import benchmark._
import scala.annotation.tailrec
import scala.util.Try
import orbit.util.ScalaSets
import orbit.util.JavaSets

object Main extends App {

  def run(solve: Definition => (collection.Set[_], Long), problemDef: Definition) {
    val (res, time) = solve(problemDef)
    println(s"${res.size} in $time ms")
  }

  def solvers(sets: util.SetProvider, M: Int, G: Int) = {
    val simple = new solver.Simple(sets)
    val akka = new solver.Akka(sets)
    Map(
      (1, ("Sequential with Immutable Set", (x: Definition) => simple.solve(x))),
      (2, ("Sequential with Mutable Set", (x: Definition) => simple.solve2(x))),
      (3, ("Parallel Collections", (x: Definition) => simple.solveParSeq(x))),
      (4, ("Parallel Collections with Concurrent Map", (x: Definition) => simple.solveParSeqWithConcurrentMap(x))),
      (5, ("Futures", (x: Definition) => simple.solveFuture(x))),
      (6, ("Akka with Immutable Set", (x: Definition) => akka.solveImmutableSet(x, G))),
      (7, ("Akka with Mutable Set", (x: Definition) => akka.solveMutableSet(x, G))),
      (8, ("Akka with Concurrent Map", (x: Definition) => akka.solveConcurrentMap(x, G))))
  }

  println("Choose mode [1 -> int64, 2 -> bigint] (default = int64): ")
  val mode = Try(readLine().toInt).getOrElse(1)
  println("Give me nOfMappers (default = ProcessorCount): ")
  val M = Try(readLine().toInt).getOrElse(Runtime.getRuntime().availableProcessors())
  println("Give me chunkSize (default = 1): ")
  val G = Try(readLine().toInt).getOrElse(1)
  println("Set Implementation [Scala, Java] (default = Scala): ")
  val sets = readLine() match {
    case "Java" => JavaSets
    case "Scala" | _ => ScalaSets
  }
  println("""Choose Implementation from [
    1 -> Sequential with Immutable Set, 
    2 -> Sequential with Mutable Set,
    3 -> Parallel Collections,
    4 -> Parallel Collections with Concurrent Map,
    5 -> Futures,
    6 -> Akka with Immutable Set,
    7 -> Akka with Mutable Set,
    8 -> Akka with Concurrent Map, 
] (default = 1): """)
  val implementation = Try(readLine().toInt).getOrElse(1)
  println("Give me l,d,f (space separated on the same line, f <= 10, default = 200000 10000 8): ")
  val Array(l, d, f) = Try {
    readLine() split (" ", 3) map (_.trim.toInt)
  }.getOrElse(Array(200000, 10000, 8))

  println()

  val problem = mode match {
    case 1 =>
      new GenBench[Long](l, d, f)
    case 2 =>
      new GenBench[BigInt](l, d, f)
    case _ =>
      null
  }
  
  val solve = solvers(sets, M, G)(implementation)._2
  for(i <- 1 to 10)
	  run(solve, problem)

//  val simple1 = new solver.Simple(orbit.util.ScalaSets)
//  val simple2 = new solver.Simple(orbit.util.JavaSets)
//  val akka1 = new solver.Akka(orbit.util.ScalaSets)
//  val akka2 = new solver.Akka(orbit.util.JavaSets)
//
//  for (i <- 1 to 10)
//    run(x => simple1.solveParSeq(x), new GenBench[Long](200000, 10000, 8))
//  run(x => akka1.solveConcurrentMap(x, 1), new GenBench[Long](200000, 10000, 8))
}