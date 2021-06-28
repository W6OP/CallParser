﻿using System.Collections.Generic;
using System.IO;
using W6OP.CallParser;
using CsvHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CallParserUnitTest
{
    [TestClass]
    public class PrefixFileParserTest
    {
        List<string> _Records;
        private PrefixFileParser _PrefixFileParser;
        private CallLookUp _CallLookUp;

        [TestInitialize()]
        public void LoadCallSigns()
        {
            _Records = new List<string>();

            using (StreamReader reader = new StreamReader("rbn2.csv"))
            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    csv.Configuration.MissingFieldFound = null;
                    _Records.Add(csv.GetField("dx"));
                }
            }
        }

        [TestMethod]
        private void TestMask()
        {

        }

        [TestMethod]
        public void CallLookUp()
        {
            List<Hit> result;
            int expected;

            _PrefixFileParser = new PrefixFileParser();
            _PrefixFileParser.ParsePrefixFile("");
            _CallLookUp = new CallLookUp(_PrefixFileParser);

            string[] testCallSigns = new string[14] { "TX9", "TX4YKP/R", "/KH0PR", "W6OP/4", "OEM3SGU/3", "AM70URE/8", "5N31/OK3CLA", "BV100", "BY1PK/VE6LB", "VE6LB/BY1PK", "DC3RJ/P/W3", "RAEM", "AJ3M/BY1RX", "4D71/N0NM" }; //, "4X130RISHON", "9N38", "AX3GAMES", "DA2MORSE", "DB50FIRAC", "DL50FRANCE", "FBC5AGB", "FBC5NOD", "FBC5YJ", "FBC6HQP", "GB50RSARS", "HA80MRASZ", "HB9STEVE", "HG5FIRAC", "HG80MRASZ", "II050SCOUT", "IP1METEO", "J42004A", "J42004Q", "LM1814", "LM2T70Y", "LM9L40Y", "LM9L40Y/P", "OEM2BZL", "OEM3SGU", "OEM3SGU/3", "OEM6CLD", "OEM8CIQ", "OM2011GOOOLY", "ON1000NOTGER", "ON70REDSTAR", "PA09SHAPE", "PA65VERON", "PA90CORUS", "PG50RNARS", "PG540BUFFALO", "S55CERKNO", "TM380", "TYA11", "U5ARTEK/A", "V6T1", "VI2AJ2010", "VI2FG30", "VI4WIP50", "VU3DJQF1", "VX31763", "WD4", "XUF2B", "YI9B4E", "YO1000LEANY", "ZL4RUGBY", "ZS9MADIBA" };
            int[] testResult = new int[14] {0,7,1,1,0,1,1,0,0,1,1,0,1,1};

            for (int counter = 0; counter <= testCallSigns.Length - 1; counter++)
            {
                result = _CallLookUp.LookUpCall(testCallSigns[counter]).ToList();
                expected = testResult[counter];
              
                Assert.AreEqual(expected, result.Count());
            }
        }

    } // end class
}
