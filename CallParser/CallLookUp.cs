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
 CallLookUp.cs
 CallParser
 
 Created by Peter Bourget on 3/11/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Analyze a call sign and find its meta data using the call sign prefix.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    public class CallLookUp
    {
        private ConcurrentBag<Hit> _HitList;
        private readonly Dictionary<string, List<Hit>> _PrefixesDictionary;
        private readonly Dictionary<Int32, Hit> _Adifs;

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="prefixFileParser"></param>
        public CallLookUp(PrefixFileParser prefixFileParser)
        {
            this._PrefixesDictionary = prefixFileParser.PrefixesDictionary;
            _Adifs = prefixFileParser.Adifs;
        }

        /// <summary>
        /// Batch lookup of call signs. A List<string> of calls may be sent in
        /// and each processed in parallel. A limit of 1 million per batch is
        /// enforced as Windows cannot handle the memory requirements for larger
        /// collections.
        /// 
        /// Why return IEnumerable<Hit>.
        /// Does code that calls the method only expect to iterate over it? Return an IEnumerable.
        /// You shouldn't care about what the caller does with it, because the return type 
        /// clearly states what the returned value is capable of doing. Any caller that gets 
        /// an IEnumerable result knows that if they want indexed access of the result, 
        /// they will have to convert to a List, because IEnumerable simple isn't capable 
        /// of it until it's been enumerated and put into an indexed structure. 
        /// Don't assume what the callers are doing, otherwise you end up taking functionality 
        /// away from them. For example, by returning a List, you've taken away the ability to 
        /// stream results which can have its own performance benefits. Your implementation 
        /// may change, but the caller can always turn an IEnumerable into a List if they need to.
        /// </summary>
        /// <param name="callSigns"></param>
        /// <returns></returns>
        public IEnumerable<Hit> LookUpCall(List<string> callSigns)
        {
            _HitList = new ConcurrentBag<Hit>();

            Console.WriteLine("Callsigns: " + callSigns.Count.ToString());

            if (callSigns.Count > 1500000)
            {
                throw new Exception("To many entries. Please reduce the number of entries to 1.5 million or less.");
            }

            // parallel foreach almost twice as fast but requires blocking collection
            Parallel.ForEach(callSigns, callSign =>
            {
                if (ValidateCallSign(callSign))
                {
                    ProcessCallSign(callSign);
                }
                else
                {
                    // don't throw, just ignore bad calls
                    //Console.WriteLine("Invalid call sign format: " + callSign);
                }
            }
             );

            IEnumerable<Hit> allHits = _HitList.AsEnumerable();
            
            return allHits;
        }

        /// <summary>
        /// Look up a single call sign. First make sure it is a valid call sign.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        public IEnumerable<Hit> LookUpCall(string callSign)
        {
            _HitList = new ConcurrentBag<Hit>();

            if (ValidateCallSign(callSign))
            {
                ProcessCallSign(callSign);
            }
            else
            {
                //throw new Exception("Invalid call sign format"); // EMBELLISH
            }

            IEnumerable<Hit> allHits = _HitList.AsEnumerable();

            return allHits;
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
            if (callSign.IndexOf("/", 1, 1) == 1) { return false; }

            // check for a "-" ie: VE7CC-7, OH6BG-1, WZ7I-3 
            if (callSign.IndexOf("-") != -1) { return false; }

            // can't be all numbers
            if (IsNumeric(callSign)) { return false; }

            // look for at least one number character
            if (!callSign.Where(x => Char.IsDigit(x)).Any()) { return false; }

            return true;
        }

        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/W4
        /// </summary>
        /// <param name="callSign"></param>
        private void ProcessCallSign(string callSign)
        {
            (string call, string callPrefix) callAndprefix = ("", ""); // tuple
            List<string> components = callSign.Split('/').ToList();

            switch (components.Count)
            {
                case 1:
                    callAndprefix = (components[0], components[0]);
                    break;
                case 2:
                    callAndprefix = ProcessPrefix(components);
                    break;
                case 3: // DC3RJ/P/W3 - remove excess parts
                    callAndprefix = TrimCallSign(components);
                    break;
                default:
                    // should I do anything here?
                    Console.WriteLine("Too many pieces: " + callSign);
                    Debug.Assert(components.Count > 3);
                    break;
            }

            CollectMatches(callAndprefix);
        }

        /// <summary>
        /// If a call sign has 3 components delete the one we don't need. (W4/W6OP/P)
        /// </summary>
        /// <param name="components"></param>
        /// <param name="callSign"></param>
        /// <returns></returns>
        private (string call, string callPrefix) TrimCallSign(List<string> components)
        {
            int counter = 0;
            List<string> tempComponents = new List<string>();
            (string call, string callPrefix) callAndprefix = ("", "");

            callAndprefix.call = "";
            callAndprefix.callPrefix = "";

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

            callAndprefix = ProcessPrefix(tempComponents);

            return callAndprefix;
        }


        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/V31, W4/W6OP, SM0KAK/BY1QH (China)
        ///  ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:'
        /// 1. Eliminate any 2 number or 2 character prefixes
        /// 2. If prefix is same length as call, use prefix/suffix instead of call
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        private (string call, string callPrefix) ProcessPrefix(List<string> components)
        {
            string call = "";
            string prefix = "";
            // added "R" as a beacon for R/IK3OTW
            // "U" for U/K2KRG
            string[] rejectPrefixes = { "U", "R", "A", "B", "M", "P", "MM", "AM", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL" };

            // shortest
            prefix = components.OrderBy(c => c.Length).FirstOrDefault();
            // longest
            call = components.OrderBy(c => c.Length).Last();

            if (call.Length == prefix.Length)
            {
                // swap call and prefix
                call += prefix;
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

            return (call, callPrefix: prefix);
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

            // only use the first 4 characters - faster search you would think
            // but truncating the string has some overhead - more accurate result though
            callPart = callPart.Length > 3 ? callPart.Substring(0, 4) : callPart;

            if (_PrefixesDictionary.ContainsKey(callPart))
            {
                List<Hit> query = _PrefixesDictionary[callPart];

                foreach (Hit hit in query)
                {
                    _HitList.Add(hit);
                }

                // get the top level DXCC hit
                if (query.Count > 0)
                {
                    Hit dxccHit = _Adifs[Convert.ToInt32(query[0].Dxcc)];
                    dxccHit.CallSign = callAndprefix.call;
                    _HitList.Add(dxccHit);
                }
            }

            if (callPart.Length > 1)
            {
                callPart = callPart.Remove(callPart.Length - 1);
                while (callPart != string.Empty)
                {
                    if (_PrefixesDictionary.ContainsKey(callPart))
                    {
                        List<Hit> query = _PrefixesDictionary[callPart];

                        foreach (Hit hit in query)
                        {
                            _HitList.Add(hit);
                        }

                        // get the top level DXCC hit
                        if (query.Count > 0)
                        {
                            Hit dxccHit = _Adifs[Convert.ToInt32(query[0].Dxcc)];
                            dxccHit.CallSign = callAndprefix.call;
                            _HitList.Add(dxccHit);
                        }
                    }

                        callPart = callPart.Remove(callPart.Length - 1);
                }
            }
        }

        /// <summary>
        /// Check for non alpha numerics other than "/"
        /// DON'T USE THIS!
        /// REGEX IS A HUGE TIME SINK - more than doubles run time
        /// </summary>
        /// <param name="strToCheck"></param>
        /// <returns></returns>
        //private Boolean IsAlphaNumeric(string strToCheck)
        //{
        //    Regex rg = new Regex(@"^[a-zA-Z0-9/]*$");
        //    return rg.IsMatch(strToCheck);

        //    //return strToCheck.All(x => char.IsLetterOrDigit(x));
        //}

        /// <summary>
        /// Test if a string is numeric.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }
    } // end class
}
