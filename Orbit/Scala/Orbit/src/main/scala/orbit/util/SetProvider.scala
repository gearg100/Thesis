package orbit.util

import collection.{ immutable => i, mutable => m, concurrent => c }

trait SetProvider {
  def iSet[A]: i.Set[A]
  def mSet[A]: m.Set[A]
  def cMap[A]: c.Map[A, Unit]
}

object ScalaSets extends SetProvider {
  def iSet[A]: i.Set[A] = i.Set[A]()
  def mSet[A]: m.Set[A] = m.Set[A]()
  def cMap[A]: c.Map[A, Unit] = c.TrieMap[A, Unit]()
}

object JavaSets extends SetProvider {
  import java.{ util => ju }, java.util.{ concurrent => juc }
  import collection.convert.WrapAsScala._
  def iSet[A]: i.Set[A] = throw new NoSuchElementException("Immutable Java Set")
  def mSet[A]: m.Set[A] = new ju.HashSet[A]
  def cMap[A]: c.Map[A, Unit] = new juc.ConcurrentHashMap[A, Unit]
}