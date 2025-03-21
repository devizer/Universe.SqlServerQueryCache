using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Exporter;

partial class SqlCacheHtmlExporter
{
    private StepsLogger _StepsLogger;

    public SqlCacheHtmlExporter()
    {
        StepsLogger.TakeOwnership();
        _StepsLogger = StepsLogger.Instance;
    }

    StepsLogger.MeasureStepImplementation LogStep(string title)
    {
        return _StepsLogger?.LogStep(title);
    }

    public string GetLogsAsString()
    {
        return _StepsLogger?.GetLogsAsString();
    }

}