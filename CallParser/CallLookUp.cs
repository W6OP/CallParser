
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
        private ConcurrentBag<CallSignInfo> HitList;
        private readonly Dictionary<string, HashSet<CallSignInfo>> CallSignDictionary;
        private SortedDictionary<int, CallSignInfo> Adifs { get; set; }
        private readonly Dictionary<string, int> PortablePrefixes;
        private readonly string[] _OneLetterSeries = { "B", "F", "G", "I", "K", "M", "N", "R", "W", "2" };



        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="prefixFileParser"></param>
        public CallLookUp(PrefixFileParser prefixFileParser)
        {
            //this._PrefixesDictionary = prefixFileParser.PrefixesDictionary;
            this.CallSignDictionary = prefixFileParser.CallSignDictionary;
            Adifs = prefixFileParser.Adifs;
            PortablePrefixes = prefixFileParser.PortablePrefixes;
        }

        /// <summary>
        /// Batch lookup of call signs. A List<string> of calls may be sent in
        /// and each processed in parallel. A limit of 1 million per batch is
        /// enforced as Windows cannot handle the memory requirements for larger
        /// collections (32 bit).
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
        /// <returns>IEnumerable<CallSignInfo></returns>
        public IEnumerable<CallSignInfo> LookUpCall(List<string> callSigns)
        {
            HitList = new ConcurrentBag<CallSignInfo>();

            Console.WriteLine("Callsigns: " + callSigns.Count.ToString());

            //if (!Environment.Is64BitProcess && callSigns.Count > 1500000)
            //{
            //    throw new Exception("To many entries. Please reduce the number of entries to 1.5 million or less.");
            //}

            // parallel foreach almost twice as fast but requires blocking collection
            // need to use non parallel foreach for debugging
             _ = Parallel.ForEach(callSigns, callSign =>
            //foreach (string callSign in callSigns)
            {
                if (ValidateCallSign(callSign))
                {
                    try
                    {
                        ProcessCallSign(callSign);
                    }
                    catch (Exception)
                    {
                        // bury exception
                        Console.WriteLine("Invalid call sign format: " + callSign);
                    }
                }
                else
                {
                    // don't throw, just ignore bad calls
                    Console.WriteLine("Invalid call sign format: " + callSign);
                }
            }
             );

            return HitList.AsEnumerable();
        }

        /// <summary>
        /// Look up a single call sign. First make sure it is a valid call sign.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns>IEnumerable<CallSignInfo></returns>
        public IEnumerable<CallSignInfo> LookUpCall(string callSign)
        {
            HitList = new ConcurrentBag<CallSignInfo>();

            if (ValidateCallSign(callSign))
            {
                try
                {
                    ProcessCallSign(callSign);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                // don't throw, just ignore bad calls
                Console.WriteLine("Invalid call sign format: " + callSign);
            }

            return HitList.AsEnumerable();
        }


        /// <summary>
        /// Check for empty call.
        /// Check for no alpha only calls.
        /// A call must be made up of only alpha, numeric and can have one or more "/".
        /// Must start with letter or number.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        private bool ValidateCallSign(string callSign)
        {
            // check for empty or null string
            if (string.IsNullOrEmpty(callSign)) { return false; }

            // calls must be at least 2 characters
            if (callSign.Length < 2) { return false; }

            // check if first character is "/"
            //if (callSign.IndexOf("/", 0, 1) == 0) { return false; }

            // check for a "-" ie: VE7CC-7, OH6BG-1, WZ7I-3 
            if (callSign.IndexOf("-") != -1) { return false; }

            // can't be all numbers
            if (callSign.All(char.IsDigit)) { return false; }

            // look for at least one number character
            if (!callSign.Any(char.IsDigit)) { return false; }

            return true;
        }

        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/W4
        /// Call signs in the international series are formed as indicated in Nos. 19.51to 19.71. 
        /// The first two characters shall be two letters or a letter followed by a digit or a digit followed by a letter.
        /// The first two characters or in certain cases the first character of a call sign constitute the nationality identification.
        /// 
        /// For call sign series beginning with B, F, G, I, K, M, N, R, W and 2, only the first character is required for nationality identification. 
        /// In the cases of half series (i.e. when the first two characters are allocated to more than one Member State), 
        /// the first three characters are required for nationality identification.
        /// </summary>
        /// <param name="callSign"></param>
        private void ProcessCallSign(string callSign)
        {
            (string baseCall, string callPrefix) callAndprefix;

            // strip leading or trailing "/"
            if (callSign.First() == '/')
            {
                callSign = callSign.Substring(1);
            }

            if (callSign.Last() == '/')
            {
                callSign = callSign.Remove(callSign.Length - 1, 1);
            }

            List<string> components = callSign.Split('/').ToList();

            switch (components.Count)
            {
                case 1:
                    callAndprefix = (components[0], components[0]);
                    CollectMatches(callAndprefix, callSign);
                    break;
                case 2:
                    callAndprefix = TrimCallSign(components);
                    CollectMatches(callAndprefix, callSign);
                    break;
                case 3: // DC3RJ/P/W3 - remove excess parts
                    callAndprefix = TrimCallSign(components);
                    CollectMatches(callAndprefix, callSign);
                    break;
                default:
                    // should I do anything here?
                    Console.WriteLine("Too many pieces: " + callSign);
                    Debug.Assert(components.Count > 3);
                    break;
            }
        }

        /// <summary>
        /// If a call sign has 2 or more components look at each component
        /// and see if some should be removed
        /// </summary>
        /// <param name="components"></param>
        /// <param name="callSign"></param>
        /// <returns>(string call, string callPrefix)</returns>
        private (string baseCall, string callPrefix) TrimCallSign(List<string> components)
        {
            //string tempComponents1;
            //string tempComponents2;
            List<string> tempComponents = new List<string>();
            (string baseCall, string callPrefix) callAndprefix = ("", "");
            // added "R" as a beacon for R/IK3OTW
            // "U" for U/K2KRG
            string[] rejectPrefixes = { "U", "R", "A", "B", "M", "P", "MM", "AM", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL" };

            foreach (string component in components)
            {
                if (!rejectPrefixes.Contains(component))
                {
                    tempComponents.Add(component);
                }
            }

            if (tempComponents.Count == 1) { return (baseCall: tempComponents[0], callPrefix: tempComponents[0]); }

            if (tempComponents.Count > 2)
            {
                throw new Exception("Call sign has too many components.");
            }

            // what if single digit?  IT9RGY/4 ??
            // replace digit in call with it 
            // of course this only works when a single digit is in the call
            if (tempComponents[0].Length == 1 && int.TryParse(tempComponents[0], out int _))
            {
                string result = new String(tempComponents[1].Where(x => Char.IsDigit(x)).ToArray());
                tempComponents[1] = tempComponents[1].Replace(result, tempComponents[0]);
                tempComponents[0] = tempComponents[1];
            }

            if (tempComponents[1].Length == 1 && int.TryParse(tempComponents[1], out int _))
            {
                try
                {
                    string result = new String(tempComponents[0].Where(x => Char.IsDigit(x)).ToArray());
                    tempComponents[0] = tempComponents[0].Replace(result, tempComponents[1]);
                    tempComponents[1] = tempComponents[0];
                }
                catch (Exception) // WAW/4
                {
                    throw;
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
        /// <returns>(string call, string callPrefix)</returns>
        private (string baseCall, string callPrefix) ProcessPrefix(List<string> components)
        {
            TriState state = TriState.None;
            string component1;
            string component2;
            // added "R" as a beacon for R/IK3OTW
            // "U" for U/K2KRG
            string[] rejectPrefixes = { "U", "R", "A", "B", "M", "P", "MM", "AM", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL" };

            component1 = components[0];
            component2 = components[1];


            if (rejectPrefixes.Contains(component1)) { return (baseCall: component2, callPrefix: component2); }
            if (rejectPrefixes.Contains(component2)) { return (baseCall: component1, callPrefix: component1); }

            // is this a portable prefix?
            if (PortablePrefixes.ContainsKey(component1 + "/")) { return (baseCall: component2, callPrefix: component1 + "/"); }

            /*
             //resolve ambiguities
              FStructure := StringReplace(FStructure, 'UU', 'PC', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'CU', 'CP', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'UC', 'PC', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'UP', 'CP', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'PU', 'PC', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'U',   'C', [rfReplaceAll]);
             
             */

            TriState component1State = IsCallSignOrPrefix(component1);
            TriState component2State = IsCallSignOrPrefix(component2);

            switch (state)
            {
                case TriState _ when component1State == TriState.CallSign && component2State == TriState.Prefix:
                    return (baseCall: component1, callPrefix: component2);

                case TriState _ when component1State == TriState.Prefix && component2State == TriState.CallSign:
                    return (baseCall: component2, callPrefix: component1);
                // 'UU', 'PC'
                case TriState _ when component1State == TriState.None && component2State == TriState.None:
                    return (baseCall: component1, callPrefix: component2);
                // 'CU', 'CP'
                case TriState _ when component1State == TriState.CallSign && component2State == TriState.None:
                    return (baseCall: component1, callPrefix: component2);
                // 'UC', 'PC'
                case TriState _ when component1State == TriState.None && component2State == TriState.CallSign:
                    return (baseCall: component1, callPrefix: component2);
                // 'UP', 'CP'
                case TriState _ when component1State == TriState.None && component2State == TriState.Prefix:
                    return (baseCall: component1, callPrefix: component2);
                // 'PU', 'PC'
                case TriState _ when component1State == TriState.Prefix && component2State == TriState.None:
                    return (baseCall: component1, callPrefix: component2);
                // 'U',  'C'
                case TriState _ when component1State == TriState.Prefix && component2State == TriState.None:
                    return (baseCall: component1, callPrefix: component1);
                // 'C', 'C' BU VU3 VU7
                case TriState _ when component1State == TriState.CallSign && component2State == TriState.CallSign:
                    if (component1.First() == 'B')
                    {
                        return (baseCall: component2, callPrefix: component1);
                    }
                    else if (component1.StartsWith("VU4") || component1.StartsWith("VU7"))
                    {
                        return (baseCall: component2, callPrefix: component1);
                    }
                    break;

                case TriState _ when component1State == TriState.CallSign && component2State == TriState.CallOrPrefix:
                    return (baseCall: component1, callPrefix: component2);

                case TriState _ when component1State == TriState.CallOrPrefix && component2State == TriState.Prefix:
                    return (baseCall: component1, callPrefix: component2);

                default:
                    break;
            }

            return (baseCall: component1, callPrefix: component2);
        }

        /// <summary>
        /// //one of "@","@@","#@","#@@" followed by 1-4 digits followed by 1-6 letters
        /// ValidPrefixes = ':@:@@:@@#:@@#@:@#:@#@:@##:#@:#@@:#@#:#@@#:';
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private TriState IsCallSignOrPrefix(string candidate)
        {
            string[] validCalls = { "@", "@@", "@#@@", "@#@@@", "#@", "#@@", "#@#@", "#@#@@", "#@#@@@", "#@#@@@@", "#@#@@@@@", "@@#", "@@#@", "@@#@@", "@@#@@@" }; // KH6Z
            string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };
            string[] validPrefixOrCall = { "@@#@", "@#@" };
            TriState state = TriState.None;

            string pattern = BuildPattern(candidate);

            switch (state)
            {
                case TriState _ when (validPrefixOrCall.Contains(pattern)):
                    return TriState.CallOrPrefix;

                case TriState _ when (validCalls.Contains(pattern)):
                    return TriState.CallSign;

                case TriState _ when (validPrefixes.Contains(pattern)):
                    return TriState.Prefix;
            }

            return TriState.None;
        }

        ///// <summary>
        ///// //one of valid patterns of portable prefixes, and prefix is known
        ///// ValidPrefixes = ':@:@@:@@#:@@#@:@#:@#@:@##:#@:#@@:#@#:#@@#:';
        ///// </summary>
        ///// <param name="candidate"></param>
        ///// <returns></returns>


        private string BuildPattern(string candidate)
        {
            string pattern = "";

            foreach (char item in candidate)
            {
                if (Char.IsLetter(item))
                {
                    pattern += "@";
                }
                else
                {
                    pattern += "#";
                }
            }

            return pattern;
        }

        /// <summary>
        /// First see if we can find a match for the full prefix. I don't use the call in the tuple
        /// but pass it in for future use. If there is not a match start removing characters from the 
        /// back until we can find a match.
        /// Once we have a match we will see if we can find a child that is a better match. Also get the
        /// DXCC parent if this is not a top level entity.
        /// Need to clone (shallow copy) each object so the full callsign is assigned correctly to each hit.
        /// </summary>
        /// <param name="callOrPrefix"></param>
        /// /// <param name="fullCall"></param>
        private void CollectMatches((string baseCall, string callPrefix) callAndprefix, string fullCall)
        {
            string searchTerm = callAndprefix.callPrefix;
            string baseCall = callAndprefix.baseCall;

            // check for portable prefixes
            // this will catch G/, W/, W4/, VU@@/ VU4@@/
            if (PortablePrefixes.ContainsKey(searchTerm))
            {
                CallSignInfo callSignInfo = Adifs[PortablePrefixes[searchTerm]];
                CallSignInfo callSignInfoCopy = callSignInfo.ShallowCopy();
                callSignInfoCopy.CallSign = fullCall;
                callSignInfoCopy.BaseCall = baseCall;
                callSignInfoCopy.SearchPrefix = searchTerm; // this needs correction
                HitList.Add(callSignInfoCopy);

                if (callSignInfo.Kind != PrefixKind.DXCC)
                {
                    callSignInfo = Adifs[callSignInfo.DXCC];
                    CallSignInfo callSignInfoCopyDxcc = callSignInfo.ShallowCopy();
                    callSignInfoCopyDxcc.CallSign = fullCall;
                    callSignInfoCopyDxcc.BaseCall = baseCall;
                    callSignInfoCopyDxcc.SearchPrefix = searchTerm; // this needs correction
                    HitList.Add(callSignInfoCopyDxcc);
                }
                return;
            }

            // is the full call in the dictionary
            if (CallSignDictionary.ContainsKey(searchTerm))
            {
                List<CallSignInfo> query = CallSignDictionary[searchTerm].ToList();

                foreach (CallSignInfo callSignInfo in query.Where(x => x.PrefixKey.Contains(searchTerm)))
                {
                    CallSignInfo callSignInfoCopy = callSignInfo.ShallowCopy();
                    callSignInfoCopy.CallSign = fullCall;
                    callSignInfoCopy.BaseCall = baseCall;
                    callSignInfoCopy.SearchPrefix = searchTerm;
                    HitList.Add(callSignInfoCopy);

                    // this should be refactored to get it out of his foreach loop - 
                    // is there ever more than one callSigninfo for the query?
                    if (callSignInfo.Kind != PrefixKind.DXCC)
                    {
                        CallSignInfo callSignInfoDxcc = Adifs[Convert.ToInt32(query[0].DXCC)];
                        CallSignInfo callSignInfoCopyDxcc = callSignInfoDxcc.ShallowCopy();
                        callSignInfoCopyDxcc.CallSign = fullCall;
                        callSignInfoCopyDxcc.BaseCall = baseCall;
                        callSignInfoCopyDxcc.SearchPrefix = searchTerm; // this needs correction
                        HitList.Add(callSignInfoCopyDxcc);
                    }
                }

                // for 4U1A and 4U1N
                if (searchTerm.Length > 3 && searchTerm.Substring(0, 2) == "4U")
                {
                    CheckAdditionalDXCCEntities(callAndprefix, fullCall);
                }

                return;
            }

            if (searchTerm.Length > 1)
            {
                searchTerm = searchTerm.Remove(searchTerm.Length - 1);
                while (searchTerm != string.Empty)
                {
                    if (CallSignDictionary.ContainsKey(searchTerm))
                    {
                        List<CallSignInfo> query = CallSignDictionary[searchTerm].ToList();

                        foreach (CallSignInfo callSignInfo in query.Where(x => x.PrefixKey.Contains(searchTerm)))
                        {
                            CallSignInfo callSignInfoCopy = callSignInfo.ShallowCopy();
                            callSignInfoCopy.CallSign = fullCall;
                            callSignInfoCopy.BaseCall = baseCall;
                            callSignInfoCopy.SearchPrefix = searchTerm;
                            HitList.Add(callSignInfoCopy);

                            if (callSignInfo.Kind != PrefixKind.DXCC)
                            {
                                CallSignInfo callSignInfoDxcc = Adifs[Convert.ToInt32(query[0].DXCC)];
                                CallSignInfo callSignInfoCopyDxcc = callSignInfoDxcc.ShallowCopy();
                                callSignInfoCopyDxcc.CallSign = fullCall;
                                callSignInfoCopyDxcc.BaseCall = baseCall;
                                callSignInfoCopyDxcc.SearchPrefix = searchTerm;
                                HitList.Add(callSignInfoCopyDxcc);
                            }
                        }
                    }
                    // is it in the DXCC list
                    //if (HitList.Count == 0)
                    //{
                    //    CheckAdditionalDXCCEntities(callAndprefix, fullCall);
                    //}

                    searchTerm = searchTerm.Remove(searchTerm.Length - 1);
                }
            }

            // could this be a DXCC only prefix kind? - (B1Z, B7M)
            if (HitList.Count == 0)
            {
                CheckAdditionalDXCCEntities(callAndprefix, fullCall);
            }
        }

        /// <summary>
        /// Some calls overlap different enities, specifically 4U1A (IARC) and 4U1N (ITU) and Austria
        /// so I check the DXCC list (Adifs) to find them
        /// </summary>
        /// <param name="callAndprefix"></param>
        /// <param name="fullCall"></param>
        private void CheckAdditionalDXCCEntities((string baseCall, string callPrefix) callAndprefix, string fullCall)
        {
            List<CallSignInfo> query = Adifs.Values.Where(q => q.PrefixKey.Contains(callAndprefix.callPrefix)).ToList();

            foreach (CallSignInfo callSignInfo in query)
            {
                // trying to eliminate dupes - big performance hit, let user do it?
                // I think this only happens with 4U1x calls
                //if (HitList.Where(q => q.Country == callSignInfo.Country).ToList().Count == 0)
                //{
                CallSignInfo callSignInfoCopy = callSignInfo.ShallowCopy();
                callSignInfoCopy.CallSign = fullCall;
                callSignInfoCopy.BaseCall = callAndprefix.baseCall;
                callSignInfoCopy.SearchPrefix = callAndprefix.callPrefix;
                HitList.Add(callSignInfoCopy);
                // }
            }

            // recurse for calls like VP8PJ where only the VP8 portion is used
            if (HitList.Count == 0)
            {
                callAndprefix.callPrefix = callAndprefix.callPrefix.Remove(callAndprefix.callPrefix.Length - 1);
                if (callAndprefix.callPrefix.Length == 0) { return; }
                CheckAdditionalDXCCEntities(callAndprefix, fullCall);
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
        /// This is duplicated - need to move to common file.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }
    } // end class
}
