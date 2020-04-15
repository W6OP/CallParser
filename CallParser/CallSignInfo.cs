using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public class CallSignInfo
    {
        internal CallSignInfo(System.Xml.Linq.XElement element)
        {
            InitializeCallSignInfo(element);
        }

        public CallSignInfo()
        {
        }

        internal CallSignInfo ShallowCopy()
        {
            return (CallSignInfo)this.MemberwiseClone();
        }

        /// <summary>
        /// public properties
        /// </summary>
        /// 
        private HashSet<List<string[]>> primaryMaskList = new HashSet<List<string[]>>();
        internal Dictionary<string, byte> IndexKey = new Dictionary<string, byte>();
        private HashSet<int> dxccMerged;
        /// <summary>
        /// The rank of the result over other results - used internally.
        /// </summary>
        internal int Rank { get; set; }
        /// <summary>
        /// True indicates this hit is the result of multiple hits being merged into one.
        /// </summary>
        public bool MergedHit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private int dXCC;
        public int GetDXCC()
        {
            return dXCC;
        }

        public void SetDXCC(int value)
        {
            dXCC = value;
            DXCCMerged = new HashSet<int>(value);
        }

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


        /// <summary>
        /// Return the lists where the length of the list matches the count.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal List<List<string[]>> GetPrimaryMaskList(int count)
        {
            var temp = primaryMaskList.Where(x => x.Count == count);
            return temp.ToList();
        }

        internal List<List<string[]>> GetPrimaryMaskList(string first, string second, bool stopFound)
        {
            var temp = new List<List<string[]>>();
            foreach (var item in primaryMaskList)
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

        internal bool MaskListExists(string first, string second, bool stopFound)
        {
            foreach (var item in primaryMaskList)
            {
                if (stopFound)
                {
                    if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1 && Array.IndexOf(item[item.Count - 1], ".") != -1)
                    {
                        return true;
                    }
                }

                if (Array.IndexOf(item[0], first) != -1 && Array.IndexOf(item[1], second) != -1)
                {
                    return true;
                }
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
            primaryMaskList.Add(value);

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

