// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;

    [Serializable]
    public class StepResults
    {
        public StepResult[] Results { get; set; }

        public string Output { get; set; }
    }
}