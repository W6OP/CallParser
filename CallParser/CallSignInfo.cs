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
 CallSingInfo.cs
 CallParser
 
 Created by Peter Bourget on 3/11/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Call Sign Information object returned to the caller.
 This contains all the meta data for a particular prefix.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace CallParser
{
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

    /// <summary>
    /// Class returned to the caling program with the call sign meta data.
    /// </summary>
    public class CallSignInfo
    {
        public string Dxcc;  //dxcc_entity
        public string Wae;
        public string Iota;
        public string Wap;
        public string Cq;           //cq_zone
        public string Itu;          //itu_zone
        public string Admin1;
        public string Latitude;     //lat
        public string Longitude;    //long
        public CallSignFlag[] Flags;

        public string Continent;     //continent
        public string TimeZone;     //time_zone
        public string Admin2;
        public string Name;
        public string Qth;
        public string Comment;
        //public string CallbookEntry: Pointer; //to find out data sources

        public PrefixKind Kind;     //kind
        public string FullPrefix;   //what I determined the prefix to be - mostly for debugging
        public string MainPrefix;
        public string Country;       //country
        public string Province;     //province

        public string StartDate;
        public string EndDate;
        public bool IsIota = false;

        public List<List<string>> PrimaryMaskList;
        private readonly string[] alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CallSignInfo()
        {
            PrimaryMaskList = new List<List<string>>();
        }

        /// <summary>
        /// Expand the mask into its separate components.
        /// </summary>
        /// <param name="mask"></param>
        internal void ExpandMask(string mask)
        {
            HashSet<string> expandedMask;       // = new HashSet<String>();
            HashSet<HashSet<string>> expandedMaskSet = new HashSet<HashSet<string>>();
            List<List<string>> allCharacters = new List<List<string>>();
            string maskPart;
            string newMask = mask;
            char item;
            int counter = 0;
            int index;
            int nextIndex;
            int pass = 0;


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
                        expandedMaskSet.Add(expandedMask);
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += maskPart.Length;
                        nextIndex = counter;
                        newMask = mask.Substring(nextIndex);
                        break;
                    case "@":
                    case "#":
                    case "?":
                        expandedMask = GetMetaMaskSet(item.ToString());
                        _ = expandedMaskSet.Add(expandedMask);
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += 1;
                        newMask = mask.Substring(counter);
                        break;
                    default: // single character
                        expandedMask = new HashSet<string>
                        {
                            item.ToString()
                        };
                        expandedMaskSet.Add(expandedMask);
                        allCharacters.Add(BuildPrefixList(expandedMask));
                        counter += 1;
                        newMask = mask.Substring(counter);
                        break;
                }
            }

            PopulatePrimaryPrefixList(allCharacters);
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
        /// Populate the list with the expanded masks.
        /// </summary>
        /// <param name="allCharacters"></param>
        private void PopulatePrimaryPrefixList(List<List<string>> allCharacters)
        {
            List<string> tempList = new List<string>();
            StringBuilder prefix = new StringBuilder();
            string temp;
            string temp2;

            if (allCharacters.Count == 1)
            {
                // THIS S NEVER HIT AND IS INCORRECT
                temp = allCharacters[0].First();
                temp2 = tempList.First();
                temp2 += temp;
                tempList[0] = temp2;
                return;
            }

            foreach (string first in allCharacters[0])
            {
                prefix.Clear();  // = new StringBuilder();
                foreach (string second in allCharacters[1])
                {
                    prefix.Append(first);
                    prefix.Append(second);
                    tempList.Add(prefix.ToString());
                    prefix.Clear(); // = new StringBuilder();
                }
            }

            if (allCharacters.Count > 2)
            {
                allCharacters.RemoveRange(0, 2);
                PopulatePrimaryPrefixList(allCharacters, tempList, prefix);
            }

            PrimaryMaskList.Add(tempList);
        }

        /// <summary>
        /// Populate the list with the expanded masks - recursively.
        /// </summary>
        /// <param name="allCharacters"></param>
        /// <param name="tempList"></param>
        /// <param name="prefix"></param>
        private void PopulatePrimaryPrefixList(List<List<string>> allCharacters, List<string> tempList, StringBuilder prefix)
        {
            List<string> tempList2 = new List<string>();
            string temp2;
            string temp;

            if (allCharacters.Count == 1)
            {
                if (allCharacters[0].Count == 1)
                {
                    temp = allCharacters[0].First();
                    for (int i = 0; i < tempList.Count; i++)
                    {
                        temp2 = tempList[i];
                        temp2 += temp;
                        tempList[i] = temp2;
                    }
                }
                else
                {
                    foreach (string first in allCharacters[0])
                    {
                        prefix.Clear(); // = new StringBuilder();
                        prefix.Append(first);
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            tempList2.Add(tempList[i] + prefix.ToString());
                        }
                        prefix.Clear(); // = new StringBuilder();
                    }
                    // add templist 2 to templist
                    tempList.AddRange(tempList2);
                }
                return;
            }

            foreach (string first in allCharacters[0])
            {
                prefix = new StringBuilder();
                foreach (string second in allCharacters[1])
                {
                    prefix.Append(first);
                    prefix.Append(second);
                    for (int i = 0; i < tempList.Count; i++)
                    {
                        tempList2.Add(tempList[i] + prefix.ToString());
                    }
                    prefix.Clear(); // = new StringBuilder();
                }
            }

            if (allCharacters.Count > 2)
            {
                allCharacters.RemoveRange(0, 2);
                PopulatePrimaryPrefixList(allCharacters, tempList, prefix);
            }

            // add templist 2 to templist
            tempList.AddRange(tempList2);
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
                    tempMask = BuildRange(currentCharacter, nextCharacter); //, expandedMaskSet
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
                            tempMask = BuildRange(previousCharacter, nextCharacter); // , expandedMaskSet
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
        private HashSet<string> BuildRange(string currentCharacter, string nextCharacter) // , HashSet<HashSet<string>> expandedMaskSetList
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
                    // expandedMaskSetList.Add(expandedMask);
                    expandedMask = new HashSet<string>();
                    expandedMask.Append(nextCharacter);
                    //expandedMaskSetList.Add(expandedMask);
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
}
