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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
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

    public class PrefixFileParser
    {
        public Dictionary<string, CallSignInfo> _PrefixDict;
        public Dictionary<string, CallSignInfo> _PrefixDict2;
        public CallSignInfo[] _Adifs;
        public Dictionary<string, CallSignInfo> _Admins;

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

            _PrefixDict = new Dictionary<string, CallSignInfo>();
            _PrefixDict2 = new Dictionary<string, CallSignInfo>();
            _Adifs = new CallSignInfo[999];
            _Admins = new Dictionary<string, CallSignInfo>();

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
        }

        /// <summary>
        /// Using the data in each prefix node build a CallSignInfo object
        /// for each one. Save it in a dictionary (_PrefixDict).
        /// </summary>
        /// <param name="prefixXml"></param>
        private void BuildCallSignInfo(XElement prefixXml)
        {
            CallSignInfo callSignInfo = new CallSignInfo();

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
                            callSignInfo.ExpandMask(currentValue);
                        }
                        break;
                    case "label":
                        callSignInfo.FullPrefix = currentValue ?? "";
                        if (currentValue.Contains("."))
                        {
                            callSignInfo.MainPrefix = callSignInfo.FullPrefix.Substring(callSignInfo.FullPrefix.LastIndexOf('.') + 1);
                        }
                        else
                        {
                            callSignInfo.MainPrefix = callSignInfo.FullPrefix;
                        }
                        break;
                    case "kind":
                        callSignInfo.Kind = EnumEx.GetValueFromDescription<PrefixKind>(currentValue);
                        break;
                    case "country":
                        callSignInfo.Country = currentValue ?? "";
                        break;
                    case "province":
                        callSignInfo.Province = currentValue ?? "";
                        break;
                    case "dxcc_entity":
                        callSignInfo.Dxcc = currentValue ?? "";
                        break;
                    case "cq_zone":
                        callSignInfo.Cq = currentValue ?? "";
                        break;
                    case "itu_zone":
                        callSignInfo.Itu = currentValue ?? "";
                        break;
                    case "continent":
                        callSignInfo.Continent = currentValue ?? "";
                        break;
                    case "time_zone":
                        callSignInfo.TimeZone = currentValue ?? "";
                        break;
                    case "lat":
                        callSignInfo.Latitude = currentValue ?? "";
                        break;
                    case "long":
                        callSignInfo.Longitude = currentValue ?? "";
                        break;
                    case "city":
                        callSignInfo.Qth = currentValue ?? "";
                        break;
                    case "wap_entity":
                        callSignInfo.Wap = currentValue ?? "";
                        break;
                    case "wae_entity":
                        callSignInfo.Wae = currentValue ?? "";
                        break;
                    case "province_id":
                        callSignInfo.Admin1 = currentValue ?? "";
                        break;
                    case "start_date":
                        callSignInfo.StartDate = currentValue ?? "";
                        break;
                    case "end_date":
                        callSignInfo.EndDate = currentValue ?? "";
                        break;
                    default:
                        currentValue = null;
                        break;
                }
            }

            if (callSignInfo.Kind == PrefixKind.pfInvalidPrefix)
            {
                _Adifs[0] = callSignInfo;
            }

            if (callSignInfo.Wae != "")
            {
                _Adifs[Convert.ToInt32(callSignInfo.Wae)] = callSignInfo;
            }

            if (callSignInfo.Kind == PrefixKind.pfDXCC)
            {
                _Adifs[Convert.ToInt32(callSignInfo.Dxcc)] = callSignInfo;
            }

            if (callSignInfo.Kind == PrefixKind.pfProvince && callSignInfo.Admin1 != "")
            {
                //_Admins.Add(callSignInfo.Admin1, callSignInfo);
            }

            // load the primary prefix for this entity
            if (!_PrefixDict.ContainsKey(callSignInfo.MainPrefix))
            {
                _PrefixDict.Add(callSignInfo.MainPrefix, callSignInfo);
            }
            else
            {
                if (!_PrefixDict2.ContainsKey(callSignInfo.MainPrefix))
                {
                    _PrefixDict2.Add(callSignInfo.MainPrefix, callSignInfo);
                }
                else
                {
                   // when we expand the mask to all possible values then some will be duplicated
                    Console.WriteLine(callSignInfo.MainPrefix + " duplicate top: duplicate top duplicate top duplicate top *******************************************************" + callSignInfo.Kind.ToString());
                }
            }

            foreach (List<string> prefixList in callSignInfo.PrimaryMaskList)
            {
                foreach (string prefix in prefixList)
                {
                    if (prefix != callSignInfo.MainPrefix)
                    {
                        if (!_PrefixDict.ContainsKey(prefix))
                        {
                            _PrefixDict.Add(prefix, callSignInfo);
                        }
                        else
                        {
                            if (!_PrefixDict2.ContainsKey(prefix))
                            {
                                _PrefixDict2.Add(prefix, callSignInfo);
                            }
                            else
                            {
                                //when we expand the mask to all possible values then some will be duplicated
                                // ie.AL, NL for Alaska

                               Console.WriteLine(prefix + " duplicate: " + callSignInfo.Kind.ToString() + " : " + callSignInfo.Country);
                            }
                        }
                    }
                }
            }
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
