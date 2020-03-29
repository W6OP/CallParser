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
            prefixKey = new Dictionary<string, byte>();
            BuildCallSignInfo(element);
        }

        public CallSignInfo()
        {
        }

        public CallSignInfo ShallowCopy()
        {
            return (CallSignInfo)this.MemberwiseClone();
        }

        /// <summary>
        /// private fields
        /// </summary>
        private Dictionary<string, byte> prefixKey; // key for making this object hashable for searches
        private int dxcc;  //dxcc_entity
        private int wae;
        private string iota;
        private string wap;
        private string cq;           //cq_zone
        private string itu;          //itu_zone
        private string admin1;
        private string latitude;     //lat
        private string longitude;    //long
        private CallSignFlag[] flags;

        private string continent;     //continent
        private string timeZone;     //time_zone
        private string admin2;
        private string name;
        private string qth;
        private string comment;
        private long callbookEntry;
        //public string CallbookEntry: Pointer; //todo: find out data sources

        private PrefixKind kind;     //kind
        private string baseCall;
        private string portablePrefix;   //what I determined the prefix to be - mostly for debugging
        private string searchPrefix;
        private string country;       //country
        private string province;     //province 

        private string startDate;
        private string endDate;
        private bool isIota;

        private string fullCallSign; // I put the call sign here only for pfDXCC types for reference/debugging

        /// <summary>
        /// public properties
        /// </summary>
        public Dictionary<string, byte> PrefixKey { get => prefixKey; set => prefixKey = value; }
        public int DXCC => dxcc;
        public int WAE { get => wae; }
        public string Iota { get => iota; }
        public string WAP { get => wap; }
        public string Cq { get => cq; }
        public string Itu { get => itu; }
        public string Admin1 { get => admin1; }
        public string Latitude { get => latitude; }
        public string Longitude { get => longitude; }
        public CallSignFlag[] Flags { get => flags; }

        //from callbook -----------------------------
        public string Continent { get => continent;  set => continent = value; }
        public string TimeZone { get => timeZone; set => timeZone = value; }
        public string Admin2 { get => admin2;  set => admin2 = value; }
        public string Name { get => name; set => name = value; }
        public string QTH { get => qth;  set => qth = value; }
        public string Comment { get => comment;  set => comment = value; }
        public long CallbookEntry { get => callbookEntry; set => callbookEntry = value; }
        // ----------------------------------------
        public PrefixKind Kind { get => kind; }
        public string PortablePrefix { get => portablePrefix; set => portablePrefix = value; }
        public string SearchPrefix { get => searchPrefix; set => searchPrefix = value; }
        public string Country { get => country; }
        public string Province { get => province; }
        public string StartDate { get => startDate; }
        public string EndDate { get => endDate; }
        public bool IsIota { get => isIota; }
        public string CallSign { get => fullCallSign; set => fullCallSign = value; }
        public string BaseCall { get => baseCall; set => baseCall = value; }
       

        private void BuildCallSignInfo(XElement prefixXml)
        {
            string currentValue;

            var points = prefixXml.Descendants("country");
            var point = points.FirstOrDefault();

            foreach (XElement element in prefixXml.Elements())
            {
                currentValue = element.Value;

                switch (element.Name.ToString())
                {
                    case "masks":
                    break;
                    case "label":
                        portablePrefix = currentValue ?? "";
                        if (currentValue.Contains("."))
                        {
                            // get string after the "."
                            searchPrefix = portablePrefix.Substring(portablePrefix.LastIndexOf('.') + 1);
                        }
                        else
                        {
                            searchPrefix = portablePrefix;
                        }
                        break;
                    case "kind":
                        kind = EnumEx.GetValueFromDescription<PrefixKind>(currentValue);
                        //initialize since DXCC.Kind does not have province field
                        //-prevent user having to check for null on a common field
                        if (kind == PrefixKind.DXCC)
                        {
                            province = "";
                        }
                        break;
                    case "country":
                        country = currentValue ?? "";
                        break;
                    case "province":
                        province = currentValue ?? "";
                        break;
                    case "dxcc_entity":
                        dxcc = Convert.ToInt32(currentValue);
                        break;
                    case "cq_zone":
                        cq = currentValue ?? "";
                        break;
                    case "itu_zone":
                        itu = currentValue ?? "";
                        break;
                    case "continent":
                        continent = currentValue ?? "";
                        break;
                    case "time_zone":
                        timeZone = currentValue ?? "";
                        break;
                    case "lat":
                        latitude = currentValue ?? "";
                        break;
                    case "long":
                        longitude = currentValue ?? "";
                        break;
                    case "city":
                        qth = currentValue ?? "";
                        break;
                    case "wap_entity":
                        wap = currentValue ?? "";
                        break;
                    case "wae_entity":
                        wae = Convert.ToInt32(currentValue);
                        break;
                    case "province_id":
                        admin1 = currentValue ?? "";
                        break;
                    case "start_date":
                        startDate = currentValue ?? "";
                        break;
                    case "end_date":
                        endDate = currentValue ?? "";
                        break;
                    default:
                        currentValue = null;
                        break;
                }
            }
        }
    } // end class
}

