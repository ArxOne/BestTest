// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a set of tests
    /// </summary>
    /// <seealso cref="System.MarshalByRefObject" />
    public class TestSet : MarshalByRefObject
    {
        [Obsolete("Serialization-only ctor")]
        public TestSet() { }

        private readonly Queue<TestDescription> _descriptions;
        private readonly IList<TestResult> _assessments = new List<TestResult>();

        public IEnumerable<TestResult> Assessments
        {
            get
            {
                lock (_assessments)
                    return _assessments.ToArray();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSet"/> class.
        /// </summary>
        /// <param name="descriptions">The descriptions.</param>
        public TestSet(IEnumerable<TestDescription> descriptions)
        {
            _descriptions = new Queue<TestDescription>(descriptions);
        }

        /// <summary>
        /// Gets the next test to run.
        /// </summary>
        /// <returns>A <see cref="TestDescription"/> or null when the set is empty</returns>
        public TestDescription PullNextTest()
        {
            lock (_descriptions)
            {
                if (_descriptions.Count == 0)
                    return null;
                return _descriptions.Dequeue();
            }
        }

        public void PushAssessment(TestResult result)
        {
            lock (_assessments)
                _assessments.Add(result);
        }
    }
}
