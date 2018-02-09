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
            var tests = EnumerateTests(parameters);
            var testSet = new TestSet(tests);
            var runners = CreateRunners(testSet, parameters);
            Await(runners);
        }

        private IEnumerable<Thread> CreateRunners(TestSet testSet, TestParameters parameters)
        {
            for (int runnerIndex = 0; runnerIndex < parameters.ParallelRuns; runnerIndex++)
            {
                var thread = new Thread(delegate () { ParallelRunner(testSet, parameters); });
                thread.Start();
                yield return thread;
            }
        }

        private void Await(IEnumerable<Thread> threads)
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
        private void ParallelRunner(TestSet testSet, TestParameters parameters)
        {
            var testInstances = new TestInstances();
            for (; ; )
            {
                var testDescription = testSet.PullNextTest();
                if (testDescription == null)
                    break;

                var assessment = Test(testDescription, testInstances, parameters);
                var testAssessments = new TestAssessments(testDescription, assessment);
                Trace(testAssessments);
                testSet.PushAssessment(testAssessments);
            }
        }

        private static TestAssessments Trace(TestAssessments testAssessments)
        {
            var methodName = testAssessments.Description.MethodName;
            const int totalWidth = 60;
            var testAssessment = testAssessments.TestStepAssessment;
            methodName = methodName.Substring(0, Math.Min(methodName.Length, totalWidth)).PadRight(totalWidth);
            Console.WriteLine($"{methodName}: {testAssessment?.ResultCode ?? TestResultCode.Success} ({testAssessment?.Exception})");
            return testAssessments;
        }

        /// <summary>
        /// Runs a single test.
        /// </summary>
        /// <param name="testDescription">The test description.</param>
        /// <param name="testInstances">The test instances.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A list of <see cref="TestAssessment"/>. The last of them indicates overall success. Multiple assessments will give details</returns>
        private IEnumerable<TestAssessment> Test(TestDescription testDescription, TestInstances testInstances, TestParameters parameters)
        {
            // initialize test
            var testInstance = testInstances.Get(testDescription, out var initializationFailureTestAssessment);
            if (initializationFailureTestAssessment != null)
                yield return initializationFailureTestAssessment;

            // run it
            TestAssessment[] testAssessments = null;
            var thread = new Thread(delegate () { testAssessments = Test(testDescription, testInstance).ToArray(); });
            thread.Start();
            // wait for test
            if (thread.Join(parameters.Timeout)) // test ended in time
            {
                foreach (var testAssessment in testAssessments)
                    yield return testAssessment;
                yield break;
            }

            // in case it took too long, say it
            thread.Abort();
            yield return new TestAssessment(TestStep.Test, TestResultCode.Timeout, null);
        }

        private static IEnumerable<TestAssessment> Test(TestDescription testDescription, TestInstance testInstance)
        {
            // if initializer fails, no need to run the test
            var testAssessment = TestAssessment.Invoke(testDescription.TestInitialize, TestStep.TestInitialize, testInstance.Instance, testInstance.Context)
                                 ?? TestAssessment.Invoke(testDescription.TestMethod, TestStep.Test, testInstance.Instance, testInstance.Context)
                                 ?? TestAssessment.TestSuccess;
            yield return testAssessment;
            var cleanupTestAssessment = TestAssessment.Invoke(testDescription.TestCleanup, TestStep.TestCleanup, testInstance.Instance, testInstance.Context);
            if (cleanupTestAssessment != null)
                yield return cleanupTestAssessment;
        }
    }
}