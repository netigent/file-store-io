using System.Collections.Generic;
using Dapper;
using Netigent.Utils.FileStoreIO.Models;

namespace Netigent.Utils.FileStoreIO.Dal
{
	public partial class InternalDatabaseClient
	{
		/// <summary>
		/// Get All as List<CustomerFiles>
		/// </summary>
		// <returns>List<CustomerFiles></returns>
		private List<CustomerFiles> CustomerFiles_GetAll()
		{
			string queryDef = $@"SELECT [Created], [CustomerId], [FileId], [Id], [Modified]
										FROM [{_schemaName}].[CustomerFiles]";

			return RunQueryToList<CustomerFiles>(queryDef, null);
		}

		/// <summary>
		/// Get CustomerFiles Object
		/// </summary>
		/// <param name="id">CustomerFiles.Id</param>
		/// <returns>CustomerFiles Object</returns>
		private CustomerFiles CustomerFiles_Get(long id)
		{
				string queryDef = $@"SELECT [Created], [CustomerId], [FileId], [Id], [Modified]
										  FROM [{_schemaName}].[CustomerFiles]
										  WHERE [Id] = @Id";

				var queryParms = new DynamicParameters();
				queryParms.Add("@Id", id);

				return RunQuery<CustomerFiles>(queryDef, queryParms);
		}


		/// <summary>
		/// Insert/Update CustomerFiles Object
		/// </summary>
		/// <param name="model">CustomerFiles Object</param>
		/// <returns>Inserted / Updated CustomerFiles.Id</returns>
		private long CustomerFiles_Upsert(CustomerFiles model)
		{
			//Existing Record?
			CustomerFiles existingRecord = model.Id == default ? default : CustomerFiles_Get(model.Id);

			//Create Parms
			var queryParms = new DynamicParameters();
			queryParms.Add("@Created", model.Created);
			queryParms.Add("@CustomerId", model.CustomerId);
			queryParms.Add("@FileId", model.FileId);
			queryParms.Add("@Id", model.Id);
			queryParms.Add("@Modified", model.Modified);


			if (existingRecord != null && existingRecord.Id != default)
			{
				string queryDef = $@"UPDATE [{_schemaName}].[CustomerFiles] SET 
											[Created] = @Created
											,[CustomerId] = @CustomerId
											,[FileId] = @FileId
											,[Modified] = @Modified

										WHERE [Id] = @Id";

				var affectedRows = ExecuteQuery(queryDef, queryParms);
				return existingRecord.Id;
			}
			else
			{
				string queryDef = $@"INSERT INTO [{_schemaName}].[CustomerFiles](
											[Created], [CustomerId], [FileId], [Modified]
										)
										OUTPUT INSERTED.[Id]
										VALUES(
											@Created, @CustomerId, @FileId, @Modified
										)";

				return RunQuery<long>(queryDef, queryParms);
			}
		}

		/// <summary>
		/// Delete CustomerFiles Object
		/// </summary>
		/// <param name="id">CustomerFiles.Id</param>
		/// <returns>rows affected count</returns>
		private int CustomerFiles_Delete(long id)
		{
			var queryParms = new DynamicParameters();
			queryParms.Add("@Id", id);

			string queryDef = $@"DELETE FROM [{_schemaName}].[CustomerFiles]
									WHERE [Id] = @Id";

			return ExecuteQuery(queryDef, queryParms);
		}
	}
}