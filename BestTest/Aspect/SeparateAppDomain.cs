// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Aspect
{
    using System;
    using System.Diagnostics;
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
            var appDomain = AppDomain.CreateDomain($"{methodDeclaringType.FullName}.{context.TargetMethod.Name}(...)");
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
                AppDomain.Unload(appDomain);
            }
        }
    }
}
