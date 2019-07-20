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

        public string Success { get; }
        public string Warning { get; }
        public string Error { get; }
        public string Normal { get; }

        public ConsoleWriter(TextWriter output, ConsoleMode mode)
        {
            _output = output;
            switch (mode)
            {
                case ConsoleMode.Ansi:
                    var csi = "\x1B[";
                    Success = csi + "32m";
                    Warning = csi + "33m";
                    Error = csi + "31m";
                    Normal = csi + "0m";
                    break;
                default:
                    Success = "";
                    Warning = "";
                    Error = "";
                    Normal = "";
                    break;
            }
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
