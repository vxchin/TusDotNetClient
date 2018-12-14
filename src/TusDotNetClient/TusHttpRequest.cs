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

        public string BodyText
        {
            get => System.Text.Encoding.UTF8.GetString(BodyBytes);
            set => BodyBytes = System.Text.Encoding.UTF8.GetBytes(value);
        }
        

        public TusHttpRequest(string u)
        {
            Url = new Uri(u);
            Method = "GET";
            Headers = new Dictionary<string, string>();
            BodyBytes = new byte[0];
        }

        public void AddHeader(string k,string v)
        {
            Headers[k] = v;
        }

        public void FireUploading(long bytesTransferred, long bytesTotal)
        {
            if (Uploading != null)
                Uploading(bytesTransferred, bytesTotal);
        }

        public void FireDownloading(long bytesTransferred, long bytesTotal)
        {
            if (Downloading != null)
                Downloading(bytesTransferred, bytesTotal);
        }

    }
}