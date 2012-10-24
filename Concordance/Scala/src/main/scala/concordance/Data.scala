package concordance

class Data(var n:Int, var lst:List[Int]){
  def this()= this(0,Nil)
  @inline
  final def +=(pos:Int){
    n += 1
    lst = pos :: lst
  }
  @inline
  final def ++=(data:Data){
    n += data.n
    lst = (lst.toBuffer ++ lst).toList
  }
  override def toString = "(" + n + ", " + lst + ")"
}