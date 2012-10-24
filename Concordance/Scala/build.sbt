import sbtassembly.Plugin._
import AssemblyKeys._

name := "Concordance"

scalaVersion := "2.9.2"

autoCompilerPlugins := true

assemblySettings

jarName in assembly := "concordance.jar"

mainClass in assembly := Some("concordance.ConcordanceAkka")
