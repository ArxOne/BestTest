﻿// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using Aspect;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Reflection;

    public class TestEngine : MarshalByRefObject
    {
        public int Run(TestParameters testParameters)
        {
            Test(testParameters);
            return 0;
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
        public void Test(TestParameters parameters)
        {
            var consoleWriter = new ConsoleWriter(Console.Out);
            var tests = EnumerateTests(parameters);
            var testSet = new TestSet(tests);
            var runners = CreateRunners(testSet, parameters, consoleWriter);
            Await(runners);
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

                var assessment = Test(testDescription, testInstances, parameters);
                var testAssessments = new TestAssessments(testDescription, assessment);
                Trace(testAssessments, consoleWriter);
                testSet.PushAssessment(testAssessments);
            }
        }

        private static TestAssessments Trace(TestAssessments testAssessments, ConsoleWriter consoleWriter)
        {
            var methodName = testAssessments.Description.MethodName;
            const int totalWidth = 60;
            var testAssessment = testAssessments.TestStepResult;
            methodName = methodName.Substring(0, Math.Min(methodName.Length, totalWidth)).PadRight(totalWidth);
            consoleWriter.WriteLine($"{methodName}: {testAssessment?.ResultCode ?? TestResultCode.Success}");
            return testAssessments;
        }

        /// <summary>
        /// Runs a single test.
        /// </summary>
        /// <param name="testDescription">The test description.</param>
        /// <param name="testInstances">The test instances.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A list of <see cref="TestResult"/>. The last of them indicates overall success. Multiple assessments will give details</returns>
        private IEnumerable<TestResult> Test(TestDescription testDescription, TestInstances testInstances, TestParameters parameters)
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
            TestResult[] testResults = null;
            var thread = new Thread(delegate () { testResults = Test(testDescription, testInstance).ToArray(); });
            thread.Start();
            // wait for test
            if (thread.Join(parameters.Timeout)) // test ended in time
            {
                foreach (var testAssessment in testResults)
                    yield return testAssessment;
                yield break;
            }

            // in case it took too long, say it
            thread.Abort();
            yield return new TestResult(TestStep.Test, TestResultCode.Timeout, null);
        }

        private static IEnumerable<TestResult> Test(TestDescription testDescription, TestInstance testInstance)
        {
            using (new ConsoleCapture())
            {
                // if initializer fails, no need to run the test
                var testAssessment = TestResult.Get(testDescription.TestInitialize, TestStep.TestInitialize, testInstance.Instance, testInstance.Context)
                                     ?? TestResult.Get(testDescription.TestMethod, TestStep.Test, testInstance.Instance, testInstance.Context)
                                     ?? TestResult.TestSuccess;
                yield return testAssessment;
                var cleanupTestAssessment = TestResult.Get(testDescription.TestCleanup, TestStep.TestCleanup, testInstance.Instance, testInstance.Context);
                if (cleanupTestAssessment != null)
                    yield return cleanupTestAssessment;
            }
        }
    }
}
