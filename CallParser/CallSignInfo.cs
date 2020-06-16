using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace W6OP.CallParser
{
    [Serializable]
    public class CallSignInfo
    {
        internal XElement PrefixXml { get; set; }

        public bool IsQRZInformation { get; set; }

        public CallSignInfo()
        {
            CallSignFlags = new HashSet<CallSignFlags>();
        }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="element"></param>
        internal CallSignInfo(XElement element)
        {
            PrefixXml = element;
            InitializeCallSignInfo();
            CallSignFlags = new HashSet<CallSignFlags>();
        }

        /// <summary>
        /// Constructor used for QRZ.com
        /// </summary>
        /// <param name="xDocument"></param>
        internal CallSignInfo(XDocument xDocument)
        {
            IsQRZInformation = true;
            InitializeCallSignInfo(xDocument);
            CallSignFlags = new HashSet<CallSignFlags>();
        }

        internal CallSignInfo ShallowCopy()
        {
            return (CallSignInfo)this.MemberwiseClone();
        }

        /// <summary>
        /// Deep clone of this object.
        /// The first time trough it will throw a file not found exception.
        /// This is a known problem Microsoft won't fix. The XmlSerializer
        /// constructor handles this error.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object2Copy"></param>
        /// <param name="objectCopy"></param>
        public void DeepCopy<T>(ref T object2Copy, ref T objectCopy)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(T));

                serializer.Serialize(stream, object2Copy);
                stream.Position = 0;
                objectCopy = (T)serializer.Deserialize(stream);
            }
        }

        /// <summary>
        /// public properties
        /// </summary>
        /// 
        private HashSet<List<string[]>> maskList = new HashSet<List<string[]>>();
        internal Dictionary<string, byte> IndexKey = new Dictionary<string, byte>();

        /// <summary>
        /// The rank of the result over other results - used internally.
        /// </summary>
        internal int Rank { get; set; }
        /// <summary>
        /// True indicates this hit is the result of multiple hits being merged into one.
        /// </summary>
        public bool IsMergedHit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int DXCC;

        public void SetDXCC(int value)
        {
            DXCC = value;
            DXCCMerged = new HashSet<int>(value);
        }

        private HashSet<int> dxccMerged;
        public HashSet<int> DXCCMerged { get => dxccMerged; set => dxccMerged = value; }

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
        public HashSet<CallSignFlags> CallSignFlags { get; set; }

        #region QRZ Fields

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string County { get; set; }
        public string Grid { get; set; }
        public bool LotW { get; set; }


        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="stopFound"></param>
        /// <returns></returns>
        internal List<List<string[]>> GetMaskList(string first, string second, bool stopFound)
        {
            var temp = new List<List<string[]>>();
            foreach (var item in maskList)
            {
                if (stopFound)
                {
                    if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[item.Count - 1], ".") != -1)
                    {
                        temp.Add(item);
                    }
                }
                else
                {
                    if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[item.Count - 1], ".") == -1)
                    {
                        temp.Add(item);
                    }
                }
            }

            return temp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal bool MaskExists(string call, int length)
        {
            call = call.Substring(0, length);
            string first = call.Substring(0, 1);
            string second = call.Substring(1, 1);
            string third;
            string fourth;
            string fifth;
            string sixth;
            string seventh;

            foreach (var item in maskList)
            {
                var searchlength = call.Length < item.Count ? call.Length : item.Count;

                try
                {
                    switch (searchlength)
                    {
                        case 2:
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1)
                            {
                                if (item.Last()[0] != "/")
                                {
                                    return true;
                                }
                            }
                            break;
                        case 3:
                            third = call.Substring(2, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1)
                            {
                                if (item.Last()[0] != "/")
                                {
                                    return true;
                                }
                            }
                            break;
                        case 4:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                                && Array.IndexOf(item[3], fourth) != -1)
                            {
                                if (item.Last()[0] != "/")
                                {
                                    return true;
                                }
                            }
                            break;
                        case 5:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            fifth = call.Substring(4, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                                && Array.IndexOf(item[3], fourth) != -1 && Array.IndexOf(item[4], fifth) != -1)
                            {
                                if (item.Last()[0] != "/")
                                {
                                    return true;
                                }
                            }
                            break;
                        case 6:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            fifth = call.Substring(4, 1);
                            sixth = call.Substring(5, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                               && Array.IndexOf(item[3], fourth) != -1 && Array.IndexOf(item[4], fifth) != -1 && Array.IndexOf(item[5], sixth) != -1)
                            {
                                if (item.Last()[0] != "/")
                                {
                                    return true;
                                }
                            }
                            break;
                        case 7:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            fifth = call.Substring(4, 1);
                            sixth = call.Substring(5, 1);
                            seventh = call.Substring(6, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                               && Array.IndexOf(item[3], fourth) != -1 && Array.IndexOf(item[4], fifth) != -1 && Array.IndexOf(item[5], sixth) != -1
                               && Array.IndexOf(item[6], seventh) != -1)
                            {
                                if (item.Last()[0] != "/")
                                {
                                    return true;
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    var a = 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a portable mask exists.
        /// TODO: This appears to be almost the same as MaskExists() - are they duplicating function
        /// </summary>
        /// <param name="call"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal bool PortableMaskExists(string call)
        {
            string first = call.Substring(0, 1);
            string second = call.Substring(1, 1);
            string third;
            string fourth;
            string fifth;
            string sixth;

            try
            {
                foreach (var item in maskList.Where(x => x.Count == call.Length))
                {
                    switch (call.Length)
                    {
                        case 2:
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1)
                            {
                                return true;
                            }
                            break;
                        case 3:
                            third = call.Substring(2, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1)
                            {
                                return true;
                            }
                            break;
                        case 4:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                                && Array.IndexOf(item[3], fourth) != -1)
                            {
                                return true;
                            }
                            break;
                        case 5:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            fifth = call.Substring(4, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                                && Array.IndexOf(item[3], fourth) != -1 && Array.IndexOf(item[4], fifth) != -1)
                            {
                                return true;
                            }
                            break;
                        case 6:
                            third = call.Substring(2, 1);
                            fourth = call.Substring(3, 1);
                            fifth = call.Substring(4, 1);
                            sixth = call.Substring(5, 1);
                            if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[2], third) != -1
                               && Array.IndexOf(item[3], fourth) != -1 && Array.IndexOf(item[4], fifth) != -1 && Array.IndexOf(item[5], sixth) != -1)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                var a = 1;
            }

            return false;
        }


        /// <summary>
        /// The index key is a character that can be the first letter of a call.
        /// This way I can search faster.
        /// </summary>
        /// <param name="value"></param>
        internal void SetPrimaryMaskList(List<string[]> value)
        {
            maskList.Add(value);

            foreach (var first in value[0])
            {
                if (!IndexKey.ContainsKey(first))
                {
                    IndexKey.Add(first, new byte() { });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefixXml"></param>
        private void InitializeCallSignInfo()
        {
            string currentValue;

            foreach (XElement element in PrefixXml.Elements())
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
                        SetDXCC(Convert.ToInt32(currentValue));
                        break;
                    case "cq_zone":
                        CQ = BuildZoneList(currentValue);
                        break;
                    case "itu_zone":
                        ITU = BuildZoneList(currentValue);
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

        /// <summary>
        /// Parse the QRZ.com response
        /// </summary>
        /// <param name="xDocument"></param>
        private void InitializeCallSignInfo(XDocument xDocument)
        {
            XNamespace xName = "http://xmldata.qrz.com";

            var error = xDocument.Descendants(xName + "Session").Select(x => x.Element(xName + "Error")).FirstOrDefault();

            if (error == null)
            {
                var key = xDocument.Descendants(xName + "Session").Select(x => x.Element(xName + "Key").Value).FirstOrDefault();

                if (key != null)
                {
                    CallSign = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "call")).FirstOrDefault() ?? "";
                    FirstName = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "fname")).FirstOrDefault() ?? "";
                    LastName = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "name")).FirstOrDefault() ?? "";
                    DXCC = (int?)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "dxcc")).FirstOrDefault() ?? 0;
                    Latitude = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "lat")).FirstOrDefault() ?? ""; 
                    Longitude = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "lon")).FirstOrDefault() ?? "";
                    Grid = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "grid")).FirstOrDefault() ?? "";
                    Country = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "country")).FirstOrDefault() ?? "";
                    Province = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "state")).FirstOrDefault() ?? "";
                    County = (string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "county")).FirstOrDefault() ?? "";
                    CQ = BuildZoneList((string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "cqzone")).FirstOrDefault()) ?? new HashSet<int>();
                    ITU = BuildZoneList((string)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "ituzone")).FirstOrDefault()) ?? new HashSet<int>();
                    LotW = (bool?)xDocument.Descendants(xName + "Callsign").Select(x => x.Element(xName + "lotw")).FirstOrDefault() ?? false;
                    Kind = PrefixKind.DXCC;
                }
            }
        }


        /// <summary>
        /// Build a list of CQ or ITU Zones.
        /// </summary>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        private HashSet<int> BuildZoneList(string currentValue)
        {
            var zones = new List<int>();

            zones = currentValue.Split(',').Select(Int32.Parse).ToList();

            return zones.ToHashSet();
        }

    } // end class
}

