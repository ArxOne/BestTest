// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Aspect;

    [Serializable]
    [DebuggerDisplay("{" + nameof(DebugLiteral) + "}")]
    public class TestDescription
    {
        public string AssemblyPath { get; }

        public string AssemblyName { get; }

        public string TypeName { get; }

        public string MethodName { get; }

        private string DebugLiteral => $"{TypeName}.{MethodName}";

        [SerializedMethodInfo] public MethodInfo TestMethod { get; private set; }
        [SerializedMethodInfo] public MethodInfo ClassInitialize { get; private set; }
        [SerializedMethodInfo] public MethodInfo ClassCleanup { get; private set; }
        [SerializedMethodInfo] public MethodInfo TestInitialize { get; private set; }
        [SerializedMethodInfo] public MethodInfo TestCleanup { get; private set; }

        [Obsolete("Serialization-only ctor")]
        public TestDescription()
        { }

        public TestDescription(string assemblyPath, MethodInfo testMethod, MethodInfo classInitialize, MethodInfo classCleanup, MethodInfo testInitialize, MethodInfo testCleanup)
        {
            AssemblyPath = assemblyPath;
            TestMethod = testMethod;
            ClassInitialize = classInitialize;
            ClassCleanup = classCleanup;
            TestInitialize = testInitialize;
            TestCleanup = testCleanup;
            AssemblyName = testMethod.DeclaringType.Assembly.FullName;
            TypeName = testMethod.DeclaringType.FullName;
            MethodName = testMethod.Name;
        }
    }
}
