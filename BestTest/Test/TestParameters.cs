﻿// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class TestParameters
    {
        public List<string> AssemblyPaths { get; set; } = new List<string>();

        public int ParallelRuns { get; set; } = 1;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        public bool InconclusiveAsError { get; set; } = true;

        public bool IsolateAssemblies { get; set; } = true;
    }
}
