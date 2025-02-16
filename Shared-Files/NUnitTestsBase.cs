using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Universe.CpuUsage;

namespace Universe.NUnitTests
{
    public class NUnitTestsBase
    {
        public static bool IsTravis => Environment.GetEnvironmentVariable("TRAVIS") == "true";
        public static bool AllowLongListsOutput => "True".Equals(Environment.GetEnvironmentVariable("ALLOW_LONG_LIST_OUTPUT"), StringComparison.OrdinalIgnoreCase);

        protected static TextWriter OUT;
        private Stopwatch StartAt;
        private CpuUsage.CpuUsage? _CpuUsage_OnStart;
        private int TestCounter = 0, TestClassCounter = 0;
        private int TestCounterStorage = 0;
        private static int TestClassCounterStorage = 0;


        Action OnDisposeList = () => { };
        Action OnDisposeClassList = () => { };

        protected void OnDispose(string title, Action action, TestDisposeOptions mode)
        {
            if (title.IndexOf('\'') < 0) title = $"'{title}'";

            var testId = TestId;
            var testName = TestContext.CurrentContext.Test.Name;
            bool isIgnoringError = (mode & TestDisposeOptions.IgnoreError) != 0;
            bool isGlobal = (mode & TestDisposeOptions.Global) != 0;
            bool isClass = (mode & TestDisposeOptions.Class) != 0;
            if (isClass && isGlobal) throw new ArgumentException("Both Global and Class mode is nonsense");
            var isAsync = (mode & TestDisposeOptions.Async) != 0;

            string whenPrefix = isGlobal ? "Global " : isClass ? "Class Bound " : "";
            Action actionWithLog = () =>
            {
                
                string prefix = $"Dispose {whenPrefix}{(isAsync ? "Async " : "")}{testId}{(isGlobal || isClass ? $" {testName}" : "")}";
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    action();
                    var msec = sw.ElapsedTicks * 1000d / Stopwatch.Frequency;
                    Console.WriteLine($"[{prefix}] {title} success (took {msec:n1} milliseconds)");
                }
                catch (Exception ex)
                {
                    var msec = sw.ElapsedTicks * 1000d / Stopwatch.Frequency;
                    var err = isIgnoringError ? $"[{ex.GetType()}] {ex.Message}" : Environment.NewLine + ex;
                    Console.WriteLine($"[{prefix}] {title} failed (took {msec:n1} milliseconds).{err}");
                }
            };

            var actionWrapped = isAsync
                ? () => ThreadPool.QueueUserWorkItem(_ => actionWithLog())
                : actionWithLog;

            if (isGlobal) 
                GlobalTestsTearDown.OnDisposeInternal(actionWrapped);
            else if (isClass)
                OnDisposeClassList += actionWrapped;
            else
                OnDisposeList += actionWrapped;
        }


        protected string TestId => $"#{TestClassCounter}.{TestCounter}";


        [SetUp]
        public void BaseSetUp()
        {
            TestConsole.Setup();
            Environment.SetEnvironmentVariable("SKIP_FLUSHING", null);
            StartAt = Stopwatch.StartNew();
            _CpuUsage_OnStart = GetCpuUsage();
            TestCounter = Interlocked.Increment(ref TestCounterStorage);

            var testClassName = TestContext.CurrentContext.Test.ClassName;
            testClassName = testClassName.Split('.').LastOrDefault();
            Console.WriteLine($"#{TestClassCounter}.{TestCounter} {{{TestContext.CurrentContext.Test.Name}}} @ {testClassName} starting...");
        }

        private CpuUsage.CpuUsage? GetCpuUsage()
        {
            try
            {
                // return LinuxResourceUsage.GetByThread();
                return CpuUsage.CpuUsage.Get(CpuUsageScope.Thread);
            }
            catch
            {
            }

            return null;
        }

