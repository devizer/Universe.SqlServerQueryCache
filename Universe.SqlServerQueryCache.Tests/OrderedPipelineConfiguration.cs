using System.Reflection;
using System.Runtime.Versioning;
using NUnit.Framework.Interfaces;
using Universe.NUnitPipeline;


// Single shared configuration for all the test assemblies
public class OrderedPipelineConfiguration
{
    public static void Configure()
    {
        PipelineLog.LogTrace($"[{typeof(OrderedPipelineConfiguration)}]::Configure()");

        var reportConfiguration = NUnitPipelineConfiguration.GetService<NUnitReportConfiguration>();
        reportConfiguration.InternalReportFile = Path.Combine("TestsOutput", $"SQL Server Administrative Views {GetCurrentNetVersion()}");

        

        var chain = NUnitPipelineConfiguration.GetService<NUnitPipelineChain>();

        chain.OnStart = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnStart },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnStart },
        };

        chain.OnEnd = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnFinish },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnFinish },
            new() { Title = DisposeInterceptor.Title, Action = DisposeInterceptor.OnFinish },
            new() { Title = CpuUsageTreeReportInterceptor.Title, Action = CpuUsageTreeReportInterceptor.OnFinish },
            new() { Title = "Debugger Global Finish", Action = MyGlobalFinish },
        };
    }

    private static string GetCurrentNetVersion()
    {
        var targetFramework = Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(TargetFrameworkAttribute))
            .OfType<TargetFrameworkAttribute>()
            .FirstOrDefault()
            ?.FrameworkName;

        var ver = targetFramework?.Split('=')?.Last();

        return ver == null ? "" : $" NET {ver}";
    }


    static void MyGlobalFinish(NUnitStage stage, ITest test)
    {
        PipelineLog.LogTrace($"[OrderedPipelineConfiguration.MyGlobalFinish] Finish Type='{test.TestType}' for Test='{test.Name}' on NUnitPipeline");
    }

}