using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using SystemInterface.IO;

namespace Habitat.Core
{
    /// <summary>
    /// Implements the operations for an in-memory Repository that uses the filesystem and a JSON parser for durability.
    /// </summary>
    /// <typeparam name="T">The type of resource that will be maintained by the Repository</typeparam>
    /// <remarks>
    /// * See http://martinfowler.com/eaaCatalog/repository.html.
    /// * Note that this repository is intended to be used for relatively small amounts of data.
    ///   For larger datasets, use an embedded database or SQL Server.
    /// * This class is thread-safe, however collisions will arise if two repository instances are configured to access the same folder at the same time.
    /// </remarks>
    public class DurableMemoryRepository<T> : IRepository<IJsonEntity<T>>
        where T : class
    {
        /// <summary>
        /// The full path to the location where the JSON repository will be stored.
        /// </summary>
        /// <remarks>
        /// Use of this respository requires that consumers have appropriate read/write access to this path
        /// </remarks>
        private readonly string _path;

        /// <summary>
        /// Helper facade that implements common file system operations
        /// </summary>
        private readonly IFileSystemFacade _fileFacade;

        /// <summary>
        /// Object used to manage concurrent access to JSON data structures and files
        /// </summary>
        private readonly ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        /// <summary>
        /// An in-memory table to keep track of pending entity operations that need to be synchronized with the filesystem
        /// </summary>
        private readonly Dictionary<int, IJsonEntity<T>> _context = new Dictionary<int, IJsonEntity<T>>();

        /// <summary>
        /// Keeps track of the next ID that needs to be used
        /// </summary>
        private int _nextId;

        /// <summary>
        /// Regular expression used to identify JSON data files
        /// </summary>
        private readonly Regex _dataFileNamePattern = new Regex(@"^(\d{10})_" + Regex.Escape(typeof(T).ToString()) + ".json$");

        /// <summary>
        /// List used to record the items that need to be deleted from disk on a Save
        /// </summary>
        private readonly List<IJsonEntity<T>> _deleteList = new List<IJsonEntity<T>>();

        /// <summary>
        /// Creates an instance of a DurableMemoryRepository
        /// </summary>
        /// <param name="path">The full path to the location where the JSON repository will be stored.</param>
        /// <param name="fileFacade">Helper facade that implements common file system operations</param>
        public DurableMemoryRepository(string path, IFileSystemFacade fileFacade)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            if (fileFacade == null)
            {
                throw new ArgumentNullException("fileFacade");
            }

            _path = path;
            _fileFacade = fileFacade;

            _readWriteLock.EnterWriteLock();
            try
            {
                 _fileFacade.CreateDirectoryIfNotExists(_path);
                InitializeNextAvailableId();
                InitializeDictionary();
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Reads all of the data files in the configured folder into memory.
        /// This can be expensive, but it's only called when the repository is created.
        /// </summary>
        private void InitializeDictionary()
        {
            IFileInfo[] entityDataFiles = GetDataFileInfoList();
            foreach (var entityDataFile in entityDataFiles)
            {
                int idValue;
                string fileContents = _fileFacade.ReadTextFile(entityDataFile.FullName);
                Match fileNameWithLargestId = _dataFileNamePattern.Match(entityDataFile.Name);
                int.TryParse(fileNameWithLargestId.Groups[1].Value, out idValue);
                var jsonEntity = new JsonEntity<T>(idValue);
                jsonEntity.JsonData = fileContents;
                _context[idValue] = jsonEntity;
            }
        }

        /// <summary>
        /// Gets the next available ID by examining the file system
        /// </summary>
        private void InitializeNextAvailableId()
        {
            int idValue = 0;
            IFileInfo[] entityDataFiles = GetDataFileInfoList();
            if (entityDataFiles.Length > 0)
            {
                Match fileNameWithLargestId = _dataFileNamePattern.Match(entityDataFiles[0].Name);
                int.TryParse(fileNameWithLargestId.Groups[1].Value, out idValue);
            }
            _nextId = idValue + 1;
        }

        /// <summary>
        /// Returns a list of all the data files present in the data directory
        /// </summary>
        /// <returns>The list of file metadata objects</returns>
        private IFileInfo[] GetDataFileInfoList()
        {
            IEnumerable<IFileInfo> currentFiles = _fileFacade.GetFilesInDirectory(_path);
            IFileInfo[] entityDataFiles = currentFiles.Where(x => _dataFileNamePattern.IsMatch(x.Name)).OrderByDescending(x => x.Name).ToArray();
            return entityDataFiles;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _readWriteLock.Dispose();
        }

        /// <summary>
        /// Property that allows select queries to be written via predicates
        /// </summary>
        /// <remarks>
        /// In order for this to be thread-safe, this property returns a copy of the in-memory
        /// data store and therefore could become stale between calls.
        /// </remarks>
        public IQueryable<IJsonEntity<T>> Entities
        {
            get
            {
                _readWriteLock.EnterReadLock();
                try
                {
                    var contextCopy = new Dictionary<int, IJsonEntity<T>>(_context);
                    return contextCopy.Values.AsQueryable();
                }
                finally
                {
                    _readWriteLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Operation to add a new resource in the repository
        /// </summary>
        /// <param name="item">The item to create</param>
        public void Add(IJsonEntity<T> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            _readWriteLock.EnterWriteLock();
            try
            {
                if (!_context.ContainsKey(item.Id))
                {
                    _context[item.Id] = item;
                }
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Operation to persist the state of the repository
        /// </summary>
        public void Save()
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                foreach (var item in _context)
                {
                    var filePath = GetDataFilePath(item.Value);
                    _fileFacade.CreateTextFile(filePath, item.Value.JsonData);
                }

                foreach (var item in _deleteList)
                {
                    var filePath = GetDataFilePath(item);
                    _fileFacade.DeleteFileIfExists(filePath);
                }
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Operation to delete a resource from the repository
        /// </summary>
        /// <param name="item">The item to delete</param>
        public void Delete(IJsonEntity<T> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            _readWriteLock.EnterWriteLock();
            try
            {
                _deleteList.Add(item);
                _context.Remove(item.Id);
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Operation to update a resource in the repository
        /// </summary>
        /// <param name="item">The item to update</param>
        public void Update(IJsonEntity<T> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            _readWriteLock.EnterWriteLock();
            try
            {
                _context[item.Id] = item;
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Operation to create a new instance of T
        /// </summary>
        /// <returns>The newly-created item</returns>
        /// <remarks>
        /// This method does not add an item to the repository by default
        /// </remarks>
        public IJsonEntity<T> Create()
        {
            JsonEntity<T> entity;
            _readWriteLock.EnterWriteLock();
            try
            {
                entity = new JsonEntity<T>(_nextId);
                _nextId++;
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
            return entity;
        }

        /// <summary>
        /// Given an entity, constructs the full path to the data file that should be used by that entity
        /// </summary>
        /// <param name="jsonEntity">The entity for which to construct a path</param>
        /// <returns>The path</returns>
        private string GetDataFilePath(IJsonEntity<T> jsonEntity)
        {
            var dataFileName = string.Format("{0:d10}_" + typeof (T) + ".json", jsonEntity.Id);
            return Path.Combine(_path, dataFileName);
        }
    }
}
