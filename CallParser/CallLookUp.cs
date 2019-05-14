﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CallParser
{
    public enum PrefixKind
    {
        pfNone,
        pfDXCC,
        pfProvince,
        pfStation,
        pfDelDXCC,
        pfOldPrefix,
        pfNonDXCC,
        pfInvalidPrefix,
        pfDelProvince,
        pfCity
    }

    public struct Hit
    {
        public string call;         //call sign as input
        public string prefix;       //what I determined the prefix to be - mostly for debugging
        public PrefixKind kind;     //kind
        public string country;       //country
        public string province;     //province
        public string city;         //city
        public string dxcc_entity;  //dxcc_entity
        public string cq;           //cq_zone
        public string itu;          //itu_zone
        public string continent;     //continent
        public string timeZone;     //time_zone
        public string latitude;     //lat
        public string longitude;    //long


        public Hit((string call, string callPrefix) callAndprefix, PrefixData prefixData)
        {
            call = callAndprefix.call;
            prefix = callAndprefix.callPrefix;
            kind = prefixData.kind;
            country = prefixData.country;
            province = prefixData.province;
            city = prefixData.city;
            dxcc_entity = prefixData.dxcc_entity;
            cq = prefixData.cq;
            itu = prefixData.itu;
            continent = prefixData.continent;
            timeZone = prefixData.timeZone;
            latitude = prefixData.latitude;
            longitude = prefixData.longitude;
        }
    }

    public class CallLookUp
    {
        private List<PrefixData> _PrefixList;
        private List<PrefixData> _ChildPrefixList;
        private List<Hit> _HitList;
        private HashSet<HashSet<string>> _CallSetList;

        private Dictionary<string, PrefixData> _PrefixDict;
        private Dictionary<string, List<PrefixData>> _ChildPrefixDict;

        public CallLookUp(PrefixFileParser prefixFilePareser)
        {
            _PrefixList = prefixFilePareser._PrefixList;
            _ChildPrefixList = prefixFilePareser._ChildPrefixList;
            _PrefixDict = prefixFilePareser._PrefixDict;
            _ChildPrefixDict = prefixFilePareser._ChildPrefixDict;
        }

        public List<Hit> LookUpCall(List<string> callSigns)
        {
            _HitList = new List<Hit>(2000000);

            var sw = Stopwatch.StartNew();

            _ = Parallel.ForEach(callSigns, callSign =>
            //foreach (string callSign in callSigns)
            {
                if (!string.IsNullOrEmpty(callSign))
                {
                    if (ValidateCallSign(callSign))
                    {
                        ProcessCallSign(callSign);
                    }
                    else
                    {
                        throw new Exception("Invalid call sign format"); // EMBELLISH
                    }
                }
            }
           );

            Console.WriteLine("Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks);

            return _HitList;
        }

        /// <summary>
        /// Look up a single call sign. 
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        public List<Hit> LookUpCall(string callSign)
        {
            _HitList = new List<Hit>();

            callSign = callSign.ToUpper();

            if (ValidateCallSign(callSign))
            {
                ProcessCallSign(callSign);
            }
            else
            {
                throw new Exception("Invalid call sign format"); // EMBELLISH
            }

            return _HitList;
        }

        /// <summary>
        /// Check for empty call.
        /// Check for no alpha characters.
        /// A call must be made up of only alpha, numeric and can have one or more "/".
        /// Must start with letter or number.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        private bool ValidateCallSign(string callSign)
        {
            // check for empty or null string
            if (string.IsNullOrEmpty(callSign)) { return false; }

            // check if first character is "/"
            if (callSign.IndexOf("/", 0, 1) == 0) { return false; }

            // check if second character is "/"
            if (callSign.IndexOf("/", 1, 1) == 0)
            {
                return false;
            }

            // can't be all numbers
            if (IsNumeric(callSign)) { return false; }

            // can't be all letters
            if (!IsAlphaNumeric(callSign)) { return false; }

            // look for at least one number character
            if (!callSign.Where(x => Char.IsDigit(x)).Any()) { return false; }

            return true;
        }

        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/V31
        /// </summary>
        /// <param name="callSign"></param>
        private void ProcessCallSign(string callSign)
        {
            (string call, string callPrefix) callAndprefix = ("", "");

            List<string> components = callSign.Split('/').ToList();

            switch (components.Count)
            {
                case 1:
                    callAndprefix.call = components[0];
                    callAndprefix.callPrefix = components[0];
                    CollectMatches(callAndprefix);
                    break;
                case 2:
                    callAndprefix = ProcessPrefix(callSign: callSign, components: components);
                    CollectMatches(callAndprefix);
                    break;
                case 3: // DC3RJ/P/W3 - remove excess parts
                    callAndprefix = TrimCallSign(components, callSign);
                    CollectMatches(callAndprefix);
                    break;
                default:
                    // should I do anything here?
                    break;
            }
        }

        /// <summary>
        /// If a call sign has 3 components delete the one we don't need.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="callSign"></param>
        /// <returns></returns>
        private (string call, string callPrefix) TrimCallSign(List<string> components, string callSign)
        {
            int counter = 0;
            List<string> tempComponents = new List<string>();
            (string call, string callPrefix) callAndprefix = ("", "");

            callAndprefix.call = "";
            callAndprefix.callPrefix = "";

            //Parallel.ForEach(components, component =>
            foreach (string component in components)
            {
                if (component.Length != 1)
                {
                    if (counter == 0)
                    {
                        counter += 1;
                        tempComponents.Add(component);
                    }
                    else
                    {
                        tempComponents.Add(component);
                    }
                }
            }
         //);

            callAndprefix = ProcessPrefix(callSign, tempComponents);

            return callAndprefix;
        }

        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/V31, W4/W6OP, SM0KAK/BY1QH (China)
        ///  ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:'
        /// 1. Eliminate any 2 number or 2 character prefixes
        /// 2. If prefix is same length as call, use prefix instead of call
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        private (string call, string callPrefix) ProcessPrefix(string callSign, List<string> components)
        {
            (string call, string callPrefix) callAndprefix = ("", "");
            string call = "";
            string prefix = "";
            // added "R" as a beacon for R/IK3OTW
            // "U" for U/K2KRG
            string[] rejectPrefixes = { "U", "R", "A", "B", "M", "P", "MM", "AM", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL" };

            // shortest
            prefix = components.OrderBy(c => c.Length).FirstOrDefault();
            // longest
            call = components.OrderBy(c => c.Length).Last();

            // DEBUG CODE
            //if (call == "IK3OTW" || prefix == "IK3OTW")
            //{
            //    Debug.Assert(prefix == "IK3OTW");
            //}

            if (call.Length == prefix.Length)
            {
                // swap call and prefix
                call = call + prefix;
                prefix = call.Substring(0, call.Length - prefix.Length);
                call = call.Substring(prefix.Length);
            }

            // should the prefix be tossed out
            if (Array.Find(rejectPrefixes, element => element == call) != null)
            {
                call = prefix;
            }

            if (Array.Find(rejectPrefixes, element => element == prefix) != null)
            {
                prefix = call;
            }

            if (prefix == string.Empty)
            {
                // collectMatches expects the prefix to be populated - that is what we search on
                Debug.Assert(prefix == string.Empty);
            }

            // call can be W4/LU2ART or LU2ART/W4 but make it W4/LU2ART
            if (!IsNumeric(prefix)) // AM70URE/8 --> 8/AM70URE
            {
                if (CheckExceptions(components))
                {
                    call = call + "/" + prefix;
                }
                else
                {
                    call = prefix + "/" + call;
                }
            }
            else
            {
                for (int i = 0; i < call.Length; i++)
                {
                    if (IsNumeric(call[i].ToString()))
                    {
                        call = call.Replace(call[i].ToString(), prefix);
                        prefix = call;
                        break;
                    }
                }
            }

            callAndprefix.call = call;
            callAndprefix.callPrefix = prefix;

            return callAndprefix;
        }

        /// <summary>
        /// Sometimes there are exceptions to the rule we can't handle in other places.
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>
        private bool CheckExceptions(List<string> components)
        {
            if (components[0].Length > 1)
            {
                switch (components[0].Substring(0, 2))
                {
                    case "BY":
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// First see if we can find a match for the max prefix of 4 characters.
        /// Then start removing characters from the back until we can find a match.
        /// Once we have a match we will see if we can find a child that is a better match.
        /// </summary>
        /// <param name="callAndprefix"></param>
        private void CollectMatches((string call, string callPrefix) callAndprefix)
        {
            string callPart = callAndprefix.callPrefix;

            callPart = callPart.Length > 3 ? callPart.Substring(0, 4) : callPart;

            //var sw = Stopwatch.StartNew();

            List<PrefixData> matches = new List<PrefixData>(); //_PrefixList.Where(p => p.mainPrefix == callPart).ToList();
            if (_PrefixDict.ContainsKey(callPart))
            {
                matches.Add(_PrefixDict[callPart]);
            }

            switch (matches.Count)
            {
                case 1:
                    PopulateHitList(matches[0], callAndprefix);
                    break;
                default:
                    if (callPart.Length > 1)
                    {
                        callPart = callPart.Remove(callPart.Length - 1);
                        while (matches.Count == 0)
                        {
                            if (_PrefixDict.ContainsKey(callPart))
                            {
                                matches.Add(_PrefixDict[callPart]);
                            }

                            callPart = callPart.Remove(callPart.Length - 1);
                            if (callPart == string.Empty)
                            {
                                //Console.WriteLine("No match: " + callAndprefix.callPrefix);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // debugging
                        Console.WriteLine("Single Character: " + callPart);
                    }

                    switch (matches.Count)
                    {
                        case 0:
                            // debugging
                            Console.WriteLine("Not Found: " + callPart);
                            //matches = SearchSecondaryPrefixes(callAndprefix: callAndprefix);


                            return;
                        default:
                            ProcessMatches(matches: matches, callAndprefix: callAndprefix);
                            break;
                    }
                    break;
            }
            // Console.WriteLine("Search Time: " + sw.ElapsedMilliseconds + "ms - ticks: " + sw.ElapsedTicks);
        }

        /// <summary>
        /// With one or matches look for children and see if we can narrow
        /// location down more.Create a Hitlist for the primary or DXCC
        /// entry. Add a hitlist for the most likely child.
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="callAndprefix"></param>
        private void ProcessMatches(List<PrefixData> matches, (string call, string callPrefix) callAndprefix)
        {
            _CallSetList = GetCallSetList(callAndprefix.call);

            //  _ = Parallel.ForEach(matches, match =>
            foreach (PrefixData match in matches)
            {
                PopulateHitList(match, callAndprefix);
            }
            //  _ = Parallel.ForEach(matches, match =>
            // );

        }

        private void ProcessChildren(List<PrefixData> children, (string call, string callPrefix) callAndprefix)
        {
            foreach (PrefixData child in children)
            {
                foreach (HashSet<HashSet<string>> mask in child.primaryMaskSets)
                {
                    if (CompareMask(mask, _CallSetList))  // UPDATE
                    {
                        PopulateHitList(child, callAndprefix);
                    }
                }
            }
        }

        private List<PrefixData> SearchSecondaryPrefixesOld((string call, string callPrefix) callAndprefix)
        {
            int maxCount = 0;
            bool match = false;
            List<PrefixData> matches = new List<PrefixData>();
            HashSet<HashSet<string>> callSetList;  // = GetCallSetList(callAndprefix.call);

            foreach (PrefixData prefixData in _PrefixList)
            {
                matches = new List<PrefixData>();
                callSetList = GetCallSetList(callAndprefix.call);

                if (prefixData.primaryMaskSets.Count > 1)
                {
                    // first find out which set is the smallest and we will only match that number a chars
                    var min = prefixData.primaryMaskSets.OrderBy(c => c.Count).FirstOrDefault(); // -------------------------------optimize
                    // get smallest int
                    maxCount = min.Count < callSetList.Count ? min.Count : callSetList.Count;

                    for (int i = 0; i < maxCount; i++)
                    {
                        try
                        {
                            // HashSet<string> temp = new HashSet<string>(callSetList[i]);
                            //temp.IntersectWith(min[i]);  // UPDATE

                            //if (temp.Count != 0)
                            //{
                            //    match = true;
                            //    //return match // is there any reason to continue here?
                            //    //found W4 do we need W4/ - however get 31 hits vs. 3
                            //}
                            //else
                            //{
                            //    match = false;
                            //    break;
                            //}

                            if (match)
                            {
                                matches.Insert(0, prefixData);
                            }
                        }
                        catch (Exception ex)
                        {
                            var a = ex.Message;
                        }
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// Search through the secondary prefixes.
        /// </summary>
        /// <param name="callAndprefix"></param>
        /// <returns></returns>
        private List<PrefixData> SearchSecondaryPrefixes((string call, string callPrefix) callAndprefix)
        {
            List<PrefixData> matches = new List<PrefixData>();
            string callPart = callAndprefix.callPrefix;

            callPart = callPart.Length > 3 ? callPart.Substring(0, 4) : callPart;

            if (_ChildPrefixDict.ContainsKey(callPart))
            {
                //var list = _ChildPrefixDict[callPart];
                // matches.Add(_ChildPrefixDict[callPart]);
                foreach (PrefixData prefixData in _ChildPrefixDict[callPart])
                {
                    matches.Add(prefixData);
                }
            }

            if (matches.Count == 0)
            {
                callPart = callPart.Remove(callPart.Length - 1);
                while (matches.Count == 0)
                {
                    if (_ChildPrefixDict.ContainsKey(callPart))
                    {
                        //var list = _ChildPrefixDict[callPart];
                        foreach (PrefixData prefixData in _ChildPrefixDict[callPart])
                        {
                            matches.Add(prefixData);
                        }
                        //matches.Add(_ChildPrefixDict[callPart]);
                        break;
                    }

                    callPart = callPart.Remove(callPart.Length - 1);
                    if (callPart == string.Empty)
                    {
                        Console.WriteLine("No match: " + callAndprefix.callPrefix);
                        break;
                    }
                }
            }




            //Parallel.ForEach(_PrefixList, prefixData =>
            //{
            //    int maxCount = 0;
            //    bool match = false;


            //    if (prefixData.primaryMaskSets.Count > 1)
            //    {
            //        maxCount = 0;
            //        match = false;
            //        matches = new List<PrefixData>();
            //        _CallSetList = GetCallSetList(callAndprefix.call);

            //        // first find out which set is the smallest and we will only match that number a chars
            //        var min = prefixData.primaryMaskSets.OrderBy(c => c.Count).FirstOrDefault(); // -------------------------------optimize
            //        maxCount = min.Count < _CallSetList.Count ? min.Count : _CallSetList.Count;

            //        for (int i = 0; i < maxCount; i++)
            //        {
            //            // UPDATE
            //            //HashSet<string> temp = new HashSet<string>(callSetList[i]); // ----------------------------optimize
            //            //temp.IntersectWith(min[i]);
            //            //if (temp.Count != 0)
            //            //{
            //            //    match = true;
            //            //    //return match // is there any reason to continue here?
            //            //    //found W4 do we need W4/ - however get 31 hits vs. 3
            //            //}
            //            //else
            //            //{
            //            //    match = false;
            //            //    break;
            //            //}

            //            if (match)
            //            {
            //                lock (matches) // only OK when you don't lock on it anywhere else
            //                {
            //                    matches.Insert(0, prefixData);
            //                }
            //            }
            //        }
            //    }
            //}
            //);

            return matches;
        }

        /// <summary>
        /// Look at the mask in every child of every parent.
        /// </summary>
        /// <param name="callAndprefix"></param>
        private void SearchChildren((string call, string callPrefix) callAndprefix)
        {
            HashSet<HashSet<string>> callSetList = GetCallSetList(callAndprefix.call);

            //if (_ChildPrefixDict.ContainsKey(callAndprefix.call))
            //{

            //}

            //foreach (PrefixData child in _ChildPrefixList)
            //{
            //    // this doubles run from 1 min to 2 min
            //    //foreach (List<HashSet<string>> mask in child.primaryMaskSets)
            //    {
            //        if (CompareMask(mask, callSetList))
            //        {
            //            PopulateHitList(child, callAndprefix);
            //        }
            //    }
            //}

            // MAYBE SHOULD PUT PRIMARYMASKSETS IN A DICTIONARY
            _ = Parallel.ForEach(_ChildPrefixList, child =>
              {
                  foreach (HashSet<HashSet<string>> mask in child.primaryMaskSets)
                  {
                      //if (CompareMask(mask, callSetList)) // UPDATE
                      //{
                      //    PopulateHitList(child, callAndprefix);
                      //}
                  }
              }
          );
        }

        /// <summary>
        /// Compare the mask with the Set created with the call sign.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="callSetList"></param>
        /// <returns></returns>
        private bool CompareMask(HashSet<HashSet<string>> mask, HashSet<HashSet<string>> callSetList)
        {
            int maxCount = 0;
            bool match = false;

            // UPDATE
            //List<HashSet<HashSet<string>>> list = new List<HashSet<HashSet<string>>>;
            ////{
            //list.Add(mask);
            //list.Add(callSetList);
            ////};

            ////// first find out which set is the smallest and we will only match that number a chars
            //var min = list.OrderBy(c => c.Count).FirstOrDefault();
            //maxCount = min.Count;

            //for (int i = 0; i < maxCount; i++)  
            //{
            //    HashSet<string> temp = new HashSet<string>(callSetList[i]);
            //    temp.IntersectWith(mask[i]);
            //    if (temp.Count != 0)
            //    {
            //        match = true;
            //        //return match // is there any reason to continue here?
            //        //found W4 do we need W4/ - however get 31 hits vs. 3
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}

            return match;
        }

        /// <summary>
        /// Create a Set from the call sign to do Set operations with.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private HashSet<HashSet<string>> GetCallSetList(string call)
        {
            string callPart = call;
            HashSet<string> callSet = new HashSet<string>();
            HashSet<HashSet<string>> callSetList = new HashSet<HashSet<string>>();

            callPart = callPart.Length > 3 ? callPart.Substring(0, 4) : callPart;

            // this needs to be the suffix if LU2ART/W4
            foreach (char item in callPart)
            {
                callSet = new HashSet<string>
                {
                    item.ToString()
                };
                callSetList.Add(callSet);
            }

            return callSetList;
        }

        /// <summary>
        /// Add to the HitList array if a match.
        /// </summary>
        /// <param name="prefixData"></param>
        /// <param name="callAndprefix"></param>
        private void PopulateHitList(PrefixData prefixData, (string call, string callPrefix) callAndprefix)
        {
            _HitList.Add(new Hit(callAndprefix, prefixData));
        }


        /// <summary>
        /// Check for non alpha numerics other than "/"
        /// </summary>
        /// <param name="strToCheck"></param>
        /// <returns></returns>
        private Boolean IsAlphaNumeric(string strToCheck)
        {
            Regex rg = new Regex(@"^[a-zA-Z0-9/]*$");
            return rg.IsMatch(strToCheck);
        }

        /// <summary>
        /// THIS IS DUPLICATE TO TWO CLASSES
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

    } // end class
}
