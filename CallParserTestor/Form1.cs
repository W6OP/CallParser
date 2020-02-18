/**
 * Copyright (c) 2019 Peter Bourget W6OP
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish,
 * distribute, sublicense, create a derivative work, and/or sell copies of the
 * Software in any work that is designed, intended, or marketed for pedagogical or
 * instructional purposes related to programming, coding, application development,
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works,
 * or sale is expressly withheld.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/*
 CallParserTestor.cs
 CallParser
 
 Created by Peter Bourget on 6/9/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Windows forms program to show usage and test with.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using W6OP.CallParser;
using CsvHelper;
using System.Threading.Tasks;
using System.Threading;

namespace CallParserTestor
{
    /// <summary>
    /// GUI tester of CallParser with sample code.
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly PrefixFileParser _PrefixFileParser;
        private CallLookUp _CallLookUp;
        private List<string> _Records; 
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// Constructor.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            PrefixFileParser prefixFileParser = new PrefixFileParser();
            _PrefixFileParser = prefixFileParser;
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

            stopwatch = Stopwatch.StartNew();

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

            Console.WriteLine("Load Time: " + stopwatch.ElapsedMilliseconds + "ms");
            label4.Text = _Records.Count.ToString() + " calls loaded";
            Cursor.Current = Cursors.Default;
        }


        /// <summary>
        /// Look up a single call
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSingleCallLookup_Click(object sender, EventArgs e)
        {
            IEnumerable<CallSignInfo> hitCollection = null;
            List<CallSignInfo> hitList;
            float divisor = 1000;

            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                if (!String.IsNullOrEmpty(TextBoxCall.Text))
                {
                    stopwatch = Stopwatch.StartNew();

                    hitCollection = _CallLookUp.LookUpCall(TextBoxCall.Text);

                    if (hitCollection != null)
                    {
                        hitList = hitCollection.ToList();

                        Console.WriteLine(hitList.Count.ToString() + " hits returned");
                        label1.Text = "Search Time: " + stopwatch.ElapsedMilliseconds;
                        label2.Text = "Finished - hitcount = " + hitList.Count.ToString();
                        label3.Text = ((float)(stopwatch.ElapsedMilliseconds / divisor)).ToString() + "us";

                        // save to a text file
                        var thread = new Thread(() =>
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            SaveHitList(hitList);
                        });
                        thread.Start();
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
        /// Send in a list of all calls at once. Be sure to move the task
        /// off the GUI thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonBatchCallSignLookup_Click(object sender, EventArgs e)
        {
            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            Task.Run(() => BatchCallSignLookup());
        }


        private void BatchCallSignLookup()
        {
            IEnumerable<CallSignInfo> hitCollection;
           
            stopwatch = Stopwatch.StartNew();

            hitCollection = _CallLookUp.LookUpCall(_Records);

            UpdateLabels(hitCollection.Count());

            // save to a text file
            var thread = new Thread(() =>
            {
                Cursor.Current = Cursors.WaitCursor;
                SaveHitList(hitCollection.ToList());
            });
            thread.Start();
        }

        /// <summary>
        /// Marshall to the GUI thread if necessary.
        /// </summary>
        /// <param name="count"></param>
        private void UpdateLabels(int count)
        {
            float divisor = 1000;

            if (!InvokeRequired)
            {
                Cursor.Current = Cursors.Default;
                label2.Text = "Finished - hitcount = " + count.ToString();
                label1.Text = "Search Time: " + stopwatch.Elapsed;
                label3.Text = ((float)(stopwatch.ElapsedMilliseconds / divisor)).ToString() + " microseconds per call sign";
            }
            else
            {
                this.BeginInvoke(new Action<Int32>(this.UpdateLabels), count);
                return;
            }
        }

        /// <summary>
        /// Read entire call sign list but send in calls one at a time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSemiBatch_Click(object sender, EventArgs e)
        {
            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            stopwatch = Stopwatch.StartNew();
            Cursor.Current = Cursors.WaitCursor;
            Task.Run(() => SemiBatchCallSignLookup());
        }

        private void SemiBatchCallSignLookup()
        {
            IEnumerable<CallSignInfo> hitCollection;
            // need to preallocate space in collection
            List<CallSignInfo> hitList = new List<CallSignInfo>(5000000);
            int total = 0;

            stopwatch = Stopwatch.StartNew();

            foreach (string call in _Records)
            {
                total += 1;

                hitCollection = _CallLookUp.LookUpCall(call);

                if (hitCollection != null)
                {
                    hitList.AddRange(hitCollection);
                }
            }

            UpdateLabels(hitList.Count());
        }

        /// <summary>
        /// Parse the prefix file.
        /// </summary>
        /// <param name="filePath"></param>
        public void ParsePrefixFile(string filePath)
        {
            Cursor.Current = Cursors.WaitCursor;
            stopwatch = Stopwatch.StartNew();
            _PrefixFileParser.ParsePrefixFile("");
            Console.WriteLine("Load Time: " + stopwatch.ElapsedMilliseconds + "ms");
            Cursor.Current = Cursors.Default;
            _CallLookUp = new CallLookUp(_PrefixFileParser);
        }

        /// <summary>
        /// Batch lookup.
        /// </summary>
        /// <param name="callSigns"></param>
        /// <returns></returns>
        public IEnumerable<CallSignInfo> LookupCall(List<string> callSigns)
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
        private void SaveHitList(List<CallSignInfo> hitList)
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String file = Path.Combine(folderPath, "hits.csv");
            int lineCount = 5500;
            //List<CallSignInfo> sortedList = hitList.OrderBy(o => o.Country).ThenBy(x => x.Kind).ToList();

            using (TextWriter writer = new StreamWriter(file, false, System.Text.Encoding.UTF8))
            {
                var csv = new CsvWriter(writer);
     
                foreach (CallSignInfo callSignInfo in hitList)
                {

                    if (callSignInfo.Kind == PrefixKind.DXCC)
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
                    csv.WriteField(callSignInfo.MainPrefix);
                    csv.WriteField(callSignInfo.FullPrefix);
                    csv.NextRecord();

                    lineCount--;
                    if (lineCount <= 0)
                    {
                        break;
                    }
                }
            }

            Console.WriteLine("Finished writing file");
            UpdateCursor();
        }


        private void UpdateCursor()
        {
            if (!InvokeRequired)
            {
                Cursor.Current = Cursors.Default;
            }
            else
            {
                this.BeginInvoke(new Action(this.UpdateCursor));
                return;
            }
        }

    } // end class
}
