using System.Collections.Generic;
using System.IO;
using System.Text;
using SystemWrapper.IO;
using SystemInterface.IO;

namespace Habitat.Core
{
    /// <summary>
    /// Concrete implementation of a set of common and useful filesystem operations.
    /// </summary>
    /// <remarks>
    /// This class, as it wraps a core environment dependency, should not be considered in test coverage.
    /// </remarks>
    public class FileSystemFacade : IFileSystemFacade
    {
        /// <summary>
        /// Checks for the existence of the given path on the filesystem and attempts to create it if not present.
        /// </summary>
        /// <param name="directoryPath">The path to check</param>
        public void CreateDirectoryIfNotExists(string directoryPath)
        {
            DirectoryWrap directory = new DirectoryWrap();
            if (!directory.Exists(directoryPath))
            {
                directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Checks for the existence of the given file.
        /// </summary>
        /// <param name="filePath">The path to check</param>
        public bool FileExists(string filePath)
        {
            FileWrap file = new FileWrap();
            return file.Exists(filePath);
        }

        /// <summary>
        /// Returns information about files in a given location
        /// </summary>
        /// <param name="directoryPath">The path to check</param>
        /// <returns>Details about files in the given location</returns>
        public IEnumerable<IFileInfo> GetFilesInDirectory(string directoryPath)
        {
            var directoryInfo = new DirectoryInfoWrap(directoryPath);
            return directoryInfo.GetFiles();
        }

        /// <summary>
        /// Creates a file at the specified location using the specified content
        /// </summary>
        /// <param name="filePath">The path where the file will be created</param>
        /// <param name="content">The content that will be written to the file</param>
        public void CreateTextFile(string filePath, string content)
        {
            WriteContentToFile(filePath, Encoding.UTF8.GetBytes(content), FileMode.Create);
        }

        /// <summary>
        /// Appends data to a file at the specified location using the specified content
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="content">The content that will be written to the file</param>
        public void AppendToTextFile(string filePath, string content)
        {
            WriteContentToFile(filePath, Encoding.UTF8.GetBytes(content), FileMode.Append);
        }

        /// <summary>
        /// Reads all data from a file into a string
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The contents of the text file</returns>
        public string ReadTextFile(string filePath)
        {
            using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Method to delete a file given its full path
        /// </summary>
        /// <param name="filePath">Full path to the file that will be deleted</param>
        public void DeleteFileIfExists(string filePath)
        {
            FileWrap fileHelper = new FileWrap();
            if (fileHelper.Exists(filePath))
            {
                fileHelper.Delete(filePath);
            }
        }

        /// <summary>
        /// Gets the directory full path of a given file path.
        /// </summary>
        /// <param name="filePath">input file path.</param>
        /// <returns>string representation of the directory full path.</returns>
        public string GetDirectoryFullPath(string filePath)
        {
            var fileInfo = new FileInfoWrap(filePath);
            return fileInfo.DirectoryName;
        }

        /// <summary>
        /// Checks to see if a directory path exists.
        /// </summary>
        /// <param name="directoryPath">input directory path.</param>
        /// <returns>true if directory path exists. False if not exist.</returns>
        public bool DirectoryExists(string directoryPath)
        {
            var dirWrap = new DirectoryWrap();
            return dirWrap.Exists(directoryPath);
        }

        /// <summary>
        /// Creates a file with the content from the given stream.
        /// </summary>
        /// <param name="streamReader">input stream</param>
        /// <param name="filePath">the file path where the output is written to.</param>
        public void CreateFileFromStream(IStreamReader streamReader, string filePath)
        {
            if (streamReader == null || streamReader.BaseStream == null || streamReader.BaseStream.Length <= 0)
            {
                return;
            }

            using (var fileStreamWrap = new FileStreamWrap(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStreamWrap.CopyFromStream(streamReader.BaseStream);
            }
        }

        /// <summary>
        /// Gets the temp directory path for the current user
        /// </summary>
        /// <returns>The path</returns>
        public string GetTempDirectoryPath()
        {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Writes string data to a text file using the specified mode
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="content">The content that will be written to the file</param>
        /// <param name="fileMode">The write mode to use</param>
        /// <remarks>
        /// This method is not thread safe, nor does it guarantee that the file won't be locked.
        /// </remarks>
        private static void WriteContentToFile(string filePath, byte[] content, FileMode fileMode)
        {
            using (var writer = new BinaryWriterWrap(new FileStreamWrap(filePath, fileMode, FileAccess.Write).StreamInstance))
            {
                writer.Write(content);
                writer.Flush();
            }
        }
    }
}