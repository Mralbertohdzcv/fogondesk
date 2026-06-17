using System;
using System.Data.SQLite;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteTicketPrintSettingsRepository : ITicketPrintSettingsRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteTicketPrintSettingsRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public TicketPrintSettingsView LoadSettings()
        {
            var settings = new TicketPrintSettingsView
            {
                TicketWidthMm = 80,
                UseFullPaperWidth = true,
                TicketCharactersPerLine = 42,
                DiningTableCount = 5,
                TicketTitleFontSize = 12,
                TicketBodyFontSize = 9,
                TicketInfoFontSize = 9,
                TicketItemsFontSize = 9,
                TicketTotalFontSize = 9,
                TicketFooterFontSize = 9,
                TicketHorizontalOffset = 0,
                TicketVerticalOffset = 0,
                PrintKitchenTicket = false,
                ShowSystemFooter = false,
                TicketLayoutName = "Clásico compacto",
                BusinessName = string.Empty,
                Address = string.Empty,
                Phone = string.Empty,
                SystemFooterText = string.Empty,
                HeaderText = string.Empty,
                FooterText = string.Empty,
                Slogan = string.Empty
            };

            using (var connection = this.connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT IFNULL(pp.printer_name, ''), IFNULL(tp.ticket_width_mm, 80), IFNULL(tp.use_full_paper_width, 1), IFNULL(tp.title_font_size, 12), IFNULL(tp.body_font_size, 9), IFNULL(tp.print_kitchen_ticket, 0), IFNULL(tp.show_system_footer, 0), IFNULL(tp.layout_name, 'Clásico compacto'), IFNULL(tp.system_footer_text, ''), IFNULL(tp.horizontal_offset, 0), IFNULL(tp.vertical_offset, 0), IFNULL(tp.characters_per_line, CASE WHEN IFNULL(tp.ticket_width_mm, 80) = 58 THEN 30 ELSE 42 END), IFNULL(tp.info_font_size, 9), IFNULL(tp.items_font_size, 9), IFNULL(tp.total_font_size, 9), IFNULL(tp.footer_font_size, 9)
FROM ticket_profile tp
LEFT JOIN printer_profile pp ON pp.id = 1
WHERE tp.id = 1;";
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings.PrinterName = reader.GetString(0);
                            settings.TicketWidthMm = reader.GetInt32(1);
                            settings.UseFullPaperWidth = reader.GetInt32(2) == 1;
                            settings.TicketTitleFontSize = reader.GetInt32(3);
                            settings.TicketBodyFontSize = reader.GetInt32(4);
                            settings.PrintKitchenTicket = reader.GetInt32(5) == 1;
                            settings.ShowSystemFooter = reader.GetInt32(6) == 1;
                            settings.TicketLayoutName = reader.GetString(7);
                            settings.SystemFooterText = reader.GetString(8);
                            settings.TicketHorizontalOffset = reader.GetInt32(9);
                            settings.TicketVerticalOffset = reader.GetInt32(10);
                            settings.TicketCharactersPerLine = reader.GetInt32(11);
                            settings.TicketInfoFontSize = reader.GetInt32(12);
                            settings.TicketItemsFontSize = reader.GetInt32(13);
                            settings.TicketTotalFontSize = reader.GetInt32(14);
                            settings.TicketFooterFontSize = reader.GetInt32(15);
                        }
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT IFNULL(trade_name, ''), IFNULL(header_text, ''), IFNULL(footer_text, ''), IFNULL(slogan, ''), IFNULL(address, ''), IFNULL(phone, '') FROM business_profile WHERE id = 1;";
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings.BusinessName = reader.GetString(0);
                            settings.HeaderText = reader.GetString(1);
                            settings.FooterText = reader.GetString(2);
                            settings.Slogan = reader.GetString(3);
                            settings.Address = reader.GetString(4);
                            settings.Phone = reader.GetString(5);
                        }
                    }
                }
            }

            return settings;
        }

        public void SaveSettings(TicketPrintSettingsView settings, DateTime updatedUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"UPDATE printer_profile
SET printer_name = @printer_name,
    updated_utc = @updated_utc
WHERE id = 1;",
                    Parameter("@printer_name", settings.PrinterName ?? string.Empty),
                    Parameter("@updated_utc", updatedUtc.ToString("o")));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"UPDATE ticket_profile
SET ticket_width_mm = @ticket_width_mm,
    use_full_paper_width = @use_full_paper_width,
    characters_per_line = @characters_per_line,
    title_font_size = @title_font_size,
    body_font_size = @body_font_size,
    info_font_size = @info_font_size,
    items_font_size = @items_font_size,
    total_font_size = @total_font_size,
    footer_font_size = @footer_font_size,
    print_kitchen_ticket = @print_kitchen_ticket,
    show_system_footer = @show_system_footer,
    system_footer_text = @system_footer_text,
    horizontal_offset = @horizontal_offset,
    vertical_offset = @vertical_offset,
    layout_name = @layout_name,
    updated_utc = @updated_utc
WHERE id = 1;",
                    Parameter("@ticket_width_mm", settings.TicketWidthMm),
                    Parameter("@use_full_paper_width", settings.UseFullPaperWidth ? 1 : 0),
                    Parameter("@characters_per_line", settings.TicketCharactersPerLine),
                    Parameter("@title_font_size", settings.TicketTitleFontSize),
                    Parameter("@body_font_size", settings.TicketBodyFontSize),
                    Parameter("@info_font_size", settings.TicketInfoFontSize),
                    Parameter("@items_font_size", settings.TicketItemsFontSize),
                    Parameter("@total_font_size", settings.TicketTotalFontSize),
                    Parameter("@footer_font_size", settings.TicketFooterFontSize),
                    Parameter("@print_kitchen_ticket", settings.PrintKitchenTicket ? 1 : 0),
                    Parameter("@show_system_footer", settings.ShowSystemFooter ? 1 : 0),
                    Parameter("@system_footer_text", settings.SystemFooterText ?? string.Empty),
                    Parameter("@horizontal_offset", settings.TicketHorizontalOffset),
                    Parameter("@vertical_offset", settings.TicketVerticalOffset),
                    Parameter("@layout_name", string.IsNullOrWhiteSpace(settings.TicketLayoutName) ? "Clásico compacto" : settings.TicketLayoutName.Trim()),
                    Parameter("@updated_utc", updatedUtc.ToString("o")));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"UPDATE business_profile
SET header_text = @header_text,
    footer_text = @footer_text,
    slogan = @slogan,
    trade_name = @trade_name,
    address = @address,
    phone = @phone,
    updated_utc = @updated_utc
WHERE id = 1;",
                    Parameter("@header_text", settings.HeaderText ?? string.Empty),
                    Parameter("@footer_text", settings.FooterText ?? string.Empty),
                    Parameter("@slogan", settings.Slogan ?? string.Empty),
                    Parameter("@trade_name", string.IsNullOrWhiteSpace(settings.BusinessName) ? string.Empty : settings.BusinessName.Trim()),
                    Parameter("@address", settings.Address ?? string.Empty),
                    Parameter("@phone", settings.Phone ?? string.Empty),
                    Parameter("@updated_utc", updatedUtc.ToString("o")));

                transaction.Commit();
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
    }
}