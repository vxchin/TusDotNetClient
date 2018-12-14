using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TusDotNetClient
{
    public class TusClient
    {
        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);
        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);
        public event DownloadingEvent Downloading;

        private CancellationTokenSource cancelSource = new CancellationTokenSource();
        
        public IWebProxy Proxy { get; set; }

        public void Cancel()
        {
            cancelSource.Cancel();
        }

        public string Create(string url, FileInfo file, Dictionary<string, string> metadata = null)
        {
            if (metadata == null)
            {
                metadata = new Dictionary<string,string>();
            }
            if (!metadata.ContainsKey("filename"))
            {
                metadata["filename"] = file.Name;
            }
            return Create(url, file.Length, metadata);
        }
        public string Create(string url, long uploadLength, Dictionary<string, string> metadata = null)
        {
            var requestUri = new Uri(url);
            var client = new TusHttpClient();
            client.Proxy = Proxy;

            var request = new TusHttpRequest(url);
            request.Method = "POST";
            request.AddHeader("Tus-Resumable", "1.0.0");
            request.AddHeader("Upload-Length", uploadLength.ToString());
            request.AddHeader("Content-Length", "0");

            if (metadata == null)
            {
                metadata = new Dictionary<string,string>();
            }

            var metadatastrings = new List<string>();
            foreach (var meta in metadata)
            {
                string k = meta.Key.Replace(" ", "").Replace(",","");
                string v = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta.Value));
                metadatastrings.Add(string.Format("{0} {1}", k, v ));
            }
            request.AddHeader("Upload-Metadata", string.Join(",",metadatastrings.ToArray()));

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                if (response.Headers.ContainsKey("Location"))
                {
                    Uri locationUri;
                    if (Uri.TryCreate(response.Headers["Location"],UriKind.RelativeOrAbsolute,out locationUri ))
                    {
                        if (!locationUri.IsAbsoluteUri)
                        {
                            locationUri = new Uri(requestUri, locationUri);
                        }
                        return locationUri.ToString();
                    }
                    else
                    {
                        throw new Exception("Invalid Location Header");
                    }

                }
                else
                {
                    throw new Exception("Location Header Missing");
                }
                
            }
            else
            {
                throw new Exception("CreateFileInServer failed. " + response.ResponseString );
            }
        }
        
        public void Upload(string url, FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                Upload(url, fs);
            }

        }
        public void Upload(string url, Stream fs)
        {

            var offset = getFileOffset(url);
            var client = new TusHttpClient();
            System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1Managed();
            int chunkSize = (int) Math.Ceiling(3 * 1024.0 * 1024.0); //3 mb

            if (offset == fs.Length)
            {
                if (Uploading != null)
                    Uploading(fs.Length, fs.Length);
            }


            while (offset < fs.Length)
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    byte[] buffer = new byte[chunkSize];
                    var bytesRead = fs.Read(buffer, 0, chunkSize);

                    Array.Resize(ref buffer, bytesRead);
                    var sha1Hash = sha.ComputeHash(buffer);

                    var request = new TusHttpRequest(url);
                    request.CancelToken = cancelSource.Token;
                    request.Method = "PATCH";
                    request.AddHeader("Tus-Resumable", "1.0.0");
                    request.AddHeader("Upload-Offset", string.Format("{0}", offset));
                    request.AddHeader("Upload-Checksum", "sha1 " + Convert.ToBase64String(sha1Hash));
                    request.AddHeader("Content-Type", "application/offset+octet-stream");
                    request.BodyBytes = buffer;

                    request.Uploading += delegate(long bytesTransferred, long bytesTotal)
                    {
                        if (Uploading != null)
                            Uploading(offset + bytesTransferred, fs.Length);
                    };

                    try
                    {
                        var response = client.PerformRequest(request);

                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            offset += bytesRead;
                        }
                        else
                        {
                            throw new Exception("WriteFileInServer failed. " + response.ResponseString);
                        }
                    }
                    catch (IOException ex)
                    {
                        if (ex.InnerException is SocketException socketException)
                        {
                            if (socketException.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                // retry by continuing the while loop but get new offset from server to prevent Conflict error
                                offset = getFileOffset(url);
                            }
                            else
                            {
                                throw socketException;
                            }                            
                        }
                        else
                        {
                            throw;
                        }                        
                    }



                }
            
        }
        
        public TusHttpResponse Download(string url)
        {
            var client = new TusHttpClient();

            var request = new TusHttpRequest(url);
            request.CancelToken = cancelSource.Token;
            request.Method = "GET";

            request.Downloading += delegate(long bytesTransferred, long bytesTotal)
            {
                if (Downloading != null)
                    Downloading(bytesTransferred, bytesTotal);
            };

            var response = client.PerformRequest(request);

            return response;
        }
        
        public TusHttpResponse Head(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url);
            request.Method = "HEAD";
            request.AddHeader("Tus-Resumable", "1.0.0");

            try
            {
                var response = client.PerformRequest(request);
                return response;
            }
            catch (TusException ex)
            {
                var response = new TusHttpResponse();
                response.StatusCode = ex.StatusCode;
                return response;
            }
        }
        
        public class TusServerInfo
        {
            public string Version = "";
            public string SupportedVersions = "";
            public string Extensions = "";
            public long MaxSize = 0;
            
            public bool SupportsDelete => Extensions.Contains("termination");
        }

        public TusServerInfo GetServerInfo(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url);
            request.Method = "OPTIONS";

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                // Spec says NoContent but tusd gives OK because of browser bugs
                var info = new TusServerInfo();
                response.Headers.TryGetValue("Tus-Resumable", out info.Version);
                response.Headers.TryGetValue("Tus-Version", out info.SupportedVersions);
                response.Headers.TryGetValue("Tus-Extension", out info.Extensions);

                string MaxSize;
                if (response.Headers.TryGetValue("Tus-Max-Size", out MaxSize))
                {
                    info.MaxSize = long.Parse(MaxSize);
                }
                else
                {
                    info.MaxSize = 0;
                }

                return info;
            }
            else
            {
                throw new Exception("getServerInfo failed. " + response.ResponseString);
            }
        }
        
        public bool Delete(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url);
            request.Method = "DELETE";
            request.AddHeader("Tus-Resumable", "1.0.0");

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private long getFileOffset(string url)
        {
            var client = new TusHttpClient();
            var request = new TusHttpRequest(url);
            request.Method = "HEAD";
            request.AddHeader("Tus-Resumable", "1.0.0");

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Headers.ContainsKey("Upload-Offset"))
                {
                    return long.Parse(response.Headers["Upload-Offset"]);
                }
                else
                {
                    throw new Exception("Offset Header Missing");
                }
            }
            else
            {
                throw new Exception("getFileOffset failed. " + response.ResponseString);
            }
        }
    }
}
