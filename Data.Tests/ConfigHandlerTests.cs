using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using OpenRasta.Hosting.InMemory;
using OpenRasta.Web;
using ProTeck.Config.Dto.V1;
using ProTeck.Core.Facades;
using ProTeck.Core.Log;
using ProTeck.Core.Repository;
using ProTeck.Core.TestingLibrary;
using StructureMap;

namespace Habitat.Server.Data.Tests
{
    [TestClass]
    public class ConfigHandlerTests
    {
        #region Fields

        private InMemoryHost _host;
        private Mock<IFileSystemFacade> _mockFileSystem;
        private IRepository<IJsonEntity<ConfigRoot>> _repository;

        private const string DataPath = @"C:\protk\data\configservice";

        #endregion Fields

        #region Setup / Teardown

        [TestInitialize]
        public void TestInitialize()
        {
            _mockFileSystem = (new MockFileSystemProvider()).MockFileSystem;
            _repository = new DurableMemoryRepository<ConfigRoot>(DataPath, _mockFileSystem.Object);

            var container = new Container();
            container.Configure(x =>
            {
                x.For<IRepository<IJsonEntity<ConfigRoot>>>().Singleton().Use(_repository);
                x.For<ILog>().Singleton().Use(l => new NoOpLog());
            });

            _host = new InMemoryHost(new Configuration(container));
        }

        #endregion Setup / Teardown

        #region General tests

        [TestMethod]
        public void Missing_registration_throws_exception()
        {
            var container = new Container();
            _host = new InMemoryHost(new Configuration(container));

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "GET",
                                       Uri = new Uri("http://localhost/Config/tacos")
                                   };

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(500, response.StatusCode);
        }

        #endregion General tests


