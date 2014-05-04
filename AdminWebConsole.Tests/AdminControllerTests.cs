using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Schedulers;
using System.Web.Mvc;
using Habitat.Core;
using Habitat.Core.TestingLibrary;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Habitat.Server.AdminWebConsole.Controllers;
using StructureMap;

namespace Habitat.Server.AdminWebConsole.Tests
{
    [TestClass]
    public class AdminControllerTests
    {
        private const string ErrorStringCannotDeserializeException = "cannot deserialize the current json array";
        private const string ErrorStringCannotDeserializeObjectException = "cannot deserialize the current json object";
        private const string ErrorStringServerError = "system.net.webexception: unsuccessful request: internalservererror";

        private const string ErrorStringTimeout = "a task was canceled";
        private const string ErrorStringBadAddress = "the remote name could not be resolved";
        private const string ErrorStringConflict = "system.net.webexception: unsuccessful request: conflict";
        private const string ErrorStringResourceNotFound = "system.net.webexception: unsuccessful request: notfound";
        private const string ErrorStringBadRequest = "system.net.webexception: unsuccessful request: badrequest";
        private const string ErrorStringNullReference = "object reference not set to an instance of an object";
        private const string ErrorStringArgumentNull = "value cannot be null";
        private const string ImportSuccessMessage = "Component '{0}' imported successfully.";
        private const string ErrorStringUnexpectedChar = "unexpected character encountered while parsing value";

        private readonly CompareLogic _objectComparer = new CompareLogic();
        private IContainer _testContainer;

        [ClassInitialize]
        public static void FixtureSetUp(TestContext context)
        {
            // This hack uses a TPL extension that forces tasks to run on a single thread without needing to change any application code
            new CurrentThreadTaskScheduler().SetDefaultScheduler();
        }

        [TestInitialize]
        public void SetUp()
        {
            _testContainer = new Container(new MockRegistry());
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_testContainer != null)
            {
                _testContainer.Dispose();
            }
            _testContainer = null;
        }

