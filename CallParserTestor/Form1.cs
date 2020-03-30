
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
using System.ComponentModel;
using System.Data;

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

        private Dictionary<string, string> DelphiCompoundKeyValuePairs;

        private List<string> CompoundCalls;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            PrefixFileParser prefixFileParser = new PrefixFileParser();
            _PrefixFileParser = prefixFileParser;
            DataGridViewResults.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

            CompoundCalls = new List<string>(500000);
        }

        /// <summary>
        /// Parse the prefix file. Marshallllll to another thread so I don't
        /// block the GUI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadPrefixFile_Click(object sender, EventArgs e)
        {
            UseWaitCursor = true;
            Task.Run(() => ParsePrefixFile(TextBoxPrefixFilePath.Text));
        }

        /// <summary>
        /// Read the file of call signs to test with. If the checkbox is checked
        /// load the compound call file instead of the Reverse Beacon Network (RBN) file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadCallSigns_Click(object sender, EventArgs e)
        {
            _Records = new List<string>();

            Cursor.Current = Cursors.WaitCursor;

            stopwatch = Stopwatch.StartNew();

            if (CheckBoxCompoundCalls.Checked)
            {
                // using (StreamReader reader = new StreamReader("compound.txt"))
                using (StreamReader reader = new StreamReader("PSKReporterCalls.txt"))
                {
                    while (!reader.EndOfStream)
                    {
                        _Records.Add(reader.ReadLine());
                    }
                }
            }
            else
            {
                using (StreamReader reader = new StreamReader("rbn2.csv"))
                using (var csv = new CsvReader(reader))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        csv.Configuration.MissingFieldFound = null;
                        _Records.Add(csv.GetField("dx").ToUpper()); //dx_pfx

                        // comment out to keep list to 1 million - most of the rest are dupes anyway
                        // I should use a HashSet to eliminate dupes
                        //string temp = csv.GetField("callsign");
                        //// check for a "_" ie: VE7CC-7, OH6BG-1, WZ7I-3 - remove the characters "-x"
                        //if (temp.IndexOf("-") != -1)
                        //{
                        //    temp = temp.Substring(0, temp.IndexOf("-"));
                        //}
                        //_Records.Add(temp.ToUpper());
                    }
                }
            }

            Console.WriteLine("Load Time: " + stopwatch.ElapsedMilliseconds + "ms");
            LabelCallsLoaded.Text = _Records.Count.ToString() + " total calls loaded";
            string compound = _Records.Where(g => g.Contains("/")).Distinct().Count().ToString();
            LabelCallsLoadedDistinct.Text = _Records.Distinct().Count().ToString() + " unique calls - (" + compound + " compound)";
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Look up a single call in the input text box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSingleCallLookup_Click(object sender, EventArgs e)
        {
            IEnumerable<CallSignInfo> hitCollection;
            List<CallSignInfo> hitList;

            // I3HG/SV7GUO
            // RW3WR/W
            // W/LE6
            // JA/6

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

                        // save to a text file - not necessary for a single call
                        //var thread = new Thread(() =>
                        //{
                        //    Cursor.Current = Cursors.WaitCursor;
                        //    SaveHitList(hitList);
                        //});
                        //    thread.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

            DataGridViewResults.DataSource = null;

            Cursor.Current = Cursors.WaitCursor;
            Task.Run(() => BatchCallSignLookup());
        }

        /// <summary>
        /// Build the hit collection by passing in a List<string>of call signs.
        /// </summary>
        private void BatchCallSignLookup()
        {
            IEnumerable<CallSignInfo> hitCollection;

            stopwatch = Stopwatch.StartNew();

            try
            {
                hitCollection = _CallLookUp.LookUpCall(_Records);
                
                UpdateLabels(hitCollection.Count());
                // fill the datagrid
                UpdateDataGrid(hitCollection);

                // superseded by SaveHitList(dt) in UpdateDataGrid(hitCollection)
                // save to a text file
                //var thread = new Thread(() =>
                //{
                //    Cursor.Current = Cursors.WaitCursor;
                //    SaveHitList(hitCollection.ToList());
                //});
                //thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Update the data grid back on the GUI thread.
        /// </summary>
        /// <param name="hitCollection"></param>
        private void UpdateDataGrid(IEnumerable<CallSignInfo> hitCollection)
        {
            DataTable dt = new DataTable();
            Cursor.Current = Cursors.WaitCursor;

            if (!InvokeRequired)
            {
                using (dt = new DataTable())
                {
                    dt.Columns.Add("CallSign");
                    dt.Columns.Add("Kind");
                    dt.Columns.Add("Country");
                    dt.Columns.Add("Province");
                    dt.Columns.Add("DXCC");

                    foreach (CallSignInfo oItem in hitCollection)
                    {
                        if (DelphiCompoundKeyValuePairs != null)
                        {
                            if (DelphiCompoundKeyValuePairs.ContainsKey(oItem.CallSign))
                            {
                                string country = DelphiCompoundKeyValuePairs[oItem.CallSign];
                                if (country != oItem.Country && oItem.Kind != PrefixKind.Province && string.IsNullOrEmpty(country)) //   && oItem.Kind != PrefixKind.InvalidPrefix
                                {
                                    dt.Rows.Add(new object[] { oItem.CallSign, oItem.Kind, oItem.Country, "Delphi: " + country, "" });
                                }
                            }
                        }
                        else
                        {
                            if (oItem.Kind == PrefixKind.DXCC ) // || oItem.Kind == PrefixKind.InvalidPrefix
                            {
                                dt.Rows.Add(new object[] { oItem.CallSign, oItem.Kind, oItem.Country, oItem.Province ?? "", oItem.DXCC.ToString() });
                            }
                            else
                            {
                                dt.Rows.Add(new object[] { "     ", oItem.Kind, oItem.Country, oItem.Province ?? "", oItem.DXCC.ToString() });
                            }
                        }
                    }

                    SaveHitList(dt);
                }
                try
                {
                    DataGridViewResults.DataSource = dt;
                }
                catch (Exception ex)
                {
                    string a = ex.Message;
                }
                finally
                {
                    UpdateCursor();
                }
            }
            else
            {
                this.BeginInvoke(new Action<IEnumerable<CallSignInfo>>(this.UpdateDataGrid), hitCollection);
                return;
            }
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
                }
                else
                {
                    item = new ListViewItem("--- " + call);
                    item.BackColor = Color.LightGray;
                }

                item.SubItems.Add(kind.ToString());
                item.SubItems.Add(country);
                item.SubItems.Add(province ?? "");
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

        /// <summary>
        /// Send in calls one at a time on anther thread.
        /// </summary>
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
           
            _CallLookUp = new CallLookUp(_PrefixFileParser);
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
                    csv.WriteField(callSignInfo.Province ?? "");
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

        /// <summary>
        /// Update the cursor on the GUI thread.
        /// </summary>
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

        /// <summary>
        /// Select the folder to load the prefix file from.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Handle button click to build compound call file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            LoadCompoundCompareFile();
            return;

            // 20110205.csv 20140406.csv 20140409.csv 20160207.csv 20200105.csv
            BuildCompoundCallFile(Path.Combine(@"C:\Users\pbourget\Downloads\Reverse beacon", "20110205.csv"));
            BuildCompoundCallFile(Path.Combine(@"C:\Users\pbourget\Downloads\Reverse beacon", "20140406.csv"));
            BuildCompoundCallFile(Path.Combine(@"C:\Users\pbourget\Downloads\Reverse beacon", "20140409.csv"));
            BuildCompoundCallFile(Path.Combine(@"C:\Users\pbourget\Downloads\Reverse beacon", "20160207.csv"));
            BuildCompoundCallFile(Path.Combine(@"C:\Users\pbourget\Downloads\Reverse beacon", "20200105.csv"));

            //int a = 1;
            // save to a text file
            var thread = new Thread(() =>
            {
                Cursor.Current = Cursors.WaitCursor;
                SaveHitList(CompoundCalls);
            });
            thread.Start();
        }

        /// <summary>
        /// Read and add to another file all compound calls.
        /// This was to test compound calls but has been superseded by using the PSKReporterCalls file.
        /// </summary>
        /// <param name="filePath"></param>
        private void BuildCompoundCallFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    csv.Configuration.MissingFieldFound = null;
                    string dx = csv.GetField("dx");
                    if (dx.Contains("/"))
                    {
                        CompoundCalls.Add(dx.ToUpper());
                    }
                }
            }
        }

        /// <summary>
        /// Load output from Delphi compound file test.
        /// </summary>
        private void LoadCompoundCompareFile()
        {
            DelphiCompoundKeyValuePairs = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(@"C:\Users\pbourget\Documents\DelphiCallParserTest.txt"))
            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    csv.Configuration.MissingFieldFound = null;
                    string callSign = csv.GetField("callsign");
                    string country = csv.GetField("country");
                    DelphiCompoundKeyValuePairs.Add(callSign, country.Trim());
                }
            }
        }

        /// <summary>
        /// Save the compound call list to a file.
        /// </summary>
        /// <param name="compoundCalls"></param>
        private void SaveHitList(List<string> compoundCalls)
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String file = Path.Combine(folderPath, "compound.txt");
            //int lineCount = 5500;

            List<string> compounds = CompoundCalls.Where(g => g.Contains("/")).Distinct().ToList();

            using (TextWriter writer = new StreamWriter(file, false, System.Text.Encoding.UTF8))
            {
                var csv = new CsvWriter(writer);

                foreach (string call in compounds)
                {
                    csv.WriteField(call);
                    csv.NextRecord();

                    //lineCount--;
                    //if (lineCount <= 0)
                    //{
                    //    break;
                    //}
                }
            }

            Console.WriteLine("Finished writing compound file");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        private void SaveHitList(DataTable table)
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String file = Path.Combine(folderPath, "CallParserList.txt");

            using (TextWriter writer = new StreamWriter(file, false, System.Text.Encoding.UTF8))
            {
                var csv = new CsvWriter(writer);

                foreach (DataRow row in table.Rows)
                {
                    csv.WriteField(row["CallSign"]);
                    csv.WriteField(row["Kind"]);
                    csv.WriteField(row["Country"]);
                    csv.WriteField(row["Province"]);
                    csv.WriteField(row["DXCC"]);
                    csv.NextRecord();

                }
            }

            Console.WriteLine("Finished writing hit file");
        }
    } // end class
}
