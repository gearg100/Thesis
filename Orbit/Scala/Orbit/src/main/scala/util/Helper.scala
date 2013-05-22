package util

object Helper {
  @inline def timedRun[T](body: => T) = {
    val t1 = System.currentTimeMillis
    val res = body
    val t2 = System.currentTimeMillis
    (res, t2 - t1)
  }
}