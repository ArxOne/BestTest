// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Reflection
{
    using System;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionExtensions
    {
        private const string AttributeSuffix = "Attribute";

        public static bool IsValidTestType(this Type type)
        {
            return type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsInterface;
        }

        public static bool IsValidTestMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsPublic && !methodInfo.IsGenericMethod && !methodInfo.IsAbstract && methodInfo.GetParameters().Length <= 1;
        }

        public static bool HasAnyAttribute(this ICustomAttributeProvider methodInfo, params string[] attributePrefixes)
        {
            var attributes = methodInfo.GetCustomAttributes(false);
            foreach (var attribute in attributes)
            {
                var name = attribute.GetType().Name;
                if (name.EndsWith(AttributeSuffix))
                    name = name.Substring(0, name.Length - AttributeSuffix.Length);
                if (attributePrefixes.Contains(name))
                    return true;
            }

            return false;
        }
    }
}
