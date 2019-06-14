using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using W6OP.CallParser;
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

            _PrefixFileParser = new PrefixFileParser();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
          
        }

        /// <summary>
        /// Parse the prefix file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonParsePrefixFile_Click(object sender, EventArgs e)
        {
            ParsePrefixFile("");
        }

        /// <summary>
        /// Read the file of call signs to test with.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadCallSigns_Click(object sender, EventArgs e)
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
        private void ButtonSingleCallLookup_Click(object sender, EventArgs e)
        {
            IEnumerable<Hit> hitCollection = null;
            List<Hit> hitList;
            float divisor = 1000;

            try
            {
                if (!String.IsNullOrEmpty(TextBoxCall.Text))
                {
                    var sw = Stopwatch.StartNew();

                    hitCollection = LookupCall(TextBoxCall.Text);

                    if (hitCollection != null)
                    {
                        hitList = hitCollection.ToList();

                        Console.WriteLine(hitList.Count.ToString() + " hits returned");
                        label1.Text = "Search Time: " + sw.ElapsedMilliseconds;
                        label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
                        label3.Text = ((float)(sw.ElapsedMilliseconds / divisor)).ToString() + "us";
                    }
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
        private void ButtonBatchCallSignLookup_Click(object sender, EventArgs e)
        {
            IEnumerable<Hit> hitCollection;
            // need to preallocate space in collection
            List<Hit> hitList = new List<Hit>(5000000);
            float divisor = 1000;

            Cursor.Current = Cursors.WaitCursor;

            var sw = Stopwatch.StartNew();

            hitCollection = LookupCall(_Records);

            label1.Text = "Search Time: " + sw.Elapsed; // + " ticks: " + sw.ElapsedTicks;

            hitList.AddRange(hitCollection);

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
            // need to preallocate space in collection
            List<Hit> hitList = new List<Hit>(5000000);
            //float divisor = 1000;
            int total = 0;

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
                hitCollection = LookupCall(call);

                if (hitCollection != null)
                {
                    hitList.AddRange(hitCollection);
                }

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
        /// Parse the prefix file.
        /// </summary>
        /// <param name="filePath"></param>
        public void ParsePrefixFile(string filePath)
        {
            Cursor.Current = Cursors.WaitCursor;
            var sw = Stopwatch.StartNew();
            _PrefixFileParser.ParsePrefixFile("");
            Console.WriteLine("Load Time: " + sw.ElapsedMilliseconds + "ms");
            Cursor.Current = Cursors.Default;
            _CallLookUp = new CallLookUp(_PrefixFileParser);
        }

        /// <summary>
        /// Single call lookup.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public IEnumerable<Hit> LookupCall(string call)
        {
            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }

            return _CallLookUp.LookUpCall(call);
        }

        /// <summary>
        /// Batch lookup.
        /// </summary>
        /// <param name="callSigns"></param>
        /// <returns></returns>
        public IEnumerable<Hit> LookupCall(List<string> callSigns)
        {
            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }

            return _CallLookUp.LookUpCall(callSigns);
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
