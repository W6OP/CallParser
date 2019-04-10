using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CallParser;
using System.Collections.Generic;

namespace CallParserUnitTest
{
    [TestClass]
    public class ParserUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            CallParser.PrefixFileParser parser = new CallParser.PrefixFileParser();

            PrefixList prefixList = new PrefixList();
            prefixList.PrefixFileName = "prefix.lst";
            prefixList.CallFileName = "call.lst";

            parser = prefixList.LoadFiles();

            
            //List<PrefixInfo> prefixList = _CallParser.GetCallInformation("w6op");
        }
    } // end class
}
