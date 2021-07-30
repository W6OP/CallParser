
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
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public class CallLookUp
    {
        // used in ReplaceCallArea()
        private readonly char[] OneCharPrefs = new char[] { 'I', 'K', 'N', 'W', 'R', 'U' };
        private readonly char[] XNUM_SET = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '#', '[' };

        /// <summary>
        /// True indicates multiple hits are merged into one.
        /// </summary>
        private bool mergeHits;
        public bool MergeHits { get => mergeHits; set => mergeHits = value; }

        private ConcurrentBag<Hit> HitList;
        
        private readonly ConcurrentDictionary<string, List<PrefixData>> CallSignPatterns;
        private SortedDictionary<int, PrefixData> adifs;

        //
        private void SetAdifs(SortedDictionary<int, PrefixData> value)
        {
            adifs = value;
        }

        // 
        private ConcurrentDictionary<string, Hit> HitCache;

        private QRZLookup QRZLookup = new QRZLookup();

        // disable caching when not doing batch lookups
        private bool IsBatchLookup = false;
        private string QRZUserId;
        private string QRZPassword;
        private bool UseQRZLookup;

        int debugCounter = 0;

        private readonly ConcurrentDictionary<string, List<PrefixData>> PortablePrefixes;

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="prefixFileParser"></param>
        public CallLookUp(PrefixFileParser prefixFileParser)
        {
            CallSignPatterns = prefixFileParser.CallSignPatterns;
            SetAdifs(prefixFileParser.Adifs);
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
        /// Use QRZ.com lookup when necessary.
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
            //_ = Parallel.ForEach(callSigns, callSign =>
            foreach (var callSign in callSigns)
            {
                try
                {
                    ProcessCallSign(callSign.ToUpper());
                }
                catch (Exception)
                {
                    // extremely rare
                    // bury exception
                }
            }
         // );
            //Console.WriteLine("Debug Counter: " + debugCounter.ToString());
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
            // only trim call sign if neccessary to reduce string allocations
            if (callSign.Any(char.IsWhiteSpace))
            {
                callSign = callSign.Trim();
                // if there is a space in the call, reject it immediatley
                if (callSign.Any(char.IsWhiteSpace))
                {
                    return;
                }
            }

            // strip leading or trailing "/"  /W6OP/
            // in this case .Equals is faster
            if (callSign.First().Equals('/'))
            //if (callSign[0] == '/')
            {
                callSign = callSign.Substring(1);
            }

            if (callSign.Last().Equals('/'))
            {
                callSign = callSign.Remove(callSign.Length - 1, 1);
            }

            if (callSign.IndexOf("//") != -1)
            {
                callSign = callSign.Replace("//", "/");
            }

            if (callSign.IndexOf("///") != -1)
            {
                callSign = callSign.Replace("///", "/");
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

            var  callStructure = new CallStructure(callSign, PortablePrefixes);

            if (callStructure.CallStructureType != CallStructureType.Invalid)
            {
                CollectMatches(callStructure);
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

        private void CollectMatches(CallStructure callStructure)
        {
            var callStructureType = callStructure.CallStructureType;

            // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
            switch (callStructureType) // GT3UCQ/P
            {
                case CallStructureType.CallPrefix:
                    if (CheckForPortablePrefix(callStructure: callStructure)) { return; }
                    break;
                case CallStructureType.PrefixCall:
                    if (CheckForPortablePrefix(callStructure: callStructure)) { return; }
                    break;
                case CallStructureType.CallPortablePrefix:
                    if (CheckForPortablePrefix(callStructure: callStructure)) { return; }
                    break;
                case CallStructureType.CallPrefixPortable:
                    if (CheckForPortablePrefix(callStructure: callStructure)) { return; }
                    break;
                case CallStructureType.PrefixCallPortable:
                    if (CheckForPortablePrefix(callStructure: callStructure)) { return; }
                    break;
                case CallStructureType.PrefixCallText:
                    if (CheckForPortablePrefix(callStructure: callStructure)) { return; }
                    break;
                case CallStructureType.CallDigit:
                    if (CheckReplaceCallArea(callStructure: callStructure)) { return; }
                    break;
                default:
                    break;
            }

            _ = SearchMainDictionary(callStructure, true);
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
        private string SearchMainDictionary(CallStructure callStructure, bool saveHit)
        {
            var baseCall = callStructure.BaseCall;
            var prefix = callStructure.Prefix;
            //var prefixDataList = new HashSet<PrefixData>();
            var matches = new HashSet<PrefixData>();
            StringBuilder patternBuilder;
            string mainPrefix;

            var firstAndSecond = (firstLetter: "", nextLetter: "");

            // this could be simplified but is almost 1 sec faster this way per million calls
            switch (callStructure.CallStructureType)
            {
                case CallStructureType.PrefixCall:
                    firstAndSecond = DetermineMaskComponents(prefix);
                    patternBuilder = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCallPortable:
                    firstAndSecond = DetermineMaskComponents(prefix);
                    patternBuilder = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                case CallStructureType.PrefixCallText:
                    firstAndSecond = DetermineMaskComponents(prefix);
                    patternBuilder = callStructure.BuildPattern(callStructure.Prefix);
                    break;
                default:
                    prefix = baseCall;
                    firstAndSecond.firstLetter = baseCall.Substring(0, 1);
                    firstAndSecond.nextLetter = baseCall.Substring(1, 1);
                    patternBuilder = callStructure.BuildPattern(callStructure.BaseCall);
                    break;
            }

            // first we look in all the "." patterns for calls like KG4AA vs KG4AAA
            //bool stopCharacterFound = MatchPattern(prefixDataList, patternBuilder, firstAndSecond.firstLetter, prefix);
            bool stopCharacterFound = false;
            var prefixDataList = MatchPattern(patternBuilder, firstAndSecond.firstLetter, prefix, out stopCharacterFound);

            // now we have a list of posibilities // HG5ACZ/P 
            if (prefixDataList.Count > 0)
            {
                
                if (prefixDataList.Count.Equals(1))
                {
                    // only one found
                    matches = new HashSet<PrefixData>(prefixDataList);
                }
                else
                {   // refine the hits
                    foreach (PrefixData prefixData in prefixDataList)
                    {
                        var primaryMaskList = prefixData.GetMaskList(firstAndSecond.firstLetter,
                                                                     firstAndSecond.nextLetter,
                                                                     stopCharacterFound);
                        RefineList(baseCall, matches, prefixData, primaryMaskList);
                    }
                }

                if (matches.Count > 0)
                {
                    mainPrefix = MatchesFound(callStructure, saveHit, matches);
                    return mainPrefix;
                }
            }

           // Console.WriteLine(callStructure.FullCall);

            mainPrefix = "";
            return mainPrefix;
        }

        /// <summary>
        /// Get the first and second letter.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="nextLetter"></param>
        /// <returns></returns>
        private (string, string) DetermineMaskComponents(string prefix)
        {
            var firstAndSecond = (firstLetter: "", nextLetter: "");

            firstAndSecond.firstLetter = prefix.Substring(0, 1);

            if (prefix.Length > 1)
            {
                firstAndSecond.nextLetter = prefix.Substring(1, 1);
            }

            return firstAndSecond;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseCall"></param>
        /// <param name="foundItems"></param>
        /// <param name="prefixData"></param>
        /// <param name="primaryMaskList"></param>
        private static void RefineList(string baseCall, HashSet<PrefixData> matches, PrefixData prefixData, List<List<string[]>> primaryMaskList)
        {
            var rank = 0;

            foreach (List<string[]> maskList in primaryMaskList)
            {
                var position = 2;
                var isPrevious = true;

                // get smaller length
                var smaller = baseCall.Length < maskList.Count ? baseCall.Length : maskList.Count;

                for (var i = position; i < smaller; i++)
                {
                    //var nextLetter = baseCall.Substring(i, 1);

                    if (maskList[position].Contains(baseCall.Substring(i, 1)) && isPrevious)
                    {
                        rank = position + 1;
                    }
                    else
                    {
                        isPrevious = false;
                        break;
                    }
                    position += 1;
                }

                // if found with 2 chars
                if (rank.Equals(smaller) || maskList.Count == 2)
                {
                    prefixData.Rank = rank;
                    matches.Add(prefixData);
                }
            }
        }

        /// <summary>
        /// Found some matches so build the hits.
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        /// <param name="saveHit"></param>
        /// <param name="mainPrefix"></param>
        /// <param name="matches"></param>
        /// <returns></returns>
        private string MatchesFound(CallStructure callStructure, bool saveHit, HashSet<PrefixData> matches)
        {
            string mainPrefix;

            if (!saveHit)
            {
                mainPrefix = matches.First().MainPrefix;
                return mainPrefix;
            }
            else
            {
                if (!MergeHits || matches.Count == 1)
                {
                    BuildHit(matches, callStructure);
                    mainPrefix = "";
                }
                else
                {
                    MergeMultipleHits(matches, callStructure);
                    mainPrefix = "";
                }

                return mainPrefix;
            }
        }

        private HashSet<PrefixData> MatchPattern(StringBuilder patternBuilder, string firstLetter, string prefix, out bool stopCharacterFound)
        {
            var prefixDataList = new HashSet<PrefixData>();
            //var tempStorage = new HashSet<PrefixData>();
            patternBuilder.Append(".");
            //bool hasDot;
            stopCharacterFound = false;
                 
            while (patternBuilder.Length > 1)
            {
                if (CallSignPatterns.TryGetValue(patternBuilder.ToString(), out var query))
                {
                    //prefixDataList.Clear();
                    foreach (var prefixData in query)
                    {
                        if (prefixData.IndexKey.ContainsKey(firstLetter))
                        {
                            int searchRank;
                            switch (patternBuilder[patternBuilder.Length - 1]) // last character
                            {
                                case '.': // if we have a '.' we do not want to continue
                                    prefix = prefix.Substring(0, patternBuilder.Length - 1);
                                    if (prefixData.MatchMask(prefix, true, out searchRank) == true)
                                    {
                                        prefixDataList.Add(prefixData);
                                        stopCharacterFound = true;
                                        // exit early
                                        goto ExitEarly;
                                    }
                                    break;
                                default:
                                    prefix = prefix.Substring(0, patternBuilder.Length);
                                    if (prefixData.MatchMask(prefix, true, out searchRank))
                                    {
                                        prefixDataList.Add(prefixData);
                                    }
                                    break;
                            }
                        }
                    }

                ExitEarly:
                    if (prefixDataList.Count != 0)
                    {
                        //prefixDataList.UnionWith(tempStorage);
                        break;
                    }
                }
                // remove last character
                //pattern = pattern.Remove(pattern.Length - 1);
                // very slightly faster as .Remove is a wrapper around Substring()
                // pattern = pattern.Substring(0, pattern.Length - 1);
                patternBuilder.Remove(patternBuilder.Length - 1, 1);
            }

            return prefixDataList;
        }

        /// <summary>
        /// Get a list of all the PrefixData that match a pattern.
        /// </summary>
        /// <param name="prefixDataList"></param>
        /// <param name="pattern"></param>
        /// <param name="firstLetter"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private bool MatchPattern(HashSet<PrefixData> prefixDataList, StringBuilder patternBuilder, string firstLetter, string prefix)
        {
            var tempStorage = new HashSet<PrefixData>();
            patternBuilder.Append(".");
            bool hasDot = false;

            while (patternBuilder.Length > 1)
            {
                if (CallSignPatterns.TryGetValue(patternBuilder.ToString(), out var query))
                {
                    tempStorage.Clear();
                    foreach (var prefixData in query)
                    {
                        if (prefixData.IndexKey.ContainsKey(firstLetter))
                        {
                            int searchRank;
                            switch (patternBuilder[patternBuilder.Length - 1]) // last character
                            {
                                case '.': // if we have a '.' we do not want to continue
                                    prefix = prefix.Substring(0, patternBuilder.Length - 1);
                                    if (prefixData.MatchMask(prefix, true, out searchRank) == true)
                                    {
                                        tempStorage.Add(prefixData);
                                        hasDot = true;
                                        // exit early
                                        goto ExitEarly;
                                    }
                                    break;
                                default:
                                    prefix = prefix.Substring(0, patternBuilder.Length);
                                    if (prefixData.MatchMask(prefix, true, out searchRank))
                                    {
                                        tempStorage.Add(prefixData);
                                    }
                                    break;
                            }
                        }
                    }

                    ExitEarly:
                    if (tempStorage.Count != 0)
                    {
                        prefixDataList.UnionWith(tempStorage);
                        break;
                    }
                }
                // remove last character
                //pattern = pattern.Remove(pattern.Length - 1);
                // very slightly faster as .Remove is a wrapper around Substring()
               // pattern = pattern.Substring(0, pattern.Length - 1);
                patternBuilder.Remove(patternBuilder.Length -1, 1);
            }

            return hasDot;
        }

        /// <summary>
        /// Portable prefixes are prefixes that end with "/"
        /// This looks almost identical to MatchPattern() but creates the Hit
        /// here. When it returns true it inhibits a full dictionary search.
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        /// <returns></returns>
        private bool CheckForPortablePrefix(CallStructure callStructure)
        {
            string prefix = callStructure.Prefix + "/";
            var prefixDataList = new HashSet<PrefixData>();
            var tempStorage = new HashSet<PrefixData>();
            //var firstLetter = prefix.Substring(0, 1);
            StringBuilder patternBuilder = callStructure.BuildPattern(prefix);
            int searchRank = 0;

            // check cache first
            if (PortablePrefixes.TryGetValue(patternBuilder.ToString(), out var query))
            {
                foreach (var prefixData in query)
                {
                    tempStorage.Clear();
                    
                    if (prefixData.IndexKey.ContainsKey(prefix.Substring(0, 1)))
                    {
                        if (prefixData.MatchMask(prefix, false, out searchRank))
                        {
                            prefixData.SearchRank = searchRank;
                            tempStorage.Add(prefixData);
                        }
                    }

                    if (tempStorage.Count != 0)
                    {
                        prefixDataList.UnionWith(tempStorage);
                        // do not exit early here
                        // VK0M/MB5KET hits Heard first and then Macquarie - VP2V/, VP2M/ also
                        //break;
                    }
                }
            }

            if (prefixDataList.Count > 0)
            {
                if (prefixDataList.Count > 1)
                {
                    // only keep the highest ranked prefixData for portable prefixes
                    // separates VK0M from VK0H and VP2V and VP2M
                    var highestRanked = prefixDataList.OrderByDescending(x => x.SearchRank).ToList();
                    int ranked = highestRanked[0].SearchRank;
                    prefixDataList.Clear();

                    foreach (PrefixData prefixData in highestRanked)
                    {
                        if (prefixData.SearchRank == ranked) {
                            prefixDataList.Add(prefixData);
                        }
                    }
                }

                BuildHit(prefixDataList, callStructure);
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
        private void BuildHit(HashSet<PrefixData> foundItems, CallStructure callStructure)
        {
            List<PrefixData> listByRank = foundItems.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Kind).ToList();

            foreach (PrefixData prefixData in listByRank)
            {
                Hit hit = new Hit(prefixData)
                {
                    CallSign = callStructure.FullCall
                };

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
        /// <param name="fullCall"></param>
        private void MergeMultipleHits(HashSet<PrefixData> foundItems, CallStructure callStructure)
        {
            List<PrefixData> HighestRankList = new List<PrefixData>();
            HighestRankList = foundItems.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Kind).ToList();
            var highestRanked = HighestRankList[0];

            Hit hit = new Hit(highestRanked)
            {
                CallSign = callStructure.FullCall,
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
        private bool CheckReplaceCallArea(CallStructure callStructure)
        {
            // UY0KM/0
            // see if the prefix == the first digit in the BaseCall
            if (callStructure.Prefix != callStructure.BaseCall.FirstOrDefault(c => char.IsDigit(c)).ToString())
            {
                try
                {
                    string mainPrefix = SearchMainDictionary(callStructure, false);
                    if (mainPrefix != "")
                    {
                        callStructure.Prefix = ReplaceCallArea(mainPrefix, callArea: callStructure.Prefix);
                        callStructure.CallStructureType = callStructure.Prefix switch
                        {
                            "" => CallStructureType.Call,// M0CCA/6 - main prefix is "G", F8ATS/9 mainPrefix = "F"
                            _ => CallStructureType.PrefixCall,
                        };

                        CollectMatches(callStructure);
                        return true;
                    }
                }
                catch (Exception)
                {

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
        private string ReplaceCallArea(string mainPrefix, string callArea)
        {
            int prefixLength = 0;

            switch (mainPrefix.Length)
            {
                case 1:
                    if (OneCharPrefs.Contains(mainPrefix.First()))
                    {
                        // I9MRY/1 - mainPrefix = I --> I1
                        prefixLength = 2;
                    }
                    else if (mainPrefix.All(char.IsLetter))
                    {
                        // FA3L/6 - mainPrefix is F - shouldn't we make it F6?
                        // F1FZH/8 
                        // MA2US/1
                        //position = 99;
                        return "";
                    }
                    break;
                case 2:
                    if (OneCharPrefs.Contains(mainPrefix.First()) && XNUM_SET.Contains(mainPrefix.Skip(1).First()))
                    {
                        // W6OP/4 - main prefix = W6 --> W4
                        prefixLength = 2;
                    }
                    else
                    {
                        // AL7NS/4 - main prefix = KL --> KL4
                        prefixLength = 3;
                    }

                    break;
                default:
                    // this means the same as the case 2 statement and is never hit
                    if (OneCharPrefs.Contains(mainPrefix.First()) && XNUM_SET.Contains(mainPrefix.Skip(1).First()))
                    {
                        prefixLength = 2;
                    }
                    else
                    {
                        if (XNUM_SET.Contains(mainPrefix.Skip(2).Take(1).First()))
                        {
                            // JI3DT/6 - mainPrefix = JA3 --> JA6
                            prefixLength = 3;
                        }
                        else
                        {
                            // 3DLE/1 - mainprefix = 3DA --> 3DA1 
                            prefixLength = 4;
                        }
                    }
                    break;
            }

            //position = prefixLength;

            // append call area to mainPrefix
            return mainPrefix.Substring(0, prefixLength - 1) + callArea + "/";
            // string format is slightly slower but barely enough to tell
            //return $"{mainPrefix.Substring(0, prefixLength - 1)}{callArea}{"/"}";
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

    } // end class
}
