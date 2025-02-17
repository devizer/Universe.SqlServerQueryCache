using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache;

public class SortingDefinition
{
    public Func<IEnumerable<QueryCacheRow>, IEnumerable<QueryCacheRow>> SortAction { get; set; }
    public string SortPropertyName { get; set; }
    public bool IsDescending { get; set; }
}

public class AllSortingDefinitions
{
    static SortingDefinition CreateDescendingSortingDefinition<TProperty>(string title, Func<QueryCacheRow, TProperty> sort)
    {
        return new SortingDefinition()
        {
            SortAction = rows => rows.OrderByDescending(sort),
            IsDescending = true,
            SortPropertyName = title,
        };
    }
    public static IEnumerable<SortingDefinition> Get()
    {
        yield return CreateDescendingSortingDefinition("ExecutionCount", r => r.ExecutionCount);

        yield return CreateDescendingSortingDefinition("TotalElapsedTime", r => r.TotalElapsedTime);
        yield return CreateDescendingSortingDefinition("LastElapsedTime", r => r.LastElapsedTime);
        yield return CreateDescendingSortingDefinition("MinElapsedTime", r => r.MinElapsedTime);
        yield return CreateDescendingSortingDefinition("MaxElapsedTime", r => r.MaxElapsedTime);

        yield return CreateDescendingSortingDefinition("TotalWorkerTime", r => r.TotalWorkerTime);
        yield return CreateDescendingSortingDefinition("LastWorkerTime", r => r.LastWorkerTime);
        yield return CreateDescendingSortingDefinition("MinWorkerTime", r => r.MinWorkerTime);
        yield return CreateDescendingSortingDefinition("MaxWorkerTime", r => r.MaxWorkerTime);

        yield return CreateDescendingSortingDefinition("TotalPhysicalReads", r => r.TotalPhysicalReads);
        yield return CreateDescendingSortingDefinition("LastPhysicalReads", r => r.LastPhysicalReads);
        yield return CreateDescendingSortingDefinition("MinPhysicalReads", r => r.MinPhysicalReads);
        yield return CreateDescendingSortingDefinition("MaxPhysicalReads", r => r.MaxPhysicalReads);
        
        yield return CreateDescendingSortingDefinition("TotalLogicalWrites", r => r.TotalLogicalWrites);
        yield return CreateDescendingSortingDefinition("LastLogicalWrites", r => r.LastLogicalWrites);
        yield return CreateDescendingSortingDefinition("MinLogicalWrites", r => r.MinLogicalWrites);
        yield return CreateDescendingSortingDefinition("MaxLogicalWrites", r => r.MaxLogicalWrites);
    }

}