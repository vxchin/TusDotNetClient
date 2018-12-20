using System;
using System.Collections.Generic;
using System.Threading;

namespace TusDotNetClient
{
    public enum RequestMethod
    {
        Get,
        Post,
        Head,
        Patch,
        Options,
        Delete
    }

    public class TusHttpRequest
    {
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();
        
        public event ProgressDelegate UploadProgressed;

        public event ProgressDelegate DownloadProgressed;

        public Uri Url { get; }
        public string Method { get; }
        public IReadOnlyDictionary<string, string> Headers => _headers;
        public byte[] BodyBytes { get; }

        public CancellationToken CancelToken { get; }

        public TusHttpRequest(
            string url,
            RequestMethod method,
            byte[] bodyBytes = null,
            CancellationToken? cancelToken = null)
        {
            Url = new Uri(url);
            Method = method.ToString().ToUpperInvariant();
            BodyBytes = bodyBytes ?? Array.Empty<byte>();
            CancelToken = cancelToken ?? CancellationToken.None;
            
            _headers.Add(TusHeaderNames.TusResumable, "1.0.0");
        }

        public void AddHeader(string key, string value) => _headers.Add(key, value);

        public void OnUploadProgressed(long bytesTransferred, long bytesTotal) =>
            UploadProgressed?.Invoke(bytesTransferred, bytesTotal);

        public void OnDownloadProgressed(long bytesTransferred, long bytesTotal) =>
            DownloadProgressed?.Invoke(bytesTransferred, bytesTotal);
    }
}