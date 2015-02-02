using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;

namespace Helpers
{
    public class RESTApi
    {
        private const string Version = "2011-10-01";
        private XNamespace wa = "http://schemas.microsoft.com/windowsazure";
        private X509Certificate2 Certificate { get; set; }

        public RESTApi(string CertificateName, string CertificatePassword)
        {
            Console.WriteLine("RestAPIHelper CTOR processing.");

            Certificate = GetManagementCertificate(CertificateName, CertificatePassword);
            if (Certificate == null)
                throw new ArgumentException();
        }

        /// <summary>
        /// The operation status values from PollGetOperationStatus.
        /// </summary>
        public enum OperationStatus
        {
            InProgress,
            Failed,
            Succeeded,
            TimedOut
        }

        /// <summary>
        /// A helper function to invoke a Service Management REST API operation.
        /// Throws an ApplicationException on unexpected status code results.
        /// </summary>
        /// <param name="uri">The URI of the operation to invoke using a web request.</param>
        /// <param name="method">The method of the web request, GET, PUT, POST, or DELETE.</param>
        /// <param name="overrideVersion">If the API version number is different from the default.</param>
        /// <param name="expectedCode">The expected status code.</param>
        /// <param name="requestBody">The XML body to send with the web request. Use null to send no request body.</param>
        /// <param name="responseBody">The XML body returned by the request, if any.</param>
        /// <returns>The requestId returned by the operation.</returns>
        public string InvokeRequest(
            Uri uri,
            string method,
            string overrideVersion,
            HttpStatusCode expectedCode,
            XDocument requestBody,
            out XDocument responseBody)
        {
            Console.WriteLine("RestAPI.InvokeRequest processing.");

            responseBody = null;
            string requestId = String.Empty;
 
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = method;

            if (overrideVersion.Length == 0)
                request.Headers.Add("x-ms-Version", Version);
            else
                request.Headers.Add("x-ms-Version", overrideVersion);

            request.ClientCertificates.Add(Certificate);
            request.ContentType = "application/xml";
 
            if (requestBody != null)
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    using (StreamWriter streamWriter = new StreamWriter(
                        requestStream, System.Text.UTF8Encoding.UTF8))
                    {
                        requestBody.Save(streamWriter, SaveOptions.DisableFormatting);
                    }
                }
            }
 
            HttpWebResponse response;
            HttpStatusCode statusCode = HttpStatusCode.Unused;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                // GetResponse throws a WebException for 4XX and 5XX status codes
                response = (HttpWebResponse)ex.Response;
            }
 
            try
            {
                statusCode = response.StatusCode;
                if (response.ContentLength > 0)
                {
                    using (XmlReader reader = XmlReader.Create(response.GetResponseStream()))
                    {
                        responseBody = XDocument.Load(reader);
                    }
                }
 
                if (response.Headers != null)
                {
                    requestId = response.Headers["x-ms-request-id"];
                }
            }
            finally
            {
                response.Close();
            }
 
            if (!statusCode.Equals(expectedCode))
            {
                throw new ApplicationException(string.Format(
                    "Call to {0} returned an error:{1}Status Code: {2} ({3}):{1}{4}",
                    uri.ToString(),
                    Environment.NewLine,
                    (int)statusCode,
                    statusCode,
                    responseBody.ToString(SaveOptions.OmitDuplicateNamespaces)));
            }
 
            return requestId;
        }

        private X509Certificate2 GetManagementCertificate(string CertificateName, string CertificatePassword)
        {
            Storage s = new Storage();
            X509Certificate2 certificate;

            byte[] certBytes = s.GetCertificateBytes(CertificateName);
            certificate = new X509Certificate2(certBytes, CertificatePassword, X509KeyStorageFlags.MachineKeySet);

            return certificate;
        }
    }
}
