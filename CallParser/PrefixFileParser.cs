
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    /// <summary>
    /// Load and parse the prefix file to create prefix
    /// patterns. 
    /// writing to List<T> are faster than writing to HashSet<T>
    /// </summary>
    public class PrefixFileParser
    {
        /// <summary>
        /// Public fields.
        /// </summary>
        /// 
        // the main dictionary of possible call signs built from the <mask> - excludes DXCC
        internal ConcurrentDictionary<string, List<PrefixData>> CallSignPatterns;
        // dxcc number with corresponding prefixData object.
        internal SortedDictionary<int, PrefixData> Adifs;
        // Admin list
        internal SortedDictionary<string, List<PrefixData>> Admins;
        // all the portable prefix entries (ends with "/") with dxcc number
        internal ConcurrentDictionary<string, List<PrefixData>> PortablePrefixes;

        /// <summary>
        /// Private fields.
        /// </summary>
        /// 
        // well known structures to be indexed into
        private readonly string[] Alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] Numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private readonly string[] AlphaNumerics = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PrefixFileParser()
        {
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
            CallSignPatterns = new ConcurrentDictionary<string, List<PrefixData>>(); //1000000
            Adifs = new SortedDictionary<int, PrefixData>();
            Admins = new SortedDictionary<string, List<PrefixData>>();
            PortablePrefixes = new ConcurrentDictionary<string, List<PrefixData>>(); //200000

            Assembly assembly = Assembly.GetExecutingAssembly();

            if (File.Exists(prefixFilePath))
            {
                try
                {
                    using XmlReader reader = XmlReader.Create(prefixFilePath);
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            if (reader.Name == "prefix")
                            {
                                XElement prefix = XElement.ReadFrom(reader) as XElement;
                                PrefixData prefixData = new PrefixData(prefix);
                                BuildPrefixData(prefix, prefixData);
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
                using StreamReader stream = new StreamReader(assembly.GetManifestResourceStream("W6OP.CallParser.PrefixList.xml"));
                using XmlReader reader = XmlReader.Create(stream);
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "prefix")
                        {
                            XElement prefix = XElement.ReadFrom(reader) as XElement;
                            PrefixData prefixData = new PrefixData(prefix);
                            BuildPrefixData(prefix, prefixData);
                        }
                    }
                }

            }

            // DEBUGGING CODE
            //foreach (KeyValuePair<string, List<prefixData>> kvp in PortablePrefixes)
            //{
            //    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value.Count);
            //}

            //Console.WriteLine("CallSignDictionary = {0},", CallSignDictionary.Count);
            //Console.WriteLine("PortablePrefixes = {0},", PortablePrefixes.Count);
            //var a = 1;
        }


        /// <summary>
        /// Loop through all of the prefix nodes and expand the masks for each prefix.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="prefixData"></param>
        private void BuildPrefixData(XElement prefix, PrefixData prefixData)
        {
            var primaryMaskList = new List<string[]>();
            IEnumerable<XElement> masks = prefix.Elements().Where(x => x.Name == "masks");

            switch (prefixData)
            {
                case PrefixData _ when prefixData.Kind == PrefixKind.DXCC:
                    Adifs.Add(Convert.ToInt32(prefixData.DXCC), prefixData);
                    break;
                case PrefixData _ when prefixData.Kind == PrefixKind.InvalidPrefix:
                    Adifs.Add(0, prefixData);
                    break;
                case PrefixData _ when prefixData.Kind == PrefixKind.Province && !string.IsNullOrEmpty(prefixData.Admin1):
                    if (Admins.TryGetValue(prefixData.Admin1, out var list))
                    {
                        list.Add(prefixData);
                    }
                    else
                    {
                        Admins.Add(prefixData.Admin1, new List<PrefixData> { prefixData });
                    }
                    break;
            }

            if (prefixData.WAE != 0)
            {
                Adifs.Add(prefixData.WAE, prefixData);
            }

            foreach (var element in masks.Descendants())
            {
                if (element.Value != "") // empty is usually a DXCC node
                {
                    // NEED TO TEST FOR DXCC 180 - Mt. Athos - has @@../@ pattern
                    // ALSO [xxx][.xxx]
                    // expand the mask if it exists
                    primaryMaskList = ExpandMask(element.Value);
                    // add for future lookups
                    prefixData.SetPrimaryMaskList(primaryMaskList);

                    // if pattern contains "?" then need two patterns
                    // one with # and one with @
                    var patternList = BuildPattern(primaryMaskList);

                    // AX9[ABD-KOPQS-VYZ][.ABD-KOPQS-VYZ]
                    foreach (var pattern in patternList)
                    {      
                        switch (pattern)
                        {
                            case string _ when pattern.Last().ToString().Contains("/"):
                                if (PortablePrefixes.TryGetValue(pattern, out var list))
                                {
                                    // add to an existing list
                                    // VK9/ has multiple DXCC numbers - 35, 150...
                                    list.Add(prefixData); //prefixData.DXCC
                                }
                                else
                                {
                                    PortablePrefixes.TryAdd(pattern, new List<PrefixData> { prefixData });
                                }
                                break;

                            case string _ when prefixData.Kind != PrefixKind.InvalidPrefix:
                                if (CallSignPatterns.TryGetValue(pattern, out var list2))
                                {
                                    // VK9/ has multiple DXCC numbers - 35, 150...
                                    list2.Add(prefixData);
                                }
                                else
                                {
                                    CallSignPatterns.TryAdd(pattern, new List<PrefixData> { prefixData });
                                }
                                break;
                            
                            default:
                                break;
                        }
                    }
                }
                primaryMaskList = new List<string[]>();
            }
        }

        /// <summary>
        /// Build the pattern from the mask
        /// KG4@@.
        /// [AKNW]H7K[./]
        /// AX9[ABD - KOPQS - VYZ][.ABD-KOPQS-VYZ] @@#@. and @@#@@. 
        /// </summary>
        /// <param name="primaryMaskList"></param>
        /// <returns></returns>
        private List<string> BuildPattern(List<string[]> primaryMaskList)
        {
            string pattern = "";
            string pattern2 = "";
            var patternList = new List<string>();

            foreach (var mask in primaryMaskList)
            {
                switch (mask)
                {
                    case string[] _ when mask.All(x => char.IsLetter(char.Parse(x))):
                        pattern += "@";
                        break;

                    case string[] _ when mask.All(x => char.IsDigit(char.Parse(x))):
                        pattern += "#";
                        break;

                    case string[] _ when mask.All(x => char.IsPunctuation(char.Parse(x))):
                        
                        switch (mask[0])
                        {
                            case "/":
                                pattern += "/";
                                if (mask.Length > 1)
                                {
                                    Console.WriteLine("Mask length = " + mask.Length);
                                }
                                break;
                            case ".":
                                pattern += ".";
                                if (mask.Length > 1)
                                {
                                    patternList.Add(pattern);
                                    patternList.Add(pattern.Replace(".", "@."));
                                    return patternList;
                                }
                                break;
                        }
                        break;

                        case string[] _ when mask.Any(x => char.IsPunctuation(char.Parse(x))):
                            if (mask[0] == ".")
                            {
                            pattern += ".";
                            if (mask.Length > 1)
                            {
                                pattern2 = pattern.Replace(".", "@.");
                                // patternList.append(pattern.replacingOccurrences(of: ".", with: "@."))
                            }
                        }
                        break;

                    default:
                        pattern += "?";
                        break;
                }
            }

            patternList = RefinePattern(pattern);

            // THIS IS A TEMP HACK TO FIX A PROBLEM _ NEED TO DO IT RIGHT LATER
            if (pattern2 != "")
            {
                patternList.Add(pattern2);
            }

            return patternList;
        }
        
        /// <summary>
        /// Look at each pattern, if one contains one or more "?" create new
        /// pattern using a "#" and a "@".
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private List<string> RefinePattern(string pattern)
        {
            var patternList = new List<string>();

            int count = pattern.Count(f => f == '?');
           
            switch (pattern)
            {
                case string _ when pattern.Contains("?"):
                    patternList.Add(pattern.Replace("?", "@"));
                    patternList.Add(pattern.Replace("?", "#"));
                    if (pattern.Count(f => f == '?') > 1) // currently only one "[0Q]?" which is invalid prefix
                    {
                        patternList.Add("@" + pattern.Last().ToString().Replace("?", "#"));
                        patternList.Add("#" + pattern.Last().ToString().Replace("?", "@"));
                    }
                    break;
           
                default:
                    patternList.Add(pattern);
                    break;
            }


            return patternList;
        }

        private List<string[]> ExpandMask(string mask)
        {
            string maskPart;
            int counter = 0;
            int index;
            string expandedMask = "";
            string maskCharacter;
            string[] metaCharacters = { "@", "#", "?", "-", "." };

            // sometimes "-" has spaces around it [1 - 8]
            mask = string.Concat(mask.Where(c => !char.IsWhiteSpace(c)));
            int length = mask.Length;

            while (counter < length)
            {
                maskCharacter = mask.Substring(0, 1);
                switch (maskCharacter)
                {
                    case "[": // range
                        index = mask.IndexOf("]");
                        maskPart = mask.Substring(0, index + 1);
                        counter += maskPart.Length;
                        mask = mask.Substring(maskPart.Length);
                        // look for expando in the set
                        if (metaCharacters.Any(maskPart.Contains))
                        {
                            maskPart = string.Join("", GetMetaMaskSet(maskPart));
                        }
                        expandedMask += maskPart;
                        break;
                    case "@": // alpha
                        // use constant for performance
                        expandedMask += "[@]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "#": // numeric
                        expandedMask += "[#]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "?": // alphanumeric
                        expandedMask += "[?]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;

                    case ".":
                        expandedMask += ".";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;

                    default: // single character
                        expandedMask += maskCharacter.ToString();
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                }
            }

            return CombineComponents(expandedMask);
        }

        /// <summary>
        /// Combine the components of the mask into a full call sign or prefix.
        /// </summary>
        /// <param name="expandedMask"></param>
        /// <returns></returns>
        private List<string[]> CombineComponents(string expandedMask)
        {
            var charsList = new List<string[]>();
            
            charsList = BuildCharArray(charsList, expandedMask);

            return charsList;
        }

       
        /// <summary>
        /// Build a list of arrays to be combined into all possible call sign possibilities.
        /// </summary>
        /// <param name="charsList"></param>
        /// <param name="expandedMask"></param>
        /// <returns></returns>
        private List<string[]> BuildCharArray(List<string[]> charsList, string expandedMask)
        {
            string maskBuffer;

            // first get everything in blocks separated by []
            if (expandedMask.IndexOf("[") != -1 && expandedMask.IndexOf("[") == 0)
            {
                maskBuffer = expandedMask.Substring(1, expandedMask.IndexOf("]") - 1);

                switch (maskBuffer)
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
                        charsList.Add(maskBuffer.Select(c => c.ToString()).ToArray());
                        break;
                }
                expandedMask = expandedMask.Remove(0, expandedMask.IndexOf("]") + 1);
            }
            else
            {
                if (expandedMask.IndexOf("[") != -1)
                {
                    maskBuffer = expandedMask.Substring(0, expandedMask.IndexOf("["));
                    expandedMask = expandedMask.Remove(0, expandedMask.IndexOf("["));
                    // Linq is faster in this instance
                    charsList.AddRange(maskBuffer.Select(x => new string[1] { x.ToString() }));
                }
                else
                {
                    maskBuffer = expandedMask;
                    expandedMask = expandedMask.Remove(0, maskBuffer.Length);
                    // Linq is faster in this instance
                    charsList.AddRange(maskBuffer.Select(x => new string[1] { x.ToString() }));
                }
            }

            if (expandedMask.Length > 0)
            {
                charsList = BuildCharArray(charsList, expandedMask);
            }

            return charsList;
        }

        /// <summary>
        /// Recursively build the lists of masks/call signs
        /// </summary>
        /// <param name="charsList"></param>
        /// <param name="expressionList"></param>
        private List<string> CombineRemainder(List<string[]> charsList, List<string> maskList)
        {
            var maskBuffer = new List<string>();
            string[] first = charsList.First();

            if (charsList.Count > 0)
            {
                foreach (var (prefix, nextItem) in from string prefix in maskList
                                                   from string nextItem in first
                                                   select (prefix, nextItem))
                {
                    switch (nextItem)
                    {
                        case ".":
                            maskBuffer.Add(prefix);
                            break;
                        default:
                            maskBuffer.Add(prefix + nextItem);
                            break;
                    }
                }
            }

            // this statement must be here before the stack is unwound
            maskList = maskBuffer;

            if (charsList.Count > 1)
            {
                charsList.Remove(first);
                maskList = CombineRemainder(charsList, maskList);
            }

            return maskList;
        }

        /// <summary>
        /// Build the range of characters for [A-FGHI] constructs.
        /// </summary>
        /// <param name="currentCharacter"></param>
        /// <param name="nextCharacter"></param>
        /// <returns></returns>
        private string BuildRange(string currentCharacter, string nextCharacter)
        {
            string expandedMask = "";
            int start;
            int end;

            switch (expandedMask)
            {
                // both numeric
                case string _ when IsNumeric(currentCharacter)
                                   && IsNumeric(nextCharacter):

                    if (Convert.ToInt32(currentCharacter) < Convert.ToInt32(nextCharacter))
                    {
                        start = GetNumberIndex(currentCharacter);
                        end = GetNumberIndex(nextCharacter);

                        for (var index = start; index <= end; index++)
                        {
                            expandedMask += Numbers[index];
                        }
                    }
                    return expandedMask;

                // both alpha
                case string _ when !IsNumeric(currentCharacter)
                                   && !IsNumeric(nextCharacter):

                    start = GetCharacterIndex(currentCharacter);
                    end = GetCharacterIndex(nextCharacter);

                    for (var index = start; index <= end; index++)
                    {
                        expandedMask += Alphabet[index];
                    }
                    return expandedMask;

                // numeric --> alpha
                case string _ when IsNumeric(currentCharacter)
                                   && !IsNumeric(nextCharacter):

                    start = GetCharacterIndex(nextCharacter);
                    end = GetCharacterIndex("Z");

                    for (var index = start; index <= end; index++)
                    {
                        expandedMask += Alphabet[index];
                    }
                    return expandedMask;

                // alpha-- > numeric - shouldn't happen
                case string _ when !IsNumeric(currentCharacter)
                                   && IsNumeric(nextCharacter):

                    Debug.Assert(!IsNumeric(currentCharacter) && IsNumeric(nextCharacter));
                    return expandedMask;
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
        /// Replace the meta characters with their string equivalent.
        /// </summary>
        /// <param name="expando"></param>
        /// <returns></returns>
        private string GetMetaMaskSet(string expando = "")
        {
            if (expando.Contains("#"))
            {
                expando = expando.Replace("#", "0123456789");
            }

            if (expando.Contains("@"))
            {
                expando = expando.Replace("@", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            }

            if (expando.Contains("?"))
            {
                expando = expando.Replace("?", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            }

            while (expando.Contains("-"))
            {
                var expandedMask = expando.Substring(0, expando.IndexOf("-") - 1);
                var post = expando.Substring(expando.IndexOf("-") + 2);
                var currentCharacter = expando.Substring(expando.IndexOf("-") - 1, 1);
                var nextCharacter = expando.Substring(expando.IndexOf("-") + 1, 1);
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
