
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

        private ConcurrentBag<Hit> HitList;
        //
        private readonly ConcurrentDictionary<string, List<PrefixData>> CallSignPatterns;
        //
        private SortedDictionary<int, PrefixData> Adifs { get; set; }
        // 
        private ConcurrentDictionary<string, Hit> HitCache;
       
        private QRZLookup QRZLookup = new QRZLookup();

        // disable caching when not doing batch lookups
        private bool IsBatchLookup = false;
        private string QRZUserId;
        private string QRZPassword;
        private bool UseQRZLookup;

        private readonly ConcurrentDictionary<string, List<PrefixData>> PortablePrefixes;
       
        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="prefixFileParser"></param>
        public CallLookUp(PrefixFileParser prefixFileParser)
        {
            CallSignPatterns = prefixFileParser.CallSignPatterns;
            Adifs = prefixFileParser.Adifs;
            PortablePrefixes = prefixFileParser.PortablePrefixes;

            QRZLookup.OnErrorDetected += QRZLookup_OnErrorDetected;
            QRZLookup.OnCallNotFound += QRZLookup_OnCallNotFound;
        }

        private void QRZLookup_OnCallNotFound(string message)
        {
            //throw new NotImplementedException();
        }

        private void QRZLookup_OnErrorDetected(string message)
        {
            throw new Exception(message);
        }

        /// <summary>
        /// Look up a single call sign. First make sure it is a valid call sign.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns>IEnumerable<Hit></returns>
        public IEnumerable<Hit> LookUpCall(string callSign)
        {
            HitList = new ConcurrentBag<Hit>();
            IsBatchLookup = false;
            UseQRZLookup = false;

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
        /// <returns>IEnumerable<Hit></returns>
        public IEnumerable<Hit> LookUpCall(string callSign, string userId, string password)
        {
            HitList = new ConcurrentBag<Hit>();
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
        /// <returns>IEnumerable<Hit></returns>
        public IEnumerable<Hit> LookUpCall(List<string> callSigns)
        {
            HitList = new ConcurrentBag<Hit>();
            HitCache = new ConcurrentDictionary<string, Hit>();
            
            IsBatchLookup = true;
            UseQRZLookup = false;

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

            // if there is a spce in the call, reject it immediatley
            if (callSign.Any(char.IsWhiteSpace))
            {
                return;
            }

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
        /// Search the CallSignDictionary for a hit with the full call. If it doesn't 
        /// hit remove characters from the end until hit or there are no letters fleft.  
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
            var list = new HashSet<PrefixData>();
            var foundItems = new HashSet<PrefixData>();
            HashSet<PrefixData> temp;
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
                    pattern = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCall:
                    searchBy = prefix;
                    firstLetter = prefix.Substring(0, 1);
                    nextLetter = callStructure.Prefix.Substring(1, 1);
                    pattern = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCallPortable:
                    searchBy = prefix;
                    firstLetter = callStructure.Prefix.Substring(0, 1);
                    nextLetter = callStructure.Prefix.Substring(1, 1);
                    pattern = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCallText:
                    searchBy = prefix;
                    firstLetter = callStructure.Prefix.Substring(0, 1);
                    nextLetter = callStructure.Prefix.Substring(1, 1);
                    pattern = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                default:
                    searchBy = baseCall;
                    firstLetter = baseCall.Substring(0, 1);
                    nextLetter = baseCall.Substring(1, 1);
                    pattern = callStructure.BuildPattern(callStructure.BaseCall);
                    break;
            }

            // first we look in all the "." patterns for calls like KG4AA vs KG4AAA
            pattern += ".";
            while (pattern.Length > 1)
            {
                if (CallSignPatterns.TryGetValue(pattern, out var query))
                {
                    temp = new HashSet<PrefixData>();
                    foreach (var prefixData in query)
                    {
                        if (prefixData.IndexKey.ContainsKey(firstLetter))
                        {
                            if (pattern.Last() == '.')
                            {
                                if (prefixData.MaskExists(searchBy, pattern.Length - 1))
                                {
                                    temp.Add(prefixData);
                                    break;
                                }
                            }
                            else
                            {
                                if (prefixData.MaskExists(searchBy, pattern.Length))
                                {
                                    temp.Add(prefixData);
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
                   foreach (PrefixData info in list)
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
            var list = new HashSet<PrefixData>();
            var temp = new HashSet<PrefixData>();
            var firstLetter = prefix.Substring(0, 1);
            var pattern = callStructure.BuildPattern(prefix);

            if (PortablePrefixes.TryGetValue(pattern, out var query))
            {
                foreach (var prefixData in query)
                {
                    temp.Clear();
                    if (prefixData.IndexKey.ContainsKey(firstLetter))
                    {
                        if (prefixData.PortableMaskExists(prefix))
                        {
                            temp.Add(prefixData);
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
        private void BuildHit(HashSet<PrefixData> foundItems, CallStructure callStructure, string prefix, string fullCall)
        {
            PrefixData prefixDataCopy = new PrefixData();
           
            List<PrefixData> HighestRankList = foundItems.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Kind).ToList();
          
            foreach (PrefixData prefixData in HighestRankList)
            {
                Hit hit = new Hit(prefixData);
                hit.CallSign = fullCall;
                //hit.CallSignFlags = callStructure.CallSignFlags;
                hit.CallSignFlags.UnionWith(callStructure.CallSignFlags);
                HitList.Add(hit);

                // add calls to the cache - if the call exists we won't have to redo all the 
                // processing earlier the next time it comes in
                if (IsBatchLookup)
                {
                    HitCache.TryAdd(hit.CallSign, hit);
                }

                if (!UseQRZLookup)
                {
                    continue;
                }

                if (QRZLookup.QRZLogon(QRZUserId, QRZPassword))
                {
                    XDocument xDocument = QRZLookup.QRZRequest(callStructure.BaseCall);
                    if (xDocument != null)
                    {
                        PrefixData prefixDataQRZ = new PrefixData(xDocument);
                        HitList.Add(new Hit(prefixDataQRZ));
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
        private void MergeMultipleHits(HashSet<PrefixData> foundItems, CallStructure callStructure, string prefix, string fullCall)
        {
            List<PrefixData> HighestRankList = new List<PrefixData>();
            Hit hit = new Hit();
            HighestRankList = foundItems.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Kind).ToList();
            var highestRanked = HighestRankList[0];


            hit = new Hit(highestRanked)
            {
                CallSign = fullCall,
                IsMergedHit = true
            };

            hit.CallSignFlags.UnionWith(callStructure.CallSignFlags);

            foreach (PrefixData prefixData in HighestRankList.Skip(1))
            {
                if (hit.DXCC != prefixData.DXCC)
                {
                    hit.DXCCMerged.Add(prefixData.DXCC);
                }

                hit.ITU.UnionWith(prefixData.ITU);
                hit.CQ.UnionWith(prefixData.CQ);
                hit.CallSignFlags.UnionWith(callStructure.CallSignFlags);
               
                if (hit.Admin1 != prefixData.Admin1)
                {
                    hit.Admin1 = "";
                    hit.CallSignFlags.Add(CallSignFlags.AmbigPrefix); // THIS MAY BE INCORRECT
                }

                if (hit.Admin2 != prefixData.Admin2)
                {
                    hit.Admin2 = "";
                }
            }

            hit.DXCCMerged = hit.DXCCMerged.OrderBy(item => item).ToHashSet();

            HitList.Add(hit);

            // add calls to the cache - if the call exists we won't have to redo all the 
            // processing earlier the next time it comes in
            if (IsBatchLookup)
            {
                if (!HitCache.ContainsKey(hit.CallSign))
                {
                    HitCache.TryAdd(hit.CallSign, hit);
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
            // see if the prefix == the first digit in the BaseCall
            if (callStructure.Prefix != callStructure.BaseCall.FirstOrDefault(c => char.IsDigit(c)).ToString())
            {
                try
                {
                    if (SearchMainDictionary(callStructure, fullCall, false, out string mainPrefix))
                    {
                        callStructure.Prefix = ReplaceCallArea(mainPrefix, callArea: callStructure.Prefix, out int position);
                        callStructure.CallStructureType = callStructure.Prefix switch
                        {
                            "" => CallStructureType.Call,// M0CCA/6 - main prefix is "G", F8ATS/9 mainPrefix = "F"
                            _ => CallStructureType.PrefixCall,
                        };

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
                        // I9MRY/1 - mainPrefix = I --> I1
                        p = 2;
                    }
                    else if (mainPrefix.All(char.IsLetter))
                    {
                        // FA3L/6 - mainPrefix is F - shouldn't we make it F6?
                        // F1FZH/8 
                        // MA2US/1
                        position = 99;
                        return "";
                    }
                    break;
                case 2: 
                    if (OneCharPrefs.Contains(mainPrefix.First()) && XNUM_SET.Contains(mainPrefix.Skip(1).First()))
                    {
                        // W6OP/4 - main prefix = W6 --> W4
                        p = 2;
                    }
                    else
                    {
                        // AL7NS/4 - main prefix = KL --> KL4
                        p = 3;
                    }

                    break;
                default:
                    // this means the same as the case 2 statement and is never hit
                    if (OneCharPrefs.Contains(mainPrefix.First()) && XNUM_SET.Contains(mainPrefix.Skip(1).First()))
                    {
                        p = 2;
                    }
                    else
                    {
                        if (XNUM_SET.Contains(mainPrefix.Skip(2).Take(1).First()))
                        {
                            // JI3DT/6 - mainPrefix = JA3 --> JA6
                            p = 3;
                        }
                        else
                        {
                            // 3DLE/1 - mainprefix = 3DA --> 3DA1 
                            p = 4;
                        }
                    }
                    break;
            }

            position = p;
            // append call area to mainPrefix
            return $"{mainPrefix.Substring(0, p - 1)}{callArea}{"/"}";
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
