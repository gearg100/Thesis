package orbit.benchmark

import orbit.Definition
import scala.annotation.tailrec

object D {
  def delay(i: Int): Int = {
    var n = 0
    var k = 2*i
    while (k > n) { n += 1; k -= 1 }
    n - k
  }
}

class LongBench(l: Int, d: Int, f: Int) extends Definition {
  type T = Long
  val list =
    Seq[T](2, 3, 5, 7, 11, 13, 17, 23, 29)
      .take(f)
      .map(i => (x: T) => (x * i % l) + D.delay(d))
  def generators(x: T): Seq[T] = list.map(_(x))
  val initData = Seq[T](1, 2, 3, 4, 5, 6, 7, 8, 9)
}

class BigIntBench(l: Int, d: Int, f: Int) extends Definition {
  type T = BigInt
  val list =
    Seq[T](2, 3, 5, 7, 11, 13, 17, 23, 29)
      .take(f)
      .map(i => (x: T) => (x * i % l) + D.delay(d))
  def generators(x: T): Seq[T] = list.map(_(x))
  val initData = Seq[T](1, 2, 3, 4, 5, 6, 7, 8, 9)
}

class GenBench[A: Integral](l: Int, d: Int, f: Int) extends Definition {
  val ev = implicitly[Integral[A]]; import ev._
  @inline implicit def int2A(x: Int) = fromInt(x)
  type T = A
  val list =
    Seq[T](2, 3, 5, 7, 11, 13, 17, 23, 29)
      .take(f)
      .map(i => (x: T) => (x * i % l) + D.delay(d))
  def generators(x: T): Seq[T] = list.map(_(x))
  val initData = Seq[T](1, 2, 3, 4, 5, 6, 7, 8, 9)
}
