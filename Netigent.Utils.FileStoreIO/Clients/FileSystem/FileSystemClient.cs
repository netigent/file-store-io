using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO.Clients.FileSystem
{
    public class FileSystemClient : IClient
    {
        #region Members
        public FileStorageProvider ProviderType => FileStorageProvider.FileSystem;

        public bool HasInit { get; set; } = false;

        private int MaxVersions { get; set; } = 1;

        private bool UseUniqueNames { get; set; } = false;

        private string FileStoreRoot { get; set; } = string.Empty;

        // If this exists it will be the 1st main folder used...

        private char ClientDirectoryChar => '\\';
        private string AppCodePrefix { get; set; } = string.Empty;

        private const string TestFile = "TestFile90u34gj93p4n3p9wcfp3h4pc9qf.txt";
        private const string _fileFlag = @":\";
        private const string _internetFlag = @"://";
        private const string _networkFlag = @"\\";
        #endregion

        #region ctor
        public FileSystemClient()
        {
        }

        public ResultModel Init(IConfig config, int maxVersions = 1, string appShortCode = "")
        {
            if (config?.StoreType == ProviderType)
            {
                FileSystemConfig fsConfig = config as FileSystemConfig;

                if (!string.IsNullOrEmpty(fsConfig.RootFolder))
                {
                    FileStoreRoot = fsConfig.RootFolder;
                    UseUniqueNames = fsConfig.StoreFileAsUniqueRef;
                    MaxVersions = maxVersions >= 1 ? maxVersions : 1;
                    AppCodePrefix = appShortCode;

                    //Test file to check access to the file system
                    string testFile = Path.Combine(FileStoreRoot, AppCodePrefix, TestFile);
                    try
                    {
                        if (!Directory.Exists(Path.Combine(FileStoreRoot, AppCodePrefix)))
                            Directory.CreateDirectory(Path.Combine(FileStoreRoot, AppCodePrefix));

                        if (File.Exists(testFile))
                            File.Delete(testFile);
                        else
                        {
                            File.CreateText(testFile).Dispose();
                            File.Delete(testFile);
                        }

                        return new ResultModel(HasInit = true, string.Empty);

                    }
                    catch (Exception ex)
                    {
                        return new ResultModel(false, ex.Message);
                    }
                }
            }

            return new ResultModel(false, "Bad or Missing Config");
        }
        #endregion

        #region Implementation
        public async Task<string> SaveFileAsync(InternalFileModel fileModel)
        {
            if (!HasInit)
            {
                throw new Exception($"File System Location {FileStoreRoot}, is not available or accessible, check permissions etc");
            }

            // Ensure folder exists
            string absoluteFolder = AsAbsolutePath(fileModel.PathTags
                .SetPathSeparator(ClientDirectoryChar) // Replace all / | \ with \
                .SafeFilename(replaceForbiddenFilenameChar: '_', allowExtendedAscii: true, ignorePathSeperators: new[] { ClientDirectoryChar }));

            bool absoluteFolderExists = Directory.Exists(absoluteFolder);
            if (!absoluteFolderExists)
            {
                Directory.CreateDirectory(absoluteFolder);
            }

            // Ensure safe filename for windows.
            string fileNameWithExt = $"{fileModel.Name}{fileModel.Extension}"
                .SafeFilename(replaceForbiddenFilenameChar: '_', allowExtendedAscii: true);

            // Full filepath
            string absoluteFile = Path.Combine(absoluteFolder, fileNameWithExt);

            // Strip relative path..
            PathInfo relativeFile = absoluteFile.GetPathInfo(removeRootFolderPrefix: FileStoreRoot);

            // Wipe any blocking files with same name...
            if (File.Exists(absoluteFile))
            {
                File.Delete(absoluteFile);
            }

            // Write 2 Disk
            using (var stream = new FileStream(absoluteFile, FileMode.Create))
            {
                await stream.WriteAsync(fileModel.Data, 0, fileModel.Data.Length);
            }

            // Relative File will be stored in index
            return absoluteFile.GetPathInfo(removeRootFolderPrefix: FileStoreRoot).RelativeFilePath;
        }

        public async Task<InternalFileModel> GetFileAsync(string filePath)
        {
            if (!HasInit)
            {
                throw new Exception($"File System Location {FileStoreRoot}, is not available or accessible, check permissions etc");
            }

            string absoluteFilePath = AsAbsolutePath(filePath);

            if (!File.Exists(absoluteFilePath))
                return null;

            FileInfo fileInfo = new FileInfo(absoluteFilePath);

            //Get from filesystem
            var memory = new MemoryStream();
            using (var stream = new FileStream(absoluteFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return new InternalFileModel
            {
                Data = memory.ToArray(),
                Name = fileInfo.Name,
                Extension = fileInfo.Extension,
            };
        }

        public Task<bool> DeleteFileAsync(string fileId)
        {
            if (!HasInit)
            {
                throw new Exception($"File System Location {FileStoreRoot}, is not available or accessible, check permissions etc");
            }

            // This will be the true path
            string absoluteFilePath = AsAbsolutePath(fileId);

            if (File.Exists(absoluteFilePath))
            {
                File.Delete(absoluteFilePath);
            }

            return Task.FromResult(true);
        }

        public async Task<long> IndexContentsAsync(ObservableCollection<InternalFileModel> indexList, string indexPathTags, bool scopeToAppFolder)
        {
            // Should we prepend AppCodePrefix
            string searchingPath = scopeToAppFolder && PathExtension.IsRelativePath(indexPathTags)
                ? AppCodePrefix + ClientDirectoryChar.ToString() + indexPathTags
                : indexPathTags;

            IList<InternalFileModel> output = await IndexFolderAsync(AsAbsolutePath(searchingPath));
            if (output.Count > 0)
            {
                indexList.InsertRange(output);
            }

            return output.Count;
        }

        private async Task<IList<InternalFileModel>> IndexFolderAsync(string currentPathTags)
        {
            List<InternalFileModel> output = new();
            var contents = new DirectoryInfo(currentPathTags);
            if (contents.Exists)
            {
                foreach (var folderItem in contents.GetDirectories())
                {
                    output.AddRange(await IndexFolderAsync(folderItem.FullName));
                }

                foreach (var fileItem in contents.GetFiles())
                {
                    PathInfo filePathInfo = fileItem.FullName.GetPathInfo(removeRootFolderPrefix: FileStoreRoot);

                    output.Add(new InternalFileModel
                    {
                        Name = filePathInfo.FilenameNoExtension,
                        Description = filePathInfo.Filename,
                        ExtClientRef = filePathInfo.RelativeFilePath,
                        Created = fileItem.CreationTimeUtc,
                        Modified = fileItem.LastWriteTimeUtc,
                        Extension = filePathInfo.FileExtension,
                        FileLocation = (int)FileStorageProvider.FileSystem,
                        PathTags = filePathInfo.PathTags,
                        MimeType = filePathInfo.MimeType,
                        SizeInBytes = fileItem.Length,
                    });
                }
            }

            return output;
        }
        #endregion

        #region Internal Functions
        private string AsAbsolutePath(string filePath)
        {

            if (string.IsNullOrEmpty(filePath))
            {
                return FileStoreRoot;
            }

            // Is the file absolute location??
            if (filePath.Contains(_fileFlag)
                || filePath.StartsWith(_networkFlag, StringComparison.InvariantCultureIgnoreCase)
                || filePath.StartsWith(FileStoreRoot))
            {
                return filePath.SetPathSeparator(ClientDirectoryChar);
            }
            else if (filePath.Contains(_internetFlag))
            {
                return filePath.SetPathSeparator('/');
            }

            // Treat as relative
            else
            {
                return Path.Combine(FileStoreRoot, filePath).SetPathSeparator(ClientDirectoryChar);
            }
        }
        #endregion
    }
}
