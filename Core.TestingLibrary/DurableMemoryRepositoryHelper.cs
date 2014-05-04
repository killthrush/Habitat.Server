using System;
using System.IO;
using Moq;
using Newtonsoft.Json;

namespace Habitat.Core.TestingLibrary
{
    /// <summary>
    /// Contains some helper methods for testing DurableMemoryRepository behaviors.
    /// </summary>
    public static class DurableMemoryRepositoryHelper
    {
        /// <summary>
        /// Allows testing of Durable Cache reads by writing to the mock file system it depends on.
        /// </summary>
        /// <typeparam name="T">The type of object being persisted in the cache</typeparam>
        /// <param name="data">An object to be written to the cache</param>
        /// <param name="mockFileSystem">The mock filesystem that will be used to perform the "write"</param>
        /// <param name="id">An ID to assign to the item we're writing to the cache</param>
        /// <param name="path">The path where the mock file will be "written"</param>
        public static void CreateMockDurableCacheEntry<T>(T data, Mock<IFileSystemFacade> mockFileSystem, int id, string path)
        {
            string jsonData = JsonConvert.SerializeObject(data);
            var mockFileName = String.Format("{0:d10}_{1}.json", id, typeof(T));
            var mockPath = Path.Combine(path, mockFileName);
            mockFileSystem.Object.CreateTextFile(mockPath, jsonData);
        }

        /// <summary>
        /// Allows testing of Durable Cache writes by reading from the mock file system it depends on.
        /// </summary>
        /// <typeparam name="T">The type of object being read from the cache</typeparam>
        /// <param name="mockFileSystem">The mock filesystem that will be used to perform the "read"</param>
        /// <param name="id">An ID to use when reading an item from the cache</param>
        /// <param name="path">The path from which the mock file will be "read"</param>
        /// <returns>An object read from the cache</returns>
        public static T ReadMockDurableCacheEntry<T>(Mock<IFileSystemFacade> mockFileSystem, int id, string path)
        {
            var mockFileName = String.Format("{0:d10}_{1}.json", id, typeof(T));
            var mockPath = Path.Combine(path, mockFileName);
            var jsonData = mockFileSystem.Object.ReadTextFile(mockPath);
            T data = JsonConvert.DeserializeObject<T>(jsonData);
            return data;
        }
    }
}
