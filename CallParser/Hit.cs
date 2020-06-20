using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    [Serializable]
    public class Hit
    {
        public Hit()
        {
           
        }

        public Hit(PrefixData prefixData)
        {
            PopulateHit(prefixData);
            DXCCMerged = new HashSet<int>();
        }

        private void PopulateHit(PrefixData prefixData)
        {
            WAE = prefixData.WAE;
            WAP = prefixData.WAP;
            Iota = prefixData.Iota;
            IsIota = prefixData.IsIota;
            CQ = prefixData.CQ;
            ITU = prefixData.ITU;
            Admin1 = prefixData.Admin1;
            Admin2 = prefixData.Admin2;
            Latitude = prefixData.Latitude;
            Longitude = prefixData.Longitude;
            Continent = prefixData.Continent;
            TimeZone = prefixData.TimeZone;
            Name = prefixData.Name;
            QTH = prefixData.QTH;
            CallbookEntry = prefixData.CallbookEntry;
            Kind = prefixData.Kind;
            Country = prefixData.Country;
            Province = prefixData.Province;
            DXCC = prefixData.DXCC;
            StartDate = prefixData.StartDate;
            EndDate = prefixData.EndDate;
            CallSign = prefixData.CallSign;
            MainPrefix = prefixData.MainPrefix;

            Comment = prefixData.Comment;
            CallSignFlags = prefixData.CallSignFlags;
            IsQRZInformation = prefixData.IsQRZInformation;
        }

        public int WAE { get; set; }
        public string Iota { get; set; }
        public string WAP { get; set; }
        public HashSet<int> CQ { get; set; }
        public HashSet<int> ITU { get; set; }
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
        public int DXCC { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool IsIota { get; set; }
        // Call sign as entered
        public string CallSign { get; set; }
        // call sign without the prefix/suffix
        public string BaseCall { get; set; }
        public string FullPrefix { get; set; }
        public string MainPrefix { get; set; }
        public string HitPrefix { get; set; }
        public HashSet<CallSignFlags> CallSignFlags { get; set; }

        public bool IsMergedHit { get; set; }

        public HashSet<int> DXCCMerged { get; set; }

        public bool IsQRZInformation { get; set; }
    } // end class
}