/*
 <QRZDatabase version="1.33" xmlns="http://xmldata.qrz.com">
  <Callsign>
    <call>W6OP</call>
    <aliases>WA6YUL</aliases>
    <dxcc>291</dxcc>
    <fname>Peter H</fname>
    <name>Bourget</name>
    <addr1>3422 Five Mile Dr</addr1>
    <addr2>Stockton</addr2>
    <state>CA</state>
    <zip>95219</zip>
    <country>United States</country>
    <lat>38.010872</lat>
    <lon>-121.355854</lon>
    <grid>CM98ha</grid>
    <county>San Joaquin</county>
    <ccode>271</ccode>
    <fips>06077</fips>
    <land>United States</land>
    <efdate>2015-03-14</efdate>
    <expdate>2025-05-20</expdate>
    <class>E</class>
    <codes>HVIE</codes>
    <qslmgr>DIRECT: SAE OR LOTW OR BUREAU</qslmgr>
    <email>pbourget@w6op.com</email>
    <u_views>8627</u_views>
    <bio>1800</bio>
    <biodate>2015-07-16 00:32:36</biodate>
    <image>https://s3.amazonaws.com/files.qrz.com/p/w6op/w6op.jpg</image>
    <imageinfo>300:400:48591</imageinfo>
    <moddate>2019-04-17 18:15:56</moddate>
    <MSA>8120</MSA>
    <AreaCode>209</AreaCode>
    <TimeZone>Pacific</TimeZone>
    <GMTOffset>-8</GMTOffset>
    <DST>Y</DST>
    <eqsl>0</eqsl>
    <mqsl>1</mqsl>
    <cqzone>3</cqzone>
    <ituzone>6</ituzone>
    <lotw>1</lotw>
    <user>W6OP</user>
    <geoloc>user</geoloc>
  </Callsign>
  <Session>
    <Key>fb2e6a25bbcbd20474b0a5415b7995b8</Key>
    <Count>9486140</Count>
    <SubExp>Tue Dec 29 00:00:00 2020</SubExp>
    <GMTime>Tue May  5 19:02:47 2020</GMTime>
    <Remark>cpu: 0.043s</Remark>
  </Session>
</QRZDatabase> 
 * */
