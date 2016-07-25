using System;
using System.Data.SqlClient;
using Dapper;

namespace reactive.tests.Fixtures
{
    public class SqlServerFixture : IDisposable
    {
        private readonly string _database;

        public string ConnectionString { get; }

        public SqlServerFixture() : this("localhost", "") { }

        private SqlServerFixture(string server, string prefix)
        {
            var database = CreateDatabase(prefix);
            var connectionString = $"Data Source={server};Initial Catalog={database};Integrated Security=SSPI";
            _database = database;
            ConnectionString = connectionString;
        }

        public void Dispose()
        {
            DeleteDatabase(_database, ConnectionString);
        }  

        private static void DeleteDatabase(string database, string connectionString)
        {
            using (var db = new SqlConnection(connectionString))
            {
                db.Open();
                try
                {
                    db.Execute($"USE master");
                    db.Execute($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                    db.Execute($"DROP DATABASE [{database}]");
                }
                catch (SqlException) { }
            }
        }

        private static string CreateDatabase(string prefix)
        {
            var database = string.Concat(prefix, Guid.NewGuid(), "_", DateTimeOffset.UtcNow.Ticks);
            const string connectionString = "Data Source=localhost;Integrated Security=SSPI;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = $"CREATE DATABASE [{database}]";
                connection.Execute(sql);
            }
            return database;
        }
    }
}
