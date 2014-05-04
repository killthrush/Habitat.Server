using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Habitat.Core.TestingLibrary
{
    /// <summary>
    /// Fake request handler that intercepts and short-circuits the usual processing of an HTTP request.
    /// </summary>
    public class FakeHttpRequestHandler : DelegatingHandler
    {
        /// <summary>
        /// Gets/sets the fake server response
        /// </summary>
        public HttpResponseMessage Response { get; set; }

        /// <summary>
        /// The number of milliseconds to wait before firing up the response task.
        /// If greater than zero, this can be used to simulate timeout conditions.
        /// </summary>
        public int ResponseTime { get; set; }

        /// <summary>
        /// The number of milliseconds to wait before the fake service response task is terminated
        /// </summary>
        public int TimeoutLimit { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="T:System.Net.Http.DelegatingHandler"/> class.
        /// </summary>
        public FakeHttpRequestHandler()
        {
            ResponseTime = 10; // 10ms response time makes the fake handler behave like a fast service
            TimeoutLimit = 60000; // 1 minute timeout is quite generous
        }

        /// <summary>
        /// Method to accept an HTTP request and asynchronously return a fake response.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>. The task object representing the asynchronous operation.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="request"/> was null.</exception>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage fakeResponse = Response ?? new HttpResponseMessage(HttpStatusCode.NotImplemented);
            fakeResponse.RequestMessage = request;

            // Set up a fake "service" that responds with fake messages.  This
            // "service" can also be configured to take a long time to respond.
            var source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            Task<HttpResponseMessage> task = new Task<HttpResponseMessage>(() =>
                                                            {
                                                                Thread.Sleep(ResponseTime);
                                                                token.ThrowIfCancellationRequested();
                                                                return fakeResponse;
                                                            }, token);

            // A Timeout condition using the HttpClient is actually implemented as a task cancellation.
            // Since we can't override this behavior in the client itself, we'll simulate it using this combination of task and cancellation source.
            // By configuring the "service" to take a long time to respond, we can intentionally cause a race condition and terminate the response task prematurely.
            // This has the effect of making the whole web request appear like a timeout, since the HttpClient links the cancellation token for Timeout with this one.
            // NOTE: this code causes an assertion failure because of a bug in HttpClient.
            // TODO: uncomment the code below when microsoft fix is available.
            task.Start(new LimitedConcurrencyLevelTaskScheduler(2));
            /*if (!task.Wait(TimeoutLimit))
            {
                source.Cancel();
                try
                {
                    task.Wait();
                }
                catch(AggregateException)
                {
                    ;
                }
            }*/

            return task;
        }
    }
}