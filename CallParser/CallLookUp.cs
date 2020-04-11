
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    public class CallLookUp
    {
        // writing to List<T> are faster than writing to Hashset<T>
        private ConcurrentBag<CallSignInfo> HitList;
        private readonly SortedDictionary<string, List<CallSignInfo>> CallSignDictionary;
        public SortedDictionary<string, List<CallSignInfo>> DXCCOnlyCallSignDictionary;
        private SortedDictionary<int, CallSignInfo> Adifs { get; set; }
        private readonly SortedDictionary<string, List<CallSignInfo>> PortablePrefixes;
        //private readonly Dictionary<string, List<int>> PortablePrefixes;
        //private readonly string[] _OneLetterSeries = { "B", "F", "G", "I", "K", "M", "N", "R", "W", "2" };
        private readonly string[] SingleCharPrefixes = { "F", "G", "M", "I", "R", "W" };
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
            this.DXCCOnlyCallSignDictionary = prefixFileParser.DXCCOnlyCallSignDictionary;
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
           //);

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

            if (callStructure.CallStructureType != CallStructureType.Invalid)
            {
                CollectMatchesEx(callStructure, callSign);
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
            //string prefix = callStructure.Prefix;
            string baseCall = callStructure.BaseCall;
            CallStructureType callStructureType = callStructure.CallStructureType;

            string searchTerm = baseCall;

            // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';

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
                    searchTerm = baseCall;
                    break;
            }

            string persistSearchTerm = searchTerm;

            // check the DXCC list first with the full search term or we will get incorrect results
            //if (CheckDXCCList(searchTerm, baseCall, fullCall))
            //{
            //    return;
            //}

            // masks are from 2 - 8 characters in the CallSignDictionary
            // remove characters from end of call until hit
            if (SearchMainDictionaryEx(searchTerm, baseCall, fullCall, true, out _))
            {
                return;
            }


            // some calls like DL6DH can't be found until you search for DL
            //searchTerm = persistSearchTerm;
            //CheckAdditionalDXCCEntities(callStructure, fullCall, searchTerm);

            return;
        }

        private void CollectMatchesEx(CallStructure callStructure, string fullCall)
        {
            //string prefix = callStructure.Prefix;
            string baseCall = callStructure.BaseCall;
            CallStructureType callStructureType = callStructure.CallStructureType;

            string searchTerm = baseCall;

            // ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';

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
                    searchTerm = baseCall;
                    break;
            }

            string persistSearchTerm = searchTerm;

            // check the DXCC list first with the full search term or we will get incorrect results
            //if (CheckDXCCList(searchTerm, baseCall, fullCall))
            //{
            //    return;
            //}

            // masks are from 2 - 8 characters in the CallSignDictionary
            // remove characters from end of call until hit
            if (SearchMainDictionaryEx(searchTerm, baseCall, fullCall, true, out _))
            {
                return;
            }


            // some calls like DL6DH can't be found until you search for DL
            searchTerm = persistSearchTerm;
            CheckAdditionalDXCCEntities(callStructure, fullCall, searchTerm);

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

            return pattern;
        }


        //  XIIZB56/6YH
        //int TotalCount = 0;
        private bool SearchMainDictionaryEx(string searchTerm, string baseCall, string fullCall, bool saveHit, out string mainPrefix)
        {
            var firstLetter = baseCall.First().ToString();
            var list = new List<CallSignInfo>();
            var rank = new CallSignInfo();
            var foundItems = new HashSet<CallSignInfo>();
            var pattern = BuildPattern(searchTerm);

            var persist = searchTerm;

           // Console.WriteLine("Total: " + TotalCount.ToString() + " --- " + HitList.Count);
            //TotalCount += 1;
            while (searchTerm != "")
            {
                if (CallSignDictionary.TryGetValue(pattern, out var query))
                {
                    // get a list of callSignInfo where the first letter in the primarymasklist == the first letter in the call
                    var temp = query.Where(x => x.IndexKeys.ContainsKey(firstLetter)).ToList();
                   
                    

                    if (temp.Count != 0)
                    {
                        list.AddRange(temp);
                        break;
                    }
                }
                pattern = pattern.Remove(pattern.Length - 1);
                searchTerm = searchTerm.Remove(searchTerm.Length - 1);
            }

            if (searchTerm == "")
            {
                searchTerm = persist;
            }

            // now we have a list of posibilities // HG5ACZ/P 
            if (list.Count > 0)
            {
                foreach (CallSignInfo info in list)
                {
                    var nextLetter = searchTerm.Skip(1).First().ToString();
                    var primaryMaskList = info.GetPrimaryMaskList(searchTerm.Length).Where(x => x.First().Contains(firstLetter) && x.Skip(1).First().Contains(nextLetter));
                   // var primaryMaskList = info.GetPrimaryMaskList(searchTerm.Length, firstLetter);

                    foreach (List<string[]> maskList in primaryMaskList.ToList()) // ToList uneccessary here
                    {
                        var position = 1;
                        var previous = true;

                        //Console.WriteLine(info.Country);
                        // get smallest
                        //var length = baseCall.Length < maskList.Count ? baseCall.Length : maskList.Count;
                        if (maskList[0].Contains(firstLetter))
                        {
                            for (var i = 1; i < searchTerm.Length; i++)
                            {
                                //var 
                                nextLetter = searchTerm.Skip(i).First().ToString();

                                if (maskList[position].Contains(nextLetter) && previous)
                                {
                                    info.Rank = position + 1;
                                }
                                else
                                {
                                    previous = false;
                                    break;
                                }
                                position += 1;
                            }

                            if (info.Rank == searchTerm.Length)
                            {
                                foundItems.Add(info);
                            }
                        }
                    }
                }

                if (foundItems.Count > 0)
                {
                    //Console.WriteLine("Found Main:" + foundItems.Count.ToString());
                    //int highestRank = foundItems.OrderByDescending(item => item.Rank).First().Rank;
                    //var HighestRankList = foundItems.Where(item => item.Rank == highestRank).ToList();

                    //foreach (CallSignInfo info2 in foundItems)
                    //{
                        if (saveHit)
                        {
                            //if (info2.Kind != PrefixKind.InvalidPrefix)
                            //{
                                BuildHit(foundItems, baseCall, searchTerm, fullCall);
                            //}
                        }
                        else
                        {
                            mainPrefix = foundItems.First().MainPrefix; // NEED TO ACOUNT FOR MORE THAN ONE
                            return true;
                        }
                    //}
                }
            }

            mainPrefix = "";

            return false;
        }

        private bool CheckForPortablePrefix(CallStructure callStructure, string fullCall)
        {
            string prefix = callStructure.Prefix + "/";
            string baseCall = callStructure.BaseCall;
            var list = new List<CallSignInfo>();
            var foundItems = new HashSet<CallSignInfo>();
            var firstLetter = prefix.First().ToString();
            var pattern = BuildPattern(prefix);

            if (PortablePrefixes.TryGetValue(pattern, out var query))
            {
                // get a list of callSignInfo where the first letter in the primarymasklist == the first letter in the call
                var temp = query.Where(x => x.IndexKeys.ContainsKey(firstLetter)).ToList();

               
                if (temp.Count != 0)
                {
                    list.AddRange(temp);
                }
            }

            if (list.Count > 0)
            {
                foreach (CallSignInfo info in list)
                {
                    var primaryMaskList = info.GetPrimaryMaskList(prefix.Length).Where(x => x.Last().Contains("/") && x.First().Contains(firstLetter));
                    // .Where(x => x.First().Contains(firstLetter) && x.Skip(1).First().Contains(nextLetter));

                    foreach (List<string[]> maskList in primaryMaskList)
                    {
                        var position = 1;
                        var previous = true;

                        if (maskList[0].Contains(firstLetter))
                        {
                            for (var i = 1; i < prefix.Length; i++)
                            {
                                var nextLetter = prefix.Skip(i).First().ToString();

                                if (maskList[position].Contains(nextLetter) && previous)
                                {
                                    info.Rank = position + 1;
                                    //foundItems.Add(info);
                                }
                                else
                                {
                                    previous = false;
                                    break;
                                }
                                position += 1;
                            }

                            if (info.Rank == prefix.Length)
                            {
                                foundItems.Add(info);
                            }
                        }
                    }
                }
                if (foundItems.Count > 0)
                {
                    //Console.WriteLine("Found Portable:" + foundItems.Count.ToString());
                    BuildHit(foundItems, baseCall, prefix, fullCall);
                    return true;
                }

            }
            return false;
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
            var firstLetter = callStructure.BaseCall.First().ToString();
            var list = new List<CallSignInfo>();
            var rank = new CallSignInfo();

            searchTerm = searchTerm.Substring(0, 2);
            var pattern = BuildPattern(searchTerm);
            var persist = searchTerm;

            if (DXCCOnlyCallSignDictionary.TryGetValue(pattern, out var query))
            {
                // get a list of callSignInfo where the first letter in the primarymasklist == the first letter in the call
                var temp = query.Where(x => x.IndexKeys.ContainsKey(firstLetter)).ToList();

                if (query.Count != 0)
                {
                    list.AddRange(temp);
                }
            }

            // now we have a list of posibilities
            if (list.Count > 0)
            {
                var foundItems = new HashSet<CallSignInfo>();

                foreach (CallSignInfo info in list)
                {
                    //var primaryMaskList = info.GetPrimaryMaskList(searchTerm.Length);
                    var nextLetter = searchTerm.Skip(1).First().ToString();
                    var primaryMaskList = info.GetPrimaryMaskList(searchTerm.Length).Where(x => x.First().Contains(firstLetter) && x.Skip(1).First().Contains(nextLetter));

                    foreach (List<string[]> maskList in primaryMaskList)
                    {
                        var position = 1;
                        var previous = true;

                        if (maskList[0].Contains(firstLetter))
                        {
                            for (var i = 1; i < searchTerm.Length; i++)
                            {
                                //var
                                nextLetter = searchTerm.Skip(i).First().ToString();

                                if (maskList[position].Contains(nextLetter) && previous)
                                {
                                    info.Rank = position + 1;
                                }
                                else
                                {
                                    previous = false;
                                    break;
                                }
                                position += 1;
                            }

                            if (info.Rank == searchTerm.Length)
                            {
                                foundItems.Add(info);
                            }
                        }
                    }
                }

                if (foundItems.Count > 0)
                {
                    int highestRank = foundItems.OrderByDescending(item => item.Rank).First().Rank;
                    var HighestRankList = foundItems.Where(item => item.Rank == highestRank).ToList();

                    //foreach (CallSignInfo info2 in HighestRankList)
                    //{
                    //    if (info2.Kind != PrefixKind.InvalidPrefix)
                    //    {
                            BuildHit(foundItems, callStructure.BaseCall, searchTerm, fullCall);
                    //    }
                    //}
                }
            }
        }

        /// <summary>
        /// Build the hit and add it to the hitlist.
        /// </summary>
        /// <param name="foundItems"></param>
        /// <param name="baseCall"></param>
        /// <param name="prefix"></param>
        /// <param name="fullCall"></param>
        private void BuildHit(HashSet<CallSignInfo> foundItems, string baseCall, string prefix, string fullCall)
        {
            int highestRank = foundItems.OrderByDescending(item => item.Rank).First().Rank;
            var HighestRankList = foundItems.Where(item => item.Rank == highestRank).ToList();

            foreach (CallSignInfo callSignInfo in HighestRankList)
            {
                var callSignInfoCopy = callSignInfo.ShallowCopy();
                callSignInfoCopy.CallSign = fullCall;
                callSignInfoCopy.BaseCall = baseCall;
                callSignInfoCopy.HitPrefix = prefix;
                HitList.Add(callSignInfoCopy);
                if (callSignInfo.Kind != PrefixKind.DXCC)
                {
                    if (Adifs[Convert.ToInt32(callSignInfo.DXCC)].Kind != PrefixKind.InvalidPrefix)
                    {
                        var callSignInfoCopyDxcc = Adifs[Convert.ToInt32(callSignInfo.DXCC)].ShallowCopy();
                        callSignInfoCopyDxcc.CallSign = fullCall;
                        callSignInfoCopyDxcc.BaseCall = baseCall;
                        callSignInfoCopy.HitPrefix = prefix;
                        HitList.Add(callSignInfoCopyDxcc);
                    }
                }
            }
        }

        /// <summary>
        /// Search the CallSignDictionary for a hit with the full call. If it doesn't 
        /// hit remove characters from the end until hit or there are no letters fleft. 
        /// Return the CallSignInfo as an out parameter for the ReplaceCallArea() function.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="baseCall"></param>
        /// <param name="fullCall"></param>
        /// <param name="saveHit"></param>
        /// <param name="callSignInfo"></param>
        /// <returns></returns>
        private bool SearchMainDictionary(string searchTerm, string baseCall, string fullCall, bool saveHit, out CallSignInfo callSignInfo)
        {
            while (searchTerm != string.Empty)
            {
                if (CallSignDictionary.TryGetValue(searchTerm, out var query))
                {
                    callSignInfo = query.First();

                    if (callSignInfo.Kind != PrefixKind.InvalidPrefix)
                    {
                        var callSignInfoCopy = callSignInfo.ShallowCopy();
                        callSignInfoCopy.CallSign = fullCall;
                        callSignInfoCopy.BaseCall = baseCall;
                        callSignInfoCopy.HitPrefix = searchTerm;
                        if (saveHit)
                        {
                            HitList.Add(callSignInfoCopy);

                            if (callSignInfo.Kind != PrefixKind.DXCC)
                            {
                                if (Adifs[Convert.ToInt32(callSignInfo.DXCC)].Kind != PrefixKind.InvalidPrefix)
                                {
                                    var callSignInfoCopyDxcc = Adifs[Convert.ToInt32(callSignInfo.DXCC)].ShallowCopy();
                                    callSignInfoCopyDxcc.CallSign = fullCall;
                                    callSignInfoCopyDxcc.BaseCall = baseCall;
                                    callSignInfoCopy.HitPrefix = searchTerm;
                                    HitList.Add(callSignInfoCopyDxcc);
                                }
                            }
                        }
                    }

                    return true;
                }

                searchTerm = searchTerm.Remove(searchTerm.Length - 1);
            }

            callSignInfo = new CallSignInfo();
            return false;
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
                if (SearchMainDictionaryEx(searchTerm: callStructure.BaseCall, baseCall: callStructure.BaseCall, fullCall, false, out string mainPrefix))
                {
                    callStructure.Prefix = ReplaceCallArea(mainPrefix, callStructure.Prefix);
                    callStructure.CallStructureType = CallStructureType.PrefixCall;
                    CollectMatchesEx(callStructure, fullCall);
                    return true;
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

            return $"{mainPrefix.Substring(0, p - 1)}{callArea}/";
        }

        /// <summary>
        /// Look through the list of valid prefixes for each DXCC entry
        /// until a match is found - or not.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="baseCall"></param>
        /// <param name="fullCall"></param>
        /// <returns></returns>
        private bool CheckDXCCList(string searchTerm, string baseCall, string fullCall)
        {
            //if (DXCCOnlyCallSignDictionary.TryGetValue(searchTerm, out var query))
            //{
            //    var dxccList = query.ToList();

            //    foreach (int dxcc in dxccList)
            //    {
            //        if (Adifs[dxcc].Kind != PrefixKind.InvalidPrefix)
            //        {
            //            var callSignInfoCopyDxcc = Adifs[dxcc].ShallowCopy();
            //            callSignInfoCopyDxcc.CallSign = fullCall;
            //            callSignInfoCopyDxcc.BaseCall = baseCall;
            //            callSignInfoCopyDxcc.HitPrefix = searchTerm;
            //            HitList.Add(callSignInfoCopyDxcc);
            //        }
            //    }
            //    return true;
            //}

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callStructure"></param>
        /// <param name="fullCall"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        private bool CheckForPortablePrefix_Old(CallStructure callStructure, string fullCall)
        {
            string prefix = callStructure.Prefix;
            string baseCall = callStructure.BaseCall;


            prefix = prefix + "/";

            // check for portable prefixes
            // this will catch G/, W/, W4/, VU@@/ VU4@@/ VK9/
            if (PortablePrefixes.TryGetValue(prefix, out var entities))
            {
                foreach (var callSignInfoCopy in from int entity in entities
                                                 let callSignInfo = Adifs[entity]
                                                 let callSignInfoCopy = callSignInfo.ShallowCopy()
                                                 select callSignInfoCopy)
                {
                    callSignInfoCopy.CallSign = fullCall;
                    callSignInfoCopy.BaseCall = baseCall;
                    callSignInfoCopy.HitPrefix = prefix;
                    //if (callStructure.CallStuctureType.ToString().Contains("Portable"))
                    //{
                    //    callSignInfoCopy.CallSignFlags.Add(CallSignFlags.Portable);
                    //}
                    HitList.Add(callSignInfoCopy);
                }
                return true;
            }
            return false;
        }

        private void CheckAdditionalDXCCEntities_Old(CallStructure callStructure, string fullCall, string searchTerm)
        {
            while (searchTerm != string.Empty)
            {
                //if (DXCCOnlyCallSignDictionary.TryGetValue(searchTerm, out var query))
                //{
                //    var dxccList = query.ToList();

                //    foreach (int dxcc in dxccList)
                //    {
                //        if (Adifs[dxcc].Kind != PrefixKind.InvalidPrefix)
                //        {
                //            var callSignInfoCopyDxcc = Adifs[dxcc].ShallowCopy();
                //            callSignInfoCopyDxcc.CallSign = fullCall;
                //            callSignInfoCopyDxcc.BaseCall = callStructure.BaseCall;
                //            callSignInfoCopyDxcc.HitPrefix = searchTerm;
                //            HitList.Add(callSignInfoCopyDxcc);
                //        }
                //    }
                //    return;
                //}

                searchTerm = searchTerm.Remove(searchTerm.Length - 1);
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
