// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
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
                if (attributePrefixes.Contains(name))
                    return true;
            }

            return false;
        }

        public static IEnumerable<Attribute> GetAnyAttribute(this ICustomAttributeProvider methodInfo, params string[] attributePrefixes)
        {
            var attributes = methodInfo.GetCustomAttributes(false);
            foreach (var attribute in attributes.OfType<Attribute>())
            {
                var name = attribute.GetType().Name;
                if (attributePrefixes.Contains(name))
                    yield return attribute;
            }
        }

        public static object GetMemberValue(this object instance, string memberName)
        {
            var staticProperty = instance.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Static);
            if (staticProperty != null)
                return staticProperty.GetValue(null);
            var instanceProperty = instance.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance);
            if (instanceProperty != null)
                return instanceProperty.GetValue(instance);
            var staticField = instance.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static);
            if (staticField != null)
                return staticField.GetValue(null);
            var instanceField = instance.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            if (instanceField != null)
                return instanceField.GetValue(null);
            return null;
        }
    }
}
