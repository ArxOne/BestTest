// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public interface ITestContext { }

    public static class TestContextBuilder
    {
        private static readonly IDictionary<Type, ITestContext> _contexts = new Dictionary<Type, ITestContext>();

        public static ITestContext Get(Type testContextType)
        {
            lock (_contexts)
            {
                if (_contexts.TryGetValue(testContextType, out var c))
                    return c;
                _contexts[testContextType] = c = Create(testContextType);
                return c;
            }
        }

        private static ITestContext Create(Type testContextType)
        {
            var assemblyName = "TestContext." + testContextType.FullName;
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);
            var typeName = "BestTestContext";
            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, testContextType);
            typeBuilder.AddInterfaceImplementation(typeof(ITestContext));
            foreach (var method in testContextType.GetMethods().Where(m => m.IsAbstract))
                GenerateEmptyMethod(typeBuilder, method);
            //foreach (var property in testContextType.GetProperties())
            //{
            //    var getMethod = property.GetMethod;
            //    if (getMethod?.IsAbstract ?? false)
            //        GenerateEmptyMethod(typeBuilder, getMethod);
            //    var setMethod = property.SetMethod;
            //    if (setMethod?.IsAbstract ?? false)
            //        GenerateEmptyMethod(typeBuilder, setMethod);
            //}

            var t = typeBuilder.CreateType();
            return (ITestContext)Activator.CreateInstance(t);
        }

        private static void GenerateEmptyMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var implementationMethod = typeBuilder.DefineMethod(method.Name, (method.Attributes | MethodAttributes.HideBySig | MethodAttributes.Virtual) & ~MethodAttributes.Abstract,
                 method.CallingConvention, method.ReturnType, method.GetParameters().Select(p => p.ParameterType).ToArray());
            var generator = implementationMethod.GetILGenerator();
            if (implementationMethod.ReturnType != typeof(void))
                generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(implementationMethod, method);
        }
    }
}