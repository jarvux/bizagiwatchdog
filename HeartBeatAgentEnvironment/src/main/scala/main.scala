/**
  * Created by JarviZ on 03/04/2017.
  */

import java.net.SocketTimeoutException
import java.time.{Clock, LocalDateTime, LocalTime}
import java.util.UUID

import akka.actor.ActorSystem
import akka.stream.ActorMaterializer
import akka.stream.scaladsl.Source

import scala.concurrent.duration._
import scala.util.Try
import scalaj.http.Http
import com.microsoft.azure.storage.CloudStorageAccount
import com.microsoft.azure.storage.table.{TableOperation, TableQuery, TableServiceEntity}


object Main extends App {

  implicit val system = ActorSystem("HeartBeatAgent")
  implicit val meterializer = ActorMaterializer()

  Source
    .tick(0 seconds, 10 second, "")
    .map(_ => invokeEnvService())
    .map(envs => envs.par.map( e => (e, ping(e))))
    .runForeach(l => l.map(r => aggregate(r._1, r._2)))

  def invokeEnvService(): List[Env] = List(
    Env("http://localhost/BizAgiR110x/", UUID.randomUUID().toString.toUpperCase),
    Env("http://localhost/BizAgiR110x/", UUID.fromString("EDD916DA-FD37-4F8D-95EA-91A43FE26649").toString.toUpperCase)
  )

  def ping(env: Env): PingResult =
    Try {
      Http(s"${env.url}/jquery/version.json.txt").timeout(1000, 5000).asString
    }.filter {
      r => r.code == 200 && r.body.contains("build")
    }.map {
      r => PingSuccess(r.body)
    }.recover {
        case _: SocketTimeoutException => PingTimeout
        case e: Exception => PingError(e.getMessage)
      }.getOrElse(PingError("unknown error"))



  def aggregate(env: Env, ping: PingResult): Unit = {

    val storageAccount = CloudStorageAccount.parse("DefaultEndpointsProtocol=http;AccountName=eus20functions0dev0sa;AccountKey=3YUdyjVyVFhxie6WrMqmnuyuPgC18LzA9L6kPCkkP5igzdU+fsnfqU9++iMUyAsckWUg6SRIs79EL0LKCheqYA==")
    val tableClient = storageAccount.createCloudTableClient()
    val cloudTable = tableClient.getTableReference("FactPerformed2")
    cloudTable.createIfNotExists()

    val utcTime = LocalDateTime.now(Clock.systemUTC())
    val pk = env.env.toString
    val rk = s"${ utcTime.getYear}-${utcTime.getMonth.getValue}-${ utcTime.getDayOfMonth}-ENGINE".toString
    val hasRow = Option(cloudTable.execute( TableOperation.retrieve(pk,rk, classOf[FactConsolidated])).getResultAsType[FactConsolidated])

    val lapse = 10.0 //10 seconds

    val fact = new FactConsolidated
    fact.setPartitionKey(pk)
    fact.setRowKey(rk)
    fact.setLapse(lapse)

    val consolidate = hasRow.getOrElse( fact )
    val minxDay = ((lapse * 100.0) / 86400.0)

    ping match {
      case PingSuccess(e) => {
        fact.setStatus(true)
        consolidate.setUp(consolidate.getUp()+ minxDay)
      }
      case PingError(e) => {
        fact.setStatus(false)
        consolidate.setDown( consolidate.getDown()+ minxDay )
      }
      case PingTimeout => {
        fact.setStatus(false)
        consolidate.setDown( consolidate.getDown()+ minxDay )
      }
    }
    cloudTable.execute(TableOperation.insertOrReplace(consolidate))
  }

  case class Env(url: String, env: String)

  trait PingResult

 case class PingSuccess(body: String) extends PingResult{

  }

  case class PingError(body: String) extends PingResult{

  }

  case object PingTimeout extends PingResult{

  }

  class FactConsolidated extends TableServiceEntity {

    override def getPartitionKey: String = super.getPartitionKey
    override def setPartitionKey(partitionKey: String): Unit = super.setPartitionKey(partitionKey)

    override def getRowKey: String = super.getRowKey
    override def setRowKey(rowKey: String): Unit = super.setRowKey(rowKey)

    private var Status: Boolean = false
    def getStatus(): Boolean = Status
    def setStatus(status: Boolean): Unit = { Status = status }

    private  var Lapse: Double = 0.0
    def getLapse(): Double = Lapse
    def setLapse(lapse: Double): Unit = { Lapse = lapse }

    private  var Up:Double = 0.0
    def getUp(): Double = Up
    def setUp(up: Double): Unit = { Up = up }

    private  var Down: Double = 0.0
    def getDown(): Double = Down
    def setDown(down: Double): Unit = { Down = down }

    private var Partial: Double = 0.0
    def getPartial(): Double = Partial
    def setPartial(partial: Double): Unit = { Partial = partial }
  }

  case class DetailError(val env: String, val timestamp: Long, val statusCode: Int, val message: String)
    extends TableServiceEntity {

    override def getPartitionKey: String = env
    override def getRowKey: String = timestamp.toString

    def getStatusCode(): Int = statusCode
    def setStatusCode(statusCode: Int): Unit = ???

    def getMessage():String = message
    def SetMessage(message: String): Unit = ???
  }
}

