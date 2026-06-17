using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteSetupRepository : ISetupRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly IClock clock;

        public SqliteSetupRepository(SqliteConnectionFactory connectionFactory, IClock clock)
        {
            this.connectionFactory = connectionFactory;
            this.clock = clock;
        }

        public bool IsSystemConfigured()
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(1) FROM business_profile;";
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public AppStartupState LoadStartupState()
        {
            var result = new AppStartupState();

            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT bp.trade_name, IFNULL(bp.slogan, ''), IFNULL(bp.address, ''), IFNULL(bp.phone, ''), IFNULL(bp.header_text, ''), IFNULL(bp.footer_text, ''), bp.business_type_code, IFNULL(sp.station_name, ''), IFNULL(sp.station_code, ''), IFNULL(pp.printer_name, ''), IFNULL(tp.ticket_width_mm, 80), IFNULL(tp.use_full_paper_width, 1), IFNULL(tp.title_font_size, 12), IFNULL(tp.body_font_size, 9), IFNULL(tp.print_kitchen_ticket, 0), IFNULL(tp.show_system_footer, 0), IFNULL(tp.system_footer_text, ''), IFNULL(tp.horizontal_offset, 0), IFNULL(tp.vertical_offset, 0), IFNULL(tp.characters_per_line, CASE WHEN IFNULL(tp.ticket_width_mm, 80) = 58 THEN 30 ELSE 42 END), IFNULL(tp.info_font_size, 9), IFNULL(tp.items_font_size, 9), IFNULL(tp.total_font_size, 9), IFNULL(tp.footer_font_size, 9), IFNULL(tp.layout_name, 'Clásico compacto'), (SELECT COUNT(1) FROM dining_tables WHERE is_active = 1)
FROM business_profile bp
LEFT JOIN station_profile sp ON sp.id = 1
LEFT JOIN printer_profile pp ON pp.id = 1
LEFT JOIN ticket_profile tp ON tp.id = 1
WHERE bp.id = 1;";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.IsConfigured = true;
                        result.BusinessName = reader.GetString(0);
                        result.BusinessSlogan = reader.GetString(1);
                        result.BusinessAddress = reader.GetString(2);
                        result.BusinessPhone = reader.GetString(3);
                        result.TicketHeaderText = reader.GetString(4);
                        result.TicketFooterText = reader.GetString(5);
                        result.BusinessTypeCode = reader.GetString(6);
                        result.StationName = reader.GetString(7);
                        result.StationCode = reader.GetString(8);
                        result.ActivePrinterName = reader.GetString(9);
                        result.TicketWidthMm = reader.GetInt32(10);
                        result.UseFullPaperWidth = reader.GetInt32(11) == 1;
                        result.TicketTitleFontSize = reader.GetInt32(12);
                        result.TicketBodyFontSize = reader.GetInt32(13);
                        result.PrintKitchenTicket = reader.GetInt32(14) == 1;
                        result.ShowSystemFooter = reader.GetInt32(15) == 1;
                        result.TicketSystemFooterText = reader.GetString(16);
                        result.TicketHorizontalOffset = reader.GetInt32(17);
                        result.TicketVerticalOffset = reader.GetInt32(18);
                        result.TicketCharactersPerLine = reader.GetInt32(19);
                        result.TicketInfoFontSize = reader.GetInt32(20);
                        result.TicketItemsFontSize = reader.GetInt32(21);
                        result.TicketTotalFontSize = reader.GetInt32(22);
                        result.TicketFooterFontSize = reader.GetInt32(23);
                        result.TicketLayoutName = reader.GetString(24);
                        result.DiningTableCount = reader.GetInt32(25) <= 0 ? 5 : reader.GetInt32(25);
                    }
                }
            }

            return result;
        }

        public void ExecuteInitialSetup(InitialSetupPersistenceModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                if (SystemAlreadyConfigured(connection, transaction))
                {
                    throw new InvalidOperationException("El sistema ya se encuentra configurado.");
                }

                var now = this.clock.UtcNow.ToString("o");

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO business_profile (id, trade_name, business_type_code, slogan, address, phone, header_text, footer_text, created_utc, updated_utc)
VALUES (@id, @trade_name, @business_type_code, @slogan, @address, @phone, @header_text, @footer_text, @created_utc, @updated_utc);",
                    Parameter("@id", model.BusinessProfile.Id),
                    Parameter("@trade_name", model.BusinessProfile.TradeName),
                    Parameter("@business_type_code", model.BusinessProfile.BusinessTypeCode),
                    Parameter("@slogan", model.BusinessProfile.Slogan),
                    Parameter("@address", model.BusinessProfile.Address),
                    Parameter("@phone", model.BusinessProfile.Phone),
                    Parameter("@header_text", model.BusinessProfile.HeaderText),
                    Parameter("@footer_text", model.BusinessProfile.FooterText),
                    Parameter("@created_utc", now),
                    Parameter("@updated_utc", now));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO visual_theme (id, primary_color_hex, accent_color_hex, created_utc, updated_utc)
VALUES (@id, @primary_color_hex, @accent_color_hex, @created_utc, @updated_utc);",
                    Parameter("@id", model.VisualTheme.Id),
                    Parameter("@primary_color_hex", model.VisualTheme.PrimaryColorHex),
                    Parameter("@accent_color_hex", model.VisualTheme.AccentColorHex),
                    Parameter("@created_utc", now),
                    Parameter("@updated_utc", now));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO ticket_profile (id, ticket_width_mm, margin_left, margin_right, auto_open_drawer, created_utc, updated_utc)
