using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallParser
{
    // #InternalUse|Longitude|Latitude|Territory|+Prefix|-CQ|-ITU|-Continent|-TZ|-ADIF|-Province|-StartDate|-EndDate|-Mask|-Source|
    internal class CallInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        internal CallInfo()
        {
        }

        internal string InternalUse { get; set; }

        internal string Longitude { get; set; }

        internal string Latitude { get; set; }

        internal string Territory { get; set; }

        internal string Prefix { get; set; }

        internal string CQ { get; set; }

        internal string ITU { get; set; }

        internal string Continent { get; set; }

        internal string TZ { get; set; }

        internal string ADIF { get; set; }

        internal string Province { get; set; }

        internal string StartDate { get; set; }

        internal string EndDate { get; set; }

        internal string Mask { get; set; }

        internal string Source { get; set; }





















        internal string CallSign { get; set; }

        private string _AdifCode;
        internal string AdifCode
        {
            get { return _AdifCode; }
            set { _AdifCode = value; }
        }




    } // end class
}
