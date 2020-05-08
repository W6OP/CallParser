
/*
 CallLookUp.cs
 CallParser
 
 Created by Peter Bourget on 3/11/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Analyze a call sign and find its meta data using the call sign prefix.
 */
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public class CallLookUp
    {
        /// <summary>
        /// True indicates multiple hits are merged into one.
        /// </summary>
        private bool mergeHits;
        public bool MergeHits { get => mergeHits; set => mergeHits = value; }
        

        private ConcurrentBag<CallSignInfo> HitList;
        //
        private readonly ConcurrentDictionary<string, List<CallSignInfo>> CallSignDictionary;
        //
        private SortedDictionary<int, CallSignInfo> Adifs { get; set; }
        // 
        private ConcurrentDictionary<string, CallSignInfo> HitCache;

       
        private QRZLookup QRZLookup = new QRZLookup();

        // disable caching when not doing batch lookups
        private bool IsBatchLookup = false;
        private string QRZUserId;
        private string QRZPassword;
        private bool UseQRZLookup;

        private readonly ConcurrentDictionary<string, List<CallSignInfo>> PortablePrefixes;
       
        //private readonly string[] _OneLetterSeries = { "B", "F", "G", "I", "K", "M", "N", "R", "W", "2" };
        //private readonly string[] SingleCharPrefixes = { "F", "G", "M", "I", "R", "W" };
        // added "R" as a beacon for R/IK3OTW
        // "U" for U/K2KRG
        //private readonly string[] RejectPrefixes = { "AG", "U", "R", "A", "B", "M", "P", "MM", "AM", "QR", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL", "MOBILE" };
        // ValidSuffixes = ':A:B:M:P:MM:AM:QRP:QRPP:LH:LGT:ANT:WAP:AAW:FJL:';

        //private CallStructure CallStructure = new CallStructure();

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="prefixFileParser"></param>
        public CallLookUp(PrefixFileParser prefixFileParser)
        {
            CallSignDictionary = prefixFileParser.CallSignDictionary;
            Adifs = prefixFileParser.Adifs;
            PortablePrefixes = prefixFileParser.PortablePrefixes;

            QRZLookup.OnErrorDetected += QRZLookup_OnErrorDetected;
        }

        private void QRZLookup_OnErrorDetected(string message)
        {
            throw new Exception(message);
        }

        /// <summary>
        /// Look up a single call sign. First make sure it is a valid call sign.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns>IEnumerable<CallSignInfo></returns>
        public IEnumerable<CallSignInfo> LookUpCall(string callSign)
        {
            HitList = new ConcurrentBag<CallSignInfo>();
            IsBatchLookup = false;

            try
            {
                ProcessCallSign(callSign.ToUpper());
            }
            catch (Exception)
            {
                throw new Exception("Invalid call sign format.");
            }

            return HitList.AsEnumerable();
        }

        /// <summary>
        /// Look up a single call sign. First make sure it is a valid call sign.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns>IEnumerable<CallSignInfo></returns>
        public IEnumerable<CallSignInfo> LookUpCall(string callSign, string userId, string password)
        {
            HitList = new ConcurrentBag<CallSignInfo>();
            IsBatchLookup = false;
            UseQRZLookup = true;

            QRZUserId = userId;
            QRZPassword = password;

            try
            {
                ProcessCallSign(callSign.ToUpper());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

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
            HitCache = new ConcurrentDictionary<string, CallSignInfo>();
            
            IsBatchLookup = true;

            if (callSigns == null)
            {
                throw new Exception("The call sign list must contain at least one entry.");
            }


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
            CallStructure callStructure;

            callSign = callSign.Trim();

            // strip leading or trailing "/"  /W6OP/
            if (callSign.First() == '/')
            {
                callSign = callSign.Substring(1);
            }

            if (callSign.Last() == '/')
            {
                callSign = callSign.Remove(callSign.Length - 1, 1);
            }

            // look in the cache first
            if (IsBatchLookup)
            {
                if (HitCache.ContainsKey(callSign))
                {
                    HitList.Add(HitCache[callSign]);
                    return;
                }
            }

            callStructure = new CallStructure(callSign, PortablePrefixes);

            if (callStructure.CallStructureType != CallStructureType.Invalid)
            {
                CollectMatches(callStructure, callSign);
            }
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

        private void CollectMatches(CallStructure callStructure, string fullCall)
        {
            CallStructureType callStructureType = callStructure.CallStructureType;

            // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
            try
            {
                switch (callStructureType) // GT3UCQ/P
                {
                    case CallStructureType.CallPrefix:
                        if (CheckForPortablePrefix(callStructure: callStructure, fullCall)) { return; }
                        break;
                    case CallStructureType.PrefixCall:
                        if (CheckForPortablePrefix(callStructure: callStructure, fullCall)) { return; }
                        break;
                    case CallStructureType.CallPortablePrefix:
                        if (CheckForPortablePrefix(callStructure: callStructure, fullCall)) { return; }
                        break;
                    case CallStructureType.CallPrefixPortable:
                        if (CheckForPortablePrefix(callStructure: callStructure, fullCall)) { return; }
                        break;
                    case CallStructureType.PrefixCallPortable:
                        if (CheckForPortablePrefix(callStructure: callStructure, fullCall)) { return; }
                        break;
                    case CallStructureType.PrefixCallText:
                        if (CheckForPortablePrefix(callStructure: callStructure, fullCall)) { return; }
                        break;
                    case CallStructureType.CallDigit:
                        if (CheckReplaceCallArea(callStructure: callStructure, fullCall)) { return; }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                var e = ex.Message;
            }

            try
            {
                if (SearchMainDictionary(callStructure, fullCall, true, out _))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return;
        }
        /// <summary>
        /// THIS IS DUPLICATED IN CallStructure.
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

                // THIS IS DIFFERENT FROM CallStructure
                if (char.IsPunctuation(item))
                {
                    pattern += "/";
                }
            }

            if (pattern.Length > 7)
            {
                pattern = pattern.Substring(0, 7);
            }
            return pattern;
        }

        /// <summary>
        /// Search the CallSignDictionary for a hit with the full call. If it doesn't 
        /// hit remove characters from the end until hit or there are no letters fleft. 
        /// Return the CallSignInfo as an out parameter for the ReplaceCallArea() function. 
        /// string[] validCallStructures = { "@#@@", "@#@@@", "@##@", "@##@@", "@##@@@", "@@#@", "@@#@@", "@@#@@@", "#@#@", "#@#@@", "#@#@@@", "#@@#@", "#@@#@@" };
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="baseCall"></param>
        /// <param name="fullCall"></param>
        /// <param name="saveHit"></param>
        /// <param name="mainPrefix"></param>
        /// <returns></returns>
        private bool SearchMainDictionary(CallStructure callStructure, string fullCall, bool saveHit, out string mainPrefix)
        {
            var baseCall = callStructure.BaseCall;
            var prefix = callStructure.Prefix;
            var list = new HashSet<CallSignInfo>();
            var foundItems = new HashSet<CallSignInfo>();
            var temp = new HashSet<CallSignInfo>();
            bool stopFound = false;

            string pattern;
            string firstLetter;
            string nextLetter;
            string searchBy;

            switch (callStructure.CallStructureType)
            {
                case CallStructureType _ when callStructure.CallStructureType == CallStructureType.PrefixCall 
                                        || callStructure.CallStructureType == CallStructureType.PrefixCallPortable
                                        || callStructure.CallStructureType == CallStructureType.PrefixCallText
                                        && prefix.Length == 1:
                    searchBy = prefix;
                    firstLetter = prefix.Substring(0, 1);
                    nextLetter = "";
                    pattern = BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCall:
                    searchBy = prefix;
                    firstLetter = prefix.Substring(0, 1);
                    nextLetter = callStructure.Prefix.Substring(1, 1);
                    pattern = BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCallPortable:
                    searchBy = prefix;
                    firstLetter = callStructure.Prefix.Substring(0, 1);
                    nextLetter = callStructure.Prefix.Substring(1, 1);
                    pattern = BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCallText:
                    searchBy = prefix;
                    firstLetter = callStructure.Prefix.Substring(0, 1);
                    nextLetter = callStructure.Prefix.Substring(1, 1);
                    pattern = BuildPattern(callStructure.Prefix);
                    break;
                default:
                    searchBy = baseCall;
                    firstLetter = baseCall.Substring(0, 1);
                    nextLetter = baseCall.Substring(1, 1);
                    pattern = BuildPattern(callStructure.BaseCall);
                    break;
            }

            // first we look in all the "." patterns for calls like KG4AA vs KG4AAA
            pattern += ".";
            while (pattern.Length > 1)
            {
                if (CallSignDictionary.TryGetValue(pattern, out var query))
                {
                    temp = new HashSet<CallSignInfo>();
                    foreach (var callSignInfo in query)
                    {
                        if (callSignInfo.IndexKey.ContainsKey(firstLetter))
                        {
                            if (pattern.Last() == '.')
                            {
                                if (callSignInfo.MaskExists(searchBy, pattern.Length - 1))
                                {
                                    temp.Add(callSignInfo);
                                    break;
                                }
                            }
                            else
                            {
                                if (callSignInfo.MaskExists(searchBy, pattern.Length))
                                {
                                    temp.Add(callSignInfo);
                                }
                            }
                        }
                    }

                    if (temp.Count != 0)
                    {
                        if (pattern.Last() == '.')
                        {
                            list.UnionWith(temp);
                            break;
                        }
                        list.UnionWith(temp);
                        break;
                    }
                }

                pattern = pattern.Remove(pattern.Length - 1);
            }

            // now we have a list of posibilities // HG5ACZ/P 
            if (list.Count > 0)
            {
                if (list.Count == 1)
                {
                    // only one found
                    foundItems = list;
                }
                else // refine the hits
                {
                    if (list.Count > 2)
                    {
                        var a = 2;
                    }
                    foreach (CallSignInfo info in list)
                    {
                        var rank = 0;
                        var previous = true;
                        var primaryMaskList = info.GetMaskList(firstLetter, nextLetter, stopFound);

                        foreach (List<string[]> maskList in primaryMaskList) // ToList uneccessary here
                        {
                            var position = 2;
                            previous = true;

                            // get smaller length
                            var length = baseCall.Length < maskList.Count ? baseCall.Length : maskList.Count;

                            for (var i = 2; i < length; i++)
                            {
                                var anotherLetter = baseCall.Substring(i, 1); //.Skip(i).First().ToString();

                                if (maskList[position].Contains(anotherLetter) && previous)
                                {
                                    rank = position + 1;
                                }
                                else
                                {
                                    previous = false;
                                    break;
                                }
                                position += 1;
                            }

                            // if found with 2 chars
                            if (rank == length || maskList.Count == 2)
                            {
                                info.Rank = rank; 
                                foundItems.Add(info);
                            }
                        }
                    }
                }

                if (foundItems.Count > 0)
                {
                    if (!saveHit)
                    {
                        mainPrefix = foundItems.First().MainPrefix;
                        return true;
                    }
                    else
                    {
                        if (!MergeHits || foundItems.Count == 1)
                        {
                            BuildHit(foundItems, callStructure, baseCall, fullCall);
                            mainPrefix = "";
                        }
                        else
                        {
                            MergeMultipleHits(foundItems, callStructure, baseCall, fullCall);
                            mainPrefix = "";
                        }

                        return true;
                    }
                }
            }

            mainPrefix = "";
            return false;
        }

        /// <summary>
        /// Portable prefixes are prefixes that end with "/"
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        /// <returns></returns>
        private bool CheckForPortablePrefix(CallStructure callStructure, string fullCall)
        {
            string prefix = callStructure.Prefix + "/";
            var list = new HashSet<CallSignInfo>();
            var temp = new HashSet<CallSignInfo>();
            var firstLetter = prefix.Substring(0, 1);
            var pattern = BuildPattern(prefix);

            if (PortablePrefixes.TryGetValue(pattern, out var query))
            {
                foreach (var callSignInfo in query)
                {
                    temp.Clear();
                    if (callSignInfo.IndexKey.ContainsKey(firstLetter))
                    {
                        if (callSignInfo.PortableMaskExists(prefix))
                        {
                            temp.Add(callSignInfo);
                        }
                    }

                    if (temp.Count != 0)
                    {
                        list.UnionWith(temp);
                        break;
                    }
                }
            }

            if (list.Count > 0)
            {
                BuildHit(list, callStructure, prefix, fullCall);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Build the hit and add it to the hitlist.
        /// </summary>
        /// <param name="foundItems"></param>
        /// <param name="baseCall"></param>
        /// <param name="prefix"></param>
        /// <param name="fullCall"></param>
        private void BuildHit(HashSet<CallSignInfo> foundItems, CallStructure callStructure, string prefix, string fullCall)
        {
            CallSignInfo callSignInfoCopy = new CallSignInfo();
            List<CallSignInfo> HighestRankList = foundItems.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Kind).ToList();
            Dictionary<int, int> dxccEntries = new Dictionary<int, int>();

            foreach (CallSignInfo callSignInfo in HighestRankList)
            {
                callSignInfoCopy = callSignInfo.ShallowCopy();
                callSignInfo.CallSignFlags = new HashSet<CallSignFlags>();
                callSignInfoCopy.CallSign = fullCall;
                callSignInfoCopy.BaseCall = callStructure.BaseCall;
                callSignInfoCopy.HitPrefix = prefix;
                callSignInfoCopy.CallSignFlags.UnionWith(callStructure.CallSignFlags);
                HitList.Add(callSignInfoCopy);
               
                // add calls to the cache - if the call exists we won't have to redo all the 
                // processing earlier the next time it comes in
                if (IsBatchLookup)
                {
                    if (!HitCache.ContainsKey(callSignInfoCopy.CallSign))
                    {
                        HitCache.TryAdd(callSignInfoCopy.CallSign, callSignInfoCopy);
                    }
                }

                if (UseQRZLookup)
                {
                    if (QRZLookup.QRZLogon(QRZUserId, QRZPassword))
                    {
                        XDocument xDocument = QRZLookup.QRZRequest(callStructure.BaseCall);
                        CallSignInfo callSignInfoQRZ = new CallSignInfo(xDocument);
                        HitList.Add(callSignInfoQRZ);
                    }
                    
                }
            }
        }

        /// <summary>
        /// If multiple hits are found for a call sign, merge them into a single hit.
        /// Build one hit and add all the dxcc numbers to the dxcc list. Also merge the
        /// CQ, ITU, Admin1 and Admin2 fields.  EU3DMB/R
        /// </summary>
        /// <param name="foundItems"></param>
        /// <param name="baseCall"></param>
        /// <param name="prefix"></param>
        /// <param name="fullCall"></param>
        private void MergeMultipleHits(HashSet<CallSignInfo> foundItems, CallStructure callStructure, string prefix, string fullCall)
        {
            CallSignInfo callSignInfoCopy = new CallSignInfo();
            List<CallSignInfo> HighestRankList = new List<CallSignInfo>();
            //int count = 1;
            HighestRankList = foundItems.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Kind).ToList();
            var highestRanked = HighestRankList[0];

           // need to deep clone to modify properties
            highestRanked.DeepCopy(ref highestRanked, ref callSignInfoCopy);
            callSignInfoCopy.CallSign = fullCall;
            callSignInfoCopy.BaseCall = callStructure.BaseCall;
            callSignInfoCopy.HitPrefix = prefix;
            callSignInfoCopy.IsMergedHit = true;
            callSignInfoCopy.CallSignFlags.UnionWith(callStructure.CallSignFlags);

            foreach (CallSignInfo callSignInfo in HighestRankList.Skip(1))
            {
                if (callSignInfo.DXCC != callSignInfoCopy.DXCC)
                {
                    callSignInfoCopy.DXCCMerged.Add(callSignInfo.DXCC);
                }
                callSignInfoCopy.ITU.UnionWith(callSignInfo.ITU);
                callSignInfoCopy.CQ.UnionWith(callSignInfo.CQ);
                callSignInfoCopy.CallSignFlags.UnionWith(callStructure.CallSignFlags);
                if (callSignInfoCopy.Admin1 != callSignInfo.Admin1)
                {
                    callSignInfoCopy.Admin1 = "";
                    callSignInfoCopy.CallSignFlags.Add(CallSignFlags.AmbigPrefix); // THIS MAY BE INCORRECT
                }

                if (callSignInfoCopy.Admin2 != callSignInfo.Admin2)
                {
                    callSignInfoCopy.Admin2 = "";
                }
            }

            callSignInfoCopy.DXCCMerged = callSignInfoCopy.DXCCMerged.OrderBy(item => item).ToHashSet();

            HitList.Add(callSignInfoCopy);

            // add calls to the cache - if the call exists we won't have to redo all the 
            // processing earlier the next time it comes in
            if (IsBatchLookup)
            {
                if (!HitCache.ContainsKey(callSignInfoCopy.CallSign))
                {
                    HitCache.TryAdd(callSignInfoCopy.CallSign, callSignInfoCopy);
                }
            }
        }

        /// <summary>
        /// Check if the call area needs to be replaced and do so if necessary.
        /// If the original call gets a hit, find the MainPrefix and replace
        /// the call area with the new call area. Then do a search with that.
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        private bool CheckReplaceCallArea(CallStructure callStructure, string fullCall)
        {
            // UY0KM/0
            if (callStructure.Prefix != callStructure.BaseCall.FirstOrDefault(c => char.IsDigit(c)).ToString())
            {
                try
                {
                    if (SearchMainDictionary(callStructure, fullCall, false, out string mainPrefix))
                    {
                        var oldDigit = callStructure.Prefix;
                        callStructure.Prefix = ReplaceCallArea(mainPrefix, callStructure.Prefix, out int position);
                        switch (callStructure.Prefix)
                        {
                            case "":
                                // M0CCA/6 - main prefix is "G", F8ATS/9 - Should I replace the digit?
                                callStructure.CallStructureType = CallStructureType.Call;
                                break;
                            default:
                                // replace the digit in case we don't find it by it's main prefix
                                callStructure.BaseCall = callStructure.BaseCall.Remove(position - 1, 1).Insert(position - 1, oldDigit);
                                callStructure.CallStructureType = CallStructureType.PrefixCall;
                                break;
                        }

                        CollectMatches(callStructure, fullCall);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    var a = 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Replace the call area with the prefix digit.
        /// </summary>
        /// <param name="mainPrefix"></param>
        /// <param name="callArea"></param>
        /// <returns></returns>
        private string ReplaceCallArea(string mainPrefix, string callArea, out int position)
        {
            char[] OneCharPrefs = new char[] { 'I', 'K', 'N', 'W', 'R', 'U' };
            char[] XNUM_SET = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '#', '[' };

            int p = 0;

            switch (mainPrefix.Length)
            {
                case 1:
                    if (OneCharPrefs.Contains(mainPrefix.First()))
                    {
                        p = 2;
                    }
                    else if (mainPrefix.All(char.IsLetter))
                    {
                        position = 99;
                        return "";
                    }
                    break;
                case 2:
                    if (OneCharPrefs.Contains(mainPrefix.First()) && XNUM_SET.Contains(mainPrefix.Skip(1).First()))
                    {
                        p = 2;
                    }
                    else
                    {
                        p = 3;
                    }
                    break;
                default:
                    if (OneCharPrefs.Contains(mainPrefix.First()) && XNUM_SET.Contains(mainPrefix.Skip(1).Take(1).First()))
                    {
                        p = 2;
                    }
                    else
                    {
                        if (XNUM_SET.Contains(mainPrefix.Skip(2).Take(1).First()))
                        {
                            p = 3;
                        }
                        else
                        {
                            p = 4;
                        }
                    }
                    break;
            }

            position = p;

            return $"{mainPrefix.Substring(0, p - 1)}{callArea}";
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
