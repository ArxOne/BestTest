// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.IO;

    public class ConsoleWriter : MarshalByRefObject
    {
        private readonly TextWriter _output;
        private readonly object _lock = new object();

        public ConsoleWriter(TextWriter output)
        {
            _output = output;
        }

        public void WriteLine(string text)
        {
            lock (_lock)
                _output.WriteLine(text);
        }
    }
}