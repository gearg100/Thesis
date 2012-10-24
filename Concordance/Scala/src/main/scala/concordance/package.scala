package object concordance {
  type Conc[T] = (List[T],Int)
  import scala.collection.mutable.ArrayBuffer
  @inline
  final def toConcordanceList[T](seq:Seq[T], chunkLimit:Int) : IndexedSeq[Conc[T]] = {
    @annotation.tailrec
    def initArrayBuffer(idx:Int, current:List[T])(acc:ArrayBuffer[Conc[T]]):ArrayBuffer[Conc[T]] = {
      if(current.isEmpty) acc
      else {
        val acc2 =  (1 until (chunkLimit-idx)).map(i => (current take i,idx))
        initArrayBuffer(idx+1, current.tail)( acc ++ acc2)
      }      
    }
    @annotation.tailrec
    def mainArrayBuffer(idx:Int, rest:Seq[T])(acc:ArrayBuffer[Conc[T]]):ArrayBuffer[Conc[T]] = {
      if(idx == seq.length - chunkLimit + 1) acc
      else{
        var tmp = acc
        var current = rest.take(chunkLimit).toList
        var p = idx
        while(!current.isEmpty){
          tmp += ((current,p))
          current = current.tail
          p += 1
        }
        mainArrayBuffer(idx+1,rest.tail)(tmp)
//        def subSeqs(p:Int, current:List[T], acc:ArrayBuffer[Conc[T]]):ArrayBuffer[Conc[T]] =
//          if(current.isEmpty) 
//            mainArrayBuffer(idx+1,rest.tail, acc)
//          else 
//            subSeqs(p+1, current.tail, acc :+ (current,p))
//        subSeqs(idx,rest.take(chunkLimit).toList,acc)
      }
    }
    initArrayBuffer(0,seq.take(chunkLimit-1).toList) _ andThen 
    mainArrayBuffer(0, seq) _ apply 
    ArrayBuffer.empty
  } 
}