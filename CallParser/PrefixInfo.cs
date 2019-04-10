using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CallParser
{
    public class PrefixInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal PrefixInfo()
        {
            Children = new List<PrefixInfo>();

            ValidPrefixTypes = new Dictionary<int, PrefixType>();

            ValidPrefixTypes.Add((int)PrefixType.pfDXCC, PrefixType.pfDXCC);
            ValidPrefixTypes.Add((int)PrefixType.pfProvince, PrefixType.pfProvince);
            ValidPrefixTypes.Add((int)PrefixType.pfStation, PrefixType.pfStation);
            ValidPrefixTypes.Add((int)PrefixType.pfNonDXCC, PrefixType.pfNonDXCC);
            ValidPrefixTypes.Add((int)PrefixType.pfCity, PrefixType.pfCity);
        }

        internal PrefixData Data { get; set; }
        internal int Id { get; set; }
        public bool IsParent { get; set; }
        internal List<PrefixInfo> Children { get; set; }
        internal Dictionary<int, PrefixType> ValidPrefixTypes { get; set; }
        internal bool IsValidPefixType { get; set; }

        private PrefixType _PrefixKind;
        internal PrefixType PrefixKind
        {
            get { return _PrefixKind; }
            set 
            {
                _PrefixKind = value;
                if (ValidPrefixTypes.ContainsKey((Int32)value))
                {
                    IsValidPefixType = true;
                }
            }
        }

        /// <summary>
        /// This signifies a parent or a child.
        /// </summary>
        private int _Level;
        internal int Level
        {
            get { return _Level; }
            set { 
                _Level = value;
                if (_Level == 0)
                {
                    IsParent = true;
                }
            }
        }
        internal int Longitude { get; set; }
        internal int Latitude { get; set; }
        internal string Territory { get; set; }
        internal string Prefix { get; set; }
        internal string CQ { get; set; }
        internal string ITU { get; set; }
        internal string Continent { get; set; }
        internal string TZ { get; set; }
        internal Int32 Adif { get; set; }
        internal string Province { get; set; }
        internal string StartDate { get; set; }
        internal string EndDate { get; set; }
        internal string Mask { get; set; }
        internal string Source { get; set; }
        
    } // end class
}
/*
   // 0       1        2           3         4           5
    pfNone, pfDXCC, pfProvince, pfStation, pfDelDXCC, pfOldPrefix,
    //  6            7               8           9
    pfNonDXCC, pfInvalidPrefix, pfDelProvince, pfCity);
 */
//      Data: TPrefixData;
//    Id: integer;
//    Kind: TPrefixKind;
//    Level: integer;
//    Mask: string;
//    Parent: integer;
//    Children: array of integer;   