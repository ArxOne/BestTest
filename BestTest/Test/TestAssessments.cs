﻿// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System.Collections.Generic;
    using System.Linq;

    public class TestAssessments
    {
        public TestDescription Description { get; }
        public IList<TestAssessment> Assessments { get; }
        public TestResult Result => Assessments.Last().Result;
        public TestAssessment TestStepAssessment => Assessments.SingleOrDefault(a => a.Step == TestStep.Test);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAssessments"/> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="assessments">The assessments.</param>
        public TestAssessments(TestDescription description, IEnumerable<TestAssessment> assessments)
        {
            Description = description;
            Assessments = assessments.ToArray();
        }
    }
}