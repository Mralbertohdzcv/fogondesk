using System.Data.SQLite;
using FogonDesk.Configuration;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteConnectionFactory
    {
        private readonly StationPaths paths;
        private readonly string connectionString;

        public SqliteConnectionFactory(StationPaths paths)
        {
            this.paths = paths;
            this.connectionString = BuildConnectionString(paths.DatabaseFilePath, false);
        }

        public string DatabaseFilePath
        {
            get { return this.paths.DatabaseFilePath; }
        }

        public SQLiteConnection CreateOpenConnection()
        {
            var connection = new SQLiteConnection(this.connectionString);
            connection.Open();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }

            return connection;
        }

        public SQLiteConnection CreateOpenConnectionTo(string databaseFilePath, bool failIfMissing)
        {
            var connection = new SQLiteConnection(BuildConnectionString(databaseFilePath, failIfMissing));
            connection.Open();
            return connection;
        }

        private static string BuildConnectionString(string databaseFilePath, bool failIfMissing)
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                Version = 3,
                FailIfMissing = failIfMissing,
                ForeignKeys = true,
                JournalMode = SQLiteJournalModeEnum.Wal,
                SyncMode = SynchronizationModes.Normal,
                Pooling = true,
                DefaultTimeout = 5,
                BinaryGUID = false
            };

            return builder.ToString();
        }
    }
}
