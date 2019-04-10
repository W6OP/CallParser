using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CallParser
{
    public class PrefixFileParser
    {
        private readonly string DIGITS = "[0123456789]";
        private readonly string LETTERS = "[ABCDEFGHIJKLMNOPQRSTUVWXYZ]";
        private readonly string CHARS = "[0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ]";

        PrefixList _PrefixList;
        public List<PrefixInfo> PrefixEntryList { get; set; }

        private string _Callsign;
        internal string Callsign
        {
            get { return _Callsign; }
            set
            {
                _Callsign = value;
            }
        }

        /// <summary>
        /// Constructor;
        /// </summary>
        public PrefixFileParser()
        {
            //_PrefixList = new PrefixList
            //{
            //    PrefixFileName = "prefix.lst",
            //    CallFileName = "call.lst"
            //};

            //_PrefixList.LoadFiles();

            //PrefixEntryList = _PrefixList.PrefixEntryList;
        }

        public void ParsePrefixFile(string prefixFilePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            XDocument xDoc;

            if (File.Exists(prefixFilePath)) {

            } else
            {
                using (StreamReader stream = new StreamReader(assembly.GetManifestResourceStream("CallParser.PrefixList.xml")))
                {
                    xDoc = XDocument.Load(stream);
                }

                BuildPrefixDataList(xDoc);
            }
        }


        private void BuildPrefixDataList(XDocument xDoc)
        {
            PrefixData prefixData = new PrefixData();

            var children = xDoc.Root.Descendants("dx_atlas_prefixes")
           .Where(x => x.Name == "prefix")
           .Descendants();



            //XmlNodeList nodeList = xmlDoc.SelectNodes("dx_atlas_prefixes/prefix");
            //foreach (XmlNode prefix in nodeList)
            //{
            //    switch (prefix.ch.Name) {
            //        case "a":
            //            break;
            //    }
            //}


            }

        /// <summary>
        /// Entry point to the parser. We need to make sure the call is formatted correctly
        /// and is a valid call.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public List<PrefixInfo> GetCallInformation(string call)
        {
            List<PrefixInfo> hits = new List<PrefixInfo>();
          
            _Callsign = call;

            if (VerifyCallFormat())
            {
               hits = ResolveCall(call);
            }

            return hits;
        }

        /// <summary>
        /// Create an entry point other than the public property. Have it return
        /// a hit list, empty or one or more hits.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private bool VerifyCallFormat() // he has this return bool and has global var for call
        {
            Regex regex;
            List<CallInfo> callList = new List<CallInfo>();
            string[] endingPreserver = new string[] { "R", "P", "M" };
            string[] endingIgnore = new string[] { "AM", "MM", "QRP", "A", "B", "BCN", "LH" };

            // upper case and strip spaces
            _Callsign = string.Concat(_Callsign.Where(c => !char.IsWhiteSpace(c))).ToUpper();

            // ADIF mapping
            // prefixList = prefixList.Where(x => x.IndexOf("#", 0, 1) == -1).ToList();
            //callList = _CallList.Where(x => x.IndexOf(call, 0, 1) != -1).ToList();
            //bool has = _CallList.Any(cus => cus.CallSign == call);
            // see if the call is in the ADIF list in the call.lst file
            //if (_PrefixList.CallInfoList.Any(callObj => callObj.CallSign == _Callsign))
            //{
            //    callList = _PrefixList.CallInfoList.Where(t => t.CallSign == _Callsign).ToList();

            //    if (Int32.TryParse(callList[0].AdifCode, out adifCode))
            //    {
            //        _Callsign = "ADIF" + adifCode;
            //        return true;
            //    }
            //}

            if (_Callsign.IndexOf("/") != -1)
            {
                // if /MM return
                if (_Callsign.Substring(_Callsign.IndexOf("/")) == "/MM")
                {
                    return false;
                }
                // same for ANTartica
                if (_Callsign.Substring(_Callsign.IndexOf("/")) == "/ANT")
                {
                    _Callsign = "ADIF013";
                    return true;
                }

                // remove trailing /
                if (_Callsign.IndexOf("/", _Callsign.Length - 1) != -1)
                {
                    _Callsign = _Callsign.Substring(0, _Callsign.Length - 1);
                }
            }

            if (IsValidCall(_Callsign))
            {
                if (_Callsign.IndexOf("/") != -1)
                {
                    // remove known endings that are not prefixes - /MM /QRP etc.
                    regex = new Regex(@"(\/QRP\b|\/MM\b|\/AM\b|\/AA\b|\/A\b|\/BCN\b|\/B\b|\/LH\b|\/ANT\b)");
                    _Callsign = regex.Replace(_Callsign, "");
                }

                if (!CheckExceptions())
                {
                    return false;
                }

                if (!CheckWPXRules())
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool CheckWPXRules()
        {
            return true;
        }

        private bool CheckExceptions()
        {
            return true;
        }

        /// <summary>
        /// Determine if the call sign is in a valid format.
        /// Could probably do this in a regex
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        internal bool IsValidCall(string call)
        {
            bool isValid = true;

            // to short
            if (call.Length < 2)
            {
                isValid = false;
            }

            // can't have / as first character
            if (call.IndexOf("/", 0, 1) == 0)
            {
                isValid = false;
            }

            // can't have // in call
            if (call.IndexOf("//", 0) != -1)
            {
                isValid = false;
            }

            // make sure only alpanumeric and /
            // need testing for "/"
            Regex r = new Regex("^[A-Z0-9/]*$");
            if (!r.IsMatch(call))
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        ///  Find the item in the prefix list that matches
        ///  Might have to look at where number is to determine 2 or 3 prefix length
        /// </summary>
        /// <param name="call"></param>
        private List<PrefixInfo> ResolveCall(string call)
        {
            PrefixInfo prefixInfo = null;
            string prefix = call.Substring(0, 1);
            List<PrefixInfo> hits = new List<PrefixInfo>();
         
            // get the parent prefixinfo using the first letter of the call - this may return multiple parents
            List<PrefixInfo> parents = PrefixEntryList.Where(x => x.Prefix.StartsWith(prefix)).ToList();

            switch (parents.Count)
            {
                case 0:
                    // do nothing, null will be returned;
                    break;
                case 1: // only one so now look at children
                    if (parents[0].Children.Count > 0)
                    {
                        hits = FindChildPrefixInfo(parents[0], call);
                    }
                    else
                    {
                        hits.Add(parents[0]);
                    }
                    break;
                default: // multiple parents
                    prefixInfo = FindSingleParent(parents, call, 1);

                    if (prefixInfo.Children.Count > 0)
                    {
                        hits = FindChildPrefixInfo(parents[0], call);
                    }
                    else
                    {
                        hits.Add(prefixInfo);
                    }
                    break;
            }


            //List<PrefixInfo> temp = PrefixEntryList.Where(x => x.Territory.Contains("America")).ToList();
            //var list = parts.Where(d => filter.Vendors.Contains(d["Vendor"]));
            //var data = MyCollection.AsEnumerable().Where(x => ContainsSequence(x.ByteArrayValue, myStringToByteArray);
            return hits;
        }

        /// <summary>
        /// For each prefixinfo check for multiple letter match ie. W then WA then WA6 etc.
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        private PrefixInfo FindSingleParent(List<PrefixInfo> parents, string call, int count)
        {
            string prefix = null; 

            while (parents.Count > 1) // need to use mask
            {
                count++;
                prefix = call.Substring(0, count);
                parents = PrefixEntryList.Where(x => x.Prefix.StartsWith(prefix)).ToList();
            }

            return parents[0];
        }

        /// <summary>
        /// Need to look through the mask to find correct child
        /// may iterate through chidren first, only use mask if necessary
        /// </summary>
        /// <param name="parentPrefixInfo"></param>
        /// <returns></returns>
        private List<PrefixInfo> FindChildPrefixInfo(PrefixInfo parentPrefixInfo, string call)
        {
            Regex regex = null;
            Match match = null;
            string parentTerritory = parentPrefixInfo.Territory;
            string prefix = call.Substring(0, 2);
            string mask = "";
            string[] maskList = null;
            List<PrefixInfo> hits = new List<PrefixInfo>();

            // first lets do the easy search, just by first two characters of call sign
            List<PrefixInfo> children = parentPrefixInfo.Children.Where(x => x.Prefix.StartsWith(prefix) && x.IsValidPefixType == true && x.Mask != String.Empty).ToList();
            
            mask = ConvertMask(parentPrefixInfo.Mask);
            maskList = mask.Split(',');

            switch (children.Count)
            {
                case 0: // did not find child with 2 character prefix use mask 
                   foreach (string rx in maskList)
                    {
                        regex = new Regex(rx);
                        match = regex.Match(prefix);
                        if (match.Success)
                        {
                            hits.Add(parentPrefixInfo);
                            break;
                        }
                    }
                    break;
                case 1:
                    // for USA move state to province and add United States as territory
                    // test for other countries
                    children[0].Province = children[0].Territory;
                    children[0].Territory = parentTerritory;
                    hits.Add(children[0]);
                    break;
                default: // multiple children
                    foreach (PrefixInfo child in children)
                    {
                        if (prefix == child.Prefix)
                        {
                            hits.Add(child);
                        }
                        //foreach (string rx in maskList)
                        //{
                        //    regex = new Regex(rx);
                        //    match = regex.Match(child.Prefix);
                        //    if (match.Success)
                        //    {
                        //        hits.Add(child);
                        //        break;
                        //    }
                        //}
                    }
                    break;
            }

            return hits;
        }

        /// <summary>
        /// Turn the mask into full regular expressions
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private string ConvertMask(string mask)
        {
            mask = mask.Replace("#", DIGITS);
            mask = mask.Replace("@", LETTERS);
            mask = mask.Replace("?", CHARS);

            return mask;
        }


        /*
          switch (parents.Count)
            {
                case 0:
                    // do nothing, null returned
                    break;

                case 1:
                    if (parents[0].Children.Count == 0)
                    {
                        info = parents[0];
                    }
                    else
                    {
                        //foreach (PrefixInfo child in parents[0].Children)
                        //{
                        maskList = parents[0].Mask.Split(',');
                        foreach (string piece in maskList)
                        {
                            mask = piece.Substring(0, 1);
                            switch (mask)
                            {
                                case "@":
                                    result = LETTERS;
                                    break;
                                case "#":
                                    result = DIGITS;
                                    break;
                                case "?":
                                    result = CHARS;
                                    break;
                                case "[": // find the "]"


                                    break;
                                default:
                                    result = mask;
                                    break;
                            }
                        }

                        prefix = call.Substring(0, 2);
                        List<PrefixInfo> children = parents[0].Children.Where(x => x.Prefix.StartsWith(prefix)).ToList();
                        var a = 1;
                        //}
                    }
                    break;
                default: // more than one parent found

                    break;
            } 
         */


        /// <summary>
        /// Find the item in the prefix list that matches
        /// </summary>
        //private void ResolveCall(string call)
        //{
        //    Int32 adf = 0;

        //    if (call.IndexOf("ADIF", 0, 4) != -1)
        //    {
        //        // get all numerics after ADIF
        //        if (call.Length > 4)
        //        {
        //            // test if numeric here
        //            if (Int32.TryParse(call.Substring(4), out adf))
        //            {
        //                if (adf != 0)
        //                {
        //                    // hit list[0] = GetAdifItem();
        //                }
        //            }
        //        }
        //    }
        //}

    } //end class
}
