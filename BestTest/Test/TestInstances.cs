// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Gathers all test instances
    /// </summary>
    public class TestInstances
    {
        private readonly IDictionary<Type, TestInstance> _testInstances = new Dictionary<Type, TestInstance>();
        private readonly HashSet<string> _assemblies = new HashSet<string>();

        private readonly IList<Tuple<object, MethodInfo, object>> _classCleanup = new List<Tuple<object, MethodInfo, object>>();
        private readonly IList<MethodInfo> _assemblyCleanup = new List<MethodInfo>();

        /// <summary>
        /// Runs all pending cleanup methods.
        /// </summary>
        /// <returns></returns>
        public TestResult[] Cleanup() => DoCleanup().Where(a => a != null).ToArray();

        private IEnumerable<TestResult> DoCleanup()
        {
            foreach (var cleanup in _classCleanup)
                yield return CreateTestsAssessments(cleanup.Item2, StepResult.Get(cleanup.Item2, TestStep.ClassCleanup, cleanup.Item1, cleanup.Item3));
            foreach (var cleanup in _assemblyCleanup)
                yield return CreateTestsAssessments(cleanup, StepResult.Get(cleanup, TestStep.AssemblyCleanup, null));
        }

        private static TestResult CreateTestsAssessments(MethodInfo method, StepResult result)
        {
            if (result == null)
                return null;
            return new TestResult(new TestDescription(method.DeclaringType.Assembly.Location, method, null, null, null, null, null, null), new[] { result }, TimeSpan.Zero);
        }

        public TestInstance Get(TestDescription testDescription, out StepResult failure)
        {
            lock (_testInstances)
            {
                // look for an existing instance
                var testClass = testDescription.TestMethod.DeclaringType;
                if (_testInstances.TryGetValue(testClass, out var testInstance))
                {
                    failure = testInstance.AssemblyInitializeFailure ?? testInstance.ClassInitializeFailure;
                    if (failure != null)
                        return null;
                    return testInstance;
                }

                // create an instance
                var assemblyFullName = testClass.Assembly.FullName;
                testInstance = new TestInstance();
                _testInstances[testClass] = testInstance;

                // initialize the assembly
                var assemblyIsNew = !_assemblies.Contains(assemblyFullName);
                if (assemblyIsNew)
                {
                    _assemblies.Add(assemblyFullName);
                    testInstance.AssemblyInitializeFailure = failure = StepResult.Get(testDescription.AssemblyInitialize, TestStep.AssemblyInitialize, null);
                    if (failure != null)
                        return null;
                    // cleanup only once setup has succeeded
                    _assemblyCleanup.Add(testDescription.AssemblyCleanup);
                }

                // and initialize the type
                testInstance.ClassInitializeFailure = failure =
                    StepResult.Get(() => testInstance.Instance = Activator.CreateInstance(testClass), TestStep.ClassInitialize)
                    ?? StepResult.Get(() => SetTestContext(testInstance.Instance, testInstance.Context), TestStep.ClassInitialize)
                    ?? StepResult.Get(testDescription.ClassInitialize, TestStep.ClassInitialize, testInstance.Instance, testInstance.Context);
                if (failure != null)
                    return null;

                // cleanup if initialized
                _classCleanup.Add(Tuple.Create(testInstance.Instance, testDescription.ClassCleanup, (object)testInstance.Context));

                return testInstance;
            }
        }

        private static void SetTestContext(object instance, ITestContext context)
        {
            var contextPropetyInfo = instance.GetType().GetProperty("TestContext");
            if (contextPropetyInfo == null)
                return;
            var testContext = TestContextBuilder.Get(contextPropetyInfo.PropertyType);
            contextPropetyInfo.SetValue(instance, testContext);
        }
    }
}
