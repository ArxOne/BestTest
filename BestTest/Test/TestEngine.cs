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
            return Test(testParameters);
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
        public int Test(TestParameters parameters)
        {
            var t0 = DateTime.UtcNow;
            var consoleWriter = new ConsoleWriter(Console.Out);
            var testDescriptions = EnumerateTests(parameters);
            var testSet = new TestSet(testDescriptions);
            var runners = CreateRunners(testSet, parameters, consoleWriter);
            Await(runners);
            var results = testSet.Results;
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
            var dt = DateTime.UtcNow - t0;
            consoleWriter.WriteLine($"Total time           : {dt}");
            var errors = failureCount + timeoutCount;
            if (parameters.InconclusiveAsError)
                errors += inconclusiveCount;
            return errors;
        }

        /// <summary>
        /// Creates the runners (whose count depends on /m parameter).
        /// </summary>
        /// <param name="testSet">The test set.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="consoleWriter">The output writer.</param>
        /// <returns></returns>
        private IEnumerable<Thread> CreateRunners(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter)
        {
            for (int runnerIndex = 0; runnerIndex < parameters.ParallelRuns; runnerIndex++)
            {
                var thread = new Thread(delegate () { ParallelRunner(testSet, parameters, consoleWriter); });
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
        private void ParallelRunner(TestSet testSet, TestParameters parameters, ConsoleWriter consoleWriter)
        {
            var testInstances = new TestInstances();
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
            consoleWriter.WriteLine($"[{ConsoleWriter.IndexMarker}/{totalTests}] {methodName}: {testStepResult?.ResultCode ?? ResultCode.Success} ({literalTime})");
            return testResult;
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
            TestInstance testInstance;
            using (new ConsoleCapture())
            {
                testInstance = testInstances.Get(testDescription, out var initializationFailureTestAssessment);
                if (initializationFailureTestAssessment != null)
                    yield return initializationFailureTestAssessment;
            }

            // run it
            StepResult[] stepResults = null;
            var thread = new Thread(delegate () { stepResults = Test(testDescription, testInstance).ToArray(); });
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
            yield return new StepResult(TestStep.Test, ResultCode.Timeout, null);
        }

        private static IEnumerable<StepResult> Test(TestDescription testDescription, TestInstance testInstance)
        {
            using (new ConsoleCapture())
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
}
