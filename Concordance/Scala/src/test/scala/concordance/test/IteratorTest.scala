package concordance.test
import concordance.ConcordanceIterator
import common._

object IteratorTest extends App {
  val it = new ConcordanceIterator(test, 3, 0, test.length-1)
  it foreach println
}
