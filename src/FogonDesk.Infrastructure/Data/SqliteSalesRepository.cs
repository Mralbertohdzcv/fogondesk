using System;
using System.Data.SQLite;
using System.Linq;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteSalesRepository : ISalesRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteSalesRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public CreateSaleResult PersistSale(CreateSaleRequest request, DateTime soldUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var sequence = GetNextSequence(connection, transaction);
                    var folio = "Folio " + sequence.ToString();
                    var subtotal = request.Items.Sum(item => item.UnitPrice * item.Quantity);
                    var estimatedCostTotal = request.Items.Sum(item => item.EstimatedCost * item.Quantity);
                    var total = subtotal;
                    var estimatedProfitTotal = total - estimatedCostTotal;

                    var saleId = InsertSale(
                        connection,
                        transaction,
                        folio,
                        request,
                        soldUtc,
                        subtotal,
                        estimatedCostTotal,
                        estimatedProfitTotal,
                        total);

                    foreach (var item in request.Items)
                    {
                        InsertSaleItem(connection, transaction, saleId, item);
                        if (item.UsesInventory)
                        {
                            DiscountInventory(connection, transaction, item);
                            InsertInventoryMovement(connection, transaction, item, request.UserId, saleId, soldUtc);
                        }
                    }

                    InsertPayment(connection, transaction, saleId, request.PaymentMethod, total);
                    if (request.CashShiftId.HasValue && request.CashShiftId.Value > 0)
                    {
                        AttachSaleToCashShift(connection, transaction, request.CashShiftId.Value, saleId, total, estimatedCostTotal, estimatedProfitTotal, request.PaymentMethod);
                    }
                    InsertAudit(connection, transaction, request.UserName, saleId, folio, soldUtc);
                    transaction.Commit();

                    return new CreateSaleResult
                    {
                        SaleId = Convert.ToInt32(saleId),
                        Folio = folio,
                        Total = total,
                        SoldUtc = soldUtc
                    };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void CancelSale(CancelSaleRequest request, DateTime cancelledUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    SaleStatus status;
                    decimal total;
                    decimal estimatedCostTotal;
                    decimal estimatedProfitTotal;
                    var paymentSummary = string.Empty;
                    int? cashShiftId;
                    var folio = string.Empty;

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"SELECT status, total, estimated_cost_total, estimated_profit_total, payment_summary, cash_shift_id, folio
FROM sales
WHERE id = @id
LIMIT 1;";
                        command.Parameters.AddWithValue("@id", request.SaleId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                throw new InvalidOperationException("El ticket indicado no existe.");
                            }

                            status = (SaleStatus)Convert.ToInt32(reader.GetValue(0));
                            total = Convert.ToDecimal(reader.GetValue(1));
                            estimatedCostTotal = Convert.ToDecimal(reader.GetValue(2));
                            estimatedProfitTotal = Convert.ToDecimal(reader.GetValue(3));
                            paymentSummary = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                            cashShiftId = reader.IsDBNull(5) ? (int?)null : Convert.ToInt32(reader.GetValue(5));
                            folio = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                        }
                    }

                    if (status == SaleStatus.Cancelada)
                    {
                        throw new InvalidOperationException("El ticket ya está cancelado.");
                    }

                    if (request.CashShiftId.HasValue && cashShiftId.HasValue && request.CashShiftId.Value != cashShiftId.Value)
                    {
                        throw new InvalidOperationException("El ticket no pertenece al corte activo.");
                    }

                    ExecuteNonQuery(
                        connection,
                        transaction,
                        @"UPDATE sales
SET status = @status
WHERE id = @id;",
                        new SQLiteParameter("@status", (int)SaleStatus.Cancelada),
                        new SQLiteParameter("@id", request.SaleId));

                    if (cashShiftId.HasValue)
                    {
                        var cashAmount = string.Equals(paymentSummary, PaymentMethod.Efectivo.ToString(), StringComparison.OrdinalIgnoreCase) ? total : 0m;
                        ExecuteNonQuery(
                            connection,
                            transaction,
                            @"UPDATE cash_shifts
SET sales_total = sales_total - @sales_total,
    estimated_cost_total = estimated_cost_total - @estimated_cost_total,
    estimated_profit_total = estimated_profit_total - @estimated_profit_total,
    expected_cash = expected_cash - @cash_amount
WHERE id = @id;",
                            new SQLiteParameter("@sales_total", total),
                            new SQLiteParameter("@estimated_cost_total", estimatedCostTotal),
                            new SQLiteParameter("@estimated_profit_total", estimatedProfitTotal),
                            new SQLiteParameter("@cash_amount", cashAmount),
                            new SQLiteParameter("@id", cashShiftId.Value));
                    }

                    ExecuteNonQuery(
                        connection,
                        transaction,
                        @"INSERT INTO audit_log (event_type, entity_name, entity_id, user_name, details, created_utc)
VALUES ('sale.cancelled', 'sales', @entity_id, @user_name, @details, @created_utc);",
                        new SQLiteParameter("@entity_id", request.SaleId.ToString()),
                        new SQLiteParameter("@user_name", request.UserName ?? string.Empty),
                        new SQLiteParameter("@details", "Ticket cancelado " + folio),
                        new SQLiteParameter("@created_utc", cancelledUtc.ToString("o")));

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static long InsertSale(SQLiteConnection connection, SQLiteTransaction transaction, string folio, CreateSaleRequest request, DateTime soldUtc, decimal subtotal, decimal estimatedCostTotal, decimal estimatedProfitTotal, decimal total)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO sales (folio, order_kind, status, pending_order_id, cash_shift_id, sold_by_user_id, sold_utc, subtotal, discount_total, tax_total, total, estimated_cost_total, estimated_profit_total, payment_summary, note, print_status)
VALUES (@folio, @order_kind, @status, NULL, @cash_shift_id, @sold_by_user_id, @sold_utc, @subtotal, 0, 0, @total, @estimated_cost_total, @estimated_profit_total, @payment_summary, @note, 'pendiente');",
                new SQLiteParameter("@folio", folio),
                new SQLiteParameter("@order_kind", (int)request.OrderKind),
                new SQLiteParameter("@status", (int)SaleStatus.Confirmada),
                new SQLiteParameter("@cash_shift_id", (object)request.CashShiftId ?? DBNull.Value),
                new SQLiteParameter("@sold_by_user_id", request.UserId),
                new SQLiteParameter("@sold_utc", soldUtc.ToString("o")),
                new SQLiteParameter("@subtotal", subtotal),
                new SQLiteParameter("@total", total),
                new SQLiteParameter("@estimated_cost_total", estimatedCostTotal),
                new SQLiteParameter("@estimated_profit_total", estimatedProfitTotal),
                new SQLiteParameter("@payment_summary", request.PaymentMethod.ToString()),
                new SQLiteParameter("@note", request.Note ?? string.Empty));

            return GetLastInsertId(connection, transaction);
        }

        private static void InsertSaleItem(SQLiteConnection connection, SQLiteTransaction transaction, long saleId, SaleLineDraft item)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO sale_items (sale_id, product_id, combo_id, product_name_snapshot, quantity, unit_price, line_total, estimated_cost_total, uses_inventory)
