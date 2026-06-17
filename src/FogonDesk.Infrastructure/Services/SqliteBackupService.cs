using System;
using System.Data.SQLite;
using System.IO;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Configuration;
using FogonDesk.Infrastructure.Data;

namespace FogonDesk.Infrastructure.Services
{
    public sealed class SqliteBackupService : IBackupService
    {
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly StationPaths paths;
        private readonly IClock clock;
        private readonly IAppLogger logger;

        public SqliteBackupService(
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

        public OperationResult<BackupSnapshot> CreateBackup(string requestedByUser)
        {
            try
            {
                StationPathsFactory.EnsureCreated(this.paths);
                var backupPath = Path.Combine(this.paths.BackupPath, "fogondesk-" + this.clock.UtcNow.ToString("yyyyMMdd-HHmmss") + ".db");

                using (var source = this.connectionFactory.CreateOpenConnection())
                using (var destination = this.connectionFactory.CreateOpenConnectionTo(backupPath, false))
                {
                    source.BackupDatabase(destination, "main", "main", -1, null, 0);
                }

                RegisterBackupRecord(backupPath, "backup", "completado", requestedByUser, "Respaldo creado correctamente.");
                return OperationResult<BackupSnapshot>.Ok(
                    new BackupSnapshot
                    {
                        FilePath = backupPath,
                        CreatedUtc = this.clock.UtcNow
                    },
                    "Respaldo creado correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible crear el respaldo SQLite.", exception);
                return OperationResult<BackupSnapshot>.Fail("No fue posible crear el respaldo.");
            }
        }

        public OperationResult RestoreBackup(string requestedByUser, string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return OperationResult.Fail("El archivo de respaldo no existe.");
            }

            try
            {
                if (!CheckIntegrity(sourceFilePath))
                {
                    return OperationResult.Fail("El respaldo no pasó la validación de integridad SQLite.");
                }

                SQLiteConnection.ClearAllPools();
                var safeguardPath = Path.Combine(this.paths.BackupPath, "pre-restore-" + this.clock.UtcNow.ToString("yyyyMMdd-HHmmss") + ".db");
                File.Copy(this.paths.DatabaseFilePath, safeguardPath, true);
                File.Copy(sourceFilePath, this.paths.DatabaseFilePath, true);
                DeleteIfExists(this.paths.DatabaseFilePath + "-wal");
                DeleteIfExists(this.paths.DatabaseFilePath + "-shm");

                RegisterBackupRecord(sourceFilePath, "restore", "completado", requestedByUser, "Restauración ejecutada correctamente.");
                return OperationResult.Ok("Restauración completada correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible restaurar el respaldo SQLite.", exception);
                return OperationResult.Fail("No fue posible restaurar el respaldo.");
            }
        }

        private bool CheckIntegrity(string databaseFilePath)
        {
            using (var connection = this.connectionFactory.CreateOpenConnectionTo(databaseFilePath, true))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA integrity_check;";
                var result = command.ExecuteScalar();
                return string.Equals(Convert.ToString(result), "ok", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void RegisterBackupRecord(string filePath, string operationType, string resultStatus, string requestedByUser, string notes)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO backup_records (file_path, operation_type, result_status, created_by_user_name, created_utc, notes)
VALUES (@file_path, @operation_type, @result_status, @created_by_user_name, @created_utc, @notes);";
                command.Parameters.AddWithValue("@file_path", filePath);
                command.Parameters.AddWithValue("@operation_type", operationType);
                command.Parameters.AddWithValue("@result_status", resultStatus);
                command.Parameters.AddWithValue("@created_by_user_name", requestedByUser ?? string.Empty);
                command.Parameters.AddWithValue("@created_utc", this.clock.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@notes", notes ?? string.Empty);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
