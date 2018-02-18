// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using Aspect;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Framework;
    using Utility;

    public class TestEngine : CrossAppDomainObject
    {
        public int Run(TestParameters testParameters)
        {
            return RunTests(testParameters);
        }

        [SeparateAppDomain]
        private TestDescription[] LoadTests(TestParameters testParameters)
        {
            return EnumerateTests(testParameters).ToArray();
        }

        private IEnumerable<TestDescription> EnumerateTests(TestParameters testParameters)
        {
            var allTests = new List<TestDescription>();
            foreach (var assemblyPathMask in testParameters.AssemblyPaths)
            {
                foreach (var assemblyPath in Directory.GetFiles(GetSafeDirectoryName(assemblyPathMask), Path.GetFileName(assemblyPathMask)))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(assemblyPath);
                        allTests.AddRange(EnumerateTests(assembly, testParameters));
                    }
                    catch
                    {
                    }
                }
            }

            return allTests;
        }

        private static string GetSafeDirectoryName(string path)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directoryName))
                    return directoryName;
            }
            catch (ArgumentException)
            { }

            return ".";
        }

        /// <summary>
        /// Enumerates the tests present in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        private IEnumerable<TestDescription> EnumerateTests(Assembly assembly, TestParameters parameters)
        {
            foreach (var testType in assembly.GetTypes().Where(parameters.Framework.IsTestClass))
            {
                foreach (var testDescription in EnumerateTests(assembly, testType, parameters))
                    yield return testDescription;
            }
        }

        private static IEnumerable<TestDescription> EnumerateTests(Assembly assembly, Type testType, TestParameters parameters)
        {
            var testFramework = parameters.Framework;
            MethodInfo assemblyInitialize = null, assemblyCleanup = null, classInitialize = null, classCleanup = null, testInitialize = null, testCleanup = null;
            foreach (var method in testType.GetMethods())
            {
                if (testFramework.IsAssemblySetupMethod(method))
                    assemblyInitialize = method;
                if (testFramework.IsAssemblyCleanupMethod(method))
                    assemblyCleanup = method;
                if (testFramework.IsTestSetupMethod(method))
                    testInitialize = method;
                if (testFramework.IsTestCleanupMethod(method))
                    testCleanup = method;
                if (testFramework.IsTypeSetupMethod(method))
                    classInitialize = method;
                if (testFramework.IsTypeCleanupMethod(method))
                    classCleanup = method;
            }

            foreach (var testMethod in testType.GetMethods().Where(testFramework.IsTestMethod))
                yield return new TestDescription(assembly.Location, testMethod, testInitialize, testCleanup, classInitialize, classCleanup, assemblyInitialize, assemblyCleanup, parameters);
        }

        [SeparateAppDomain]
        private int RunTests(TestParameters parameters)
        {
            var t0 = DateTime.UtcNow;
            var consoleWriter = new ConsoleWriter(Console.Out);

            if (parameters.Verbosity >= Verbosity.Detailed)
            {
                var literalThreading = parameters.ParallelRuns > 1 ? $"multi-thread ({parameters.ParallelRuns} threads)" : "single-thread";
                consoleWriter.WriteLine($"Threading:              {literalThreading}");
                var inconclusiveAsErrors = parameters.InconclusiveAsError ? "yes" : "no";
                consoleWriter.WriteLine($"Inconclusive as errors: {inconclusiveAsErrors}");
                consoleWriter.WriteLine($"Isolation:              {GetLiteral(parameters.Isolation)}");
            }

            var results = Test(parameters, consoleWriter);
            var successCount = results.Count(r => r.ResultCode == ResultCode.Success);
            var inconclusiveCount = results.Count(r => r.ResultCode == ResultCode.Inconclusive);
            var failureCount = results.Count(r => r.ResultCode == ResultCode.Failure);
            var timeoutCount = results.Count(r => r.ResultCode == ResultCode.Timeout);
            if (parameters.Verbosity >= Verbosity.Normal)
                consoleWriter.WriteLine();
            if (parameters.Verbosity >= Verbosity.Minimal)
            {
                consoleWriter.WriteLine($"Total tests          : {results.Length}");
                consoleWriter.WriteLine($"- succeeded tests    : {successCount}");
                consoleWriter.WriteLine($"- inconclusive tests : {inconclusiveCount}");
                consoleWriter.WriteLine($"- failed tests       : {failureCount}");
                consoleWriter.WriteLine($"- timeout tests      : {timeoutCount}");
                var dt = GetLiteral(DateTime.UtcNow - t0);
                consoleWriter.WriteLine($"Total time           : {dt}");
            }

            var errors = failureCount + timeoutCount;
            if (parameters.InconclusiveAsError)
                errors += inconclusiveCount;
            return errors;
        }

        private static string GetLiteral(IsolationLevel level)
        {
            switch (level)
            {
                case IsolationLevel.None:
                    return "none";
                case IsolationLevel.Assemblies:
                    return "assemblies (MSTest compatibility)";
                case IsolationLevel.Threads:
                    return "threads";
                case IsolationLevel.Everything:
                    return "assemblies+threads";
                case IsolationLevel.Tests:
                    return "tests";
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        /// <summary>
        /// Runs the tests described by parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public TestResult[] Test(TestParameters parameters)
        {
            return Test(parameters, new ConsoleWriter(Console.Out));
        }

        public TestResult[] Test(TestParameters parameters, ConsoleWriter consoleWriter)
        {
            var testDescriptions = EnumerateTests(parameters).ToArray();
            if (parameters.Verbosity >= Verbosity.Detailed)
            {
                var assemblies = testDescriptions.Select(t => t.AssemblyName).Distinct();
                var testedAssemblies = string.Join(Environment.NewLine + "                        ", assemblies);
                consoleWriter.WriteLine($"Tested assemblies:      {testedAssemblies}");
                consoleWriter.WriteLine();
            }
            return Test(testDescriptions, parameters, consoleWriter);
        }

        public TestResult[] Test(TestDescription[] testDescriptions, TestParameters parameters)
        {
            return Test(testDescriptions, parameters, new ConsoleWriter(Console.Out));
        }

        public TestResult[] Test(TestDescription[] testDescriptions, TestParameters parameters, ConsoleWriter consoleWriter)
        {
            var maxNameLength = Math.Min(testDescriptions.Aggregate(0, (s, t) => Math.Max(s, t.TestName.Length)) + 1, 80);
            var groupedTestDescriptions = GroupDescriptions(testDescriptions, parameters);
            var testSets = groupedTestDescriptions.Select(t => new TestSet(t, testDescriptions.Length));
            var results = testSets.SelectMany(testSet => Test(testSet, parameters, consoleWriter, maxNameLength)).ToArray();
            return results;
        }

        [SeparateAppDomain]
        private TestResult[] Test(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter, int maxNameLength)
        {
            // when assemblies are isolated, they all run with the same instances (and in the same appdomain)
            var testInstances = parameters.Isolation.HasFlag(IsolationLevel.Threads) ? null : new TestInstances();
            var runners = CreateRunners(testSet, parameters, consoleWriter, testInstances, maxNameLength);
            Await(runners);
            if (testInstances != null)
                testSet.PushResults(testInstances.Cleanup(parameters));
            return testSet.Results;
        }

        private static IEnumerable<IEnumerable<TestDescription>> GroupDescriptions(IEnumerable<TestDescription> testDescriptions, TestParameters parameters)
        {
            if (parameters.Isolation.HasFlag(IsolationLevel.Assemblies))
                return testDescriptions.GroupBy(t => t.AssemblyName);
            return new[] { testDescriptions };
        }

        /// <summary>
        /// Creates the runners (whose count depends on /m parameter).
        /// </summary>
        /// <param name="testSet">The test set.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="consoleWriter">The output writer.</param>
        /// <param name="testInstances">The test instances.</param>
        /// <param name="maxNameLength"></param>
        /// <returns></returns>
        private IEnumerable<Thread> CreateRunners(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter, TestInstances testInstances,
            int maxNameLength)
        {
            for (int runnerIndex = 0; runnerIndex < parameters.ParallelRuns; runnerIndex++)
            {
                var thread = new Thread(delegate ()
                {
                    if (testInstances != null)
                        InlineParallelRunner(testSet, parameters, consoleWriter, testInstances, maxNameLength);
                    else
                        SeparatedParallelRunner(testSet, parameters, consoleWriter, maxNameLength);
                });
                thread.Start();
                yield return thread;
            }
        }

        private static void Await(IEnumerable<Thread> threads)
        {
            var allThreads = threads.ToArray();
            for (; ; )
            {
                if (allThreads.All(t => !t.IsAlive))
                    return;
                Thread.Sleep(100);
            }
        }

        [SeparateAppDomain]
        private void SeparatedParallelRunner(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter, int maxNameLength)
        {
            var testInstances = new TestInstances();
            InlineParallelRunner(testSet, parameters, consoleWriter, testInstances, maxNameLength);
            testSet.PushResults(testInstances.Cleanup(parameters));
        }

        private void InlineParallelRunner(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter, TestInstances testInstances, int maxNameLength)
        {
            for (; ; )
            {
                var testDescription = testSet.PullNextTest();
                if (testDescription == null)
                    break;

                TestResult testResult;
                if (parameters.Isolation.HasFlag(IsolationLevel.Tests))
                    testResult = SeparatedTest(testDescription, parameters);
                else
                    testResult = InlineTest(testDescription, testInstances, parameters);

                TraceResult(testSet, testResult, consoleWriter, parameters, maxNameLength);
                testSet.PushResult(testResult);
            }
        }

        private static TestResult TraceResult(TestSet testSet, TestResult testResult, ConsoleWriter consoleWriter, TestParameters parameters, int maxNameLength)
        {
            // below normal, nothing is shown
            if (parameters.Verbosity < Verbosity.Normal)
                return testResult;

            var methodName = testResult.Description.TestName;
            var testStepResult = testResult.TestStepResult;
            methodName = methodName.Substring(0, Math.Min(methodName.Length, maxNameLength)).PadRight(maxNameLength);
            var literalTime = GetLiteral(testResult.Duration);
            var totalTests = testSet.Count.ToString(CultureInfo.InvariantCulture);
            consoleWriter.MarkerPadding = (int)Math.Log10(testSet.Count) + 1;
            var resultCode = GetLiteral(testStepResult?.ResultCode ?? ResultCode.Success);
            // >= Normal: single line
            var outputLine = $"[{ConsoleWriter.IndexMarker}/{totalTests}] {methodName}: {resultCode.PadRight(12)} ({literalTime.PadLeft(7)})";
            // >= Detailed: stack trace on error
            if (parameters.Verbosity >= Verbosity.Detailed && testResult.ResultCode != ResultCode.Success)
                outputLine += Environment.NewLine + testResult.TestStepResult?.Exception;
            // >= Diagnostic: full output
            if (parameters.Verbosity >= Verbosity.Diagnostic)
            {
                var output = testResult.TestStepResult?.Output;
                if (!string.IsNullOrEmpty(output))
                    outputLine += Environment.NewLine + output;
            }
            consoleWriter.WriteLine(outputLine);
            return testResult;
        }

        private static string GetLiteral(ResultCode resultCode)
        {
            switch (resultCode)
            {
                case ResultCode.Success:
                    return "OK";
                case ResultCode.Inconclusive:
                    return "Inconclusive";
                case ResultCode.Failure:
                    return "Failed";
                case ResultCode.Timeout:
                    return "Timed out";
                default:
                    throw new ArgumentOutOfRangeException(nameof(resultCode), resultCode, null);
            }
        }

        private static string GetLiteral(TimeSpan timeSpan)
        {
            var formatProvider = CultureInfo.InvariantCulture;

            var milliseconds = (int)timeSpan.TotalMilliseconds;
            if (milliseconds < 10000)
                return milliseconds.ToString(formatProvider) + "ms";
            var literal = timeSpan.Seconds.ToString(formatProvider) + "s";
            var minutes = (int)timeSpan.TotalMinutes;
            if (minutes > 0)
                literal = minutes.ToString(formatProvider) + "mn" + literal;
            return literal;
        }

        [SeparateAppDomain]
        private TestResult SeparatedTest(TestDescription testDescription, TestParameters parameters)
        {
            var testInstances = new TestInstances();
            var testResult = InlineTest(testDescription, testInstances, parameters);
            // TODO: do not ignore the results
            testInstances.Cleanup(parameters);
            return testResult;
        }

        private TestResult InlineTest(TestDescription testDescription, TestInstances testInstances, TestParameters parameters)
        {
            var t0 = DateTime.UtcNow;
            var stepResults = Test(testDescription, testInstances, parameters).ToArray();
            var testResult = new TestResult(testDescription, stepResults, DateTime.UtcNow - t0);
            return testResult;
        }

        /// <summary>
        /// Runs a single test.
        /// </summary>
        /// <param name="testDescription">The test description.</param>
        /// <param name="testInstances">The test instances.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A list of <see cref="StepResult"/>. The last of them indicates overall success. Multiple assessments will give details</returns>
        private IEnumerable<StepResult> Test(TestDescription testDescription, TestInstances testInstances, TestParameters parameters)
        {
            // initialize test
            var testInstance = testInstances.Get(testDescription, out var initializationFailureTestAssessment, parameters.Framework);
            if (initializationFailureTestAssessment != null)
                return new[] { initializationFailureTestAssessment };

            // run it
            var stepResults = new StepResults();
            var thread = ThreadUtility.Start(Test, testDescription, testInstance, parameters.Framework, stepResults);
            // wait for test
            if (thread.Join(parameters.Timeout)) // test ended in time
                return stepResults.Results;

            // in case it took too long, say it
            thread.Abort();
            return new[] { new StepResult(TestStep.Test, ResultCode.Timeout, null, stepResults.Output) };
        }

        private static void Test(TestDescription testDescription, TestInstance testInstance, ITestFramework testFramework, StepResults results)
        {
            using (var consoleCapture = new ConsoleCapture())
            {
                try
                {
                    results.Results = Test(testDescription, testInstance, testFramework).ToArray();
                }
                finally // when thread is aborted
                {
                    results.Output = consoleCapture.Capture;
                }
            }
        }

        private static IEnumerable<StepResult> Test(TestDescription testDescription, TestInstance testInstance, ITestFramework testFramework)
        {
            // if initializer fails, no need to run the test
            var testAssessment = StepResult.Get(testDescription.TestInitialize, TestStep.TestInitialize, testInstance.Instance, testFramework, testInstance.Context)
                                 ?? StepResult.Get(testDescription.TestMethod, TestStep.Test, testInstance.Instance, testFramework, testInstance.Context)
                                 ?? StepResult.TestSuccess;
            yield return testAssessment;
            var cleanupTestAssessment = StepResult.Get(testDescription.TestCleanup, TestStep.TestCleanup, testInstance.Instance, testFramework, testInstance.Context);
            if (cleanupTestAssessment != null)
                yield return cleanupTestAssessment;
        }
    }
}
