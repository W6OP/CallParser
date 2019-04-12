﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CallParser;

namespace CallParserTestor
{
    public partial class Form1 : Form
    {
        //PrefixList _PrefixList;
        PrefixFileParser _PrefixFileParser;
        CallLookUp _CallLookUp;
        public Form1()
        {
            InitializeComponent();

            _PrefixFileParser = new CallParser.PrefixFileParser();
            _PrefixFileParser.ParsePrefixFile("");
            _CallLookUp = new CallLookUp(_PrefixFileParser._PrefixList, _PrefixFileParser._ChildPrefixList);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            

            _CallLookUp.LookUpCall("fo/kh0pr");
           // _PrefixFileParser.ParsePrefixFile("");
           //_CallParser = _PrefixList.LoadFiles();
           //List<PrefixInfo> hitList =  _PrefixFileParser.GetCallInformation(TextBoxCall.Text.Trim());
           /*
             obj.Call := '9m6ro/8';
 obj.Call := 'fo/kh0pr';
 obj.Call := 'hc8/g8ofq';
 obj.Call := 'hp0int/2';
 obj.Call := 'fo/ut6ud';
 obj.Call := 'hc2/rc5a';
 obj.Call := 'kh2/g3zem';
 obj.Call := 'v5/dj2hd';
 obj.Call := 'vp5/w5cw';
 obj.Call := 'aa4vk/cy0';
 obj.Call := 'tx6t/p';
 obj.Call := 'f/tu5kg';
            */
        }
    } // end class
}
