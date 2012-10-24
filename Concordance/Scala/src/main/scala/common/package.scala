package object common {
  @inline
  final def using[Closeable <: {def close(): Unit}, B](closeable: Closeable)(getB: Closeable => B): B =
    try {
      getB(closeable)
    } finally {
      closeable.close()
    }

  @inline
  final def time[R](block: => R,msg: String="Elapsed time") (implicit times:Int): R = {
    val t0 = System.nanoTime()
    val result = block
    val t1 = System.nanoTime()
    var total = (t1 - t0)/1000000
    for(i <- 1 until times){
      val t0 = System.nanoTime()
      block
      val t1 = System.nanoTime()
      total += (t1 - t0)/1000000
    }
    println(msg + " : "+ total/times + "ms")
    result
  }

  @inline
  final def getWords(fileName : String): IndexedSeq[String] = {
    import io.Source
    using(Source.fromFile(fileName)) {
      import collection.mutable.ArrayBuffer
      source =>
        source.getLines().toIterable.par.aggregate(ArrayBuffer.empty[String])(
          (wordsBuffer, line) => wordsBuffer ++ line.trim().split( """\s+"""),
          _ ++ _
        ).toIndexedSeq
    }
  }
  
  class RichString(s:String) {
    def tryParseInt:Option[Int] = 
      try{
        Some(s.toInt)
      }
      catch{
        case _ => None
      } 
  }
  
  implicit def wrapString(str:String) = new RichString(str)

  final val test = IndexedSeq("hi","there","you","hi","there","me","there","you")
}