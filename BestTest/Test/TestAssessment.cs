// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Xml;

    public class TestAssessment
    {
        public TestStep Step;
        public TestResult Result;
        public string ResultMessage;
        public string Exception;

        public static readonly TestAssessment Success = new TestAssessment { Result = TestResult.Success };

        public TestAssessment() { }

        public TestAssessment(TestStep step, TestResult result, Exception e)
        {
            Step = step;
            Result = result;
            ResultMessage = e.Message;
            Exception = e.ToString();
        }
    }
}
