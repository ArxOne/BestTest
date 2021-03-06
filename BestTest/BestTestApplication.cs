﻿// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest
{
    using System;
    using Mono.Options;
    using Reflection;
    using Test;

    public static class BestTestApplication
    {
        public static int Main(string[] args)
        {
            var testParameters = new TestParameters();

            // these variables will be set when the command line is parsed
            var shouldShowHelp = false;
            bool showLogo = true;
            bool checkForUpdates = true;

            // https://github.com/xamarin/XamarinComponents/tree/master/XPlat/Mono.Options
            var options = new OptionSet
            {
                {
                    "m|maxcpucount:", "Specifies the maximum number of concurrent processes to use when building",
                    (int? v) => testParameters.ParallelRuns = v ?? Environment.ProcessorCount
                },
                {"d|description", "Display test description (when available) instead of name", _ => testParameters.DisplayDescription = true},
                {
                    "v|verbosity:", @"Specifies verbosity level:
Q[uiet]:      nothing is shown
M[inimal]:    assessment is displayed
N[ormal]:     show tests list and result (default)
D[etailed]:   show stack trace on failed test
Diag[nostic]: show all tests output",
                    (Verbosity v ) => testParameters.Verbosity = v
                },
                {
                    "c|color:",@"Sets color mode:
N[one]: no color (default)
A[ansi]: ANSI colors",
                    (ConsoleMode m) => testParameters.ConsoleMode = m
                },
                {"I|isolation:", @"Sets isolation level:
N[one]: no isolation, all threads and assemblies share the same space
A[ssemblies]: isolate assemblies but no thread (MSTest compatibility, default)
T[hreads]:    isolate test threads (when there are threads)
E[verything]: assemblies and threads are isolated (for poorly written tests and SpecFlow :))",
                    (IsolationLevel i) => testParameters.Isolation = i
    },
                {"i|inconclusivesucceed", "Consider inconclusive tests succeed", _ => testParameters.InconclusiveAsError = false},
                {"t|timeout=", "Set individual test time out", (TimeSpan t) => testParameters.Timeout = t},
                {"u|noupdatecheck", "Does not check for updates", _ => checkForUpdates = false},
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
                    options.WriteOptionDescriptions(Console.Out);
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

            var consoleOut = Console.Out;
            var updater = new Updater();
            if (checkForUpdates)
                updater.StartCheck();

            var testEngine = new TestEngine();
            var resultCode = testEngine.Run(testParameters);

            if (updater.HasUpdate)
                consoleOut.WriteLine($"A new version {updater.OnlineVersion} is available at {updater.OnlineVersionUri}");

            return resultCode;
        }

        private static void WriteHeader()
        {
            var title = $"BestTest {AssemblyReflection.FileVersion}";
            Console.WriteLine(title);
            Console.WriteLine();
        }
    }
}

