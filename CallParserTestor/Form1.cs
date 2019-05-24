using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
          
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            var sw = Stopwatch.StartNew();
            _PrefixFileParser.ParsePrefixFile("");
            Console.WriteLine("Load Time: " + sw.ElapsedMilliseconds + "ms");
            Cursor.Current = Cursors.Default;
            _CallLookUp = new CallLookUp(_PrefixFileParser);
        }

        /// <summary>
        /// What should I do about W/M0RYB or F/M0RYB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, EventArgs e)
        {
            List<Hit> hitList;
            float divisor = 1000;

            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            var sw = Stopwatch.StartNew();

            try
            {
                if (!String.IsNullOrEmpty(TextBoxCall.Text))
                { 
                    hitList = _CallLookUp.LookUpCall(TextBoxCall.Text);
                    Console.WriteLine(hitList.Count.ToString() + " hits returned");
                    label1.Text = "Search Time: " + sw.ElapsedMilliseconds;
                    label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
                    label3.Text = ((float)(sw.ElapsedMilliseconds / divisor)).ToString() + "us";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Cursor.Current = Cursors.Default;
            Console.WriteLine("Finished");
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            _Records = new List<string>();
            string temp;

            Cursor.Current = Cursors.WaitCursor;

            var sw = Stopwatch.StartNew();

            using (StreamReader reader = new StreamReader("rbn2.csv"))
            using (var csv = new CsvReader(reader))
            {

                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    csv.Configuration.MissingFieldFound = null;
                    _Records.Add(csv.GetField("dx"));

                    // comment out to keep list to 1 million
                    temp = csv.GetField("callsign");
                    // check for a "_" ie: VE7CC-7, OH6BG-1, WZ7I-3 - remove the characters "-x"
                    if (temp.IndexOf("-") != -1)
                    {
                        temp = temp.Substring(0, temp.IndexOf("-"));
                    }
                    _Records.Add(temp);

                }
            }

            Console.WriteLine("Load Time: " + sw.ElapsedMilliseconds + "ms");
            //label3.Text = ((float)(sw.ElapsedMilliseconds)).ToString() + "ms";
            label4.Text = _Records.Count.ToString() + " calls loaded";
            Cursor.Current = Cursors.Default;
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            List<Hit> hitList;
            float divisor = 1000;

            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;

            var sw = Stopwatch.StartNew();

            hitList = _CallLookUp.LookUpCall(_Records);
            //Test();

            label1.Text = "Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks;
            label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
            label3.Text = ((float)(sw.ElapsedMilliseconds / divisor)).ToString() + "us";
            Console.WriteLine("Finished - hitcount = " + hitList.Count.ToString());

            var thread = new Thread(() =>
            {
                Cursor.Current = Cursors.WaitCursor;
                SaveHitList(hitList);
            });
            thread.Start();
        }

        /// <summary>
        /// Create a CSV file and save a bunch of hits to see if they are correct.
        /// </summary>
        /// <param name="hitList"></param>
        private void SaveHitList(List<Hit> hitList)
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String file = Path.Combine(folderPath, "hits.csv");

            List<Hit> sortedList = hitList;    //.OrderBy(o => o.CallSign).ToList();

            using (TextWriter writer = new StreamWriter(file, false, System.Text.Encoding.UTF8))
            {
                var csv = new CsvWriter(writer);
                //csv.WriteRecords(hitList); // where values implements IEnumerable
                //csv.WriteRecord();
                foreach (Hit callSignInfo in sortedList)
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
            UpdateCursor();
           // Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Doesn't really make any difference.
        /// </summary>
        private void Test()
        {
            List<Hit> hit;

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

        private void UpdateCursor()
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action(this.UpdateCursor));
                return;
            }
            Cursor.Current = Cursors.Default;
        }

    } // end class
}
