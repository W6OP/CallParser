using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallParser
{
    internal class PrefixData
    {
        // from file
        private Point _Location;
        public Point Location
        {
            get { return _Location; }
            set { _Location = value; }
        }

        private string _Territory;
        public string Territory
        {
            get { return _Territory; }
            set { _Territory = value; }
        }
       
        private string _Prefix;
        public string Prefix
        {
            get { return _Prefix; }
            set { _Prefix = value; }
        }

        private string _CQ;
        public string CQ
        {
            get { return _CQ; }
            set { _CQ = value; }
        }

        private string _ITU;
        public string ITU
        {
            get { return _ITU; }
            set { _ITU = value; }
        }

        private string _Continent;
        public string Continent
        {
            get { return _Continent; }
            set { _Continent = value; }
        }

        private string _TZ;
        public string TZ
        {
            get { return _TZ; }
            set { _TZ = value; }
        }

        private string _ADIF;
        public string ADIF
        {
            get { return _ADIF; }
            set { _ADIF = value; }
        }

        private string _ProvinceCode;
        public string ProvinceCode
        {
            get { return _ProvinceCode; }
            set { _ProvinceCode = value; }
        }

        //inferred
        private string _Province;
        public string Province
        {
            get { return _Province; }
            set { _Province = value; }
        }

        private string _City;
        public string City
        {
            get { return _City; }
            set { _City = value; }
        }

        private List<string> _Attributes;
        public List<string> Attributes
        {
            get { return _Attributes; }
            set { _Attributes = value; }
        }

        internal PrefixData()
        {
        }
        
    } // end class
}
