package com.bizagi.webhook

import akka.actor.ActorSystem
import akka.http.scaladsl.Http
import akka.http.scaladsl.marshallers.sprayjson.SprayJsonSupport
import akka.http.scaladsl.server.Directives._
import akka.stream.ActorMaterializer
import com.bizagi.webhook.Facts.PingPerformed

import scala.io.StdIn
import akka.http.scaladsl.unmarshalling.FromRequestUnmarshaller
import akka.http.scaladsl.unmarshalling.Unmarshal
import spray.json.DefaultJsonProtocol

/**
  * Created by dev-camiloh on 2/9/17.
  */
object WebServer extends App {

  implicit val system = ActorSystem("Facts")
  implicit val materializer = ActorMaterializer()
  // needed for the future flatMap/onComplete in the end
  implicit val executionContext = system.dispatcher

  case class Params(timestamp: Long, env: String, status: Boolean, node: String, statusCode: Int, lapse: Double)

  object PersonJsonSupport extends DefaultJsonProtocol with SprayJsonSupport {
    implicit val PortofolioFormats = jsonFormat6(Params)
  }

  import PersonJsonSupport._

  val route =
    path("api" / "webhook" / "pingperformed") {
      get {
        complete("ping")
      } ~ post {
        entity(as[Params]) { p =>
          complete{
            val response: String = Facts.saveFact(new PingPerformed( p.timestamp, p.env, p.status, p.node, p.statusCode, p.lapse ))
              .map(_ => "fact was saved")
              .recover {
                case e: Exception => e.printStackTrace(); e.toString
              }
              .getOrElse("error")
            response
          }
        }
      }
    }

  val bindingFuture = Http().bindAndHandle(route, "0.0.0.0", 8000)

  println(s"Server online at http://localhost:8000/\nPress RETURN to stop...")
  StdIn.readLine() // let it run until user presses return
  bindingFuture
    .flatMap(_.unbind()) // trigger unbinding from the port
    .onComplete(_ => system.terminate()) // and shutdown when done

}
