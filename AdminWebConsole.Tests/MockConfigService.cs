using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProTeck.Config.Dto.V1;
using ProTeck.Core.TestingLibrary;

namespace Habitat.Server.AdminWebConsole.Tests
{
    /// <summary>
    /// Mock request handler that acts like Config Service
    /// </summary>
    public class MockConfigService : FakeHttpRequestHandler
    {
        private const string JsonMediaType = "application/json";
        private const string ComponentNameMatchPattern = @"http://fake/Config/(\w+)/?$";
        private const string ResourceRequestPattern = @"http://fake/Config/\w+/?$";
        private const string RootRequestPattern = @"http://fake/Config/?$";

        private readonly Dictionary<string, ConfigRoot> _testComponents =
            new Dictionary<string, ConfigRoot>
                {
                    {
                        "bar",
                        new ConfigRoot
                            {
                                ComponentName = "bar",
                                LastModified = new DateTime(2009, 11, 14),
                                Data =
                                    new ConfigNode
                                        {
                                            Name = "bar",
                                            Children =
                                                new List<ConfigNode>
                                                    {
                                                        new ConfigNode {Name = "N1", Value = "V1"},
                                                        new ConfigNode {Name = "N2", Value = "V2"}
                                                    }
                                        }
                            }
                        },
                    {
                        "baz",
                        new ConfigRoot
                            {
                                ComponentName = "baz",
                                LastModified = new DateTime(2009, 11, 14),
                                Data =
                                    new ConfigNode
                                        {
                                            Name = "baz",
                                            Children =
                                                new List<ConfigNode>
                                                    {
                                                        new ConfigNode {Name = "N1", Value = "V1"},
                                                        new ConfigNode {Name = "N2", Value = "V2"}
                                                    }
                                        }
                            }
                        },
                    {
                        "duplicate",
                        new ConfigRoot
                            {
                                ComponentName = "duplicate",
                                LastModified = new DateTime(2009, 11, 14),
                                Data =
                                    new ConfigNode
                                        {
                                            Name = "duplicate",
                                            Children =
                                                new List<ConfigNode>
                                                    {
                                                        new ConfigNode {Name = "N1", Value = "V1"},
                                                        new ConfigNode {Name = "N2", Value = "V2"}
                                                    }
                                        }
                            }
                        },
                    {
                        "foo",
                        new ConfigRoot
                            {
                                ComponentName = "foo",
                                LastModified = new DateTime(2009, 11, 14),
                                Data =
                                    new ConfigNode
                                        {
                                            Name = "foo",
                                            Children =
                                                new List<ConfigNode>
                                                    {
                                                        new ConfigNode {Name = "N1", Value = "V1"},
                                                        new ConfigNode {Name = "N2", Value = "V2"}
                                                    }
                                        }
                            }
                        }
                };

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
            string requestUri = request.RequestUri.OriginalString;

            if (IsRootUrlRequest(requestUri))
            {
                if (request.Method == HttpMethod.Get)
                {
                    Response = CreateGoodResponseForGetComponentList(_testComponents.Keys);
                }
                else if (request.Method == HttpMethod.Post)
                {
                    Response = BuildJsonObjectResponse(request, CreateGoodResponseForAddNewComponent, string.Empty);
                }
            }
            else if (IsResourceUrlRequest(requestUri))
            {
                var match = Regex.Match(requestUri, ComponentNameMatchPattern);
                Group componentNameGroup = match.Groups[1];
                if (!_testComponents.ContainsKey(componentNameGroup.Value))
                {
                    Response = new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                else
                {
                    if (request.Method == HttpMethod.Get)
                    {
                        var root = _testComponents[componentNameGroup.Value];
                        Response = CreateGoodResponseForSaveOrGetComponent(root);
                    }
                    else if (request.Method == HttpMethod.Put)
                    {
                        Response = BuildJsonObjectResponse(request, CreateGoodResponseForSaveOrGetComponent, componentNameGroup.Value);
                    }
                    else if (request.Method == HttpMethod.Delete)
                    {
                        if (_testComponents.ContainsKey(componentNameGroup.Value))
                            _testComponents.Remove(componentNameGroup.Value);

                        Response = new HttpResponseMessage(HttpStatusCode.NoContent);
                    }
                }
            }

            return base.SendAsync(request, cancellationToken);
        }

        public static ConfigRoot GetConfigRoot(string componentName)
        {
            ConfigRoot root = new ConfigRoot();
            root.ComponentName = componentName;
            root.LastModified = new DateTime(2009, 11, 14);
            var node = new ConfigNode();
            node.Name = root.ComponentName;
            node.Children = new List<ConfigNode>
                                {
                                    new ConfigNode {Name = "N1", Value = "V1"},
                                    new ConfigNode {Name = "N2", Value = "V2"}
                                };
            root.Data = node;
            return root;
        }

        private static bool IsResourceUrlRequest(string requestUri)
        {
            return Regex.IsMatch(requestUri, ResourceRequestPattern);
        }

        private static bool IsRootUrlRequest(string requestUri)
        {
            return Regex.IsMatch(requestUri, RootRequestPattern);
        }

        private HttpResponseMessage BuildJsonObjectResponse(HttpRequestMessage request, Func<ConfigRoot, HttpResponseMessage> createGoodResponseMessage, string requestResource)
        {
            ConfigRoot root = null;
            bool badFormat = (request.Content.Headers.ContentType.MediaType != JsonMediaType);
            try
            {
                var readAsStringAsync = request.Content.ReadAsStringAsync();
                readAsStringAsync.Wait();
                root = JsonConvert.DeserializeObject<ConfigRoot>(readAsStringAsync.Result);
                badFormat = badFormat || (Regex.IsMatch(root.ComponentName, @"\W"));
            }
            catch
            {
                badFormat = true;
            }

            if (badFormat)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (requestResource != root.ComponentName && _testComponents.ContainsKey(root.ComponentName))
                return new HttpResponseMessage(HttpStatusCode.Conflict);

            _testComponents[root.ComponentName] = root;

            return createGoodResponseMessage(root);
        }

        private static HttpResponseMessage CreateGoodResponseForSaveOrGetComponent(ConfigRoot configRoot)
        {
            string jsonString = JsonConvert.SerializeObject(configRoot);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(jsonString, Encoding.UTF8, JsonMediaType);
            return response;
        }

        private static HttpResponseMessage CreateGoodResponseForGetComponentList(IEnumerable<string> componentList)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(string.Format(@"[""{0}""]", string.Join(@""",""", componentList)), Encoding.UTF8, JsonMediaType);
            return response;
        }

        private static HttpResponseMessage CreateGoodResponseForAddNewComponent(ConfigRoot root)
        {
            string jsonString = JsonConvert.SerializeObject(root);
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            response.Headers.Add("location", String.Format("http://config/{0}", root.ComponentName));
            response.Content = new StringContent(jsonString, Encoding.UTF8, JsonMediaType);
            return response;
        }
    }
}