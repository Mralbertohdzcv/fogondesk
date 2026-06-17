using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteOperationSettingsRepository : IOperationSettingsRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteOperationSettingsRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public OperationSettingsView LoadSettings()
        {
            var result = new OperationSettingsView
            {
                DiningTableCount = 5,
                DigitalPlatforms = new List<DigitalPlatformConfigurationView>()
            };

            using (var connection = this.connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT id, name, COALESCE(pricing_mode, 'manual'), is_active
FROM digital_platforms
ORDER BY CASE WHEN is_active = 1 THEN 0 ELSE 1 END, name COLLATE NOCASE;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.DigitalPlatforms.Add(new DigitalPlatformConfigurationView
                            {
                                PlatformId = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                PricingMode = reader.IsDBNull(2) ? "manual" : reader.GetString(2),
                                IsActive = !reader.IsDBNull(3) && Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture) == 1
                            });
                        }
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(1) FROM dining_tables WHERE is_active = 1;";
                    var count = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                    result.DiningTableCount = count <= 0 ? 5 : count;
                }
            }

            return result;
        }

        public void SaveSettings(SaveOperationSettingsRequest request, DateTime updatedUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                // Keep physical table records and toggle active state based on configured count.
                for (var index = 1; index <= request.DiningTableCount; index++)
                {
                    var code = "M" + index.ToString("00", CultureInfo.InvariantCulture);
                    ExecuteNonQuery(
                        connection,
                        transaction,
                        @"INSERT OR IGNORE INTO dining_tables (code, name, seats, is_active)
VALUES (@code, @name, 4, 1);",
                        Parameter("@code", code),
                        Parameter("@name", "Mesa " + index.ToString(CultureInfo.InvariantCulture)));
                }

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"UPDATE dining_tables
SET is_active = CASE
    WHEN CAST(SUBSTR(code, 2) AS INTEGER) BETWEEN 1 AND @table_count THEN 1
    ELSE 0
END
WHERE code LIKE 'M%';",
                    Parameter("@table_count", request.DiningTableCount));

                foreach (var platform in request.DigitalPlatforms)
                {
                    if (platform.PlatformId.HasValue && platform.PlatformId.Value > 0)
                    {
                        ExecuteNonQuery(
                            connection,
                            transaction,
                            @"UPDATE digital_platforms
SET name = @name,
    pricing_mode = @pricing_mode,
    is_active = @is_active
WHERE id = @id;",
                            Parameter("@name", platform.Name),
                            Parameter("@pricing_mode", platform.PricingMode),
                            Parameter("@is_active", platform.IsActive ? 1 : 0),
                            Parameter("@id", platform.PlatformId.Value));
                    }
                    else
                    {
                        ExecuteNonQuery(
                            connection,
                            transaction,
                            @"INSERT INTO digital_platforms (code, name, pricing_mode, is_active, next_sequence)
VALUES (@code, @name, @pricing_mode, @is_active, 1);",
                            Parameter("@code", BuildPlatformCode(platform.Name)),
                            Parameter("@name", platform.Name),
                            Parameter("@pricing_mode", platform.PricingMode),
                            Parameter("@is_active", platform.IsActive ? 1 : 0));
                    }
                }

                transaction.Commit();
            }
        }

        private static string BuildPlatformCode(string platformName)
        {
            var text = (platformName ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length == 0)
            {
                return "plataforma";
            }

            var buffer = new char[text.Length];
            var index = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    buffer[index++] = ch;
                }
                else if (index > 0 && buffer[index - 1] != '_')
                {
                    buffer[index++] = '_';
                }
            }

            var normalized = new string(buffer, 0, index).Trim('_');
            return string.IsNullOrWhiteSpace(normalized) ? "plataforma" : normalized;
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params SQLiteParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                command.ExecuteNonQuery();
            }
        }

        private static SQLiteParameter Parameter(string name, object value)
        {
            return new SQLiteParameter(name, value ?? DBNull.Value);
        }
    }
}
