using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache;

public class TableHeaderDefinition
{
    public string Caption { get; set; }
    public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();

    public TableHeaderDefinition(string caption)
    {
        Caption = caption;
    }

    public TableHeaderDefinition AddColumn(ColumnDefinition column)
    {
        Columns.Add(column);
        return this;
    }


}
public class ColumnDefinition
{
    public Func<QueryCacheRow, object> PropertyAccessor { get; set; }
    public Func<IEnumerable<QueryCacheRow>, IEnumerable<QueryCacheRow>> SortAction { get; set; }
    public string PropertyName { get; set; }
    public bool IsDescending { get; set; }
    public bool AllowSort => SortAction != null;
}

public class AllSortingDefinitions
{
    static ColumnDefinition CreateSortableColumn<TProperty>(string propertyName, Func<QueryCacheRow, TProperty> sort)
    {
        return new ColumnDefinition()
        {
            PropertyAccessor = r => sort(r),
            SortAction = rows => rows.OrderByDescending(sort),
            IsDescending = true,
            PropertyName = propertyName,
        };
    }
    static ColumnDefinition CreateNonSortableColumn<TProperty>(string propertyName, Func<QueryCacheRow, TProperty> sort)
    {
        return new ColumnDefinition()
        {
            PropertyAccessor = r => sort(r),
            SortAction = null,
            PropertyName = propertyName,
        };
    }

    public static IEnumerable<TableHeaderDefinition> GetHeaders()
    {
        yield return new TableHeaderDefinition("Summary")
            .AddColumn(CreateSortableColumn("Count", r => r.ExecutionCount))
            .AddColumn(CreateNonSortableColumn("Created at", r => r.CreationTime));

        yield return new TableHeaderDefinition("Duration")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalElapsedTime))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgElapsedTime))
            .AddColumn(CreateSortableColumn("Last", r => r.LastElapsedTime))
            .AddColumn(CreateSortableColumn("Min", r => r.MinElapsedTime))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxElapsedTime));

        yield return new TableHeaderDefinition("CPU Time")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalWorkerTime))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgWorkerTime))
            .AddColumn(CreateSortableColumn("Last", r => r.LastWorkerTime))
            .AddColumn(CreateSortableColumn("Min", r => r.MinWorkerTime))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxWorkerTime));

        yield return new TableHeaderDefinition("Physical Reads")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalPhysicalReads))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgPhysicalReads))
            .AddColumn(CreateSortableColumn("Last", r => r.LastPhysicalReads))
            .AddColumn(CreateSortableColumn("Min", r => r.MinPhysicalReads))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxPhysicalReads));

        yield return new TableHeaderDefinition("Writes")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalLogicalWrites))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgLogicalWrites))
            .AddColumn(CreateSortableColumn("Last", r => r.LastLogicalWrites))
            .AddColumn(CreateSortableColumn("Min", r => r.MinLogicalWrites))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxLogicalWrites));
    }

    public static IEnumerable<ColumnDefinition> Get()
    {
        yield return CreateNonSortableColumn("CreationTime", r => r.CreationTime);

        yield return CreateSortableColumn("ExecutionCount", r => r.ExecutionCount);

        yield return CreateSortableColumn("TotalElapsedTime", r => r.TotalElapsedTime);
        yield return CreateSortableColumn("AvgElapsedTime", r => r.AvgElapsedTime);
        yield return CreateSortableColumn("LastElapsedTime", r => r.LastElapsedTime);
        yield return CreateSortableColumn("MinElapsedTime", r => r.MinElapsedTime);
        yield return CreateSortableColumn("MaxElapsedTime", r => r.MaxElapsedTime);

        yield return CreateSortableColumn("TotalWorkerTime", r => r.TotalWorkerTime);
        yield return CreateSortableColumn("AvgWorkerTime", r => r.AvgWorkerTime);
        yield return CreateSortableColumn("LastWorkerTime", r => r.LastWorkerTime);
        yield return CreateSortableColumn("MinWorkerTime", r => r.MinWorkerTime);
        yield return CreateSortableColumn("MaxWorkerTime", r => r.MaxWorkerTime);

        yield return CreateSortableColumn("TotalPhysicalReads", r => r.TotalPhysicalReads);
        yield return CreateSortableColumn("AvgPhysicalReads", r => r.AvgPhysicalReads);
        yield return CreateSortableColumn("LastPhysicalReads", r => r.LastPhysicalReads);
        yield return CreateSortableColumn("MinPhysicalReads", r => r.MinPhysicalReads);
        yield return CreateSortableColumn("MaxPhysicalReads", r => r.MaxPhysicalReads);

        yield return CreateSortableColumn("TotalLogicalWrites", r => r.TotalLogicalWrites);
        yield return CreateSortableColumn("AvgLogicalWrites", r => r.AvgLogicalWrites);
        yield return CreateSortableColumn("LastLogicalWrites", r => r.LastLogicalWrites);
        yield return CreateSortableColumn("MinLogicalWrites", r => r.MinLogicalWrites);
        yield return CreateSortableColumn("MaxLogicalWrites", r => r.MaxLogicalWrites);
    }

}