package com.bizagi.webhook

import com.microsoft.azure.storage.CloudStorageAccount
import com.microsoft.azure.storage.table.{TableOperation, TableServiceEntity}
import scala.util.Try

/**
  * Created by dev-camiloh on 2/9/17.
  */
object Facts {

  case class PingPerformed(val timestamp: Long , val env: String, val status: Boolean, val node: String, val statusCode: Int, val lapse: Double) extends TableServiceEntity {
    override def getPartitionKey: String = env
    override def getRowKey: String = timestamp.toString

    def getNode(): String = node
    def setNode(node: String): Unit = ???

    def getStatus(): Boolean = status
    def setStatus(status: Boolean): Unit = ???

    def getStatusCode(): Int = statusCode
    def setStatusCode(statusCode: Int): Unit = ???

    def getLapse(): Double = lapse
    def setLapse(lapse: Double): Unit = ???
  }

  def saveFact(fact: PingPerformed): Try[Unit] = {
    Try {
      val storageAccount =
        CloudStorageAccount.parse("DefaultEndpointsProtocol=http;AccountName=camilohbotxjt4cl;AccountKey=pnPB+Nb6NYjLWpWE+pvKbUNodBO77wuBaq6fTT3Woyn0ysn67+2F119n2AlOOCMoyota4+LCesDM04PqoNuZPg==")
      val tableClient = storageAccount.createCloudTableClient()
      val cloudTable = tableClient.getTableReference("FactPerformed")
      cloudTable.createIfNotExists()
      val insertFact = TableOperation.insertOrReplace(fact)
      cloudTable.execute(insertFact)
    }
  }
}
