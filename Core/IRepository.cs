using System;
using System.Linq;

namespace Habitat.Core
{
    /// <summary>
    /// Defines the operations for a generic Repository.
    /// </summary>
    /// <typeparam name="T">The type of resource that will be maintained by the Repository</typeparam>
    /// <remarks>
    /// See http://martinfowler.com/eaaCatalog/repository.html
    /// </remarks>
    public interface IRepository<T> : IDisposable
    {
        /// <summary>
        /// Property that allows select queries to be written via predicates
        /// </summary>
        IQueryable<T> Entities { get; }

        /// <summary>
        /// Operation to add a new resource in the repository
        /// </summary>
        /// <param name="item">The item to create</param>
        void Add(T item);

        /// <summary>
        /// Operation to persist the state of the repository
        /// </summary>
        void Save();

        /// <summary>
        /// Operation to delete a resource from the repository
        /// </summary>
        /// <param name="item">The item to delete</param>
        void Delete(T item);

        /// <summary>
        /// Operation to update a resource in the repository
        /// </summary>
        /// <param name="item">The item to update</param>
        void Update(T item);

        /// <summary>
        /// Operation to create a new instance of T, without necessarily adding it to the repository
        /// </summary>
        /// <returns>The newly-created item</returns>
        T Create();
    }
}
