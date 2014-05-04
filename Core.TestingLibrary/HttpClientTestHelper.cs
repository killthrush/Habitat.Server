using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Habitat.Core.TestingLibrary
{
    /// <summary>
    /// Class that provides useful HttpClient implementations for unit tests
    /// </summary>
    public static class HttpClientTestHelper
    {
        /// <summary>
        /// Creates an HttpClient that is contrived to cause timeouts.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientSimulatingRequestTimeout()
        {
            // Because of a Microsoft issue in HttpClient that causes assertion failures, we have to cause a timeout by hitting a real website.
            // TODO: when Microsoft fix is available, uncomment the code below and use it instead.

            /*var fakeHttpHandler = new FakeConfigService
            {
                ResponseTime = 5000,
                TimeoutLimit = 100
            };
            HttpClient fakeClient = new HttpClient(fakeHttpHandler);
            fakeClient.BaseAddress = new Uri("http://fake");
            return fakeClient;*/

            HttpClient fakeClient = new HttpClient();
            fakeClient.BaseAddress = new Uri("http://www.mit.edu");
            fakeClient.Timeout = new TimeSpan(0, 0, 0, 0, 5);
            return fakeClient;
        }

        /// <summary>
        /// Creates an HttpClient that is contrived to always throw HTTP 500 errors.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientThatAlwaysThrowsServerError()
        {
            return CreateStandardFakeClient(new FakeHttpRequestHandler
            {
                Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            });
        }

        /// <summary>
        /// Creates an HttpClient that is contrived to always return a string result that can't be parsed as JSON, XML, or HTML.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientThatAlwaysReturnsGibberish()
        {
            return CreateStandardFakeClient(new FakeHttpRequestHandler
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ashgdfiuhsadfuh? aiusdfhiuosah aiusdfsabsyu!")
                }
            });
        }

        /// <summary>
        /// Creates an HttpClient that is contrived to always return null content.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientThatAlwaysReturnsNull()
        {
            return CreateStandardFakeClient(new FakeHttpRequestHandler
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = null
                }
            });
        }

        /// <summary>
        /// Creates an HttpClient that is contrived to always return a JSON array.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientThatAlwaysReturnsJsonArray()
        {
            return CreateStandardFakeClient(new FakeHttpRequestHandler
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"[""test"",""test1""]", Encoding.UTF8, "application/json")
                }
            });
        }

        /// <summary>
        /// Creates an HttpClient that is contrived to always return a JSON object.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientThatAlwaysReturnsJsonObject()
        {
            return CreateStandardFakeClient(new FakeHttpRequestHandler
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{""test"":""test1""}", Encoding.UTF8, "application/json")
                }
            });
        }

        /// <summary>
        /// Creates an HttpClient that uses the provided fake implementation to build responses.
        /// </summary>
        /// <param name="handler">The fake handler to use.  This could simulate a single response, or even simulate an entire service</param>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateStandardFakeClient(FakeHttpRequestHandler handler)
        {
            HttpClient fakeClient = new HttpClient(handler);
            fakeClient.BaseAddress = new Uri("http://fake");
            return fakeClient;
        }

        /// <summary>
        /// Creates an HttpClient that can find a server (valid address), but the server does not understand HTTP.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientSimulatingServerWithNoHttpEndpoint()
        {
            HttpClient fakeClient = new HttpClient();
            fakeClient.BaseAddress = new Uri("http://brco-sql.protk.com");  //TODO: need a different address here
            return fakeClient;
        }

        /// <summary>
        /// Creates an HttpClient that is contrived to throw bad (unreachable) address errors.
        /// </summary>
        /// <returns>The HttpClient implementation</returns>
        public static HttpClient CreateClientSimulatingABadAddress()
        {
            HttpClient fakeClient = new HttpClient();
            fakeClient.BaseAddress = new Uri("http://fake");
            return fakeClient;
        }
    }
}
