// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Reflection
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Misc information about assembly
    /// </summary>
    public static class AssemblyReflection
    {
        /// <summary>
        /// Gets the assembly file version.
        /// </summary>
        /// <value>
        /// The file version.
        /// </value>
        public static Version FileVersion => Version.Parse(((AssemblyFileVersionAttribute)typeof(AssemblyReflection).Assembly
            .GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version);
    }
}