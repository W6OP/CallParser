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

namespace CallParser
{
    public class CallLookUp
    {
        private BlockingCollection<CallSignInfo> _HitList;
        private readonly Dictionary<string, CallSignInfo> _PrefixDict;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="prefixFileParser"></param>
        public CallLookUp(Dictionary<string, CallSignInfo> prefixDict1)
        {
            _PrefixDict = prefixDict1;
        }

        /// <summary>
        /// Batch lookup of call signs. A list of calls may be sent in
        /// and each processed in parallel.
        /// </summary>
        /// <param name="callSigns"></param>
        /// <returns></returns>
        public List<CallSignInfo> LookUpCall(List<string> callSigns)
        {
            // preallocate space I need plus padding - otherwise extremely slow with large collections
            _HitList = new BlockingCollection<CallSignInfo>(callSigns.Count + 100000);

            // parallel foreach almost twice as fast but rquires blocking collection
            _ = Parallel.ForEach(callSigns, callSign =>
            {
                callSign = callSign.ToUpper();

                if (!string.IsNullOrEmpty(callSign))
                {
                    if (ValidateCallSign(callSign))
                    {
                        ProcessCallSign(callSign);
                    }
                    else
                    {
                        // don't throw, just ignore bad calls
                        Console.WriteLine("Invalid call sign format: " + callSign);
                    }
                }
            }
           );

            _HitList.CompleteAdding();

            return _HitList.ToList();
        }

        /// <summary>
        /// Look up a single call sign. 
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        public BlockingCollection<CallSignInfo> LookUpCall(string callSign)
        {
            _HitList = new BlockingCollection<CallSignInfo>();

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
            if (callSign.IndexOf("/", 1, 1) == 0) { return false; }

            // check for a "-" ie: VE7CC-7, OH6BG-1, WZ7I-3 
            if (callSign.IndexOf("-") != -1) { return false; }

            // can't be all numbers
            if (IsNumeric(callSign)) { return false; }

            // can't be all letters
            // I don't think this is the correct check
            //if (!IsAlphaNumeric(callSign)) { return false; }

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
                    callAndprefix = ProcessPrefix(components);
                    CollectMatches(callAndprefix);
                    break;
                case 3: // DC3RJ/P/W3 - remove excess parts
                    callAndprefix = TrimCallSign(components);
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
        /// 2. If prefix is same length as call, use prefix instead of call
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        private (string call, string callPrefix) ProcessPrefix(List<string> components)
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
            List<CallSignInfo> matches = new List<CallSignInfo>();
            string callPart = callAndprefix.callPrefix;

            callPart = callPart.Length > 3 ? callPart.Substring(0, 4) : callPart;

            if (_PrefixDict.ContainsKey(callPart))
            {
                matches.Add(_PrefixDict[callPart]);
            }

            switch (matches.Count)
            {
                case 1:
                    ProcessMatches(matches, callAndprefix.call);
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
                            return;
                        default:
                            ProcessMatches(matches, callAndprefix.call);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// With one or matches look for children and see if we can narrow
        /// location down more.Create a Hitlist for the primary or DXCC
        /// entry. Add a hitlist for the most likely child.
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="callAndprefix"></param>
        private void ProcessMatches(List<CallSignInfo> matches, string call)
        {
            foreach (CallSignInfo match in matches)
            {
                match.CallSign = call;
                _HitList.Add(match);
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
