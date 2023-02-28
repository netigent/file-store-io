using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Netigent.Utils.FileStoreIO.Dal
{
	public partial class InternalDatabaseClient
	{
		public string DbClientErrorMessage { get; private set; } = string.Empty;

		public bool IsReady;

		private readonly string _schemaName;
		private string _managementConnection { get; }

		public InternalDatabaseClient(string sqlManagementConnection, string schemaName)
		{
			_managementConnection = sqlManagementConnection;
			_schemaName = schemaName;

			IsReady = InitializeDatabase(out string errorMessage);
			DbClientErrorMessage = errorMessage;
		}

		#region Database I/O
		private bool InitializeDatabase(out string errors)
		{
			try
			{
				errors = string.Empty;

				string schemaExists = $@"
						IF NOT EXISTS (SELECT  schema_name FROM information_schema.schemata WHERE schema_name = '{_schemaName}' )
						BEGIN
							EXEC('CREATE SCHEMA [{_schemaName}]')
						END";

				string fileIndexExists = $@"
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
						END";

				var schemaResult = ExecuteQuery(schemaExists, null);
				var tableResult = ExecuteQuery(fileIndexExists, null);
				return true;
			}
			catch (Exception ex)
			{
				errors = ex.Message;
				return false;
			}
		}

		private List<T> RunQueryToList<T>(string query, DynamicParameters parms = null, string dbConnection = "")
		{
			List<T> output;

			var connectionString = string.IsNullOrEmpty(dbConnection) ? _managementConnection : dbConnection;

			//try get model from database
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				output = connection.Query<T>(query, parms).ToList();
				connection.Close();
			}

			return output;
		}

		private T RunQuery<T>(string query, DynamicParameters parms = null, string dbConnection = "")
		{
			T output;

			var connectionString = string.IsNullOrEmpty(dbConnection) ? _managementConnection : dbConnection;

			//try get model from database
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				output = connection.Query<T>(query, parms).FirstOrDefault();
				connection.Close();
			}

			return output;
		}

		private int ExecuteQuery(string query, DynamicParameters parms = null, string dbConnection = "")
		{
			int rowCount;

			var connectionString = string.IsNullOrEmpty(dbConnection) ? _managementConnection : dbConnection;

			//try get model from database
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				rowCount = connection.Execute(query, parms);
				connection.Close();
			}

			return rowCount;
		}
		#endregion
	}
}