        [TestMethod]
        public void Ensure_that_the_default_view_can_be_loaded()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            ViewResult view = controller.Index();
            Assert.IsNotNull(view);
            Assert.AreEqual(String.Empty, view.ViewName);
        }

        [TestMethod]
        public void Get_component_list_with_good_response()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.GetComponentListAsync,
                                                           controller.GetComponentListCompleted);
            AssertJsonResponse(jsonResult, new List<string> {"bar", "baz", "duplicate", "foo"});
        }

        [TestMethod]
        public void Get_component_list_with_invalid_json_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.GetComponentListAsync,
                                                           controller.GetComponentListCompleted);
            AssertExceptionResponse<List<string>>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Get_component_list_with_wrong_json_type_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonObject);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.GetComponentListAsync,
                                                           controller.GetComponentListCompleted);
            AssertExceptionResponse<List<string>>(jsonResult, ErrorStringCannotDeserializeObjectException);
        }

        [TestMethod]
        public void Get_component_list_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.GetComponentListAsync,
                                                           controller.GetComponentListCompleted);
            AssertExceptionResponse<List<string>>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Get_component_list_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.GetComponentListAsync,
                                                           controller.GetComponentListCompleted);
            AssertExceptionResponse<List<string>>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Get_component_list_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.GetComponentListAsync,
                                                           controller.GetComponentListCompleted);
            AssertExceptionResponse<List<string>>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Add_new_component_with_good_response()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = "new";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(configRoot),
                                                           controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult, configRoot);
        }

        [TestMethod]
        public void Add_new_component_with_invalid_characters_in_name()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = " sajdf9sad 77&&dsjfs d%$A";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(configRoot),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadRequest);
        }

        [TestMethod]
        public void Add_new_component_with_null_name()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = null;
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(configRoot),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadRequest);
        }

        [TestMethod]
        public void Add_new_component_with_duplicate_name()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = "duplicate";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(configRoot),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringConflict);
        }

        [TestMethod]
        public void Add_new_component_with_invalid_json_input()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(null),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadRequest);
        }

        [TestMethod]
        public void Add_new_component_with_invalid_json_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(null),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Add_new_component_with_wrong_json_type_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonArray);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(null),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringCannotDeserializeException);
        }

        [TestMethod]
        public void Add_new_component_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(null),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Add_new_component_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(null),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Add_new_component_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.AddNewComponentAsync(null),
                                                           controller.AddNewComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Get_component_with_good_response()
        {
            ConfigRoot configRoot = CreateValidConfig();
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo"),
                                                           controller.GetComponentCompleted);
            AssertJsonResponse(jsonResult, configRoot);
        }

        [TestMethod]
        public void Get_component_that_does_not_exist()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo2"),
                                                           controller.GetComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringResourceNotFound);
        }

        [TestMethod]
        public void Get_component_with_invalid_json_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo"),
                                                           controller.GetComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Get_component_with_wrong_json_type_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonArray);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo"),
                                                           controller.GetComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringCannotDeserializeException);
        }

        [TestMethod]
        public void Get_component_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo"),
                                                           controller.GetComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Get_component_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo"),
                                                           controller.GetComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Get_component_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.GetComponentAsync("foo"),
                                                           controller.GetComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Remove_component_with_good_response()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.RemoveComponentAsync("foo"),
                                                           controller.RemoveComponentCompleted);
            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(jsonResult.Data);
            Assert.IsInstanceOfType(jsonResult.Data, typeof (ConfigResults<ConfigRoot>));
            var configResults = (ConfigResults<ConfigRoot>) jsonResult.Data;
            Assert.IsNull(configResults.Data);
            Assert.IsNull(configResults.ExceptionMessage);
        }

        [TestMethod]
        public void Remove_component_that_does_not_exist()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.RemoveComponentAsync("foo2"),
                                                           controller.RemoveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringResourceNotFound);
        }

        [TestMethod]
        public void Remove_component_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.RemoveComponentAsync("foo"),
                                                           controller.RemoveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Remove_component_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.RemoveComponentAsync("foo"),
                                                           controller.RemoveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Remove_component_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.RemoveComponentAsync("foo"),
                                                           controller.RemoveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Save_component_with_good_response()
        {
            ConfigRoot configRoot = CreateValidConfig();
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.SaveComponentAsync("foo", configRoot),
                                                           controller.SaveComponentCompleted);
            AssertJsonResponse(jsonResult, configRoot);
        }

        [TestMethod]
        public void Save_component_with_invalid_characters_in_name()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = " sajdf9sad 77&&dsjfs d%$A";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.SaveComponentAsync("foo", configRoot),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadRequest);
        }

        [TestMethod]
        public void Save_component_name_change_that_causes_conflict()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = "duplicate";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.SaveComponentAsync("foo", configRoot),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringConflict);
        }

        [TestMethod]
        public void Save_component_that_does_not_exist()
        {
            ConfigRoot configRoot = CreateValidConfig();
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.SaveComponentAsync("foo2", configRoot),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringResourceNotFound);
        }

        [TestMethod]
        public void Save_component_with_invalid_json_input()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.SaveComponentAsync("foo", null),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadRequest);
        }

        [TestMethod]
        public void Save_component_with_invalid_json_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.SaveComponentAsync("foo", null),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Save_component_with_wrong_json_type_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonArray);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.SaveComponentAsync("foo", null),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringCannotDeserializeException);
        }

        [TestMethod]
        public void Save_component_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.SaveComponentAsync("foo", null),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Save_component_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.SaveComponentAsync("foo", null),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Save_component_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.SaveComponentAsync("foo", null),
                                                           controller.SaveComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Swap_components_with_good_response()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();

            ConfigRoot configRoot1 = CreateValidConfig();
            configRoot1.ComponentName = "config1";
            configRoot1.Data.Name = configRoot1.ComponentName;
            configRoot1.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name1", Value = "Value1"}};
            JsonResult jsonResult1 = InvokeConfigController(controller,
                                                            () => controller.AddNewComponentAsync(configRoot1),
                                                            controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult1, configRoot1);

            ConfigRoot configRoot2 = CreateValidConfig();
            configRoot2.ComponentName = "config2";
            configRoot2.Data.Name = configRoot2.ComponentName;
            configRoot2.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name2", Value = "Value2"}};
            JsonResult jsonResult2 = InvokeConfigController(controller,
                                                            () => controller.AddNewComponentAsync(configRoot2),
                                                            controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult2, configRoot2);

            InvokeConfigController(controller,
                                   () => controller.SwapComponentAsync("config1", "config2"),
                                   controller.SwapComponentCompleted);

            ConfigRoot expectedConfigRoot1 = CreateValidConfig();
            expectedConfigRoot1.ComponentName = "config1";
            expectedConfigRoot1.Data.Name = expectedConfigRoot1.ComponentName;
            expectedConfigRoot1.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name2", Value = "Value2"}};

            ConfigRoot expectedConfigRoot2 = CreateValidConfig();
            expectedConfigRoot2.ComponentName = "config2";
            expectedConfigRoot2.Data.Name = expectedConfigRoot2.ComponentName;
            expectedConfigRoot2.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name1", Value = "Value1"}};

            JsonResult jsonResultUpdatedConfig1 = InvokeConfigController(controller,
                                                                         () => controller.GetComponentAsync("config1"),
                                                                         controller.GetComponentCompleted);
            AssertJsonResponse(jsonResultUpdatedConfig1, expectedConfigRoot1);

            JsonResult jsonResultUpdatedConfig2 = InvokeConfigController(controller,
                                                                         () => controller.GetComponentAsync("config2"),
                                                                         controller.GetComponentCompleted);
            AssertJsonResponse(jsonResultUpdatedConfig2, expectedConfigRoot2);
        }

        [TestMethod]
        public void Swap_components_reversible_with_good_response()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();

            ConfigRoot configRoot1 = CreateValidConfig();
            configRoot1.ComponentName = "config1";
            configRoot1.Data.Name = configRoot1.ComponentName;
            configRoot1.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name1", Value = "Value1"}};
            JsonResult jsonResult1 = InvokeConfigController(controller,
                                                            () => controller.AddNewComponentAsync(configRoot1),
                                                            controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult1, configRoot1);

            ConfigRoot configRoot2 = CreateValidConfig();
            configRoot2.ComponentName = "config2";
            configRoot2.Data.Name = configRoot2.ComponentName;
            configRoot2.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name2", Value = "Value2"}};
            JsonResult jsonResult2 = InvokeConfigController(controller,
                                                            () => controller.AddNewComponentAsync(configRoot2),
                                                            controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult2, configRoot2);

            InvokeConfigController(controller,
                                   () => controller.SwapComponentAsync("config1", "config2"),
                                   controller.SwapComponentCompleted);
            InvokeConfigController(controller,
                                   () => controller.SwapComponentAsync("config1", "config2"),
                                   controller.SwapComponentCompleted);

            JsonResult jsonResultUpdatedConfig1 = InvokeConfigController(controller,
                                                                         () => controller.GetComponentAsync("config1"),
                                                                         controller.GetComponentCompleted);
            AssertJsonResponse(jsonResultUpdatedConfig1, configRoot1);

            JsonResult jsonResultUpdatedConfig2 = InvokeConfigController(controller,
                                                                         () => controller.GetComponentAsync("config2"),
                                                                         controller.GetComponentCompleted);
            AssertJsonResponse(jsonResultUpdatedConfig2, configRoot2);
        }

        [TestMethod]
        public void Swap_components_that_does_not_find_first_component()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();

            ConfigRoot configRoot2 = CreateValidConfig();
            configRoot2.ComponentName = "config2";
            configRoot2.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name2", Value = "Value2"}};
            JsonResult jsonResult2 = InvokeConfigController(controller,
                                                            () => controller.AddNewComponentAsync(configRoot2),
                                                            controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult2, configRoot2);

            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.SwapComponentAsync("config1", "config2"),
                                                           controller.SwapComponentCompleted);

            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringResourceNotFound);
        }

        [TestMethod]
        public void Swap_components_that_does_not_find_second_component()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();

            ConfigRoot configRoot1 = CreateValidConfig();
            configRoot1.ComponentName = "config1";
            configRoot1.Data.Children = new List<ConfigNode> {new ConfigNode {Name = "Name1", Value = "Value1"}};
            JsonResult jsonResult1 = InvokeConfigController(controller,
                                                            () => controller.AddNewComponentAsync(configRoot1),
                                                            controller.AddNewComponentCompleted);
            AssertJsonResponse(jsonResult1, configRoot1);

            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.SwapComponentAsync("config1", "config2"),
                                                           controller.SwapComponentCompleted);

            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringResourceNotFound);
        }

        [TestMethod]
        public void Copy_component_with_good_response()
        {
            ConfigRoot configRoot = CreateValidConfig();
            configRoot.ComponentName = "boo";
            configRoot.Data.Name = configRoot.ComponentName;
            // In this case, we expect the controller to get foo and save a copy as boo
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.CopyComponentAsync("foo", "boo"),
                                                           controller.CopyComponentCompleted);
            AssertJsonResponse(jsonResult, configRoot);
        }

        [TestMethod]
        public void Copy_component_that_causes_conflict()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.CopyComponentAsync("foo", "duplicate"),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringConflict);
        }

        [TestMethod]
        public void Copy_component_that_does_not_exist()
        {
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () => controller.CopyComponentAsync("foo2", "bar"),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringResourceNotFound);
        }

        [TestMethod]
        public void Copy_component_with_invalid_json_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () =>
                                                           controller.CopyComponentAsync(string.Empty, string.Empty),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Copy_component_with_wrong_json_type_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonArray);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () =>
                                                           controller.CopyComponentAsync(string.Empty, string.Empty),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringCannotDeserializeException);
        }

        [TestMethod]
        public void Copy_component_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () =>
                                                           controller.CopyComponentAsync(string.Empty, string.Empty),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Copy_component_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () =>
                                                           controller.CopyComponentAsync(string.Empty, string.Empty),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Copy_component_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller,
                                                           () =>
                                                           controller.CopyComponentAsync(string.Empty, string.Empty),
                                                           controller.CopyComponentCompleted);
            AssertExceptionResponse<ConfigRoot>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Given_valid_config_ensure_that_an_exported_list_can_be_obtained()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.ExportConfigAsync,
                                                           controller.ExportConfigCompleted);
            AssertJsonResponse(jsonResult, configList);
        }

        [TestMethod]
        public void Export_config_with_invalid_json_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.ExportConfigAsync,
                                                           controller.ExportConfigCompleted);
            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Export_config_with_wrong_json_type_response()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonArray);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.ExportConfigAsync,
                                                           controller.ExportConfigCompleted);
            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringCannotDeserializeException);
        }

        [TestMethod]
        public void Export_config_with_http_error_code()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.ExportConfigAsync,
                                                           controller.ExportConfigCompleted);
            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Export_config_with_http_timeout()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.ExportConfigAsync,
                                                           controller.ExportConfigCompleted);
            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Export_config_with_bad_address()
        {
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, controller.ExportConfigAsync,
                                                           controller.ExportConfigCompleted);
            AssertExceptionResponse<List<ConfigRoot>>(jsonResult, ErrorStringBadAddress);
        }

        [TestMethod]
        public void Ensure_that_a_valid_list_of_new_items_can_be_imported()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            configList[1].ComponentName = "newitem2";
            configList[2].ComponentName = "newitem3";
            configList[3].ComponentName = "newitem4";
            ImportResult expectedResults = new ImportResult
                                               {
                                                   ImportSuccesses = new List<string>
                                                                         {
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[0].ComponentName),
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[1].ComponentName),
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[2].ComponentName),
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[3].ComponentName)
                                                                         },
                                                   ImportWarnings = new List<string>()
                                               };

            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertJsonResponse(jsonResult, expectedResults);
        }

        [TestMethod]
        public void Ensure_that_a_valid_list_of_existing_items_can_be_imported_without_conflicts()
        {
            List<ConfigRoot> configList = CreateValidConfigList();

            ImportResult expectedResults = new ImportResult
                                               {
                                                   ImportSuccesses = new List<string>
                                                                         {
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[0].ComponentName +
                                                                                           "Imported01022011000000"),
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[1].ComponentName +
                                                                                           "Imported01022011000000"),
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[2].ComponentName +
                                                                                           "Imported01022011000000"),
                                                                             string.Format(ImportSuccessMessage,
                                                                                           configList[3].ComponentName +
                                                                                           "Imported01022011000000")
                                                                         },
                                                   ImportWarnings = new List<string>()
                                               };

            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertJsonResponse(jsonResult, expectedResults);
        }

        [TestMethod]
        public void Given_a_mix_of_valid_and_invalid_items_in_a_list_ensure_that_only_the_valid_ones_can_be_imported()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            configList[1].ComponentName = "newitem2";
            configList[2].ComponentName = "not allowed at all";
            configList[3].ComponentName = "!@#$%^&";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);

            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(jsonResult.Data);
            Assert.AreEqual(JsonRequestBehavior.DenyGet, jsonResult.JsonRequestBehavior);
            Assert.IsInstanceOfType(jsonResult.Data, typeof (ConfigResults<ImportResult>));
            var configResults = (ConfigResults<ImportResult>) jsonResult.Data;
            Assert.IsNotNull(configResults.Data);
            Assert.AreEqual(2, configResults.Data.ImportSuccesses.Count());
            Assert.AreEqual(string.Format(ImportSuccessMessage, configList[0].ComponentName),
                            configResults.Data.ImportSuccesses.ElementAt(0));
            Assert.AreEqual(string.Format(ImportSuccessMessage, configList[1].ComponentName),
                            configResults.Data.ImportSuccesses.ElementAt(1));
            Assert.AreEqual(2, configResults.Data.ImportWarnings.Count());
            Assert.IsTrue(configResults.Data.ImportWarnings.All(x => x.ToLower().Contains(ErrorStringBadRequest)));
            Assert.IsNull(configResults.ExceptionMessage);
        }

        [TestMethod]
        public void Given_a_list_of_invalid_items_in_a_list_ensure_that_nothing_can_be_imported_but_no_error_is_returned
            ()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "!@#$%^&";
            configList[1].ComponentName = "#$%^&*";
            configList[2].ComponentName = "(*&^";
            configList[3].ComponentName = "%&*^%*%&*)";
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);

            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(jsonResult.Data);
            Assert.AreEqual(JsonRequestBehavior.DenyGet, jsonResult.JsonRequestBehavior);
            Assert.IsInstanceOfType(jsonResult.Data, typeof (ConfigResults<ImportResult>));
            var configResults = (ConfigResults<ImportResult>) jsonResult.Data;
            Assert.IsNotNull(configResults.Data);
            Assert.AreEqual(0, configResults.Data.ImportSuccesses.Count());
            Assert.AreEqual(4, configResults.Data.ImportWarnings.Count());
            Assert.IsTrue(configResults.Data.ImportWarnings.All(x => x.ToLower().Contains(ErrorStringBadRequest)));
            Assert.IsNull(configResults.ExceptionMessage);
        }

        [TestMethod]
        public void Import_config_with_invalid_json_input()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0] = null;
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertExceptionResponse<ImportResult>(jsonResult, ErrorStringNullReference);
        }

        [TestMethod]
        public void Import_config_with_null_input()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0] = null;
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(null),
                                                           controller.ImportConfigCompleted);
            AssertExceptionResponse<ImportResult>(jsonResult, ErrorStringArgumentNull);
        }

        [TestMethod]
        public void Import_config_with_invalid_json_response()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerReturnsGibberish);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertExceptionResponse<ImportResult>(jsonResult, ErrorStringUnexpectedChar);
        }

        [TestMethod]
        public void Import_config_with_wrong_json_type_response()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturnsJsonArray);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(jsonResult.Data);
            Assert.AreEqual(JsonRequestBehavior.DenyGet, jsonResult.JsonRequestBehavior);
            Assert.IsInstanceOfType(jsonResult.Data, typeof (ConfigResults<ImportResult>));
            var configResults = (ConfigResults<ImportResult>) jsonResult.Data;
            Assert.IsNotNull(configResults.Data);
            Assert.AreEqual(0, configResults.Data.ImportSuccesses.Count());
            Assert.AreEqual(4, configResults.Data.ImportWarnings.Count());
            Assert.IsTrue(configResults.Data.ImportWarnings.All(x => x.ToLower().Contains(ErrorStringCannotDeserializeException)));
            Assert.IsNull(configResults.ExceptionMessage);
        }

        [TestMethod]
        public void Import_config_with_http_error_code()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            _testContainer.SetDefaultsToProfile(MockRegistry.ServerAlwaysReturns500Error);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertExceptionResponse<ImportResult>(jsonResult, ErrorStringServerError);
        }

        [TestMethod]
        public void Import_config_with_http_timeout()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            _testContainer.SetDefaultsToProfile(MockRegistry.ConnectionTimeout);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertExceptionResponse<ImportResult>(jsonResult, ErrorStringTimeout);
        }

        [TestMethod]
        public void Import_config_with_bad_address()
        {
            List<ConfigRoot> configList = CreateValidConfigList();
            configList[0].ComponentName = "newitem1";
            _testContainer.SetDefaultsToProfile(MockRegistry.BadHttpAddress);
            AdminController controller = _testContainer.GetInstance<AdminController>();
            JsonResult jsonResult = InvokeConfigController(controller, () => controller.ImportConfigAsync(configList),
                                                           controller.ImportConfigCompleted);
            AssertExceptionResponse<ImportResult>(jsonResult, ErrorStringBadAddress);
        }

        private void AssertJsonResponse<TR>(JsonResult jsonResult, TR expectedResponse)
        {
            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(jsonResult.Data);
            Assert.AreEqual(JsonRequestBehavior.DenyGet, jsonResult.JsonRequestBehavior);
            Assert.IsInstanceOfType(jsonResult.Data, typeof (ConfigResults<TR>));
            var configResults = (ConfigResults<TR>) jsonResult.Data;
            Assert.IsNotNull(configResults.Data);
            ComparisonResult comparisonResult = _objectComparer.Compare(expectedResponse, configResults.Data);
            Assert.IsTrue(comparisonResult.AreEqual, comparisonResult.DifferencesString);
            Assert.IsNull(configResults.ExceptionMessage);
        }

        private static void AssertExceptionResponse<TR>(JsonResult jsonResult, string expectedErrorContents)
        {
            if (expectedErrorContents == null)
                throw new ArgumentNullException("expectedErrorContents");
            Assert.IsNotNull(jsonResult);
            Assert.IsNotNull(jsonResult.Data);
            Assert.AreEqual(JsonRequestBehavior.DenyGet, jsonResult.JsonRequestBehavior);
            Assert.IsInstanceOfType(jsonResult.Data, typeof (ConfigResults<TR>));
            var configResults = (ConfigResults<TR>) jsonResult.Data;
            Assert.IsNull(configResults.Data);
            Assert.IsNotNull(configResults.ExceptionMessage);
            Assert.IsTrue(configResults.ExceptionMessage.ToLower().Contains(expectedErrorContents));
        }

        public static ConfigRoot CreateValidConfig()
        {
            ConfigRoot root = new ConfigRoot();
            root.ComponentName = "foo";
            root.LastModified = new DateTime(2009, 11, 14);
            // JSON.NET loses the last 4 digits for Ticks when doing a round-trip conversion so we can't use DateTime.Now if we want to check output
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

        private static List<ConfigRoot> CreateValidConfigList()
        {
            var configEntries = new List<ConfigRoot>
                                    {
                                        CreateValidConfig(),
                                        CreateValidConfig(),
                                        CreateValidConfig(),
                                        CreateValidConfig()
                                    };
            configEntries[0].ComponentName = "bar";
            configEntries[0].Data.Name = configEntries[0].ComponentName;
            configEntries[1].ComponentName = "baz";
            configEntries[1].Data.Name = configEntries[1].ComponentName;
            configEntries[2].ComponentName = "duplicate";
            configEntries[2].Data.Name = configEntries[2].ComponentName;
            configEntries[3].ComponentName = "foo";
            configEntries[3].Data.Name = configEntries[3].ComponentName;

            return configEntries;
        }

        public static JsonResult InvokeConfigController(AsyncController controller, Action startAction,
                                                        Func<ConfigResults<ImportResult>, JsonResult> completedAction)
        {
            return controller.InvokeAsyncController(startAction, completedAction, "configResults");
        }

        public static JsonResult InvokeConfigController(AsyncController controller, Action startAction,
                                                        Func<ConfigResults<List<ConfigRoot>>, JsonResult>
                                                            completedAction)
        {
            return controller.InvokeAsyncController(startAction, completedAction, "configResults");
        }

        public static JsonResult InvokeConfigController(AsyncController controller, Action startAction,
                                                        Func<ConfigResults<List<string>>, JsonResult> completedAction)
        {
            return controller.InvokeAsyncController(startAction, completedAction, "configResults");
        }

        public static JsonResult InvokeConfigController(AsyncController controller, Action startAction,
                                                        Func<ConfigResults<ConfigRoot>, JsonResult> completedAction)
        {
            return controller.InvokeAsyncController(startAction, completedAction, "configResults");
        }
    }
}