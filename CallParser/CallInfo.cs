using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallParser
{
   internal class CallInfo
    {
        private string _CallSign;
        internal string CallSign
        {
            get { return _CallSign; }
            set { _CallSign = value; }
        }

        private string _AdifCode;
        internal string AdifCode
        {
            get { return _AdifCode; }
            set { _AdifCode = value; }
        }

       /// <summary>
       /// Constructor
       /// </summary>
       internal CallInfo()
       {
       }


    } // end class
}
