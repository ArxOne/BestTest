// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Linq;
    using System.Reflection;

    [Serializable]
    public class TestDescription
    {
        public string AssemblyPath { get; }

        public string AssemblyName { get; }

        public string TypeName { get; }

        public string MethodName { get; }

        [NonSerialized] private MethodInfo _method;

        public MethodInfo Method
        {
            get
            {
                if (_method == null)
                    _method = GetMethod();
                return _method;
            }
        }

        [Obsolete("Serialization-only ctor")]
        public TestDescription()
        { }

        public TestDescription(string assemblyPath, MethodInfo method)
        {
            AssemblyPath = assemblyPath;
            _method = method;
            AssemblyName = method.DeclaringType.Assembly.FullName;
            TypeName = method.DeclaringType.FullName;
            MethodName = method.Name;
        }

        /// <summary>
        /// Gets the method.
        /// Since we may have crossed appdomains, it needs to be retrieved
        /// </summary>
        /// <returns></returns>
        private MethodInfo GetMethod()
        {
            // a public test method has 0 parameters, remember?
            var method = GetType().GetMethod(MethodName, new Type[0]);
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
