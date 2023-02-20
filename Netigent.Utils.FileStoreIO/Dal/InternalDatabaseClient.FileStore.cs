using System.Collections.Generic;
using Dapper;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;

namespace Netigent.Utils.FileStoreIO.Dal
{
    public partial class InternalDatabaseClient
    {
        /// <summary>
        /// Get By MainGroup and SubGroup as List<FileStore>
        /// </summary>
        /// <param name="mainGroup">Main-Group-Code / Customer / Record  e.g. customer-1 / record-1</param>
        /// <param name="subGroup">Sub-Group-Code / File-Group / File-Area e.g. profile-pics</param>
        /// <returns></returns>
        internal List<InternalFileModel> FileStore_GetAllByLocation(string mainGroup = "", string subGroup = "")
        {
            // WARNING: Never getdata from here, file could be stored in DB, will kill performancc
            string queryDef = $@"SELECT [Created], [Description], [Extension], [FileRef], [MimeType], [Id], [Modified], [Name], [UploadedBy], [FileLocation], [MainGroup], [SubGroup]
										FROM [{_schemaName}].[FileStoreIndex]
										WHERE (@MainGroup = '' OR IsNull([MainGroup],'') = @MainGroup)
										AND (@SubGroup = '' OR IsNull([SubGroup],'') = @SubGroup)";

            var queryParms = new DynamicParameters();
            queryParms.Add("@MainGroup", mainGroup);
            queryParms.Add("@SubGroup", subGroup);

            return RunQueryToList<InternalFileModel>(queryDef, queryParms);
        }

        /// <summary>
        /// Get By MainGroup and SubGroup as List<FileStore>
        /// </summary>
        /// <param name="mainGroup">Main-Group-Code / Customer / Record  e.g. customer-1 / record-1</param>
        /// <param name="subGroup">Sub-Group-Code / File-Group / File-Area e.g. profile-pics</param>
        /// <returns></returns>
        internal List<InternalFileModel> FileStore_GetAllByRef(string fileRef)
        {
            // WARNING: Never getdata from here, file could be stored in DB, will kill performancc
            string queryDef = $@"SELECT [Created], [Description], [Extension], [FilePath], [FileRef], [MimeType], [Id], [Modified], [Name], [UploadedBy], [FileLocation], [MainGroup], [SubGroup]
											  FROM [{_schemaName}].[FileStoreIndex]
											  WHERE [FileRef] = @FileRef
											  ORDER BY Id Desc";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQueryToList<InternalFileModel>(queryDef, queryParms);
        }

        /// Get FileStore Object
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal InternalFileModel FileStore_GetInfo(string fileRef)
        {
            string queryDef = $@"SELECT TOP 1 [Created], [Description], [Extension], [FilePath], [FileRef], [MimeType], [Id], [Modified], [Name], [UploadedBy], [FileLocation], [MainGroup], [SubGroup]
											  FROM [{_schemaName}].[FileStoreIndex]
											  WHERE [FileRef] = @FileRef";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQuery<InternalFileModel>(queryDef, queryParms);
        }

        /// <summary>
        /// Get FileStore Object
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal InternalFileModel FileStore_Get(long id)
        {
            string queryDef = $@"SELECT [Created], [Data], [Description], [Extension], [FilePath], [FileRef], [MimeType], [Id], [Modified], [Name], [UploadedBy], [FileLocation], [MainGroup], [SubGroup]
										  FROM [{_schemaName}].[FileStoreIndex]
										  WHERE [Id] = @Id";

            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            return RunQuery<InternalFileModel>(queryDef, queryParms);
        }

        /// <summary>
        /// Get FileStore Object
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal string FileStore_GetFileRef(long id)
        {
            string queryDef = $@"SELECT [FileRef]
									  FROM [{_schemaName}].[FileStoreIndex]
									  WHERE [Id] = @Id";

            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            return RunQuery<string>(queryDef, queryParms);
        }

        /// <summary>
        /// Get FileStore Object
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>FileStore Object</returns>
        internal bool FileStore_IsFileRefUnique(string fileRef)
        {
            if (string.IsNullOrEmpty(fileRef))
                return false;

            string queryDef = $@"SELECT COUNT([FileRef]) Cnt
											  FROM [{_schemaName}].[FileStoreIndex]
											  WHERE [FileRef] = @FileRef";

            var queryParms = new DynamicParameters();
            queryParms.Add("@FileRef", fileRef);

            return RunQuery<int>(queryDef, queryParms) > 0 ? false : true;
        }

        /// <summary>
        /// Insert/Update FileStore Object
        /// </summary>
        /// <param name="model">FileStore Object</param>
        /// <returns>Inserted / Updated FileStore.Id</returns>
        internal long FileStore_Upsert(InternalFileModel model)
        {
            //Existing Record?
            InternalFileModel existingRecord = model.Id == default ? default : FileStore_Get(model.Id);

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
            queryParms.Add("@FilePath", model.FilePath);
            queryParms.Add("@FileRef", model.FileRef);
            queryParms.Add("@MimeType", model.MimeType);
            queryParms.Add("@Id", model.Id);
            queryParms.Add("@Modified", model.Modified);
            queryParms.Add("@Name", model.Name);
            queryParms.Add("@UploadedBy", model.UploadedBy);
            queryParms.Add("@FileLocation", model.FileLocation);
            queryParms.Add("@MainGroup", model.MainGroup);
            queryParms.Add("@FileTypeGroup", model.SubGroup);



            if (existingRecord != null && existingRecord.Id != default)
            {
                string queryDef = $@"UPDATE [{_schemaName}].[FileStoreIndex] SET 
											[Created] = @Created
											,[Data] = @Data
											,[Description] = @Description
											,[Extension] = @Extension
											,[FilePath] = @FilePath
											,[FileRef] = @FileRef
											,[MimeType] = @MimeType
											,[Modified] = @Modified
											,[Name] = @Name
											,[UploadedBy] = @UploadedBy
											,[FileLocation] = @FileLocation
											,[MainGroup] = @MainGroup
											,[SubGroup] = @FileTypeGroup

										WHERE [Id] = @Id";

                var affectedRows = ExecuteQuery(queryDef, queryParms);
                return existingRecord.Id;
            }
            else
            {
                string queryDef = $@"INSERT INTO [{_schemaName}].[FileStoreIndex](
											[Created], [Data], [Description], [Extension], [FilePath], [FileRef], [MimeType], [Modified], [Name], [UploadedBy], [FileLocation], [MainGroup], [SubGroup]
										)
										OUTPUT INSERTED.[Id]
										VALUES(
											@Created, @Data, @Description, @Extension, @FilePath, @FileRef, @MimeType, @Modified, @Name, @UploadedBy,@FileLocation, @MainGroup, @FileTypeGroup
										)";

                return RunQuery<long>(queryDef, queryParms);
            }
        }

        /// <summary>
        /// Delete FileStore Object
        /// </summary>
        /// <param name="id">FileStore.Id</param>
        /// <returns>rows affected count</returns>
        internal int FileStore_Delete(long id)
        {
            var queryParms = new DynamicParameters();
            queryParms.Add("@Id", id);

            string queryDef = $@"DELETE FROM [{_schemaName}].[FileStoreIndex]
									WHERE [Id] = @Id";

            return ExecuteQuery(queryDef, queryParms);
        }
    }
}