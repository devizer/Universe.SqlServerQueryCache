using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.External;
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
    public string TheCaption { get; set; }
    public string PropertyName { get; set; }
    public bool IsDescending { get; set; }
    public bool AllowSort => SortAction != null;
}

public class AllSortingDefinitions
{
    static ColumnDefinition CreateSortableColumn<TProperty>(string caption, Expression<Func<QueryCacheRow, TProperty>> sort)
    {
        var compiled = sort.Compile();
        return new ColumnDefinition()
        {
            PropertyAccessor = r => compiled(r),
            SortAction = rows => rows.OrderByDescending(compiled).ThenByDescending(x => x.AvgElapsedTime),
            IsDescending = true,
            TheCaption = caption,
            PropertyName = ExpressionExtensions.GetName(sort),
        };
    }
    static ColumnDefinition CreateNonSortableColumn<TProperty>(string caption, Func<QueryCacheRow, TProperty> sort)
    {
        return new ColumnDefinition()
        {
            PropertyAccessor = r => sort(r),
            SortAction = null,
            TheCaption = caption,
            PropertyName = null,
        };
    }

    public static IEnumerable<TableHeaderDefinition> GetHeaders()
    {
        yield return new TableHeaderDefinition("Summary")
            .AddColumn(CreateSortableColumn("Count", r => r.ExecutionCount))
            .AddColumn(CreateSortableColumn("Created At", r => r.CreationTime));

        yield return new TableHeaderDefinition("Duration")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalElapsedTime))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgElapsedTime))
            // .AddColumn(CreateSortableColumn("Last", r => r.LastElapsedTime))
            .AddColumn(CreateSortableColumn("Min", r => r.MinElapsedTime))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxElapsedTime));

        yield return new TableHeaderDefinition("CPU Time")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalWorkerTime))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgWorkerTime))
            // .AddColumn(CreateSortableColumn("Last", r => r.LastWorkerTime))
            .AddColumn(CreateSortableColumn("Min", r => r.MinWorkerTime))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxWorkerTime));

        yield return new TableHeaderDefinition("Logical Reads")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalLogicalReads))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgLogicalReads))
            // .AddColumn(CreateSortableColumn("Last", r => r.LastLogicalReads))
            .AddColumn(CreateSortableColumn("Min", r => r.MinLogicalReads))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxLogicalReads));

        yield return new TableHeaderDefinition("Physical Reads")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalPhysicalReads))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgPhysicalReads))
            // .AddColumn(CreateSortableColumn("Last", r => r.LastPhysicalReads))
            .AddColumn(CreateSortableColumn("Min", r => r.MinPhysicalReads))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxPhysicalReads));

        yield return new TableHeaderDefinition("Writes")
            .AddColumn(CreateSortableColumn("Total", r => r.TotalLogicalWrites))
            .AddColumn(CreateSortableColumn("Avg", r => r.AvgLogicalWrites))
            // .AddColumn(CreateSortableColumn("Last", r => r.LastLogicalWrites))
            .AddColumn(CreateSortableColumn("Min", r => r.MinLogicalWrites))
            .AddColumn(CreateSortableColumn("Max", r => r.MaxLogicalWrites));
    }

    public static IEnumerable<ColumnDefinition> Get()
    {
        return GetHeaders().SelectMany(x => x.Columns).Where(x => x.AllowSort);
    }

}