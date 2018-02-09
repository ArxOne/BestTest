﻿// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace WorseTest
{
    using System;
    using System.ComponentModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SimpleTest
    {
        [TestMethod]
        public void SucceedingTest()
        {
        }

        [TestMethod]
        public void InconclusiveTest()
        {
            Assert.Inconclusive("Not sure {0}", "of it");
        }

        [TestMethod]
        public void FailingTest()
        {
            Assert.Fail(":(");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ExpectedExceptionTest()
        {
            throw new ArgumentException();
        }

        [TestMethod]
        public void UnexpectedExceptionTest()
        {
            throw new ArgumentException();
        }
    }
}