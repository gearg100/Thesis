Concordance
===========

In order to compile and run the Scala version one has to install [Scala 2.9.2](http://www.scala-lang.org/downloads) and [sbt](http://www.scala-sbt.org/release/docs/Getting-Started/Setup.html).

To compile this vesion of Concordance, on Scala folder in a terminal type:
```
sbt compile
```
The resulting classes will appear in **target**.
Another option is to create a jar file by:
```
sbt assembly
```
The jar will appear in **target**

To run from Scala folder:
```
scala target/concordance.jar
```