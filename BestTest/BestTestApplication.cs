// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest
{
    using System;
    using System.IO;
    using Mono.Options;
    using Test;

    public static class BestTestApplication
    {
        public static int Main(string[] args)
        {
            var testParameters = new TestParameters();

            // these variables will be set when the command line is parsed
            var shouldShowHelp = false;
            bool showLogo = true;

            // https://github.com/xamarin/XamarinComponents/tree/master/XPlat/Mono.Options
            var options = new OptionSet
            {
                {
                    "m|maxcpucount:", "Specifies the maximum number of concurrent processes to use when building",
                    (int? v) => testParameters.ParallelRuns = v ?? Environment.ProcessorCount
                },
                {"nologo", "Hides header", _ => showLogo = false},
                {"h|help", "show this message and exit", _ => shouldShowHelp = true},
            };

            try
            {
                // parse the command line
                testParameters.AssemblyPaths.AddRange(options.Parse(args));
                if (testParameters.AssemblyPaths.Count == 0 || shouldShowHelp)
                {
                    WriteHeader();
                    Console.WriteLine("Syntax: BestTest [options] <assemblies>");
                    Console.WriteLine("Options:");
                    using (var s = new StringWriter())
                    {
                        options.WriteOptionDescriptions(s);
                        Console.WriteLine(s);
                    }
                    return 0;
                }
            }
            catch (OptionException e)
            {
                // output some error message
                WriteHeader();
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `BestTest --help' for more information.");
                return -1;
            }

            if (showLogo)
                WriteHeader();

            var testEngine = new TestEngine();
            return testEngine.Run(testParameters);
        }

        private static void WriteHeader()
        {
            Console.WriteLine("BestTest - A(nother) test engine with great expectations (as usual)");
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine();
        }
    }
}

