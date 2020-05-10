using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace W6OP.CallParser
{
    public delegate void QRZError(string message);
    public delegate void QRZCallNotFound(string message);

    public class QRZLookup
    {
        public event QRZError OnErrorDetected;
        public event QRZCallNotFound OnCallNotFound;

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
            switch (QRZSessionKey)
            {
                case null:
                    {
                        var requestUri = string.Format("http://xmldata.qrz.com/xml/current/?username={0};password={1};{2}={3}", userId, password, "CallParser", "2.0");
                        var request = WebRequest.Create(requestUri);
                        var response = request.GetResponse();
                        XDocument xDocument = XDocument.Load(response.GetResponseStream());

                        XNamespace xname = "http://xmldata.qrz.com";

                        var error = xDocument.Descendants(xname + "Session").Select(x => x.Element(xname + "Error")).FirstOrDefault();

                        if (error == null)
                        {
                            var key = xDocument.Descendants(xname + "Session").Select(x => x.Element(xname + "Key").Value).FirstOrDefault();
                            QRZSessionKey = key.ToString();
                            return true;
                        }
                        else
                        {
                            QRZSessionKey = null;
                            OnErrorDetected?.Invoke(error.Value);
                        }

                        break;
                    }

                default:
                    return true;
            }

            return false;
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

                if (!CheckForValidData(xDocument))
                {
                    xDocument = null;
                }
            }
            catch (Exception ex)
            {
                OnErrorDetected?.Invoke(ex.Message);
            }

            return xDocument;
        }

        /// <summary>
        /// Check to see if an error, usually "Not Found" was returned.
        /// </summary>
        /// <param name="xDocument"></param>
        /// <returns></returns>
        private bool CheckForValidData(XDocument xDocument)
        {
            XNamespace xname = "http://xmldata.qrz.com";

            var error = xDocument.Descendants(xname + "Session").Select(x => x.Element(xname + "Error")).FirstOrDefault();

            if (error != null)
            {
                return false;
                //QRZCallNotFound?.Invoke(error.Value);
            }

           return true;
        }
    } // end class
}
/*
 <QRZDatabase version="1.33" xmlns="http://xmldata.qrz.com">
  <Session>
    <Error>Not found: TX4YKP</Error>
    <Key>b6f1374dbbd5f848f617d5d75b31baa9</Key>
    <Count>9486210</Count>
    <SubExp>Tue Dec 29 00:00:00 2020</SubExp>
    <GMTime>Sun May 10 14:43:01 2020</GMTime>
    <Remark>cpu: 0.038s</Remark>
  </Session>
</QRZDatabase>
     */
