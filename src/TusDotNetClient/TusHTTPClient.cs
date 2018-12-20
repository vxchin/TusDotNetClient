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
                var inputStream = new MemoryStream(req.BodyBytes);

                var request = (HttpWebRequest) WebRequest.Create(req.Url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                request.Timeout = Timeout.Infinite;
                request.ReadWriteTimeout = Timeout.Infinite;
                request.Method = req.Method;
                request.KeepAlive = false;

                request.Proxy = Proxy;

                try
                {
                    var currentServicePoint = request.ServicePoint;
                    currentServicePoint.Expect100Continue = false;
                }
                catch (PlatformNotSupportedException)
                {
                    //expected on .net core 2.0 with systemproxy
                    //fixed by https://github.com/dotnet/corefx/commit/a9e01da6f1b3a0dfbc36d17823a2264e2ec47050
                    //should work in .net core 2.2
                }


                //SEND
                req.OnUploading(0, 0);
                var buffer = new byte[4096];

                var totalBytesWritten = 0L;

                var contentLength = inputStream.Length;
                request.AllowWriteStreamBuffering = false;
                request.ContentLength = inputStream.Length;

                foreach (var header in req.Headers)
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

                if (req.BodyBytes.Length > 0)
                    using (var requestStream = request.GetRequestStream())
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                        var bytesWritten = inputStream.Read(buffer, 0, buffer.Length);

                        while (bytesWritten > 0)
                        {
                            totalBytesWritten += bytesWritten;

                            req.OnUploading(totalBytesWritten, contentLength);

                            requestStream.Write(buffer, 0, bytesWritten);

                            bytesWritten = inputStream.Read(buffer, 0, buffer.Length);

                            req.CancelToken.ThrowIfCancellationRequested();
                        }
                    }

                req.FireDownloading(0, 0);

                var response = (HttpWebResponse) request.GetResponse();


                contentLength = 0;
                contentLength = response.ContentLength;
                //contentLength=0 for gzipped responses due to .net bug

                buffer = new byte[16 * 1024];
                var outputStream = new MemoryStream();

                if (response.GetResponseStream() is Stream responseStream)
                    using (responseStream)
                    {
                        var bytesread = 0;
                        var totalbytesread = 0L;

                        bytesread = responseStream.Read(buffer, 0, buffer.Length);

                        while (bytesread > 0)
                        {
                            totalbytesread += bytesread;

                            req.FireDownloading(totalbytesread, contentLength);

                            outputStream.Write(buffer, 0, bytesread);

                            bytesread = responseStream.Read(buffer, 0, buffer.Length);

                            req.CancelToken.ThrowIfCancellationRequested();
                        }
                    }

                var resp = new TusHttpResponse
                {
                    ResponseBytes = outputStream.ToArray(),
                    StatusCode = response.StatusCode
                };
                foreach (string headerName in response.Headers.Keys)
                {
                    resp.Headers[headerName] = response.Headers[headerName];
                }

                return resp;
            }
            catch (OperationCanceledException cancelEx)
            {
                throw new TusException(cancelEx);
            }
            catch (WebException ex)
            {
                throw new TusException(ex);
            }
        }
    }
}