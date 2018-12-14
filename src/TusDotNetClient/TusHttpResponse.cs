using System.Collections.Generic;
using System.Net;

namespace TusDotNetClient
{
    public class TusHttpResponse
    {
        public byte[] ResponseBytes { get; set; }
        public string ResponseString => System.Text.Encoding.UTF8.GetString(ResponseBytes);
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public TusHttpResponse()
        {
            Headers = new Dictionary<string, string>();
        }

    }
}