Concordance
===========

In order to compile and run the Scala version one has to install [Scala 2.9.2](http://www.scala-lang.org/downloads) and [sbt](http://www.scala-sbt.org/release/docs/Getting-Started/Setup.html).

To compile this vesion of Concordance, from Scala folder in a terminal:
```
>sbt compile
```
The resulting classes will appear in **target**.
Another option is to create a jar file:
```
>sbt assembly
```
The jar will appear in **target**

To run from Scala folder:
```
>scala target/concordance.jar
```

Important! This implementation is **not** memory efficient and the standard 256MB of heap size  will not suffice.
A heap size of 1 - 1.5 GB will be necessary for the standart sequence limit.
More details on [Stackoverflow](http://stackoverflow.com/questions/1441373/increase-jvm-heap-size-for-scala)
