// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [Serializable]
    public class StepResult
    {
        private static readonly object[] NoParameter = new object[0];

        public TestStep Step { get; }
        public ResultCode ResultCode { get; }
        public string ResultMessage { get; }
        public string Exception { get; }

        public static readonly StepResult TestSuccess = new StepResult(TestStep.Test, ResultCode.Success, null, null);

        [Obsolete("Serialization-only ctor")]
        public StepResult() { }

        public StepResult(TestStep step, ResultCode resultCode, Exception e, string output)
        {
            Step = step;
            ResultCode = resultCode;
            ResultMessage = e?.Message;
            Exception = e?.ToString();
        }

        private static StepResult Invoke(Action action, TestStep step, ICustomAttributeProvider expectedExceptionsAttributeProvider)
        {
            if (action == null)
                return null;
            using (var consoleCapture = new ConsoleCapture())
            {
                try
                {
                    action();
                    return null;
                }
                catch (TargetInvocationException e) when (step == TestStep.Test &&
                                                          (e.InnerException.GetType().Name == "AssertInconclusiveException" ||
                                                           e.InnerException.GetType().Name == "InconclusiveException"))
                {
                    return new StepResult(step, ResultCode.Inconclusive, e.InnerException, consoleCapture.Capture);
                }
                catch (TargetInvocationException e) when (step == TestStep.Test &&
                                                          (e.InnerException.GetType().Name == "AssertFailedException" || e.InnerException.GetType().Name == "FailedException"))
                {
                    return new StepResult(step, ResultCode.Failure, e.InnerException, consoleCapture.Capture);
                }
                catch (TargetInvocationException e)
                {
                    if (step == TestStep.Test && GetExpectedExceptionTypes(expectedExceptionsAttributeProvider)
                            .Any(expectedType => expectedType.IsInstanceOfType(e.InnerException)))
                        return null;
                    return new StepResult(step, ResultCode.Failure, e.InnerException, consoleCapture.Capture);
                }
            }
        }

        /// <summary>
        /// Invokes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="step">The step.</param>
        /// <returns>An assessment on failure, null on success</returns>
        public static StepResult Get(Action action, TestStep step) => Invoke(action, step, null);

        /// <summary>
        /// Invokes the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="step">The step.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// An assessment on failure, null on success
        /// </returns>
        public static StepResult Get(MethodInfo method, TestStep step, object instance, object parameter = null)
        {
            if (method == null)
                return null;
            return Invoke(delegate
            {
                if (method.IsStatic)
                    instance = null;
                var parameterInfos = method.GetParameters();
                if (parameterInfos.Length == 1)
                {
                    var testContext = TestContextBuilder.Get(parameterInfos[0].ParameterType);
                    method.Invoke(instance, new[] { testContext });
                }
                else
                    method.Invoke(instance, NoParameter);
            }, step, method);
        }

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
