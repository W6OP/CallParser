/**
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
using Microsoft.Collections.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CallParser
{
    public enum PrefixKind
    {
        pfNone,
        pfDXCC,
        pfProvince,
        pfStation,
        pfDelDXCC,
        pfOldPrefix,
        pfNonDXCC,
        pfInvalidPrefix,
        pfDelProvince,
        pfCity
    }

    public enum CallSignFlag
    {
        cfInvalid,
        cfMaritime,
        cfPortable,
        cfSpecial,
        cfClub,
        cfBeacon,
        cfLotw,
        cfAmbigPrefix,
        cfQrp
    }

    /// <summary>
    /// Identify the type of character.
    /// </summary>
    enum CharacterType
    {
        [Description("")]
        empty,
        [Description("#")]
        numeric,
        [Description("@")]
        alphabetical,
        [Description("?")]
        alphanumeric,
        [Description("-")]
        dash,
        [Description(".")]
        dot,
        [Description("/")]
        slash
    }

    public class PrefixFileParser
    {

        //private List<Hit> _HitList;
        public List<ValueTuple<string, Hit>> _PrefixTuples;

        public Dictionary<string, List<Hit>> PrefixesDictionary { get; set; }


        /// <summary>
        /// Used so I don't have overhead of "new" in loops
        /// </summary>
        private List<string> result = new List<string>();
        private List<string> tempResult = new List<string>();
        private List<List<string>> allCharacters = new List<List<string>>();
        ///////////////////////////////////////////////////////////////

        public Hit[] _Adifs;
        public Dictionary<string, Hit> _Admins;

        public HashSet<List<string>> _PrimaryMaskList;
        private readonly string[] alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        

        /// <summary>
        /// Constructor.
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
            XDocument xDoc;

            PrefixesDictionary = new Dictionary<string, List<Hit>>(20000000);


            //_HitList = new List<Hit>();
            _PrefixTuples = new List<ValueTuple<string, Hit>>(20000000);
            _PrimaryMaskList = new HashSet<List<string>>();

            _Adifs = new Hit[999];
            _Admins = new Dictionary<string, Hit>();

            if (File.Exists(prefixFilePath))
            {
                try
                {
                    xDoc = XDocument.Load(prefixFilePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to parse the prefix file: " + new { prefixFilePath }, ex);
                }
            }
            else
            {
                using (StreamReader stream = new StreamReader(assembly.GetManifestResourceStream("CallParser.PrefixList.xml")))
                {
                    xDoc = XDocument.Load(stream);
                }
            }

            ParsePrefixDataList(xDoc);
        }

        /// <summary>
        /// Parse the prefix data list. Loop through each prefix node.
        /// </summary>
        /// <param name="xDoc"></param>
        private void ParsePrefixDataList(XDocument xDoc)
        {
            var prefixes = xDoc.Root.Elements("prefix");

            foreach (XElement prefixXml in prefixes)
            {
                BuildCallSignInfo(prefixXml);
            }

            var a = 2;
        }

        /// <summary>
        /// Using the data in each prefix node build a CallSignInfo object
        /// for each one. Save it in a dictionary (_PrefixDict).
        /// </summary>
        /// <param name="prefixXml"></param>
        private void BuildCallSignInfo(XElement prefixXml)
        {
            Hit hit = new Hit();
            List<Hit> hitList;  // = new List<Hit>();
            //(string mask, Hit res) prefixInformation; // ValueTuple

            _PrimaryMaskList.Clear(); 

            string currentValue;

            foreach (XElement element in prefixXml.Elements())
            {
                currentValue = element.Value;

                switch (element.Name.ToString())
                {
                    case "masks":
                        foreach (XElement mask in element.Elements())
                        {
                            currentValue = mask.Value;
                            ExpandMask(currentValue);
                        }
                        break;
                    case "label":
                        hit.FullPrefix = currentValue ?? "";
                        if (currentValue.Contains("."))
                        {
                            // get string after the "."
                            hit.MainPrefix = hit.FullPrefix.Substring(hit.FullPrefix.LastIndexOf('.') + 1);
                        }
                        else
                        {
                            hit.MainPrefix = hit.FullPrefix;
                        }
                        break;
                    case "kind":
                        hit.Kind = EnumEx.GetValueFromDescription<PrefixKind>(currentValue);
                        break;
                    case "country":
                        hit.Country = currentValue ?? "";
                        break;
                    case "province":
                        hit.Province = currentValue ?? "";
                        break;
                    case "dxcc_entity":
                        hit.Dxcc = currentValue ?? "";
                        break;
                    case "cq_zone":
                        hit.Cq = currentValue ?? "";
                        break;
                    case "itu_zone":
                        hit.Itu = currentValue ?? "";
                        break;
                    case "continent":
                        hit.Continent = currentValue ?? "";
                        break;
                    case "time_zone":
                        hit.TimeZone = currentValue ?? "";
                        break;
                    case "lat":
                        hit.Latitude = currentValue ?? "";
                        break;
                    case "long":
                        hit.Longitude = currentValue ?? "";
                        break;
                    case "city":
                        hit.Qth = currentValue ?? "";
                        break;
                    case "wap_entity":
                        hit.Wap = currentValue ?? "";
                        break;
                    case "wae_entity":
                        hit.Wae = currentValue ?? "";
                        break;
                    case "province_id":
                        hit.Admin1 = currentValue ?? "";
                        break;
                    case "start_date":
                        hit.StartDate = currentValue ?? "";
                        break;
                    case "end_date":
                        hit.EndDate = currentValue ?? "";
                        break;
                    default:
                        currentValue = null;
                        break;
                }
            }

            if (hit.Kind == PrefixKind.pfInvalidPrefix)
            {
                //_Adifs[0] = hit;
            }

            if (hit.Wae != "")
            {
                //_Adifs[Convert.ToInt32(hit.Wae)] = hit;
            }

            if (hit.Kind == PrefixKind.pfDXCC)
            {
               // _Adifs[Convert.ToInt32(hit.Dxcc)] = hit;
            }

            if (hit.Kind == PrefixKind.pfProvince && hit.Admin1 != "")
            {
                //_Admins.Add(callSignInfo.Admin1, callSignInfo);
            }

            // load the primary prefix for this entity
            if (hit.Kind == PrefixKind.pfDXCC)
            {
                hitList = new List<Hit>();
                if (!PrefixesDictionary.ContainsKey(hit.MainPrefix))
                {
                    hitList.Add(hit);
                    PrefixesDictionary.Add(hit.MainPrefix, hitList);
                }
                else
                {
                    hitList = PrefixesDictionary[hit.MainPrefix];
                    hitList.Add(hit);
                    // when we expand the mask to all possible values then some will be duplicated
                    // Console.WriteLine(hit.FullPrefix + " duplicate top: duplicate top duplicate top duplicate top *******************************************************" + hit.Kind.ToString());
                }
            }


            // prefixInformation.mask = hit



            foreach (List<string> prefixList in _PrimaryMaskList)
            {
                hitList = new List<Hit>(prefixList.Count);
                foreach (string prefix in prefixList)
                {
                    ////if (prefix != callSignInfo.MainPrefix)
                    ////{
                    if (!PrefixesDictionary.ContainsKey(prefix))
                    {
                        hitList.Add(hit);
                        PrefixesDictionary.Add(prefix, hitList);
                    }
                    else
                    {
                        //    //when we expand the mask to all possible values then some will be duplicated
                        //    // ie.AL, NL for Alaska
                        hitList = PrefixesDictionary[prefix];
                        hitList.Add(hit);
                        //Console.WriteLine(prefix + " duplicate: " + hit.Kind.ToString() + " : " + hit.Country);
                    }
                }
            }
        }



        // *********************************************************************************************************

       


        /// <summary>
        /// Expand the mask into its separate components.
        /// </summary>
        /// <param name="mask"></param>
        internal void ExpandMask(string mask)
        {
            HashSet<string> expandedMask;
            //HashSet<HashSet<string>> expandedMaskSet = new HashSet<HashSet<string>>();
            //List<List<string>> allCharacters = new List<List<string>>();
            string maskPart;
            string newMask = mask;
            char item;
            int counter = 0;
            int index;
            int nextIndex;
            int pass = 0;

            allCharacters.Clear();

            // TEMPORARY: get rid of "."
            mask = mask.Replace(".", "");


            while (counter < mask.Length)
            {
                item = mask[counter];
                pass += 1;

                switch (item.ToString())
                {
                    case "[":
                        index = newMask.IndexOf("]");
                        nextIndex = index + 1;
                        maskPart = newMask.Substring(0, nextIndex);
                        string[] maskComponents = maskPart.Split("[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        expandedMask = ParseMask(maskComponents);
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += maskPart.Length;
                        nextIndex = counter;
                        newMask = mask.Substring(nextIndex);
                        break;
                    case "@":
                    case "#":
                    case "?":
                        expandedMask = GetMetaMaskSet(item.ToString());
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += 1;
                        newMask = mask.Substring(counter);
                        break;
                    default: // single character
                        expandedMask = new HashSet<string>
                        {
                            item.ToString()
                        };
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += 1;
                        newMask = mask.Substring(counter);
                        break;
                }
            }

            PopulatePrimaryPrefixListEx(allCharacters);
        }

        
        /// <summary>
        /// Primary
        /// </summary>
        /// <param name="allCharacters"></param>
        private void PopulatePrimaryPrefixListEx(List<List<string>> allCharacters)
        {
            result.Clear();

            // take first 2 columns
            foreach (string first in allCharacters[0])
            {
                foreach (string second in allCharacters[1])
                {
                    result.Add(first + second);
                }
            }

            allCharacters.RemoveRange(0, 2);
            if (allCharacters.Count > 0)
            {
                PopulatePrimaryPrefixListEx(result, allCharacters);
            }
            else
            {
                _PrimaryMaskList.Add(result);
            }
        }

        /// <summary>
        /// Recursive
        /// </summary>
        /// <param name="result"></param>
        /// <param name="allCharacters"></param>
        private void PopulatePrimaryPrefixListEx(List<string> result, List<List<string>> allCharacters)
        {
            //List<string> firstColumn;
            //List<string> secondColumn;

            //List<string> tempResult = new List<string>();
            List<string> tempResult2 = new List<string>();
            tempResult.Clear();
            //tempResult2.Clear();
           

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
                    //firstColumn = allCharacters[0];
                    //secondColumn = allCharacters[1];

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
                PopulatePrimaryPrefixListEx(tempResult2, allCharacters);
            }
            else
            {
               _PrimaryMaskList.Add(tempResult2);

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

            //bool isNumeric = int.TryParse(currentCharacter, out int n);

            // both numeric
            if (IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                if (Convert.ToInt32(currentCharacter) < Convert.ToInt32(nextCharacter))
                {
                    int start = GetNumberIndex(currentCharacter);
                    int end = GetNumberIndex(nextCharacter);

                    for (int index = start; index <= end; index++)
                    {
                        expandedMask.Add(numbers[index]);
                    }
                }
                else
                {
                    // I seem to never hit this condition
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
                    expandedMask.Add(alphabet[index]);
                }
            }

            // numeric --> alpha
            if (IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(nextCharacter);
                int end = GetCharacterIndex("Z");

                for (int index = start; index <= end; index++)
                {
                    expandedMask.Add(alphabet[index]);
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
                    foreach (string digit in numbers)
                    {
                        expandedMask.Add(digit);
                    }
                    break;
                case CharacterType.alphanumeric:
                    foreach (string digit in numbers)
                    {
                        expandedMask.Add(digit);
                    }
                    foreach (string letter in alphabet)
                    {
                        expandedMask.Add(letter);
                    }
                    break;
                case CharacterType.alphabetical:
                    foreach (string letter in alphabet)
                    {
                        expandedMask.Add(letter);
                    }
                    break;
                default:
                    break;
            }

            return expandedMask;
        }

        private int GetCharacterIndex(string character)
        {
            return Array.IndexOf(alphabet, character);
        }

        private int GetNumberIndex(string character)
        {
            return Array.IndexOf(numbers, character);
        }

    } // end class



    /// <summary>
    /// Get the enum value from the description.
    /// </summary>
    public static class EnumEx
    {
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            return default;
        }
    } // end class
}
