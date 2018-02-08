// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace WorseTest
{
    using System;

    /// <summary>
    /// This class is not recognized by mstests
    /// But recognized by BestTest. So we will use it to test our tester :)
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
    }
}
