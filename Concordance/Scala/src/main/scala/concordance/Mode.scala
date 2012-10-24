package concordance

import akka.actor.Actor
import akka.event.Logging

trait Mode { this : Actor => 
  def log(str : => String):Unit
  def assert(body : => Boolean):Unit
}

trait Debug extends Mode { this : Actor =>
  val logging = Logging(context.system,this)
  
  override def log(str : => String) = logging.info(str)
  override def assert(body : => Boolean) = Predef.assert(body)  
}

trait Release extends Mode { this: Actor =>
  override def log(str : => String){}
  override def assert(body : => Boolean){}  
}