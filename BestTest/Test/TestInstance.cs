// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System.Reflection;

    public  class TestInstance
  {
      public object Instance;
      public MethodInfo ClassCleanup;
      public TestAssessment Assessment;
  }
}