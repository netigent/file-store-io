using Dapper;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Models;
using System.Collections.Generic;

namespace Netigent.Utils.FileStoreIO.Dal
{
    internal partial class IndexDb
    {
        /// <summary>
        /// Get List of fileRecords as List<FileStoreItem> from FileStoreIndex, with a variety of patterns.
        /// </summary>
        /// <param name="pathToSearch">FilePath e.g. './Brochures/222/', 'Brochures/32', './SALES/Training/ColdCalling/'</param>
        /// <param name="subFolders">Include all subFolders?</param>
        /// <param name="fileToFind">(Optional) Do you want to filter for a file - e.g. Summary.Pdf?</param>
        /// <param name="exactFile">(Optional) Do you want exact file match?</param>
        /// <returns></returns>
        internal List<FileStoreItem> FileStoreIndex_GetAllByFolder(string pathToSearch, bool subFolders, string fileToFind = "", bool exactFile = false)
        {
            // WARNING: Never getdata from here, file could be stored in DB, will kill performancc
            string queryDef = $@"
                --DECLARE @pathToSearch varchar(500) = './Brochures/'
                --DECLARE @pathToSearch varchar(500) = './Brochures/2/'
                --DECLARE @pathToSearch varchar(500) = './Brochures/22/'
                --DECLARE @pathToSearch varchar(500) = './Brochures/222/'
                --DECLARE @pathToSearch varchar(500) = './SALES/Training/ColdCalling/'
                --DECLARE @pathToSearch varchar(500) = './NETIGENT/34534534/'
                --DECLARE @subFolders bit = 1
                --DECLARE @fileToFind varchar(250) = ''
                --DECLARE @exactFile bit = 1 

                SELECT 
                    [Id],
                    [FileRef],
                    [Name],
                    [Extension],  
                    [MimeType],
                    [Description], 
                    [Created], 
                    [Modified],
                    [UploadedBy],
                    [FileLocation],
                    [Folder],
                    [ExtClientRef],
                    [SizeInBytes],
                    [Version]
                FROM [{_schemaName}].[FileStoreIndex]
                WHERE
                    -- Folder match
                    (
                        Folder = @pathToSearch
                        OR (@subFolders = 1 AND Folder LIKE @pathToSearch + '%')
                    )

                    -- File match
                    AND
                    (
                        @fileToFind = '' 
                        OR (
                            @exactFile = 1 
                            AND LOWER(Name + Extension) = LOWER(@fileToFind)
                        )
                        OR (
                            @exactFile = 0
                            AND LOWER(Name + Extension) LIKE '%' + LOWER(@fileToFind) + '%'
                        )
                    )";

            var queryParms = new DynamicParameters();
            queryParms.Add("@pathToSearch", pathToSearch);
            queryParms.Add("@subFolders", subFolders);
            queryParms.Add("@fileToFind", fileToFind);
            queryParms.Add("@exactFile", exactFile);
            return RunQueryToList<FileStoreItem>(queryDef, queryParms);
        }

        /// <summary>
        /// Get By MainGroup and SubGroup as List<FileStore>
        /// </summary>
        /// <param name="mainGroup">Main-Group-Code / Customer / Record  e.g. customer-1 / record-1</param>
        /// <param name="subGroup">Sub-Group-Code / File-Group / File-Area e.g. profile-pics</param>
        /// <returns></returns>
        internal List<FileStoreItem> FileStoreIndex_GetAllByRef(string fileRef)
        {
            // WARNING: Never getdata from here, file could be stored in DB, will kill performancc
            string queryDef = $@"
                    SELECT
	                    [Id],
	                    [FileRef],
	                    [Name],
	                    [Extension],  
	                    [MimeType],
	                    [Description], 
	                    [Created], 
	                    [Modified],
	                    [UploadedBy],
	                    [FileLocation],
	                    [Folder],
	                    [ExtClientRef],
                        [SizeInBytes],
                        [Version]
					FROM [{_schemaName}].[FileStoreIndex]
					WHERE [FileRef] = @FileRef
					ORDER BY id Desc";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQueryToList<FileStoreItem>(queryDef, queryParms);
        }

        /// <summary>
        /// Get fileRecord from FileStoreIndex by longId.
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal FileStoreItem FileStoreIndex_Get(long id)
        {
            string queryDef = $@"
                    SELECT 
	                    [Id],
	                    [FileRef],
	                    [Name],
	                    [Extension],  
	                    [MimeType],
	                    [Description], 
	                    [Created], 
	                    [Modified],
	                    [UploadedBy],
	                    [FileLocation],
	                    [Folder],
	                    [ExtClientRef],
                        [SizeInBytes],
                        [Data],
                        [Version]
                    FROM [{_schemaName}].[FileStoreIndex]
                    WHERE [Id] = @Id";

            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            return RunQuery<FileStoreItem>(queryDef, queryParms);
        }

        /// <summary>
        /// Get fileRef from FileStoreIndex by longId.
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal string FileStoreIndex_GetFileRef(long id)
        {
            string queryDef = $@"
                    SELECT [FileRef]
					FROM [{_schemaName}].[FileStoreIndex]
					WHERE [Id] = @Id";

            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            return RunQuery<string>(queryDef, queryParms);
        }

