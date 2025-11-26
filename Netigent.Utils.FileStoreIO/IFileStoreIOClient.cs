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
        /// To join paths, what is the internal path separator used on folders?
        /// </summary>
        char FolderSeperator { get; }

        /// <summary>
        /// Checks if FileStorageProvider is available?
        /// </summary>
        /// <param name="fileStorageProvider"></param>
        /// <returns></returns>
        bool IsClientAvailable(FileStorageProvider fileStorageProvider);

        /// <summary>
        /// Insert / Update a File (byte[]) to the intended file storage, by relationalPath e.g. HR/Training/Sales/myguide.pdf.
        /// </summary>
        /// <param name="relationalFilePathAndName">Full path and filename with extension e.g. HR/Training/Sales/myguide.pdf</param>
        /// <param name="fileContents">byte[] of the file contents.</param>
        /// <param name="fileStorageProvider">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <param name="priorCopies">(Optional) Historical Copies to keep if greater than appSettings default will use this</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsyncV2(string relationalFilePathAndName, byte[] fileContents, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default, string uploadedBy = "", int? priorCopies = null);

        /// <summary>
        /// Insert / Update a File (byte[]) to the intended file storage, by Folder e.g. new [] { "HR", "Training", "Sales" }.
        /// </summary>
        /// <param name="fileContents">byte[] of the file contents.</param>
        /// <param name="filename">Filename with extension e.g. myfile.pdf</param>
        /// <param name="folder">string array of path sections in order e.g. new [] { "HR", "Training", "Sales" }</param>
        /// <param name="fileStorageProvider">Where to store file, current options Box, FileSystem (UNC / Folder) or Database</param>
        /// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
        /// <param name="created">(Optional) Created Date/Time</param>
        /// <param name="priorCopies">(Optional) Historical Copies to keep if greater than appSettings default will use this</param>
        /// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
        Task<string> File_UpsertAsyncV2(byte[] fileContents, string filename, string[] folders, FileStorageProvider fileStorageProvider = FileStorageProvider.UseDefault, string description = "", DateTime created = default, string uploadedBy = "", int? priorCopies = null);

        /// <summary>
        /// Get All Files stored within by Folder Group.
        /// </summary>
        /// <param name="folderPath">FolderPath e.g. './Brochures/222/', 'Brochures/32', './SALES/Training/ColdCalling/'</param>
        /// <param name="includeSubFolders">Include all subFolders?</param>
        /// <returns></returns>
        List<FileStoreItem> Files_GetByFolder(string folderPath, bool includeSubFolders = false);

        /// <summary>
        /// Get All Files stored within by Folder Group with FileName.
        /// </summary>
        /// <param name="folderPath">FolderPath e.g. './Brochures/222/', 'Brochures/32', './SALES/Training/ColdCalling/'</param>
        /// <param name="includeSubFolders">Include all subFolders?</param>
        /// <param name="fileToFind">Do you want to filter for a file - e.g. Summary.Pdf?</param>
        /// <param name="exactFileMatch">Do you want exact file match?</param>
        /// <returns></returns>
        List<FileStoreItem> Files_GetByFileAndFolder(string folderPath, bool includeSubFolders, string fileToFind, bool exactFileMatch);

        /// <summary>
        /// Get File from where its stored, you can also go back X versions of the file.
        /// </summary>
        /// <param name="fileRef">FileRef assigned e.g. _$5352532555325</param>
        /// <param name="goBackVersions">If you have history enabled, number of versons to go back, 0 = Current, 1 = Last, 2 = 2nd Last and so on.</param>
        /// <returns></returns>
        Task<FileOutput> File_GetAsyncV2(string fileRef, int goBackVersions = 0);

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
        List<FileStoreItem> File_GetVersionsInfo(string fileRef);

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
        Task<ResultModel> File_IndexAsync(FileStorageProvider indexLocation, string startPath = "", bool scopeToAppPrefix = true);

        /// <summary>
        /// Move the File to a New Folder location in the endstorage.
        /// </summary>
        /// <param name="fileRef"></param>
        /// <param name="Folder">string array of path sections in order e.g. new [] { "HR", "GUIDES", "PUBLISHED", "myguide1.pdf" }</param>
        /// <returns></returns>
        Task<ResultModel> File_MoveAsync(string fileRef, string[] Folder);

        /// <summary>
        /// Move the File to a New Folder location in the endstorage.
        /// </summary>
        /// <param name="fileRef"></param>
        /// <param name="relationalFilePathAndName">Full path and filename with extension e.g. HR/GUIDES/PUBLISHED/myguide1.pdf</param>
        /// <returns></returns>
        Task<ResultModel> File_MoveAsync(string fileRef, string relationalFilePathAndName);

        #region Obsolete Functions
        /// <summary>
        /// Get All Files stored within the System, it'll return all indexed copies, whether in db, filesystem of elsewhere
        /// </summary>
        /// <param name="pathTags">string array of path sections in order e.g. new [] { "HR", "Training", "Sales", "myfile.pdf" }</param>
        /// <param name="recursiveSearch">SubFolders and Tags</param>
        /// <returns></returns>
        [Obsolete("Use Files_GetByFolder or Files_GetByFileAndFolder, if you want the file name included.")]
        List<FileStoreItem> Files_GetAllV2(string[] pathTags, bool recursiveSearch = false);


        /// <summary>
        /// Get All Files stored within the System, it'll return all indexed copies, whether in db, filesystem of elsewhere
        /// </summary>
        /// <param name="relationalFilePathAndName">Full path and filename with extension e.g. HR/Training/Sales/ gets everything in Sales director or Tags</param>
        /// <param name="recursiveSearch">SubFolders and Tags</param>
        /// <returns></returns>
        [Obsolete("Use Files_GetByFolder or Files_GetByFileAndFolder, if you want the file name included.")]
        List<FileStoreItem> Files_GetAllV2(string relationalFilePathAndName, bool recursiveSearch = false);
        #endregion
    }
}
