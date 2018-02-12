// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Aspect
{
    using System;
    using System.CodeDom;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ArxOne.MrAdvice.Advice;
    using ArxOne.MrAdvice.Annotation;
    using ArxOne.MrAdvice.Introduction;
    using Reflection;
    using Test;

    [AttributeUsage(AttributeTargets.Property)]
    [AbstractTarget]
    public class SerializedMethodInfo : Attribute, IPropertyAdvice
    {
        [Serializable]
        public class MethodDescriptor
        {
            public string AssemblyPath;
            public string AssemblyName;
            public string TypeName;
            public string MethodName;
        }

        [NonSerialized]
        // ReSharper disable once UnassignedField.Global
        public IntroducedField<MethodInfo> MethodInfo;

        // ReSharper disable once UnassignedField.Global
        public IntroducedField<MethodDescriptor> Descriptor;

        [DebuggerStepThrough]
        public void Advise(PropertyAdviceContext context)
        {
            if (context.IsSetter)
            {
                var methodInfo = (MethodInfo)context.Value;
                Descriptor[context] = new MethodDescriptor
                {
                    MethodName = methodInfo?.Name,
                    TypeName = methodInfo?.DeclaringType.FullName,
                    AssemblyName = methodInfo?.DeclaringType.Assembly.FullName,
                    AssemblyPath = methodInfo?.DeclaringType.Assembly.Location
                };
                MethodInfo[context] = methodInfo;
            }
            if (context.IsGetter)
            {
                // fast path: load from field
                var methodInfo = MethodInfo[context];
                if (methodInfo != null)
                {
                    context.ReturnValue = methodInfo;
                    return;
                }
                // now try to find it
                var methodDescriptor = Descriptor[context];
                // no name? No method
                if (methodDescriptor?.MethodName == null)
                {
                    context.ReturnValue = null;
                    return;
                }

                methodInfo = GetMethod(methodDescriptor);
                MethodInfo[context] = methodInfo;
                context.ReturnValue = methodInfo;
            }
        }

        /// <summary>
        /// Gets the method.
        /// Since we may have crossed appdomains, it needs to be retrieved
        /// </summary>
        /// <param name="methodDescriptor">The method descriptor.</param>
        /// <returns></returns>
        private static MethodInfo GetMethod(MethodDescriptor methodDescriptor)
        {
            var method = GetType(methodDescriptor).GetMethod(methodDescriptor.MethodName);
            return method;
        }

        private static Type GetType(MethodDescriptor methodDescriptor)
        {
            // first, get the assembly
            var assembly = GetAssembly(methodDescriptor) ?? LoadAssembly(methodDescriptor);
            // then ask for type
            var type = assembly.GetType(methodDescriptor.TypeName);
            return type;
        }

        private static Assembly GetAssembly(MethodDescriptor methodDescriptor)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == methodDescriptor.AssemblyName);
        }

        private static Assembly LoadAssembly(MethodDescriptor methodDescriptor)
        {
            using (new ConfigFileContext(methodDescriptor.AssemblyPath))
                return Assembly.LoadFrom(methodDescriptor.AssemblyPath);
        }
    }
}
