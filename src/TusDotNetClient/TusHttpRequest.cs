using System;
using System.Collections.Generic;
using System.Threading;

namespace TusDotNetClient
{
    public class TusHttpRequest
    {
        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);
        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);
        public event DownloadingEvent Downloading;

        public Uri Url { get; set; }
        public string Method { get; set; }
        public Dictionary<string,string> Headers { get; set; }
        public byte[] BodyBytes { get; set; }

        public CancellationToken CancelToken;

        public TusHttpRequest(string url)
        {
            Url = new Uri(url);
            Method = "GET";
            Headers = new Dictionary<string, string>();
            BodyBytes = new byte[0];
        }

        public void AddHeader(string k,string v) => Headers[k] = v;

        public void OnUploading(long bytesTransferred, long bytesTotal) => 
            Uploading?.Invoke(bytesTransferred, bytesTotal);

        public void FireDownloading(long bytesTransferred, long bytesTotal) => 
            Downloading?.Invoke(bytesTransferred, bytesTotal);
    }
}