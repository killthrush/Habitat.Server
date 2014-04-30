using System;
using System.Net.Http;
using SystemWrapper;
using Moq;
using ProTeck.Core.TestingLibrary;
using StructureMap.Configuration.DSL;

namespace Habitat.Server.AdminWebConsole.Tests
{
    /// <summary>
    /// Class used to establish the bindings used for this test fixture's Dependency Injection
    /// </summary>
    internal class MockRegistry : Registry
    {
        public const string NoHttpConnection = "NoHttpConnection";
        public const string BadHttpAddress = "BadHttpAddress";
        public const string ConnectionTimeout = "ConnectionTimeout";
        public const string ServerAlwaysReturns500Error = "ServerAlwaysReturns500Error";
        public const string ServerReturnsGibberish = "ServerReturnsGibberish";
        public const string ServerAlwaysReturnsJsonArray = "ServerAlwaysReturnsJsonArray";
        public const string ServerAlwaysReturnsJsonObject = "ServerAlwaysReturnsJsonObject";

        /// <summary>
        /// Constructs a MockRegistry instance that contains bindings for mock/fake objects
        /// </summary>
        public MockRegistry()
        {
            Mock<IDateTimeWrap> mockDateProvider = new Mock<IDateTimeWrap>(MockBehavior.Strict);
            mockDateProvider.SetupGet(x => x.Now).Returns(new DateTimeWrap(new DateTime(2011, 1, 2)));
            For<IDateTimeWrap>().Use(() => mockDateProvider.Object);
            For<HttpClient>().Use(() => HttpClientTestHelper.CreateStandardFakeClient(new MockConfigService()));

            Profile(NoHttpConnection, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientSimulatingServerWithNoHttpEndpoint));
            Profile(BadHttpAddress, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientSimulatingABadAddress));
            Profile(ConnectionTimeout, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientSimulatingRequestTimeout));
            Profile(ServerAlwaysReturns500Error, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientThatAlwaysThrowsServerError));
            Profile(ServerReturnsGibberish, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientThatAlwaysReturnsGibberish));
            Profile(ServerAlwaysReturnsJsonArray, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientThatAlwaysReturnsJsonArray));
            Profile(ServerAlwaysReturnsJsonObject, x => x.For<HttpClient>().Use(HttpClientTestHelper.CreateClientThatAlwaysReturnsJsonObject));
        }
    }
}