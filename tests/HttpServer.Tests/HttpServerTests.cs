using System;
using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HttpServer.Tests
{
    [TestClass]
    public class TestHttpServerTests
    {
        [TestMethod]
        public void CanGet404()
        {
            var url = HttpServer.GetLocalhostAddress();
            using (var server = new HttpServer(url,
                new[]
                {
                    new RouteUrl("GET", url, "some html") 
                }
            ))
            {
                using (var httpClient = new HttpClient())
                using (var response = httpClient.GetAsync(new Uri(string.Format("{0}/dave", url))).Result)
                {
                    Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound);
                }
            }
        }

        [TestMethod]
        public void CanPost()
        {
            var url = HttpServer.GetLocalhostAddress();
            using (var server = new HttpServer(url,
                new[]
                {
                    new RouteUrl("POST", url, "some html")
                }
            ))
            {
                Assert.IsTrue(PostContent(new Uri(url), new StringContent("dave")).Contains("some html"));
            }
        }

        [TestMethod]
        public void CanGet()
        {
            var url = HttpServer.GetLocalhostAddress();
            var destinationUrl = $"{url}test/test/test";
            using (var server = new HttpServer(url,
                new[]
                {
                    new RouteUrl("GET", destinationUrl, "some html")
                }
                ))
            {
                Assert.IsTrue(GetContent(new Uri(destinationUrl)).Contains("some html"));
            }
        }

        [TestMethod]
        public void CanGetTwice()
        {
            var url = HttpServer.GetLocalhostAddress();
            var destinationUrl = $"{url}test/test/test";
            using (var server = new HttpServer(url,
                new[]
                {
                    new RouteUrl("GET", destinationUrl, "some html"),
                    new RouteUrl("GET", destinationUrl, "some html")
                }
            ))
            {
                Assert.IsTrue(GetContent(new Uri(destinationUrl)).Contains("some html"));
                Assert.IsTrue(GetContent(new Uri(destinationUrl)).Contains("some html"));
            }
        }

        private string PostContent(Uri page, HttpContent postContent)
        {
            using (var httpClient = new HttpClient())
            using (var response = httpClient.PostAsync(page, postContent).Result)
            using (var content = response.Content)
            {
                return content.ReadAsStringAsync().Result;
            }
        }

        private string GetContent(Uri page)
        {
            using (var httpClient = new HttpClient())
            using (var response = httpClient.GetAsync(page).Result)
            using (var content = response.Content)
            {
                return content.ReadAsStringAsync().Result;
            }
        }
    }

}
