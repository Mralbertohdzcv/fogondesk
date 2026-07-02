using System;
using System.Data.SQLite;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteDataResetService : IDataResetService
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteDataResetService(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public OperationResult ResetSalesData()
        {
            try
            {
                using (var connection = this.connectionFactory.CreateOpenConnection())
                using (var transaction = connection.BeginTransaction())
                {
                    Exec(connection, transaction, "DELETE FROM sale_item_modifiers");
                    Exec(connection, transaction, "DELETE FROM sale_items");
                    Exec(connection, transaction, "DELETE FROM payments");
                    Exec(connection, transaction, "DELETE FROM cash_shift_sales");
                    Exec(connection, transaction, "DELETE FROM cancellation_requests");
                    Exec(connection, transaction, "DELETE FROM sales");
                    Exec(connection, transaction, "DELETE FROM cash_shifts");
                    Exec(connection, transaction, "DELETE FROM pending_order_item_modifiers");
                    Exec(connection, transaction, "DELETE FROM pending_order_items");
                    Exec(connection, transaction, "DELETE FROM pending_orders");
                    Exec(connection, transaction, "DELETE FROM inventory_movements");
                    Exec(connection, transaction, "DELETE FROM audit_log");
                    Exec(connection, transaction, "UPDATE app_counters SET next_value = 1");
                    Exec(connection, transaction, "UPDATE digital_platforms SET next_sequence = 1");
                    transaction.Commit();
                }

                return OperationResult.Ok("Datos de ventas restablecidos correctamente.");
            }
            catch (Exception exception)
            {
                return OperationResult.Fail("No fue posible restablecer los datos: " + exception.Message);
            }
        }

        private static void Exec(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, connection, transaction))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
