// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTestTest
{
    using System;
    using System.IO;
    using System.Linq;
    using BestTest.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WorseTest;

    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class RunTest
    {
        [TestMethod]
        public void SimpleMethodsTest()
        {
            var testEngine = new TestEngine();
            var worseTest = typeof(WorseTest.SimpleTest);
            var testParameters = new TestParameters { AssemblyPaths = { worseTest.Assembly.Location }, Timeout = TimeSpan.FromSeconds(5) };
            var results = testEngine.Test(testParameters, new ConsoleWriter(new StringWriter(), ConsoleMode.None));
            Assert.IsTrue(results.Any(r => r.Description.MethodName == nameof(SimpleTest.SucceedingTest) && r.ResultCode == ResultCode.Success));
            Assert.IsTrue(results.Any(r => r.Description.MethodName == nameof(SimpleTest.InconclusiveTest) && r.ResultCode == ResultCode.Inconclusive));
            Assert.IsTrue(results.Any(r => r.Description.MethodName == nameof(SimpleTest.FailingTest) && r.ResultCode == ResultCode.Failure));
            Assert.IsTrue(results.Any(r => r.Description.MethodName == nameof(SimpleTest.TimeoutTest) && r.ResultCode == ResultCode.Timeout));
        }
    }
}