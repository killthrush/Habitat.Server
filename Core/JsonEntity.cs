using Newtonsoft.Json;

namespace Habitat.Core
{
    /// <summary>
    /// Class used to package a type as an entity that can be persisted to a JSON repository
    /// </summary>
    /// <remarks>
    /// This class is not thread-safe.
    /// </remarks>
    internal class JsonEntity<T> : IJsonEntity<T> where T : class
    {
        /// <summary>
        /// The entity representation in JSON
        /// </summary>
        private string _jsonData;

        /// <summary>
        /// The entity representation as a given CLR type
        /// </summary>
        private T _contents;

        /// <summary>
        /// ID assigned to this entity from a Repository.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The entity representation as a given CLR type
        /// </summary>
        public T Contents
        {
            get
            {
                return _contents;
            }

            set
            {
                _contents = value;

            }
        }

        /// <summary>
        /// The entity representation in JSON
        /// </summary>
        public string JsonData
        {
            get
            {
                try
                {
                    _jsonData = _contents == null ? null : JsonConvert.SerializeObject(_contents, Formatting.None);
                }
                catch
                {
                    _jsonData = null;
                    _contents = null;
                }
                return _jsonData;
            }

            set
            {
                _jsonData = value;
                try
                {
                    _contents = _jsonData == null ? null : JsonConvert.DeserializeObject<T>(_jsonData);
                }
                catch
                {
                    _contents = null;
                    _jsonData = null;
                }
            }
        }

        /// <summary>
        /// Creates an instance of JsonEntity
        /// </summary>
        /// <param name="id">ID assigned to this entity from a Repository.</param>
        internal JsonEntity(int id)
        {
            Id = id;
        }
    }
}