// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest
{
    public enum Verbosity
    {
        Quiet,
        Q = Quiet,

        Minimal,
        M = Minimal,

        Normal,
        N = Normal,

        Detailed,
        D = Detailed,

        Diagnostic,
        Diag = Diagnostic,
    }
}