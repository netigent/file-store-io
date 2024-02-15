using Microsoft.AspNetCore.Http;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO
{
    public interface IFileStoreIOClient
    {
        /// <summary>
        /// Has the FileStore table been created in the database and is the database online etc?
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// What is the current AppPrefix if any?
        /// </summary>
        string AppPrefix { get; }

        /// <summary>
        /// To join paths, what is the internal path separator?
        /// </summary>
        char PathSeperator { get; }

        bool IsClientAvailable(FileStorageProvider fileStorageProvider);

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="relationalFilePathAndName">Full path and filename with extension e.g. HR/Training/Sales/myguide.pdf</param>
        /// <param name="fileContents">byte[] of the file contents.</param>
        /// <param name="fileStorageProvider">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsyncV2(string relationalFilePathAndName, byte[] fileContents, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default);

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="relationalFilePathAndName">Full path and filename with extension e.g. HR/Training/Sales/myguide.pdf</param>
        /// <param name="file">File as an IFormFile bject</param>
        /// <param name="fileStorageProvider">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsyncV2(string relationalFilePathAndName, IFormFile file, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default);

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="fileContents">byte[] of the file contents.</param>
        /// <param name="filename">Filename with extension e.g. myfile.pdf</param>
        /// <param name="pathTags">string array of path sections in order e.g. new [] { "HR", "Training", "Sales" }</param>
        /// <param name="fileStorageProvider">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsyncV2(byte[] fileContents, string filename, string[] pathTags, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default);

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="file">File as an IFormFile bject</param>
        /// <param name="filename">Filename with extension e.g. myfile.pdf</param>
        /// <param name="pathTags">string array of path sections in order e.g. new [] { "HR", "Training", "Sales" }</param>
        /// <param name="fileStorageProvider">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsyncV2(IFormFile file, string filename, string[] pathTags, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default);

        /// <summary>
        /// Get All Files stored within the System, it'll return all indexed copies, whether in db, filesystem of elsewhere
        /// </summary>
        /// <param name="pathTags">string array of path sections in order e.g. new [] { "HR", "Training", "Sales", "myfile.pdf" }</param>
        /// <returns></returns>
        List<InternalFileModel> Files_GetAllV2(string[] pathTags);

        /// <summary>
        /// Get All Files stored within the System, it'll return all indexed copies, whether in db, filesystem of elsewhere
        /// </summary>
        /// <param name="relationalFilePathAndName">Full path and filename with extension e.g. HR/Training/Sales/ gets everything in Sales director or Tags</param>
        /// <returns></returns>
        List<InternalFileModel> Files_GetAllV2(string relationalFilePathAndName);

        /// <summary>
        /// Get File from where its stored, you can also go back X versions of the file.
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <param name="goBackVersions">If you have history enabled, number of versons to go back, 0 = Current, 1 = Last, 2 = 2nd Last and so on.</param>
        /// <returns></returns>
        Task<FileObjectModel> File_GetAsyncV2(string fileRef, int goBackVersions = 0);

        /// <summary>
        /// Delete file from where its stored, this will remove db record and all prior versions
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <returns></returns>
        Task<bool> File_DeleteAsync(string fileRef);

        /// <summary>
        /// Get FileStore Info on the file, including all versions.
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <returns></returns>
        List<InternalFileModel> File_GetVersionsInfo(string fileRef);

        /// <summary>
        /// Migrate Binary to New Provider Location.
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <param name="newLocation">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="moveFile">(Optional) ONLY if the file location moves, then true will delete original file once moved, if false will leave original file in place.</param>
        /// <returns></returns>
        Task<ResultModel> File_Migrate(string fileRef, FileStorageProvider newLocation, bool moveFile = true);

        /// <summary>
        /// Reindex all files.
        /// </summary>
        /// <param name="indexLocation">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <returns></returns>
        Task<ResultModel> File_IndexAsync(FileStorageProvider indexLocation, string indexFrom = "", bool scopeToAppPrefix = true);

        #region Obselete Methods
        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="fileContents">byte[] of the file contents.</param>
        /// <param name="fullFilename">Filename with Extenstion.</param>
        /// <param name="fileLocation">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        [Obsolete(message: "This method is now obsolete, use File_UpsertAsyncV2", error: false)]
        Task<string> File_UpsertAsync(byte[] fileContents, string fullFilename, FileStorageProvider fileLocation = FileStorageProvider.UseDefault, string description = "", string mainGroup = "", string subGroup = "", DateTime created = default);

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="file">File as an IFormFile bject</param>
        /// <param name="fileLocation">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        [Obsolete(message: "This method is now obsolete, use File_UpsertAsyncV2", error: false)]
        Task<string> File_UpsertAsync(IFormFile file, FileStorageProvider fileLocation = FileStorageProvider.UseDefault, string description = "", string mainGroup = "", string subGroup = "");

        /// <summary>
        /// Get All Files stored within the System, it'll return all indexed copies, whether in db, filesystem of elsewhere
        /// </summary>
        /// <param name="mainGroup">Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <returns></returns>
        [Obsolete(message: "This method is now obsolete, use Files_GetAllV2", error: false)]
        List<InternalFileModel> Files_GetAll(string mainGroup = "", string subGroup = "");

        /// <summary>
        /// Get File from where its stored, you can also go back X versions of the file.
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <param name="goBackVersions">If you have history enabled, number of versons to go back, 0 = Current, 1 = Last, 2 = 2nd Last and so on.</param>
        /// <returns></returns>
        [Obsolete(message: "This method is now obsolete, use File_GetAsyncV2", error: false)]
        Task<FileObjectModel> File_Get(string fileRef, int goBackVersions = 0);
        #endregion
    }
}
