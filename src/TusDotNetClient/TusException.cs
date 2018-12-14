using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TusDotNetClient
{
    public class TusException : WebException
    {

        public string ResponseContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }


        public WebException OriginalException;
        public TusException(TusException ex, string msg)
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {
            OriginalException = ex;


            StatusCode = ex.StatusCode;
            StatusDescription = ex.StatusDescription;
            ResponseContent = ex.ResponseContent;


        }

        public TusException(OperationCanceledException ex)
            : base(ex.Message, ex, WebExceptionStatus.RequestCanceled, null)
        {
            OriginalException = null;           
        }

        public TusException(WebException ex, string msg = "")
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {

            OriginalException = ex;

            if (ex.Response is HttpWebResponse webResponse &&
                webResponse.GetResponseStream() is Stream responseStream)
                using (var reader = new StreamReader(responseStream))
                {
                    StatusCode = webResponse.StatusCode;
                    StatusDescription = webResponse.StatusDescription;

                    var resp = reader.ReadToEnd();

                    ResponseContent = resp;
                }
           

        }

        public string FullMessage
        {
            get
            {
                var bits = new List<string>();
                if (Response != null)
                {
                    bits.Add(string.Format("URL:{0}", Response.ResponseUri));
                }
                bits.Add(Message);
                if (StatusCode != HttpStatusCode.OK)
                {
                    bits.Add(string.Format("{0}:{1}", StatusCode, StatusDescription));
                }
                if (!string.IsNullOrEmpty(ResponseContent))
                {
                    bits.Add(ResponseContent);
                }

                return string.Join(Environment.NewLine, bits.ToArray());
            }
        }

    }
}