// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface ITestFramework
    {
        /// <summary>
        /// Determines whether the given type is a test class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// </returns>
        bool IsTestClass(Type type);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is an assembly-level setup method (called only once per tested assembly).
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsAssemblySetupMethod(MethodInfo methodInfo);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is an class-level setup method (called only once per tested class).
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsTypeSetupMethod(MethodInfo methodInfo);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is an test-level setup method (called every time before a test is being made).
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsTestSetupMethod(MethodInfo methodInfo);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is a test method.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsTestMethod(MethodInfo methodInfo);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is an test-level cleanup method (called every time after a test is being made).
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsTestCleanupMethod(MethodInfo methodInfo);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is an class-level cleanup method (called only once per tested class).
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsTypeCleanupMethod(MethodInfo methodInfo);
        /// <summary>
        /// Determines whether the given <see cref="MethodInfo"/> is an assembly-level cleanup method (called only once per tested assembly).
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>
        /// </returns>
        bool IsAssemblyCleanupMethod(MethodInfo methodInfo);

        /// <summary>
        /// Determines whether the specified exception is inconclusive.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>
        ///   <c>true</c> if the specified exception is inconclusive; otherwise, <c>false</c>.
        /// </returns>
        bool IsInconclusive(Exception exception);

        /// <summary>
        /// Gets the expected exceptions.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns></returns>
        IEnumerable<Type> GetExpectedExceptions(MethodInfo methodInfo);

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <returns></returns>
        string GetDescription(MethodInfo methodInfo);
    }
}