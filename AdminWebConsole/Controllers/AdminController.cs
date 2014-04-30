using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;
using SystemWrapper;
using Newtonsoft.Json;
using ProTeck.Config.Dto.V1;

namespace Habitat.Server.AdminWebConsole.Controllers
{
    /// <summary>
    /// Controller that handles administration of the Config Service
    /// </summary>
    [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
    public class AdminController : AsyncController
    {
        /// <summary>
        /// Media type to use when sending data to Config Service in a POST or PUT request
        /// </summary>
        private const string ConfigMediaType = "application/json";

        /// <summary>
        /// Name of the root level resource used with config service URLs
        /// </summary>
        private const string ConfigResourceRoot = "Config";

        /// <summary>
        /// The HttpClient implementation to use for interfacing with the Config Service.
        /// It's assumed that this implementation has been set up with the correct URL and security settings.
        /// </summary>
        private readonly HttpClient _httpConfigClient;

        /// <summary>
        /// Provides timestamps in a loosely-coupled manner
        /// </summary>
        private readonly IDateTimeWrap _dateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminController"/> class.
        /// </summary>
        /// <param name="httpConfigClient">
        /// The HttpClient implementation to use for interfacing with the Config Service.
        /// It's assumed that this implementation has been set up with the correct URL and security settings.
        /// </param>
        /// <param name="dateProvider">Provides timestamps in a loosely-coupled manner</param>
        public AdminController(HttpClient httpConfigClient, IDateTimeWrap dateProvider)
        {
            _httpConfigClient = httpConfigClient;
            _dateProvider = dateProvider;
        }

        /// <summary>
        /// Returns the default admin console page
        /// </summary>
        /// <returns>The view for the main console page</returns>
        public ViewResult Index()
        {
            return View();
        }

        /// <summary>
        /// Obtains a JSON array of all application components that are registered in the configuration system paired with their URLs.
        /// </summary>
        /// <remarks>
        /// This call is asynchronous - GetComponentListCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void GetComponentListAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            GetComponentListInternal().ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Internal method used to load the names of all configured components
        /// </summary>
        /// <returns>The asynchronous task that obtains the config data</returns>
        private Task<ConfigResults<List<string>>> GetComponentListInternal()
        {
            var serviceCallTask = _httpConfigClient.GetAsync(ConfigResourceRoot);
            var handleResponseTask = serviceCallTask.ContinueWith<ConfigResults<List<string>>>(HandleConfigServiceResponse<JsonArray, List<string>>);
            return handleResponseTask;
        }

        /// <summary>
        /// Signals receipt of all application components that are registered in the configuration system paired with their URLs.
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON array of the components, or an exception message</returns>
        public JsonResult GetComponentListCompleted(ConfigResults<List<string>> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Obtains the configuration for a given application component name
        /// </summary>
        /// <param name="componentName">The name of the component for which to load config data</param>
        /// <remarks>
        /// This call is asynchronous - GetComponentCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void GetComponentAsync(string componentName)
        {
            AsyncManager.OutstandingOperations.Increment();
            GetComponentInternal(componentName).ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Internal method used to obtain the configuration for a given application component name
        /// </summary>
        /// <param name="componentName">The name of the component for which to load config data</param>
        /// <returns>The asynchronous task that obtains the config data</returns>
        private Task<ConfigResults<ConfigRoot>> GetComponentInternal(string componentName)
        {
            var serviceCallTask = _httpConfigClient.GetAsync(string.Format("{0}/{1}", ConfigResourceRoot, componentName));
            var handleResponseTask = serviceCallTask.ContinueWith<ConfigResults<ConfigRoot>>(HandleConfigServiceResponse<JsonObject, ConfigRoot>);
            return handleResponseTask;
        }

        /// <summary>
        /// Signals receipt of the configuration for a given application component name
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object containing the configuration, or an exception message</returns>
        public JsonResult GetComponentCompleted(ConfigResults<ConfigRoot> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Signals a desire to save the configuration for a given application component 
        /// </summary>
        /// <param name="componentName">The name of the component for which to save config data</param>
        /// <param name="component">The config data to save</param>
        /// <remarks>
        /// This call is asynchronous - SaveComponentCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void SaveComponentAsync(string componentName, ConfigRoot component)
        {
            AsyncManager.OutstandingOperations.Increment();
            SaveComponentInternal(componentName, component).ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Internal method used to save configuration for an existing application component.
        /// </summary>
        /// <param name="componentName">The name of the exsiting component for which to save config data</param>
        /// <param name="component">The config data to save</param>
        /// <returns>The asynchronous task that performs the operation and obtains the config data</returns>
        private Task<ConfigResults<ConfigRoot>> SaveComponentInternal(string componentName, ConfigRoot component)
        {
            string componentJson = JsonConvert.SerializeObject(component);
            HttpContent content = new StringContent(componentJson, Encoding.UTF8, ConfigMediaType);
            var serviceCallTask = _httpConfigClient.PutAsync(string.Format("{0}/{1}", ConfigResourceRoot, componentName), content);
            var handleResponseTask = serviceCallTask.ContinueWith<ConfigResults<ConfigRoot>>(HandleConfigServiceResponse<JsonObject, ConfigRoot>);
            return handleResponseTask;
        }
        
        /// <summary>
        /// Signals completion of the Save Config process
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object echoing the latest configuration from the server, or an exception message</returns>
        public JsonResult SaveComponentCompleted(ConfigResults<ConfigRoot> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Signals a desire to save configuration for a new application component.
        /// </summary>
        /// <param name="newComponent">The config data to save</param>
        /// <remarks>
        /// The new component's name is derived from what's in the config data itself.
        /// This call is asynchronous - AddNewComponentCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void AddNewComponentAsync(ConfigRoot newComponent)
        {
            AsyncManager.OutstandingOperations.Increment();
            AddNewComponentInternal(newComponent).ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Internal method used to save configuration for a new application component.
        /// </summary>
        /// <param name="newComponent">The config data to save</param>
        /// <returns>The asynchronous task that performs the operation and obtains the config data</returns>
        private Task<ConfigResults<ConfigRoot>> AddNewComponentInternal(ConfigRoot newComponent)
        {
            string newComponentJson = JsonConvert.SerializeObject(newComponent);
            HttpContent content = new StringContent(newComponentJson, Encoding.UTF8, ConfigMediaType);
            var serviceCallTask = _httpConfigClient.PostAsync(ConfigResourceRoot, content);
            var handleResponseTask = serviceCallTask.ContinueWith<ConfigResults<ConfigRoot>>(HandleConfigServiceResponse<JsonObject, ConfigRoot>);
            return handleResponseTask;
        }

        /// <summary>
        /// Signals completion of the Add New Config process
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object containing the new configuration, or an exception message</returns>
        public JsonResult AddNewComponentCompleted(ConfigResults<ConfigRoot> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Signals a desire to remove the named config resource from storage
        /// </summary>
        /// <param name="componentName">The resource to remove</param>
        /// <remarks>
        /// This call is asynchronous - RemoveComponentCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void RemoveComponentAsync(string componentName)
        {
            AsyncManager.OutstandingOperations.Increment();
            var serviceCallTask = _httpConfigClient.DeleteAsync(string.Format("{0}/{1}", ConfigResourceRoot, componentName));
            var handleResponseTask = serviceCallTask.ContinueWith<ConfigResults<ConfigRoot>>(HandleConfigServiceResponse<JsonObject, ConfigRoot>);
            handleResponseTask.ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Signals completion of the Remove Config process
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>Empty (null) results or an exception message</returns>
        public JsonResult RemoveComponentCompleted(ConfigResults<ConfigRoot> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Copies the named config resource to a new resource with a new name
        /// </summary>
        /// <param name="existingComponentName">The source for the copy operation</param>
        /// <param name="newComponentName">The destination for the copy operation</param>
        /// <remarks>
        /// This call is asynchronous - CopyComponentCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void CopyComponentAsync(string existingComponentName, string newComponentName)
        {
            AsyncManager.OutstandingOperations.Increment();

            // Get existing config item
            Task<ConfigResults<ConfigRoot>> getComponentTask = GetComponentInternal(existingComponentName);
            getComponentTask.Wait();
            ConfigResults<ConfigRoot> configResults = getComponentTask.Result;
            if (configResults.ExceptionMessage != null)
            {
                CompleteAsyncOperation(configResults);
                return;
            }

            // Create a copy with a new name
            ConfigRoot newConfigRoot = configResults.Data;
            newConfigRoot.ComponentName = newComponentName;
            newConfigRoot.Data.Name = newConfigRoot.ComponentName;
            Task<ConfigResults<ConfigRoot>> createCopyTask = AddNewComponentInternal(newConfigRoot);
            createCopyTask.ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Signals completion of the Copy operation
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object containing the new configuration, or an exception message</returns>
        public JsonResult CopyComponentCompleted(ConfigResults<ConfigRoot> configResults)
        {
            return Json(configResults);
        }
        
        /// <summary>
        /// Swaps the named config resource to another config resource
        /// </summary>
        /// <param name="firstComponentName">The source1 for the swap operation</param>
        /// <param name="secondComponentName">The source2 for the swap operation</param>
        /// <remarks>
        /// This call is asynchronous - SwapComponentCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void SwapComponentAsync(string firstComponentName, string secondComponentName)
        {
            AsyncManager.OutstandingOperations.Increment();

            // Get first config item
            Task<ConfigResults<ConfigRoot>> getFirstComponentTask = GetComponentInternal(firstComponentName);
            getFirstComponentTask.Wait();
            ConfigResults<ConfigRoot> firstConfigResults = getFirstComponentTask.Result;
            if (firstConfigResults.ExceptionMessage != null)
            {
                CompleteAsyncOperation(new ConfigResults<List<ConfigRoot>>
                                           {
                                               Data = null,
                                               ExceptionMessage = firstConfigResults.ExceptionMessage
                                           });
                return;
            }

            // Get second config item
            Task<ConfigResults<ConfigRoot>> getSecondComponentTask = GetComponentInternal(secondComponentName);
            getSecondComponentTask.Wait();
            ConfigResults<ConfigRoot> secondConfigResults = getSecondComponentTask.Result;
            if (secondConfigResults.ExceptionMessage != null)
            {
                CompleteAsyncOperation(new ConfigResults<List<ConfigRoot>>
                                           {
                                               Data = null,
                                               ExceptionMessage = secondConfigResults.ExceptionMessage
                                           });
                return;
            }

            // Swap the compnents here
            ConfigRoot firstConfigRoot = firstConfigResults.Data;
            firstConfigRoot.ComponentName = secondComponentName;
            firstConfigRoot.Data.Name = firstConfigRoot.ComponentName;
            ConfigRoot secondConfigRoot = secondConfigResults.Data;
            secondConfigRoot.ComponentName = firstComponentName;
            secondConfigRoot.Data.Name = secondConfigRoot.ComponentName;

            Task<ConfigResults<ConfigRoot>> createSwapTask = SaveComponentInternal(firstComponentName, secondConfigRoot);
            createSwapTask.ContinueWith(
                first =>
                SaveComponentInternal(secondComponentName, firstConfigRoot).ContinueWith(
                    second => CompleteAsyncOperation(new ConfigResults<List<ConfigRoot>>
                                                         {
                                                             Data = new List<ConfigRoot>
                                                                        {
                                                                            first.Result.Data,
                                                                            second.Result.Data
                                                                        }
                                                         })));
        }
        
        /// <summary>
        /// Signals completion of the Swap operation
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object containing the new configuration, or an exception message</returns>
        public JsonResult SwapComponentCompleted(ConfigResults<List<ConfigRoot>> configResults)
        {
            return Json(configResults);
        }
        
        /// <summary>
        /// Retrieves all configuration data currently in storage
        /// </summary>
        /// <remarks>
        /// This call is asynchronous - ExportConfigCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void ExportConfigAsync()
        {
            AsyncManager.OutstandingOperations.Increment();

            var getComponentListTask = GetComponentListInternal();
            var getComponentListConfigData = getComponentListTask.ContinueWith<ConfigResults<List<ConfigRoot>>>(GetAllConfigurationData);
            getComponentListConfigData.ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Signals completion of the Export operation
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object containing the full set of configuration data, or an exception message</returns>
        public JsonResult ExportConfigCompleted(ConfigResults<List<ConfigRoot>> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Imports the provided set of configuration data into storage
        /// </summary>
        /// <param name="configEntries">The config entries to import. Invalid entries will not be imported.  Duplicates will be given a new name.</param>
        /// <remarks>
        /// This call is asynchronous - ImportConfigCompleted is called automatically when the operation is complete.
        /// </remarks>
        public void ImportConfigAsync(List<ConfigRoot> configEntries)
        {
            AsyncManager.OutstandingOperations.Increment();

            var getComponentListTask = GetComponentListInternal();
            var getComponentListConfigData = getComponentListTask.ContinueWith(componentListTask => ImportConfigInternal(configEntries, componentListTask.Result));
            getComponentListConfigData.ContinueWith(x => CompleteAsyncOperation(x.Result));
        }

        /// <summary>
        /// Internal method used to import a set of configuration entries in one operation
        /// </summary>
        /// <param name="configEntries">The config entries to import. Invalid entries will not be imported.  Duplicates will be given a new name.</param>
        /// <param name="existingComponentListTaskResults">The results of the GetComponentList operation.  Contains the names of all existing configured components</param>
        /// <returns>The results of the import task</returns>
        private ConfigResults<ImportResult> ImportConfigInternal(List<ConfigRoot> configEntries, ConfigResults<List<string>> existingComponentListTaskResults)
        {
            if (existingComponentListTaskResults.ExceptionMessage != null)
            {
                return new ConfigResults<ImportResult>
                            {
                                ExceptionMessage = existingComponentListTaskResults.ExceptionMessage
                            };
            }

            try
            {
                // For each item being imported, assign a new (hopefully unique) name, and perform an Add
                List<string> namesOfAllExistingComponents = existingComponentListTaskResults.Data;
                IEnumerable<ConfigRoot> importComponentsThatHaveNameConflicts = configEntries.Join(namesOfAllExistingComponents, x => x.ComponentName.ToLower(), y => y.ToLower(), (x, y) => x).ToArray();
                IEnumerable<ConfigRoot> importComponentsThatHaveUniqueNames = configEntries.Except(importComponentsThatHaveNameConflicts).ToArray();
                var currentTimestamp = _dateProvider.Now.DateTimeInstance;
                foreach (var importComponent in importComponentsThatHaveNameConflicts)
                {
                    importComponent.ComponentName = string.Format("{0}Imported{1:MMddyyyyHHmmss}", importComponent.ComponentName, currentTimestamp);
                    importComponent.Data.Name = importComponent.ComponentName;
                }

                var importSuccesses = new List<string>();
                var importWarnings = new List<string>();
                List<ConfigRoot> allImportComponents = importComponentsThatHaveNameConflicts.Union(importComponentsThatHaveUniqueNames).ToList();
                foreach (ConfigRoot importComponent in allImportComponents)
                {
                    var addComponentTask = AddNewComponentInternal(importComponent).ContinueWith(x => x.Result, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.NotOnFaulted);
                    if (addComponentTask.Result.ExceptionMessage != null)
                    {
                        importWarnings.Add(string.Format("Component '{0}' NOT imported successfully.  It may contain invalid contents. Error Message: {1}", importComponent.ComponentName, addComponentTask.Result.ExceptionMessage));
                    }
                    else
                    {
                        importSuccesses.Add(string.Format("Component '{0}' imported successfully.", importComponent.ComponentName));
                    }
                }

                return new ConfigResults<ImportResult>
                            {
                                Data = new ImportResult
                                            {
                                                ImportSuccesses = importSuccesses,
                                                ImportWarnings = importWarnings
                                            }
                            };
            }
            catch (Exception exception)
            {
                return new ConfigResults<ImportResult>
                            {
                                ExceptionMessage = exception.ToString()
                            };
            }
        }

        /// <summary>
        /// Signals completion of the Import operation
        /// </summary>
        /// <param name="configResults">The data returned by the async operation</param>
        /// <returns>A JSON object containing per-item details about the import operation, or an exception message.</returns>
        public JsonResult ImportConfigCompleted(ConfigResults<ImportResult> configResults)
        {
            return Json(configResults);
        }

        /// <summary>
        /// Helper method to handle all responses from config service (including errors) in a consistent manner
        /// </summary>
        /// <typeparam name="TJ">The type of JSON response to expect from the service</typeparam>
        /// <typeparam name="TR">The type of data object that's inside the ConfigResults instance returned by this method</typeparam>
        /// <param name="task">The response task from the HttpClient</param>
        private ConfigResults<TR> HandleConfigServiceResponse<TJ, TR>(Task<HttpResponseMessage> task)
            where TJ : class
        {
            ConfigResults<TR> configResults = new ConfigResults<TR>();
            try
            {
                HttpResponseMessage response = task.Result;
                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        var dataReadTask = response.Content.ReadAsStringAsync().ContinueWith(x => ReadConfigServiceJson<TJ, TR>(x));
                        dataReadTask.Wait();
                        configResults = dataReadTask.Result;
                    }
                }
                else
                {
                    configResults.ExceptionMessage = new WebException(string.Format("Unsuccessful Request: {0}", response.StatusCode.ToString())).ToString();
                }
            }
            catch (Exception exception)
            {
                configResults.ExceptionMessage = exception.ToString();
            }
            return configResults;
        }

        /// <summary>
        /// Helper method to parse config service responses as the appropriate JSON type and format for readability.
        /// </summary>
        /// <typeparam name="TJ">The expected type of JSON being returned (e.g. array)</typeparam>
        /// <typeparam name="TR">The type of data object that's inside the ConfigResults instance returned by this method</typeparam>
        /// <param name="readTask">The config service response task that provides the JSON values</param>
        private ConfigResults<TR> ReadConfigServiceJson<TJ, TR>(Task<string> readTask)
            where TJ : class
        {
            ConfigResults<TR> configResults = new ConfigResults<TR>();
            try
            {
                if (readTask.Result != null)
                {
                    TR tempResults = JsonConvert.DeserializeObject<TR>(readTask.Result.ToString());
                    configResults.Data = tempResults;
                }
            }
            catch (Exception exception)
            {
                configResults.ExceptionMessage = exception.ToString();
            }
            return configResults;
        }

        /// <summary>
        /// Helper method to spawn child tasks to get config data for each component
        /// </summary>
        /// <param name="componentListTask">The task that obtains all configured component names</param>
        private ConfigResults<List<ConfigRoot>> GetAllConfigurationData(Task<ConfigResults<List<string>>> componentListTask)
        {
            if (componentListTask.Result.ExceptionMessage != null)
            {
                return new ConfigResults<List<ConfigRoot>>
                {
                    ExceptionMessage = componentListTask.Result.ExceptionMessage
                };
            }

            List<string> componentNames = componentListTask.Result.Data;
            Task<ConfigResults<ConfigRoot>>[] taskList = new Task<ConfigResults<ConfigRoot>>[componentNames.Count];
            for (int i = 0; i < componentNames.Count; i++)
            {
                string componentName = componentNames[i];
                taskList[i] = GetComponentInternal(componentName).ContinueWith(x => x.Result, TaskContinuationOptions.AttachedToParent);
            }

            var configDataCollection = taskList.Select(getConfigTask => getConfigTask.Result.Data).ToList();
            var exceptionMessages = taskList.Select(getConfigTask => getConfigTask.Result.ExceptionMessage).ToArray();

            if (exceptionMessages.Any(x => x != null))
            {
                return new ConfigResults<List<ConfigRoot>>
                {
                    ExceptionMessage = string.Join(Environment.NewLine, exceptionMessages.Where(x => x != null).ToArray())
                };
            }

            return new ConfigResults<List<ConfigRoot>>
                       {
                           Data = configDataCollection
                       };
        }

        /// <summary>
        /// Signals that the given config results are complete and we're ready to return a JsonResult
        /// </summary>
        /// <typeparam name="TR">The type of data object that's inside the ConfigResults instance</typeparam>
        /// <param name="configResults">The config results to send</param>
        private void CompleteAsyncOperation<TR>(ConfigResults<TR> configResults)
        {
            AsyncManager.Parameters["configResults"] = configResults;
            AsyncManager.OutstandingOperations.Decrement();
        }
    }
}