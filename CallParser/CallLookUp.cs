﻿
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
        //private readonly string[] _OneLetterSeries = { "B", "F", "G", "I", "K", "M", "N", "R", "W", "2" };
        private readonly string[] SingleCharPrefixes = { "F", "G", "I", "M", "R", "W" };
        // added "R" as a beacon for R/IK3OTW
        // "U" for U/K2KRG
        private readonly string[] RejectPrefixes = { "U", "R", "A", "B", "M", "P", "MM", "AM", "QR", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL", "MOBILE" };

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

            if (callSigns == null)
            {
                throw new Exception("The call sign list must contain at least one entry.");
            }
            // Console.WriteLine("Callsigns: " + callSigns.Count.ToString());

            //if (!Environment.Is64BitProcess && callSigns.Count > 1500000)
            //{
            //    throw new Exception("To many entries. Please reduce the number of entries to 1.5 million or less.");
            //}

            // parallel foreach almost twice as fast but requires blocking collection
            // need to use non parallel foreach for debugging
            // _ = Parallel.ForEach(callSigns, callSign =>
            foreach (string callSign in callSigns)
            {
                if (ValidateCallSign(callSign))
                {
                    try
                    {
                        //if (callSign == "CM4FAR/R") //CM4FAR/R
                        //{
                        //    var a = 1;
                        //}
                        ProcessCallSign(callSign);
                    }
                    //catch (ArgumentException aex) // "NX7F ES AK7ID/P"
                    //{
                    //    var a = 1;
                    //}
                    catch (Exception ex)
                    {
                        // bury exception
                        //Console.WriteLine("Invalid call sign format: " + callSign);
                    }
                }
                else
                {
                    // don't throw, just ignore bad calls
                   // Console.WriteLine("Invalid call sign format: " + callSign);
                }
            }
            //);

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
                    throw new Exception("Invalid call sign format.");
                }
            }
            else
            {
                throw new Exception("Invalid call sign format.");
                //Console.WriteLine("Invalid call sign format: " + callSign);
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

            callAndprefix = TrimCallSign(callSign.Split('/').ToList());

            if (callAndprefix.callPrefix == "" || callAndprefix.baseCall == "")
            {
                return;
            }

            CollectMatches(callAndprefix, callSign);
        }

        /// <summary>
        /// Look at each component of a call sign and see if some should be removed or modified.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="callSign"></param>
        /// <returns>(string call, string callPrefix)</returns>
        private (string baseCall, string callPrefix) TrimCallSign(List<string> components)
        {
            List<string> tempComponents = new List<string>();
            (string baseCall, string callPrefix) callAndprefix = ("", "");
           
            //////////////////////////////////////
            List<(string call, StringTypes sType)> stringTypes = (components.Select(item => (call: item, sType: GetComponentType(item)))).ToList();

            // 2.8us
            // strip off /MM /QRP etc. but not MM/ as it is a valid prifix for Scotland
            tempComponents.AddRange(
                    from (string, StringTypes) component in stringTypes
                    where component.Item1 == components.First() || !RejectPrefixes.Contains(component.Item1)
                    where component.Item2 != StringTypes.Invalid
                    select component.Item1);

            // "NX7F ES AK7ID/P"
            if (!tempComponents.Distinct().Skip(1).Any())
            {
                return (baseCall: "", callPrefix: "");
            }
            /////////////////////////////////////////

            // 3.2us
            // strip off /MM /QRP etc. but not MM/ as it is a valid prifix for Scotland
            //foreach (string component in components)
            //{
            //    if (component == components.First() || !RejectPrefixes.Contains(component))
            //    {
            //        tempComponents.Add(component);
            //    }
            //}

            // WAW/4 ==> 4
            if (tempComponents.Count == 1)
            {
                if (tempComponents[0].Length > 3)
                {
                    return (baseCall: tempComponents[0], callPrefix: tempComponents[0]);
                }
                else
                {
                    // HA4
                    return (baseCall: "", callPrefix: "");
                    //throw new Exception("Invalid call sign format.");
                }
            }

            if (tempComponents.Count > 2)
            {
                //throw new Exception("Call sign has too many components.");
                // EA5/SM/YBJ, IK1/DH2SAQIK1/DH2SAQ
                return (baseCall: "", callPrefix: "");
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
            ComponentType state = ComponentType.Unknown;
            string component0;
            string component1;
            string result;

            component0 = components[0];
            component1 = components[1];

            // this will change W6OP/4 to W4OP and R44YETI/5 to R5YETI
            try
            {
                switch (components)
                {
                    case List<string> _ when component0.Length == 1 && int.TryParse(component0, out int _):
                        result = new String(component1.Where(x => Char.IsDigit(x)).ToArray());
                        if (result != "") //1/D
                        {
                            component1 = component1.Replace(result, component0);
                            component0 = component1;
                        }
                        break;
                    case List<string> _ when component1.Length == 1 && int.TryParse(component1, out int _):
                        result = new String(component0.Where(x => Char.IsDigit(x)).ToArray());
                        if (result != "") // SVFMF/4
                        {
                            component0 = component0.Replace(result, component1);
                            component1 = component0;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex) // WAW/4
            {
                throw;
            }

            // is this a portable prefix?
            if (PortablePrefixes.ContainsKey(component0 + "/")) { return (baseCall: component1, callPrefix: component0 + "/"); }
            // can this happen ??
            if (PortablePrefixes.ContainsKey(component1 + "/")) { return (baseCall: component0, callPrefix: component1 + "/"); }

            // valid single character prefix
            if (component0.Length == 1 && SingleCharPrefixes.Contains(component0)) { return (baseCall: component1, callPrefix: component0); }

            ComponentType component0Type = IsCallSignOrPrefix(component0);
            ComponentType component1Type = IsCallSignOrPrefix(component1);

            switch (state)
            {
                // CallSign and Prefix
                // C - P 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix:
                    return (baseCall: component0, callPrefix: component1);

                // P - C 
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign:
                    return (baseCall: component1, callPrefix: component0);

                // C - C BU BY VU4 VU7
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.CallSign:
                    if (component0.First() == 'B')
                    {
                        return (baseCall: component1, callPrefix: component0);
                    }
                    else if (component0.StartsWith("VU4") || component0.StartsWith("VU7"))
                    {
                        return (baseCall: component1, callPrefix: component0);
                    }
                    break;

                // Prefix and Prefix
                // P - P
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.Prefix:
                    return (baseCall: component0, callPrefix: component1);

                // CallOrPrefix check
                // CorP - CorP
                case ComponentType _ when component0Type == ComponentType.CallOrPrefix && component1Type == ComponentType.CallOrPrefix:
                    return (baseCall: component0, callPrefix: component1);

                // C - CorP
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.CallOrPrefix:
                    return (baseCall: component0, callPrefix: component1);

                // P - CorP
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallOrPrefix:
                    return (baseCall: component1, callPrefix: component0);

                // CorP - C
                case ComponentType _ when component0Type == ComponentType.CallOrPrefix && component1Type == ComponentType.CallSign:
                    return (baseCall: component1, callPrefix: component0);

                // CorP - P
                case ComponentType _ when component0Type == ComponentType.CallOrPrefix && component1Type == ComponentType.Prefix:
                    return (baseCall: component0, callPrefix: component1);

                // CallSign, Prefix, CallOrprefix and None
                // 'CU', 'CP'
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Unknown:
                    return (baseCall: component0, callPrefix: component0);

                // 'PU', 'PC'
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.Unknown:
                    return (baseCall: component0, callPrefix: component1);

                case ComponentType _ when component0Type == ComponentType.CallOrPrefix && component1Type == ComponentType.Unknown:
                    return (baseCall: component0, callPrefix: component0);

                // None and CallSign, Prefix, CallOrprefix
                // 'UC', 'PC'
                case ComponentType _ when component0Type == ComponentType.Unknown && component1Type == ComponentType.CallSign:
                    return (baseCall: component1, callPrefix: component1);

                // 'UP', 'CP'
                case ComponentType _ when component0Type == ComponentType.Unknown && component1Type == ComponentType.Prefix:
                    return (baseCall: component1, callPrefix: component1);

                case ComponentType _ when component0Type == ComponentType.Unknown && component1Type == ComponentType.CallOrPrefix:
                    return (baseCall: component1, callPrefix: component1);

                // 'UU', 'PC'
                case ComponentType _ when component0Type == ComponentType.Unknown && component1Type == ComponentType.Unknown:
                    return (baseCall: component0, callPrefix: component1);
                default:
                    break;
            }

            return (baseCall: component0, callPrefix: component1);
        }

        /// <summary>
        /// //one of "@","@@","#@","#@@" followed by 1-4 digits followed by 1-6 letters
        /// ValidPrefixes = ':@:@@:@@#:@@#@:@#:@#@:@##:#@:#@@:#@#:#@@#:';
        /// ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private ComponentType IsCallSignOrPrefix(string candidate)
        {
            string[] validCallStructures = { "@#@@", "@#@@@", "@##@", "@##@@", "@##@@@", "@@#@", "@@#@@", "@@#@@@", "#@#@", "#@#@@", "#@#@@@", "#@@#@", "#@@#@@" }; // KH6Z
            // string[] validCallStructures = { "@", "@@", "@#@@", "@#@@@", "#@", "#@@", "#@#@", "#@#@@", "#@#@@@", "#@#@@@@", "#@#@@@@@", "@@#", "@@#@", "@@#@@", "@@#@@@" }; // KH6Z
            string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };
            string[] validPrefixOrCall = { "@@#@", "@#@" };
            ComponentType state = ComponentType.Unknown;

            string pattern = BuildPattern(candidate);

            switch (state)
            {
                case ComponentType _ when (validPrefixOrCall.Contains(pattern)):
                    return ComponentType.CallOrPrefix;

                case ComponentType _ when (validCallStructures.Contains(pattern)):
                    return ComponentType.CallSign;

                case ComponentType _ when (validPrefixes.Contains(pattern)):
                    return ComponentType.Prefix;
            }

            return ComponentType.Unknown;
        }

        ///// <summary>
        ///// //one of valid patterns of portable prefixes, and prefix is known
        ///// ValidPrefixes = ':@:@@:@@#:@@#@:@#:@#@:@##:#@:#@@:#@#:#@@#:';
        ///// </summary>
        ///// <param name="candidate"></param>
        ///// <returns></returns>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
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
        /// Look at a string and return the ComponentType it matches.
        /// Used to eliminate invalid parts of a portable call sign.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        //private List<(string, StringTypes)> AnalyzeComponent(string callSign)
        //{
        //    List<string> tempComponents = callSign.Split('/').ToList();

        //    // 

        //    return (tempComponents.Select(item => (item, GetComponentType(item)))).ToList();
        //}

        /// <summary>
        /// Test for string only, int only, special chars. and other types
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private StringTypes GetComponentType(string item)
        {
            // W6 OP
            if (item.Any(char.IsWhiteSpace))
            {
                return StringTypes.Invalid;
            }

            // W#OP
            if (item.Any(c => !char.IsLetterOrDigit(c)))
            {
                return StringTypes.Invalid;
            }

            // WAOP
            if (item.All(char.IsLetter))
            {
                return StringTypes.Text;
            }

            //37747
            if (item.All(char.IsDigit))
            {
                return StringTypes.Numeric;
            }

            return StringTypes.Valid;
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

                //foreach (CallSignInfo callSignInfo in query.Where(x => x.PrefixKey.Contains(searchTerm)))
                //{
                //    CallSignInfo callSignInfoCopy = callSignInfo.ShallowCopy();

                foreach (var (callSignInfo, callSignInfoCopy) in from CallSignInfo callSignInfo in query.Where(x => x.PrefixKey.Contains(searchTerm))
                                                                 let callSignInfoCopy = callSignInfo.ShallowCopy()
                                                                 select (callSignInfo, callSignInfoCopy))
                {
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
