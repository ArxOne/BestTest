// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Aspect
{
    using System;
    using System.Reflection;
    using ArxOne.MrAdvice.Advice;
    using ArxOne.MrAdvice.Introduction;
    using Test;

    [AttributeUsage(AttributeTargets.Property)]
    public class SerializedMethodInfo : Attribute, IPropertyAdvice
    {
        public void Advise(PropertyAdviceContext context)
        {
            var target = (TestDescription)context.Target;
            if (context.IsSetter)
                target.Methods[context.TargetProperty.Name] = ((MethodInfo)context.Value)?.Name;
            if (context.IsGetter)
            {
                var methodName = target.Methods[context.TargetProperty.Name];
                if (methodName != null)
                    context.ReturnValue = target.GetMethod(methodName);
            }
        }
    }
}