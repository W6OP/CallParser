
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
        private readonly string[] SingleCharPrefixes = { "F", "G","M", "I", "R", "W" };
        // added "R" as a beacon for R/IK3OTW
        // "U" for U/K2KRG
        private readonly string[] RejectPrefixes = { "AG", "U", "R", "A", "B", "M", "P", "MM", "AM", "QR", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL", "MOBILE" };
        // ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:';

        private CallStructure CallStructure = new CallStructure();

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
            _ = Parallel.ForEach(callSigns, callSign =>
            //foreach (var callSign in callSigns)
            {
                try
                {
                    ProcessCallSign(callSign.ToUpper());
                }
                catch (Exception ex)
                {
                    var q = ex.Message;
                    // bury exception
                }
            }
            );

            return HitList.AsEnumerable();
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
            //(string baseCall, string prefix, string suffix, CompositeType composite) callStructure;
            CallStructure callStructure;

            // strip leading or trailing "/"  /W6OP/
            if (callSign.First() == '/')
            {
                callSign = callSign.Substring(1);
            }

            if (callSign.Last() == '/')
            {
                callSign = callSign.Remove(callSign.Length - 1, 1);
            }

            callStructure = new CallStructure(callSign, PortablePrefixes);

            if (callStructure.CallStuctureType != CallStructureType.Invalid)
            {
                CollectMatches(callStructure, callSign);
            }
        }

        /// <summary>
        /// Preliminary check to eliminate the most obvious invalid call signs.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="callSign"></param>
        /// <returns>(string call, string prefix)</returns>
        //private CallStructure AnalyzeCallSign(List<string> components)
        //{
        //    //CallStructure callStructure = new CallStructure(components);

        //    // call is too long
        //    // W6OP/4/P/QRP not valid
        //    if (components.Count > 3)
        //    {
        //        return callStructure;
        //    }

        //    // eliminate the most obvious invalid call signs
        //    List<(string call, StringTypes sType)> stringTypes =
        //        (components.Select(item => (call: item, sType: GetComponentType(item)))).ToList();

        //    // if any portions are obviously invalid don't bother processing it
        //    if (stringTypes.Where(t => t.sType == StringTypes.Invalid)
        //                   .ToList().Count > 0)
        //    {
        //        return callStructure;
        //    }



            // strip off /MM /QRP etc. but not MM/ as it is a valid prifix for Scotland
            // NEED TO WORK ON THIS, SHOULD ONLY TRIM REJECT PREFIXES FROM FIRST OR LAST
            // MAYBE DO IT A LOT EARLIER IN THE PROCESS
            //if (RejectPrefixes.Contains(components.First()))
            //{
            //    components.RemoveAt(0);
            //}

            //if (RejectPrefixes.Contains(components.Last()))
            //{
            //    components.Remove(components.Last());
            //}

            //tempComponents = components;

            //tempComponents.AddRange(
            //        from (string, StringTypes) component in stringTypes
            //        where component.Item1 == components.First() || !RejectPrefixes.Contains(component.Item1)
            //        select component.Item1);

        //    switch (components.Count)
        //    {
        //        case 0:
        //            //"NX7F ES AK7ID/P"  "IZ3IJG /QRP" - spaces and a reject prefix
        //            return callStructure;
        //        case 1:
        //            // 6/W7UIO - 8/P - 2/P
        //            //if (components[0].Length == 1 && components[0].All(char.IsDigit))
        //            //{
        //                return new CallStructure(components);
        //            //}
        //            //if (components[0].Length > 2)
        //            //{
        //            //    //return VerifyIfCallSign(components[0]) == ComponentType.Invalid
        //            //    //    ? (baseCall: "", prefix: "", suffix: "" , CompositeType.Invalid)
        //            //    //    : (baseCall: components[0], prefix: "", suffix: "", CompositeType.Call);
        //            //}
        //            //else
        //            //{
        //            //    // 7N(/QRP) - 41(/R) - 9A(/R)
        //            //    return callStructure;
        //            //}
        //        case 2:
        //                //return ProcessPrefix(components);
        //        default:
        //            //Console.WriteLine(tempComponents[0] + " : " + tempComponents[1]);
        //            // EA5/SM/YBJ, IK1/DH2SAQIK1/DH2SAQ, 9M6/LA/XK, WQ14YBNEQL1QX/MEQU1QX/MN
        //            return callStructure;
        //    }
        //}


        /// <summary>
        /// Process a call sign into its component parts ie: W6OP/V31, W4/W6OP, SM0KAK/BY1QH (China)
        ///  ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:'
        /// 1. Eliminate any 2 number or 2 character prefixes
        /// 2. If prefix is same length as call, use prefix/suffix instead of call
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="components"></param>
        /// <returns>(string call, string prefix)</returns>
        //private (string baseCall, string prefix, string suffix, CallStructureType composite) ProcessPrefix(List<string> components)
        //{
        //    ComponentType state = ComponentType.Unknown;
        //    string component0;
        //    string component1;
        //    string component2;
        //    ComponentType component0Type;
        //    ComponentType component1Type;
        //    ComponentType component2Type;

        //    component0 = components[0];
        //    component1 = components[1];
        //    component2 = components[2];


        //    // Prefix, Call, Text, Numeric, Unknown
        //    component0Type = IsCallSignOrPrefix(component0);
        //    component1Type = IsCallSignOrPrefix(component1);
        //    component2Type = IsCallSignOrPrefix(component2);


        //    /*
        //     //resolve ambiguities
        //      FStructure := StringReplace(FStructure, 'UU', 'PC', [rfReplaceAll]);
        //      FStructure := StringReplace(FStructure, 'CU', 'CP', [rfReplaceAll]);
        //      FStructure := StringReplace(FStructure, 'UC', 'PC', [rfReplaceAll]);
        //      FStructure := StringReplace(FStructure, 'UP', 'CP', [rfReplaceAll]);
        //      FStructure := StringReplace(FStructure, 'PU', 'PC', [rfReplaceAll]);
        //      FStructure := StringReplace(FStructure, 'U',   'C', [rfReplaceAll]); 
        //     */

        //    // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
        //    switch (state)
        //    {
        //        // if either is invalid short cicuit all the checks and exit immediately
        //        case ComponentType _ when component0Type == ComponentType.Invalid || component1Type == ComponentType.Invalid:
        //            return (baseCall: component0, prefix: component1, suffix: "", composite: CallStructureType.Invalid);

        //        // CP 
        //        case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.CallPrefix);

        //        // PC 
        //        case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.PrefixCall);

        //        // CC check BU - BY - VU4 - VU7
        //        case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.CallSign:
        //            if (component1.First() == 'B')
        //            {
        //                return (baseCall: component0, prefix: component1, composite: CallStructureType.CallPrefix);
        //            }
        //            else if (component0.StartsWith("VU4") || component0.StartsWith("VU7"))
        //            {
        //                return (baseCall: component0, prefix: component1, composite: CallStructureType.CallPrefix);
        //            }
        //            else
        //            {
        //                return (baseCall: component0, prefix: component1, composite: CallStructureType.Invalid);
        //            }

        //        // Prefix and Prefix
        //        // PP
        //        case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.Prefix:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.PrefixPrefix);

        //        // CT
        //        case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Text:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.Call);

        //        // C
        //        case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.CallDigit);
        //        //--------- All the invalid ones could fall trough but I leave them here to debug with ------------------
        //        // CU
        //        case ComponentType _ when component0Type == ComponentType.Unknown && component1Type == ComponentType.Unknown:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.Invalid);

        //        // TP
        //        case ComponentType _ when component0Type == ComponentType.Text && component1Type == ComponentType.Prefix:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.Invalid);

        //        // TN
        //        case ComponentType _ when component0Type == ComponentType.Text && component1Type == ComponentType.Numeric:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.Invalid);

        //        // NC
        //        case ComponentType _ when component0Type == ComponentType.Numeric && component1Type == ComponentType.CallSign:
        //            return (baseCall: component0, prefix: component1, composite: CallStructureType.Invalid);

        //        default:
        //            break;
        //    }

        //    return (baseCall: component0, prefix: component1, composite: CallStructureType.Invalid);
        //}

        /// <summary>
        /// //one of "@","@@","#@","#@@" followed by 1-4 digits followed by 1-6 letters
        /// ValidPrefixes = ':@:@@:@@#:@@#@:@#:@#@:@##:#@:#@@:#@#:#@@#:';
        /// ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        //private ComponentType IsCallSignOrPrefix(string candidate)
        //{
        //    string[] validCallStructures = { "@#@@", "@#@@@", "@##@", "@##@@", "@##@@@", "@@#@", "@@#@@", "@@#@@@", "#@#@", "#@#@@", "#@#@@@", "#@@#@", "#@@#@@" }; // KH6Z
        //    string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };
        //    string[] validPrefixOrCall = { "@@#@", "@#@" };
        //    ComponentType state = ComponentType.Unknown;

        //    string pattern = BuildPattern(candidate);

        //    switch (state)
        //    {
        //        // this first case is somewhat redundant 
        //        case ComponentType _ when (validPrefixOrCall.Contains(pattern)):
        //            // now determine if prefix or call
        //            if (VerifyIfPrefix(candidate) == ComponentType.Prefix)
        //            {
        //                return ComponentType.Prefix;
        //            }
        //            else
        //            {
        //                return VerifyIfCallSign(candidate);
        //            }

        //        case ComponentType _ when (validPrefixes.Contains(pattern)):
        //            // is it really a prefix though
        //            return VerifyIfPrefix(candidate);

        //        case ComponentType _ when (validCallStructures.Contains(pattern)):
        //            return VerifyIfCallSign(candidate);

        //        case ComponentType _ when (candidate.All(char.IsLetter)):
        //            return ComponentType.Text;

        //        case ComponentType _ when (candidate.All(char.IsDigit)):
        //            return ComponentType.Numeric;
        //        default:
        //            if (VerifyIfPrefix(candidate) == ComponentType.Prefix)
        //            {
        //                return ComponentType.Prefix;
        //            }
        //            else
        //            {
        //                return VerifyIfCallSign(candidate);
        //            }
        //    }

        //    // return ComponentType.Unknown;
        //}

        /// <summary>
        /// Test if a candidate is truly a prefix.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        //private ComponentType VerifyIfPrefix(string candidate)
        //{
        //    if (PortablePrefixes.ContainsKey(candidate + "/"))
        //    {
        //        return ComponentType.Prefix;
        //    }

        //    if (candidate.Length == 1 && SingleCharPrefixes.Contains(candidate))
        //    {
        //        return ComponentType.Prefix;
        //    }

        //    return ComponentType.Unknown;
        //}

        /// <summary>
        /// Strip the prefix and make sure there are numbers and letters left.
        /// //one of "@","@@","#@","#@@" followed by 1-4 digits followed by 1-6 letters
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        //private ComponentType VerifyIfCallSign(string candidate)
        //{
        //    int digits = 0;

        //    // strip prefix
        //    switch (candidate)
        //    {
        //        case string _ when candidate.Take(2).All(char.IsLetter): // "@@"
        //            candidate = candidate.Remove(0, 2);
        //            break;
        //        case string _ when candidate.Take(1).All(char.IsLetter): // "@"
        //            candidate = candidate.Remove(0, 1);
        //            break;
        //        case string _ when candidate.Take(1).All(char.IsDigit) && candidate.Take(2).Skip(1).All(char.IsLetter): // "#@@"
        //            candidate = candidate.Remove(0, 3);
        //            break;
        //        case string _ when candidate.Take(1).All(char.IsDigit) && candidate.Take(1).Skip(1).All(char.IsLetter): // #@
        //            candidate = candidate.Remove(0, 2);
        //            break;
        //        default:
        //            return ComponentType.Unknown;
        //    }

        //    try
        //    {
        //        // count letters and  digits
        //        while (candidate.Take(1).All(char.IsDigit))
        //        {
        //            if (!string.IsNullOrEmpty(candidate))
        //            {
        //                digits++;
        //                candidate = candidate.Remove(0, 1);
        //                if (candidate.Length == 0) { return ComponentType.Invalid; }
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var q = ex.Message;
        //        // bury exception
        //    }

        //    if (digits > 0 && digits <= 4)
        //    {
        //        if (candidate.Length < 6 && candidate.All(char.IsLetter))
        //        {
        //            return ComponentType.CallSign;
        //        }
        //    }

        //    return ComponentType.Invalid;
        //}

        ///// <summary>
        ///// Build a pattern that models the string passed in.
        ///// </summary>
        ///// <param name="candidate"></param>
        ///// <returns></returns>
        //private string BuildPattern(string candidate)
        //{
        //    string pattern = "";

        //    foreach (char item in candidate)
        //    {
        //        if (Char.IsLetter(item))
        //        {
        //            pattern += "@";
        //        }

        //        if (char.IsDigit(item))
        //        {
        //            pattern += "#";
        //        }

        //        if (char.IsPunctuation(item) || char.IsWhiteSpace(item))
        //        {
        //            pattern += "?";
        //        }
        //    }

        //    return pattern;
        //}

        ///// <summary>
        ///// Just a quick test for grossly invalid call signs.
        ///// </summary>
        ///// <param name="item"></param>
        ///// <returns></returns>
        //private StringTypes GetComponentType(string item)
        //{
        //    switch (item)
        //    {
        //        //W6 OP -no spaces alowed
        //        case string _ when item.Any(char.IsWhiteSpace):
        //            return StringTypes.Invalid;

        //        case string _ when string.IsNullOrEmpty(item):
        //            return StringTypes.Invalid;

        //        // W$OP - no special characters allowed
        //        case string _ when item.Any(c => !char.IsLetterOrDigit(c)):
        //            return StringTypes.Invalid;

        //        // WAOP
        //        //case string _ when item.All(char.IsLetter):
        //        //    return StringTypes.Invalid;

        //        // 9A, 44 - is considered letter - will later check if known prefix
        //        //case string _ when item.Length == 2:
        //        //    if (item.Any(char.IsDigit) && item.Any(char.IsLetter))
        //        //    {
        //        //        return StringTypes.Text;
        //        //    }
        //        //    break;

        //        // 37747
        //        //case string _ when item.Length > 1 && item.All(char.IsDigit):
        //        //    return StringTypes.Invalid;

        //        // N666
        //        //case string _ when HasExcessDigits(item):
        //        //    return StringTypes.Invalid;

        //        // call is too long
        //        case string _ when item.Length > 13:
        //            return StringTypes.Invalid;

        //        default:
        //            return StringTypes.Valid;
        //    }

        //    //return StringTypes.Valid;
        //}

        /// <summary>
        /// Catch calls like N666 but allow R44xxx
        /// Still allows invalid calls like 55PT
        /// THIS IS PROBABLY UNECESSARY NOW - I do a beter check in is cal or prefix section
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        //private bool HasExcessDigits(string item)
        //{

        //    // test for number of digits
        //    IEnumerable<char> stringQuery =
        //              from ch in item
        //              where Char.IsDigit(ch)
        //              select ch;

        //    if (stringQuery.Count() > 2)
        //    {
        //        return true;
        //    }


        //    return false; // GT3UCQ/PGT3UCQ/P
        //}

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
        private void CollectMatches(CallStructure callStructure, string fullCall)
        {
            string prefix = callStructure.Prefix;
            string baseCall = callStructure.BaseCall;
            int[] portableDXCC = { };
            CallStructureType callStructureType = callStructure.CallStuctureType;

            string searchTerm;

            // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';

            switch (callStructureType) // GT3UCQ/P
            {
                case CallStructureType.CallPrefix:
                   portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall);
                    return;
                case CallStructureType.PrefixCall:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall);
                    return;
                case CallStructureType.CallPortablePrefix:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall);
                    return;
                case CallStructureType.CallPrefixPortable:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall);
                    return;
                case CallStructureType.PrefixCallPortable:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall);
                    return;
                case CallStructureType.PrefixCallText:
                    portableDXCC = CheckForPortablePrefix(callStructure: callStructure, fullCall);
                    return;
                default:
                    searchTerm = baseCall;
                    break;
            }

            string persistSearchTerm = searchTerm;

            // is the full call in the dictionary, never will be more than one
            if (CallSignDictionary.TryGetValue(searchTerm, out var lookup))
            {
                var callSignInfo = lookup.First();
                // now if it has a numeric suffix replace it and start again
                if (callStructureType == CallStructureType.CallDigit && callSignInfo.Kind != PrefixKind.InvalidPrefix)
                {
                    string result = new String(baseCall.Where(x => Char.IsDigit(x)).ToArray());
                    searchTerm = baseCall.Replace(result, prefix);
                    callStructure.CallStuctureType = CallStructureType.Call;
                    callStructure.BaseCall = searchTerm;
                    callStructure.Prefix = "";
                    CollectMatches(callStructure, fullCall);
                    return;
                }

                //var callSignInfo = lookup.First();
                if (callSignInfo.Kind != PrefixKind.InvalidPrefix)
                {
                    var callSignInfoCopy = callSignInfo.ShallowCopy();
                    callSignInfoCopy.CallSign = fullCall;
                    callSignInfoCopy.BaseCall = baseCall;
                    //callSignInfoCopy.SearchPrefix = searchTerm;
                    HitList.Add(callSignInfoCopy);

                    if (callSignInfo.Kind != PrefixKind.DXCC)
                    {
                        var callSignInfoDxcc = Adifs[Convert.ToInt32(callSignInfo.DXCC)];
                        var callSignInfoCopyDxcc = callSignInfoDxcc.ShallowCopy();
                        callSignInfoCopyDxcc.CallSign = fullCall;
                        callSignInfoCopyDxcc.BaseCall = baseCall;
                        //callSignInfoCopyDxcc.SearchPrefix = searchTerm; // this needs correction ??
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
                        // now if it has a numeric suffix replace it and start again
                        if (callStructureType == CallStructureType.CallDigit && callSignInfo.Kind != PrefixKind.InvalidPrefix)
                        {
                            string result = new String(baseCall.Where(x => Char.IsDigit(x)).ToArray());
                            searchTerm = baseCall.Replace(result, prefix);
                            callStructure.CallStuctureType = CallStructureType.Call;
                            callStructure.BaseCall = searchTerm;
                            callStructure.Prefix = "";
                            CollectMatches(callStructure, fullCall);
                            return;
                        }
                        //var callSignInfo = query.First();
                        if (callSignInfo.Kind != PrefixKind.InvalidPrefix)
                        {
                            var callSignInfoCopy = callSignInfo.ShallowCopy();
                            callSignInfoCopy.CallSign = fullCall;
                            callSignInfoCopy.BaseCall = baseCall;
                            //callSignInfoCopy.SearchPrefix = searchTerm;
                            HitList.Add(callSignInfoCopy);
                            if (callSignInfo.Kind != PrefixKind.DXCC)
                            {
                                var callSignInfoDxcc = Adifs[Convert.ToInt32(callSignInfo.DXCC)];
                                var callSignInfoCopyDxcc = callSignInfoDxcc.ShallowCopy();
                                callSignInfoCopyDxcc.CallSign = fullCall;
                                callSignInfoCopyDxcc.BaseCall = baseCall;
                                //callSignInfoCopyDxcc.SearchPrefix = searchTerm;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        private int[] CheckForPortablePrefix(CallStructure  callStructure, string fullCall)
        {
            string prefix = callStructure.Prefix;
            string baseCall = callStructure.BaseCall;
            string searchTerm;
            int[] dxcc = { };

            if (callStructure.CallStuctureType == CallStructureType.CallPrefix)
            {
                searchTerm = prefix + "/";
            }
            else
            {
                searchTerm = baseCall + "/";
            }

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
                    //callSignInfoCopy.SearchPrefix = searchTerm;// this needs correction
                    HitList.Add(callSignInfoCopy);
                    dxccList.Add(callSignInfoCopy.DXCC);
                }
                dxcc = dxccList.ToArray();
            }
            return dxcc;
        }

        /// <summary>
        /// Some calls overlap different entities, specifically 4U1A (IARC) and 4U1N (ITU) and Austria
        /// Others like B1Z and DL(6DH) only are in the Adifs list
        /// so I check the DXCC list (Adifs) to find them
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        private void CheckAdditionalDXCCEntities(CallStructure callStructure, string fullCall, string searchTerm)
        {
            var query = Adifs.Values.Where(q => q.PrefixKey.ContainsKey(searchTerm)).ToList();

            foreach (CallSignInfo callSignInfo in query)
            {
                var callSignInfoCopy = callSignInfo.ShallowCopy();
                callSignInfoCopy.CallSign = fullCall;
                callSignInfoCopy.BaseCall = callStructure.BaseCall;
                //callSignInfoCopy.SearchPrefix = callStructure.prefix;
                HitList.Add(callSignInfoCopy);
            }

            if (query.Count == 1)
            {
                return; // check this
            }

            // IF MULTIPLE HITS HERE NEED TO NARROW THEM DOWN - GB4BYR
            // this may not be necessary
            //if (query.Count > 1)
            //{
            //    MergeHits(query);
            //    return;
            //}


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

            // NEED TO FIND OUT IF I AM MISSING ANYTHING - DEBUGGING ONLY
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine(fullCall);
            }
        }

        private void MergeHits(List<CallSignInfo> query)
        {
            CallSignInfo callSignInfo = query.First();

            foreach (CallSignInfo info in query.Skip(1))
            {
                if (info.DXCC != callSignInfo.DXCC)
                {
                    //callSignInfo.
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
