using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TusDotNetClient
{
    /// <summary>
    /// Represents a response with content from a request made to a Tus enabled server.
    /// </summary>
    public class TusHttpResponse : TusHttpResponseBase
    {
        /// <summary>
        /// Get the content of the HTTP response as bytes.
        /// </summary>
        public byte[] ResponseBytes { get; }
        /// <summary>
        /// Get the content of the HTTP response as a <see cref="string"/>.
        /// </summary>
        public string ResponseString => Encoding.UTF8.GetString(ResponseBytes);

        /// <summary>
        /// Create an instance of a <see cref="TusHttpResponse"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="headers">The HTTP headers of the response.</param>
        /// <param name="responseBytes">The content of the response.</param>
        public TusHttpResponse(
            HttpStatusCode statusCode,
            IDictionary<string, string> headers = null,
            byte[] responseBytes = null):base(statusCode, headers)
        {
            ResponseBytes = responseBytes;
        }
    }
}