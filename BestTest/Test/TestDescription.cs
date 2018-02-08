// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Reflection;

    [Serializable]
    internal class TestDescription
    {
        public string TypeName { get; set; }

        public string MethodName { get; set; }

        [Obsolete("Serialization-only ctor")]
        public TestDescription()
        { }

        public TestDescription(MethodInfo methodInfo)
        {
            TypeName = methodInfo.DeclaringType.AssemblyQualifiedName;
            MethodName = methodInfo.Name;
        }
    }
}