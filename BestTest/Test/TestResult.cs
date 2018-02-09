// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    [Serializable]
    [DebuggerDisplay("{Description.MethodName}: {ResultCode}")]
    public class TestResult
    {
        public TestDescription Description { get; }
        public TimeSpan Duration { get; }
        public IList<StepResult> StepResults { get; }
        public ResultCode ResultCode => StepResults.Last().ResultCode;
        public StepResult TestStepResult => StepResults.SingleOrDefault(a => a.Step == TestStep.Test);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResult" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="stepResults">The assessments.</param>
        /// <param name="duration">The duration.</param>
        public TestResult(TestDescription description, StepResult[] stepResults, TimeSpan duration)
        {
            Description = description;
            Duration = duration;
            StepResults = stepResults;
        }
    }
}
