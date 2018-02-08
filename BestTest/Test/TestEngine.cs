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

    public class TestEngine : MarshalByRefObject
    {
        public int Run(TestParameters testParameters)
        {
            //var t = LoadTests(testParameters);
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

        private string GetSafeDirectoryName(string path)
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
                foreach (var testMethod in testType.GetMethods().Where(IsTestMethod))
                    yield return new TestDescription(assembly.Location, testMethod);
        }

        private static bool IsTestClass(Type type)
        {
            // a test class is not abstract/interface and public
            if (type.IsAbstract || type.IsInterface || !type.IsClass || !type.IsPublic)
                return false;
            var testAttributes = type.GetCustomAttributes();
            return testAttributes.Any(a => a.GetType().Name == "TestClassAttribute" || a.GetType().Name == "TestFixtureAttribute");
        }

        private static bool IsTestMethod(MethodInfo method)
        {
            // a test method is non-static and public
            if (method.IsStatic || !method.IsPublic)
                return false;
            if (method.GetParameters().Length > 0)
                return false;
            var testAttributes = method.GetCustomAttributes();
            return testAttributes.Any(a => a.GetType().Name == "TestMethodAttribute" || a.GetType().Name == "TestAttribute");
        }

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
            var method = testDescription.Method;
        }
    }
}
