using System.Data.SQLite;
using FogonDesk.Application.Contracts;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteCounterRepository : ICounterRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteCounterRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public int GetNextValue(string counterName)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                int current;
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT OR IGNORE INTO app_counters (name, next_value) VALUES (@name, 1);";
                    command.Parameters.Add(new SQLiteParameter("@name", counterName));
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT next_value FROM app_counters WHERE name = @name;";
                    command.Parameters.Add(new SQLiteParameter("@name", counterName));
                    current = System.Convert.ToInt32(command.ExecuteScalar());
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "UPDATE app_counters SET next_value = @next_value WHERE name = @name;";
                    command.Parameters.Add(new SQLiteParameter("@next_value", current + 1));
                    command.Parameters.Add(new SQLiteParameter("@name", counterName));
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return current;
            }
        }

        public int PeekCurrentValue(string counterName)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT OR IGNORE INTO app_counters (name, next_value) VALUES (@name, 1);";
                    cmd.Parameters.Add(new SQLiteParameter("@name", counterName));
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT next_value FROM app_counters WHERE name = @name;";
                    cmd.Parameters.Add(new SQLiteParameter("@name", counterName));
                    return System.Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void ResetValue(string counterName, int value)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO app_counters (name, next_value) VALUES (@name, @value)
ON CONFLICT(name) DO UPDATE SET next_value = @value;";
                command.Parameters.Add(new SQLiteParameter("@name", counterName));
                command.Parameters.Add(new SQLiteParameter("@value", value));
                command.ExecuteNonQuery();
            }
        }
    }
}
