﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public class CallSignInfo
    {
        public CallSignInfo(System.Xml.Linq.XElement element)
        {
            InitializeCallSignInfo(element);
        }

        public CallSignInfo()
        {
        }

        public CallSignInfo ShallowCopy()
        {
            return (CallSignInfo)this.MemberwiseClone();
        }

        /// <summary>
        /// public properties
        /// </summary>
        /// 
        private List<List<string[]>> primaryMaskList = new List<List<string[]>>();
        public Dictionary<string, byte> IndexKeys = new Dictionary<string, byte>();
        public int Rank;

        // --------------------------------------
        public int DXCC { get; set; }
        public int WAE { get; set; }
        public string Iota { get; set; }
        public string WAP { get; set; }
        public string CQ { get; set; }
        public string ITU { get; set; }
        public string Admin1 { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }

        //from callbook -----------------------------
        public string Continent { get; set; }
        public string TimeZone { get; set; }
        public string Admin2 { get; set; }
        public string Name { get; set; }
        public string QTH { get; set; }
        public string Comment { get; set; }
        public long CallbookEntry { get; set; }
        // ----------------------------------------
        public PrefixKind Kind { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool IsIota { get; set; }
        public string CallSign { get; set; }
        public string BaseCall { get; set; }
        public string FullPrefix { get; set; }
        public string MainPrefix { get; set; }
        public string HitPrefix { get; set; }
        public List<CallSignFlags> CallSignFlags { get; set; }

        /// <summary>
        /// Return the lists where the length of the list matches the count.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<List<string[]>> GetPrimaryMaskList(int count)
        {
            var temp = primaryMaskList.Where(x => x.Count == count);
            return temp.ToList(); 
        }

        public List<List<string[]>> GetPrimaryMaskList(string letter)
        {
            var temp = primaryMaskList.Where(x => x.First().Contains(letter));
            return temp.ToList(); 
        }
      
        /// <summary>
        /// The index key is a character that can be the first letter of a call.
        /// This way I can search faster.
        /// </summary>
        /// <param name="value"></param>
        public void SetPrimaryMaskList(List<string[]> value)
        {
            primaryMaskList.Add(value);

            foreach (var first in value[0])
            {
                if (!IndexKeys.ContainsKey(first))
                {
                     IndexKeys.Add(first, new byte() { });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefixXml"></param>
        private void InitializeCallSignInfo(XElement prefixXml)
        {
            string currentValue;

            foreach (XElement element in prefixXml.Elements())
            {
                currentValue = element.Value;

                switch (element.Name.ToString())
                {
                    case "masks":
                        break;
                    case "label":
                        FullPrefix = currentValue ?? "";
                        if (FullPrefix.Contains("."))
                        {
                            // get string after the "."
                            MainPrefix = FullPrefix.Substring(FullPrefix.LastIndexOf('.') + 1);
                        }
                        else
                        {
                            MainPrefix = FullPrefix;
                        }
                        break;
                    case "kind":
                        Kind = EnumEx.GetValueFromDescription<PrefixKind>(currentValue);
                        //initialize since DXCC.Kind does not have province field
                        //-prevent user having to check for null on a common field
                        if (Kind == PrefixKind.DXCC)
                        {
                            Province = "";
                        }
                        break;
                    case "country":
                        Country = currentValue ?? "";
                        break;
                    case "province":
                        Province = currentValue ?? "";
                        break;
                    case "dxcc_entity":
                        DXCC = Convert.ToInt32(currentValue);
                        break;
                    case "cq_zone":
                        CQ = currentValue ?? "";
                        break;
                    case "itu_zone":
                        ITU = currentValue ?? "";
                        break;
                    case "continent":
                        Continent = currentValue ?? "";
                        break;
                    case "time_zone":
                        TimeZone = currentValue ?? "";
                        break;
                    case "lat":
                        Latitude = currentValue ?? "";
                        break;
                    case "long":
                        Longitude = currentValue ?? "";
                        break;
                    case "city":
                        QTH = currentValue ?? "";
                        break;
                    case "wap_entity":
                        WAP = currentValue ?? "";
                        break;
                    case "wae_entity":
                        WAE = Convert.ToInt32(currentValue);
                        break;
                    case "province_id":
                        Admin1 = currentValue ?? "";
                        break;
                    case "start_date":
                        StartDate = currentValue ?? "";
                        break;
                    case "end_date":
                        EndDate = currentValue ?? "";
                        break;
                    default:
                        currentValue = null;
                        break;
                }
            }
        }
    } // end class
}

