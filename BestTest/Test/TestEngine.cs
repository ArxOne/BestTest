// BestTest: test better than using MSTest
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
    using Reflection;

    public class TestEngine : MarshalByRefObject
    {
        private static readonly object[] NoParameter = new object[0];

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
                MethodInfo classInitialize = null, testInitialize = null, testCleanup = null, classCleanup = null;
                foreach (var method in testType.GetMethods().Where(IsTestMethod))
                {
                    if (!method.IsValidTestMethod())
                        continue;
                    if (method.HasAnyAttribute("ClassInitialize"))
                        classInitialize = method;
                    else if (method.HasAnyAttribute("ClassCleanup"))
                        classCleanup = method;
                    else if (method.HasAnyAttribute("TestInitialize"))
                        testInitialize = method;
                    else if (method.HasAnyAttribute("TestCleanup"))
                        testCleanup = method;
                }

                foreach (var testMethod in testType.GetMethods().Where(IsTestMethod))
                    yield return new TestDescription(assembly.Location, testMethod, classInitialize, classCleanup, testInitialize, testCleanup);
            }
        }

        private static bool IsTestClass(Type type) => type.IsValidTestType() && type.HasAnyAttribute("TestClass", "TestFixture");
        private static bool IsTestMethod(MethodInfo method) => method.IsValidTestMethod() && method.HasAnyAttribute("TestMethod", "Test");

        [SeparateAppDomain]
        public void Test(TestParameters parameters)
        {
            var tests = EnumerateTests(parameters);
            var instances = new Dictionary<Type, TestInstance>();
            var assessments = new List<TestAssessment>();
            foreach (var test in tests)
                assessments.Add(Test(test, instances));
            Cleanup(instances);
        }

        private void Cleanup(IDictionary<Type, TestInstance> testInstances)
        {
            foreach (var testInstance in testInstances.Values)
                Invoke(testInstance.Instance, testInstance.ClassCleanup, TestStep.ClassCleanup);
        }

        public TestAssessment Test(TestDescription testDescription, IDictionary<Type, TestInstance> testInstances)
        {
            var testClass = testDescription.TestMethod.DeclaringType;
            if (!testInstances.TryGetValue(testClass, out var testInstance))
            {
                var instance = Activator.CreateInstance(testClass);
                testInstance = new TestInstance { Instance = instance, ClassCleanup = testDescription.ClassCleanup };
                testInstance.Assessment = Invoke(testInstance.Instance, testDescription.ClassInitialize, TestStep.ClassInitialize);
                testInstances[testClass] = testInstance;
            }

            var testAssessment = testInstance.Assessment
                                 ?? Invoke(testInstance.Instance, testDescription.TestInitialize, TestStep.TestInitialize)
                                 ?? Invoke(testInstance.Instance, testDescription.TestMethod, TestStep.Test)
                                 ?? Invoke(testInstance.Instance, testDescription.TestCleanup, TestStep.TestCleanup)
                                 ?? TestAssessment.Success;

            return testAssessment;
        }

        private static TestAssessment Invoke(object instance, MethodInfo method, TestStep step)
        {
            if (method == null)
                return null;
            try
            {
                method.Invoke(instance, NoParameter);
                return null;
            }
            catch (TargetInvocationException e) when (e.InnerException.GetType().Name == "AssertInconclusiveException")
            {
                return new TestAssessment(step, TestResult.Inconclusive, e.InnerException);
            }
            catch (TargetInvocationException e) when (e.InnerException.GetType().Name == "AssertFailedException")
            {
                return new TestAssessment(step, TestResult.Failure, e.InnerException);
            }
            catch (TargetInvocationException e)
            {
                if (GetExpectedTypes(method).Any(expectedType => expectedType == e.InnerException.GetType()))
                    return null;
                return new TestAssessment(step, TestResult.Failure, e.InnerException);
            }
        }

        private static IEnumerable<Type> GetExpectedTypes(MethodInfo methodInfo)
        {
            var expectedExceptionAttributes = methodInfo.GetCustomAttributes().Where(a => a.GetType().Name == "ExpectedExceptionAttribute");
            foreach (var expectedExceptionAttribute in expectedExceptionAttributes)
            {
                var exceptionTypeMember = expectedExceptionAttribute.GetType().GetProperty("ExceptionType");
                if (exceptionTypeMember != null)
                {
                    var expectedType = (Type)exceptionTypeMember.GetValue(expectedExceptionAttribute);
                    yield return expectedType;
                }
            }
        }
    }
}