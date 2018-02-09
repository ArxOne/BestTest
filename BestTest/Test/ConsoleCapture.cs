// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Test
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Allows to mute console output (for verbose test fuckers)
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class ConsoleCapture : IDisposable
    {
        [ThreadStatic]
        private static StringBuilder _capture;

        private class ConsoleWriter : TextWriter
        {
            public override Encoding Encoding { get; } = Encoding.UTF8;

            /// <summary>
            /// Writes a character to the text string or stream.
            /// All other write methods fall back here
            /// </summary>
            /// <param name="value">The character to write to the text stream.</param>
            public override void Write(char value)
            {
                _capture.Append(value);
            }
        }

        /// <summary>
        /// Gets the captured console output.
        /// </summary>
        /// <value>
        /// The capture.
        /// </value>
        public string Capture => _capture.ToString();

        public ConsoleCapture()
        {
            _capture = new StringBuilder();
            var consoleWriter = new ConsoleWriter();
            Console.SetOut(consoleWriter);
            Console.SetError(consoleWriter);
            MuteStdHandle();
        }

        public void Dispose()
        {
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        private const int STD_OUTPUT_HANDLE = -11;

        private void MuteStdHandle()
        {
            try
            {
                SetStdHandle(STD_OUTPUT_HANDLE, IntPtr.Zero);
            }
            catch { }
        }
    }
}