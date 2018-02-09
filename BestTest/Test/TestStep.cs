// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    public enum TestStep
    {
        AssemblyInitialize,
        ClassInitialize,
        TestInitialize,
        Test,
        TestCleanup,
        ClassCleanup,
        AssemblyCleanup,
    }
}