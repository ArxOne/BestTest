// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Utility
{
    using System;
    using System.Threading;

    public static class ThreadUtility
    {
        /// <summary>
        /// Starts the specified action in a new thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static Thread Start(ThreadStart action)
        {
            var thread = new Thread(action);
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Starts the specified action in a new thread.
        /// </summary>
        /// <typeparam name="TArg1">The type of the arg1.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="arg1">The arg1.</param>
        /// <returns></returns>
        public static Thread Start<TArg1>(Action<TArg1> action, TArg1 arg1)
        {
            var thread = new Thread(delegate () { action(arg1); });
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Starts the specified action in a new thread.
        /// </summary>
        /// <typeparam name="TArg1">The type of the arg1.</typeparam>
        /// <typeparam name="TArg2">The type of the arg2.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="arg1">The arg1.</param>
        /// <param name="arg2">The arg2.</param>
        /// <returns></returns>
        public static Thread Start<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var thread = new Thread(delegate () { action(arg1, arg2); });
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Starts the specified action in a new thread.
        /// </summary>
        /// <typeparam name="TArg1">The type of the arg1.</typeparam>
        /// <typeparam name="TArg2">The type of the arg2.</typeparam>
        /// <typeparam name="TArg3">The type of the arg3.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="arg1">The arg1.</param>
        /// <param name="arg2">The arg2.</param>
        /// <param name="arg3">The arg3.</param>
        /// <returns></returns>
        public static Thread Start<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var thread = new Thread(delegate () { action(arg1, arg2, arg3); });
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Starts the specified action in a new thread.
        /// </summary>
        /// <typeparam name="TArg1">The type of the arg1.</typeparam>
        /// <typeparam name="TArg2">The type of the arg2.</typeparam>
        /// <typeparam name="TArg3">The type of the arg3.</typeparam>
        /// <typeparam name="TArg4">The type of the arg4.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="arg1">The arg1.</param>
        /// <param name="arg2">The arg2.</param>
        /// <param name="arg3">The arg3.</param>
        /// <param name="arg4">The arg4.</param>
        /// <returns></returns>
        public static Thread Start<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var thread = new Thread(delegate () { action(arg1, arg2, arg3, arg4); });
            thread.Start();
            return thread;
        }
    }
}