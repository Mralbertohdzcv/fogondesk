using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Application.Utilities;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteCashShiftRepository : ICashShiftRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly FolioGenerator folioGenerator;

        public SqliteCashShiftRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
            this.folioGenerator = new FolioGenerator();
        }

        public CashShiftSummaryView FindActiveShift(string stationCode)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT cs.id, cs.folio, cs.station_code, u.display_name, cs.opened_utc, cs.closed_utc, cs.opening_cash, cs.expected_cash, cs.actual_cash, cs.sales_total, cs.estimated_profit_total, cs.difference_total, cs.status
FROM cash_shifts cs
INNER JOIN users u ON u.id = cs.opened_by_user_id
WHERE cs.station_code = @station_code AND cs.status = 'abierta'
ORDER BY cs.id DESC
LIMIT 1;";
                command.Parameters.AddWithValue("@station_code", stationCode);

                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadSummary(reader) : null;
                }
            }
        }

        public CashShiftSummaryView OpenShift(OpenCashShiftRequest request, DateTime openedUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var sequence = GetNextSequence(connection, transaction);
                var folio = this.folioGenerator.Generate("CJA", openedUtc, sequence);

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"INSERT INTO cash_shifts (folio, station_code, opened_by_user_id, closed_by_user_id, opened_utc, closed_utc, opening_cash, expected_cash, actual_cash, sales_total, estimated_cost_total, estimated_profit_total, difference_total, status)
VALUES (@folio, @station_code, @opened_by_user_id, NULL, @opened_utc, NULL, @opening_cash, @expected_cash, NULL, 0, 0, 0, NULL, 'abierta');";
                    command.Parameters.AddWithValue("@folio", folio);
                    command.Parameters.AddWithValue("@station_code", request.StationCode.Trim());
                    command.Parameters.AddWithValue("@opened_by_user_id", request.UserId);
                    command.Parameters.AddWithValue("@opened_utc", openedUtc.ToString("o"));
                    command.Parameters.AddWithValue("@opening_cash", request.OpeningCash);
                    command.Parameters.AddWithValue("@expected_cash", request.OpeningCash);
                    command.ExecuteNonQuery();
                }

                var shiftId = GetLastInsertId(connection, transaction);
                InsertAudit(connection, transaction, "cashshift.opened", shiftId, request.UserName, folio, openedUtc);
                transaction.Commit();
            }

            return FindActiveShift(request.StationCode.Trim());
        }

        public CashShiftSummaryView CloseShift(CloseCashShiftRequest request, DateTime closedUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                decimal expectedCash;
                using (var readCommand = connection.CreateCommand())
                {
                    readCommand.Transaction = transaction;
                    readCommand.CommandText = "SELECT expected_cash, status FROM cash_shifts WHERE id = @id LIMIT 1;";
                    readCommand.Parameters.AddWithValue("@id", request.ShiftId);
                    using (var reader = readCommand.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException("El corte indicado no existe.");
                        }

                        if (!string.Equals(reader.GetString(1), "abierta", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("El corte indicado ya fue cerrado.");
                        }

                        expectedCash = Convert.ToDecimal(reader.GetValue(0));
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"UPDATE cash_shifts
SET closed_by_user_id = @closed_by_user_id,
    closed_utc = @closed_utc,
    actual_cash = @actual_cash,
    difference_total = @difference_total,
    status = 'cerrada'
WHERE id = @id;";
                    command.Parameters.AddWithValue("@closed_by_user_id", request.UserId);
                    command.Parameters.AddWithValue("@closed_utc", closedUtc.ToString("o"));
                    command.Parameters.AddWithValue("@actual_cash", request.ActualCash);
                    command.Parameters.AddWithValue("@difference_total", request.ActualCash - expectedCash);
                    command.Parameters.AddWithValue("@id", request.ShiftId);
                    command.ExecuteNonQuery();
                }

                InsertAudit(connection, transaction, "cashshift.closed", request.ShiftId, request.UserName, request.ShiftId.ToString(), closedUtc);
                transaction.Commit();
            }

            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT cs.id, cs.folio, cs.station_code, u.display_name, cs.opened_utc, cs.closed_utc, cs.opening_cash, cs.expected_cash, cs.actual_cash, cs.sales_total, cs.estimated_profit_total, cs.difference_total, cs.status
FROM cash_shifts cs
INNER JOIN users u ON u.id = cs.opened_by_user_id
WHERE cs.id = @id
LIMIT 1;";
                command.Parameters.AddWithValue("@id", request.ShiftId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadSummary(reader) : null;
                }
            }
        }

        public IList<CashShiftSummaryView> LoadRecentShifts(string stationCode, int maxCount)
        {
            var result = new List<CashShiftSummaryView>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT cs.id, cs.folio, cs.station_code, u.display_name, cs.opened_utc, cs.closed_utc, cs.opening_cash, cs.expected_cash, cs.actual_cash, cs.sales_total, cs.estimated_profit_total, cs.difference_total, cs.status
FROM cash_shifts cs
INNER JOIN users u ON u.id = cs.opened_by_user_id
WHERE (@station_code = '' OR cs.station_code = @station_code)
ORDER BY cs.id DESC
LIMIT @limit;";
                command.Parameters.AddWithValue("@station_code", stationCode ?? string.Empty);
                command.Parameters.AddWithValue("@limit", maxCount);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(ReadSummary(reader));
                    }
                }
            }

            return result;
        }

        public int CountConfirmedSalesByOrderKind(int shiftId, int orderKind)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM sales WHERE cash_shift_id = @shiftId AND order_kind = @orderKind AND status = 1;";
                command.Parameters.AddWithValue("@shiftId", shiftId);
                command.Parameters.AddWithValue("@orderKind", orderKind);
                return System.Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static CashShiftSummaryView ReadSummary(SQLiteDataReader reader)
        {
            return new CashShiftSummaryView
            {
                ShiftId = reader.GetInt32(0),
                Folio = reader.GetString(1),
                StationCode = reader.GetString(2),
                OpenedByDisplayName = reader.GetString(3),
                OpenedUtc = DateTime.Parse(reader.GetString(4)),
                ClosedUtc = reader.IsDBNull(5) ? (DateTime?)null : DateTime.Parse(reader.GetString(5)),
                OpeningCash = Convert.ToDecimal(reader.GetValue(6)),
                ExpectedCash = Convert.ToDecimal(reader.GetValue(7)),
                ActualCash = reader.IsDBNull(8) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(8)),
                SalesTotal = Convert.ToDecimal(reader.GetValue(9)),
                EstimatedProfitTotal = Convert.ToDecimal(reader.GetValue(10)),
                DifferenceTotal = reader.IsDBNull(11) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(11)),
                Status = reader.GetString(12)
            };
        }

        private static int GetNextSequence(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT IFNULL(MAX(id), 0) + 1 FROM cash_shifts;";
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static int GetLastInsertId(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT last_insert_rowid();";
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void InsertAudit(SQLiteConnection connection, SQLiteTransaction transaction, string eventType, int entityId, string userName, string details, DateTime createdUtc)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO audit_log (event_type, entity_name, entity_id, user_name, details, created_utc)
VALUES (@event_type, 'cash_shifts', @entity_id, @user_name, @details, @created_utc);";
                command.Parameters.AddWithValue("@event_type", eventType);
                command.Parameters.AddWithValue("@entity_id", entityId.ToString());
                command.Parameters.AddWithValue("@user_name", userName ?? string.Empty);
                command.Parameters.AddWithValue("@details", details ?? string.Empty);
                command.Parameters.AddWithValue("@created_utc", createdUtc.ToString("o"));
                command.ExecuteNonQuery();
            }
        }
    }
}