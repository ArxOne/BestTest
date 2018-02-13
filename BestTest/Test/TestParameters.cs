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
        public List<string> AssemblyPaths { get; } = new List<string>();

        public int ParallelRuns { get; set; } = 1;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        public bool InconclusiveAsError { get; set; } = true;

        public IsolationLevel Isolation { get; set; } = IsolationLevel.Assemblies;

        public Verbosity Verbosity { get; set; } = Verbosity.Normal;

        public MSTestFramework Framework { get; set; } = new MSTestFramework();
    }
}