VALUES (@sale_id, @product_id, NULL, @product_name_snapshot, @quantity, @unit_price, @line_total, @estimated_cost_total, @uses_inventory);",
                new SQLiteParameter("@sale_id", saleId),
                new SQLiteParameter("@product_id", item.ProductId),
                new SQLiteParameter("@product_name_snapshot", item.ProductName),
                new SQLiteParameter("@quantity", item.Quantity),
                new SQLiteParameter("@unit_price", item.UnitPrice),
                new SQLiteParameter("@line_total", item.UnitPrice * item.Quantity),
                new SQLiteParameter("@estimated_cost_total", item.EstimatedCost * item.Quantity),
                new SQLiteParameter("@uses_inventory", item.UsesInventory ? 1 : 0));
        }

        private static void InsertPayment(SQLiteConnection connection, SQLiteTransaction transaction, long saleId, PaymentMethod paymentMethod, decimal total)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO payments (sale_id, payment_method, amount, reference_text)
VALUES (@sale_id, @payment_method, @amount, '');",
                new SQLiteParameter("@sale_id", saleId),
                new SQLiteParameter("@payment_method", (int)paymentMethod),
                new SQLiteParameter("@amount", total));
        }

        private static void DiscountInventory(SQLiteConnection connection, SQLiteTransaction transaction, SaleLineDraft item)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"UPDATE products
