using Dapper;
using Netigent.Utils.FileStoreIO.Constants;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Models;
using System.Collections.Generic;

namespace Netigent.Utils.FileStoreIO.Dal
{
    public partial class InternalDatabaseClient
    {
        /// <summary>
        /// Get List of fileRecords as List<InternalFileModel> from FileStoreIndex, with a variety of patterns.
        /// </summary>
        /// <param name="pathToSearch">FilePath e.g. '816', '/816/Signed Approval Template/', '/816/Signed Approval Template/SignoffTemplate.PDF' or '816/Signed Approval Template/SignoffTemplatev2.PDF'</param>
        /// <returns></returns>
        internal List<InternalFileModel> FileStoreIndex_GetAllByLocation(string pathToSearch, bool recursiveSearch)
        {
            // WARNING: Never getdata from here, file could be stored in DB, will kill performancc
            string queryDef = $@"
                    --DECLARE @PathToSearch varchar(500) = '8026/VendorPaymentSchedule/Charles pay schedule.pdf'
                    --DECLARE @PathToSearch varchar(500) = '/8026/VendorPaymentSchedule/Charles pay schedule.pdf'
					--DECLARE @PathToSearch varchar(500) = '4447/SignedInvoice'
                    --DECLARE @PathToSearch varchar(500) = '/4447/SignedInvoice'
                    --DECLARE @PathToSearch varchar(500) = '/4554/Invoice/'
                    --DECLARE @PathToSearch varchar(500) = '/4565'
                    --DECLARE @PathToSearch varchar(500) = '4550'
                    --DECLARE @RecursiveMode bit = 1
                    
                    DECLARE @pathSep varchar(1) = '{SystemConstants.InternalDirectorySeparator}'

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
	                    [PathTags],
	                    [ExtClientRef],
                        [SizeInBytes]
                    FROM [{_schemaName}].[FileStoreIndex]
                    WHERE
                    (
                    @RecursiveMode = 1 AND
                        -- MainGroup is PathType i.e. HR/Training/Sales, would get HR/Training/Sales/John + Mary etc
                        REPLACE(CONCAT(@pathSep, IsNull([PathTags],''),@pathSep, [Name], Extension),CONCAT(@pathSep,@pathSep),@pathSep)
                        LIKE
                        REPLACE(CONCAT(@pathSep, @PathToSearch),CONCAT(@pathSep,@pathSep),@pathSep) + '%'
                    )
                    OR
                    (
                    @RecursiveMode = 0 AND
                        -- MainGroup is PathType i.e. HR/Training/Sales, would get HR/Training/Sales/John + Mary etc
                        REPLACE(CONCAT(@pathSep, IsNull([PathTags],''),@pathSep),CONCAT(@pathSep,@pathSep),@pathSep)
                        =
                        REPLACE(CONCAT(@pathSep, @PathToSearch,@pathSep),CONCAT(@pathSep,@pathSep),@pathSep)
                    )";

            var queryParms = new DynamicParameters();
            queryParms.Add("@PathToSearch", pathToSearch.SetPathSeparator(SystemConstants.InternalDirectorySeparator));
            queryParms.Add("@RecursiveMode", recursiveSearch);
            return RunQueryToList<InternalFileModel>(queryDef, queryParms);
        }

        /// <summary>
        /// Get By MainGroup and SubGroup as List<FileStore>
        /// </summary>
        /// <param name="mainGroup">Main-Group-Code / Customer / Record  e.g. customer-1 / record-1</param>
        /// <param name="subGroup">Sub-Group-Code / File-Group / File-Area e.g. profile-pics</param>
        /// <returns></returns>
        internal List<InternalFileModel> FileStoreIndex_GetAllByRef(string fileRef)
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
	                    [PathTags],
	                    [ExtClientRef],
                        [SizeInBytes]
					FROM [{_schemaName}].[FileStoreIndex]
					WHERE [FileRef] = @FileRef
					ORDER BY Id Desc";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQueryToList<InternalFileModel>(queryDef, queryParms);
        }

        /// <summary>
        /// Get fileRecord from FileStoreIndex by longId.
        /// </summary>
        /// <param name="fileRef">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal InternalFileModel FileStoreIndex_GetInfo(string fileRef)
        {
            string queryDef = $@"
                    SELECT TOP 1                     
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
	                    [PathTags],
	                    [ExtClientRef],
                        [SizeInBytes],
                        [Data]
                    FROM [{_schemaName}].[FileStoreIndex]
                    WHERE [FileRef] = @FileRef";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQuery<InternalFileModel>(queryDef, queryParms);
        }

        /// <summary>
        /// Get fileRecord from FileStoreIndex by longId.
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal InternalFileModel FileStoreIndex_Get(long id)
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
	                    [PathTags],
	                    [ExtClientRef],
                        [SizeInBytes],
                        [Data]
                    FROM [{_schemaName}].[FileStoreIndex]
                    WHERE [Id] = @Id";

            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            return RunQuery<InternalFileModel>(queryDef, queryParms);
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
        internal long FileStoreIndex_Upsert(InternalFileModel model)
        {
            //Existing Record?
            InternalFileModel existingRecord = model.Id == default ? default : FileStoreIndex_Get(model.Id);

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
            queryParms.Add("@PathTags", model.PathTags.SetPathSeparator(SystemConstants.InternalDirectorySeparator));
            queryParms.Add("@SizeInBytes", model.SizeInBytes);

            if (existingRecord != null && existingRecord.Id != default)
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
	                    ,[PathTags] = @PathTags
	                    ,[SizeInBytes] = @SizeInBytes

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
	                    ,[PathTags]
	                    ,[SizeInBytes]
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
	                    ,@PathTags
	                    ,@SizeInBytes
                    )";

                return RunQuery<long>(queryDef, queryParms);
            }
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