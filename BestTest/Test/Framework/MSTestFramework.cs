// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Reflection;

    [Serializable]
    public class MSTestFramework : ITestFramework
    {
        public bool IsTestClass(Type type)
        {
            return type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsInterface
                   && type.HasAnyAttribute("TestClassAttribute");
        }

        private bool IsTestMethodBase(MethodInfo methodInfo)
        {
            return methodInfo.IsPublic && !methodInfo.IsGenericMethod && !methodInfo.IsAbstract && methodInfo.GetParameters().Length <= 1;
        }

        public bool IsAssemblySetupMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && methodInfo.IsStatic
                                                && methodInfo.HasAnyAttribute("AssemblyInitializeAttribute");
        }

        public bool IsTypeSetupMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && methodInfo.HasAnyAttribute("ClassInitializeAttribute");
        }

        public bool IsTestSetupMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && !methodInfo.IsStatic
                                                && methodInfo.HasAnyAttribute("TestInitializeAttribute");
        }

        public bool IsTestMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && !methodInfo.IsStatic
                                                && methodInfo.HasAnyAttribute("TestMethodAttribute");
        }

        public bool IsTestCleanupMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && !methodInfo.IsStatic
                                                && methodInfo.HasAnyAttribute("TestCleanupAttribute");
        }

        public bool IsTypeCleanupMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && methodInfo.HasAnyAttribute("ClassCleanupAttribute");
        }

        public bool IsAssemblyCleanupMethod(MethodInfo methodInfo)
        {
            return IsTestMethodBase(methodInfo) && methodInfo.IsStatic
                                                && methodInfo.HasAnyAttribute("AssemblyCleanupAttribute");
        }

        public bool IsInconclusive(Exception exception)
        {
            return exception.GetType().Name.Contains("Inconclusive");
        }

        public IEnumerable<Type> GetExpectedExceptions(MethodInfo methodInfo)
        {
            return methodInfo.GetAnyAttribute("ExpectedExceptionAttribute")
                .Select(a => a.GetMemberValue("ExceptionType"))
                .OfType<Type>();
        }

        public string GetDescription(MethodInfo methodInfo)
        {
            var descriptionAttributes = methodInfo.GetAnyAttribute("DescriptionAttribute");
            var descriptions = descriptionAttributes.Select(a => a.GetMemberValue("Description")).Where(d => d != null).ToArray();
            if (descriptions.Length == 0)
                return null;
            return string.Join(" ", descriptions);
        }

        public IEnumerable<string> GetCategories(MethodInfo methodInfo)
        {
            var categoryAttributes = methodInfo.GetAnyAttribute("TestCategoryAttribute");
            foreach (var categoryAttribute in categoryAttributes)
            {
                if (categoryAttribute.GetMemberValue("TestCategories") is IEnumerable<string> categories)
                {
                    foreach (var category in categories)
                        yield return category;
                }
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetProperties(MethodInfo methodInfo)
        {
            var propertyAttributes = methodInfo.GetAnyAttribute("TestPropertyAttribute");
            foreach (var propertyAttribute in propertyAttributes)
            {
                if (propertyAttribute.GetMemberValue("Name") is string name)
                {
                    var value = propertyAttribute.GetMemberValue("Value") as string;
                    yield return new KeyValuePair<string, string>(name, value);
                }
            }
        }
    }
}
