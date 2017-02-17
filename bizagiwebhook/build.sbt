import sbt._
import Keys._
import sbtassembly.Plugin._
import AssemblyKeys._

assemblySettings

enablePlugins(DockerPlugin)

name := "bizagiwebhook"

version := "1.0"

scalaVersion := "2.12.1"

libraryDependencies ++= Seq(
  "com.typesafe.akka" %% "akka-http-core" % "10.0.3",
  "com.typesafe.akka" %% "akka-http" % "10.0.3",
  "com.typesafe.akka" %% "akka-http-testkit" % "10.0.3",
  "com.typesafe.akka" %% "akka-http-spray-json" % "10.0.3",
  "com.typesafe.akka" %% "akka-http-jackson" % "10.0.3",
  "com.typesafe.akka" %% "akka-http-xml" % "10.0.3",
  "com.microsoft.azure" % "azure-storage" % "5.0.0"
)

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
    //add(new File("/Users/dev-camiloh/Camilo/Data/dv/github/bizagiwatchdog/bizagiwebhook"), "/app/")
    workDir("/app")
    entryPoint("java", "-jar", artifactTargetPath)
  }
}

