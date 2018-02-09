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

        private readonly IList<Tuple<object, MethodInfo>> _classCleanup = new List<Tuple<object, MethodInfo>>();
        private readonly IList<MethodInfo> _assemblyCleanup = new List<MethodInfo>();

        public TestAssessment[] Cleanup() => DoCleanup().Where(a => a != null).ToArray();

        private IEnumerable<TestAssessment> DoCleanup()
        {
            foreach (var cleanup in _classCleanup)
                yield return TestAssessment.Invoke(cleanup.Item2, TestStep.ClassCleanup, cleanup.Item1);
            foreach (var cleanup in _assemblyCleanup)
                yield return TestAssessment.Invoke(cleanup, TestStep.AssemblyCleanup, null);
        }

        public TestInstance Get(TestDescription testDescription, out TestAssessment failure)
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
                    testInstance.AssemblyInitializeFailure = failure = TestAssessment.Invoke(testDescription.AssemblyInitialize, TestStep.AssemblyInitialize, null);
                    if (failure != null)
                        return null;
                    // cleanup only once setup has succeeded
                    _assemblyCleanup.Add(testDescription.AssemblyCleanup);
                }

                // and initialize the type
                testInstance.ClassInitializeFailure = failure = TestAssessment.Invoke(() => testInstance.Instance = Activator.CreateInstance(testClass), TestStep.ClassInitialize)
                    ?? TestAssessment.Invoke(testDescription.ClassInitialize, TestStep.ClassInitialize, testInstance.Instance);
                if (failure != null)
                    return null;

                // cleanup if initialized
                _classCleanup.Add(Tuple.Create(testInstance.Instance, testDescription.ClassCleanup));

                return testInstance;
            }
        }
    }
}