SET stock_on_hand = stock_on_hand - @quantity
WHERE id = @id
  AND uses_inventory = 1
  AND stock_on_hand >= @quantity;";
                command.Parameters.AddWithValue("@quantity", item.Quantity);
                command.Parameters.AddWithValue("@id", item.ProductId);
                var affectedRows = command.ExecuteNonQuery();
                if (affectedRows != 1)
                {
                    throw new InvalidOperationException("Inventario insuficiente para " + item.ProductName + ".");
                }
            }
        }

        private static void InsertInventoryMovement(SQLiteConnection connection, SQLiteTransaction transaction, SaleLineDraft item, int userId, long saleId, DateTime soldUtc)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO inventory_movements (product_id, movement_type, quantity, source_document, reference_id, created_by_user_id, created_utc, note)
VALUES (@product_id, 'venta', @quantity, 'sales', @reference_id, @created_by_user_id, @created_utc, 'Descuento por venta');",
                new SQLiteParameter("@product_id", item.ProductId),
                new SQLiteParameter("@quantity", item.Quantity),
                new SQLiteParameter("@reference_id", saleId),
                new SQLiteParameter("@created_by_user_id", userId),
                new SQLiteParameter("@created_utc", soldUtc.ToString("o")));
        }

        private static void AttachSaleToCashShift(SQLiteConnection connection, SQLiteTransaction transaction, int cashShiftId, long saleId, decimal total, decimal estimatedCostTotal, decimal estimatedProfitTotal, PaymentMethod paymentMethod)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO cash_shift_sales (cash_shift_id, sale_id)
VALUES (@cash_shift_id, @sale_id);",
                new SQLiteParameter("@cash_shift_id", cashShiftId),
                new SQLiteParameter("@sale_id", saleId));

            var cashAmount = paymentMethod == PaymentMethod.Efectivo ? total : 0m;
            ExecuteNonQuery(
                connection,
                transaction,
                @"UPDATE cash_shifts
SET sales_total = sales_total + @sales_total,
    estimated_cost_total = estimated_cost_total + @estimated_cost_total,
    estimated_profit_total = estimated_profit_total + @estimated_profit_total,
    expected_cash = expected_cash + @cash_amount
WHERE id = @id AND status = 'abierta';",
                new SQLiteParameter("@sales_total", total),
                new SQLiteParameter("@estimated_cost_total", estimatedCostTotal),
                new SQLiteParameter("@estimated_profit_total", estimatedProfitTotal),
                new SQLiteParameter("@cash_amount", cashAmount),
                new SQLiteParameter("@id", cashShiftId));
        }

        private static void InsertAudit(SQLiteConnection connection, SQLiteTransaction transaction, string userName, long saleId, string folio, DateTime soldUtc)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO audit_log (event_type, entity_name, entity_id, user_name, details, created_utc)
VALUES ('sale.created', 'sales', @entity_id, @user_name, @details, @created_utc);",
                new SQLiteParameter("@entity_id", saleId.ToString()),
                new SQLiteParameter("@user_name", userName ?? string.Empty),
                new SQLiteParameter("@details", "Venta registrada con folio " + folio),
                new SQLiteParameter("@created_utc", soldUtc.ToString("o")));
        }

        private static int GetNextSequence(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT IFNULL(MAX(id), 0) + 1 FROM sales;";
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static long GetLastInsertId(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT last_insert_rowid();";
                return (long)command.ExecuteScalar();
            }
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
    }
}
