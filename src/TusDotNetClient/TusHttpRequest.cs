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
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        
        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);

        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);

        public event DownloadingEvent Downloading;

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
            
            _headers.Add("Tus-Resumable", "1.0.0");
        }

        public void AddHeader(string key, string value) => _headers.Add(key, value);

        public void OnUploading(long bytesTransferred, long bytesTotal) =>
            Uploading?.Invoke(bytesTransferred, bytesTotal);

        public void FireDownloading(long bytesTransferred, long bytesTotal) =>
            Downloading?.Invoke(bytesTransferred, bytesTotal);
    }
}