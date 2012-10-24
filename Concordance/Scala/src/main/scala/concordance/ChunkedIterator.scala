package concordance

import scala.collection.mutable.ArrayBuffer
import collection.Iterator

class ChunkedIterator[T](it:Iterator[T],chunkSize:Int) extends Iterator[Iterator[T]] {
  override def hasNext = it.hasNext
  override def next() = it.take(chunkSize)
}

object ChunkedIterator{
  implicit def toChunked[T](it:Iterator[T]) =
    new {
      def chunked(chunkSize:Int) = new ChunkedIterator[T](it,chunkSize)
    }
}