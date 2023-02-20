using Microsoft.AspNetCore.Http;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO
{
    public interface IFileStoreIOClient
    {
        /// <summary>
        /// Has the FileStore table been created in the database, and has current account context been able to create and delete a test file?
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Execution messages from the client.
        /// </summary>
        List<string> Messages { get; }

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="file">File as an IFormFile bject</param>
        /// <param name="fileContents">byte[] of the file contents.</param>
        /// <param name="fullFilename">Filename with Extenstion.</param>
        /// <param name="storageType">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsync(byte[] fileContents, string fullFilename, FileStorageProvider fileLocation = FileStorageProvider.UseDefault, string description = "", string mainGroup = "", string subGroup = "", DateTime created = default);

        /// <summary>
        /// Insert / Update a File to the intended file storage
        /// </summary>
        /// <param name="file">File as an IFormFile bject</param>
        /// <param name="storageType">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsync(IFormFile file, FileStorageProvider fileLocation = FileStorageProvider.UseDefault, string description = "", string mainGroup = "", string subGroup = "");

        /// <summary>
        /// Get File from where its stored, you can also go back X versions of the file.
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <param name="goBackVersions">If you have history enabled, number of versons to go back, 0 = Current, 1 = Last, 2 = 2nd Last and so on.</param>
        /// <returns></returns>
        Task<FileObjectModel> File_Get(string fileRef, int goBackVersions = 0);

        /// <summary>
        /// Delete file from where its stored, this will remove db record and all prior versions
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <returns></returns>
        Task<bool> File_DeleteAsync(string fileRef);

        /// <summary>
        /// Get All Files stored within the System, it'll return all indexed copies, whether in db, filesystem of else where
        /// </summary>
        /// <param name="mainGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <param name="subGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{mainGroup}\{subGroup}\filename.ext</param>
        /// <returns></returns>
        List<InternalFileModel> Files_GetAll(string mainGroup = "", string subGroup = "");

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
        /// <returns></returns>
        Task<ResultModel> File_Migrate(string fileRef, FileStorageProvider newLocation);
    }
}
