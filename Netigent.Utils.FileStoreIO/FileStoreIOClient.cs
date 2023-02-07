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
        private readonly InternalDatabaseClient _ioClient;
        private readonly string _filePrefix;
        private readonly bool _useUniqueName;
        private readonly int _maxVersions;
        private readonly BoxConfig? _boxConfig;

        private const string _notSpecifiedFlag = "_$";
        private const string _versionFlag = "__ver_";
        private const string _notSpecifiedSchema = "dbo";
        private const string _fileFlag = @":\";
        private const string _internetFlag = @"://";
        private const string _networkFlag = @"\\";
        #endregion

        #region ctor
        /// <summary>
        /// FileIOClient using FileIOConfig
        /// </summary>
        /// <param name="fileIOConfig"></param>
        public FileStoreIOClient(IOptions<FileStoreIOConfig> fileIOConfig)
        {
            _ioClient = new InternalDatabaseClient(fileIOConfig.Value.Database, fileIOConfig.Value.DatabaseSchema);
            _fileStoreRoot = fileIOConfig.Value.FileStoreRoot;
            _filePrefix = fileIOConfig.Value.FilePrefix ?? _notSpecifiedFlag;
            _useUniqueName = fileIOConfig.Value.StoreFileAsUniqueRef;
            _maxVersions = fileIOConfig.Value.MaxVersions > 1 ? fileIOConfig.Value.MaxVersions : 1;

            _boxConfig = fileIOConfig.Value.Box;

            Startup(out string fileSystemErrorMessage);

            if (string.IsNullOrEmpty(fileSystemErrorMessage) && string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
                IsReady = true;

            else
            {
                if (!string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
                    Messages.Add(_ioClient.DbClientErrorMessage);

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
            _ioClient = new InternalDatabaseClient(databaseConnection, dbSchema);
            _fileStoreRoot = fileStoreRoot;
            _filePrefix = filePrefix;
            _useUniqueName = useUniqueRef;
            _maxVersions = maxVersions > 1 ? maxVersions : 1;

            _boxConfig = boxConfig;

            Startup(out string fileSystemErrorMessage);

            if (string.IsNullOrEmpty(fileSystemErrorMessage) && string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
                IsReady = true;

            else
            {
                if (!string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
                    Messages.Add(_ioClient.DbClientErrorMessage);

                if (!string.IsNullOrEmpty(fileSystemErrorMessage))
                    Messages.Add(fileSystemErrorMessage);
            }
        }
        #endregion

        #region interface implementations

        /// <inheritdoc />
        public string File_Upsert(byte[] fileContents, string fullFilename, FileStorageProvider fileLocation = FileStorageProvider.Database, string description = "", string mainGroup = "", string subGroup = "", DateTime created = default)
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
                FileLocation = (int)fileLocation,
                MainGroup = mainGroup,
                SubGroup = subGroup,
                Data = fileContents,
            };

            return File_Upsert(fileModel, fileLocation, mainGroup, subGroup);
        }

        /// <inheritdoc />
        public async Task<string> File_Upsert(IFormFile file, FileStorageProvider fileLocation = FileStorageProvider.Database, string description = "", string mainGroup = "", string subGroup = "")
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
                FileLocation = (int)fileLocation,
                MainGroup = mainGroup,
                SubGroup = subGroup
            };

            using (var dataStream = new MemoryStream())
            {
                await file.CopyToAsync(dataStream);
                fileModel.Data = dataStream.ToArray();
            }

            return File_Upsert(fileModel, fileLocation, mainGroup, subGroup);
        }

        /// <inheritdoc />
        public async Task<FileObjectModel> File_Get(string fileRef, int priorVersion = 0)
        {
            //Grab the fileRecord and if stored in database get the binary also
            var fileList = _ioClient.FileStore_GetAllByRef(fileRef);

            // If we have files and the version we're being asked for doesnt exceed the array
            if (fileList?.Count > 0 && fileList?.Count > priorVersion)
            {
                //Try and get the file from index
                InternalFileModel? file2Get = fileList[priorVersion];

                if (file2Get != null)
                {
                    // Pull specific file by id
                    // If this is a DB record it will now have the data
                    InternalFileModel file = _ioClient.FileStore_Get(file2Get.Id);

                    // Chop off version info
                    string[] fileNameParts = file.Name.Split(new string[] { _versionFlag }, StringSplitOptions.RemoveEmptyEntries);
                    string versionInfo = fileNameParts.Length > 1 ? $" (Version: {fileNameParts[1]})" : string.Empty;

                    // Build file for output
                    FileObjectModel outputFile = new()
                    {
                        FileRef = file.FileRef,
                        ContentType = file.MimeType,
                        Description = $"{file.Description}{versionInfo}",
                        Name = $"{fileNameParts[0]}{file.Extension}"
                    };

                    // Find the file data for the output
                    switch (file.FileLocation)
                    {
                        case (int)FileStorageProvider.FileSystem:
                            string absoluteFilePath = GetAbsoluteFilePath(file.FilePath);

                            if (!File.Exists(absoluteFilePath))
                                return null;

                            //Get from filesystem
                            var memory = new MemoryStream();
                            using (var stream = new FileStream(absoluteFilePath, FileMode.Open))
                            {
                                await stream.CopyToAsync(memory);
                            }
                            memory.Position = 0;
                            outputFile.Data = memory.ToArray();
                            break;


                        case (int)FileStorageProvider.Database:
                            outputFile.Data = file.Data;
                            break;

                        default:
                            break;
                    }

                    return outputFile;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public bool File_Delete(string fileRef)
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
                            // Prune versions beyond, max copies
                            var file2Delete = fileCopies[i];
                            _ = DeleteFile(file2Delete);
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
                return _ioClient.FileStore_GetAllByRef(fileRef);
            }

            return default;
        }

        /// <inheritdoc/>
        public List<InternalFileModel> Files_GetAll(string mainGroup = "", string subGroup = "") => _ioClient.FileStore_GetAllByLocation(mainGroup, subGroup);
        #endregion

        #region internal functions
        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="fileObject">The file to save, ensure the file is saved as byte[] to Data</param>
        /// <param name="storageType">Where to store file, current options FileSystem or Database</param>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        private string File_Upsert(InternalFileModel fileObject, FileStorageProvider storageType, string mainGroup = "", string subGroup = "")
        {
            // if files exist with same name, extentsion and groups, then consider it the same file,
            // Its upto the write2file etc to store as versioned
            var existingFilesList = Files_GetAll(mainGroup, subGroup).Where(x =>
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
                        maxFileId = existingMaxId + 1;
                    }
                }

                // Rewrite with version increment
                fileObject.Name = $"{fileObject.Name}{_versionFlag}{maxFileId}";
            }
            else
            {
                //Assign unique fileRef code
                fileObject.FileRef = FileStore_FindNewFileRef();
            }

            //Determine where the file needs to go
            switch (storageType)
            {
                case FileStorageProvider.FileSystem:
                    //Send byte[] to filesystem
                    if (!FileSystemStorageAvailable)
                        throw new InvalidOperationException($"Couldnt write to folder '{_fileStoreRoot}', please ensure you specified a folder and that you have permissions to access it");

                    try
                    {
                        var fileName = _useUniqueName ? $"{fileObject.FileRef.Substring(_filePrefix.Length, fileObject.FileRef.Length - _filePrefix.Length)}{fileObject.Extension}" : $"{fileObject.Name}{fileObject.Extension}";
                        var successfullFullFilePath = Write2Filesystem(fileName, fileObject.Data, mainGroup, subGroup);

                        if (string.IsNullOrEmpty(successfullFullFilePath))
                            return string.Empty;

                        //Update the model to insert into database tracking table
                        fileObject.FilePath = successfullFullFilePath;

                        // Since in Non DB mode, ensure it doesn't get wrote to the table
                        fileObject.Data = null;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Couldn't create file, {Path.Combine(_fileStoreRoot, mainGroup, subGroup, fileObject.Name, fileObject.Extension)}. Please ensure you have permissions and no opened files by same name etc, here is the error {ex.Message}");
                    }

                    break;
                case FileStorageProvider.Database:
                    break;

                case FileStorageProvider.Box:
                    try
                    {
                        BoxClient box = new BoxClient(_boxConfig);
                        BoxResult result = box.GetContents<BoxResult>(mainGroup);

                        var reuslt = box.UploadAsync(0, fileObject).Result;

                        fileObject.FileRef = $"{_filePrefix}{reuslt}";
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    break;
                default:
                    break;
            }

            // Add the fileRecord
            // Database files will be wrote directly to the filesystemindextable
            long id = _ioClient.FileStore_Upsert(fileObject);
            if (id > 0)
            {
                // Ensure the Id was def entered
                string insertedId = _ioClient.FileStore_GetFileRef(id);
                if (!string.IsNullOrEmpty(insertedId))
                {
                    // Ensure we dont have more versions of the file than permitted.
                    List<InternalFileModel> fileCopies = _ioClient.FileStore_GetAllByRef(insertedId);
                    if (fileCopies?.Count > _maxVersions)
                    {
                        for (int i = _maxVersions; i < fileCopies.Count; i++)
                        {
                            // Prune versions beyond, max copies
                            var file2Delete = fileCopies[i];
                            _ = DeleteFile(file2Delete);
                        }
                    }

                    // All is right with the world, return inserted id
                    return insertedId;
                }
            }

            return string.Empty;
        }

        private string GetAbsoluteFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return string.Empty;
            }

            // Is the file absolute location??
            if (filePath.Contains(_fileFlag)
            || filePath.Contains(_internetFlag)
                || filePath.StartsWith(_networkFlag, StringComparison.InvariantCultureIgnoreCase))
            {
                return filePath;
            }
            // Treat as relative
            else
            {
                return Path.Combine(_fileStoreRoot, filePath);
            }
        }

        private bool DeleteFile(InternalFileModel file)
        {
            try
            {
                if (file.Id > 0)
                {

                    switch (file.FileLocation)
                    {
                        case (int)FileStorageProvider.FileSystem:
                            // This will be the true path
                            string absoluteFilePath = GetAbsoluteFilePath(file.FilePath);

                            if (System.IO.File.Exists(absoluteFilePath))
                            {
                                System.IO.File.Delete(absoluteFilePath);
                            }
                            break;

                        case (int)FileStorageProvider.Database:
                            // Do nothing, file will be deleted as on the record itself
                            break;

                        default:
                            break;
                    }

                    _ = _ioClient.FileStore_Delete(file.Id);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private string Write2Filesystem(string fileNameWithExt, byte[] data, string mainGroup = "", string subGroup = "")
        {
            try
            {
                // Will be stored in db
                string relativeFolder = Path.Combine(mainGroup, subGroup);

                // Ensure folder exists
                string absoluteFolder = Path.Combine(_fileStoreRoot, relativeFolder);
                bool absoluteFolderExists = Directory.Exists(absoluteFolder);
                if (!absoluteFolderExists)
                {
                    Directory.CreateDirectory(absoluteFolder);
                }

                //Full filepath
                string absoluteFile = Path.Combine(absoluteFolder, fileNameWithExt);
                string relativeFile = Path.Combine(relativeFolder, fileNameWithExt);

                // Wipe any blocking files with same name...
                if (System.IO.File.Exists(absoluteFile))
                {
                    System.IO.File.Delete(absoluteFile);
                }

                // Write 2 Disk
                using (var stream = new FileStream(absoluteFile, FileMode.Create))
                {
                    stream.Write(data, 0, data.Length);
                }

                // Relative File will be stored in index
                return relativeFile;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string FileStore_FindNewFileRef()
        {
            // No existing files find a new Id
            string generatedFileRef = "";
            do
            {
                generatedFileRef = $"{_filePrefix}{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
            } while (!_ioClient.FileStore_IsFileRefUnique(generatedFileRef));

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
        #endregion
    }
}
