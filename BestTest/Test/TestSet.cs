// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utility;

    /// <summary>
    /// Represents a set of tests
    /// </summary>
    /// <seealso cref="System.MarshalByRefObject" />
    public class TestSet : CrossAppDomainObject
    {
        [Obsolete("Serialization-only ctor")]
        public TestSet() { }

        private readonly Queue<TestDescription> _descriptions;
        private readonly List<TestResult> _results = new List<TestResult>();

        /// <summary>
        /// Gets the total tests count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count { get; }

        public TestResult[] Results
        {
            get
            {
                lock (_results)
                    return _results.ToArray();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSet" /> class.
        /// </summary>
        /// <param name="descriptions">The descriptions.</param>
        /// <param name="totalCount">The total count.</param>
        public TestSet(IEnumerable<TestDescription> descriptions, int totalCount)
        {
            _descriptions = new Queue<TestDescription>(descriptions);
            Count = totalCount;
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

        public void PushResult(TestResult result)
        {
            lock (_results)
                _results.Add(result);
        }

        public void PushResults(IEnumerable<TestResult> results)
        {
            lock (_results)
                _results.AddRange(results);
        }
    }
}
