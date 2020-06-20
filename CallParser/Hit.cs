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

        public Hit(CallSignInfo callSignInfo)
        {
            PopulateHit(callSignInfo);
            DXCCMerged = new HashSet<int>();
        }

        private void PopulateHit(CallSignInfo callSignInfo)
        {
            WAE = callSignInfo.WAE;
            WAP = callSignInfo.WAP;
            Iota = callSignInfo.Iota;
            IsIota = callSignInfo.IsIota;
            CQ = callSignInfo.CQ;
            ITU = callSignInfo.ITU;
            Admin1 = callSignInfo.Admin1;
            Admin2 = callSignInfo.Admin2;
            Latitude = callSignInfo.Latitude;
            Longitude = callSignInfo.Longitude;
            Continent = callSignInfo.Continent;
            TimeZone = callSignInfo.TimeZone;
            Name = callSignInfo.Name;
            QTH = callSignInfo.QTH;
            CallbookEntry = callSignInfo.CallbookEntry;
            Kind = callSignInfo.Kind;
            Country = callSignInfo.Country;
            Province = callSignInfo.Province;
            DXCC = callSignInfo.DXCC;
            StartDate = callSignInfo.StartDate;
            EndDate = callSignInfo.EndDate;
            CallSign = callSignInfo.CallSign;
            MainPrefix = callSignInfo.MainPrefix;

            Comment = callSignInfo.Comment;
            CallSignFlags = callSignInfo.CallSignFlags;
            IsQRZInformation = callSignInfo.IsQRZInformation;
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
