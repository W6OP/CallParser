using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public delegate void QRZError(string message);

    public class QRZLookup
    {
        public event QRZError OnErrorDetected;
        private string QRZSessionKey;

        public QRZLookup()
        {

        }

        /// <summary>
        /// Logon to QRZ.com - TODO: Add credentials to Config screen
        /// Save the session key.
        /// </summary>
        /// <returns></returns>
        internal bool QRZLogon(string userId, string password)
        {
            bool isLoggedOn = false;

            var requestUri = string.Format("http://xmldata.qrz.com/xml/current/?username={0};password={1};{2}={3}", userId, password, "DXA", "2.0");
            var request = WebRequest.Create(requestUri);
            var response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            XNamespace xname = "http://xmldata.qrz.com";

            var error = xdoc.Descendants(xname + "Session").Select(x => x.Element(xname + "Error")).FirstOrDefault();

            if (error == null)
            {
                var key = xdoc.Descendants(xname + "Session").Select(x => x.Element(xname + "Key").Value).FirstOrDefault();
                QRZSessionKey = key.ToString();
                return true;
            }
            else
            {
                OnErrorDetected?.Invoke(error.Value);
            }

            return isLoggedOn;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="prefixInfo"></param>
        /// <returns></returns>
        internal XDocument QRZRequest(string call)
        {
            WebResponse webResponse;
            WebRequest webRequest;
            XDocument xDocument = new XDocument();
            string requestUri = null;

            try
            {
                if (QRZSessionKey == null)
                {
                    OnErrorDetected?.Invoke("Session key is missing.");
                }

                requestUri = string.Format("http://xmldata.qrz.com/xml/current/?s={0};callsign={1}", QRZSessionKey, call);
                webRequest = WebRequest.Create(requestUri);

                webResponse = webRequest.GetResponse();
                xDocument = XDocument.Load(webResponse.GetResponseStream());
            }
            catch (Exception ex)
            {
                OnErrorDetected?.Invoke(ex.Message);
            }

            return xDocument;
        }

    } // end class
}
