using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallParser
{
    [Serializable]
    public class PrefixData
    {
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

        public string mainPrefix = "";             //label ie: 3B6
        public string fullPrefix = "";             // ie: 3B6.3B7
        public PrefixKind kind = PrefixKind.pfNone;    //kind
        public string country = "";                //country
        public string province = "";              //province
        public string city = "";                    //city
        public string dxcc_entity = "";                   //dxcc_entity
        public string cq = "";                     //cq_zone
        public string itu = "";                    //itu_zone
        public string continent = "";              //continent
        public string timeZone = "";               //time_zone
        public string latitude = "0.0";            //lat
        public string longitude = "0.0";           //long


        public bool isParent = false;
        public bool hasChildren = false;
        public List<PrefixData> children = new List<PrefixData>();
        // expanded masks
   
        public List<HashSet<HashSet<string>>> primaryMaskSets;        //: [[Set<String>]]
        public List<List<string>> primaryMaskList;      //: [[Set<String>]]
        public OrderedDictionary primaryMaskDictionary = new OrderedDictionary();
        public List<String> rawMasks = new List<String>();

        bool adif = false;
        public string wae = "";
        public string wap = "";
        public string admin1 = "";
        public string admin2 = "";
        public string startDate = "";
        public string endDate = "";
        bool isIota = false; // implement
        string comment = "";

        string[] alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        /// Constructor
        /// </summary>
        public PrefixData()
        {
            // all the primary masks for this DXCC entity
            primaryMaskSets = new List<HashSet<HashSet<string>>>();
            primaryMaskList = new List<List<string>>();
        }

        /// <summary>
        /// Parse the FullPrefix to get the MainPrefix
        /// </summary>
        /// <param name="fullPrefix"></param>
        public void SetMainPrefix(string fullPrefix)
        {
            if (fullPrefix.IndexOf(".") != -1)
            {
                mainPrefix = fullPrefix.Substring(0, fullPrefix.IndexOf("."));
            }
            else
            {
                mainPrefix = fullPrefix;
            }
        }

        /// <summary>
        /// If this is a top level set the kind and adif flags.
        /// </summary>
        /// <param name="prefixKind"></param>
        public void SetDXCC(PrefixKind prefixKind)
        {
            kind = prefixKind;

            if (prefixKind == PrefixKind.pfDXCC)                      /////// THIS NEEDS CHECKING
            {
                adif = true;
                isParent = true;
            }
        }

        /// <summary>
        /// Save the original unexpanded mask.
        /// </summary>
        /// <param name="mask"></param>
        public void StoreMask(string mask)
        {
            rawMasks.Add(mask);
            ExpandMask(mask);
           // Console.WriteLine(mask);
        }

        /// <summary>
        /// Expand the mask into its separate components.
        /// </summary>
        /// <param name="mask"></param>
        public void ExpandMask(string mask)
        {
            HashSet<string> expandedMask;       // = new HashSet<String>();
            HashSet<HashSet<string>> expandedMaskSet = new HashSet<HashSet<string>>();
            List<List<string>> allCharacters = new List<List<string>>();
            string maskPart = "";
            string newMask = mask;
            char item;
            int counter = 0;
            int index;
            int nextIndex;
            int pass = 0;

            Console.WriteLine("New Set:");

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
                        expandedMaskSet.Add(expandedMask);
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

            PopulatePrimaryPrefixList(allCharacters, new List<string>(), new StringBuilder());
            primaryMaskSets.Add(expandedMaskSet);
        }

        private List<string> BuildPrefixList(HashSet<string> expandedMask)
        {
            List<string> characters = new List<string>();

            foreach (string piece in expandedMask)
            {
                characters.Add(piece);
            }

            return characters;
        }

        private void PopulatePrimaryPrefixList (List<List<string>> allCharacters, List<string> firstCharacter, StringBuilder prefix)
        {
            if (allCharacters.Count == 1)
            {
                string temp = allCharacters[0].First();
                string temp2 = firstCharacter.First();
                temp2 += temp;
                firstCharacter[0] = temp2;
                return;
            }

            foreach (string first in allCharacters[0])
            {
                foreach (string second in allCharacters[1])
                {
                    prefix.Append(first);
                    prefix.Append(second);
                    firstCharacter.Add(prefix.ToString());
                    prefix = new StringBuilder();
                }
            }

            if (allCharacters.Count > 2)
            {
                allCharacters.RemoveRange(0, 2);
                PopulatePrimaryPrefixList(allCharacters, firstCharacter, prefix);
            }

            foreach (string first in firstCharacter)
            {
                Console.WriteLine(first);
            }

            primaryMaskList.Add(firstCharacter);
            //primaryMaskDictionary.Add(firstCharacter[0], this);
        }

        /// <summary>
        /// Split string and eliminate "[" or "]" and empty entries.
        /// Save the mask after it has been processed.
        /// </summary>
        /// <param name="maskPart"></param>
        //private void ProcessLeftOver(string maskPart, HashSet<HashSet<string>> expandedMaskSet)
        //{
        //    string[] maskComponents = maskPart.Split("[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        //    Debug.Assert(maskComponents.Length == 1);

        //    //ParseMask(maskComponents, expandedMaskSet);
        //}

        /// <summary>
        /// Look at each character and see if it is a meta character to be expanded
        /// or a "-" which indicates a range. Otherewise store it as is.
        /// </summary>
        /// <param name="components"></param>
        public HashSet<string> ParseMask(string[] components)
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
                    } else
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

                if ((counter + 1) < componentString.Length) {
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
                  // }
                  //); // end for

            //expandedMaskSet.Add(expandedMask);
            return expandedMask;
        }

        /// <summary>
        /// A "-" was found which indicates a range. Build the alpha or numeric range to return.
        /// </summary>
        /// <param name="currentCharacter"></param>
        /// <param name="nextCharacter"></param>
        /// <returns></returns>
        public HashSet<string> BuildRange(string currentCharacter, string nextCharacter) // , HashSet<HashSet<string>> expandedMaskSetList
        {
            HashSet<string> expandedMask = new HashSet<string>();

            bool isNumeric = int.TryParse(currentCharacter, out int n);

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
                Debug.Assert(!IsNumeric(currentCharacter) && IsNumeric(nextCharacter));
            }

            return expandedMask;
        }

        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

        /// <summary>
        /// A #, ? or @  was found which indicates a range. Build the alpha or numeric range to return.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public HashSet<string> GetMetaMaskSet(string character)
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
            //throw new ArgumentException("Not found.", "description");
            return default;
        }
    }
}
