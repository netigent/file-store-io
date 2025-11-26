using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Netigent.Utils.FileStoreIO.Dal
{
    internal partial class IndexDb
    {
        internal string DbClientErrorMessage { get; private set; } = string.Empty;

        internal bool IsReady;

        private readonly string _schemaName;
        private readonly string _appPrefix;

        private string _managementConnection { get; }

        internal IndexDb(string sqlManagementConnection, string schemaName, string appPrefix)
        {
            _managementConnection = sqlManagementConnection;
            _schemaName = schemaName;
            _appPrefix = appPrefix;

            IsReady = InitializeDatabase(out string errorMessage);
            DbClientErrorMessage = errorMessage;
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
    }
}