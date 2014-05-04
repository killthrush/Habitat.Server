using System.Collections.Generic;
using SystemInterface.IO;

namespace Habitat.Core
{
    /// <summary>
    /// Defines a set of common and useful filesystem operations.
    /// This abstraction reduces direct dependencies on the filesystem in code.
    /// </summary>
    public interface IFileSystemFacade
    {
        /// <summary>
        /// Checks for the existence of the given path on the filesystem and attempts to create it if not present.
        /// </summary>
        /// <param name="directoryPath">The path to check</param>
        void CreateDirectoryIfNotExists(string directoryPath);

        /// <summary>
        /// Checks for the existence of the given file.
        /// </summary>
        /// <param name="filePath">The path to check</param>
        bool FileExists(string filePath);

        /// <summary>
        /// Returns information about files in a given location
        /// </summary>
        /// <param name="directoryPath">The path to check</param>
        /// <returns>Details about files in the given location</returns>
        IEnumerable<IFileInfo> GetFilesInDirectory(string directoryPath);

        /// <summary>
        /// Creates a file at the specified location using the specified content
        /// </summary>
        /// <param name="filePath">The path where the file will be created</param>
        /// <param name="content">The content that will be written to the file</param>
        void CreateTextFile(string filePath, string content);

        /// <summary>
        /// Appends data to a file at the specified location using the specified content
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="content">The content that will be written to the file</param>
        void AppendToTextFile(string filePath, string content);

        /// <summary>
        /// Reads string data from a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The contents of the text file</returns>
        string ReadTextFile(string filePath);

        /// <summary>
        /// Method to delete a file given its full path
        /// </summary>
        /// <param name="filePath">Full path to the file that will be deleted</param>
        void DeleteFileIfExists(string filePath);

        /// <summary>
        /// Gets the directory full path of a given file path.
        /// </summary>
        /// <param name="filePath">input file path.</param>
        /// <returns>string representation of the directory full path.</returns>
        string GetDirectoryFullPath(string filePath);

        /// <summary>
        /// Checks to see if a directory path exists.
        /// </summary>
        /// <param name="directoryPath">input directory path.</param>
        /// <returns>true if directory path exists. False if not exist.</returns>
        bool DirectoryExists(string directoryPath);

        /// <summary>
        /// Creates a file with the content from the given stream.
        /// </summary>
        /// <param name="streamReaderWrap">input stream</param>
        /// <param name="filePath">the file path where the output is written to.</param>
        void CreateFileFromStream(IStreamReader streamReaderWrap, string filePath);

        /// <summary>
        /// Gets the temp directory path for the current user
        /// </summary>
        /// <returns>The path</returns>
        string GetTempDirectoryPath();
    }
}