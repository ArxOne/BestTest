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
                        allTests.AddRange(EnumerateTests(assembly));
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

        private IEnumerable<TestDescription> EnumerateTests(Assembly assembly)
        {
            foreach (var testType in assembly.GetTypes().Where(IsTestClass))
            {
                foreach (var testDescription in EnumerateTests(assembly, testType))
                    yield return testDescription;
            }
        }

        private static IEnumerable<TestDescription> EnumerateTests(Assembly assembly, Type testType)
        {
            MethodInfo assemblyInitialize = null, assemblyCleanup = null, classInitialize = null, classCleanup = null, testInitialize = null, testCleanup = null;
            foreach (var method in testType.GetMethods())
            {
                if (!method.IsValidTestMethod())
                    continue;
                // static methods only allowed in assembly manipulation
                if (method.IsStatic)
                {
                    if (method.HasAnyAttribute("AssemblyInitialize"))
                        assemblyInitialize = method;
                    if (method.HasAnyAttribute("AssemblyCleanup"))
                        assemblyCleanup = method;
                }
                else
                {
                    if (method.HasAnyAttribute("TestInitialize"))
                        testInitialize = method;
                    if (method.HasAnyAttribute("TestCleanup"))
                        testCleanup = method;
                }

                // class methods apparently can be both (mother fuckers!)
                if (method.HasAnyAttribute("ClassInitialize"))
                    classInitialize = method;
                if (method.HasAnyAttribute("ClassCleanup"))
                    classCleanup = method;
            }

            foreach (var testMethod in testType.GetMethods().Where(t => IsTestMethod(t) && !t.IsStatic))
                yield return new TestDescription(assembly.Location, testMethod, testInitialize, testCleanup, classInitialize, classCleanup, assemblyInitialize, assemblyCleanup);
        }

        private static bool IsTestClass(Type type) => type.IsValidTestType() && type.HasAnyAttribute("TestClass", "TestFixture");
        private static bool IsTestMethod(MethodInfo method) => method.IsValidTestMethod() && method.HasAnyAttribute("TestMethod", "Test");

        [SeparateAppDomain]
        private int RunTests(TestParameters parameters)
        {
            var t0 = DateTime.UtcNow;
            var consoleWriter = new ConsoleWriter(Console.Out);
            var results = Test(parameters, consoleWriter);
            var successCount = results.Count(r => r.ResultCode == ResultCode.Success);
            var inconclusiveCount = results.Count(r => r.ResultCode == ResultCode.Inconclusive);
            var failureCount = results.Count(r => r.ResultCode == ResultCode.Failure);
            var timeoutCount = results.Count(r => r.ResultCode == ResultCode.Timeout);
            consoleWriter.WriteLine();
            consoleWriter.WriteLine($"Total tests          : {results.Length}");
            consoleWriter.WriteLine($"- succeeded tests    : {successCount}");
            consoleWriter.WriteLine($"- inconclusive tests : {inconclusiveCount}");
            consoleWriter.WriteLine($"- failed tests       : {failureCount}");
            consoleWriter.WriteLine($"- timeout tests      : {timeoutCount}");
            var dt = GetLiteral(DateTime.UtcNow - t0);
            consoleWriter.WriteLine($"Total time           : {dt}");
            var errors = failureCount + timeoutCount;
            if (parameters.InconclusiveAsError)
                errors += inconclusiveCount;
            return errors;
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
            var testInstances = parameters.IsolateAssemblies ? new TestInstances() : null;
            var runners = CreateRunners(testSet, parameters, consoleWriter, testInstances);
            Await(runners);
            if (testInstances != null)
                testSet.PushResults(testInstances.Cleanup());
            return testSet.Results;
        }

        private static IEnumerable<IEnumerable<TestDescription>> GroupDescriptions(IEnumerable<TestDescription> testDescriptions, TestParameters parameters)
        {
            if (parameters.IsolateAssemblies)
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
            testSet.PushResults(testInstances.Cleanup());
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
                TraceResult(testAssessments, consoleWriter, testSet);
                testSet.PushResult(testAssessments);
            }
        }

        private static TestResult TraceResult(TestResult testResult, ConsoleWriter consoleWriter, TestSet testSet)
        {
            var methodName = testResult.Description.MethodName;
            const int totalWidth = 60;
            var testStepResult = testResult.TestStepResult;
            methodName = methodName.Substring(0, Math.Min(methodName.Length, totalWidth)).PadRight(totalWidth);
            var literalTime = GetLiteral(testResult.Duration);
            var totalTests = testSet.Count.ToString(CultureInfo.InvariantCulture);
            var resultCode = GetLiteral(testStepResult?.ResultCode ?? ResultCode.Success);
            consoleWriter.WriteLine($"[{ConsoleWriter.IndexMarker}/{totalTests}] {methodName}: {resultCode.PadRight(12)} ({literalTime.PadLeft(10)})");
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
            var testInstance = testInstances.Get(testDescription, out var initializationFailureTestAssessment);
            if (initializationFailureTestAssessment != null)
                yield return initializationFailureTestAssessment;

            // run it
            StepResult[] stepResults = null;
            string consoleOutput = null;
            var thread = new Thread(delegate ()
            {
                using (var consoleCapture = new ConsoleCapture())
                {
                    stepResults = Test(testDescription, testInstance).ToArray();
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

        private static IEnumerable<StepResult> Test(TestDescription testDescription, TestInstance testInstance)
        {
            // if initializer fails, no need to run the test
            var testAssessment = StepResult.Get(testDescription.TestInitialize, TestStep.TestInitialize, testInstance.Instance, testInstance.Context)
                                 ?? StepResult.Get(testDescription.TestMethod, TestStep.Test, testInstance.Instance, testInstance.Context)
                                 ?? StepResult.TestSuccess;
            yield return testAssessment;
            var cleanupTestAssessment = StepResult.Get(testDescription.TestCleanup, TestStep.TestCleanup, testInstance.Instance, testInstance.Context);
            if (cleanupTestAssessment != null)
                yield return cleanupTestAssessment;
        }
    }
}
