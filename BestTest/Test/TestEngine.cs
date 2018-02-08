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
            var d = EnumerateTests(parameters);
            Test(d.Last());
            Test(d.First());
        }

        [SeparateAppDomain]
        public void Test(TestDescription testDescription)
        {
            var method = testDescription.TestMethod;
        }
    }
}
