using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>
        /// Parse the prefix file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// Read the file of call signs to test with.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button2_Click(object sender, EventArgs e)
        {
            _Records = new List<string>();
        
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
                    _Records.Add(csv.GetField("dx").ToUpper());

                    //// comment out to keep list to 1 million
                    //temp = csv.GetField("callsign");
                    //// check for a "_" ie: VE7CC-7, OH6BG-1, WZ7I-3 - remove the characters "-x"
                    //if (temp.IndexOf("-") != -1)
                    //{
                    //    temp = temp.Substring(0, temp.IndexOf("-"));
                    //}
                    //_Records.Add(temp.ToUpper());

                }
            }

            Console.WriteLine("Load Time: " + sw.ElapsedMilliseconds + "ms");
            //label3.Text = ((float)(sw.ElapsedMilliseconds)).ToString() + "ms";
            label4.Text = _Records.Count.ToString() + " calls loaded";
            Cursor.Current = Cursors.Default;
        }


        /// <summary>
        /// What should I do about W/M0RYB or F/M0RYB
        /// 
        /// LOOKUP A SINGLE CALL SIGN.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, EventArgs e)
        {
            IEnumerable<Hit> hitCollection;
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
                    hitCollection = _CallLookUp.LookUpCall(TextBoxCall.Text);
                    hitList = hitCollection.ToList();                   
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

       
        /// <summary>
        /// Batch lookup.
        /// Send in a list of all calls at once.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button3_Click(object sender, EventArgs e)
        {
            IEnumerable<Hit> hitCollection;
            List<Hit> hitList = new List<Hit>(10000000);
            float divisor = 1000;

            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;

            var sw = Stopwatch.StartNew();
            hitCollection = _CallLookUp.LookUpCall(_Records);
            label1.Text = "Search Time: " + sw.Elapsed; // + " ticks: " + sw.ElapsedTicks;

           foreach (Hit hit in hitCollection)
            {
                hitList.Add(hit);
            }

            divisor = hitList.Count / 1000;
            
            label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
            label3.Text = ((float)(sw.ElapsedMilliseconds / divisor)).ToString() + " microseconds per call sign";
            Console.WriteLine("Finished - hitcount = " + hitList.Count.ToString());

            // save to a text file
            //var thread = new Thread(() =>
            //{
            //    Cursor.Current = Cursors.WaitCursor;
            //    SaveHitList(hitList);
            //});
            //thread.Start();
        }

        /// <summary>
        /// Read entire call sign list but send in calls one at a time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSemiBatch_Click(object sender, EventArgs e)
        {
            IEnumerable<Hit> hitCollection;
            List<Hit> hitList = new List<Hit>();
            List<Hit> tempHitList;
            //float divisor = 1000;
            int total = 0;

            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;

            var sw = Stopwatch.StartNew();
            Cursor.Current = Cursors.WaitCursor;

            foreach (string call in _Records)
            {
                total += 1;
                //if (total == _Records.Count - 5)
                //{
                //    var a = 2;
                //}
                hitCollection = _CallLookUp.LookUpCall(call);
                tempHitList = hitCollection.ToList();
                hitList.AddRange(tempHitList);

                //foreach (Hit hit in tempHitList)
                //{
                //    hitList.AddRange(tempHitList);
                //}
                Application.DoEvents();
            }

            label1.Text = "Search Time: " + sw.Elapsed;
            label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
            label3.Text =  ((float)(sw.ElapsedMilliseconds / hitList.Count)).ToString() + "us";
            Console.WriteLine("Finished - hitcount = " + hitList.Count.ToString());

            //var thread = new Thread(() =>
            //{
            //    Cursor.Current = Cursors.WaitCursor;
            //    SaveHitList(hitList);
            //});
            //thread.Start();
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
                    
                    if (callSignInfo.Kind == PrefixKind.pfDXCC)
                    {
                        csv.WriteField(callSignInfo.CallSign);
                        csv.WriteField(callSignInfo.MainPrefix);
                    }
                    else
                    {
                        csv.WriteField("----  " + callSignInfo.MainPrefix);
                    }
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
        //private void Test()
        //{
        //    List<Hit> hit;

        //    var thread = new Thread(() =>
        //    {
        //        var sw = Stopwatch.StartNew();
        //        hit = _CallLookUp.LookUpCall(_Records);
        //        UpdateDisplay("Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks);
        //    });
        //    thread.Start();
        //}

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
