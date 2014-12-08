using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CallParser
{
    public class Parser
    {
        private string _Callsign;
        internal string Callsign
        {
            get { return _Callsign; }
            set
            {
                _Callsign = value;
                //if (FormatCall(_Callsign) == false)
                //{
                //    // I am going to make this a method call later from the calling program
                //    ResolveCall();
                //}
            }
        }

        private List<PrefixInfo> _Hits;
        public List<PrefixInfo> Hits
        {
            get { return _Hits; }
            set { _Hits = value; }
        }

        private List<CallInfo> _CallList;
        internal List<CallInfo> CallList
        {
            get { return _CallList; }
            set { _CallList = value; }
        }

        /// <summary>
        /// Constructor;
        /// </summary>
        public Parser()
        {
        }

        public List<PrefixInfo> GetCallInformation(string call)
        {
            _Hits = new List<PrefixInfo>();

            _Callsign = call;

            if (FormatCall())
            {
                ResolveCall(call);
            }

            return _Hits;
        }

        /// <summary>
        /// Create an entry point other than the public property. Have it return
        /// a hit list, empty or one or more hits.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private bool FormatCall() // he has this return bool and has global var for call
        {
            Regex regex;
            List<CallInfo> callList = new List<CallInfo>();
            string[] endingPreserver = new string[] { "R", "P", "M" };
            string[] endingIgnore = new string[] { "AM", "MM", "QRP", "A", "B", "BCN", "LH" };
            Int32 adifCode;

            //_Callsign = call;

            // upper case and strip spaces
            _Callsign = _Callsign.ToUpper().Replace(" ", "");

            // ADIF mapping
            // prefixList = prefixList.Where(x => x.IndexOf("#", 0, 1) == -1).ToList();
            //callList = _CallList.Where(x => x.IndexOf(call, 0, 1) != -1).ToList();
            //bool has = _CallList.Any(cus => cus.CallSign == call);
            // see if the call is in the ADIF list in the call.lst file
            if (_CallList.Any(callObj => callObj.CallSign == _Callsign))
            {
                callList = _CallList.Where(t => t.CallSign == _Callsign).ToList();

                if (Int32.TryParse(callList[0].AdifCode, out adifCode))
                {
                    _Callsign = "ADIF" + adifCode;
                    return true;
                }
            }

            if (_Callsign.IndexOf("/") != -1)
            {
                // if /MM return
                if (_Callsign.Substring(_Callsign.IndexOf("/")) == "/MM")
                {
                    return false;
                }
                // same for ANTartica
                if (_Callsign.Substring(_Callsign.IndexOf("/")) == "/ANT")
                {
                    _Callsign = "ADIF013";
                    return true;
                }

                // remove trailing /
                if (_Callsign.IndexOf("/", _Callsign.Length - 1) != -1)
                {
                    _Callsign = _Callsign.Substring(0, _Callsign.Length - 1);
                }
            }

            if (IsValidCall(_Callsign))
            {
                if (_Callsign.IndexOf("/") != -1)
                {
                    // remove known endings that are not prefixes - /MM /QRP etc.
                    regex = new Regex(@"(\/QRP\b|\/MM\b|\/AM\b|\/AA\b|\/A\b|\/BCN\b|\/B\b|\/LH\b|\/ANT\b)");
                    _Callsign = regex.Replace(_Callsign, "");
                }

                if (!CheckExceptions())
                {
                    return false;
                }

                if (!CheckWPXRules())
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool CheckWPXRules()
        {
            return true;
        }

        private bool CheckExceptions()
        {
            return true;
        }

        /// <summary>
        /// Determine if the call sign is in a valid format.
        /// Could probably do this in a regex
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        internal bool IsValidCall(string call)
        {
            bool isValid = true;

            // to short
            if (call.Length < 2)
            {
                isValid = false;
            }

            // can't have / as first character
            if (call.IndexOf("/", 0, 1) == 0)
            {
                isValid = false;
            }

            // can't have // in call
            if (call.IndexOf("//", 0) != -1)
            {
                isValid = false;
            }

            // make sure only alpanumeric and /
            // need testing for "/"
            Regex r = new Regex("^[A-Z0-9/]*$");
            if (!r.IsMatch(call))
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        private void ResolveCall(string call)
        {
            Int32 adf = 0;

            if (call.IndexOf("ADIF", 0, 4) != -1)
            {
                // get all numerics after ADIF
                if (call.Length > 4)
                {
                    // test if numeric here
                    if (Int32.TryParse(call.Substring(4), out adf))
                    {
                        if (adf != 0)
                        {
                            // hit list[0] = GetAdifItem();
                        }
                    }
                }
            }
        }

    } //end class
}
