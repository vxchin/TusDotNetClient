using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TusDotNetClient
{
    /// <summary>
    /// Represents a response from a "download-to-file" request made to a Tus enabled server.
    /// </summary>
    public class TusHttpFileResponse : TusHttpResponseBase
    {
        /// <summary>
        /// Get the file contents the content of the HTTP response.
        /// </summary>
        public FileInfo File { get; }

        public TusHttpFileResponse(
            HttpStatusCode statusCode,
            string fileName,
            IDictionary<string, string> headers = null) : base(statusCode, headers)
        {
            File = string.IsNullOrEmpty(fileName) ? null : new FileInfo(fileName);
        }
    }
}