        [TearDown]
        public void BaseTearDown()
        {
            var elapsed = StartAt.Elapsed;
            var cpuUsage = "";
            if (_CpuUsage_OnStart.HasValue)
            {
                var onEnd = GetCpuUsage();
                if (onEnd != null)
                {
                    var delta = CpuUsage.CpuUsage.Substruct(onEnd.Value, _CpuUsage_OnStart.Value);
                    var user = delta.UserUsage.TotalMicroSeconds / 1000d;
                    var kernel = delta.KernelUsage.TotalMicroSeconds / 1000d;
                    var perCents = (user + kernel) / 1000d / elapsed.TotalSeconds;
                    cpuUsage = $" (cpu: {perCents * 100:f0}%, {user + kernel:n3} = {user:n3} [user] + {kernel:n3} [kernel] milliseconds)";
                }
            }

            Console.WriteLine(
                $"#{TestClassCounter}.{TestCounter} {{{TestContext.CurrentContext.Test.Name}}} >{TestContext.CurrentContext.Result.Outcome.Status.ToString().ToUpper()}< in {elapsed}{cpuUsage}");

            var copy = OnDisposeList;
            OnDisposeList = () => { };
            if (copy.GetInvocationList().Length > 0)
            {
                Stopwatch sw = Stopwatch.StartNew();
                copy();
            }

            Console.WriteLine("");
        }

        [OneTimeSetUp]
        public void BaseOneTimeSetUp()
        {
            TestClassCounter = Interlocked.Increment(ref TestClassCounterStorage);
            TestConsole.Setup();
        }

        [OneTimeTearDown]
        public void BaseOneTimeTearDown()
        {
            var copy = OnDisposeClassList;
            OnDisposeClassList = () => { };
            copy?.Invoke();
        }

        protected static void SilentExecute(Action action)
        {
            try
            {
                action();
            }
            catch
            {
            }
        }

        public static T SilentEvaluate<T>(Func<T> factory)
        {
            try
            {
                return factory();
            }
            catch
            {
                return default(T);
            }
        }


        protected static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
				return false;
#endif
            }
        }

        public class TestConsole
        {
            private static bool Done = false;

            public static void Setup()
            {
                if (!Done)
                {
                    Done = true;
                    Console.SetOut(new TW());
                }
            }

            private class TW : TextWriter
            {
                public override Encoding Encoding { get; }

                public override void WriteLine(string value)
                {
                    try
                    {
                        TestContext.Progress.WriteLine(value);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }

    public enum Os
    {
        Windows,
        Mac,
        Linux,
        FreeBSD,
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RequiredOsAttribute : NUnitAttribute, IApplyToTest
    {
        public readonly Os[] OperatingSystems;

        public RequiredOsAttribute(params Os[] operatingSystems)
        {
            if (operatingSystems == null) throw new ArgumentNullException(nameof(operatingSystems));
            OperatingSystems = operatingSystems;
        }

        public void ApplyToTest(Test test)
        {
            if (test.RunState == RunState.NotRunnable)
            {
                return;
            }

            bool isIt = false;
            if (OperatingSystems.Contains(Os.Windows) && CrossInfo.ThePlatform == CrossInfo.Platform.Windows) isIt = true;
            if (OperatingSystems.Contains(Os.Linux) && CrossInfo.ThePlatform == CrossInfo.Platform.Linux) isIt = true;
            if (OperatingSystems.Contains(Os.Mac) && CrossInfo.ThePlatform == CrossInfo.Platform.MacOSX) isIt = true;
            if (OperatingSystems.Contains(Os.FreeBSD) && CrossInfo.ThePlatform == CrossInfo.Platform.FreeBSD) isIt = true;

            if (!isIt)
            {
                test.RunState = RunState.Ignored;
                string onOs = string.Join(", ", OperatingSystems);
                if (OperatingSystems.Length == 0) onOs = "none of any OS";
                test.Properties.Set(PropertyNames.SkipReason, $"This test should run only on '{onOs}'");
            }
        }
    }

    [Flags]
    public enum TestDisposeOptions
    {
        Default = 0,
        Class = 1,
        Global = 2,
        Async = 4,
        IgnoreError = 8,
    }
}

[SetUpFixture]
public class GlobalTestsTearDown
{
    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        var copy = OnDisposeList;
        OnDisposeList = () => { };
        if (copy.GetInvocationList().Length > 1)
        {
            Stopwatch sw = Stopwatch.StartNew();
            copy();
            Console.WriteLine($"[Global Dispose] Completed in {sw.ElapsedMilliseconds:n0} milliseconds");
        }
    }

    static Action OnDisposeList = () => { };

    public static void OnDisposeInternal(Action action) => OnDisposeList += action;

}
