using System;
using System.Data.SQLite;
using System.IO;
using FogonDesk.Application.Contracts;
using FogonDesk.Configuration;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteDatabaseBootstrapper : IDatabaseBootstrapper
    {
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly IClock clock;
        private readonly IAppLogger logger;
        private readonly StationPaths paths;

        public SqliteDatabaseBootstrapper(
            SqliteConnectionFactory connectionFactory,
            StationPaths paths,
            IClock clock,
            IAppLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.paths = paths;
            this.clock = clock;
            this.logger = logger;
        }

        public void EnsureDatabaseReady()
        {
            StationPathsFactory.EnsureCreated(this.paths);
            TryInitializeFromBundledSeed();

            using (var connection = this.connectionFactory.CreateOpenConnection())
            {
                EnsureMigrationsTable(connection);

                foreach (var script in SqliteSchemaScripts.All)
                {
                    if (IsApplied(connection, script.Name))
                    {
                        continue;
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            ExecuteNonQuery(connection, transaction, script.Sql);
                            ExecuteNonQuery(
                                connection,
                                transaction,
                                "INSERT INTO schema_migrations (name, applied_utc) VALUES (@name, @applied_utc);",
                                new SQLiteParameter("@name", script.Name),
                                new SQLiteParameter("@applied_utc", this.clock.UtcNow.ToString("o")));
                            transaction.Commit();
                            this.logger.Info("Migración aplicada: " + script.Name + ".");
                        }
                        catch (Exception exception)
                        {
                            transaction.Rollback();
                            this.logger.Error("Fallo al aplicar migración: " + script.Name + ".", exception);
                            throw;
                        }
                    }
                }
            }
        }

        private void TryInitializeFromBundledSeed()
        {
            try
            {
                var seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seed", "fogondesk.db");
                if (!File.Exists(seedPath))
                {
                    return;
                }

                if (!IsDatabaseConfigured(seedPath))
                {
                    return;
                }

                var targetPath = this.paths.DatabaseFilePath;
                var targetConfigured = File.Exists(targetPath) && IsDatabaseConfigured(targetPath);
                if (targetConfigured)
                {
                    return;
                }

                if (File.Exists(targetPath))
                {
                    var backupPath = targetPath + ".preseed-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                    File.Copy(targetPath, backupPath, true);
                }

                File.Copy(seedPath, targetPath, true);
                this.logger.Info("Base semilla copiada a perfil local: " + targetPath + ".");
            }
            catch (Exception exception)
            {
                this.logger.Warn("No fue posible aplicar base semilla en inicio: " + exception.Message);
            }
        }

        private static bool IsDatabaseConfigured(string databasePath)
        {
            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + databasePath + ";Version=3;Foreign Keys=True;"))
                {
                    connection.Open();

                    using (var existsCommand = connection.CreateCommand())
                    {
                        existsCommand.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = 'business_profile';";
                        var tableCount = Convert.ToInt32(existsCommand.ExecuteScalar());
                        if (tableCount <= 0)
                        {
                            return false;
                        }
                    }

                    using (var configuredCommand = connection.CreateCommand())
                    {
                        configuredCommand.CommandText = "SELECT COUNT(1) FROM business_profile;";
                        return Convert.ToInt32(configuredCommand.ExecuteScalar()) > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureMigrationsTable(SQLiteConnection connection)
        {
            ExecuteNonQuery(
                connection,
                null,
                @"CREATE TABLE IF NOT EXISTS schema_migrations (
                    name TEXT PRIMARY KEY,
                    applied_utc TEXT NOT NULL
                );");
        }

        private static bool IsApplied(SQLiteConnection connection, string scriptName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(1) FROM schema_migrations WHERE name = @name;";
                command.Parameters.AddWithValue("@name", scriptName);
                var result = Convert.ToInt32(command.ExecuteScalar());
                return result > 0;
            }
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params SQLiteParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                command.ExecuteNonQuery();
            }
        }
    }
}
