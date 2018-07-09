using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    public class HttpServer : IDisposable
    {
        private CancellationToken _cancelToken;

        public HttpServer(string url, IEnumerable<RouteUrl> testRoutes, int maxConcurrentRequests = 10)
        {
            _cancelToken = new CancellationToken();
            Task.Run(() =>
            {
                Listen(url, maxConcurrentRequests, _cancelToken, testRoutes).Wait();
            });
        }

        public static string GetLocalhostAddress()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return $"http://localhost:{port}/";
        }

        public async Task Listen(string prefix, int maxConcurrentRequests, CancellationToken token,
            IEnumerable<RouteUrl> endPoints)
        {
            var routedEndPoints = endPoints.ToArray();

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var requests = new HashSet<Task>();
            for (int i = 0; i < maxConcurrentRequests; i++)
                requests.Add(listener.GetContextAsync());

            while (!token.IsCancellationRequested)
            {
                var t = await Task.WhenAny(requests);
                requests.Remove(t);

                if (t is Task<HttpListenerContext>)
                {
                    var context = (t as Task<HttpListenerContext>).Result;
                    requests.Add(ProcessRequestAsync(context, routedEndPoints));
                    requests.Add(listener.GetContextAsync());
                }
            }
        }

        public async Task ProcessRequestAsync(HttpListenerContext context, IEnumerable<RouteUrl> endPoints)
        {
            var response = context.Response;
            var stream = response.OutputStream;
            var writer = new StreamWriter(stream);

            var endPoint = endPoints.FirstOrDefault(o =>
                o.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase) &&
                o.Url.Equals(context.Request.Url.ToString(), StringComparison.OrdinalIgnoreCase));
            if (endPoint == null)
            {
                response.StatusCode = 404;
            }
            else
            {
                writer.Write(endPoint.Response);
            }

            writer.Close();
        }

        public void Dispose()
        {
            _cancelToken = new CancellationToken(true);
        }
    }

    public class RouteUrl
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public string Response { get; set; }

        public RouteUrl(string method, string url, string response)
        {
            Method = method;
            Url = url;
            Response = response;
        }
    }
}
