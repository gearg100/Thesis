package concordance.test

object ConcordTest extends App{
  import common.test
  import concordance._
  
  println(toConcordanceList(test, 3).toList)

}