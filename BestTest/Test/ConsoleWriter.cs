// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Globalization;
    using System.IO;

    public class ConsoleWriter : MarshalByRefObject
    {
        private readonly TextWriter _output;
        private readonly object _lock = new object();
        private int _countWritten;

        public const string IndexMarker = "¤¤¤";

        public ConsoleWriter(TextWriter output)
        {
            _output = output;
        }

        public void WriteLine(string text = "")
        {
            lock (_lock)
            {
                if (text.Contains(IndexMarker))
                    text = text.Replace(IndexMarker, (++_countWritten).ToString(CultureInfo.InvariantCulture).PadLeft(5));
                _output.WriteLine(text);
            }
        }
    }
}
