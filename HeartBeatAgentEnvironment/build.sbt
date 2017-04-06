import sbt._
import Keys._
import sbtassembly.Plugin._
import AssemblyKeys._

assemblySettings

enablePlugins(DockerPlugin)

name := "HeartBeatAgentEnvironment"

version := "1.0"

scalaVersion := "2.12.1"
/*
libraryDependencies += "com.typesafe.akka" %% "akka-actor" % "2.4.17"
libraryDependencies += "com.typesafe" % "config" % "1.3.0"
libraryDependencies += "org.apache.kafka" % "kafka-clients" % "0.9.0.0"
libraryDependencies += "com.typesafe.akka" % "akka-stream_2.12" % "2.5-M2"
libraryDependencies +=  "org.scalaj" %% "scalaj-http" % "2.3.0"

*/

libraryDependencies ++= Seq(
  "com.typesafe.akka" %% "akka-actor" % "2.4.17",
  "com.typesafe" % "config" % "1.3.0",
  "org.apache.kafka" % "kafka-clients" % "0.9.0.0",
  "com.typesafe.akka" % "akka-stream_2.12" % "2.5-M2",
  "org.scalaj" %% "scalaj-http" % "2.3.0",
  "com.microsoft.azure" % "azure-storage" % "5.0.0"
)

libraryDependencies += "net.liftweb" % "lift-webkit_2.12" % "3.1.0-M1"

mergeStrategy in assembly := {
  case "META-INF/MSFTSIG.RSA" => MergeStrategy.first
  case x =>
    val oldStrategy = (mergeStrategy in assembly).value
    oldStrategy(x)
}

dockerfile in docker := {
  // The assembly task generates a fat JAR file
  val artifact: File = assembly.value
  val artifactTargetPath = s"/app/${artifact.name}"

  new Dockerfile {
    from("jdk:1.8")
    add(artifact, artifactTargetPath)
    workDir("/app")
    entryPoint("java", "-jar", artifactTargetPath)
  }
}
