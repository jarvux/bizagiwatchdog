#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Win32;

public static async Task Run(string facts, CloudTable tableWriterBinding, TraceWriter log)
{
    var fact = JsonConvert.DeserializeObject<Fact>(facts);
    var utcDateFact = DateTime.UtcNow;
    var pk = fact.Env;
    var rk = $"{utcDateFact.Year}-{utcDateFact.Month}-{utcDateFact.Day}_{fact.Component}";
    float minxDay = (((float)fact.Lapse * 100) / 1440);
    var retrievedDayResult = await tableWriterBinding.ExecuteAsync(TableOperation.Retrieve<RowByDay>(pk, rk));
    var consolidatedByDay = (RowByDay)retrievedDayResult.Result;

    if (consolidatedByDay == null)
    {
        consolidatedByDay = new RowByDay
        {
            PartitionKey = pk,
            RowKey = rk,
            Status = fact.Status,
            Partial = 0.0,
            Down = fact.Status ? 0.0 : minxDay,
            Up = fact.Status ? minxDay : 0.0
        };
    }
    else
    {
        consolidatedByDay.Status = fact.Status;
        consolidatedByDay.Up = fact.Status ? (consolidatedByDay.Up + minxDay) : consolidatedByDay.Up;
        consolidatedByDay.Down = fact.Status ? consolidatedByDay.Down : (consolidatedByDay.Down + minxDay);
    }

    await tableWriterBinding.ExecuteAsync(TableOperation.InsertOrReplace(consolidatedByDay));
}

public class Fact
{
    public string Env { get; set; }
    public string Node { get; set; }
    public bool Status { get; set; }
    public int Lapse { get; set; }
    public string Component { get; set; }
    public long Timestamp { get; set; }
    public int StatusCode { get; set; }
}
public class RowByDay : TableEntity
{
    public bool Status { get; set; }
    public double Partial { get; set; }
    public double Down { get; set; }
    public double Up { get; set; }
}