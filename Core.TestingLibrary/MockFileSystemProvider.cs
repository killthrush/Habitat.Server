using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SystemInterface.IO;
using SystemWrapper.IO;
using Moq;

namespace Habitat.Core.TestingLibrary
{
    public class MockFileSystemProvider
    {
        private readonly Dictionary<string, MockFile> _mockFileTable = new Dictionary<string, MockFile>();
        private Mock<IFileSystemFacade> _mockFileSystem;

        private class MockFile : Tuple<Mock<IFileInfo>, string>
        {
            public MockFile(Mock<IFileInfo> fileMetadata, string fileContents) : base(fileMetadata, fileContents)
            {
            }
        }

        public Mock<IFileSystemFacade> MockFileSystem
        {
            get
            {
                if (_mockFileSystem == null)
                {
                    _mockFileSystem = CreateMockFileSystem();
                }
                return _mockFileSystem;
            }
        }

        private Mock<IFileSystemFacade> CreateMockFileSystem()
        {
            // Set up basic file system operations
            Mock<IFileSystemFacade> mock = new Mock<IFileSystemFacade>(MockBehavior.Strict);
            mock.Setup(x => x.CreateDirectoryIfNotExists(It.IsAny<string>())).Callback(() => {});
            mock.Setup(x => x.FileExists(It.IsAny<string>())).Returns((string filePath) => _mockFileTable.ContainsKey(filePath));

            // Create a mock folder for the mock files
            mock.Setup(x => x.GetFilesInDirectory(It.IsAny<string>())).Returns(
                delegate
                    {
                        return _mockFileTable.Values.Select(x => x.Item1.Object);
                    });

            // Set up the IO operations for the mock files so it mimics the real thing
            mock.Setup(x => x.CreateTextFile(It.IsAny<string>(), It.IsAny<string>())).Callback(
                delegate(string fileName, string fileContents)
                    {
                        CreateOrUpdateMockFile(fileName, fileContents);
                    });
            mock.Setup(x => x.AppendToTextFile(It.IsAny<string>(), It.IsAny<string>())).Callback(
                delegate(string fileName, string fileContents)
                    {
                        AppendToMockFile(fileName, fileContents);
                    });
            mock.Setup(x => x.ReadTextFile(It.IsAny<string>())).Returns(
                delegate(string fileName)
                    {
                        return _mockFileTable[fileName].Item2;
                    });
            mock.Setup(x => x.DeleteFileIfExists(It.IsAny<string>())).Callback(
                delegate(string x)
                    {
                        if (_mockFileTable.ContainsKey(x))
                        {
                            _mockFileTable.Remove(x);
                        }
                    });
            mock.Setup(x => x.GetDirectoryFullPath(It.IsAny<string>())).Returns(
                (string filePath) =>
                {
                    var lastSlashPos = filePath.LastIndexOf("\\", StringComparison.Ordinal);
                    var lastSegment = filePath.Substring(lastSlashPos, filePath.Length - lastSlashPos);
                    return lastSegment.LastIndexOf(".", StringComparison.Ordinal) > 0 ? filePath.Substring(0, lastSlashPos) : filePath;
                });
            mock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(
                (string directoryPath) =>  _mockFileTable.ContainsKey(directoryPath));
            mock.Setup(x => x.CreateFileFromStream(It.IsAny<IStreamReader>(), It.IsAny<string>())).Callback(
                (IStreamReader streamReaderWrap, string filePath) =>
                    {
                        if (streamReaderWrap == null || streamReaderWrap.BaseStream == null || streamReaderWrap.BaseStream.Length <= 0)
                            return;

                        using (var memoryStreamWrap = new MemoryStreamWrap())
                        {
                            memoryStreamWrap.CopyFromStream(streamReaderWrap.BaseStream);
                            CreateOrUpdateMockFile(filePath, Convert.ToBase64String(memoryStreamWrap.ToArray()));
                        }
                    });

            return mock;
        }

        public void CreateOrUpdateMockFile(string fileName, string fileContents)
        {
            if (!_mockFileTable.ContainsKey(fileName))
            {
                CreateMockFile(fileName, fileContents);
            }
            else
            {
                var existingMockMetadata = _mockFileTable[fileName].Item1;
                _mockFileTable[fileName] = new MockFile(existingMockMetadata, fileContents);
            }
        }

        public void AppendToMockFile(string fileName, string fileContents)
        {
            if (!_mockFileTable.ContainsKey(fileName))
            {
                CreateMockFile(fileName, fileContents);
            }
            else
            {
                var existingMockMetadata = _mockFileTable[fileName].Item1;
                StringBuilder builder = new StringBuilder(_mockFileTable[fileName].Item2);
                builder.AppendFormat("{0}{1}", Environment.NewLine, fileContents);
                _mockFileTable[fileName] = new MockFile(existingMockMetadata, builder.ToString());
            }
        }

        private void CreateMockFile(string fileName, string fileContents)
        {
            FileInfo f = new FileInfo(fileName);
            Mock<IFileInfo> mockFile = new Mock<IFileInfo>(MockBehavior.Strict);
            mockFile.SetupGet(x => x.Name).Returns(f.Name);
            mockFile.SetupGet(x => x.FullName).Returns(fileName);
            _mockFileTable.Add(fileName, new MockFile(mockFile, fileContents));
        }

        public void Reset()
        {
            _mockFileTable.Clear();
        }
    }
}
