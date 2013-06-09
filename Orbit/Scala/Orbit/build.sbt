import AssemblyKeys._ // put this at the top of the file

assemblySettings

name := "Orbit"

version := "0.1"

scalaVersion := "2.10.2"

jarName in assembly := "orbit.jar"

resolvers += "Typesafe Repository" at "http://repo.typesafe.com/typesafe/releases/"

libraryDependencies ++= Seq(
	"com.typesafe.akka" %% "akka-actor" % "2.1.+", 
	"com.typesafe" % "config" % "1.0.0"
)
