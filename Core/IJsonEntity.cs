namespace Habitat.Core
{
    /// <summary>
    /// Defines the shape of a class used to package an object that can be persisted to a JSON repository
    /// </summary>
    public interface IJsonEntity<T> where T : class
    {
        /// <summary>
        /// ID assigned to this entity from a Repository.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The entity representation as a given CLR type
        /// </summary>
        T Contents { get; set; }

        /// <summary>
        /// The entity representation in JSON
        /// </summary>
        string JsonData { get; set; }
    }
}