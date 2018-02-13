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
        public string Output { get; }
        public string ResultMessage { get; }
        public string Exception { get; }

        public static readonly StepResult TestSuccess = new StepResult(TestStep.Test, ResultCode.Success, null, null);

        [Obsolete("Serialization-only ctor")]
        public StepResult() { }

        public StepResult(TestStep step, ResultCode resultCode, Exception e, string output)
        {
            Step = step;
            ResultCode = resultCode;
            Output = output;
            ResultMessage = e?.Message;
            Exception = e?.ToString();
        }

        private static StepResult Invoke(Action action, TestStep step, MethodInfo expectedExceptionsAttributeProvider, TestParameters parameters)
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
                catch (TargetInvocationException e) when (step == TestStep.Test && parameters.Framework.IsInconclusive(e.InnerException))
                {
                    return new StepResult(step, ResultCode.Inconclusive, e.InnerException, consoleCapture.Capture);
                }
                catch (TargetInvocationException e)
                {
                    if (step == TestStep.Test && expectedExceptionsAttributeProvider != null && parameters.Framework
                            .GetExpectedExceptions(expectedExceptionsAttributeProvider)
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
        /// <param name="parameters"></param>
        /// <returns>An assessment on failure, null on success</returns>
        public static StepResult Get(Action action, TestStep step, TestParameters parameters) => Invoke(action, step, null, parameters);

        /// <summary>
        /// Invokes the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="step">The step.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="parameters"></param>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// An assessment on failure, null on success
        /// </returns>
        public static StepResult Get(MethodInfo method, TestStep step, object instance, TestParameters parameters, object parameter = null)
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
            }, step, method, parameters);
        }
    }
}
