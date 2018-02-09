// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    public class TestInstance
    {
        public object Instance;

        public ITestContext Context;

        public TestResult ClassInitializeFailure;
        public TestResult AssemblyInitializeFailure;
    }
}
