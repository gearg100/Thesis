package concordance
import collection.Iterator

class ConcordanceIterator[A](col:IndexedSeq[A], chunkLimit:Int, begin:Int, end:Int) extends Iterator[(List[A],Int)]{
  private var n = begin
  private val e = if(end == col.size - 1) end - 1 else end
  def hasNext = n < e

  private def nextSlice(): List[A] = col.slice(n,n + chunkLimit).toList

  private var current =
    if (begin == 0) col.take(chunkLimit-1).toList else nextSlice()

  private var i = 1
  private def init():(List[A],Int) = {
    val res = (current.take(i),n)
    i += 1
    if (n + i == chunkLimit){
      i = 1
      n += 1
      current = current.tail
      if (current.isEmpty){
        n = 0
        i = 0
        current = nextSlice()
        fun = maincase
      }
    }
    res
  }

  private def maincase(): (List[A],Int) = {
    val res = (current,n + i)
    current = current.tail
    i += 1
    if (current.isEmpty){
      n += 1
      i = 0
      current = nextSlice()
    }
    res
  }

  private var fun: () => (List[A],Int) =
    if (begin == 0)
      () => init()
    else
      () => maincase()

  def next: (List[A],Int) = fun()
}