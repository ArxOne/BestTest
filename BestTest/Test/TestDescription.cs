// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;
    using Aspect;

    [Serializable]
    public class TestDescription
    {
        public string AssemblyPath { get; }

        public string AssemblyName { get; }

        public string TypeName { get; }

        public string TestMethodName { get; private set; }

        internal StringDictionary Methods = new StringDictionary();

        [SerializedMethodInfo] public MethodInfo TestMethod { get; set; }
        [SerializedMethodInfo] public MethodInfo ClassInitialize { get; set; }
        [SerializedMethodInfo] public MethodInfo ClassCleanup { get; set; }
        [SerializedMethodInfo] public MethodInfo TestInitialize { get; set; }
        [SerializedMethodInfo] public MethodInfo TestCleanup { get; set; }

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
        }

        /// <summary>
        /// Gets the method.
        /// Since we may have crossed appdomains, it needs to be retrieved
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <returns></returns>
        public MethodInfo GetMethod(string methodName)
        {
            // a public test method has 0 parameters, remember?
            var method = GetType().GetMethod(methodName, new Type[0]);
            return method;
        }

        private new Type GetType()
        {
            // first, get the assembly
            var assembly = GetAssembly() ?? LoadAssembly();
            // then ask for type
            var type = assembly.GetType(TypeName);
            return type;
        }

        private Assembly GetAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == AssemblyName);
        }

        private Assembly LoadAssembly()
        {
            return Assembly.LoadFrom(AssemblyPath);
        }
    }
}
