using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace TusDotNetClient
{
    public class TusHttpClient
    {

        public IWebProxy Proxy { get; set; }
        

        public TusHttpResponse PerformRequest(TusHttpRequest req)
        {

            try
            {
                var instream = new MemoryStream(req.BodyBytes);

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(req.Url);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                
                request.Timeout = Timeout.Infinite;
                request.ReadWriteTimeout = Timeout.Infinite;
                request.Method = req.Method;
                request.KeepAlive = false;

                request.Proxy = Proxy;

                try
                {
                    ServicePoint currentServicePoint = request.ServicePoint;
                    currentServicePoint.Expect100Continue = false;
                }
                catch (PlatformNotSupportedException)
                {
                    //expected on .net core 2.0 with systemproxy
                    //fixed by https://github.com/dotnet/corefx/commit/a9e01da6f1b3a0dfbc36d17823a2264e2ec47050
                    //should work in .net core 2.2
                }


                //SEND
                req.FireUploading(0, 0);
                byte[] buffer = new byte[4096];

                long contentlength = 0;

                int byteswritten = 0;
                long totalbyteswritten = 0;

                contentlength = instream.Length;
                request.AllowWriteStreamBuffering = false;
                request.ContentLength = instream.Length;

                foreach (var header in req.Headers)
                {
                    switch (header.Key)
                    {
                        case "Content-Length":
                            request.ContentLength = long.Parse(header.Value);
                            break;
                        case "Content-Type":
                            request.ContentType = header.Value;
                            break;
                        default:
                            request.Headers.Add(header.Key, header.Value);
                            break;
                    }
                }

                if (req.BodyBytes.Length > 0)
                {
                    using (var requestStream = request.GetRequestStream())
                    {
                        instream.Seek(0, SeekOrigin.Begin);
                        byteswritten = instream.Read(buffer, 0, buffer.Length);

                        while (byteswritten > 0)
                        {
                            totalbyteswritten += byteswritten;

                            req.FireUploading(totalbyteswritten, contentlength);

                            requestStream.Write(buffer, 0, byteswritten);

                            byteswritten = instream.Read(buffer, 0, buffer.Length);

                            req.CancelToken.ThrowIfCancellationRequested();
                        }


                    }
                }

                req.FireDownloading(0, 0);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();


                contentlength = 0;
                contentlength = (long)response.ContentLength;
                //contentlength=0 for gzipped responses due to .net bug

                buffer = new byte[16 * 1024];
                var outstream = new MemoryStream();

                using (Stream responseStream = response.GetResponseStream())
                {
                    int bytesread = 0;
                    long totalbytesread = 0;

                    bytesread = responseStream.Read(buffer, 0, buffer.Length);

                    while (bytesread > 0)
                    {
                        totalbytesread += bytesread;

                        req.FireDownloading(totalbytesread, contentlength);

                        outstream.Write(buffer, 0, bytesread);

                        bytesread = responseStream.Read(buffer, 0, buffer.Length);

                        req.CancelToken.ThrowIfCancellationRequested();
                    }
                }

                TusHttpResponse resp = new TusHttpResponse();
                resp.ResponseBytes = outstream.ToArray();
                resp.StatusCode = response.StatusCode;
                foreach (string headerName in response.Headers.Keys)
                {
                    resp.Headers[headerName] = response.Headers[headerName];
                }

                return resp;

            }
            catch (OperationCanceledException cancelEx)
            {
                TusException rex = new TusException(cancelEx);
                throw rex;
            }
            catch (WebException ex)
            {
                TusException rex = new TusException(ex);
                throw rex;
            }
        }
    }
}


