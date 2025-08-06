using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Netigent.Utils.FileStoreIO.Clients;
using Netigent.Utils.FileStoreIO.Clients.Box;
using Netigent.Utils.FileStoreIO.Clients.FileSystem;
using Netigent.Utils.FileStoreIO.Clients.S3;
using Netigent.Utils.FileStoreIO.Constants;
using Netigent.Utils.FileStoreIO.Dal;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO
{
    public class FileStoreIOClient : IFileStoreIOClient
    {
        #region Members
        private IList<StoreProviderDef> StoreProvidersList { get; set; } = new List<StoreProviderDef>();

        public List<string> OutputMessages { get; internal set; } = new();

        public bool IsReady { get; set; } = false;

        public char PathSeperator => SystemConstants.InternalDirectorySeparator;

        public string AppPrefix => _appPrefix ?? string.Empty;

        private readonly InternalDatabaseClient _dbClient;
        private readonly string _filePrefix;
        private readonly string _appPrefix;
        private readonly FileStorageProvider _defaultStorage;
        private readonly int _maxVersions;

        private const string _notSpecifiedFlag = "_$";
        private const string _versionFlag = "__ver_";
        private const string _notSpecifiedSchema = "dbo";

        private const string offlineWarning = "is Offline or Configuration issue exists";
        #endregion

        #region ctor
        /// <summary>
        /// FileIOClient using FileIOConfig
        /// </summary>
        /// <param name="fileIOConfig"></param>
        public FileStoreIOClient(IOptions<FileStoreIOConfig> fileIOConfig)
        {
            _filePrefix = fileIOConfig.Value.FilePrefix ?? _notSpecifiedFlag;
            _appPrefix = fileIOConfig.Value.AppPrefix ?? string.Empty;
            _maxVersions = fileIOConfig.Value.MaxVersions > 1 ? fileIOConfig.Value.MaxVersions : 1;
            _dbClient = new InternalDatabaseClient(fileIOConfig.Value.Database, fileIOConfig.Value.DatabaseSchema, _appPrefix);
            _defaultStorage = fileIOConfig.Value.DefaultStorage != FileStorageProvider.UseDefault
                ? fileIOConfig.Value.DefaultStorage
                : FileStorageProvider.Database;
            IsReady = Internal_StartupCheck();

            if (IsReady)
            {
                StoreProvidersList.Add(new StoreProviderDef(fileIOConfig.Value.FileSystem, _maxVersions, _appPrefix));
                StoreProvidersList.Add(new StoreProviderDef(fileIOConfig.Value.S3, _maxVersions, _appPrefix));
                StoreProvidersList.Add(new StoreProviderDef(fileIOConfig.Value.Box, _maxVersions, _appPrefix));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="databaseConnection">Database to use for fileStoreIndex table, account in connection string should have the ability to create tables in that database</param>
        /// <param name="fileStoreRoot">Location you wish to store files uploaded to the filesystem e.g. c:\temp\filestore \\myfiles\fileuploads</param>
        /// <param name="filePrefix">(Optional) Default=_$, Will mark the files with a prefix that is used in the file system</param>
        /// <param name="dbSchema">(Optional) Default=dbo, database schema to create or use the FileStoreIndex table, default is dbo</param>
        /// <param name="useUniqueRef">(Optional) Default=true, means files on filesystem will be stored as 436265634626235245.doc etc rather than myfile.doc</param>
        public FileStoreIOClient(
            string appPrefix,
            string databaseConnection,
            string filePrefix = _notSpecifiedFlag,
            string dbSchema = _notSpecifiedSchema,
            int maxVersions = 1,
            FileStorageProvider defaultFileStore = FileStorageProvider.Database,
            FileSystemConfig? fileSystemConfig = null,
            BoxConfig? boxConfig = null,
            S3Config? s3Config = null)
        {

            _filePrefix = filePrefix;
            _maxVersions = maxVersions > 1 ? maxVersions : 1;
            _appPrefix = appPrefix ?? string.Empty;
            _defaultStorage = defaultFileStore != FileStorageProvider.UseDefault
                ? defaultFileStore
                : FileStorageProvider.Database;

            //Create the filestore client
            _dbClient = new InternalDatabaseClient(databaseConnection, dbSchema, _appPrefix);
            IsReady = Internal_StartupCheck();

            if (IsReady)
            {
                StoreProvidersList.Add(new StoreProviderDef(fileSystemConfig, _maxVersions, _appPrefix));
                StoreProvidersList.Add(new StoreProviderDef(s3Config, _maxVersions, _appPrefix));
                StoreProvidersList.Add(new StoreProviderDef(boxConfig, _maxVersions, _appPrefix));
            }
        }
        #endregion

        #region Implementations

        /// <inheritdoc />
        public bool IsClientAvailable(FileStorageProvider fileStorageProvider)
        {
            return StoreProvidersList.FirstOrDefault(x => x.StoreType == fileStorageProvider)?.IsAvailable ?? false;
        }

        /// <inheritdoc />
        public async Task<string> File_UpsertAsyncV2(string relationalFilePathAndName, byte[] fileContents, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default)
        {
            PathInfo pathInfo = relationalFilePathAndName.GetPathInfo(addRootFolderPrefix: _appPrefix);

            var createdDate = created == default ? DateTime.UtcNow : created;

            var fileModel = new InternalFileModel
            {
                Created = createdDate,
                MimeType = pathInfo.MimeType,
                Extension = pathInfo.FileExtension,
                Name = pathInfo.FilenameNoExtension,
                Description = description ?? pathInfo.FilenameNoExtension,
                FileLocation = fileStorageProvider == FileStorageProvider.UseDefault ? (int)_defaultStorage : (int)fileStorageProvider,
                PathTags = pathInfo.PathTags,
                ExtClientRef = relationalFilePathAndName,
                Data = fileContents,
            };

            return await Internal_UpsertAsync(fileModel, fileStorageProvider);
        }

        /// <inheritdoc />
        public async Task<string> File_UpsertAsyncV2(
            string relationalFilePathAndName,
            IFormFile file,
            FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault,
            string description = "",
            DateTime created = default)
        {
            byte[] data = null;

            using (var dataStream = new MemoryStream())
            {
                await file.CopyToAsync(dataStream);
                data = dataStream.ToArray();
            }

            return await File_UpsertAsyncV2(
                relationalFilePathAndName: relationalFilePathAndName.EndsWith(file.FileName, StringComparison.InvariantCultureIgnoreCase)
                    ? relationalFilePathAndName // Its got the filename already
                    : relationalFilePathAndName + SystemConstants.InternalDirectorySeparator + file.FileName,
                fileContents: data,
                fileStorageProvider: fileStorageProvider,
                description: description ?? file.FileName,
                created: created == default ? DateTime.UtcNow : created);

        }

        /// <inheritdoc/>
        public async Task<string> File_UpsertAsyncV2(
            byte[] fileContents,
            string filename,
            string[] pathTags,
            FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault,
            string description = "",
            DateTime created = default) =>

            // Repoint to new method
            await File_UpsertAsyncV2(
                relationalFilePathAndName: $"{string.Join(SystemConstants.InternalDirectorySeparator.ToString(), pathTags)}{SystemConstants.InternalDirectorySeparator.ToString()}{filename}",
                fileContents: fileContents,
                fileStorageProvider: fileStorageProvider,
                description: description,
                created: created);

        /// <inheritdoc/>
        public async Task<string> File_UpsertAsyncV2(
            IFormFile file,
            string filename,
            string[] pathTags,
            FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault,
            string description = "",
            DateTime created = default) =>

            // Repoint to new method
            await File_UpsertAsyncV2(
                relationalFilePathAndName: $"{string.Join(SystemConstants.InternalDirectorySeparator.ToString(), pathTags)}{SystemConstants.InternalDirectorySeparator.ToString()}{filename}",
                file: file,
                fileStorageProvider: fileStorageProvider,
                description: description,
                created: created);

        /// <inheritdoc/>
        public List<InternalFileModel> Files_GetAllV2(string[] pathTags, bool recursiveSearch = true) =>
           Files_GetAllV2(relationalFilePath: $"{string.Join(SystemConstants.InternalDirectorySeparator.ToString(), pathTags)}", recursiveSearch);

        /// <inheritdoc />
        public async Task<FileObjectModel> File_GetAsyncV2(string fileRef, int priorVersion = 0)
        {
            //Grab the fileRecord and if stored in database (this doesnt get the binary in the dataField)
            var fileList = _dbClient.FileStoreIndex_GetAllByRef(fileRef);

            // If we have files and the version we're being asked for doesnt exceed the array
            if (fileList?.Count > 0 && fileList?.Count > priorVersion)
            {
                //Try and get the file from index
                InternalFileModel? file2Get = fileList[priorVersion];

                if (file2Get != null)
                {
                    // Pull specific file by id
                    // If this is a DB record it will now have the data
                    InternalFileModel populatedFile = await Internal_GetFileAsync(_dbClient.FileStoreIndex_Get(file2Get.Id));

                    return new FileObjectModel
                    {
                        FileRef = populatedFile.FileRef,
                        ContentType = populatedFile.MimeType,
                        Description = $"{populatedFile.Description} {(populatedFile.VersionInfo > 0 ? $"(Version: {populatedFile.VersionInfo})" : string.Empty)}",
                        Name = $"{populatedFile.OrginalNameWithExt}",
                        Data = populatedFile.Data,
                    }; ;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public List<InternalFileModel> Files_GetAllV2(string relationalFilePath, bool recursiveSearch = false)
        {
            if (!string.IsNullOrEmpty(_appPrefix) &&
                !relationalFilePath.DropFirstChar(new char[] { '\\', '|', '/' }).StartsWith(_appPrefix) &&
                PathExtension.IsRelativePath(relationalFilePath))
            {
                return _dbClient.FileStoreIndex_GetAllByLocation(pathToSearch: $"{_appPrefix}{SystemConstants.InternalDirectorySeparator}{relationalFilePath}", recursiveSearch);
            }
            else
            {
                return _dbClient.FileStoreIndex_GetAllByLocation(pathToSearch: relationalFilePath, recursiveSearch);
            }
        }

        /// <inheritdoc />
        public async Task<bool> File_DeleteAsync(string fileRef)
        {
            try
            {
                // Ensure the Id was def entered
                if (!string.IsNullOrEmpty(fileRef))
                {
                    // Ensure we dont have more versions of the file than permitted.
                    List<InternalFileModel> fileCopies = File_GetVersionsInfo(fileRef);
                    if (fileCopies?.Count > 0)
                    {
                        for (int i = 0; i < fileCopies.Count; i++)
                        {
                            _ = await Internal_DeleteFileAsync(fileCopies[i], removeDbReference: true, deleteAllIfBox: true);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            // All is right with the world!
            return true;
        }

        /// <inheritdoc/>
        public List<InternalFileModel> File_GetVersionsInfo(string fileRef)
        {
            // Ensure the Id was def entered
            if (!string.IsNullOrEmpty(fileRef))
            {
                // Ensure we dont have more versions of the file than permitted.
                return _dbClient.FileStoreIndex_GetAllByRef(fileRef);
            }

            return default;
        }

        /// <inheritdoc/>
        public async Task<ResultModel> File_Migrate(string fileRef, FileStorageProvider newLocation, bool moveFile = true)
        {
            // Pick the client based on target.
            FileStorageProvider saveTo = newLocation == FileStorageProvider.UseDefault ? _defaultStorage : newLocation;
            bool overallStatus = true;
            string modeText = moveFile ? "Moving" : "Copying";

            List<string> messages = new();

            // Move all copies of the file to the new location
            // Reversing the order to oldest first to ensure they migrate in the correct order
            var getFileVersions = File_GetVersionsInfo(fileRef).OrderBy(x => x.Id).ToList();
            if (getFileVersions?.Count > 0)
            {
                foreach (var existingFile in getFileVersions)
                {
                    // Friendly string
                    string fileInfo = $"{existingFile.OrginalNameNoExt} (fileRef: {fileRef} / id: {existingFile.Id})";

                    // If its not in the desired location then we need to move it
                    if (existingFile.FileLocation != (int)saveTo)
                    {
                        try
                        {
                            // Fetch the binary from the endProvider, and save to new location
                            var fetchFile = new InternalFileModel(await Internal_GetFileAsync(existingFile));
                            long sourceFileSize = fetchFile.Data.LongLength;

                            InternalFileModel updatedFile = await Internal_SaveDataToProviderAsync(
                                fileModel: fetchFile,
                                saveToLocation: saveTo);

                            var verifyFetchFile = new InternalFileModel(await Internal_GetFileAsync(updatedFile));

                            if (verifyFetchFile.Data.LongLength != sourceFileSize)
                            {
                                // Throw error fetching file
                                throw new Exception($"Size was {sourceFileSize} now {verifyFetchFile.Data.LongLength}");
                            }

                            if (moveFile && verifyFetchFile.Data.LongLength == sourceFileSize)
                            {
                                // Delete the file from Prior Location
                                _ = await Internal_DeleteFileAsync(existingFile, removeDbReference: false);
                            }

                            messages.Add($"Success: {modeText} file {fileInfo} to {saveTo}");
                        }
                        catch (Exception ex)
                        {
                            overallStatus = false;
                            messages.Add($"Failed: {modeText} {fileInfo}, to {saveTo}, Error: {ex.Message}");

                            if (ex.Message.Contains(offlineWarning))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        messages.Add($"NoAction: {fileInfo} already in {saveTo}");
                    }
                }
            }

            return new ResultModel { Success = overallStatus, Messages = messages };
        }

        /// <inheritdoc/>
        public async Task<ResultModel> File_IndexAsync(FileStorageProvider indexLocation, string indexFrom = "", bool scopeToAppPrefix = true)
        {
            if (indexLocation == FileStorageProvider.Database)
            {
                return new ResultModel
                {
                    Success = false,
                    Messages = new List<string> { "Database Location is not supported" },
                };
            }

            // Resolve correct FileStoreClient.
            IClient? storeClient = Internal_GetClient(indexLocation);
            if (storeClient?.HasInit == true)
            {
                // Set a start time and reset a few things
                DateTime startTime = DateTime.Now;
                fileIndex.CollectionChanged += FileIndex_CollectionChanged;
                NewlyAddedFiles = 0;

                // Kick off indexing
                long discoveredFiles = await storeClient.IndexContentsAsync(fileIndex, indexFrom, scopeToAppPrefix);

                // Returning results.
                return new ResultModel
                {
                    Success = NewlyAddedFiles == discoveredFiles,
                    Messages = new List<string>
                {
                    $"Indexed {NewlyAddedFiles}x new files in {indexLocation}, scaned in {(DateTime.Now - startTime).TotalMinutes}mins!"
                }
                };
            }

            return new ResultModel
            {
                Success = false,
                Messages = new List<string> { $"{indexLocation} Provider is not available, please check config!" },
            };
        }

        /// <inheritdoc/>
        public async Task<ResultModel> File_MoveAsync(string fileRef, string[] pathTags)
        {
            var relationalFilePathAndName = $"{string.Join(SystemConstants.InternalDirectorySeparator.ToString(), pathTags)}";
            return await File_MoveAsync(fileRef, relationalFilePathAndName);
        }

        /// <inheritdoc/>
        public async Task<ResultModel> File_MoveAsync(string fileRef, string relationalFilePathAndName)
        {
            bool overallStatus = true;
            List<string> messages = new();
            PathInfo pathInfo = relationalFilePathAndName.GetPathInfo(addRootFolderPrefix: _appPrefix);

            // Get all versions of the file in ascending order (oldest first)
            var getFileVersions = File_GetVersionsInfo(fileRef).OrderBy(x => x.Id).ToList();
            if (getFileVersions?.Count > 0)
            {
                foreach (var existingFile in getFileVersions)
                {
                    string fileInfo = $"{existingFile.OrginalNameNoExt} (fileRef: {fileRef} / id: {existingFile.Id})";

                    try
                    {
                        // Fetch the binary from the existing location and save it to the new location
                        var fetchFile = await Internal_GetFileAsync(existingFile);
                        var newFile = new InternalFileModel
                        {
                            Created = fetchFile.Created,
                            MimeType = pathInfo.MimeType,
                            Extension = pathInfo.FileExtension,
                            Name = pathInfo.FilenameNoExtension,
                            Description = fetchFile.Description,
                            FileLocation = fetchFile.FileLocation,
                            PathTags = pathInfo.PathTags,
                            ExtClientRef = relationalFilePathAndName,
                            FileRef = fileRef,
                            Data = fetchFile.Data,
                        };
                        long sourceFileSize = newFile.Data.LongLength;

                        InternalFileModel updatedFile = await Internal_SaveDataToProviderAsync(
                            fileModel: newFile,
                            saveToLocation: FileStorageProviderExts.GetProvider(newFile.FileLocation)
                        );

                        var verifyFetchFile = new InternalFileModel(await Internal_GetFileAsync(updatedFile));

                        if (verifyFetchFile.Data.LongLength != sourceFileSize)
                        {
                            // Throw error fetching file
                            throw new Exception($"Size was {sourceFileSize} now {verifyFetchFile.Data.LongLength}");
                        }

                        if (verifyFetchFile.Data.LongLength == sourceFileSize)
                        {
                            // Delete the file from the original location
                            await Internal_DeleteFileAsync(existingFile, removeDbReference: true);
                        }

                        messages.Add($"Success: file {fileInfo} to {relationalFilePathAndName}");
                    }
                    catch (Exception ex)
                    {
                        overallStatus = false;
                        messages.Add($"Failed: {fileInfo}, to {relationalFilePathAndName}, Error: {ex.Message}");

                        if (ex.Message.Contains(offlineWarning))
                        {
                            break;
                        }
                    }
                }
            }

            return new ResultModel { Success = overallStatus, Messages = messages };
        }
        
        private long NewlyAddedFiles { get; set; } = 0;

        /// <inheritdoc />
        private ObservableCollection<InternalFileModel> fileIndex = new();

        private void FileIndex_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    // InsertRange triggers a reset
                    if (fileIndex?.Count > 0)
                    {
                        NewlyAddedFiles += Internal_UpsertIndexAsync(fileIndex);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    // .Add triggers, add! :)
                    // InsertRange triggers a reset
                    if (fileIndex?.Count > 0)
                    {
                        NewlyAddedFiles += Internal_UpsertIndexAsync(fileIndex);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                default:
                    break;
            }
        }
        
        #endregion

        #region Will Speak to binary providers only
        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="fileObject">The file to save, ensure the file is saved as byte[] to Data</param>
        /// <param name="targetStorageProvider">Where to store file, current options FileSystem or Database</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        private async Task<string> Internal_UpsertAsync(InternalFileModel fileObject, FileStorageProvider targetStorageProvider = FileStorageProvider.UseDefault)
        {
            // Pick the client based on target.
            FileStorageProvider saveTo = targetStorageProvider == FileStorageProvider.UseDefault ? _defaultStorage : targetStorageProvider;

            // if files exist with same name, extension and fullFilePath, then consider it the same file,
            // Its upto the write2file etc to store as versioned
            var existingFilesList = Files_GetAllV2(
                relationalFilePath: fileObject.PathTags.SetPathSeparator(SystemConstants.InternalDirectorySeparator),
                recursiveSearch: false)
                .Where(x =>
                x.Extension.Equals(fileObject.Extension, StringComparison.InvariantCultureIgnoreCase) &&
                x.Name.StartsWith(fileObject.Name, StringComparison.InvariantCultureIgnoreCase));

            // Have we got existing files?
            if (existingFilesList?.Count() > 0)
            {
                fileObject.FileRef = existingFilesList.FirstOrDefault().FileRef;

                int maxFileId = 1;
                // Figure out highestId
                foreach (var existingFile in existingFilesList)
                {
                    string[] parts = existingFile.Name.Split(new string[] { _versionFlag }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        _ = Int32.TryParse(s: parts[1], out int existingMaxId);
                        if (existingMaxId >= maxFileId)
                        {
                            maxFileId = existingMaxId + 1;
                        }
                    }

                }

                // Rewrite with version increment
                fileObject.Name = $"{fileObject.Name}{_versionFlag}{maxFileId}";
            }
            else
            {
                //Assign unique fileRef code
                fileObject.FileRef = Internal_FindNewFileRef();
            }

            // Save the file to the endProvider and create the db record.
            fileObject = await Internal_SaveDataToProviderAsync(fileObject, saveTo);

            if (fileObject.Id > 0)
            {
                // Ensure the Id was def entered
                string fileRef = _dbClient.FileStoreIndex_GetFileRef(fileObject.Id);
                if (!string.IsNullOrEmpty(fileRef))
                {
                    // Ensure we dont have more versions of the file than permitted.
                    List<InternalFileModel> fileCopies = _dbClient.FileStoreIndex_GetAllByRef(fileRef);
                    if (fileCopies?.Count > _maxVersions)
                    {
                        for (int i = _maxVersions; i < fileCopies.Count; i++)
                        {
                            // Prune versions beyond, max copies
                            var file2Delete = fileCopies[i];
                            _ = await Internal_DeleteFileAsync(file2Delete, removeDbReference: true, deleteAllIfBox: false);
                        }
                    }

                    // All is right with the world, return inserted id
                    return fileRef;
                }
            }

            return string.Empty;
        }

        private long Internal_UpsertIndexAsync(ObservableCollection<InternalFileModel> fileCollection)
        {
            long newIndexes = 0;
            var indexcollection = fileCollection.ToList();

            if (indexcollection?.LongCount() > 0)
            {
                foreach (var fileRecord in indexcollection)
                {
                    // if files exist with same name, extentsion and groups, then consider it the same file,
                    // Its upto the write2file etc to store as versioned
                    var existingFilesList = Files_GetAllV2(fileRecord.PathTags).Where(x =>
                        x.Extension.Equals(fileRecord.Extension, StringComparison.InvariantCultureIgnoreCase) &&
                        x.Name.StartsWith(fileRecord.Name, StringComparison.InvariantCultureIgnoreCase));

                    // Have we got existing files?
                    if (existingFilesList?.Count() > 0)
                    {
                        // Skip to next as we already have a record of this item.
                        continue;
                    }
                    else
                    {
                        // Create a brand new fileRecord in the FileStoreIndex for this file..
                        fileRecord.FileRef = Internal_FindNewFileRef();
                        long result = _dbClient.FileStoreIndex_Upsert(fileRecord);
                        if (result > 0)
                        {
                            newIndexes++;
                        }
                    }
                }

                fileCollection.Clear();
            }

            return newIndexes;
        }

        private string Internal_FindNewFileRef()
        {
            // No existing files find a new Id
            string generatedFileRef = "";
            do
            {
                generatedFileRef = $"{_filePrefix}{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
            } while (!_dbClient.FileStoreIndex_IsFileRefUnique(generatedFileRef));

            return generatedFileRef;
        }

        private bool Internal_StartupCheck()
        {
            if (_dbClient == null || !_dbClient.IsReady || !string.IsNullOrEmpty(_dbClient.DbClientErrorMessage))
            {
                throw new Exception($"Database Not Available, Check Connection String, you may need to add 'TrustServerCertificate=True;', internal errors: {_dbClient.DbClientErrorMessage}");
            }

            return true;
        }


        #endregion

        #region Internal BinaryWrappers

        /// <summary>
        /// Save the file to the chosen end provider, if its db it will insert the record
        /// </summary>
        /// <param name="fileModel"></param>
        /// <param name="saveToLocation"></param>
        /// <returns></returns>
        private async Task<InternalFileModel> Internal_SaveDataToProviderAsync(InternalFileModel fileModel, FileStorageProvider saveToLocation)
        {
            fileModel.SizeInBytes = fileModel.Data?.LongLength ?? -1;

            if (saveToLocation != FileStorageProvider.Database)
            {
                if (saveToLocation == FileStorageProvider.Box
                    && fileModel.Data?.LongLength >= 52428800)
                {
                    // Need to implement this into the box client
                    // https://developer.box.com/reference/post-files-upload-sessions/
                    throw new Exception("Exceeds currently supported 50MB max filesize for box, will implement soon");
                }

                // Resolve correct FileStoreClient.
                IClient? storeClient = Internal_GetClient(saveToLocation);
                if (storeClient?.HasInit == true)
                {
                    fileModel.ExtClientRef = await storeClient.SaveFileAsync(fileModel);
                }
                else
                {
                    throw new Exception($"FileStorageProvider: {saveToLocation}, {offlineWarning}");
                }

                // Ensure db doesnt have the file in the record
                fileModel.Data = null;
            }
            else if (saveToLocation == FileStorageProvider.Database)
            {
                // Wipe the ExtClientRef location just weird otherwise!
                fileModel.ExtClientRef = null;
            }

            // Update record, with where the binary was savedTo.
            fileModel.FileLocation = (int)saveToLocation;

            // Upsert fileRecord to the FileStoreIndex
            fileModel.Id = _dbClient.FileStoreIndex_Upsert(fileModel);

            return fileModel;
        }

        /// <summary>
        /// Removes the binary from the provider store.
        /// </summary>
        /// <param name="fileModel"></param>
        /// <param name="removeDbReference"></param>
        /// <returns></returns>
        private async Task<bool> Internal_DeleteFileAsync(InternalFileModel fileModel, bool removeDbReference = true, bool deleteAllIfBox = false)
        {
            // If Non-Db Location
            if (fileModel.FileLocation != (int)FileStorageProvider.Database)
            {
                // Pick the client based on where file is stored
                IClient? storeClient = Internal_GetClient(fileModel.FileLocation);
                if (storeClient?.HasInit == true)
                {
                    string deleteRef = deleteAllIfBox && fileModel.FileLocation == (int)FileStorageProvider.Box ? fileModel.ExtClientRef.Split('/')[0] : fileModel.ExtClientRef;
                    _ = await storeClient.DeleteFileAsync(deleteRef);
                }
            }

            // If in db, get the FileAgain, could have been changed, wipe the data and save it
            if (fileModel.FileLocation == (int)FileStorageProvider.Database && !removeDbReference)
            {
                var fileRecord = _dbClient.FileStoreIndex_Get(fileModel.Id);
                fileRecord.Data = null;
                _ = _dbClient.FileStoreIndex_Upsert(fileRecord);
            }
            // If we've been asked to remove reference it'll wipe file in db anyway
            else if (removeDbReference)
            {
                _ = _dbClient.FileStoreIndex_Delete(fileModel.Id);
            }

            return true;
        }

        /// <summary>
        /// Used to populate the exact record.
        /// </summary>
        /// <param name="fileModel"></param>
        /// <returns></returns>
        private async Task<InternalFileModel> Internal_GetFileAsync(InternalFileModel fileModel)
        {
            // Pick the client based on where file is stored
            if (fileModel.FileLocation != (int)FileStorageProvider.Database)
            {
                IClient? storeClient = Internal_GetClient(fileModel.FileLocation);
                if (storeClient?.HasInit == true)
                {
                    fileModel.Data = (await storeClient.GetFileAsync(fileModel.ExtClientRef)).Data;
                }
                else
                {
                    throw new Exception($"StoreClient: {FileStorageProviderExts.GetProvider(fileModel.FileLocation)}: Is not ready - please check config.");
                }
            }

            else if (fileModel.FileLocation == (int)FileStorageProvider.Database)
            {
                fileModel = _dbClient.FileStoreIndex_Get(fileModel.Id);
            }

            return fileModel;
        }

        private IClient? Internal_GetClient(FileStorageProvider storeType) =>
                   Internal_GetClient((int)storeType);

        private IClient? Internal_GetClient(int storeTypeId)
        {
            StoreProviderDef? factory = StoreProvidersList.FirstOrDefault(x => x.StoreTypeId == storeTypeId);
            return factory?.GetClient();
        }
        #endregion
    }
}
