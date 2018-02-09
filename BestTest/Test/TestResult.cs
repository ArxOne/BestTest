// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class TestResult
    {
        public TestDescription Description { get; }
        public IList<StepResult> StepResults { get; }
        public ResultCode ResultCode => StepResults.Last().ResultCode;
        public StepResult TestStepResult => StepResults.SingleOrDefault(a => a.Step == TestStep.Test);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResult"/> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="stepResults">The assessments.</param>
        public TestResult(TestDescription description, IEnumerable<StepResult> stepResults)
        {
            Description = description;
            StepResults = stepResults.ToArray();
        }
    }
}
