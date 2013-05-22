package orbit

trait Definition{
  type T
  def generators(x: T): Seq[T]
  val initData: Seq[T]
}