using System;
using System.Net.Http;
using System.Web.Mvc;
using SystemWrapper;
using StructureMap.Configuration.DSL;

namespace Habitat.Server.AdminWebConsole
{
    /// <summary>
    /// Class used to establish the bindings used for Dependency Injection for this application
    /// </summary>
    internal class DependencyRegistry : Registry
    {
        public DependencyRegistry(string configServiceUrl)
        {
            For<IControllerActivator>().Use<StructureMapControllerActivator>();
            For<IDateTimeWrap>().Use(() => new DateTimeWrap(new DateTime()));
            For<HttpClient>().Singleton().Use(() =>
                                              {
                                                  // Set up the default client to use NTLM.  This assumes that the admin console application is also secured with NTLM, as it should be.
                                                  var webRequestHandler = new WebRequestHandler();
                                                  webRequestHandler.UseDefaultCredentials = true;
                                                  var client = new HttpClient(webRequestHandler);
                                                  client.BaseAddress = new Uri(configServiceUrl);
                                                  return client;
                                              });
        }
    }
}