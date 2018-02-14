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
    using Reflection;

    public class TestEngine : MarshalByRefObject
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

        private IEnumerable<TestDescription> EnumerateTests(Assembly assembly, TestParameters parameters)
        {
            foreach (var testType in assembly.GetTypes().Where(t => parameters.Framework.IsTestClass(t)))
            {
                foreach (var testDescription in EnumerateTests(assembly, testType, parameters))
                    yield return testDescription;
            }
        }

        private static IEnumerable<TestDescription> EnumerateTests(Assembly assembly, Type testType, TestParameters parameters)
        {
            MethodInfo assemblyInitialize = null, assemblyCleanup = null, classInitialize = null, classCleanup = null, testInitialize = null, testCleanup = null;
            foreach (var method in testType.GetMethods())
            {
                if (parameters.Framework.IsAssemblySetupMethod(method))
                    assemblyInitialize = method;
                if (parameters.Framework.IsAssemblyCleanupMethod(method))
                    assemblyCleanup = method;
                if (parameters.Framework.IsTestSetupMethod(method))
                    testInitialize = method;
                if (parameters.Framework.IsTestCleanupMethod(method))
                    testCleanup = method;
                if (parameters.Framework.IsTypeSetupMethod(method))
                    classInitialize = method;
                if (parameters.Framework.IsTypeCleanupMethod(method))
                    classCleanup = method;
            }

            foreach (var testMethod in testType.GetMethods().Where(m => parameters.Framework.IsTestMethod(m)))
                yield return new TestDescription(assembly.Location, testMethod, testInitialize, testCleanup, classInitialize, classCleanup, assemblyInitialize, assemblyCleanup);
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
                var testedAssemblies = string.Join(Environment.NewLine + "                      ", assemblies);
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
            var groupedTestDescriptions = GroupDescriptions(testDescriptions, parameters);
            var testSets = groupedTestDescriptions.Select(t => new TestSet(t, testDescriptions.Length));
            var results = testSets.SelectMany(testSet => Test(testSet, parameters, consoleWriter)).ToArray();
            return results;
        }

        [SeparateAppDomain]
        private TestResult[] Test(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter)
        {
            // when assemblies are isolated, they all run with the same instances (and in the same appdomain)
            var testInstances = parameters.Isolation.HasFlag(IsolationLevel.Threads) ? null : new TestInstances();
            var runners = CreateRunners(testSet, parameters, consoleWriter, testInstances);
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
        /// <returns></returns>
        private IEnumerable<Thread> CreateRunners(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter, TestInstances testInstances)
        {
            for (int runnerIndex = 0; runnerIndex < parameters.ParallelRuns; runnerIndex++)
            {
                var thread = new Thread(delegate ()
                {
                    if (testInstances != null)
                        InlineParallelRunner(testSet, parameters, consoleWriter, testInstances);
                    else
                        SeparatedParallelRunner(testSet, parameters, consoleWriter);
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
        private void SeparatedParallelRunner(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter)
        {
            var testInstances = new TestInstances();
            InlineParallelRunner(testSet, parameters, consoleWriter, testInstances);
            testSet.PushResults(testInstances.Cleanup(parameters));
        }

        private void InlineParallelRunner(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter, TestInstances testInstances)
        {
            for (; ; )
            {
                var testDescription = testSet.PullNextTest();
                if (testDescription == null)
                    break;

                var t0 = DateTime.UtcNow;
                var stepResults = Test(testDescription, testInstances, parameters).ToArray();
                var testAssessments = new TestResult(testDescription, stepResults, DateTime.UtcNow - t0);
                TraceResult(testSet, testAssessments, consoleWriter, parameters);
                testSet.PushResult(testAssessments);
            }
        }

        private static TestResult TraceResult(TestSet testSet, TestResult testResult, ConsoleWriter consoleWriter, TestParameters parameters)
        {
            // below normal, nothing is shown
            if (parameters.Verbosity < Verbosity.Normal)
                return testResult;

            var methodName = testResult.Description.MethodName;
            const int totalWidth = 60;
            var testStepResult = testResult.TestStepResult;
            methodName = methodName.Substring(0, Math.Min(methodName.Length, totalWidth)).PadRight(totalWidth);
            var literalTime = GetLiteral(testResult.Duration);
            var totalTests = testSet.Count.ToString(CultureInfo.InvariantCulture);
            var resultCode = GetLiteral(testStepResult?.ResultCode ?? ResultCode.Success);
            // >= Normal: single line
            var outputLine = $"[{ConsoleWriter.IndexMarker}/{totalTests}] {methodName}: {resultCode.PadRight(12)} ({literalTime.PadLeft(10)})";
            // >= Detailed: stack trace on error
            if (parameters.Verbosity >= Verbosity.Detailed && testResult.ResultCode != ResultCode.Success)
                outputLine += Environment.NewLine + testResult.TestStepResult?.Exception;
            // >= Diagnostic: full output
            if (parameters.Verbosity >= Verbosity.Diagnostic)
                outputLine += Environment.NewLine + testResult.TestStepResult?.Output;
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
            var testInstance = testInstances.Get(testDescription, out var initializationFailureTestAssessment, parameters);
            if (initializationFailureTestAssessment != null)
            {
                yield return initializationFailureTestAssessment;
                yield break;
            }

            // run it
            StepResult[] stepResults = null;
            string consoleOutput = null;
            var thread = new Thread(delegate ()
            {
                using (var consoleCapture = new ConsoleCapture())
                {
                    stepResults = Test(testDescription, testInstance, parameters).ToArray();
                    consoleOutput = consoleCapture.Capture;
                }
            });
            thread.Start();
            // wait for test
            if (thread.Join(parameters.Timeout)) // test ended in time
            {
                foreach (var stepResult in stepResults)
                    yield return stepResult;
                yield break;
            }

            // in case it took too long, say it
            thread.Abort();
            yield return new StepResult(TestStep.Test, ResultCode.Timeout, null, consoleOutput);
        }

        private static IEnumerable<StepResult> Test(TestDescription testDescription, TestInstance testInstance, TestParameters parameters)
        {
            // if initializer fails, no need to run the test
            var testAssessment = StepResult.Get(testDescription.TestInitialize, TestStep.TestInitialize, testInstance.Instance, parameters, testInstance.Context)
                                 ?? StepResult.Get(testDescription.TestMethod, TestStep.Test, testInstance.Instance, parameters, testInstance.Context)
                                 ?? StepResult.TestSuccess;
            yield return testAssessment;
            var cleanupTestAssessment = StepResult.Get(testDescription.TestCleanup, TestStep.TestCleanup, testInstance.Instance, parameters, testInstance.Context);
            if (cleanupTestAssessment != null)
                yield return cleanupTestAssessment;
        }
    }
}
