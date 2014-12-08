using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CallParser;

namespace CallParserUnitTest
{
    [TestClass]
    public class ParserUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            CallParser.Parser parser = new CallParser.Parser();

            PrefixList prefixList = new PrefixList();
            prefixList.PrefixFileName = "prefix.lst";
            prefixList.CallFileName = "call.lst";
           
        }
    } // end class
}
