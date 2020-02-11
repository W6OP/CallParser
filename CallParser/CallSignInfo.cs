using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    public class CallSignInfo
    {
        public CallSignInfo()
        {
        }

        /// <summary>
        /// private fields
        /// </summary>
        private int dxcc;  //dxcc_entity
        private int wae;
        private string iota;
        private string wap;
        private int cq;           //cq_zone
        private int itu;          //itu_zone
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
        //public string CallbookEntry: Pointer; //todo: find out data sources

        private PrefixKind kind;     //kind
        private string fullPrefix;   //what I determined the prefix to be - mostly for debugging
        private string mainPrefix;
        private string country;       //country
        private string province;     //province

        private DateTime startDate;
        private DateTime endDate;
        private bool isIota;

        private string callSign; // I put the call sign here only for pfDXCC types for reference/debugging

        /// <summary>
        /// public properties
        /// </summary>
        public int DXCC { get => dxcc; set => dxcc = value; }
        public int WAE { get => wae; set => wae = value; }
        public string Iota { get => iota; set => iota = value; }
        public string WAP { get => wap; set => wap = value; }
        public int Cq { get => cq; set => cq = value; }
        public int Itu { get => itu; set => itu = value; }
        public string Admin1 { get => admin1; set => admin1 = value; }
        public string Latitude { get => latitude; set => latitude = value; }
        public string Longitude { get => longitude; set => longitude = value; }
        public CallSignFlag[] Flags { get => flags; set => flags = value; }
        public string Continent { get => continent; set => continent = value; }
        public string TimeZone { get => timeZone; set => timeZone = value; }
        public string Admin2 { get => admin2; set => admin2 = value; }
        public string Name { get => name; set => name = value; }
        public string QTH { get => qth; set => qth = value; }
        public string Comment { get => comment; set => comment = value; }
        public PrefixKind Kind { get => kind; set => kind = value; }
        public string FullPrefix { get => fullPrefix; set => fullPrefix = value; }
        public string MainPrefix { get => mainPrefix; set => mainPrefix = value; }
        public string Country { get => country; set => country = value; }
        public string Province { get => province; set => province = value; }
        public DateTime StartDate { get => startDate; set => startDate = value; }
        public DateTime EndDate { get => endDate; set => endDate = value; }
        public bool IsIota { get => isIota; set => isIota = value; }
        public string CallSign { get => callSign; set => callSign = value; }
    } // end class
}

/*
 * 
			SortedList<string, object="">
    sl = new SortedList<string, object="">
      ();
      foreach(item in prefixesDoc) {
      prefix = new prefix(xdoc.element);
      foreach (mask in prefix) {
      List call = GenerateCallSignm( prefix);
      sl.Add(call, prefix);

      }

 TCallsignInfo = class
    //quick info
    Dxcc,
    Wae,
    Iota,
    Wap,
    Cq,
    Itu,
    Admin1: string;
    Location: TPoint;
    Flags: TCallsignFlags;

    //from callbook
    Continent,
    TimeZone,
    Admin2,
    Name,
    Qth,
    Comment: string;
    CallbookEntry: Pointer; //to find out data sources

    //from prefix
    Kind: TPrefixKind;
    FullPrefix,
    MainPrefix,
    Country,
    Province,
    StartDate, EndDate: string;

    function IsIota: boolean;
   */
