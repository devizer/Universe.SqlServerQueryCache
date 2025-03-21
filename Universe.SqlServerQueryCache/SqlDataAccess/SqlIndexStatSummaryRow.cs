﻿using System.Data;
using System.Globalization;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlIndexStatSummaryRow
{
    public int DatabaseId { get; set; }
    public string Database { get; set; }
    public int SchemaId { get; set; }
    public string SchemaName { get; set; }
    public string ObjectType { get; set; }
    public int ObjectId { get; set; }
    public string ObjectName { get; set; }
    public bool IsMsShipped { get; set; }
    public int IndexId { get; set; }
    public string IndexName { get; set; }
    public string IndexType { get; set; }
    public int PartitionNumber { get; set; }
    public long PageLatchWaitCount { get; set; }
    public long PageLatchWaitInMs { get; set; }
    public long PageIoLatchWaitCount { get; set; }
    public long PageIoLatchWaitInMs { get; set; }
    public IDictionary<string, long> Metrics { get; set; }

    public long? GetMetricValue(string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) && Metrics.TryGetValue(propertyName, out var rawRet) ? rawRet : null;
    }

    public string ObjectTypeName => CultureInfo.InvariantCulture.TextInfo.ToTitleCase((ObjectType ?? "").Replace("_", " ").ToLower()).Replace("User Table", "Table");


}