// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;
    using Framework;

    [Serializable]
    public class TestParameters
    {
        /// <summary>
        /// Gets the assembly paths (wildcards allowed here).
        /// </summary>
        /// <value>
        /// The assembly paths.
        /// </value>
        public List<string> AssemblyPaths { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the parallel runs (the number of parallel threads involved in testing).
        /// </summary>
        /// <value>
        /// The parallel runs.
        /// </value>
        public int ParallelRuns { get; set; } = 1;

        /// <summary>
        /// Gets or sets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets a value indicating whether inconclusive inconclusive tests must be considered as errors.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [inconclusive as error]; otherwise, <c>false</c>.
        /// </value>
        public bool InconclusiveAsError { get; set; } = true;

        /// <summary>
        /// Gets or sets the isolation level.
        /// </summary>
        /// <value>
        /// The isolation.
        /// </value>
        public IsolationLevel Isolation { get; set; } = IsolationLevel.Assemblies;

        /// <summary>
        /// Gets or sets the verbosity level.
        /// </summary>
        /// <value>
        /// The verbosity.
        /// </value>
        public Verbosity Verbosity { get; set; } = Verbosity.Normal;

        /// <summary>
        /// Gets or sets the test framework.
        /// </summary>
        /// <value>
        /// The framework.
        /// </value>
        public ITestFramework Framework { get; set; } = new MSTestFramework();

        /// <summary>
        /// Gets or sets a value indicating whether tests description should be displayed instead of names.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [display description]; otherwise, <c>false</c>.
        /// </value>
        public bool DisplayDescription { get; set; }
    }
}
