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
        private readonly Dictionary<string, List<int>> PortablePrefixes;
        //private readonly string[] _OneLetterSeries = { "B", "F", "G", "I", "K", "M", "N", "R", "W", "2" };
        private readonly string[] SingleCharPrefixes = { "F", "G", "I", "M", "R", "W" };
        // added "R" as a beacon for R/IK3OTW
        // "U" for U/K2KRG
        private readonly string[] RejectPrefixes = { "AG", "U", "R", "A", "B", "M", "P", "MM", "AM", "QR", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL", "MOBILE" };
        // ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:';

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
        /// Look up a single call sign. First make sure it is a valid call sign.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns>IEnumerable<CallSignInfo></returns>
        public IEnumerable<CallSignInfo> LookUpCall(string callSign)
        {
            HitList = new ConcurrentBag<CallSignInfo>();

            //if (ValidateCallSign(callSign))
            //{
            try
            {
                ProcessCallSign(callSign.ToUpper());
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid call sign format.");
            }
            //}
            //else
            //{
            //    throw new Exception("Invalid call sign format.");
            //    //Console.WriteLine("Invalid call sign format: " + callSign);
            //}

            return HitList.AsEnumerable();
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
            // comment out for debugging - need to use non parallel foreach for debugging
            //_ = Parallel.ForEach(callSigns, callSign =>
            foreach (var callSign in callSigns)
            {
                //if (ValidateCallSign(callSign))
                //{
                try
                {
                    ProcessCallSign(callSign.ToUpper());
                }
                catch (Exception ex)
                {
                    // bury exception
                }
            }
            //);

            return HitList.AsEnumerable();
        }

        /// <summary>
        /// THIS MAY NO LONGER BE NECESSARY
        /// Do a preliminary test of call signs.
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

            // check for a "-" ie: VE7CC-7, OH6BG-1, WZ7I-3 
            if (callSign.IndexOf("-") != -1) { return false; }

            // can't be all numbers or all letters
            if (callSign.All(char.IsDigit) || callSign.All(char.IsLetter)) { return false; }

            // look for at least one number character
            if (!callSign.Any(char.IsDigit)) { return true; }

            return true;
        }

        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/W4
        /// Call signs in the international series are formed as indicated in Nos. 19.51 to 19.71. 
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
            (string baseCall, string prefix, CompositeType composite) callStructure;

            // strip leading or trailing "/"  /W6OP/
            if (callSign.First() == '/')
            {
                callSign = callSign.Substring(1);
            }

            if (callSign.Last() == '/')
            {
                callSign = callSign.Remove(callSign.Length - 1, 1);
            }

            // trim the call sign of invalid of unnecessary parts
            callStructure = TrimCallSign(callSign.Split('/').ToList());

            if (callStructure.composite == CompositeType.Invalid)
            {
                return;
            }

            CollectMatches(callStructure, callSign);
        }

        /// <summary>
        /// Preliminary check to eliminate the most obvious invalid call signs.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="callSign"></param>
        /// <returns>(string call, string prefix)</returns>
        private (string baseCall, string prefix, CompositeType composite) TrimCallSign(List<string> components)
        {
            List<string> tempComponents = new List<string>();

            // call is too long
            if (components.Count > 3) // was 4 but I don't see how that would ever work
            {
                return (baseCall: components[0], prefix: components[1], CompositeType.Invalid);
            }

            List<(string call, StringTypes sType)> stringTypes =
                (components.Select(item => (call: item, sType: GetComponentType(item)))).ToList();

            // if any portions are obviously invalid don't bother processing it
            if (stringTypes.Where(t => t.sType == StringTypes.Invalid).ToList().Count > 0)
            {
                return (baseCall: components[0], prefix: "", CompositeType.Invalid);
            }

            // strip off /MM /QRP etc. but not MM/ as it is a valid prifix for Scotland
            tempComponents.AddRange(
                    from (string, StringTypes) component in stringTypes
                    where component.Item1 == components.First() || !RejectPrefixes.Contains(component.Item1)
                    select component.Item1);

            switch (tempComponents.Count)
            {
                case 0:
                    //"NX7F ES AK7ID/P"  "IZ3IJG /QRP" - spaces and a reject prefix
                    return (baseCall: "", prefix: "", CompositeType.Invalid);
                case 1:
                    // 6/W7UIO - 8/P - 2/P
                    if (tempComponents[0].Length == 1 && tempComponents[0].All(char.IsDigit))
                    {
                        return (baseCall: "", prefix: "", CompositeType.Invalid);
                    }
                    if (tempComponents[0].Length > 2 && stringTypes.First().sType == StringTypes.Valid)
                    {
                        // ON3RX(/P) - CN2DA(/QRP) - B1Z
                        return (baseCall: tempComponents[0], prefix: "", CompositeType.Call); ;

                    }
                    else
                    {
                        // 7N(/QRP) - 41(/R) - 9A(/R)
                        return (baseCall: "", prefix: "", CompositeType.Invalid);
                    }
                case 2:
                    // 1/D
                    if (tempComponents[0].Length >= 3 || tempComponents[1].Length >= 3)
                    {
                        // everything good HB9/OE6POD
                        return ProcessPrefix(tempComponents);
                    }
                    return (baseCall: "", prefix: "", CompositeType.Invalid);
                default:
                    Console.WriteLine(tempComponents[0] + " : " + tempComponents[1]);
                    // EA5/SM/YBJ, IK1/DH2SAQIK1/DH2SAQ, 9M6/LA/XK, WQ14YBNEQL1QX/MEQU1QX/MN
                    return (baseCall: "", prefix: "", CompositeType.Invalid);
            }
        }


        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/V31, W4/W6OP, SM0KAK/BY1QH (China)
        ///  ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:'
        /// 1. Eliminate any 2 number or 2 character prefixes
        /// 2. If prefix is same length as call, use prefix/suffix instead of call
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="components"></param>
        /// <returns>(string call, string prefix)</returns>
        private (string baseCall, string prefix, CompositeType composite) ProcessPrefix(List<string> components)
        {
            ComponentType state = ComponentType.Unknown;
            string component0;
            string component1;
            ComponentType component0Type;
            ComponentType component1Type;

            component0 = components[0];
            component1 = components[1];

            // Prefix, Call, Text, Numeric, Unknown
            component0Type = IsCallSignOrPrefix(component0);
            component1Type = IsCallSignOrPrefix(component1);

            // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';

            /*
             //resolve ambiguities
              FStructure := StringReplace(FStructure, 'UU', 'PC', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'CU', 'CP', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'UC', 'PC', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'UP', 'CP', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'PU', 'PC', [rfReplaceAll]);
              FStructure := StringReplace(FStructure, 'U',   'C', [rfReplaceAll]); 
             */

            switch (state)
            {
                // CP 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.CallPrefix);

                // PC 
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.PrefixCall);

                // C - C BU - BY - VU4 - VU7
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.CallSign:
                    if (component0.First() == 'B')
                    {
                        return (baseCall: component0, prefix: component1, composite: CompositeType.CallPrefix);
                    }
                    else if (component0.StartsWith("VU4") || component0.StartsWith("VU7"))
                    {
                        return (baseCall: component0, prefix: component1, composite: CompositeType.CallPrefix);
                    }
                    else
                    {
                        return (baseCall: component0, prefix: component1, composite: CompositeType.CallCall);
                    }

                // Prefix and Prefix
                // PP
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.Prefix:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.PrefixPrefix);

                // CT
                case ComponentType _ when component0Type == ComponentType.CallSign || component1Type == ComponentType.Text:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.Call);

                // C
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric:
                    _ = ReplaceCallArea(components,
                                        ref component0,
                                        ref component1);
                    return (baseCall: component0, prefix: component1, composite: CompositeType.Call);
                //--------- All the invalid ones could fall trough but I leave them here to debug with ------------------
                // CU
                case ComponentType _ when component0Type == ComponentType.Unknown || component1Type == ComponentType.Unknown:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.Invalid);
                
                // TP
                case ComponentType _ when component0Type == ComponentType.Text || component1Type == ComponentType.Prefix:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.Invalid);

                // TN
                case ComponentType _ when component0Type == ComponentType.Text || component1Type == ComponentType.Numeric:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.Invalid);

                // NC
                case ComponentType _ when component0Type == ComponentType.Numeric || component1Type == ComponentType.CallSign:
                    return (baseCall: component0, prefix: component1, composite: CompositeType.Invalid);

                default:
                    break;
            }

            return (baseCall: component0, prefix: component1, composite: CompositeType.Invalid);
        }

        /// <summary>
        /// Change W6OP/4 to W4OP and R44YETI/5 to R5YETI
        /// </summary>
        /// <param name="components"></param>
        /// <param name="component0"></param>
        /// <param name="component1"></param>
        /// <returns></returns>
        private static string ReplaceCallArea(List<string> components, ref string component0, ref string component1)
        {
            string result = "";
            try
            {
                switch (components)
                {
                    // THE FIRST CHARACTER SHOULD NEVER BE A SINGLE DIGIT
                    //case List<string> _ when component0.Length == 1 && int.TryParse(component0, out int _):
                    //    result = new String(component1.Where(x => Char.IsDigit(x)).ToArray());
                    //    if (result != "") // 1/D
                    //    {
                    //        component1 = component1.Replace(result, component0);
                    //        component0 = component1;
                    //    }
                    //    break;
                    case List<string> _ when component1.Length == 1 && int.TryParse(component1, out int _):
                        result = new String(component0.Where(x => Char.IsDigit(x)).ToArray());
                        if (result != "") // SVFMF/4
                        {
                            component0 = component0.Replace(result, component1);
                            component1 = "";
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

            return result;
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
            string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };
            string[] validPrefixOrCall = { "@@#@", "@#@" };
            ComponentType state = ComponentType.Unknown;

            string pattern = BuildPattern(candidate);

            switch (state)
            {
                // this first case is somewhat redundant 
                case ComponentType _ when (validPrefixOrCall.Contains(pattern)):
                    // now determine if prefix or call
                    if (VerifyIfPrefix(candidate) == ComponentType.Prefix)
                    {
                       return  ComponentType.Prefix;
                    }
                    else
                    {
                        return VerifyIfCallSign(candidate);
                    }

                case ComponentType _ when (validPrefixes.Contains(pattern)):
                    // is it really a prefix though
                    return VerifyIfPrefix(candidate);

                case ComponentType _ when (validCallStructures.Contains(pattern)):
                    return VerifyIfCallSign(candidate);

                case ComponentType _ when (candidate.All(char.IsLetter)):
                    return ComponentType.Text;

                case ComponentType _ when (candidate.All(char.IsDigit)):
                    return ComponentType.Numeric;
            }

            return ComponentType.Unknown;
        }

        /// <summary>
        /// Test if a candidate is truly a prefix.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private ComponentType VerifyIfPrefix(string candidate)
        {
            if (PortablePrefixes.ContainsKey(candidate + "/"))
            {
                return ComponentType.Prefix;
            }

            if (candidate.Length == 1 && SingleCharPrefixes.Contains(candidate))
            {
                return ComponentType.Prefix;
            }

            return ComponentType.Unknown;
        }

        /// <summary>
        /// Strip the prefix and make sure there are numbers and letters left.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private ComponentType VerifyIfCallSign(string candidate)
        {
            int digits = 0;
            int alphas = 0;
            int count = 0;

            // strip prefix
            switch (candidate)
            {
                case string _ when candidate.Take(2).All(char.IsLetter): // "@@"
                    candidate = candidate.Remove(0, 2);
                    break;
                case string _ when candidate.Take(1).All(char.IsLetter): // "@"
                    candidate = candidate.Remove(0, 1);
                    break;
                case string _ when candidate.Take(1).All(char.IsDigit) && candidate.Take(2).Skip(1).All(char.IsLetter): // "#@@"
                    candidate = candidate.Remove(0, 3);
                    break;
                case string _ when candidate.Take(1).All(char.IsDigit) && candidate.Take(1).Skip(1).All(char.IsLetter): // #@
                    candidate = candidate.Remove(0, 2);
                    break;
                default:
                    return ComponentType.Unknown;
            }

            // count letters and  digits
            while (count < candidate.Length)
            {
                if (candidate[count] >= '0' && candidate[count] <= '9')
                {
                    digits++;
                }

                if (candidate[count] >= 'A' && candidate[count] <= 'Z')
                {
                    alphas++;
                }
                count++;
            }

            if (alphas > 0 && alphas <= 6 && digits > 0 && digits <= 4)
            {
                return ComponentType.CallSign;
            }

            return ComponentType.Unknown;
        }

        /// <summary>
        /// Build a pattern that models the string passed in.
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

                if (char.IsDigit(item))
                {
                    pattern += "#";
                }

                if (char.IsPunctuation(item) || char.IsWhiteSpace(item))
                {
                    pattern += "?";
                }
            }

            return pattern;
        }


        /// <summary>
        /// Test for string only, int only, special chars. and other types. 
        /// Look at valid string types closely and determine if they really are valid (N666A)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private StringTypes GetComponentType(string item)
        {
            // W6 OP - no spaces alowed
            if (item.Any(char.IsWhiteSpace))
            {
                return StringTypes.Invalid;
            }

            // W$OP - no special characters allowed
            if (item.Any(c => !char.IsLetterOrDigit(c)))
            {
                return StringTypes.Invalid;
            }

            // WAOP
            if (item.All(char.IsLetter))
            {
                return StringTypes.Text;
            }

            // 9A is considered letter - will later check if know prefix
            if (item.Length == 2)
            {
                if (item.Any(char.IsDigit) && item.Any(char.IsLetter))
                {
                    return StringTypes.Text;
                }
            }

            //37747
            if (item.All(char.IsDigit))
            {
                return StringTypes.Numeric;
            }

            // N666
            if (HasExcessDigits(item))
            {
                return StringTypes.Invalid;
            }

            // maybe can be longer ?
            if (item.Length > 8)
            {
                return StringTypes.Invalid;
            }


            return StringTypes.Valid;
        }

        /// <summary>
        /// Catch calls like N666 but allow R44xxx
        /// Still allows invalid calls like 55PT
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool HasExcessDigits(string item)
        {

            // test for number of digits
            IEnumerable<char> stringQuery =
                      from ch in item
                      where Char.IsDigit(ch)
                      select ch;

            if (stringQuery.Count() > 2)
            {
                return true;
            }


            return false; // GT3UCQ/PGT3UCQ/P
        }

        /// <summary>
        /// First see if we can find a match for the full prefix. I don't use the base call in the tuple
        /// but pass it in for future use. If there is not a match start removing characters from the 
        /// back until we can find a match.
        /// Once we have a match we will see if we can find a child that is a better match. Also get the
        /// DXCC parent if this is not a top level entity.
        /// Need to clone (shallow copy) each object so the full callsign is assigned correctly to each hit.
        /// </summary>
        /// <param name="callOrPrefix"></param>
        /// /// <param name="fullCall"></param>
        private void CollectMatches((string baseCall, string prefix, CompositeType composite) callStructure, string fullCall)
        {
            string prefix = callStructure.prefix;
            string baseCall = callStructure.baseCall;
            string searchTerm = "";
            string persistSearchTerm = "";
            int[] portableDXCC = { }; // to eliminate dupe dxcc entries
            CompositeType composite = callStructure.composite;

            switch (composite) // GT3UCQ/P
            {
                case CompositeType.Call:
                    searchTerm = baseCall;
                    break;
                case CompositeType.CallText:
                    searchTerm = baseCall;
                    break;
                case CompositeType.CallCall:
                    searchTerm = baseCall; // may be incorrect
                    break;
                case CompositeType.CallPrefix:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall, prefix + "/");
                    searchTerm = baseCall;
                    break;
                case CompositeType.PrefixCall:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall, baseCall + "/");
                    searchTerm = prefix;
                    break;
                case CompositeType.PrefixPrefix:
                    searchTerm = baseCall; // probably invalid
                    break;
                case CompositeType.CallDigit:
                    //_ = ReplacecallArea(components, ref component0, ref component1);
                    string result = new String(baseCall.Where(x => Char.IsDigit(x)).ToArray());
                    searchTerm = baseCall.Replace(result, prefix); ;
                    break;
                //case CompositeType.Portable1:
                //    break;
                //case CompositeType.Portable2:
                //    break;
                case CompositeType.Invalid:
                    break;
                default:
                    return;
            }

            persistSearchTerm = searchTerm;

            // is the full call in the dictionary, never will be more than one
            if (CallSignDictionary.TryGetValue(searchTerm, out var lookup))
            {
                var callSignInfo = lookup.First();
                if (callSignInfo.Kind != PrefixKind.InvalidPrefix && portableDXCC.Contains(callSignInfo.DXCC))
                {
                    var callSignInfoCopy = callSignInfo.ShallowCopy();
                    callSignInfoCopy.CallSign = fullCall;
                    callSignInfoCopy.BaseCall = baseCall;
                    callSignInfoCopy.SearchPrefix = searchTerm;
                    HitList.Add(callSignInfoCopy);

                    if (callSignInfo.Kind != PrefixKind.DXCC)
                    {
                        var callSignInfoDxcc = Adifs[Convert.ToInt32(callSignInfo.DXCC)];
                        var callSignInfoCopyDxcc = callSignInfoDxcc.ShallowCopy();
                        callSignInfoCopyDxcc.CallSign = fullCall;
                        callSignInfoCopyDxcc.BaseCall = baseCall;
                        callSignInfoCopyDxcc.SearchPrefix = searchTerm; // this needs correction ??
                        HitList.Add(callSignInfoCopyDxcc);
                    }
                }

                // for 4U1A and 4U1N
                if (searchTerm.Length > 3 && searchTerm.Substring(0, 2) == "4U")
                {
                    CheckAdditionalDXCCEntities(callStructure, fullCall, searchTerm);
                }

                return;
            }

            if (searchTerm.Length > 1)
            {
                searchTerm = searchTerm.Remove(searchTerm.Length - 1);

                while (searchTerm != string.Empty)
                {
                    if (CallSignDictionary.TryGetValue(searchTerm, out var query))
                    {
                        var callSignInfo = query.First();
                        if (callSignInfo.Kind != PrefixKind.InvalidPrefix && portableDXCC.Contains(callSignInfo.DXCC))
                        {
                            var callSignInfoCopy = callSignInfo.ShallowCopy();
                            callSignInfoCopy.CallSign = fullCall;
                            callSignInfoCopy.BaseCall = baseCall;
                            callSignInfoCopy.SearchPrefix = searchTerm;
                            HitList.Add(callSignInfoCopy);
                            if (callSignInfo.Kind != PrefixKind.DXCC)
                            {
                                var callSignInfoDxcc = Adifs[Convert.ToInt32(callSignInfo.DXCC)];
                                var callSignInfoCopyDxcc = callSignInfoDxcc.ShallowCopy();
                                callSignInfoCopyDxcc.CallSign = fullCall;
                                callSignInfoCopyDxcc.BaseCall = baseCall;
                                callSignInfoCopyDxcc.SearchPrefix = searchTerm;
                                HitList.Add(callSignInfoCopyDxcc);
                            }
                        }

                        return; // ??? Added 3/24 because I think too many calls are going to  if (string.IsNullOrEmpty(searchTerm)) below
                    }

                    searchTerm = searchTerm.Remove(searchTerm.Length - 1);
                }
            }



            // THIS NEEDS MORE TESTING
            // THIS IS WAY TOO SLOW !!!
            // could this be a DXCC only prefix kind? - (B1Z, B7M, DL6DH)
            if (string.IsNullOrEmpty(searchTerm))
            {
                //searchTerm = callStructure.baseCall;
                if (persistSearchTerm.Length > 4)
                {
                    persistSearchTerm = persistSearchTerm.Substring(0, 4);
                }
                CheckAdditionalDXCCEntities(callStructure, fullCall, persistSearchTerm);
            }
        }

        private int[] CheckForPortablePrefix((string baseCall, string prefix, CompositeType composite) callStructure, string fullCall, string searchTerm)
        {
            string prefix = callStructure.prefix;
            string baseCall = callStructure.baseCall;
            int[] dxcc = { };

            // check for portable prefixes
            // this will catch G/, W/, W4/, VU@@/ VU4@@/ VK9/
            if (PortablePrefixes.TryGetValue(searchTerm, out var entities))
            {
                List<int> dxccList = new List<int>();
                foreach (var callSignInfoCopy in from int entity in entities
                                                 let callSignInfo = Adifs[entity]
                                                 let callSignInfoCopy = callSignInfo.ShallowCopy()
                                                 select callSignInfoCopy)
                {
                    callSignInfoCopy.CallSign = fullCall;
                    callSignInfoCopy.BaseCall = baseCall;
                    callSignInfoCopy.SearchPrefix = searchTerm;// this needs correction
                    HitList.Add(callSignInfoCopy);
                    dxccList.Add(callSignInfoCopy.DXCC);
                }
                dxcc = dxccList.ToArray();
            }
            return dxcc.ToArray();
        }

        /// <summary>
        /// Some calls overlap different entities, specifically 4U1A (IARC) and 4U1N (ITU) and Austria
        /// Others like B1Z and DL(6DH) only are in the Adifs list
        /// so I check the DXCC list (Adifs) to find them
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        private void CheckAdditionalDXCCEntities((string baseCall, string prefix, CompositeType composite) callStructure, string fullCall, string searchTerm)
        {
            var query = Adifs.Values.Where(q => q.PrefixKey.ContainsKey(searchTerm)).ToList();

            foreach (CallSignInfo callSignInfo in query)
            {
                var callSignInfoCopy = callSignInfo.ShallowCopy();
                callSignInfoCopy.CallSign = fullCall;
                callSignInfoCopy.BaseCall = callStructure.baseCall;
                callSignInfoCopy.SearchPrefix = callStructure.prefix;
                HitList.Add(callSignInfoCopy);
            }

            // this is major performance enhancement, but is it accurate?
            //if (PortablePrefixes.TryGetValue(searchTerm + "/", out var entities))
            //{
            //    foreach (var callSignInfoCopy in from int entity in entities
            //                                     let callSignInfo = Adifs[entity]
            //                                     let callSignInfoCopy = callSignInfo.ShallowCopy()
            //                                     select callSignInfoCopy)
            //    {
            //        callSignInfoCopy.CallSign = fullCall;
            //        callSignInfoCopy.BaseCall = callStructure.baseCall;
            //        callSignInfoCopy.SearchPrefix = searchTerm;// this needs correction
            //        HitList.Add(callSignInfoCopy);
            //    }

            //    return;
            //}

            //if (entities != null && entities.Count > 0)
            //{
            //    return;
            //}

            // recurse for calls like VP8PJ where only the VP8 portion is used
            if (searchTerm.Length > 0)
            {
                searchTerm = searchTerm.Remove(searchTerm.Length - 1);
                if (searchTerm.Length == 0) { return; }
                CheckAdditionalDXCCEntities(callStructure, fullCall, searchTerm);
            }

            // NEED TO FIND OUT IF I AM MISSING ANYTHING
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine(fullCall);
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
