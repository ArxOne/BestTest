// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest
{
    using System;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Reflection;

    public class Updater
    {
        private Thread _thread;

        private Tuple<Version, Uri> _result;

        public bool HasUpdate => _result != null;

        public Version OnlineVersion => _result?.Item1;
        public Uri OnlineVersionUri => _result?.Item2;

        public void StartCheck()
        {
            _thread = new Thread(GetLatestVersion);
            _thread.Start();
        }

        private void GetLatestVersion()
        {
            try
            {
                var lineEx = new Regex(@"Version\s*=\s*\""(?<version>[0-9.]+)\""", RegexOptions.IgnoreCase);
                var request = WebRequest.CreateHttp("https://raw.githubusercontent.com/ArxOne/BestTest/master/ProductInfo.tt");
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    using (var streamReader = new StreamReader(responseStream))
                    {
                        for (; ; )
                        {
                            var line = streamReader.ReadLine();
                            if (line == null)
                                break;

                            var match = lineEx.Match(line);
                            if (match.Success)
                            {
                                if (Version.TryParse(match.Groups["version"].Value, out var onlineVersion))
                                {
                                    if (AssemblyReflection.FileVersion <= onlineVersion)
                                        return;

                                    var latestVersionDirectDownload = new Uri($"https://github.com/ArxOne/BestTest/releases/download/BestTest-{onlineVersion}/BestTest.exe");
                                    _result = Tuple.Create(onlineVersion, latestVersionDirectDownload);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
