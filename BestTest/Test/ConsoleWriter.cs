// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.Globalization;
    using System.IO;
    using Utility;

    public class ConsoleWriter : CrossAppDomainObject
    {
        private readonly TextWriter _output;
        private readonly object _lock = new object();
        private int _countWritten;

        public const string IndexMarker = "¤¤¤";

        public int MarkerPadding { get; set; } = 5;

        public ConsoleWriter(TextWriter output)
        {
            _output = output;
        }

        public void WriteLine(string text = "")
        {
            lock (_lock)
            {
                if (text.Contains(IndexMarker))
                    text = text.Replace(IndexMarker, (++_countWritten).ToString(CultureInfo.InvariantCulture).PadLeft(MarkerPadding));
                _output.WriteLine(text);
            }
        }
    }
}
