using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CallParser
{
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

        public CallSignInfo()
        {
        }
    }
    public class PrefixFileParser
    {
        public Dictionary<string, PrefixData> _PrefixDict;

        /////////////////////////////////////////////////
        //public List<CallSignInfo> _HitList;
        //public CallSignInfo[] _Adifs;
        //public Dictionary<string, CallSignInfo> _Admins;
        /////////////////////////////////////////////////
        ///
        /// <summary>
        /// Constructor.
        /// </summary>
        public PrefixFileParser()
        {
            _PrefixDict = new Dictionary<string, PrefixData>();

            //////////////////////////////////////////////
            //_HitList = new List<CallSignInfo>();
            //_Adifs = new CallSignInfo[999];
            //_Admins = new Dictionary<string, CallSignInfo>();
            //////////////////////////////////////////////////////////
        }

        public void ParsePrefixFile(string prefixFilePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            XDocument xDoc;

            if (File.Exists(prefixFilePath))
            {
                xDoc = XDocument.Load(prefixFilePath);
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
        /// Parse the prefix data list.
        /// </summary>
        /// <param name="xDoc"></param>
        private void ParsePrefixDataList(XDocument xDoc)
        {
            // DELETE
            PrefixData prefixData = new PrefixData();

            var prefixes = xDoc.Root.Elements("prefix");
            
            foreach (XElement prefixXml in prefixes)
            {
                BuildPrefixData(prefixXml);
            }

            var a = 2;
        }

        /// <summary>
        /// Populate the PrefixData object.
        /// </summary>
        /// <param name="prefixXml"></param>
        private void BuildPrefixData(XElement prefixXml)
        {
            PrefixData prefixData = new PrefixData();
           
            string currentValue = "";

            foreach (XElement element in prefixXml.Elements())
            {
                currentValue = element.Value;

                switch (element.Name.ToString())
                {
                    case "masks":
                        foreach (XElement mask in element.Elements())
                        {
                            currentValue = mask.Value;
                            prefixData.StoreMask(currentValue);
                        }
                        break;
                    case "label":
                        prefixData.fullPrefix = currentValue ?? "";
                        prefixData.SetMainPrefix(fullPrefix: currentValue ?? "");
                        break;
                    case "kind":
                        prefixData.SetDXCC(EnumEx.GetValueFromDescription<PrefixKind>(currentValue));
                        break;
                    case "country":
                        prefixData.country = currentValue ?? "";
                        break;
                    case "province":
                        prefixData.province = currentValue ?? "";
                        break;
                    case "dxcc_entity":
                        prefixData.dxcc_entity = currentValue ?? "";
                        break;
                    case "cq_zone":
                        prefixData.cq = currentValue ?? "";
                        break;
                    case "itu_zone":
                        prefixData.itu = currentValue ?? "";
                        break;
                    case "continent":
                        prefixData.continent = currentValue ?? "";
                        break;
                    case "time_zone":
                        prefixData.timeZone = currentValue ?? "";
                        break;
                    case "lat":
                        prefixData.latitude = currentValue ?? "";
                        break;
                    case "long":
                        prefixData.longitude = currentValue ?? "";
                        break;
                    case "city":
                        prefixData.city = currentValue ?? "";
                        break;
                    case "wap_entity":
                        prefixData.wap = currentValue ?? "";
                        break;
                    case "wae_entity":
                        prefixData.wae = currentValue ?? "";
                        break;
                    case "province_id":
                        prefixData.admin1 = currentValue ?? "";
                        break;
                    case "start_date":
                        prefixData.startDate = currentValue ?? "";
                        break;
                    case "end_date":
                        prefixData.endDate = currentValue ?? "";
                        break;
                    default:
                        currentValue = null;
                        break;
                }
            }

            // load the primary prefix for this entity
            // if (!_PrefixDict.ContainsKey(prefixData.fullPrefix))
            if (!_PrefixDict.ContainsKey(prefixData.mainPrefix))
            {
                _PrefixDict.Add(prefixData.mainPrefix, prefixData);
                // WAS !!! fullPrefix
                //_PrefixDict.Add(prefixData.fullPrefix, prefixData);
            }

            // add the additional prefixes
            foreach (List<string> prefixList in prefixData.primaryMaskList)
           {
                foreach (string prefix in prefixList)
                {
                    // if (prefix != prefixData.fullPrefix)
                    if (prefix != prefixData.mainPrefix)
                    {
                        if (!_PrefixDict.ContainsKey(prefix))
                        {
                            _PrefixDict.Add(prefix, prefixData);
                        } else
                        {
                           // Console.WriteLine(prefix + " duplicate: " + prefixData.kind.ToString());
                        }
                    }
                }
           }
        }

    } //end class
}
