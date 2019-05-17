using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CallParser;
using CsvHelper;

namespace CallParserTestor
{
    public partial class Form1 : Form
    {
        readonly PrefixFileParser _PrefixFileParser;
        CallLookUp _CallLookUp;
        List<string> _Records;  // = new List<string>();


        public Form1()
        {
            InitializeComponent();

            _PrefixFileParser = new CallParser.PrefixFileParser();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //_PrefixFileParser.ParsePrefixFile("");
            //_CallLookUp = new CallLookUp(_PrefixFileParser._PrefixDict);
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            var sw = Stopwatch.StartNew();
            _PrefixFileParser.ParsePrefixFile("");
            Console.WriteLine("Load Time: " + sw.ElapsedMilliseconds + "ms");
            Cursor.Current = Cursors.Default;

            _CallLookUp = new CallLookUp(_PrefixFileParser._PrefixDict);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            BlockingCollection<CallSignInfo> hit;
            // string[] testCallSigns = {"TX9","/KH0PR", "W6OP/4", "OEM3SGU/3", "AM70URE/8", "5N31/OK3CLA", "BV100", "BY1PK/VE6LB", "DC3RJ/P/W3", "RAEM", "TX9", "AJ3M/BY1RX", "4D71/N0NM", "4X130RISHON", "9N38", "AX3GAMES", "DA2MORSE", "DB50FIRAC", "DL50FRANCE", "FBC5AGB", "FBC5NOD", "FBC5YJ", "FBC6HQP", "GB50RSARS", "HA80MRASZ", "HB9STEVE", "HG5FIRAC", "HG80MRASZ", "II050SCOUT", "IP1METEO", "J42004A", "J42004Q", "LM1814", "LM2T70Y", "LM9L40Y", "LM9L40Y/P", "OEM2BZL", "OEM3SGU", "OEM3SGU/3", "OEM6CLD", "OEM8CIQ", "OM2011GOOOLY", "ON1000NOTGER", "ON70REDSTAR", "PA09SHAPE", "PA65VERON", "PA90CORUS", "PG50RNARS", "PG540BUFFALO", "S55CERKNO", "TM380", "TYA11", "U5ARTEK/A", "V6T1", "VI2AJ2010", "VI2FG30", "VI4WIP50", "VU3DJQF1", "VX31763", "WD4", "XUF2B", "YI9B4E", "YO1000LEANY", "ZL4RUGBY", "ZS9MADIBA" };

            Cursor.Current = Cursors.WaitCursor;

            //hit = new BlockingCollection<CallSignInfo>();

            var sw = Stopwatch.StartNew();
            foreach (string call in _Records)
            {
                try
                {
                    if (!String.IsNullOrEmpty(call))
                    {
                        hit = new BlockingCollection<CallSignInfo>();
                        hit = _CallLookUp.LookUpCall(call);
                    }
                    // Console.WriteLine(hit.Count);
                }
                catch (Exception ex)
                {
                    // Console.WriteLine(call);
                    Console.WriteLine(ex.Message);
                }
            }

            label1.Text = "Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks;
            //Console.WriteLine("Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks);
            Cursor.Current = Cursors.Default;
            Console.WriteLine("Finished");
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            _Records = new List<string>();
            string temp;

            Cursor.Current = Cursors.WaitCursor;
            using (StreamReader reader = new StreamReader("rbn2.csv"))
            using (var csv = new CsvReader(reader))
            {

                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    csv.Configuration.MissingFieldFound = null;
                    _Records.Add(csv.GetField("dx"));

                    temp = csv.GetField("callsign");
                    // check for a "_" ie: VE7CC-7, OH6BG-1, WZ7I-3 - remove the characters "-x"
                    if (temp.IndexOf("-") != -1)
                    {
                        temp = temp.Substring(0, temp.IndexOf("-"));
                    }
                    _Records.Add(temp);

                }
            }

            label4.Text = _Records.Count.ToString() + " calls loaded";
            Cursor.Current = Cursors.Default;
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            List<CallSignInfo> hitList; // = new BlockingCollection<CallSignInfo>();
            Cursor.Current = Cursors.WaitCursor;
            float divisor = 1000;

            var sw = Stopwatch.StartNew();

            hitList = _CallLookUp.LookUpCall(_Records);
            //Test();

            Cursor.Current = Cursors.Default;
            label1.Text = "Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks;
            label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
            label3.Text = ((float)(sw.ElapsedMilliseconds / divisor)).ToString() + "us";
            Console.WriteLine("Finished - hitcount = " + hitList.Count.ToString());
            //Application.DoEvents();

            var thread = new Thread(() =>
            {
                SaveHitList(hitList);
            });
            thread.Start();
        }

        /// <summary>
        /// Create a CSV file and save a bunch of hits to see if they are correct.
        /// </summary>
        /// <param name="hitList"></param>
        private void SaveHitList(List<CallSignInfo> hitList)
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String file = Path.Combine(folderPath, "hits.csv");

           
            using (TextWriter writer = new StreamWriter(file, false, System.Text.Encoding.UTF8))
            {
                var csv = new CsvWriter(writer);
                //csv.WriteRecords(hitList); // where values implements IEnumerable
                //csv.WriteRecord();
                foreach (CallSignInfo callSignInfo in hitList)
                {
                    csv.WriteField(callSignInfo.CallSign);
                    csv.WriteField(callSignInfo.MainPrefix);
                    csv.WriteField(callSignInfo.Country);
                    csv.WriteField(callSignInfo.Province);
                    csv.WriteField(callSignInfo.Kind.ToString());
                    csv.WriteField(callSignInfo.Latitude);
                    csv.WriteField(callSignInfo.Longitude);
                    csv.NextRecord();
                }
            }

            Console.WriteLine("Finished writing file");

            //foreach (CallSignInfo callSignInfo in hitList)
            //{
            //    count++;
            //    Console.WriteLine(callSignInfo.CallSign + ":" + callSignInfo.MainPrefix + ":" + callSignInfo.Country);
            //    if (count == 100) { break; }
            //}
        }

        /// <summary>
        /// Doesn't really make any difference.
        /// </summary>
        private void Test()
        {
            List<CallSignInfo> hit;

            var thread = new Thread(() =>
            {
                var sw = Stopwatch.StartNew();
                hit = _CallLookUp.LookUpCall(_Records);
                UpdateDisplay("Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks);
            });
            thread.Start();
        }

        private void UpdateDisplay(string message)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(this.UpdateDisplay), message);
                return;
            }

            label1.Text = message;
        }

    } // end class
}
