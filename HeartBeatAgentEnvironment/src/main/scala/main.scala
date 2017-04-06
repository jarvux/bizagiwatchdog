/**
  * Created by JarviZ on 03/04/2017.
  */

import java.net.{SocketTimeoutException, UnknownHostException}
import java.time.{Clock, LocalDateTime, LocalTime}

import akka.actor.ActorSystem
import akka.stream.ActorMaterializer
import akka.stream.scaladsl.Source

import scala.concurrent.duration._
import scala.util.Try
import scalaj.http.Http
import com.microsoft.azure.storage.CloudStorageAccount
import com.microsoft.azure.storage.table.{CloudTableClient, TableOperation, TableQuery, TableServiceEntity}
import net.liftweb.json._


object Main extends App {

  implicit val system = ActorSystem("HeartBeatAgent")
  implicit val materializer = ActorMaterializer()
  val interval = 10
  val runCloudEnvironmentsService = "http://dev-diegopt/BizagiCloudRun/api/environments/all"

  Source
    .tick(0 seconds, interval second, "")
    .map(_ => invokeEnvService(runCloudEnvironmentsService))
    .map(list => list.par.map(e => (e, ping(e), interval)))
    .runForeach(l => l.map(r => aggregate(r._1, r._2, interval)))

  def invokeEnvService (url: String): List[Env] = {
    Try {
      Http(url).timeout(10000, 20000).asString
    }.map { r => stringToList(r.body) }.getOrElse(List.empty)
  }

  def stringToList(body: String): List[Env] = {
    implicit val formats = DefaultFormats
    parse(body).extract[List[Env]]
  }

  def ping(env: Env): PingResult =
    Try {
      Http(s"${env.publicUrl}/jquery/version.json.txt").timeout(1000, 5000).asString
    }.filter { r => r.code == 200 && r.body.contains("build")
    }.map { r => PingSuccess(r.body)
    }.recover {
      case _: SocketTimeoutException => PingTimeout("503")
      case _: UnknownHostException => PingTimeout("505")
      case _: Exception => PingError("500")
    }.getOrElse(PingError("509"))

  def aggregate(env: Env, ping: PingResult, interval: Double): Unit = {

    val storageAccount = CloudStorageAccount.parse("DefaultEndpointsProtocol=http;AccountName=eus20functions0dev0sa;AccountKey=JpUG8+fMZuCqMs9++vzQbFzHrUFQykzWZO3thqXTN70fE/edkXusYPhYwEu1Y/tTdgeVJjBJQiTyxzmy4Zk3MQ==")
    val tableClient = storageAccount.createCloudTableClient()
    val cloudTable = tableClient.getTableReference("performed10")
    cloudTable.createIfNotExists()

    val utcTime = LocalDateTime.now(Clock.systemUTC())
    //val timeStamp = Timestamp.valueOf(LocalDateTime.now());

    val pk = env.environmentId.toString
    val rk = s"${utcTime.getYear}-${utcTime.getMonth.getValue}-${utcTime.getDayOfMonth}-ENGINE".toString

    val fact = new FactConsolidated
    fact.setPartitionKey(pk)
    fact.setRowKey(rk)
    fact.setLapse(interval)

    val hasRow = Option(cloudTable.execute(TableOperation.retrieve(pk, rk, classOf[FactConsolidated])).getResultAsType[FactConsolidated])
    val consolidate = hasRow.getOrElse(fact)

    ping match {
      case PingSuccess(_) => {
        consolidate.setStatus(true)
        consolidate.setUp(consolidate.getUp() + ((interval * 100.0) / 86400.0))
      }
      case PingError(statusCode) => {
        consolidate.setStatus(false)
        consolidate.setDown(consolidate.getDown() + ((interval * 100.0) / 86400.0))
        createIncident(pk, s"${rk}_${utcTime.toLocalTime}", statusCode, storageAccount)
      }
      case PingTimeout(statusCode) => {
        consolidate.setStatus(false)
        consolidate.setDown(consolidate.getDown() + ((interval * 100.0) / 86400.0))
        createIncident(pk, s"${rk}_${utcTime.toLocalTime}", statusCode, storageAccount)
      }
    }

    cloudTable.execute(TableOperation.insertOrReplace(consolidate))
  }

  def createIncident(pk: String, rk: String, statusCode: String, storageAccount: CloudStorageAccount): Unit = {

    val incident = new Incidents
    incident.setPartitionKey(pk)
    incident.setRowKey(rk)
    incident.setStatusCode(statusCode)

    val tableClient = storageAccount.createCloudTableClient()
    val cloudTable = tableClient.getTableReference("incidents10")
    cloudTable.createIfNotExists()
    cloudTable.execute(TableOperation.insertOrReplace(incident))
  }

  case class Env(publicUrl: String, environmentId: String)

  trait PingResult

  case class PingSuccess(body: String) extends PingResult {
  }

  case class PingError(statusCode: String) extends PingResult {
  }

  case class PingTimeout(statusCode: String) extends PingResult {
  }

  class FactConsolidated extends TableServiceEntity {

    override def getPartitionKey: String = super.getPartitionKey

    override def setPartitionKey(partitionKey: String): Unit = super.setPartitionKey(partitionKey)

    override def getRowKey: String = super.getRowKey

    override def setRowKey(rowKey: String): Unit = super.setRowKey(rowKey)

    private var Status: Boolean = false

    def getStatus(): Boolean = Status

    def setStatus(status: Boolean): Unit = {
      Status = status
    }

    private var Lapse: Double = 0.0

    def getLapse(): Double = Lapse

    def setLapse(lapse: Double): Unit = {
      Lapse = lapse
    }

    private var Up: Double = 0.0

    def getUp(): Double = Up

    def setUp(up: Double): Unit = {
      Up = up
    }

    private var Down: Double = 0.0

    def getDown(): Double = Down

    def setDown(down: Double): Unit = {
      Down = down
    }

    private var Partial: Double = 0.0

    def getPartial(): Double = Partial

    def setPartial(partial: Double): Unit = {
      Partial = partial
    }
  }

  class Incidents extends TableServiceEntity {

    override def getPartitionKey: String = super.getPartitionKey

    override def setPartitionKey(partitionKey: String): Unit = super.setPartitionKey(partitionKey)

    override def getRowKey: String = super.getRowKey

    override def setRowKey(rowKey: String): Unit = super.setRowKey(rowKey)

    private var StatusCode: String = null

    def getStatusCode(): String = StatusCode

    def setStatusCode(statusCode: String): Unit = {
      StatusCode = statusCode
    }
  }

}

