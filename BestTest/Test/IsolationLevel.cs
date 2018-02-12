// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;

    [Flags]
    public enum IsolationLevel
    {
        None = 0,
        N = None,

        Assemblies = 0x01,
        A = Assemblies,

        Threads = 0x02,
        T = Threads,

        Everything = Assemblies | Threads,
        E = Everything
    }
}
