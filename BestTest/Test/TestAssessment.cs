// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    public class TestAssessment
    {
        private static readonly object[] NoParameter = new object[0];

        public TestStep Step;
        public TestResult Result;
        public string ResultMessage;
        public string Exception;

        public static readonly TestAssessment TestSuccess = new TestAssessment { Step = TestStep.Test, Result = TestResult.Success };

        public TestAssessment() { }

        public TestAssessment(TestStep step, TestResult result, Exception e)
        {
            Step = step;
            Result = result;
            ResultMessage = e?.Message;
            Exception = e?.ToString();
        }

        private static TestAssessment Invoke(Action action, TestStep step, ICustomAttributeProvider expectedExceptionsAttributeProvider)
        {
            if (action == null)
                return null;
            try
            {
                action();
                return null;
            }
            catch (TargetInvocationException e) when (step == TestStep.Test && e.InnerException.GetType().Name == "AssertInconclusiveException")
            {
                return new TestAssessment(step, TestResult.Inconclusive, e.InnerException);
            }
            catch (TargetInvocationException e) when (step == TestStep.Test && e.InnerException.GetType().Name == "AssertFailedException")
            {
                return new TestAssessment(step, TestResult.Failure, e.InnerException);
            }
            catch (TargetInvocationException e)
            {
                if (step == TestStep.Test && GetExpectedExceptionTypes(expectedExceptionsAttributeProvider).Any(expectedType => expectedType == e.InnerException.GetType()))
                    return null;
                return new TestAssessment(step, TestResult.Failure, e.InnerException);
            }
        }

        /// <summary>
        /// Invokes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="step">The step.</param>
        /// <returns>An assessment on failure, null on success</returns>
        public static TestAssessment Invoke(Action action, TestStep step) => Invoke(action, step, null);

        /// <summary>
        /// Invokes the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="step">The step.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>An assessment on failure, null on success</returns>
        public static TestAssessment Invoke(MethodInfo method, TestStep step, object instance) => Invoke(() => method?.Invoke(instance, NoParameter), step, method);

        private static IEnumerable<Type> GetExpectedExceptionTypes(ICustomAttributeProvider attributeProvider)
        {
            if (attributeProvider == null)
                yield break;
            var expectedExceptionAttributes = attributeProvider.GetCustomAttributes(false).Where(a => a.GetType().Name == "ExpectedExceptionAttribute");
            foreach (var expectedExceptionAttribute in expectedExceptionAttributes)
            {
                var exceptionTypeMember = expectedExceptionAttribute.GetType().GetProperty("ExceptionType");
                if (exceptionTypeMember != null)
                {
                    var expectedType = (Type)exceptionTypeMember.GetValue(expectedExceptionAttribute);
                    yield return expectedType;
                }
            }
        }
    }
}
