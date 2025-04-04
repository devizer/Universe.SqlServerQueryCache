﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.Exporter;

namespace Universe.SqlServerQueryCache.External
{
    public class StepsLogger
    {

#if NET46_OR_GREATER || NETCOREAPP1_0_OR_GREATER || NET6_0 || NETSTANDARD1_0_OR_GREATER
        private static AsyncLocal<StepsLogger> _Instance = new AsyncLocal<StepsLogger>();
#else
        private static ThreadLocal<StepsLogger> _Instance = new ThreadLocal<StepsLogger>();
#endif

        public static StepsLogger Instance => _Instance.Value;

        public static void TakeOwnership()
        {
            _Instance.Value = new StepsLogger();
        }

        public static void ReleaseOwnership()
        {
            _Instance.Value = null;
        }

        public string GetLogsAsString()
        {
            int index = 0;
            StringBuilder ret = new StringBuilder();
            var col1Width = LogSteps.Any() ? LogSteps.Select(x => x.Title.Length).Max() : 7;
            foreach (var step in LogSteps)
            {
                var i = $"{++index}:";
                var kb = $"{(step.DeltaMemory / 1024):n0} Kb";
                kb = kb.StartsWith("-") ? kb : "+" + kb;
                var perCentCpuUsage = step.Duration > 0 ? $"{step.CpuUsage.TotalMicroSeconds / 1000000d / step.Duration * 100:n0}%" : "";
                ret.AppendLine($"{i,3} {step.Title.PadRight(col1Width)} {kb,-11}  {perCentCpuUsage,6}  {step.Duration * 1000,-10:n3} {step.CpuUsage}");
            }

            return ret.ToString();
        }

        public MeasureStepImplementation LogStep(string title)
        {
            return new MeasureStepImplementation(this, title);
        }

        public class MeasureStepImplementation : IDisposable
        {
            readonly private StepsLogger Parent;
            private string Title;
            private Stopwatch StartAt;
            private CpuUsage.CpuUsage CpuUsage0;
            private long Memory0;


            public MeasureStepImplementation(StepsLogger parent, string title)
            {
                Parent = parent;
                Init(title);
            }

            public void Restart(string title)
            {
                Finish();
                Init(title);
            }

            public void Dispose()
            {
                Finish();
            }

            private void Init(string title)
            {
                Title = title;
                StartAt = Stopwatch.StartNew();
                Memory0 = Process.GetCurrentProcess().WorkingSet64;
                CpuUsage0 = Universe.CpuUsage.CpuUsage.GetByThread().GetValueOrDefault();

            }

            private void Finish()
            {
                var cpuUsage1 = Universe.CpuUsage.CpuUsage.GetByThread().GetValueOrDefault();
                var duration = StartAt.Elapsed.TotalSeconds;
                var memory1 = Process.GetCurrentProcess().WorkingSet64;
                Parent.LogSteps.Add(new LogStepRow()
                {
                    Title = Title,
                    Duration = duration,
                    CpuUsage = cpuUsage1 - CpuUsage0,
                    DeltaMemory = memory1 - Memory0,
                });
            }
        }

        private List<LogStepRow> LogSteps = new List<LogStepRow>();
        public class LogStepRow
        {
            public string Title { get; set; }
            public double Duration { get; set; }
            public CpuUsage.CpuUsage CpuUsage { get; set; }
            public long DeltaMemory { get; set; }
        }

    }
}