        /// <summary>
        /// Check if fileRef exists in the FileStoreIndex.
        /// </summary>
        /// <param name="fileRef">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal bool FileStoreIndex_IsFileRefUnique(string fileRef)
        {
            if (string.IsNullOrEmpty(fileRef))
                return false;

            string queryDef = $@"
                    SELECT COUNT([FileRef]) Cnt
                    FROM [{_schemaName}].[FileStoreIndex]
                    WHERE [FileRef] = @FileRef";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQuery<int>(queryDef, queryParms) > 0 ? false : true;
        }

        /// <summary>
        /// Insert/Update fileRecord into FileStoreIndex, if FileLocation is not DB Data is wiped.
        /// </summary>
        /// <param name="model">FileStore Object</param>
        /// <returns>Inserted / Updated FileStore.Id</returns>
        internal long FileStoreIndex_Upsert(FileStoreItem model)
        {
            //Existing Record?
            FileStoreItem? existingRecord = model.Id == default ? default : FileStoreIndex_Get(model.Id);

            // Ensure you aint storing data in db if not specifically asked for
            if (model.FileLocation != (int)FileStorageProvider.Database)
            {
                model.Data = null;
            }

            //Create Parms
            var queryParms = new DynamicParameters();
            queryParms.Add("@Created", model.Created);
            queryParms.Add("@Data", model.Data?.Length > 0 ? model.Data : null, System.Data.DbType.Binary);
            queryParms.Add("@Description", model.Description);
            queryParms.Add("@Extension", model.Extension);
            queryParms.Add("@ExtClientRef", model.ExtClientRef);
            queryParms.Add("@FileRef", model.FileRef);
            queryParms.Add("@MimeType", model.MimeType);
            queryParms.Add("@Id", model.Id);
            queryParms.Add("@Modified", model.Modified);
            queryParms.Add("@Name", model.Name);
            queryParms.Add("@UploadedBy", model.UploadedBy);
            queryParms.Add("@FileLocation", model.FileLocation);
            queryParms.Add("@Folder", model.Folder);
            queryParms.Add("@SizeInBytes", model.SizeInBytes);
            queryParms.Add("@Version", model.Version);

            if (existingRecord != null && existingRecord.Id != default && existingRecord.Id > 0)
            {
                string queryDef = $@"
                    UPDATE [{_schemaName}].[FileStoreIndex]
                    SET 
                         [Data] = @Data
	                    ,[Description] = @Description
	                    ,[Extension] = @Extension
	                    ,[ExtClientRef] = @ExtClientRef
	                    ,[FileRef] = @FileRef
	                    ,[MimeType] = @MimeType
	                    ,[Modified] = @Modified
	                    ,[Name] = @Name
	                    ,[UploadedBy] = @UploadedBy
	                    ,[FileLocation] = @FileLocation
	                    ,[Folder] = @Folder
	                    ,[SizeInBytes] = @SizeInBytes
                        ,[Version] = @Version

                    WHERE [Id] = @Id";

                var affectedRows = ExecuteQuery(queryDef, queryParms);
                return existingRecord.Id;
            }
            else
            {
                string queryDef = $@"
                    INSERT INTO [{_schemaName}].[FileStoreIndex]
                    (
	                     [Created]
	                    ,[Data]
	                    ,[Description]
	                    ,[Extension]
	                    ,[ExtClientRef]
	                    ,[FileRef]
	                    ,[MimeType]
	                    ,[Modified]
	                    ,[Name]
	                    ,[UploadedBy]
	                    ,[FileLocation]
	                    ,[Folder]
	                    ,[SizeInBytes]
                        ,[Version]
                    )
                    OUTPUT INSERTED.[Id]
                    VALUES
                    (
	                     @Created
	                    ,@Data
	                    ,@Description
	                    ,@Extension
	                    ,@ExtClientRef
	                    ,@FileRef
	                    ,@MimeType
	                    ,@Modified
	                    ,@Name
	                    ,@UploadedBy
	                    ,@FileLocation
	                    ,@Folder
	                    ,@SizeInBytes
                        ,@Version
                    )";

                return RunQuery<long>(queryDef, queryParms);
            }
        }

        /// <summary>
        /// Get fileRef from FileStoreIndex by longId.
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal int FileStoreIndex_GetVersionId(string folder, string name, string ext)
        {
            string queryDef = $@"
                    SELECT COALESCE(
                            (SELECT Max([Version]) + 1 VersionId 
                            FROM [{_schemaName}].[FileStoreIndex]
                            WHERE Folder = @Folder AND [Name] = @Name AND [Extension] = @Extension
                            GROUP BY Folder,[Name],[Extension])
                        ,1) VersionId";

            var queryParms = new DynamicParameters();

            queryParms.Add("@Folder", folder);
            queryParms.Add("@Name", name);
            queryParms.Add("@Extension", ext);

            return RunQuery<int>(queryDef, queryParms);
        }

        /// <summary>
        /// Delete fileRecord from the FileStoreIndex.
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>rows affected count</returns>
        internal int FileStoreIndex_Delete(long id)
        {
            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            string queryDef = $@"DELETE FROM [{_schemaName}].[FileStoreIndex]
									WHERE [Id] = @Id";

            return ExecuteQuery(queryDef, queryParms);
        }
    }
}