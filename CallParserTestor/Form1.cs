﻿
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
using System.Drawing;

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
        private void ButtonLoadPrefixFile_Click(object sender, EventArgs e)
        {
            UseWaitCursor = true;
            Task.Run(() => ParsePrefixFile(TextBoxPrefixFilePath.Text));
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
            LabelCallsLoaded.Text = _Records.Count.ToString() + " calls loaded";
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

            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missng Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            ListViewResults.Items.Clear();
            LabelCallsLoaded.Text = "";

            try
            {
                if (!String.IsNullOrEmpty(TextBoxCall.Text))
                {
                    stopwatch = Stopwatch.StartNew();

                    hitCollection = _CallLookUp.LookUpCall(TextBoxCall.Text);

                    if (hitCollection != null)
                    {
                        hitList = hitCollection.ToList();   //.OrderByDescending(o => o.DXCC).ThenByDescending(x => x.Kind).ToList();
                        
                        Console.WriteLine(hitList.Count.ToString() + " hits returned");
                        LabelHitCount.Text = "Finished - hitcount = " + hitList.Count.ToString();

                        foreach (CallSignInfo hit in hitList)
                        {
                            UpdateListViewResults(hit.CallSign, hit.Kind, hit.Country, hit.Province, hit.DXCC.ToString());
                        }

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
        /// 
        /// Why return IEnumerable<CallSignInfo>.
        /// Do you only expect to iterate over it? Return an IEnumerable.
        /// I shouldn't care about what the caller does with it, because the return type 
        /// clearly states what the returned value is capable of doing. Any caller that gets 
        /// an IEnumerable result knows that if they want indexed access of the result, 
        /// they will have to convert to a List, because IEnumerable simple isn't capable 
        /// of it until it's been enumerated and put into an indexed structure. 
        /// I don't assume what the callers are doing, otherwise I end up taking functionality 
        /// away from them. For example, by returning a List, I would have taken away the ability to 
        /// stream results which can have its own performance benefits. My implementation 
        /// may change, but the caller can always turn an IEnumerable into a List if they need to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonBatchCallSignLookup_Click(object sender, EventArgs e)
        {
            if (_CallLookUp == null)
            {
                MessageBox.Show("Please load the prefix file before doing a lookup.", "Missing Prefix file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            ListViewResults.Items.Clear();
            Cursor.Current = Cursors.WaitCursor;
            Task.Run(() => BatchCallSignLookup());
        }

        /// <summary>
        /// 
        /// </summary>
        private void BatchCallSignLookup()
        {
            IEnumerable<CallSignInfo> hitCollection;
            int count = 0;

            stopwatch = Stopwatch.StartNew();

            hitCollection = _CallLookUp.LookUpCall(_Records);

            UpdateLabels(hitCollection.Count());

            foreach (CallSignInfo hit in hitCollection)
            {
                count++;
                UpdateListViewResults(hit.CallSign, hit.Kind, hit.Country, hit.Province, hit.DXCC.ToString());
                if (count > 200) // runaway limit
                {
                    break;
                }
            }

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

                LabelElapsedTime.Text = "";
                LabelHitCount.Text = "";
                LabelPerCallTime.Text = "";

                LabelHitCount.Text = "Finished - hitcount = " + count.ToString();
                LabelElapsedTime.Text = "Total Time: " + stopwatch.Elapsed;
                LabelPerCallTime.Text = ((float)(stopwatch.ElapsedMilliseconds / divisor)).ToString("#0.0000") + " us per call sign";
            }
            else
            {
                this.BeginInvoke(new Action<Int32>(this.UpdateLabels), count);
                return;
            }
        }

        /// <summary>
        /// Prevent cross thread calls.
        /// UpdateListViewResults(call, hit.Kind.ToString(), hit.Country, hit.Province, "new", "");
        /// UpdateListViewResults(call, hit.Kind, hit.Country, hit.Province, "old", "");
        /// </summary>
        /// <param name="call"></param>
        /// <param name="clear"></param>
        private void UpdateListViewResults(string call, PrefixKind kind, string country, string province, string dxcc)
        {
            if (!InvokeRequired)
            {
                ListViewItem item;

                if (kind == PrefixKind.DXCC)
                {
                    item = new ListViewItem(call);
                    item.BackColor = Color.Honeydew;
                } else
                {
                    item = new ListViewItem("--- " + call);
                    item.BackColor = Color.LightGray;
                }
                
                item.SubItems.Add(kind.ToString());
                item.SubItems.Add(country);
                item.SubItems.Add(province);
                item.SubItems.Add(dxcc);
                ListViewResults.Items.Add(item);
                Application.DoEvents();
            }
            else
            {
                this.BeginInvoke(new Action<string, PrefixKind, string, string, string>(this.UpdateListViewResults), call, kind, country, province, dxcc);
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

            ListViewResults.Items.Clear();
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
        private void ParsePrefixFile(string filePath)
        {
            stopwatch = Stopwatch.StartNew();
            _PrefixFileParser.ParsePrefixFile(TextBoxPrefixFilePath.Text);
            Console.WriteLine("Load Time: " + stopwatch.ElapsedMilliseconds + "ms");
            UpdateCursor();
            //UseWaitCursor = false;
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
                        csv.WriteField(callSignInfo.SearchPrefix);
                    }
                    else
                    {
                        csv.WriteField("----  " + callSignInfo.SearchPrefix);
                    }
                    csv.WriteField(callSignInfo.Country);
                    csv.WriteField(callSignInfo.Province);
                    csv.WriteField(callSignInfo.Kind.ToString());
                    csv.WriteField(callSignInfo.Latitude);
                    csv.WriteField(callSignInfo.Longitude);
                    csv.WriteField(callSignInfo.SearchPrefix);
                    csv.WriteField(callSignInfo.PortablePrefix);
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
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;
            }
            else
            {
                this.BeginInvoke(new Action(this.UpdateCursor));
                return;
            }
        }

        private void ButtonSelectFolder_Click(object sender, EventArgs e)
        {
            if (OpenPrefixFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(OpenPrefixFileDialog.FileName))
                {
                    TextBoxPrefixFilePath.Text = OpenPrefixFileDialog.FileName;
                }
            }
         }
    } // end class
}
