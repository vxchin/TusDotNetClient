using System;
using System.IO;
using System.Net;
using System.Threading;

namespace TusDotNetClient
{
    public class TusHttpClient
    {
        public IWebProxy Proxy { get; set; }


        public TusHttpResponse PerformRequest(TusHttpRequest request)
        {
            try
            {
                var inputStream = new MemoryStream(request.BodyBytes);

                var webRequest = (HttpWebRequest) WebRequest.Create(request.Url);
                webRequest.AutomaticDecompression = DecompressionMethods.GZip;

                webRequest.Timeout = Timeout.Infinite;
                webRequest.ReadWriteTimeout = Timeout.Infinite;
                webRequest.Method = request.Method;
                webRequest.KeepAlive = false;

                webRequest.Proxy = Proxy;

                try
                {
                    var currentServicePoint = webRequest.ServicePoint;
                    currentServicePoint.Expect100Continue = false;
                }
                catch (PlatformNotSupportedException)
                {
                    //expected on .net core 2.0 with systemproxy
                    //fixed by https://github.com/dotnet/corefx/commit/a9e01da6f1b3a0dfbc36d17823a2264e2ec47050
                    //should work in .net core 2.2
                }

                //SEND
                var buffer = new byte[4096];

                var totalBytesWritten = 0L;

                var contentLength = inputStream.Length;
                webRequest.AllowWriteStreamBuffering = false;
                webRequest.ContentLength = inputStream.Length;

                foreach (var header in request.Headers)
                    switch (header.Key)
                    {
                        case TusHeaderNames.ContentLength:
                            webRequest.ContentLength = long.Parse(header.Value);
                            break;
                        case TusHeaderNames.ContentType:
                            webRequest.ContentType = header.Value;
                            break;
                        default:
                            webRequest.Headers.Add(header.Key, header.Value);
                            break;
                    }

                if (request.BodyBytes.Length > 0)
                    using (var requestStream = webRequest.GetRequestStream())
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                        var bytesWritten = inputStream.Read(buffer, 0, buffer.Length);

                        request.OnUploadProgressed(0, contentLength);
                        while (bytesWritten > 0)
                        {
                            totalBytesWritten += bytesWritten;

                            request.OnUploadProgressed(totalBytesWritten, contentLength);

                            requestStream.Write(buffer, 0, bytesWritten);

                            bytesWritten = inputStream.Read(buffer, 0, buffer.Length);

                            request.CancelToken.ThrowIfCancellationRequested();
                        }
                    }

                request.OnDownloadProgressed(0, 0);

                var response = (HttpWebResponse) webRequest.GetResponse();

                //contentLength=0 for gzipped responses due to .net bug
                contentLength = Math.Max(response.ContentLength, 0);

                buffer = new byte[16 * 1024];
                var outputStream = new MemoryStream();

                if (response.GetResponseStream() is Stream responseStream)
                    using (responseStream)
                    {
                        var bytesRead = 0;
                        var totalBytesRead = 0L;

                        bytesRead = responseStream.Read(buffer, 0, buffer.Length);

                        while (bytesRead > 0)
                        {
                            totalBytesRead += bytesRead;

                            request.OnDownloadProgressed(totalBytesRead, contentLength);

                            outputStream.Write(buffer, 0, bytesRead);

                            bytesRead = responseStream.Read(buffer, 0, buffer.Length);

                            request.CancelToken.ThrowIfCancellationRequested();
                        }
                    }

                var resp = new TusHttpResponse(response.StatusCode, outputStream.ToArray());
                foreach (string headerName in response.Headers.Keys)
                    resp.AddHeader(headerName, response.Headers[headerName]);

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