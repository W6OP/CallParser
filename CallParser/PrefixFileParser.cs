
/*
 PrefixFileParser.cs
 CallParser
 
 Created by Peter Bourget on 3/11/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Parse a prefix xml file and prefix
 combinations.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    /// <summary>
    /// Load and parse the prefix file to create prefix
    /// patterns. 
    /// </summary>
    public class PrefixFileParser
    {
        /// <summary>
        /// Public fields.
        /// </summary>
        public Dictionary<string, HashSet<CallSignInfo>> CallSignDictionary;
        public Dictionary<string, List<int>> DXCCOnlyCallSignDictionary;
        public SortedDictionary<int, CallSignInfo> Adifs { get; set; }
        public SortedDictionary<string, List<CallSignInfo>> Admins;
        public Dictionary<string, List<int>> PortablePrefixes;

        /// <summary>
        /// Private fields.
        /// </summary>
        private readonly string[] Alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] Numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private readonly string[] AlphaNumerics = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PrefixFileParser()
        {
            // preallocate space
            //CallSignDictionary = new Dictionary<string, HashSet<CallSignInfo>>(1500000);
            //Adifs = new SortedDictionary<int, CallSignInfo>();
            //Admins = new SortedDictionary<string, List<CallSignInfo>>();
            //PortablePrefixes = new Dictionary<string, List<int>>(200000);
        }

        /// <summary>
        /// Read the prefix fle. If a file path is passed in then read that file.
        /// Otherwise use the embedded resource file for the prefix list. I use an
        /// XMLReader so the file is not actually loaded into memory, only the 
        /// elements I am currently working with.
        /// </summary>
        /// <param name="prefixFilePath"></param>
        public void ParsePrefixFile(string prefixFilePath)
        {
            // cleanup if running more than once
            CallSignDictionary = new Dictionary<string, HashSet<CallSignInfo>>(1000000);
            DXCCOnlyCallSignDictionary = new Dictionary<string, List<int>>(10000000);
            Adifs = new SortedDictionary<int, CallSignInfo>();
            Admins = new SortedDictionary<string, List<CallSignInfo>>();
            PortablePrefixes = new Dictionary<string, List<int>>(200000);

            Assembly assembly = Assembly.GetExecutingAssembly();

            if (File.Exists(prefixFilePath))
            {
                try
                {
                    using (XmlReader reader = XmlReader.Create(prefixFilePath))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                if (reader.Name == "prefix")
                                {
                                    XElement prefix = XElement.ReadFrom(reader) as XElement;
                                    CallSignInfo callSignInfo = new CallSignInfo(prefix);
                                    BuildCallSignInfo(prefix, callSignInfo);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to parse the prefix file: " + new { prefixFilePath }, ex);
                }
            }
            else
            {
                using (StreamReader stream = new StreamReader(assembly.GetManifestResourceStream("W6OP.CallParser.PrefixList.xml")))
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                if (reader.Name == "prefix")
                                {
                                    XElement prefix = XElement.ReadFrom(reader) as XElement;
                                    CallSignInfo callSignInfo = new CallSignInfo(prefix);
                                    BuildCallSignInfo(prefix, callSignInfo);
                                }
                            }
                        }
                    }
                }
            }
            GC.Collect();
        }

        /// <summary>
        /// Loop through all of the prefix nodes and expand the masks for each prefix.
        /// </summary>
        /// <param name="prefix"></param>
        private void BuildCallSignInfo(XElement prefix, CallSignInfo callSignInfo)
        {
            //HashSet<CallSignInfo> callSignInfoSet = new HashSet<CallSignInfo>();
            HashSet<string> primaryMaskList = new HashSet<string>();
            IEnumerable<XElement> masks = prefix.Elements().Where(x => x.Name == "masks");

            if (callSignInfo.Kind == PrefixKind.DXCC)
            {
                Adifs.Add(Convert.ToInt32(callSignInfo.DXCC), callSignInfo);
            }

            if (callSignInfo.Kind == PrefixKind.InvalidPrefix)
            {
                Adifs.Add(0, callSignInfo);
            }

            if (callSignInfo.WAE != 0)
            {
                Adifs.Add(callSignInfo.WAE, callSignInfo);
            }

            if (callSignInfo.Kind == PrefixKind.Province && !string.IsNullOrEmpty(callSignInfo.Admin1))
            {
                if (Admins.TryGetValue(callSignInfo.Admin1, out var list))
                {
                    list.Add(callSignInfo);
                }
                else
                {
                    Admins.Add(callSignInfo.Admin1, new List<CallSignInfo> { callSignInfo });
                }
            }

            foreach (XElement element in masks.Descendants())
            {
                if (element.Value != "") // empty is usually a DXCC node
                {
                    // expand the mask if it exists
                    primaryMaskList = ExpandMask(element.Value);
                   
                    foreach (string mask in primaryMaskList)
                    {
                        // need to find the DXCC for this and add 
                        //if (callSignInfo.Kind == PrefixKind.DXCC)
                        //{
                        //    callSignInfo.PrefixKey.Add(mask, new byte());
                        //}
                        //else
                        //{
                        //    if (!Adifs[callSignInfo.DXCC].PrefixKey.ContainsKey(mask))
                        //    {
                        //        Adifs[callSignInfo.DXCC].PrefixKey.Add(mask, new byte());
                        //    }
                        //}

                        // this is a portable prefix
                        if (mask.EndsWith("/"))
                        {
                            // used in CallLookUp to identify portable prefixes
                            if (PortablePrefixes.TryGetValue(mask, out var list))
                            {
                                // VK9/ has multiple DXCC numbers - 35, 150...
                                list.Add(callSignInfo.DXCC);
                            }
                            else
                            {
                                PortablePrefixes.Add(mask, new List<int> { callSignInfo.DXCC });
                            }
                        }

                        //all DXCC kinds are in ADIFS
                        if (callSignInfo.Kind != PrefixKind.DXCC && !mask.EndsWith("/"))
                        {
                            if (CallSignDictionary.TryGetValue(mask, out var list))
                            {
                                // VK9/ has multiple DXCC numbers - 35, 150...
                                list.Add(callSignInfo);
                            }
                            else
                            {
                                CallSignDictionary.Add(mask, new HashSet<CallSignInfo> { callSignInfo });
                            }
                        }
                        else
                        {
                            if (DXCCOnlyCallSignDictionary.TryGetValue(mask, out var list))
                            {
                                // VK9/ has multiple DXCC numbers - 35, 150...
                                list.Add(callSignInfo.DXCC);
                            }
                            else
                            {
                              DXCCOnlyCallSignDictionary.Add(mask, new List<int> { callSignInfo.DXCC });  
                            }
                        }
                    }
                }
                primaryMaskList.Clear();
            }
        }

        /// <summary>
        /// Break up the mask into sections and expand all of the meta characters.
        /// </summary>
        /// <param name="mask"></param>
        internal HashSet<string> ExpandMask(string mask)
        {
            string maskPart;
            int counter = 0;
            int index;
            string expression = "";
            string item;
            // TEMPORARY: get rid of "." - need to work on this
            mask = mask.Replace(".", "");
            // sometimes "-" has spaces around it [1 - 8]
            mask = String.Concat(mask.Where(c => !Char.IsWhiteSpace(c)));
            int length = mask.Length;

            string[] stringArray = { "@", "#", "?", "-" };

            while (counter < length)
            {
                item = mask.Substring(0, 1);
                switch (item)
                {
                    case "[": // range
                        index = mask.IndexOf("]");
                        maskPart = mask.Substring(0, index + 1);
                        counter += maskPart.Length;
                        mask = mask.Substring(maskPart.Length);
                        // look for expando in the set
                        if (stringArray.Any(maskPart.Contains))
                        {
                            maskPart = string.Join("", GetMetaMaskSetEx(maskPart));
                        }
                        expression += maskPart;
                        break;
                    case "@": // alpha
                        // use constant for performance
                        expression += "[@]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "#": // numeric
                        expression += "[#]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "?": // alphanumeric
                        expression += "[?]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    default: // single character
                        expression += item.ToString();
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                }
            }

            return CombineComponents(expression);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private HashSet<string> CombineComponents(string expression)
        {
            HashSet<string> primaryMaskList = new HashSet<string>(5000);
            List<string[]> charsList = new List<string[]>(1000);
            StringBuilder builder;

            charsList = BuildCharArray(charsList, expression);

            // have list of arrays
            // for each caracter in the first list append each character of the second list
            if (charsList.Count == 1)
            {
                primaryMaskList.Add(charsList.First().ToString());
                return primaryMaskList;
            }

            string[] first = charsList.First();
            string[] next = charsList.Skip(1).First();

            // saves about 1/2 second overall
            _ = Parallel.ForEach(first, firstItem =>
            //foreach (string firstItem in first)
            {
                foreach (string nextItem in next)
                {
                    // slightly faster than concatenation
                    builder = new StringBuilder().Append(firstItem).Append(nextItem);
                    primaryMaskList.Add(builder.ToString());
                }
            }
            );

            // are there more?
            if (charsList.Count > 2)
            {
                charsList.Remove(first);
                charsList.Remove(next);
                primaryMaskList = CombineRemainder(charsList, primaryMaskList);
            }

            return primaryMaskList;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="charsList"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        private List<string[]> BuildCharArray(List<string[]> charsList, string expression)
        {
            string temp;

            // first get everything in blocks separated by []
            if (expression.IndexOf("[") != -1 && expression.IndexOf("[") == 0)
            {
                temp = expression.Substring(1, expression.IndexOf("]") - 1);
                switch (temp)
                {
                    case "@":
                        charsList.Add(Alphabet);
                        break;
                    case "#":
                        charsList.Add(Numbers);
                        break;
                    case "?":
                        charsList.Add(AlphaNumerics);
                        break;
                    default:
                        charsList.Add(temp.Select(c => c.ToString()).ToArray());
                        break;
                }
                expression = expression.Remove(0, expression.IndexOf("]") + 1);
            }
            else
            {
                if (expression.IndexOf("[") != -1)
                {
                    temp = expression.Substring(0, expression.IndexOf("["));
                    expression = expression.Remove(0, expression.IndexOf("["));
                    // Linq is faster
                    charsList.AddRange(temp.Select(x => new string[1] { x.ToString() }));
                    //foreach (char x in temp)
                    //{
                    //    charsList.Add(new string[1] { x.ToString() });
                    //}
                }
                else
                {
                    temp = expression;
                    expression = expression.Remove(0, temp.Length);
                    // Linq is faster
                    charsList.AddRange(temp.Select(x => new string[1] { x.ToString() }));
                    //foreach (char x in temp)
                    //{
                    //    charsList.Add(new string[1] { x.ToString() });
                    //}
                }
            }

            if (expression.Length > 0)
            {
                charsList = BuildCharArray(charsList, expression);
            }

            return charsList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="charsList"></param>
        /// <param name="expressionList"></param>
        private HashSet<string> CombineRemainder(List<string[]> charsList, HashSet<string> primaryMaskList)
        {
            HashSet<string> tempList = new HashSet<string>(1000);

            string[] first = charsList.First();

            if (primaryMaskList.Count == 0)
            {
                var a = 1;
            }

            try
            {
                // faster without Linq
                if (charsList.Count > 0)
                {
                    // _ = Parallel.ForEach(primaryMaskList, prefix =>
                    foreach (string prefix in primaryMaskList)
                    {
                        foreach (string nextItem in first)
                        {
                            tempList.Add(prefix + nextItem);
                        }
                    }
                    //);
                }
            }
            catch (Exception ex)
            {
                var b = ex.Message;
            }

            // this statement must be here before the stack is unwound
            primaryMaskList = tempList;

            if (charsList.Count > 1)
            {
                charsList.Remove(first);
                primaryMaskList = CombineRemainder(charsList, primaryMaskList);
            }

            return primaryMaskList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentCharacter"></param>
        /// <param name="nextCharacter"></param>
        /// <returns></returns>
        private string BuildRange(string currentCharacter, string nextCharacter)
        {
            string expandedMask = "";

            // both numeric
            if (IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                if (Convert.ToInt32(currentCharacter) < Convert.ToInt32(nextCharacter))
                {
                    int start = GetNumberIndex(currentCharacter);
                    int end = GetNumberIndex(nextCharacter);

                    for (int index = start; index <= end; index++)
                    {
                        expandedMask += Numbers[index];
                    }
                }
            }

            // both alpha
            if (!IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(currentCharacter);
                int end = GetCharacterIndex(nextCharacter);

                for (int index = start; index <= end; index++)
                {
                    expandedMask += Alphabet[index];
                }
            }

            // numeric --> alpha
            if (IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(nextCharacter);
                int end = GetCharacterIndex("Z");

                for (int index = start; index <= end; index++)
                {
                    expandedMask += Alphabet[index];
                }
            }

            if (!IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                Debug.Assert(!IsNumeric(currentCharacter) && IsNumeric(nextCharacter));
            }

            return expandedMask;
        }

        /// <summary>
        /// Tests if a string is numeric.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>bool</returns>
        private bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expando"></param>
        /// <returns></returns>
        private string GetMetaMaskSetEx(string expando = "")
        {
            if (expando.IndexOf("#") != -1)
            {
                expando = expando.Replace("#", "0123456789");
            }

            if (expando.IndexOf("@") != -1)
            {
                expando = expando.Replace("@", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            }

            if (expando.IndexOf("?") != -1)
            {
                expando = expando.Replace("?", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            }

            while (expando.IndexOf("-") != -1)
            {
                string expandedMask = expando.Substring(0, expando.IndexOf("-") - 1);
                string post = expando.Substring(expando.IndexOf("-") + 2);
                string currentCharacter = expando.Substring(expando.IndexOf("-") - 1, 1);
                string nextCharacter = expando.Substring(expando.IndexOf("-") + 1, 1);
                expandedMask += BuildRange(currentCharacter, nextCharacter);
                expandedMask += post;
                expando = expandedMask;
            }

            return expando;
        }

        /// <summary>
        /// Get the index in to the _Alphabet array.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private int GetCharacterIndex(string character)
        {
            return Array.IndexOf(Alphabet, character);
        }

        /// <summary>
        /// Get the index in the _Numbers array.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private int GetNumberIndex(string character)
        {
            return Array.IndexOf(Numbers, character);
        }

    } // end class
}
