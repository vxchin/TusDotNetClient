using System.Collections.Generic;
using System.Net;

namespace TusDotNetClient
{
    /// <summary>
    /// Represents a generic response from a request made to a Tus enabled server.
    /// </summary>
    public abstract class TusHttpResponseBase
    {
        /// <summary>
        /// Get the HTTP status code from the Tus server.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Get the HTTP headers from the response.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }
        
        /// <summary>
        /// Create an instance of a <see cref="TusHttpResponse"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="headers">The HTTP headers of the response.</param>
        protected TusHttpResponseBase(
            HttpStatusCode statusCode,
            IDictionary<string, string> headers = null)
        {
            StatusCode = statusCode;
            Headers = headers is null
                ? new Dictionary<string, string>(0)
                : new Dictionary<string, string>(headers);
        }
    }
}