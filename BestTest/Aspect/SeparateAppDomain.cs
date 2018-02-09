// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Aspect
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using ArxOne.MrAdvice.Advice;

    /// <summary>
    /// Methods marked with this attribe are run in a separate <see cref="AppDomain"/>
    /// </summary>
    /// <seealso cref="System.Attribute" />
    /// <seealso cref="ArxOne.MrAdvice.Advice.IMethodAdvice" />
    public class SeparateAppDomain : Attribute, IMethodAdvice
    {
        [DebuggerStepThrough]
        public void Advise(MethodAdviceContext context)
        {
            var methodDeclaringType = context.TargetMethod.DeclaringType;
            var assemblyLocation = methodDeclaringType.Assembly.Location;
            var basePath = string.IsNullOrEmpty(assemblyLocation) ? null : Path.GetDirectoryName(assemblyLocation);
            var appDomain = AppDomain.CreateDomain($"{methodDeclaringType.FullName}.{context.TargetMethod.Name}(...)", null, basePath, null, false);
            try
            {
                if (!methodDeclaringType.IsSubclassOf(typeof(MarshalByRefObject)))
                    throw new InvalidOperationException();
                // Simple magic here
                context.Target = (MarshalByRefObject)appDomain.CreateInstanceAndUnwrap(methodDeclaringType.Assembly.FullName, methodDeclaringType.FullName);
                context.Proceed();
            }
            finally
            {
                var unloadThread = new Thread(delegate ()
                {
                    try
                    {
                        AppDomain.Unload(appDomain);
                    }
                    catch
                    {
                    }
                });
                unloadThread.Start();
            }
        }
    }
}
