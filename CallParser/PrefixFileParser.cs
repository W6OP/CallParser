
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
        private HashSet<string> PrimaryMaskList;
        private HashSet<string> PrimaryMaskListEx;
        private readonly string[] Alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private readonly string[] Numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

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

            CallSignDictionary = new Dictionary<string, HashSet<CallSignInfo>>();
            Adifs = new Dictionary<int, CallSignInfo>();
            Admins = new List<Admin>();

            PrimaryMaskList = new HashSet<string>();
            PrimaryMaskListEx = new HashSet<string>();

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
        /// Parse the xml prefix data list. 
        /// Loop through each prefix node.
        /// </summary>
        /// <param name="xDoc"></param>
        private void ParsePrefixDataList(XDocument xDocument)
        {
            var prefixes = xDocument.Root.Elements("prefix");
            var groups = xDocument.Descendants("prefix")
                .GroupBy(x => (string)x.Element("dxcc_entity"))
                .ToList();

            int b = 1;

            foreach (var group in groups)
            {
                BuildCallSignInfo(group);
            }

            //int distinctCount = CallSignDictionary.Select(x => x.Value).Distinct().Count();
            int a = 2;
        }

        /// <summary>
        /// Loop through all of the prefix nodes and expand the masks for each prefix.
        /// </summary>
        /// <param name="group"></param>
        private void BuildCallSignInfo(IGrouping<string, XElement> group)
        {
            HashSet<CallSignInfo> callSignInfoSet = new HashSet<CallSignInfo>();
            CallSignInfo callSignInfo = new CallSignInfo();
            IEnumerable<XElement> masks = group.Elements().Where(x => x.Name == "masks");

            int dxcc_entity = Convert.ToInt32(group.Key);
            if (dxcc_entity != 0)
            {
                XElement dxccElement = group.Descendants().Where(x => x.Name == "kind" && x.Value == "pfDXCC").First().Parent;
                // create the DXCC structure and save in the Adifs list.
                callSignInfo = new CallSignInfo(dxccElement);
                Adifs.Add(dxcc_entity, callSignInfo);
            }
            else
            {
                XElement dxccElement = group.Descendants().Where(x => x.Name == "kind" && x.Value == "pfInvalidPrefix").First().Parent;
                // create the DXCC structure and save in the Adifs list.
                callSignInfo = new CallSignInfo(dxccElement);
                Adifs.Add(dxcc_entity, callSignInfo);
            }

            // loop through each prefix node
            foreach (XElement prefix in group)
            {
                masks = prefix.Elements().Where(x => x.Name == "masks");

                // CallSignInfo class populates itself
                callSignInfo = new CallSignInfo(prefix);

                foreach (XElement element in masks.Descendants())
                {
                    if (element.Value != "") // empty is usually a DXCC node
                    {
                        // expand the mask if it exists
                        ExpandMaskEx(element.Value);
                        
                        // this must be "new()" not Clear() or it clears existing objects in the CallSignDictionary
                        //callSignInfoSet = new HashSet<CallSignInfo>();
                        //foreach (string mask in PrimaryMaskListEx)
                        //{
                        //    callSignInfo.PrefixKey.Add(mask);
                        //    callSignInfoSet.Add(callSignInfo);

                        //    if (!CallSignDictionary.ContainsKey(mask))
                        //    {
                        //        CallSignDictionary.Add(mask, callSignInfoSet);
                        //    }
                        //    else
                        //    {
                        //        if (CallSignDictionary[mask].First().DXCC != callSignInfo.DXCC)
                        //        {
                        //            // this is to eliminate dupes 
                        //            CallSignDictionary[mask].UnionWith(callSignInfoSet);
                        //        }
                        //    }
                        //}
                    }
                    PrimaryMaskList.Clear();
                    PrimaryMaskListEx.Clear();
                }
            }
        }


        /// <summary>
        /// Break up the mask into sections and expand all of the meta characters.
        /// </summary>
        /// <param name="mask"></param>
        internal void ExpandMaskEx(string mask)
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
                item = mask.Substring(0,1);
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
                        //expression += "[A-Z]";
                        // use constant for performance
                        expression += "[ABCDEFGHIJKLMNOPQRSTUVWXYZ]";       //"[" + String.Join("", Alphabet) + "]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "#": // numeric
                        //expression += "[0-9]";
                        expression += "[0123456789]";       // "[" + String.Join("", Numbers) + "]";
                        counter += 1;
                        if (counter < length)
                        {
                            mask = mask.Substring(1);
                        }
                        break;
                    case "?": // alphanumeric
                        //expression += "[0-9A-Z]";
                        expression += "[ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789]";       // "[" + String.Join("", Numbers) + String.Join("", Alphabet) + "]";
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
            //PrimaryMaskListEx = expressionList.ToHashSet();
        }

        private void CombineComponents(string expression)
        {
            HashSet<string> expressionList = new HashSet<string>();
            HashSet<char[]> charsList = new HashSet<char[]>();

            charsList = BuildCharArray(charsList, expression);

            // have list of arrays
            // for each caracter in the first list append each character of the second list
            if (charsList.Count == 1)
            {
                PrimaryMaskListEx.Add(new string(charsList.First()));
                return;
            }

            char[] first = charsList.First();
            char[] next = charsList.Skip(1).First();

            foreach (char firstItem in first)
            {
                foreach (char nextItem in next)
                {
                    expressionList.Add(firstItem.ToString() + nextItem);
                }
            }

            // are there more?
            if (charsList.Count > 2)
            {
                charsList.Remove(first);
                charsList.Remove(next);
                CombineRemainder(charsList, expressionList);
            }
            else
            {
                PrimaryMaskListEx = expressionList.ToHashSet();
            }
        }

        private HashSet<char[]> BuildCharArray(HashSet<char[]> charsList, string expression)
        {
            string temp;

            // first get everything in blocks separated by []
            if (expression.IndexOf("[") != -1 && expression.IndexOf("[") == 0)
            {
                temp = expression.Substring(1, expression.IndexOf("]") - 1);
                switch (temp.IndexOf("-"))
                {
                    case -1:
                        {
                            char[] letters2 = new char[temp.Length];
                            letters2 = temp.ToCharArray();
                            charsList.Add(letters2);
                            expression = expression.Remove(0, expression.IndexOf("]") + 1);
                            break;
                        }

                    default:
                        {
                           // temp = temp.Replace(" ", ""); // sometimes the "-" has spaces around it
                            while (temp[0] != '-' ) 
                            {
                                char[] letters3 = new char[1];
                                letters3[0] = temp[0]; //.ToCharArray();
                                charsList.Add(letters3);
                                temp = temp.Remove(0, 1);
                            }
                            HashSet<string> tempMask = BuildRange(charsList.Last()[0].ToString(), temp[1].ToString());

                            charsList.Remove(charsList.Last());

                            //tempMask.Remove(tempMask.First());
                            char[] letters2 = new char[tempMask.Count];
                            string output = string.Join("", tempMask);
                            letters2 = output.ToCharArray();
                            charsList.Add(letters2);
                            expression = expression.Remove(0, expression.IndexOf("]") + 1);
                            break;
                        }                 // if (EnumEx.GetValueFromDescription<CharacterType>(x.ToString()) == CharacterType.dash)
                }                 // if (EnumEx.GetValueFromDescription<CharacterType>(x.ToString()) == CharacterType.dash)
            }
            else
            {
                if (expression.IndexOf("[") != -1)
                {
                    temp = expression.Substring(0, expression.IndexOf("["));
                    expression = expression.Remove(0, expression.IndexOf("["));
                    foreach (char x in temp)
                    {
                        char[] singles = new char[1];
                        singles[0] = x;
                        charsList.Add(singles);
                    }
                }
                else
                {
                    temp = expression;
                    expression = expression.Remove(0, temp.Length);
                    foreach (char x in temp)
                    {
                        char[] singles = new char[1];
                        singles[0] = x;
                        charsList.Add(singles);
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
        private void CombineRemainder(HashSet<char[]> charsList, HashSet<string> expressionList)
        {
            HashSet<string> tempList = new HashSet<string>();
            char[] first = charsList.First();

            if (charsList.Count > 0)
            {
                foreach (string prefix in expressionList)
                {
                    foreach (char nextItem in first)
                    {
                        tempList.Add(prefix + nextItem.ToString());
                    }
                }
            }

            PrimaryMaskListEx = tempList;

            if (charsList.Count > 1)
            {
                charsList.Remove(first);
                CombineRemainder(charsList, tempList);
            }
        }



        /// <summary>
        /// Expand the #, @ and ? symbols
        /// </summary>
        /// <param name="maskPart"></param>
        /// <returns></returns>
        private string ExpandMetaCharacter(string maskPart)
        {
            maskPart = maskPart.Replace("@", "A-Z");
            maskPart = maskPart.Replace("#", "0-9");
            maskPart = maskPart.Replace("@", "A-Z0-9");

            return maskPart;
        }

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
            mask = String.Concat(mask.Where(c => !Char.IsWhiteSpace(c))); // sometimes "-" has spaces around it
            temporaryMask = mask;

            

            while (counter < mask.Length)
            {
                item = mask[counter];
                pass += 1;

                switch (item.ToString())
                {
                    case "[": // range
                        index = temporaryMask.IndexOf("]");
                        nextIndex = index + 1;
                        maskPart = temporaryMask.Substring(0, nextIndex);
                        string[] maskComponents = maskPart.Split("[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        allCharacters.Add(ParseMask(maskComponents));
                        counter += maskPart.Length;
                        nextIndex = counter;
                        temporaryMask = mask.Substring(nextIndex);
                        break;
                    case "@": // alpha
                    case "#": // numeric
                    case "?": // alphanumeric
                        allCharacters.Add(GetMetaMaskSet(item.ToString()).ToList());
                        counter += 1;
                        temporaryMask = mask.Substring(counter);
                        break;
                    default: // single character
                        expandedMask = new HashSet<string>
                        {
                            item.ToString()
                        };
                        allCharacters.Add(expandedMask.ToList());
                        counter += 1;
                        temporaryMask = mask.Substring(counter);
                        break;
                }
            }

            PopulatePrimaryPrefixList(allCharacters);
        }


        /// <summary>
        /// Start building the primary prefix list. Concatenate the first two entities.
        /// If there are more entities to add and then call the recursive secondary
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
                    PrimaryMaskList.Add(first + second);
                }
            }

            allCharacters.RemoveRange(0, 2);
            if (allCharacters.Count > 0)
            {
                PopulatePrimaryPrefixList(PrimaryMaskList, allCharacters);
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
            // want the full possible list and are compiling x64. The extra characters
            // are not necessary to determine the dxcc entity.
            //if (allCharacters.Count > 2)
            //{
            //    allCharacters.RemoveRange(1, allCharacters.Count - 1);
            //}

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
                PrimaryMaskList = tempResult2;
            }
        }


        /// <summary>
        /// This just copys the contents of a hashset to a list.
        /// I don't know if it is really useful or I should jut put it in a list to begin with.
        /// </summary>
        /// <param name="expandedMask"></param>
        /// <returns>List<string></returns>
        //private List<string> BuildPrefixList(HashSet<string> expandedMask)
        //{
        //    List<string> characters = new List<string>();

        //    foreach (string piece in expandedMask)
        //    {
        //        characters.Add(piece);
        //    }

        //    return characters;
        //}

        /// <summary>
        /// Look at each character and see if it is a meta character to be expanded
        /// or a "-" which indicates a range. Otherewise store it as is.
        /// </summary>
        /// <param name="components"></param>
        /// <returns>HashSet<string></returns>
        private List<string> ParseMask(string[] components)
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
                    return expandedMask.ToList(); // completed
                }

                if (componentString[counter].ToString() == string.Empty)
                {
                    return expandedMask.ToList(); // completed
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
            }

            return expandedMask.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentCharacter"></param>
        /// <param name="nextCharacter"></param>
        /// <returns></returns>
        private string BuildRangeEx(string currentCharacter, string nextCharacter)
        {
            //HashSet<string> expandedMask = new HashSet<string>();
            string expandedMaskEx = "";

            // both numeric
            if (IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                if (Convert.ToInt32(currentCharacter) < Convert.ToInt32(nextCharacter))
                {
                    int start = GetNumberIndex(currentCharacter);
                    int end = GetNumberIndex(nextCharacter);

                    for (int index = start; index <= end; index++)
                    {
                        //expandedMask.Add(Numbers[index]);
                        expandedMaskEx += Numbers[index];
                    }
                }
                else
                {
                    // I seem to never hit this condition.
                    bool fail = false;
                    Debug.Assert(fail);
                    // 31 = V31/
                    //expandedMask.Append(currentCharacter);
                    expandedMaskEx += currentCharacter;
                    //expandedMask = new HashSet<string>();
                    //expandedMask.Append(nextCharacter);
                    expandedMaskEx += nextCharacter;
                    //expandedMask = new HashSet<string>();
                }
            }

            // both alpha
            if (!IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(currentCharacter);
                int end = GetCharacterIndex(nextCharacter);

                for (int index = start; index <= end; index++)
                {
                    //expandedMask.Add(Alphabet[index]);
                    expandedMaskEx += Alphabet[index];
                }
            }

            // numeric --> alpha
            if (IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(nextCharacter);
                int end = GetCharacterIndex("Z");

                for (int index = start; index <= end; index++)
                {
                    //expandedMask.Add(Alphabet[index]);
                    expandedMaskEx += Alphabet[index];
                }
            }

            if (!IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                Debug.Assert(!IsNumeric(currentCharacter) && IsNumeric(nextCharacter));
            }

            return expandedMaskEx;
        }

        /// <summary>
        /// A "-" was found which indicates a range. Build the alpha or numeric range to return.
        /// </summary>
        /// <param name="currentCharacter"></param>
        /// <param name="nextCharacter"></param>
        /// <returns>HashSet<string></returns>
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
                        expandedMask.Add(Numbers[index]);
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
                    expandedMask.Add(Alphabet[index]);
                }
            }

            // numeric --> alpha
            if (IsNumeric(currentCharacter) && !IsNumeric(nextCharacter))
            {
                int start = GetCharacterIndex(nextCharacter);
                int end = GetCharacterIndex("Z");

                for (int index = start; index <= end; index++)
                {
                    expandedMask.Add(Alphabet[index]);
                }
            }

            if (!IsNumeric(currentCharacter) && IsNumeric(nextCharacter))
            {
                System.Diagnostics.Debug.Assert(!IsNumeric(currentCharacter) && IsNumeric(nextCharacter));
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
        /// A #, ? or @  was found which indicates a range. Build the alpha or numeric range to return.
        /// </summary>
        /// <param name="character"></param>
        /// <returns>HashSet<string></returns>
        private HashSet<string> GetMetaMaskSet(string character)
        {
            HashSet<string> expandedMask = new HashSet<string>();
            CharacterType characterType = EnumEx.GetValueFromDescription<CharacterType>(character);

            switch (characterType)
            {
                case CharacterType.numeric:
                    expandedMask = new HashSet<string>(Numbers);
                    break;
                case CharacterType.alphanumeric:
                    expandedMask = new HashSet<string>(Numbers);
                    expandedMask.UnionWith(Alphabet);
                    break;
                case CharacterType.alphabetical:
                    expandedMask = new HashSet<string>(Alphabet);
                    break;
                default:
                    break;
            }

            return expandedMask;
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
                //string temp = String.Join("", Numbers) + String.Join("", Alphabet);
                //temp += string.Join("", Alphabet);
                expando = expando.Replace("?", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            }

            while (expando.IndexOf("-") != -1)
            {
                string expandedMask = expando.Substring(0, expando.IndexOf("-") - 1);
                string post = expando.Substring(expando.IndexOf("-") + 2);
                string currentCharacter = expando.Substring(expando.IndexOf("-") - 1, 1);
                string nextCharacter = expando.Substring(expando.IndexOf("-") + 1, 1);
                //HashSet<string> expandedMe = BuildRangeEx(currentCharacter, nextCharacter);
                //foreach (string s in expandedMe)
                //{
                //    expandedMask += s;
                //}
                expandedMask += BuildRangeEx(currentCharacter, nextCharacter);
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