VALUES (@id, @ticket_width_mm, @margin_left, @margin_right, @auto_open_drawer, @created_utc, @updated_utc);",
                    Parameter("@id", model.TicketProfile.Id),
                    Parameter("@ticket_width_mm", model.TicketProfile.TicketWidthMm),
                    Parameter("@margin_left", model.TicketProfile.MarginLeft),
                    Parameter("@margin_right", model.TicketProfile.MarginRight),
                    Parameter("@auto_open_drawer", ToBit(model.TicketProfile.AutoOpenDrawer)),
                    Parameter("@created_utc", now),
                    Parameter("@updated_utc", now));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO printer_profile (id, printer_name, printer_output_mode, drawer_command_hex, drawer_pulse_on_ms, drawer_pulse_off_ms, created_utc, updated_utc)
VALUES (@id, @printer_name, @printer_output_mode, @drawer_command_hex, @drawer_pulse_on_ms, @drawer_pulse_off_ms, @created_utc, @updated_utc);",
                    Parameter("@id", model.PrinterProfile.Id),
                    Parameter("@printer_name", model.PrinterProfile.PrinterName),
                    Parameter("@printer_output_mode", (int)model.PrinterProfile.OutputMode),
                    Parameter("@drawer_command_hex", model.PrinterProfile.DrawerCommandHex),
                    Parameter("@drawer_pulse_on_ms", model.PrinterProfile.DrawerPulseOnMilliseconds),
                    Parameter("@drawer_pulse_off_ms", model.PrinterProfile.DrawerPulseOffMilliseconds),
                    Parameter("@created_utc", now),
                    Parameter("@updated_utc", now));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO station_profile (id, station_name, station_code, created_utc, updated_utc)
VALUES (@id, @station_name, @station_code, @created_utc, @updated_utc);",
                    Parameter("@id", model.StationProfile.Id),
                    Parameter("@station_name", model.StationProfile.StationName),
                    Parameter("@station_code", model.StationProfile.StationCode),
                    Parameter("@created_utc", now),
                    Parameter("@updated_utc", now));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO users (username, display_name, password_hash, password_salt, role_code, is_active, created_utc, updated_utc)
VALUES (@username, @display_name, @password_hash, @password_salt, @role_code, 1, @created_utc, @updated_utc);",
                    Parameter("@username", model.AdminUser.Username),
                    Parameter("@display_name", model.AdminUser.DisplayName),
                    Parameter("@password_hash", model.AdminUser.PasswordHashBase64),
                    Parameter("@password_salt", model.AdminUser.PasswordSaltBase64),
                    Parameter("@role_code", model.AdminUser.RoleCode),
                    Parameter("@created_utc", now),
                    Parameter("@updated_utc", now));

                var categoriesByName = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                foreach (var category in model.Categories)
                {
                    var categoryId = ExecuteInsertAndGetId(
                        connection,
                        transaction,
                        @"INSERT INTO categories (name, sort_order, is_active, created_utc)
VALUES (@name, @sort_order, 1, @created_utc);",
                        Parameter("@name", category.Name),
                        Parameter("@sort_order", category.SortOrder),
                        Parameter("@created_utc", now));
                    categoriesByName[category.Name] = categoryId;
                }

                foreach (var product in model.Products)
                {
                    long categoryId;
                    if (!categoriesByName.TryGetValue(product.CategoryName, out categoryId))
                    {
                        continue;
                    }

                    ExecuteNonQuery(
                        connection,
                        transaction,
                        @"INSERT INTO products (category_id, sku, name, sale_price, estimated_cost, uses_inventory, stock_on_hand, is_active, created_utc)
VALUES (@category_id, @sku, @name, @sale_price, @estimated_cost, @uses_inventory, @stock_on_hand, 1, @created_utc);",
                        Parameter("@category_id", categoryId),
                        Parameter("@sku", string.Empty),
                        Parameter("@name", product.Name),
                        Parameter("@sale_price", product.SalePrice),
                        Parameter("@estimated_cost", product.EstimatedCost),
                        Parameter("@uses_inventory", ToBit(product.UsesInventory)),
                        Parameter("@stock_on_hand", product.StockOnHand),
                        Parameter("@created_utc", now));
                }

                InsertAudit(connection, transaction, "setup.initialized", "business_profile", "1", model.AdminUser.Username, "Configuración inicial completada.", now);
                transaction.Commit();
            }
        }

        private static bool SystemAlreadyConfigured(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT COUNT(1) FROM business_profile;";
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static void InsertAudit(SQLiteConnection connection, SQLiteTransaction transaction, string eventType, string entityName, string entityId, string userName, string details, string createdUtc)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                @"INSERT INTO audit_log (event_type, entity_name, entity_id, user_name, details, created_utc)
VALUES (@event_type, @entity_name, @entity_id, @user_name, @details, @created_utc);",
                Parameter("@event_type", eventType),
                Parameter("@entity_name", entityName),
                Parameter("@entity_id", entityId),
                Parameter("@user_name", userName),
                Parameter("@details", details),
                Parameter("@created_utc", createdUtc));
        }

        private static long ExecuteInsertAndGetId(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params SQLiteParameter[] parameters)
        {
            ExecuteNonQuery(connection, transaction, sql, parameters);
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

        private static SQLiteParameter Parameter(string name, object value)
        {
            return new SQLiteParameter(name, value ?? DBNull.Value);
        }

        private static int ToBit(bool value)
        {
            return value ? 1 : 0;
        }
    }
}
