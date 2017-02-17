package com.bizagi.webhook

import com.microsoft.azure.storage.CloudStorageAccount
import com.microsoft.azure.storage.table.{TableOperation, TableServiceEntity}

import scala.util.Try

/**
  * Created by dev-camiloh on 2/9/17.
  */
object Facts {

  case class Fact(pk: String, id: String, value: String) extends TableServiceEntity {
    override def getPartitionKey: String = pk
    override def getRowKey: String = id
  }

  def saveFact(fact: Fact): Try[Unit] = {
    Try {
      val storageAccount =
        CloudStorageAccount.parse("DefaultEndpointsProtocol=http;AccountName=camilohbotxjt4cl;AccountKey=pnPB+Nb6NYjLWpWE+pvKbUNodBO77wuBaq6fTT3Woyn0ysn67+2F119n2AlOOCMoyota4+LCesDM04PqoNuZPg==");
      val tableClient = storageAccount.createCloudTableClient();
      val cloudTable = tableClient.getTableReference("fact");
      cloudTable.createIfNotExists();
      val insertFact = TableOperation.insertOrReplace(fact);
      cloudTable.execute(insertFact)
    }
  }

}
