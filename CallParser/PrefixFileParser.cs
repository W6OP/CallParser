﻿/**
 * Copyright (c) 2019 Peter Bourget W6OP
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish,
 * distribute, sublicense, create a derivative work, and/or sell copies of the
 * Software in any work that is designed, intended, or marketed for pedagogical or
 * instructional purposes related to programming, coding, application development,
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works,
 * or sale is expressly withheld.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/*
 PrefixFileParser.cs
 CallParser
 
 Created by Peter Bourget on 3/11/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Parse a prefix xml file and build all possible prefix
 combinations.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    /// <summary>
    /// Load and parse the prefix file to create all possible prefix
    /// patterns. 
    /// </summary>
    public class PrefixFileParser
    {
        /// <summary>
        /// Public fields.
        /// </summary>
        public Dictionary<string, List<Hit>> PrefixesDictionary { get; set; }
        public Dictionary<string, HashSet<CallSignInfo>> CallSignDictionary;
        //public Dictionary<Int32, Hit> Adifs { get; set; }
        public Dictionary<Int32, CallSignInfo> Adifs { get; set; }
        public List<Admin> Admins;


        /// <summary>
        /// Normally these would be local variables but here they are global
        /// so I don't have overhead of "new" in loops. Can use .Clear() instead
        /// sometimes but not always.
        /// </summary>
        //private List<string> tempResult = new List<string>();
        // private List<List<string>> allCharacters = new List<List<string>>();

        /// <summary>
        /// Private fields.
        /// </summary>
        private HashSet<string> _PrimaryMaskList;
        private readonly string[] _Alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] _Numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PrefixFileParser()
        {

        }

        /// <summary>
        /// Load the prefix fle. If a file path is passed in then load that file.
        /// Otherwise use the embedded resource file for the prefix list.
        /// </summary>
        /// <param name="prefixFilePath"></param>
        public void ParsePrefixFile(string prefixFilePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            XDocument xDocument;

            // preallocate collection size for performance
            PrefixesDictionary = new Dictionary<string, List<Hit>>(10000000);
            CallSignDictionary = new Dictionary<string, HashSet<CallSignInfo>>();

            //Adifs = new Dictionary<int, Hit>();
            Adifs = new Dictionary<int, CallSignInfo>();
            Admins = new List<Admin>();

            _PrimaryMaskList = new HashSet<string>();

            if (File.Exists(prefixFilePath))
            {
                try
                {
                    xDocument = XDocument.Load(prefixFilePath);
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
                    xDocument = XDocument.Load(stream);
                }
            }

            ParsePrefixDataList(xDocument);
        }

        /// <summary>
        /// Parse the prefix data list. 
        /// Loop through each prefix node.
        /// </summary>
        /// <param name="xDoc"></param>
        private void ParsePrefixDataList(XDocument xDocument)
        {
            var prefixes = xDocument.Root.Elements("prefix");
            //IEnumerable<XElement> dxcc = prefixes.Elements().Where(x => x.Name == "dxcc_entity");

            var groups = xDocument.Descendants("prefix")
                .GroupBy(x => (string)x.Element("dxcc_entity"))
                .ToList();

            //var group = groups[291];

            foreach (var group in groups)
            {
                BuildCallSignInfoEx(group);
            }

            int a = 1;

            // original code
            //foreach (XElement prefixXml in prefixes)
            //{
            //    BuildCallSignInfo(prefixXml);
            //}
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="group"></param>
        private void BuildCallSignInfoEx(IGrouping<string, XElement> group)
        {
            HashSet<CallSignInfo> callSignInfoSet = new HashSet<CallSignInfo>();
            CallSignInfo callSignInfo = new CallSignInfo();
            IEnumerable<XElement> masks = group.Elements().Where(x => x.Name == "masks");

            int dxcc_entity = Convert.ToInt32(group.Key);
            if (dxcc_entity != 0)
            {
                XElement dxccElement = group.Descendants().Where(x => x.Name == "kind" && x.Value == "pfDXCC").First().Parent;
                // create the DXCC structure
                callSignInfo = new CallSignInfo(dxccElement);
                Adifs.Add(dxcc_entity, callSignInfo);
            }
            else
            {
                XElement dxccElement = group.Descendants().Where(x => x.Name == "kind" && x.Value == "pfInvalidPrefix").First().Parent;
                // create the DXCC structure
                callSignInfo = new CallSignInfo(dxccElement);
                Adifs.Add(dxcc_entity, callSignInfo);
            }

            foreach (XElement prefix in group)
            {
                masks = prefix.Elements().Where(x => x.Name == "masks");

                callSignInfo = new CallSignInfo(prefix);

                foreach (XElement element in masks.Descendants())
                {
                    if (element.Value != "") // empty is usually a DXCC node
                    {
                        ExpandMask(element.Value);

                        callSignInfoSet = new HashSet<CallSignInfo>();
                        foreach (string item in _PrimaryMaskList)
                        {
                            callSignInfo.PrefixKey.Add(item);
                            if (!CallSignDictionary.ContainsKey(item))
                            {
                                
                                callSignInfoSet.Add(callSignInfo);
                                CallSignDictionary.Add(item, callSignInfoSet);
                            }
                            else
                            {
                                callSignInfoSet.Add(callSignInfo);
                                HashSet<CallSignInfo> callDictionarySet = CallSignDictionary[item];

                                if (callDictionarySet.First().DXCC != callSignInfo.DXCC)
                                {
                                    callDictionarySet.UnionWith(callSignInfoSet);
                                }
                            }
                        }
                    }
                    _PrimaryMaskList.Clear();
                }
            }
        }

        /*
         
			SortedList<string, object="">
    sl = new SortedList<string, object="">
      ();
      foreach(item in prefixesDoc) {
      prefix = new prefix(xdoc.element);
      foreach (mask in prefix) {
      List call = GenerateCallSignm( prefix);
      sl.Add(call, prefix);

      }
             */

        //private void BuildCallSignInfoEx(XElement prefixXml)
        //{
        //    CallSignInfo callSignInfo = new CallSignInfo();
        //    IEnumerable<XElement> masks = prefixXml.Elements().Where(x => x.Name == "masks");
        //    foreach (XElement element in masks)
        //    {
        //        //ExpandMask(element.Value);
        //        ExpandMask("K[ABDEFIJKMNOQ-Z]4");
        //    }

        //    var a = 1;
        //}

        /// <summary>
        /// Expand the mask into its separate components.
        /// </summary>
        /// <param name="mask"></param>
        internal void ExpandMask(string mask)
        {
            HashSet<string> expandedMask;
            string maskPart;
            string temporaryMask;
            char item;
            int counter = 0;
            int index;
            int nextIndex;
            int pass = 0;

            List<List<string>> allCharacters = new List<List<string>>();

            // TEMPORARY: get rid of "." - need to work on this
            mask = mask.Replace(".", "");
            temporaryMask = mask;

            while (counter < mask.Length)
            {
                item = mask[counter];
                pass += 1;

                switch (item.ToString())
                {
                    case "[":
                        index = temporaryMask.IndexOf("]");
                        nextIndex = index + 1;
                        maskPart = temporaryMask.Substring(0, nextIndex);
                        string[] maskComponents = maskPart.Split("[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        expandedMask = ParseMask(maskComponents);
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += maskPart.Length;
                        nextIndex = counter;
                        temporaryMask = mask.Substring(nextIndex);
                        break;
                    case "@":
                    case "#":
                    case "?":
                        expandedMask = GetMetaMaskSet(item.ToString());
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += 1;
                        temporaryMask = mask.Substring(counter);
                        break;
                    default: // single character
                        expandedMask = new HashSet<string>
                        {
                            item.ToString()
                        };
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += 1;
                        temporaryMask = mask.Substring(counter);
                        break;
                }
            }

            PopulatePrimaryPrefixList(allCharacters);
        }


        /// <summary>
        /// Start building the primary prefix list. Concatenate the first two entities.
        /// If there are more entities to add then call the recursive secondary
        /// method.
        /// </summary>
        /// <param name="allCharacters"></param>
        private void PopulatePrimaryPrefixList(List<List<string>> allCharacters)
        {
            // take first 2 columns
            foreach (string first in allCharacters[0])
            {
                foreach (string second in allCharacters[1])
                {
#if DEBUG
                    if (_PrimaryMaskList.Contains(first + second))
                    {
                        var a = 1;
                    }
#endif
                    _PrimaryMaskList.Add(first + second);
                }
            }

            allCharacters.RemoveRange(0, 2);
            if (allCharacters.Count > 0)
            {
                PopulatePrimaryPrefixList(_PrimaryMaskList, allCharacters);
            }
            else
            {
                //_PrimaryMaskList = result;
            }
        }

        /// <summary>
        /// Recursively add more entities to the primary prefix list.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="allCharacters"></param>
        private void PopulatePrimaryPrefixList(HashSet<string> result, List<List<string>> allCharacters)
        {

            HashSet<string> tempResult2 = new HashSet<string>();
            HashSet<string> tempResult = new HashSet<string>();

            // Because there are millions of possible call signs possible when the masks
            // are expanded I am limiting them to max of 4 characters long or we run
            // out of memory on a 32 bit system (any CPU). Remove these 3 lines if you
            // want the full possible list and are compiling x64.
            if (allCharacters.Count > 2)
            {
                allCharacters.RemoveRange(1, allCharacters.Count - 1);
                //allCharacters.RemoveRange(2, allCharacters.Count - 2);
            }
            //-------------------------------------------------------------------------

            switch (allCharacters.Count)
            {
                case 0:
                    Debug.Assert(allCharacters.Count == 0);
                    break;
                case 1:
                    foreach (string pre in result)
                    {
                        foreach (string end in allCharacters[0])
                        {
                            tempResult2.Add(pre + end);
                        }
                    }
                    allCharacters.RemoveRange(0, 1);
                    break;
                default:
                    foreach (string first in allCharacters[0])
                    {
                        foreach (string second in allCharacters[1])
                        {
                            tempResult.Add(first + second);
                        }
                    }

                    foreach (string pre in result)
                    {
                        foreach (string end in tempResult)
                        {

                            tempResult2.Add(pre + end);
                        }
                    }
                    allCharacters.RemoveRange(0, 2);
                    break;
            }


            if (allCharacters.Count > 0)
            {
                PopulatePrimaryPrefixList(tempResult2, allCharacters);
            }
            else
            {
                _PrimaryMaskList = tempResult2;
            }
        }


        /// <summary>
        /// DELETE ??
        /// This just copys the contents of a hashset to a list.
        /// I don't know if it is really useful or I should jut put it in a list to begin with.
        /// </summary>
        /// <param name="expandedMask"></param>
        /// <returns></returns>
        private List<string> BuildPrefixList(HashSet<string> expandedMask)
        {
            List<string> characters = new List<string>();

            foreach (string piece in expandedMask)
            {
                characters.Add(piece);
            }

            return characters;
        }

        /// <summary>
        /// Look at each character and see if it is a meta character to be expanded
        /// or a "-" which indicates a range. Otherewise store it as is.
        /// </summary>
        /// <param name="maskComponents"></param>
        /// <returns></returns>
        private HashSet<string> ParseMask(string[] components)
        {
            string currentCharacter;
            string nextCharacter;
            string previousCharacter;
            string componentString;
            int counter = 0;
            HashSet<string> expandedMask = new HashSet<string>();
            HashSet<string> tempMask;

            foreach (char item in components[0])
            {
                componentString = components[0];

                if (counter >= componentString.Length)
                {
                    return expandedMask; // completed
                }

                if (componentString[counter].ToString() == string.Empty)
                {
                    return expandedMask; // completed
                }

                // TODO: double check the [0-9-W] is processed correctly - 7[RT-Y][016-9@] - [AKW]L#/

                currentCharacter = componentString[counter].ToString() ?? "";
                tempMask = GetMetaMaskSet(currentCharacter);
                expandedMask.UnionWith(tempMask);

                while (tempMask.Count != 0)
                { // in case of ##
                    counter += 1;
                    if (counter < componentString.Length)
                    {
                        currentCharacter = componentString[counter].ToString() ?? "";
                    }
                    else
                    {
                        currentCharacter = "";
                    }
                    tempMask = GetMetaMaskSet(currentCharacter);
                    expandedMask.Union(tempMask);
                }

                if (counter < componentString.Length)
                {
                    currentCharacter = componentString[counter].ToString() ?? "";
                }
                else
                {
                    currentCharacter = "";
                }

                if ((counter + 1) < componentString.Length)
                {
                    nextCharacter = componentString[counter + 1].ToString() ?? "";
                }
                else
                {
                    nextCharacter = "";
                }

                // is the nextChar a "-" ??
                if (EnumEx.GetValueFromDescription<CharacterType>(nextCharacter) == CharacterType.dash)
                {
                    counter += 1;
                    nextCharacter = componentString[counter + 1].ToString() ?? "";
                    tempMask = BuildRange(currentCharacter, nextCharacter);
                    expandedMask.UnionWith(tempMask);
                    counter += 2;
                }
                else
                {
                    if (currentCharacter != string.Empty)
                    {
                        if (EnumEx.GetValueFromDescription<CharacterType>(nextCharacter) == CharacterType.dash)
                        {
                            // 0-9-W get previous character
                            previousCharacter = componentString[counter - 1].ToString() ?? "";
                            tempMask = BuildRange(previousCharacter, nextCharacter);
                            expandedMask.UnionWith(tempMask);
                            counter += 1;
                        }
                        else
                        {
                            expandedMask.Add(currentCharacter);
                        }
                    }
                    counter += 1;
                }
            } // end for

            return expandedMask;
        }

        /// <summary>
        /// A "-" was found which indicates a range. Build the alpha or numeric range to return.
        /// </summary>
        /// <param name="currentCharacter"></param>
        /// <param name="nextCharacter"></param>
        /// <returns></returns>
        private HashSet<string> BuildRange(string currentCharacter, string nextCharacter)
        {
            HashSet<string> expandedMask = new HashSet<string>();

            // both numeric
            if (IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                if (Convert.ToInt32(currentCharacter) < Convert.ToInt32(nextCharacter))
                {
                    int start = GetNumberIndex(currentCharacter);
                    int end = GetNumberIndex(nextCharacter);

                    for (int index = start; index <= end; index++)
                    {
                        expandedMask.Add(_Numbers[index]);
                    }
                }
                else
                {
                    // I seem to never hit this condition.
                    bool fail = false;
                    Debug.Assert(fail);
                    // 31 = V31/
                    expandedMask.Append(currentCharacter);
                    expandedMask = new HashSet<string>();
                    expandedMask.Append(nextCharacter);
                    expandedMask = new HashSet<string>();
                }
            }

            // both alpha
            if (!IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(currentCharacter);
                int end = GetCharacterIndex(nextCharacter);

                for (int index = start; index <= end; index++)
                {
                    expandedMask.Add(_Alphabet[index]);
                }
            }

            // numeric --> alpha
            if (IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(nextCharacter);
                int end = GetCharacterIndex("Z");

                for (int index = start; index <= end; index++)
                {
                    expandedMask.Add(_Alphabet[index]);
                }
            }

            if (!IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                System.Diagnostics.Debug.Assert(!IsNumeric(currentCharacter) && IsNumeric(nextCharacter));
            }

            return expandedMask;
        }

        private bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

        /// <summary>
        /// A #, ? or @  was found which indicates a range. Build the alpha or numeric range to return.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private HashSet<string> GetMetaMaskSet(string character)
        {
            HashSet<string> expandedMask = new HashSet<string>();
            CharacterType characterType = EnumEx.GetValueFromDescription<CharacterType>(character);

            //print(character)
            switch (characterType)
            {
                case CharacterType.numeric:
                    foreach (string digit in _Numbers)
                    {
                        expandedMask.Add(digit);
                    }
                    break;
                case CharacterType.alphanumeric:
                    foreach (string digit in _Numbers)
                    {
                        expandedMask.Add(digit);
                    }
                    foreach (string letter in _Alphabet)
                    {
                        expandedMask.Add(letter);
                    }
                    break;
                case CharacterType.alphabetical:
                    foreach (string letter in _Alphabet)
                    {
                        expandedMask.Add(letter);
                    }
                    break;
                default:
                    break;
            }

            return expandedMask;
        }

        /// <summary>
        /// Get the index in to the _Alphabet array.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private int GetCharacterIndex(string character)
        {
            return Array.IndexOf(_Alphabet, character);
        }

        /// <summary>
        /// Get the index in the _Numbers array.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private int GetNumberIndex(string character)
        {
            return Array.IndexOf(_Numbers, character);
        }

    } // end class
}
