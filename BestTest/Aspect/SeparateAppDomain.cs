// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Aspect
{
    using System;
    using ArxOne.MrAdvice.Advice;

    /// <summary>
    /// Methods marked with this attribe are run in a separate <see cref="AppDomain"/>
    /// </summary>
    /// <seealso cref="System.Attribute" />
    /// <seealso cref="ArxOne.MrAdvice.Advice.IMethodAdvice" />
    public class SeparateAppDomain : Attribute, IMethodAdvice
    {
        public void Advise(MethodAdviceContext context)
        {
            var appDomain = AppDomain.CreateDomain(context.TargetMethod.Name);
            try
            {
                var methodDeclaringType = context.TargetMethod.DeclaringType;
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
