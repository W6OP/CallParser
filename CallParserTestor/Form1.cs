using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CallParser;
using CsvHelper;

namespace CallParserTestor
{
    public partial class Form1 : Form
    {
        //PrefixList _PrefixList;
        PrefixFileParser _PrefixFileParser;
        CallLookUp _CallLookUp;
        List<string> _Records;  // = new List<string>();


        public Form1()
        {
            InitializeComponent();

            _PrefixFileParser = new CallParser.PrefixFileParser();
            _PrefixFileParser.ParsePrefixFile("");
            _CallLookUp = new CallLookUp(_PrefixFileParser._PrefixList, _PrefixFileParser._ChildPrefixList, _PrefixFileParser._PrefixDict);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            List<Hit> hit;
           // string[] testCallSigns = {"TX9","/KH0PR", "W6OP/4", "OEM3SGU/3", "AM70URE/8", "5N31/OK3CLA", "BV100", "BY1PK/VE6LB", "DC3RJ/P/W3", "RAEM", "TX9", "AJ3M/BY1RX", "4D71/N0NM", "4X130RISHON", "9N38", "AX3GAMES", "DA2MORSE", "DB50FIRAC", "DL50FRANCE", "FBC5AGB", "FBC5NOD", "FBC5YJ", "FBC6HQP", "GB50RSARS", "HA80MRASZ", "HB9STEVE", "HG5FIRAC", "HG80MRASZ", "II050SCOUT", "IP1METEO", "J42004A", "J42004Q", "LM1814", "LM2T70Y", "LM9L40Y", "LM9L40Y/P", "OEM2BZL", "OEM3SGU", "OEM3SGU/3", "OEM6CLD", "OEM8CIQ", "OM2011GOOOLY", "ON1000NOTGER", "ON70REDSTAR", "PA09SHAPE", "PA65VERON", "PA90CORUS", "PG50RNARS", "PG540BUFFALO", "S55CERKNO", "TM380", "TYA11", "U5ARTEK/A", "V6T1", "VI2AJ2010", "VI2FG30", "VI4WIP50", "VU3DJQF1", "VX31763", "WD4", "XUF2B", "YI9B4E", "YO1000LEANY", "ZL4RUGBY", "ZS9MADIBA" };

            Cursor.Current = Cursors.WaitCursor;

            hit = new List<Hit>();
            foreach (string call in _Records)
            {
                try
                {
                   Console.WriteLine(call);
                   hit.Clear();
                   hit = _CallLookUp.LookUpCall(call);
                   Console.WriteLine(hit.Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(call);
                    Console.WriteLine(ex.Message);
                }
            }

            Cursor.Current = Cursors.Default;
            Console.WriteLine("Finished");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _Records = new List<string>();
            //string dx = null;

            Cursor.Current = Cursors.WaitCursor;
            using (StreamReader reader = new StreamReader("rbn2.csv"))
            using (var csv = new CsvReader(reader))
            {
                
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    // dx = csv.GetField("dx");CsvHelper.MissingFieldException
                    csv.Configuration.MissingFieldFound = null;
                    _Records.Add(csv.GetField("dx"));
                }
            }

            Cursor.Current = Cursors.Default;
        }
    } // end class
}
