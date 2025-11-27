using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Helpers;
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
                FileSystemConfig? fsConfig = config as FileSystemConfig;

                if (fsConfig != null && !string.IsNullOrEmpty(fsConfig.RootFolder))
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
        public async Task<string> SaveFileAsync(FileStoreItem fileModel)
        {
            if (!HasInit)
            {
                throw new Exception($"File System Location {FileStoreRoot}, is not available or accessible, check permissions etc");
            }

            string absoluteFolder = fileModel.Folder
                .ToAbsolutePath(FileStoreRoot, AppCodePrefix)
                .SafeFilename(replaceForbiddenFilenameChar: '_', allowExtendedAscii: true, ignorePathSeperators: [CurrentSeperator]);

            bool absoluteFolderExists = Directory.Exists(absoluteFolder);
            if (!absoluteFolderExists)
            {
                Directory.CreateDirectory(absoluteFolder);
            }

            // Ensure safe filename for windows.
            string fileNameWithExt = fileModel.NameWithVersion
                .SafeFilename(replaceForbiddenFilenameChar: '_', allowExtendedAscii: true);

            // Full filepath
            string absoluteFile = Path.Combine(absoluteFolder, fileNameWithExt);

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

            // If root matches, then remove and return a relative file path.
            string extPath = absoluteFile
                .RemoveRootFolder(FileStoreRoot)
                .ToRelativeFile(useRelativeRoot: string.Empty, useSeperator: CurrentSeperator);

            return extPath;
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            if (!HasInit)
            {
                throw new Exception($"File System Location {FileStoreRoot}, is not available or accessible, check permissions etc");
            }

            string absoluteFilePath = filePath.ToAbsolutePath(FileStoreRoot, includePrefix: AppCodePrefix);

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

            return memory.ToArray();
        }

        public Task<bool> DeleteFileAsync(string fileId)
        {
            if (!HasInit)
            {
                throw new Exception($"File System Location {FileStoreRoot}, is not available or accessible, check permissions etc");
            }

            // This will be the true path
            string absoluteFilePath = fileId.ToAbsolutePath(rootFolder: FileStoreRoot, includePrefix: AppCodePrefix);

            if (File.Exists(absoluteFilePath))
            {
                File.Delete(absoluteFilePath);
            }

            return Task.FromResult(true);
        }

        public async Task<long> IndexContentsAsync(ObservableCollection<FileStoreItem> indexList, string indexFolder, bool scopeToAppFolder)
        {
            IList<FileStoreItem> output = await IndexFolderAsync(indexFolder.ToAbsolutePath(FileStoreRoot, includePrefix: AppCodePrefix));
            if (output.Count > 0)
            {
                indexList.InsertRange(output);
            }

            return output.Count;
        }

        private async Task<IList<FileStoreItem>> IndexFolderAsync(string currentFolder)
        {
            List<FileStoreItem> output = new();
            var contents = new DirectoryInfo(currentFolder);
            if (contents.Exists)
            {
                foreach (var folderItem in contents.GetDirectories())
                {
                    output.AddRange(await IndexFolderAsync(folderItem.FullName));
                }

                foreach (var fileItem in contents.GetFiles())
                {
                    string fullPath = fileItem.FullName;
                    var extension = Path.GetExtension(fullPath);

                    output.Add(new FileStoreItem
                    {
                        Name = Path.GetFileNameWithoutExtension(fileItem.FullName),
                        Description = string.Empty,
                        ExtClientRef = fullPath.RemoveRootFolder(FileStoreRoot).ToRelativeFile(useRelativeRoot: ""),
                        Created = fileItem.CreationTimeUtc,
                        Modified = fileItem.LastWriteTimeUtc,
                        Extension = extension,
                        FileLocation = (int)FileStorageProvider.FileSystem,
                        Folder = fullPath.RemoveRootFolder(FileStoreRoot).ToRelativeFile(),
                        MimeType = MimeHelper.GetMimeType(extension),
                        SizeInBytes = fileItem.Length,
                    });
                }
            }

            return output;
        }
        #endregion

        #region Internal Functions
        private char CurrentSeperator => IoExtensions.GetPathSeparator(FileStoreRoot);
        #endregion
    }
}
