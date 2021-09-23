using System.Collections.Generic;
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

        /// <summary>
        /// Send in multiple call signs and make sure the correct DXCC number
        /// and entity literal is returned.
        /// </summary>
        [TestMethod]
        public void CallLookUp()
        {
            List<Hit> result;
            int expected;

            _PrefixFileParser = new PrefixFileParser();
            _PrefixFileParser.ParsePrefixFile("");
            _CallLookUp = new CallLookUp(_PrefixFileParser);

            // Add calls where mask ends with '.' ie: KG4AA and as compare KG4AAA
            string[] testCallSigns = new string[16] { "TX9", "TX4YKP/R", "/KH0PR", "W6OP/4", "OEM3SGU/3", "AM70URE/8", "5N31/OK3CLA", "BV100", "BY1PK/VE6LB",
                "VE6LB/BY1PK", "DC3RJ/P/W3", "RAEM", "AJ3M/BY1RX", "4D71/N0NM",  "OEM3SGU", "KG4AA" }; //, "4X130RISHON", "9N38", "AX3GAMES", "DA2MORSE", "DB50FIRAC", "DL50FRANCE", "FBC5AGB", "FBC5NOD", "FBC5YJ", "FBC6HQP", "GB50RSARS", "HA80MRASZ", "HB9STEVE", "HG5FIRAC", "HG80MRASZ", "II050SCOUT", "IP1METEO", "J42004A", "J42004Q", "LM1814", "LM2T70Y", "LM9L40Y", "LM9L40Y/P", "OEM2BZL", "OEM3SGU", "OEM3SGU/3", "OEM6CLD", "OEM8CIQ", "OM2011GOOOLY", "ON1000NOTGER", "ON70REDSTAR", "PA09SHAPE", "PA65VERON", "PA90CORUS", "PG50RNARS", "PG540BUFFALO", "S55CERKNO", "TM380", "TYA11", "U5ARTEK/A", "V6T1", "VI2AJ2010", "VI2FG30", "VI4WIP50", "VU3DJQF1", "VX31763", "WD4", "XUF2B", "YI9B4E", "YO1000LEANY", "ZL4RUGBY", "ZS9MADIBA" };
            int[] testResult = new int[16] { 0, 7, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1 };

            for (int counter = 0; counter <= testCallSigns.Length - 1; counter++)
            {
                result = _CallLookUp.LookUpCall(testCallSigns[counter]).ToList();
                expected = testResult[counter];

                Assert.AreEqual(expected, result.Count());
            }
        }

        [TestMethod]
        public void CallLookUpEx()
        {
            List<Hit> result;
            (double dxcc, string entity) expected;
            bool isMatchFound;

            _PrefixFileParser = new PrefixFileParser();
            _PrefixFileParser.ParsePrefixFile("");
            _CallLookUp = new CallLookUp(_PrefixFileParser);

            // Add calls where mask ends with '.' ie: KG4AA and as compare KG4AAA

            foreach (var key in goodDataCheck.Keys)
            {
                isMatchFound = false;
                result = _CallLookUp.LookUpCall(key).ToList();

                switch (result.Count)
                {
                    case 0:
                        Assert.IsTrue(badDataCheck.ContainsKey(key));
                        break;
                    case 1:
                        if (result[0].Kind == PrefixKind.Province)
                        {
                            expected = (result[0].DXCC, result[0].Province);
                            Assert.AreEqual(goodDataCheck[key], expected); //W6OP/3B7
                        }
                        else
                        {
                            expected = (result[0].DXCC, result[0].Country);
                            Assert.AreEqual(goodDataCheck[key], expected);
                        }
                        break;
                    default: // multiple hits
                        foreach (Hit hit in result)
                        {
                            if (hit.Kind == PrefixKind.Province)
                            {
                                expected = (hit.DXCC, hit.Province);
                                if (expected == goodDataCheck[key])
                                {
                                    isMatchFound = true;
                                }
                            }
                            else
                            {
                                expected = (hit.DXCC, hit.Country);
                                if (expected == goodDataCheck[key])
                                {
                                    isMatchFound = true;
                                }
                            }
                        }

                        Assert.IsTrue(isMatchFound);
                        break;
                }
            }
        }

        [TestMethod]
        public void CallLookUpBatch()
        {
            List<Hit> result;
            List<string> callSigns = new List<string>();
            (double dxcc, string entity) expected;
            bool isMatchFound;

            _PrefixFileParser = new PrefixFileParser();
            _PrefixFileParser.ParsePrefixFile("");
            _CallLookUp = new CallLookUp(_PrefixFileParser);

            foreach (var key in goodDataCheck.Keys)
            {
                callSigns.Add(key);
            }

            List<Hit> hitList = _CallLookUp.LookUpCall(callSigns).ToList();

            foreach (var hit in hitList)
            {
                isMatchFound = false;
                result = hitList; // _CallLookUp.LookUpCall(key).ToList();

                switch (result.Count)
                {
                    case 0:
                        Assert.IsTrue(badDataCheck.ContainsKey(hit.CallSign));
                        break;
                    case 1:
                        if (result[0].Kind == PrefixKind.Province)
                        {
                            expected = (result[0].DXCC, result[0].Province);
                            Assert.AreEqual(goodDataCheck[hit.CallSign], expected); //W6OP/3B7
                        }
                        else
                        {
                            expected = (result[0].DXCC, result[0].Country);
                            Assert.AreEqual(goodDataCheck[hit.CallSign], expected);
                        }
                        break;
                    default: // multiple hits
                        foreach (Hit hit2 in result)
                        {
                            if (hit2.Kind == PrefixKind.Province)
                            {
                                expected = (hit2.DXCC, hit2.Province);
                                if (expected == goodDataCheck[hit2.CallSign])
                                {
                                    isMatchFound = true;
                                }
                            }
                            else
                            {
                                expected = (hit2.DXCC, hit2.Country);
                                if (expected == goodDataCheck[hit2.CallSign])
                                {
                                    isMatchFound = true;
                                }
                            }
                        }

                        Assert.IsTrue(isMatchFound);
                        break;
                }
            }


        }

        // test data relating call sign to dxcc number
        // if it is a province then I need to look at province instead of country
        readonly Dictionary<string, (double dxcc, string entity)> goodDataCheck = new Dictionary<string, (double dxcc, string entity)>()
        {
            { "AM70URE/8", (029, "Canary Is.") },
            { "PU2Z", (108, "Call Area 2") }, // province
            { "PU2ZZ", (108, "Call Area 2") }, // province
            { "IG0NFQ", (248, "Lazio;Umbria") }, // province
            { "IG0NFU", (225, "Sardinia") },
            { "W6OP", (291, "CA") }, // province
            { "TJ/W6OP", (406, "Cameroon") },
            { "W6OP/3B7", (004, "St. Brandon") }, // province
            { "KL6OP", (006, "Alaska") },
            { "YA6AA", (003, "Afghanistan") },
            { "3Y2/W6OP", (024, "Bouvet I.") }, // could also be Peter 1 - need to test antartica
            { "W6OP/VA6", (001, "Alberta") }, // province
            { "VA6AY", (001, "Alberta") }, // province
            { "CE7AA", (112, "Aisen;Los Lagos (Llanquihue, Isla Chiloe and Palena)") }, // province
            { "3G0DA", (112, "Chile") },
            { "FK6DA", (512, "Chesterfield Is.") }, // can also be New Caledonia - test for multiple returns 
            { "BA6V", (318, "Hu Bei") }, // province
            { "5J7AA", (116, "Arauca;Boyaca;Casanare;Santander") }, // province
            { "TX4YKP/R", (298, "Wallis & Futuna Is.") },
            { "TX4YKP/B", (162, "New Caledonia") },
            { "TX4YKP", (509, "Marquesas I.") },
            { "TX5YKP", (175, "French Polynesia") },
            { "TX6YKP", (036, "Clipperton I.") },
            { "TX7YKP", (512, "Chesterfield Is.") },
            { "TX8YKP", (508, "Austral I.") },
            { "KG4AA", (105, "Guantanamo Bay") },
            { "KG4AAA", (291, "AL;FL;GA;KY;NC;SC;TN;VA") }, // province
            { "BS4BAY/P", (506, "Scarborough Reef") },
            { "CT8AA", (149, "Azores") },
            { "BU7JP", (386, "Taiwan") },
            { "BU7JP/P", (386, "Kaohsiung") },
            { "VE0AAA", (001, "Canada") },
            { "VE3NEA", (001, "Ontario") },
            { "VK9O", (150, "External territories") },
            { "VK9OZ", (150, "External territories") },
            { "VK9OC", (038, "Cocos-Keeling Is.") },
            { "VK0M/MB5KET", (153, "Macquarie I.") },
            { "VK0H/MB5KET", (111, "Heard I.") },
            { "WK0B", (291, "CO;IA;KS;MN;MO;ND;NE;SD") },
            { "VP2V/MB5KET", (065, "British Virgin Is.") },
            { "VP2M/MB5KET", (096, "Montserrat") },
            { "VK9X/W6OP", (035, "Christmas Is.") },
            { "VK9/W6OP", (035, "Christmas Is.") },
            { "VK9/W6OA", (303, "Willis I.") },
            { "VK9/W6OB", (150, "External territories") },
            { "VK9/W6OC", (038, "Cocos-Keeling Is.") },
            { "VK9/W6OD", (147, "Lord Howe I.") },
            { "VK9/W6OE", (171, "Mellish Reef") },
            { "VK9/W6OF", (189, "Norfolk I.") },
            { "RA9BW", (015, "Chelyabinskaya oblast") },
            { "RA9BW/3", (054, "Central") },
            { "LR9B/22QIR", (100, "Argentina") },
            { "6KDJ/UW5XMY", (137, "South Korea") },
            { "WP5QOV/P", (43, "Desecheo I.") },
            // bad calls
            { "NJY8/QV3ZBY", (291, "United States") },
            { "QZ5U/IG0NFQ", (248, "Lazio;Umbria") },
            { "Z42OIO", (0, "Unassigned prefix") },
           // { "LR9B/22QIR", (0, "invalid prefix pattern and invalid call") },
            
        };

        readonly Dictionary<string, string> badDataCheck = new Dictionary<string, string>()
        {
            { "QZ5U/IG0NFQ", "valid prefix pattern but invalid prefix" },
            { "NJY8/QV3ZBY", "invalid prefix pattern and invalid call" },
           // { "LR9B/22QIR", "invalid prefix pattern and invalid call" },
            //{ "6KDJ/UW5XMY", "invalid prefix pattern" },
            { "Z42OIO", "Unassigned prefix" },
        };

    } // end class
}
//6KDJ/UW5XMY