using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Netigent.Utils.FileStoreIO.Clients;
using Netigent.Utils.FileStoreIO.Dal;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Helpers;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO
{
    public class FileStoreIOClient : IFileStoreIOClient
    {
        #region public props
        public bool IsReady { get; internal set; } = false;
        public bool FileSystemStorageAvailable { get; internal set; } = false;
        public List<string> Messages { get; internal set; } = new();

        #endregion

        #region internal settings
        private readonly string _fileStoreRoot;
        private readonly InternalDatabaseClient _dbClient;
        private readonly string _filePrefix;
        private readonly bool _useUniqueName;
        private readonly FileStorageProvider _defaultStorage;
        private readonly int _maxVersions;
        private readonly BoxConfig? _boxConfig;

        private const string _notSpecifiedFlag = "_$";
        private const string _versionFlag = "__ver_";
        private const string _notSpecifiedSchema = "dbo";

        private readonly IClient _boxClient;
        private readonly IClient _uncClient;
        #endregion

        #region ctor
        /// <summary>
        /// FileIOClient using FileIOConfig
        /// </summary>
        /// <param name="fileIOConfig"></param>
        public FileStoreIOClient(IOptions<FileStoreIOConfig> fileIOConfig)
        {
            _dbClient = new InternalDatabaseClient(fileIOConfig.Value.Database, fileIOConfig.Value.DatabaseSchema);
            _fileStoreRoot = fileIOConfig.Value.FileStoreRoot;
            _defaultStorage = fileIOConfig.Value.DefaultStorage;
            _filePrefix = fileIOConfig.Value.FilePrefix ?? _notSpecifiedFlag;
            _useUniqueName = fileIOConfig.Value.StoreFileAsUniqueRef;
            _maxVersions = fileIOConfig.Value.MaxVersions > 1 ? fileIOConfig.Value.MaxVersions : 1;

            _boxConfig = fileIOConfig.Value.Box;

            _boxClient = new BoxClient(_boxConfig, _maxVersions);
            _uncClient = new FileSystemClient(_fileStoreRoot);

            Startup(out string fileSystemErrorMessage);

            if (string.IsNullOrEmpty(fileSystemErrorMessage) && string.IsNullOrEmpty(_dbClient.DbClientErrorMessage))
                IsReady = true;

            else
            {
                if (!string.IsNullOrEmpty(_dbClient.DbClientErrorMessage))
                    Messages.Add(_dbClient.DbClientErrorMessage);

                if (!string.IsNullOrEmpty(fileSystemErrorMessage))
                    Messages.Add(fileSystemErrorMessage);
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
            string databaseConnection,
            string fileStoreRoot,
            string filePrefix = _notSpecifiedFlag,
            string dbSchema = _notSpecifiedSchema,
            bool useUniqueRef = true,
            int maxVersions = 1,
            FileStorageProvider defaultFileStore = FileStorageProvider.Database,
            BoxConfig? boxConfig = null)
        {
            //Create the filestore client
            _dbClient = new InternalDatabaseClient(databaseConnection, dbSchema);
            _fileStoreRoot = fileStoreRoot;
            _filePrefix = filePrefix;
            _useUniqueName = useUniqueRef;
            _defaultStorage = defaultFileStore;
            _maxVersions = maxVersions > 1 ? maxVersions : 1;

            _boxConfig = boxConfig;

            _boxClient = new BoxClient(boxConfig, _maxVersions);
            _uncClient = new FileSystemClient(_fileStoreRoot);

            Startup(out string fileSystemErrorMessage);

            if (string.IsNullOrEmpty(fileSystemErrorMessage) && string.IsNullOrEmpty(_dbClient.DbClientErrorMessage))
                IsReady = true;

            else
            {
                if (!string.IsNullOrEmpty(_dbClient.DbClientErrorMessage))
                    Messages.Add(_dbClient.DbClientErrorMessage);

                if (!string.IsNullOrEmpty(fileSystemErrorMessage))
                    Messages.Add(fileSystemErrorMessage);
            }
        }
        #endregion

        #region Implementations

        /// <inheritdoc />
        public async Task<string> File_UpsertAsync(byte[] fileContents, string fullFilename, FileStorageProvider fileLocation = FileStorageProvider.UseDefault, string description = "", string mainGroup = "", string subGroup = "", DateTime created = default)
        {

            var fileName = Path.GetFileNameWithoutExtension(fullFilename);
            var extension = Path.GetExtension(fullFilename);
            var mimeType = MimeHelper.GetMimeType(extension);

            var createdDate = created == default ? DateTime.UtcNow : created;

            var fileModel = new InternalFileModel
            {
                Created = createdDate,
                MimeType = mimeType,
                Extension = extension,
                Name = fileName,
                Description = description ?? fileName,
                FileLocation = fileLocation == FileStorageProvider.UseDefault ? (int)_defaultStorage : (int)fileLocation,
                MainGroup = mainGroup,
                SubGroup = subGroup,
                Data = fileContents,
            };

            return await File_UpsertAsync(fileModel, fileLocation);
        }

        /// <inheritdoc />
        public async Task<string> File_UpsertAsync(IFormFile file, FileStorageProvider fileLocation = FileStorageProvider.UseDefault, string description = "", string mainGroup = "", string subGroup = "")
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var fileModel = new InternalFileModel
            {
                Created = DateTime.UtcNow,
                MimeType = file.ContentType,
                Extension = extension,
                Name = fileName,
                Description = description ?? fileName,
                FileLocation = fileLocation == FileStorageProvider.UseDefault ? (int)_defaultStorage : (int)fileLocation,
                MainGroup = mainGroup,
                SubGroup = subGroup
            };

            using (var dataStream = new MemoryStream())
            {
                await file.CopyToAsync(dataStream);
                fileModel.Data = dataStream.ToArray();
            }

            return await File_UpsertAsync(fileModel, fileLocation);
        }

        /// <inheritdoc />
        public async Task<FileObjectModel> File_Get(string fileRef, int priorVersion = 0)
        {
            //Grab the fileRecord and if stored in database (this doesnt get the binary in the dataField)
            var fileList = _dbClient.FileStore_GetAllByRef(fileRef);

            // If we have files and the version we're being asked for doesnt exceed the array
            if (fileList?.Count > 0 && fileList?.Count > priorVersion)
            {
                //Try and get the file from index
                InternalFileModel? file2Get = fileList[priorVersion];

                if (file2Get != null)
                {
                    // Pull specific file by id
                    // If this is a DB record it will now have the data
                    InternalFileModel populatedFile = await GetFileAsync(_dbClient.FileStore_Get(file2Get.Id));

                    return new FileObjectModel
                    {
                        FileRef = populatedFile.FileRef,
                        ContentType = populatedFile.MimeType,
                        Description = $"{populatedFile.Description} {(populatedFile.VersionInfo > 0 ? $"(Version: {populatedFile.VersionInfo})" : string.Empty)}",
                        Name = $"{populatedFile.RawName}{populatedFile.Extension}",
                        Data = populatedFile.Data,
                    }; ;
                }
            }

            return null;
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
                            _ = await DeleteFileAsync(fileCopies[i], removeDbReference: true, deleteAllIfBox: true);
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
                return _dbClient.FileStore_GetAllByRef(fileRef);
            }

            return default;
        }

        /// <inheritdoc/>
        public List<InternalFileModel> Files_GetAll(string mainGroup = "", string subGroup = "") => _dbClient.FileStore_GetAllByLocation(mainGroup, subGroup);

        /// <inheritdoc/>
        public async Task<ResultModel> File_Migrate(string fileRef, FileStorageProvider newLocation)
        {
            bool overallStatus = true;

            List<string> messages = new();

            // Move all copies of the file to the new location
            // Reversing the order to oldest first to ensure they migrate in the correct order
            var getFileVersions = File_GetVersionsInfo(fileRef).OrderBy(x => x.Id).ToList();
            if (getFileVersions?.Count > 0)
            {
                foreach (var existingFile in getFileVersions)
                {
                    // Friendly string
                    string fileInfo = $"{existingFile.RawName} (fileRef: {fileRef} / id: {existingFile.Id})";

                    // If its not in the desired location then we need to move it
                    if (existingFile.FileLocation != (int)newLocation)
                    {
                        try
                        {
                            // Fetch the binary from the endProvider, and save to new location
                            InternalFileModel updatedFile = await SaveFileAsync(
                                fileModel: new InternalFileModel(
                                    await GetFileAsync(existingFile)
                                    ),
                                saveToLocation: newLocation);

                            // Delete the file from Prior Location
                            _ = await DeleteFileAsync(existingFile, removeDbReference: false);

                            messages.Add($"Moved {fileInfo} to {newLocation}");
                        }
                        catch (Exception ex)
                        {
                            overallStatus = false;
                            messages.Add($"Failed {fileInfo}, to {newLocation}, Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        messages.Add($"NoAction: {fileInfo} already in {newLocation}");
                    }
                }
            }

            return new ResultModel { Success = overallStatus, Message = messages };
        }
        #endregion

        #region Will Speak to binary providers only
        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="fileObject">The file to save, ensure the file is saved as byte[] to Data</param>
        /// <param name="storageType">Where to store file, current options FileSystem or Database</param>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        private async Task<string> File_UpsertAsync(InternalFileModel fileObject, FileStorageProvider storageType)
        {
            // if files exist with same name, extentsion and groups, then consider it the same file,
            // Its upto the write2file etc to store as versioned
            var existingFilesList = Files_GetAll(fileObject.MainGroup, fileObject.SubGroup).Where(x =>
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

                    //                     maxFileId = existingFile.VersionInfo + 1;

                }

                // Rewrite with version increment
                fileObject.Name = $"{fileObject.Name}{_versionFlag}{maxFileId}";
            }
            else
            {
                //Assign unique fileRef code
                fileObject.FileRef = FileStore_FindNewFileRef();
            }

            // Save the file to the endProvider and create the db record.
            fileObject = await SaveFileAsync(fileObject, storageType);

            if (fileObject.Id > 0)
            {
                // Ensure the Id was def entered
                string fileRef = _dbClient.FileStore_GetFileRef(fileObject.Id);
                if (!string.IsNullOrEmpty(fileRef))
                {
                    // Ensure we dont have more versions of the file than permitted.
                    List<InternalFileModel> fileCopies = _dbClient.FileStore_GetAllByRef(fileRef);
                    if (fileCopies?.Count > _maxVersions)
                    {
                        for (int i = _maxVersions; i < fileCopies.Count; i++)
                        {
                            // Prune versions beyond, max copies
                            var file2Delete = fileCopies[i];
                            _ = await DeleteFileAsync(file2Delete, removeDbReference: true, deleteAllIfBox: false);
                        }
                    }

                    // All is right with the world, return inserted id
                    return fileRef;
                }
            }

            return string.Empty;
        }

        private string FileStore_FindNewFileRef()
        {
            // No existing files find a new Id
            string generatedFileRef = "";
            do
            {
                generatedFileRef = $"{_filePrefix}{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
            } while (!_dbClient.FileStore_IsFileRefUnique(generatedFileRef));

            return generatedFileRef;
        }

        private bool Startup(out string fileSystemErrors)
        {
            if (string.IsNullOrEmpty(_fileStoreRoot))
                FileSystemStorageAvailable = false;

            //Test file to check access to the file system
            string testFile = Path.Combine(_fileStoreRoot, "TestFile90u34gj93p4n3p9wcfp3h4pc9qf.txt");
            try
            {

                fileSystemErrors = string.Empty;
                if (!Directory.Exists(_fileStoreRoot))
                    Directory.CreateDirectory(_fileStoreRoot);

                if (System.IO.File.Exists(testFile))
                    System.IO.File.Delete(testFile);
                else
                {
                    System.IO.File.CreateText(testFile).Dispose();
                    System.IO.File.Delete(testFile);
                }

                FileSystemStorageAvailable = true;
                return true;
            }
            catch (Exception ex)
            {
                fileSystemErrors = ex.Message;
                FileSystemStorageAvailable = false;
                return false;
            }
        }

        private IClient GetClient(int provider)
        {
            switch (provider)
            {
                case (int)FileStorageProvider.FileSystem:
                    return _uncClient;

                case (int)FileStorageProvider.Box:
                    return _boxClient;
                default:

                    return null;
            }
        }

        private IClient GetClient(FileStorageProvider provider)
        {
            switch (provider)
            {
                case FileStorageProvider.FileSystem:
                    return _uncClient;

                case FileStorageProvider.Box:
                    return _boxClient;
                default:

                    return null;
            }
        }
        #endregion

        #region BinaryWrappers

        /// <summary>
        /// Save the file to the chosen end provider, if its db it will insert the record
        /// </summary>
        /// <param name="fileModel"></param>
        /// <param name="saveToLocation"></param>
        /// <returns></returns>
        private async Task<InternalFileModel> SaveFileAsync(InternalFileModel fileModel, FileStorageProvider saveToLocation)
        {
            // Pick the client based on target.
            FileStorageProvider saveTo = saveToLocation == FileStorageProvider.UseDefault ? _defaultStorage : saveToLocation;



            if (saveTo != FileStorageProvider.Database)
            {
                if (saveTo == FileStorageProvider.Box
                    && fileModel.Data?.LongLength >= 52428800)
                {
                    // Need to implement this into the box client
                    // https://developer.box.com/reference/post-files-upload-sessions/
                    throw new Exception("Exceeds currently supported 50MB max filesize for box, will implement soon");
                }

                var client = GetClient(saveTo);
                if (client != null)
                {
                    fileModel.FilePath = await client.SaveFileAsync(fileModel);

                }

                // Ensure db doesnt have the file in the record
                fileModel.Data = null;
            }
            else if (saveTo == FileStorageProvider.Database)
            {
                // Wipe the filePath just weird otherwise!
                fileModel.FilePath = null;
            }

            // Save binary to the database?
            fileModel.FileLocation = (int)saveTo;
            fileModel.Id = _dbClient.FileStore_Upsert(fileModel);

            return fileModel;
        }

        /// <summary>
        /// Removes the binary from the provider store.
        /// </summary>
        /// <param name="fileModel"></param>
        /// <param name="removeDbReference"></param>
        /// <returns></returns>
        private async Task<bool> DeleteFileAsync(InternalFileModel fileModel, bool removeDbReference = true, bool deleteAllIfBox = false)
        {
            // If Non-Db Location
            if (fileModel.FileLocation != (int)FileStorageProvider.Database)
            {
                // Pick the client based on where file is stored
                var client = GetClient(fileModel.FileLocation);
                if (client != null)
                {
                    string deleteRef = deleteAllIfBox && fileModel.FileLocation == (int)FileStorageProvider.Box ? fileModel.FilePath.Split('/')[0] : fileModel.FilePath;
                    _ = await client.DeleteFileAsync(deleteRef);
                }
            }

            // If in db, get the FileAgain, could have been changed, wipe the data and save it
            if (fileModel.FileLocation == (int)FileStorageProvider.Database && !removeDbReference)
            {
                var fileRecord = _dbClient.FileStore_Get(fileModel.Id);
                fileRecord.Data = null;
                _ = _dbClient.FileStore_Upsert(fileRecord);
            }
            // If we've been asked to remove reference it'll wipe file in db anyway
            else if (removeDbReference)
            {
                _ = _dbClient.FileStore_Delete(fileModel.Id);
            }

            return true;
        }

        /// <summary>
        /// Used to populate the exact record.
        /// </summary>
        /// <param name="fileModel"></param>
        /// <returns></returns>
        private async Task<InternalFileModel> GetFileAsync(InternalFileModel fileModel)
        {
            // Pick the client based on where file is stored
            if (fileModel.FileLocation != (int)FileStorageProvider.Database)
            {
                var client = GetClient(fileModel.FileLocation);
                if (client != null)
                {
                    try
                    {
                        fileModel.Data = (await client.GetFileAsync(fileModel.FilePath)).Data;
                    }
                    catch { }
                }
            }
            else if (fileModel.FileLocation == (int)FileStorageProvider.Database)
            {
                fileModel = _dbClient.FileStore_Get(fileModel.Id);
            }

            return fileModel;
        }

        #endregion
    }
}
