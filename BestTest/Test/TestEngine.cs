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
            var t = EnumerateTests(testParameters);
            return 0;
        }

        [SeparateAppDomain]
        private TestDescription[] EnumerateTests(TestParameters testParameters)
        {
            var allTests = new List<TestDescription>();
            foreach (var assemblyPathMask in testParameters.AssemblyPaths)
            {
                foreach (var assemblyPath in Directory.GetFiles(Path.GetDirectoryName(assemblyPathMask) ?? ".", Path.GetFileName(assemblyPathMask)))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(assemblyPath);
                        allTests.AddRange(EnumerateTests(assembly));
                    }
                    catch { }
                }
            }

            return allTests.ToArray();
        }

        private IEnumerable<TestDescription> EnumerateTests(Assembly assembly)
        {
            foreach (var testType in assembly.GetTypes().Where(IsTestClass))
                foreach (var testMethod in testType.GetMethods().Where(IsTestMethod))
                    yield return new TestDescription(testMethod);
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
            var testAttributes = method.GetCustomAttributes();
            return testAttributes.Any(a => a.GetType().Name == "TestMethodAttribute" || a.GetType().Name == "TestAttribute");
        }
    }
}