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
        private PrefixData _Data;
        internal PrefixData Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        private Int32 _Id;
        internal Int32 Id
        {
            get { return _Id; }
            set { _Id = value; }
        }
        

        // #InternalUse|Longitude|Latitude|Territory|+Prefix|-CQ|-ITU|-Continent|-TZ|-ADIF
        //|-Province|-StartDate|-EndDate|-Mask|-Source|
       

        /// <summary>
        /// Indicates this is a parent entity and may have children.
        /// </summary>
        private bool _IsParent;
        public bool IsParent
        {
            get { return _IsParent; }
            set { _IsParent = value; }
        }

        /// <summary>
        /// All of the children of this entry.
        /// </summary>
        private List<PrefixInfo> _Children;
        internal List<PrefixInfo> Children
        {
            get { return _Children; }
            set { _Children = value; }
        }
        /// <summary>
        /// List of valid prefix types to use.
        /// </summary>
        private Dictionary<Int32, PrefixType> _ValidPrefixTypes;
        internal Dictionary<Int32, PrefixType> ValidPrefixTypes
        {
            get { return _ValidPrefixTypes; }
            set { _ValidPrefixTypes = value; }
        }
        
        /// <summary>
        /// Is this a valid prefix type to use for a lookup.
        /// </summary>
        private bool _IsValidPefixType;
        internal bool IsValidPefixType
        {
            get { return _IsValidPefixType; }
            set { _IsValidPefixType = value; }
        }

        private PrefixType _PrefixKind;
        internal PrefixType PrefixKind
        {
            get { return _PrefixKind; }
            set 
            {
                _PrefixKind = value;
                if (_ValidPrefixTypes.ContainsKey((Int32)value))
                {
                    _IsValidPefixType = true;
                }
            }
        }

        /// <summary>
        /// This signifies a parent or a child.
        /// </summary>
        private Int32 _Level;
        internal Int32 Level
        {
            get { return _Level; }
            set { 
                _Level = value;
                if (_Level == 0)
                {
                    _IsParent = true;
                }
            }
        }

        private Int32 _Longitude;
        internal Int32 Longitude
        {
            get { return _Longitude; }
            set { _Longitude = value; }
        }

        private Int32 _Latitude;
        internal Int32 Latitude
        {
            get { return _Latitude; }
            set { _Latitude = value; }
        }

        private string _Territory;
        internal string Territory
        {
            get { return _Territory; }
            set { _Territory = value; }
        }

        private string _Prefix;
        internal string Prefix
        {
            get { return _Prefix; }
            set { _Prefix = value; }
        }

        // can be an array of int
        private string _CQ;
        internal string CQ
        {
            get { return _CQ; }
            set { _CQ = value; }
        }

        // can be an array of int
        private string _ITU;
        internal string ITU
        {
            get { return _ITU; }
            set { _ITU = value; }
        }

        private string _Continent;
        internal string Continent
        {
            get { return _Continent; }
            set { _Continent = value; }
        }

        private string _TZ;
        internal string TZ
        {
            get { return _TZ; }
            set { _TZ = value; }
        }

        private Int32 _Adif;
        internal Int32 Adif
        {
            get { return _Adif; }
            set { _Adif = value; }
        }

        private string _Province;
        internal string Province
        {
            get { return _Province; }
            set { _Province = value; }
        }

        private string _StartDate;
        internal string StartDate
        {
            get { return _StartDate; }
            set { _StartDate = value; }
        }

        private string _EndDate;
        internal string EndDate
        {
            get { return _EndDate; }
            set { _EndDate = value; }
        }

        // [KWN]#,[KWN][ABDEFIJKMNOQ-Z],A[A-GIJK],[KNW]P[06-9],[NW][CG],K[CG][0-35-9],KG4@.,KG4@@@,KC4@.,KC4@@.,KC4[B-TV-Z]@@.,KC4A[B-Z]@,KC4AA[G-Z],KC4U[A-RT-Z]@,K[CG]4#,KC4[B-TV-Z]@@@,KC4AA[A-F]@,KC4US@@
        private string _Mask;
        internal string Mask
        {
            get { return _Mask; }
            set { _Mask = value; }
        }

        private string _Source;
        internal string Source
        {
            get { return _Source; }
            set { _Source = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal PrefixInfo()
        {
            _Children = new List<PrefixInfo>();

            _ValidPrefixTypes = new Dictionary<int, PrefixType>();

            _ValidPrefixTypes.Add((Int32)PrefixType.pfDXCC, PrefixType.pfDXCC);
            _ValidPrefixTypes.Add((Int32)PrefixType.pfProvince, PrefixType.pfProvince);
            _ValidPrefixTypes.Add((Int32)PrefixType.pfStation, PrefixType.pfStation);
            _ValidPrefixTypes.Add((Int32)PrefixType.pfNonDXCC, PrefixType.pfNonDXCC);
            _ValidPrefixTypes.Add((Int32)PrefixType.pfCity, PrefixType.pfCity);
        }

       
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