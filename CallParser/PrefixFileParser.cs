﻿
/*
 PrefixFileParser.cs
 CallParser
 
 Created by Peter Bourget on 3/11/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Parse a prefix xml file and prefix
 combinations.
 */

using System;
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
        public Dictionary<int, CallSignInfo> Adifs { get; set; }
        public List<Admin> Admins;

        /// <summary>
        /// Private fields.
        /// </summary>
        //private HashSet<string> PrimaryMaskList;
        private HashSet<string> PrimaryMaskList;
        private readonly string[] Alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] Numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PrefixFileParser()
        {
            // preallocate space
            CallSignDictionary = new Dictionary<string, HashSet<CallSignInfo>>(1100000);
            Adifs = new Dictionary<int, CallSignInfo>();
            Admins = new List<Admin>();

            PrimaryMaskList = new HashSet<string>();
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
                                    BuildCallSignInfoEx(prefix, callSignInfo);
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
                                    //Task.Run(() => SemiBatchCallSignLookup());
                                    CallSignInfo callSignInfo = new CallSignInfo(prefix);
                                    BuildCallSignInfoEx(prefix, callSignInfo);
                                }
                            }
                        }
                    }
                }
            }
            var a = 1;
        }

        /// <summary>
        /// Loop through all of the prefix nodes and expand the masks for each prefix.
        /// </summary>
        /// <param name="prefix"></param>
        private void BuildCallSignInfoEx(XElement prefix, CallSignInfo callSignInfo)
        {
            HashSet<CallSignInfo> callSignInfoSet = new HashSet<CallSignInfo>();

            IEnumerable<XElement> masks = prefix.Elements().Where(x => x.Name == "masks");

            XElement dxcc_kind = prefix.Descendants().Where(x => x.Name == "kind").First();

            if (dxcc_kind.Value == "pfDXCC")
            {
                int dxcc = Convert.ToInt32(prefix.Descendants().Where(x => x.Name == "dxcc_entity").First().Value);
                Adifs.Add(dxcc, callSignInfo);
            }

            if (dxcc_kind.Value == "pfInvalidPrefix")
            {
                Adifs.Add(0, callSignInfo);
            }

            foreach (XElement element in masks.Descendants())
            {
                if (element.Value != "") // empty is usually a DXCC node
                {
                    // expand the mask if it exists
                    ExpandMask(element.Value);
                    // this must be "new()" not Clear() or it clears existing objects in the CallSignDictionary
                    callSignInfoSet = new HashSet<CallSignInfo>();
                    foreach (string mask in PrimaryMaskList)
                    {
                        callSignInfo.PrefixKey.Add(mask);
                        callSignInfoSet.Add(callSignInfo);

                        if (!CallSignDictionary.ContainsKey(mask))
                        {
                            CallSignDictionary.Add(mask, callSignInfoSet);
                        }
                        else
                        {
                            if (CallSignDictionary[mask].First().DXCC != callSignInfo.DXCC)
                            {
                                // this is to eliminate dupes 
                                CallSignDictionary[mask].UnionWith(callSignInfoSet);
                            }
                        }
                    }
                }
                PrimaryMaskList.Clear();
            }
        }

        /// <summary>
        /// Break up the mask into sections and expand all of the meta characters.
        /// </summary>
        /// <param name="mask"></param>
        internal void ExpandMask(string mask)
        {
            string maskPart;
            int counter = 0;
            int index;
            string expression = "";
            string item;
            // TEMPORARY: get rid of "." - need to work on this
            mask = mask.Replace(".", "");
            mask = String.Concat(mask.Where(c => !Char.IsWhiteSpace(c))); // sometimes "-" has spaces around it [1 - 8]
            int length = mask.Length;
            //mask = "[0Q]?A"; //"B[MNPQU-X#][1-8]@"; //B[MNPQU-X]0[#A-NP-Z]

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
                        expression += "[ABCDEFGHIJKLMNOPQRSTUVWXYZ]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "#": // numeric
                        expression += "[0123456789]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "?": // alphanumeric
                        expression += "[ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789]";
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

            CombineComponents(expression);
        }

        private void CombineComponents(string expression)
        {
            //HashSet<string> expressionList = new HashSet<string>();
            HashSet<char[]> charsList = new HashSet<char[]>();

            charsList = BuildCharArray(charsList, expression);

            // have list of arrays
            // for each caracter in the first list append each character of the second list
            if (charsList.Count == 1)
            {
                PrimaryMaskList.Add(new string(charsList.First()));
                return;
            }

            char[] first = charsList.First();
            char[] next = charsList.Skip(1).First();

            foreach (char firstItem in first)
            {
                foreach (char nextItem in next)
                {
                    //expressionList.Add(firstItem.ToString() + nextItem);
                    PrimaryMaskList.Add(firstItem.ToString() + nextItem);
                }
            }

            // are there more?
            if (charsList.Count > 2)
            {
                charsList.Remove(first);
                charsList.Remove(next);
                //CombineRemainder(charsList, expressionList);
                CombineRemainder(charsList);
            }
            else
            {
                //PrimaryMaskList = expressionList.ToHashSet();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="charsList"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        private HashSet<char[]> BuildCharArray(HashSet<char[]> charsList, string expression)
        {
            string temp;

            // first get everything in blocks separated by []
            if (expression.IndexOf("[") != -1 && expression.IndexOf("[") == 0)
            {
                temp = expression.Substring(1, expression.IndexOf("]") - 1);
                charsList.Add(temp.ToCharArray());
                expression = expression.Remove(0, expression.IndexOf("]") + 1);
            }
            else
            {
                if (expression.IndexOf("[") != -1)
                {
                    temp = expression.Substring(0, expression.IndexOf("["));
                    expression = expression.Remove(0, expression.IndexOf("["));
                    foreach (char x in temp)
                    {
                        charsList.Add(new char[1] { x });
                    }
                }
                else
                {
                    temp = expression;
                    expression = expression.Remove(0, temp.Length);
                    foreach (char x in temp)
                    {
                        charsList.Add(new char[1] { x });
                    }
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
        private void CombineRemainder(HashSet<char[]> charsList)
        {
            HashSet<string> tempList = new HashSet<string>();
            char[] first = charsList.First();

            if (charsList.Count > 0)
            {
                 _ = Parallel.ForEach(PrimaryMaskList, prefix =>
                //foreach (string prefix in PrimaryMaskList)
                {
                    foreach (char nextItem in first)
                    {
                        tempList.Add(prefix + nextItem);
                    }
                }
                );
            }

            // this statement must be here before the stack is unwound
            PrimaryMaskList = tempList;

            if (charsList.Count > 1)
            {
                charsList.Remove(first);
                CombineRemainder(charsList);
            }
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
                else
                {
                    // I seem to never hit this condition.
                    bool fail = false;
                    Debug.Assert(fail);
                    // 31 = V31/
                    expandedMask += currentCharacter;
                    expandedMask += nextCharacter;
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
