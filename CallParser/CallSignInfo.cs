using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public class CallSignInfo
    {
        public CallSignInfo(System.Xml.Linq.XElement element)
        {
            prefixKey = new HashSet<string>();
            BuildCallSignInfo(element);
        }

        public CallSignInfo()
        {
        }

        /// <summary>
        /// private fields
        /// </summary>
        private HashSet<string> prefixKey;
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
        //public string CallbookEntry: Pointer; //todo: find out data sources

        private PrefixKind kind;     //kind
        private string fullPrefix;   //what I determined the prefix to be - mostly for debugging
        private string mainPrefix;
        private string country;       //country
        private string province;     //province

        private string startDate;
        private string endDate;
        private bool isIota;

        private string callSign; // I put the call sign here only for pfDXCC types for reference/debugging

        /// <summary>
        /// public properties
        /// </summary>
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
        public string Continent { get => continent; }
        public string TimeZone { get => timeZone; }
        public string Admin2 { get => admin2; }
        public string Name { get => name; }
        public string QTH { get => qth; }
        public string Comment { get => comment; }
        public PrefixKind Kind { get => kind; }
        public string FullPrefix { get => fullPrefix; }
        public string MainPrefix { get => mainPrefix; }
        public string Country { get => country; }
        public string Province { get => province; }
        public string StartDate { get => startDate; }
        public string EndDate { get => endDate; }
        public bool IsIota { get => isIota; }
        public string CallSign { get => callSign; set => callSign = value; }
        public HashSet<string> PrefixKey { get => prefixKey; set => prefixKey = value; }

        private void BuildCallSignInfo(XElement prefixXml)
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
                        fullPrefix = currentValue ?? "";
                        if (currentValue.Contains("."))
                        {
                            // get string after the "."
                            mainPrefix = fullPrefix.Substring(fullPrefix.LastIndexOf('.') + 1);
                        }
                        else
                        {
                            mainPrefix = fullPrefix;
                        }
                        break;
                    case "kind":
                        kind = EnumEx.GetValueFromDescription<PrefixKind>(currentValue);
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