        #region GET Tests
        [TestMethod]
        public void Get_component_list_returns_list()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
                                  {
                                      ComponentName = "tacos",
                                      Data = new ConfigNode {Name = "host", Value = "tortilla"}
                                  };
            _repository.Add(entity);
            _repository.Save();

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "GET",
                                       Uri = new Uri("http://localhost/Config")
                                   };

            var response = _host.ProcessRequest(request);
            var data = GetStringListFromResponseStream(response.Entity.Stream);

            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("tacos", data.ToArray()[0]);
        }

        [TestMethod]
        public void Get_returns_valid_config_root()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
                                  {
                                      ComponentName = "tacos",
                                      Data = new ConfigNode {Name = "host", Value = "tortilla"}
                                  };
            _repository.Add(entity);
            _repository.Save();

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "GET",
                                       Uri = new Uri("http://localhost/Config/tacos")
                                   };

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(200, response.StatusCode);

            var configObj = GetConfigRootFromResponseStream(response.Entity.Stream);

            Assert.IsNotNull(configObj);
            Assert.AreEqual("tortilla", configObj.Data.Value);
        }

        [TestMethod]
        public void Get_on_empty_repo_returns_404()
        {
            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "GET",
                                       Uri = new Uri("http://localhost/Config/tacos")
                                   };

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(404, response.StatusCode);
        }

        [TestMethod]
        public void Get_returns_regardless_of_casing()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
                                  {
                                      ComponentName = "taCos",
                                      Data = new ConfigNode {Name = "host", Value = "tortilla"}
                                  };
            _repository.Add(entity);
            _repository.Save();

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "GET",
                                       Uri = new Uri("http://localhost/Config/tacos")
                                   };

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(200, response.StatusCode);

            var configObj = GetConfigRootFromResponseStream(response.Entity.Stream);

            Assert.IsNotNull(configObj);
            Assert.AreEqual("tortilla", configObj.Data.Value);
        }

        #endregion GET Tests

        #region POST Tests

        [TestMethod]
        public void Post_to_existing_component_config_returns_400()
        {
            // add tabouli to repo
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
                                  {
                                      ComponentName = "tabouli",
                                      Data = new ConfigNode {Name = "host", Value = "tortilla"}
                                  };
            _repository.Add(entity);
            _repository.Save();

            // create new tabouli config to POST
            var configRoot = new ConfigRoot
                                 {
                                     ComponentName = "tabouli",
                                     Data = new ConfigNode
                                                {
                                                    Name = "tabouli",
                                                    Value = "tomatoes",
                                                    Children =
                                                        new List<ConfigNode>
                                                            {new ConfigNode {Name = "Side", Value = "Rice"}}
                                                }
                                 };

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "POST",
                                       Uri = new Uri("http://localhost/Config")
                                   };

            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public void Post_adds_config_root_to_repository()
        {
            var configRoot = new ConfigRoot
            {
                ComponentName = "tabouli",
                Data = new ConfigNode
                {
                    Name = "tabouli",
                    Value = "tomatoes",
                    Children =
                        new List<ConfigNode> { new ConfigNode { Name = "Side", Value = "Rice" } }
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "POST",
                Uri = new Uri("http://localhost/Config")
            };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(201, response.StatusCode);

            var configObj = GetConfigRootFromResponseStream(response.Entity.Stream);

            Assert.IsNotNull(configObj);
            Assert.AreEqual("tabouli", configObj.ComponentName);
            Assert.AreEqual("tomatoes", configObj.Data.Value);
            Assert.AreEqual("Rice", configObj.Data.Children[0].Value);
        }


        [TestMethod]
        public void Post_returns_415_for_invalid_content_type()
        {
            IRequest request = new InMemoryRequest
            {
                HttpMethod = "POST",
                Uri = new Uri("http://localhost/Config")
            };
            request.Headers["Content-Type"] = "text/plain";

            using (var sw = new StreamWriter(request.Entity.Stream))
            {
                sw.Write("The cat ran down the road");
                sw.Flush();
            }
            request.Entity.Stream.Position = 0;

            request.Headers["Content-Length"] = request.Entity.Stream.Length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(415, response.StatusCode);
        }

        [TestMethod]
        public void Post_returns_400_if_component_name_is_null()
        {
            var configRoot = new ConfigRoot
            {
                ComponentName = null,
                Data = new ConfigNode
                {
                    Name = string.Empty,
                    Value = string.Empty
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "POST",
                Uri = new Uri("http://localhost/Config")
            };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public void Post_returns_400_if_component_name_contains_invalid_characters()
        {
            var configRoot = new ConfigRoot
            {
                ComponentName = "jsnsadnc sd 8d8*** sad8s7sagd ^Q^ G5asds",
                Data = new ConfigNode
                {
                    Name = string.Empty,
                    Value = string.Empty
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "POST",
                Uri = new Uri("http://localhost/Config")
            };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        #endregion POST Tests

        #region PUT Tests

        [TestMethod]
        public void Put_updates_config_root_in_repository()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
                                  {
                                      ComponentName = "taboUli",
                                      Data = new ConfigNode {Name = "tabouli", Value = "salad"}
                                  };
            _repository.Add(entity);
            _repository.Save();

            var configRoot = new ConfigRoot
                                 {
                                     ComponentName = "tabouLi",
                                     Data = new ConfigNode
                                                {
                                                    Name = "tabouli",
                                                    Value = "tomatoes",
                                                    Children =
                                                        new List<ConfigNode>
                                                            {new ConfigNode {Name = "Side", Value = "Rice"}}
                                                }
                                 };

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "PUT",
                                       Uri = new Uri("http://localhost/Config/tabouli")
                                   };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(200, response.StatusCode);

            var configObj = GetConfigRootFromResponseStream(response.Entity.Stream);

            Assert.IsNotNull(configObj);
            Assert.AreEqual("tabouli", configObj.ComponentName.ToLower());
            Assert.AreEqual("tomatoes", configObj.Data.Value);
            Assert.AreEqual("Rice", configObj.Data.Children[0].Value);
        }

        [TestMethod]
        public void If_an_attempt_is_made_to_change_a_name_using_put_make_sure_it_doesnt_overwrite_something_else()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
                                  {
                                      ComponentName = "taBouli",
                                      Data = new ConfigNode {Name = "tabouli", Value = "salad"}
                                  };
            _repository.Add(entity);

            var anotherEntity = _repository.Create();
            anotherEntity.Contents = new ConfigRoot
                                         {
                                             ComponentName = "Grapes",
                                             Data = new ConfigNode {Name = "grapes", Value = "wrath"}
                                         };
            _repository.Add(anotherEntity);

            _repository.Save();

            var configRoot = new ConfigRoot
                                 {
                                     ComponentName = "grapeS",
                                     Data = new ConfigNode
                                                {
                                                    Name = "taboulI",
                                                    Value = "tomatoes",
                                                    Children =
                                                        new List<ConfigNode>
                                                            {new ConfigNode {Name = "Side", Value = "Rice"}}
                                                }
                                 };

            IRequest request = new InMemoryRequest
                                   {
                                       HttpMethod = "PUT",
                                       Uri = new Uri("http://localhost/Config/tabouli")
                                   };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(409, response.StatusCode);
        }

        [TestMethod]
        public void Put_with_invalid_content_type_returns_415()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
            {
                ComponentName = "taboUli",
                Data = new ConfigNode { Name = "tabouli", Value = "salad" }
            };
            _repository.Add(entity);
            _repository.Save();

            var configRoot = new ConfigRoot
            {
                ComponentName = "tabouLi",
                Data = new ConfigNode
                {
                    Name = "tabouli",
                    Value = "tomatoes",
                    Children =
                        new List<ConfigNode> { new ConfigNode { Name = "Side", Value = "Rice" } }
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "PUT",
                Uri = new Uri("http://localhost/Config/tabouli")
            };
            request.Headers["Content-Type"] = "text/plain";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(415, response.StatusCode);
        }

        [TestMethod]
        public void Put_on_empty_repo_returns_404()
        {
            var configRoot = new ConfigRoot
            {
                ComponentName = "tabouli",
                Data = new ConfigNode
                {
                    Name = "tabouli",
                    Value = "tomatoes",
                    Children =
                        new List<ConfigNode> { new ConfigNode { Name = "Side", Value = "Rice" } }
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "PUT",
                Uri = new Uri("http://localhost/Config/tacos")
            };

            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(404, response.StatusCode);
        }

        [TestMethod]
        public void Ensure_that_error_handling_works_for_put()
        {
            // Override the config to give it a broken logger
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(s => s.Debug(It.Is<string>(m => m.Contains("Entering ConfigHandler"))))
                .Throws(new ApplicationException("ha ha!"));

            _mockFileSystem = (new MockFileSystemProvider()).MockFileSystem;
            _repository = new DurableMemoryRepository<ConfigRoot>(DataPath, _mockFileSystem.Object);

            var container = new Container();
            container.Configure(x =>
            {
                x.For<IRepository<IJsonEntity<ConfigRoot>>>().Singleton().Use(_repository);
                x.For<ILog>().Singleton().Use(mockLogger.Object);
            });

            _host = new InMemoryHost(new Configuration(container));

            var configRoot = new ConfigRoot
            {
                ComponentName = "tabOuli",
                Data = new ConfigNode
                {
                    Name = "tabouli",
                    Value = "tomatoes",
                    Children =
                        new List<ConfigNode> { new ConfigNode { Name = "Side", Value = "Rice" } }
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "PUT",
                Uri = new Uri("http://localhost/Config/tacos")
            };

            request.Headers["Content-Type"] = "application/json";
            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(500, response.StatusCode);
        }

        [TestMethod]
        public void Put_returns_400_if_component_name_is_null()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
            {
                ComponentName = "taboUli",
                Data = new ConfigNode { Name = "tabouli", Value = "salad" }
            };
            _repository.Add(entity);
            _repository.Save();

            var configRoot = new ConfigRoot
            {
                ComponentName = null,
                Data = new ConfigNode
                {
                    Name = string.Empty,
                    Value = string.Empty
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "PUT",
                Uri = new Uri("http://localhost/Config/tabouli")
            };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public void Put_returns_400_if_component_name_contains_invalid_characters()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
            {
                ComponentName = "taboUli",
                Data = new ConfigNode { Name = "tabouli", Value = "salad" }
            };
            _repository.Add(entity);
            _repository.Save();

            var configRoot = new ConfigRoot
            {
                ComponentName = "jsnsadnc sd 8d8*** sad8s7sagd ^Q^ G5asds",
                Data = new ConfigNode
                {
                    Name = string.Empty,
                    Value = string.Empty
                }
            };

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "PUT",
                Uri = new Uri("http://localhost/Config/tabouli")
            };
            request.Headers["Content-Type"] = "application/json";

            var length = WriteConfigRootToStream(request.Entity.Stream, configRoot);
            request.Headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        #endregion PUT Tests

        #region DELETE Tests


        [TestMethod]
        public void Delete_removes_resource_from_repo()
        {
            var entity = _repository.Create();
            entity.Contents = new ConfigRoot
            {
                ComponentName = "taboUli",
                Data = new ConfigNode { Name = "taboUli", Value = "salad" }
            };
            _repository.Add(entity);
            _repository.Save();

            IRequest request = new InMemoryRequest
            {
                HttpMethod = "DELETE",
                Uri = new Uri("http://localhost/Config/tabouli")
            };
            var response = _host.ProcessRequest(request);

            Assert.AreEqual(204, response.StatusCode);
        }


        [TestMethod]
        public void Delete_on_empty_repo_returns_404()
        {
            IRequest request = new InMemoryRequest
            {
                HttpMethod = "DELETE",
                Uri = new Uri("http://localhost/Config/tabouli")
            };

            var response = _host.ProcessRequest(request);

            Assert.AreEqual(404, response.StatusCode);
        }

        #endregion DELETE Tests


        #region Helper Methods

        private long WriteConfigRootToStream(Stream stream, ConfigRoot configRoot)
        {
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(JsonConvert.SerializeObject(configRoot));
            }
            stream.Position = 0;
            return stream.Length;
        }

        private ConfigRoot GetConfigRootFromResponseStream(Stream stream)
        {
            stream.Position = 0;
            using (var sr = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<ConfigRoot>(sr.ReadToEnd());
            }
        }

        private List<string> GetStringListFromResponseStream(Stream stream)
        {
            stream.Position = 0;
            using (var sr = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<List<string>>(sr.ReadToEnd());
            }
        }

        #endregion Helper Methods
    }
}
