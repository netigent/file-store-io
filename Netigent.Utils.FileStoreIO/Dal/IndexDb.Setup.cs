using System;

namespace Netigent.Utils.FileStoreIO.Dal
{
    internal partial class IndexDb
    {
        private bool InitializeDatabase(out string errors)
        {
            errors = string.Empty;

            try
            {
                string v0_1_intialSchemaCreation = $@"
						IF NOT EXISTS (SELECT  schema_name FROM information_schema.schemata WHERE schema_name = '{_schemaName}' )
						BEGIN
							EXEC('CREATE SCHEMA [{_schemaName}]')
						END";

                ExecuteQuery(v0_1_intialSchemaCreation, null);
            }
            catch (Exception ex)
            {
                errors = $"Failed Initial v.0.1.x Schema creation, {ex.Message}";
                return false;
            }

            try
            {
                string v0_1_initalTableCreation = $@"
						If not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex')
						BEGIN
							CREATE TABLE [{_schemaName}].[FileStoreIndex](
								[Id] [bigint] IDENTITY(1,1) NOT NULL,
								[Name] [varchar](250) NULL,
								[MimeType] [varchar](250) NULL,
								[Extension] [varchar](50) NULL,
								[Description] [varchar](500) NULL,
								[UploadedBy] [varchar](250) NULL,
								[FilePath] [varchar](500) NULL,
								[Data] [varbinary](max) NULL,
								[FileRef] [varchar](50) NULL,
								[Created] [datetime] NULL DEFAULT (getdate()),
								[Modified] [datetime] NULL DEFAULT (getdate()),
								[FileLocation] [int] NULL,
								[MainGroup] [varchar](250) NULL,
								[SubGroup] [varchar](250) NULL ) 
						END

						If not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'MainGroup')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							[MainGroup] [varchar](250) NULL
						END

						If not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'SubGroup')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							[SubGroup] [varchar](250) NULL
						END

						If not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'MimeType')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							[MimeType] [varchar](250) NULL
									
							EXEC('UPDATE [{_schemaName}].[FileStoreIndex] SET [MimeType] = [MimeType]')
						END

						IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'FileStoreIndex' and TABLE_SCHEMA = '{_schemaName}' and COLUMN_NAME = 'MainGroup' and CHARACTER_MAXIMUM_LENGTH = 1200)
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex]
							ALTER COLUMN [MainGroup] VARCHAR(1200)
						END
  
						IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'FileStoreIndex' and TABLE_SCHEMA = '{_schemaName}' and COLUMN_NAME = 'SizeInBytes')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex]
							ADD [SizeInBytes] BIGINT
						END
						";

                ExecuteQuery(v0_1_initalTableCreation, null);
            }
            catch (Exception ex)
            {
                errors = $"Failed Initial v.0.1.x Table creation, {ex.Message}";
                return false;
            }

            try
            {

                string v1_0_addUpdateMetaData = $@"
						If not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'MainGroup')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							[MainGroup] [varchar](250) NULL
						END

						If not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'SubGroup')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							[SubGroup] [varchar](250) NULL
						END

						If not exists(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'MimeType')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							[MimeType] [varchar](250) NULL
									
							EXEC('UPDATE [{_schemaName}].[FileStoreIndex] SET [MimeType] = [MimeType]')
						END

						IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'FileStoreIndex' and TABLE_SCHEMA = '{_schemaName}' and COLUMN_NAME = 'MainGroup' and CHARACTER_MAXIMUM_LENGTH = 1200)
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex]
							ALTER COLUMN [MainGroup] VARCHAR(1200)
						END
  
						IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'FileStoreIndex' and TABLE_SCHEMA = '{_schemaName}' and COLUMN_NAME = 'SizeInBytes')
						BEGIN
							ALTER TABLE [{_schemaName}].[FileStoreIndex]
							ADD [SizeInBytes] BIGINT
						END
						";
                ExecuteQuery(v1_0_addUpdateMetaData, null);
            }
            catch (Exception ex)
            {
                errors = $"Failed Adding v.1.0.x support, {ex.Message}";
                return false;
            }

            try
            {
                var v1_1_awsSupport = $@"
                        IF NOT EXISTS (select 1 from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{_schemaName}' and TABLE_NAME = 'FileStoreIndex' and COLUMN_NAME = 'ExtClientRef')
							BEGIN
								ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
								[PathTags] [varchar](2048) NULL
							
								EXEC('
                                        UPDATE [{_schemaName}].[FileStoreIndex]
                                        SET [PathTags] =
                                        REPLACE(
                                          CONCAT(
	                                        CASE 
		                                        WHEN IsNull(''{_appPrefix}'', '''') != ''''
		                                        THEN ''{_appPrefix}/'' 
		                                        ELSE ''''
	                                        END,
	                                        CASE 
		                                        WHEN IsNull([MainGroup], '''') != '''' AND IsNull([SubGroup], '''') != ''''
		                                        THEN [MainGroup] + ''/'' + [SubGroup]
		                                        ELSE IsNull([MainGroup], '''') + '''' + IsNull([SubGroup], '''')
	                                        END)
                                        , ''//'', ''/'')
                                    ')

                                EXEC('sp_rename ''{_schemaName}.FileStoreIndex.FilePath'', ''ExtClientRef'', ''COLUMN''')
							END
                        ";

                ExecuteQuery(v1_1_awsSupport, null);
            }
            catch (Exception ex)
            {
                errors = $"Failed Adding v.1.1.x support, {ex.Message}";
                return false;
            }

            try
            {
                var appendingEndTag = $@"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{_schemaName}' AND TABLE_NAME = 'FileStoreIndex' AND COLUMN_NAME = 'Folder')
                    BEGIN
                          ALTER TABLE [{_schemaName}].[FileStoreIndex] ADD
							  [Folder] varchar(2048) null,
							  [Version] int null;
	                          
							  EXEC('UPDATE [{_schemaName}].[FileStoreIndex] SET [PathTags] = LEFT([PathTags], LEN([PathTags]) - 1) WHERE [PathTags] LIKE ''%/''')
							  EXEC('UPDATE [{_schemaName}].[FileStoreIndex] SET [Folder] = CONCAT(''./'',PathTags,''/''), [Version] = 1')
                    END";

                ExecuteQuery(appendingEndTag, null);
            }
            catch (Exception ex)
            {
                errors = $"Failed Adding v1.4.0 Folder and Version, {ex.Message}";
                return false;
            }
            return true;
        }
    }
}