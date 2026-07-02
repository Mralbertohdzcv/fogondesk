using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;
using FogonDesk.Infrastructure.Data;

namespace FogonDesk.Infrastructure.Services
{
    public sealed class TelegramIntegrationService : ITelegramIntegrationService
    {
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly SqliteConnectionFactory connectionFactory;
        private readonly IClock clock;
        private readonly IAppLogger logger;
        private readonly Dictionary<long, ChatConversationState> chatStates = new Dictionary<long, ChatConversationState>();

        public TelegramIntegrationService(SqliteConnectionFactory connectionFactory, IClock clock, IAppLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.clock = clock;
            this.logger = logger;
        }

        public TelegramSettingsView GetSettings()
        {
            var settings = new TelegramSettingsView
            {
                BotToken = string.Empty,
                LastUpdateId = 0,
                LinkedChats = new List<TelegramLinkedChatView>()
            };

            using (var connection = this.connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT IFNULL(bot_token, ''), IFNULL(last_update_id, 0) FROM telegram_settings WHERE id = 1 LIMIT 1;";
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings.BotToken = reader.GetString(0);
                            settings.LastUpdateId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture);
                        }
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT chat_id, IFNULL(username, ''), IFNULL(display_name, ''), linked_utc FROM telegram_linked_chats ORDER BY linked_utc DESC;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ((List<TelegramLinkedChatView>)settings.LinkedChats).Add(new TelegramLinkedChatView
                            {
                                ChatId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                                Username = reader.GetString(1),
                                DisplayName = reader.GetString(2),
                                LinkedUtc = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                            });
                        }
                    }
                }
            }

            return settings;
        }

        public OperationResult SaveBotToken(string botToken)
        {
            var sanitized = (botToken ?? string.Empty).Trim();
            if (sanitized.Length > 0 && !sanitized.Contains(":"))
            {
                return OperationResult.Fail("El token de Telegram no parece valido.");
            }

            try
            {
                using (var connection = this.connectionFactory.CreateOpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"UPDATE telegram_settings
SET bot_token = @bot_token,
    updated_utc = @updated_utc
WHERE id = 1;";
                    command.Parameters.AddWithValue("@bot_token", sanitized);
                    command.Parameters.AddWithValue("@updated_utc", this.clock.UtcNow.ToString("o"));
                    command.ExecuteNonQuery();
                }

                return OperationResult.Ok("Configuracion de Telegram guardada.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible guardar el token de Telegram.", exception);
                return OperationResult.Fail("No fue posible guardar el token de Telegram.");
            }
        }

        public OperationResult<TelegramLinkCodeResult> GenerateLinkCode(int expiresInMinutes)
        {
            var minutes = expiresInMinutes <= 0 ? 10 : expiresInMinutes;
            var now = this.clock.UtcNow;
            var expires = now.AddMinutes(minutes);
            var code = GenerateCode(6);

            try
            {
                using (var connection = this.connectionFactory.CreateOpenConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO telegram_link_codes (code, created_utc, expires_utc, consumed_utc, consumed_chat_id)
VALUES (@code, @created_utc, @expires_utc, NULL, NULL);";
                    command.Parameters.AddWithValue("@code", code);
                    command.Parameters.AddWithValue("@created_utc", now.ToString("o"));
                    command.Parameters.AddWithValue("@expires_utc", expires.ToString("o"));
                    command.ExecuteNonQuery();
                }

                return OperationResult<TelegramLinkCodeResult>.Ok(new TelegramLinkCodeResult
                {
                    Code = code,
                    ExpiresUtc = expires
                }, "Codigo generado correctamente.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible generar el codigo de vinculacion Telegram.", exception);
                return OperationResult<TelegramLinkCodeResult>.Fail("No fue posible generar el codigo de vinculacion.");
            }
        }

        public OperationResult<int> SyncLinkRequests()
        {
            try
            {
                var settings = GetSettings();
                var token = (settings.BotToken ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return OperationResult<int>.Fail("Configura primero el token del bot de Telegram.");
                }

                var updates = GetUpdates(token, settings.LastUpdateId + 1);
                var linkedChatIds = new HashSet<long>((settings.LinkedChats ?? new List<TelegramLinkedChatView>()).Select(item => item.ChatId));
                var linkedCount = 0;
                long maxUpdateId = settings.LastUpdateId;
                foreach (var update in updates)
                {
                    if (update == null)
                    {
                        continue;
                    }

                    if (update.UpdateId > maxUpdateId)
                    {
                        maxUpdateId = update.UpdateId;
                    }

                    if (update.Message == null || update.Message.Chat == null)
                    {
                        continue;
                    }

                    var chat = update.Message.Chat;
                    var text = (update.Message.Text ?? string.Empty).Trim();

                    if (linkedChatIds.Contains(chat.Id))
                    {
                        TrySendMessage(token, chat.Id, HandleAdminCommand(chat.Id, text));
                        continue;
                    }

                    var code = ExtractCodeFromMessage(text);
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        continue;
                    }

                    string responseMessage;
                    if (TryConsumeCodeAndLinkChat(code, chat.Id, chat.Username, BuildDisplayName(chat), out responseMessage))
                    {
                        linkedCount += 1;
                    }

                    TrySendMessage(token, chat.Id, responseMessage);
                }

                SaveLastUpdateId(maxUpdateId);
                return OperationResult<int>.Ok(linkedCount, linkedCount > 0
                    ? "Vinculaciones aplicadas: " + linkedCount + "."
                    : "No hubo codigos nuevos para vincular.");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible sincronizar solicitudes de Telegram.", exception);
                return OperationResult<int>.Fail("No fue posible sincronizar Telegram. Verifica token y conexion.");
            }
        }

        public OperationResult SendAdminBroadcast(string message)
        {
            var text = (message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return OperationResult.Fail("El mensaje de Telegram es obligatorio.");
            }

            try
            {
                var settings = GetSettings();
                var token = (settings.BotToken ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return OperationResult.Ok();
                }

                var sent = 0;
                foreach (var chat in settings.LinkedChats ?? new List<TelegramLinkedChatView>())
                {
                    if (TrySendMessage(token, chat.ChatId, text))
                    {
                        sent += 1;
                    }
                }

                return OperationResult.Ok("Notificaciones Telegram enviadas: " + sent + ".");
            }
            catch (Exception exception)
            {
                this.logger.Error("No fue posible enviar notificaciones Telegram.", exception);
                return OperationResult.Fail("No fue posible enviar la notificacion a Telegram.");
            }
        }

        private bool TryConsumeCodeAndLinkChat(string code, long chatId, string username, string displayName, out string responseMessage)
        {
            responseMessage = "⚠️ Código inválido o vencido. Pide uno nuevo al administrador.";
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    DateTime expiresUtc;
                    string consumedUtcRaw;
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"SELECT expires_utc, consumed_utc
FROM telegram_link_codes
WHERE code = @code
LIMIT 1;";
                        command.Parameters.AddWithValue("@code", code);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                transaction.Commit();
                                return false;
                            }

                            expiresUtc = DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                            consumedUtcRaw = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(consumedUtcRaw))
                    {
                        responseMessage = "⚠️ Ese código ya fue usado.";
                        transaction.Commit();
                        return false;
                    }

                    if (expiresUtc < this.clock.UtcNow)
                    {
                        responseMessage = "⚠️ Ese código ya venció. Pide uno nuevo al administrador.";
                        transaction.Commit();
                        return false;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"INSERT INTO telegram_linked_chats (chat_id, username, display_name, linked_utc)
VALUES (@chat_id, @username, @display_name, @linked_utc)
ON CONFLICT(chat_id) DO UPDATE SET
    username = excluded.username,
    display_name = excluded.display_name,
    linked_utc = excluded.linked_utc;";
                        command.Parameters.AddWithValue("@chat_id", chatId);
                        command.Parameters.AddWithValue("@username", username ?? string.Empty);
                        command.Parameters.AddWithValue("@display_name", displayName ?? string.Empty);
                        command.Parameters.AddWithValue("@linked_utc", this.clock.UtcNow.ToString("o"));
                        command.ExecuteNonQuery();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"UPDATE telegram_link_codes
SET consumed_utc = @consumed_utc,
    consumed_chat_id = @consumed_chat_id
WHERE code = @code;";
                        command.Parameters.AddWithValue("@consumed_utc", this.clock.UtcNow.ToString("o"));
                        command.Parameters.AddWithValue("@consumed_chat_id", chatId);
                        command.Parameters.AddWithValue("@code", code);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    responseMessage = "✅ ¡Listo! Tu chat quedó vinculado a FogonDesk POS.\n\nEscribe \"hola\" para ver en qué puedo ayudarte.";
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static readonly string[] GreetingKeywords =
        {
            "hola", "hi", "hey", "buenas", "buenos dias", "buenas tardes", "buenas noches",
            "menu", "opciones", "ayuda", "help", "inicio", "start", "/start", "?"
        };

        private static readonly string[] SpanishDayNames = { "Domingo", "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado" };

        private static readonly string[] SpanishMonthNames =
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        private const string FarewellMessage = "🙌 ¡Fue un gusto ayudarte! Escríbeme un saludo (por ejemplo \"hola\") cuando necesites otra consulta.";
        private const string IdleNudgeMessage = "👋 Escríbeme un saludo para iniciar una nueva consulta.";

        private enum ConversationStep
        {
            Idle,
            Root,
            LocalMenu,
            PlatformsMenu,
            PlatformsMonthSelect
        }

        private sealed class ChatConversationState
        {
            public ConversationStep Step { get; set; }
            public string PendingPlatform { get; set; }
            public List<DateTime> AvailableMonths { get; set; }
        }

        private sealed class PlatformSaleRecord
        {
            public DateTime SoldLocal { get; set; }
            public string PlatformName { get; set; }
            public decimal SecondaryAmount { get; set; }
            public string PaymentMethod { get; set; }
        }

        private string HandleAdminCommand(long chatId, string rawText)
        {
            var normalized = RemoveDiacritics((rawText ?? string.Empty).Trim().ToLowerInvariant());

            ChatConversationState state;
            if (!this.chatStates.TryGetValue(chatId, out state))
            {
                state = new ChatConversationState { Step = ConversationStep.Idle };
                this.chatStates[chatId] = state;
            }

            if (GreetingKeywords.Contains(normalized))
            {
                state.Step = ConversationStep.Root;
                return BuildGreetingMessage();
            }

            switch (state.Step)
            {
                case ConversationStep.Idle:
                    return IdleNudgeMessage;

                case ConversationStep.Root:
                    if (normalized == "1")
                    {
                        state.Step = ConversationStep.LocalMenu;
                        return BuildLocalMenuMessage();
                    }

                    if (normalized == "2")
                    {
                        state.Step = ConversationStep.PlatformsMenu;
                        return BuildPlatformsMenuMessage();
                    }

                    return "🤔 No entendí. Responde con el número de la opción.\n\n" + BuildRootOptionsMessage();

                case ConversationStep.LocalMenu:
                    if (normalized == "1")
                    {
                        state.Step = ConversationStep.Idle;
                        return BuildWeeklySalesSummaryMessage() + "\n\n" + FarewellMessage;
                    }

                    if (normalized == "2")
                    {
                        state.Step = ConversationStep.Idle;
                        return BuildWeeklyTicketsMessage() + "\n\n" + FarewellMessage;
                    }

                    return "🤔 No entendí. Responde con el número de la opción.\n\n" + BuildLocalMenuMessage();

                case ConversationStep.PlatformsMenu:
                    if (normalized == "1" || normalized == "2" || normalized == "3")
                    {
                        var platformKeyword = normalized == "1" ? "didi" : normalized == "2" ? "rappi" : "ambas";
                        var platformLabel = normalized == "1" ? "Didi" : normalized == "2" ? "Rappi" : "Didi y Rappi";
                        var records = LoadPlatformSaleRecords();
                        var months = platformKeyword == "ambas"
                            ? GetAvailableMonths(records, "didi").Union(GetAvailableMonths(records, "rappi")).Distinct().OrderByDescending(item => item).ToList()
                            : GetAvailableMonths(records, platformKeyword);
                        if (months.Count == 0)
                        {
                            state.Step = ConversationStep.Idle;
                            return "📭 Todavía no hay registros de " + platformLabel + ".\n\n" + FarewellMessage;
                        }

                        state.Step = ConversationStep.PlatformsMonthSelect;
                        state.PendingPlatform = platformKeyword;
                        state.AvailableMonths = months;
                        return BuildMonthSelectionMessage(platformLabel, months);
                    }

                    return "🤔 No entendí. Responde con el número de la opción.\n\n" + BuildPlatformsMenuMessage();

                case ConversationStep.PlatformsMonthSelect:
                    int monthIndex;
                    if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out monthIndex)
                        && state.AvailableMonths != null
                        && monthIndex >= 1 && monthIndex <= state.AvailableMonths.Count)
                    {
                        var monthStart = state.AvailableMonths[monthIndex - 1];
                        var records = LoadPlatformSaleRecords();
                        string report;
                        if (string.Equals(state.PendingPlatform, "ambas", StringComparison.OrdinalIgnoreCase))
                        {
                            report = BuildDidiMonthlyReportMessage(records, monthStart) + "\n\n— — —\n\n" + BuildRappiMonthlyReportMessage(records, monthStart);
                        }
                        else
                        {
                            report = string.Equals(state.PendingPlatform, "didi", StringComparison.OrdinalIgnoreCase)
                                ? BuildDidiMonthlyReportMessage(records, monthStart)
                                : BuildRappiMonthlyReportMessage(records, monthStart);
                        }

                        state.Step = ConversationStep.Idle;
                        return report + "\n\n" + FarewellMessage;
                    }

                    return "🤔 Responde con el número del mes de la lista.\n\n"
                        + BuildMonthSelectionMessage(
                            string.Equals(state.PendingPlatform, "didi", StringComparison.OrdinalIgnoreCase) ? "Didi" : string.Equals(state.PendingPlatform, "rappi", StringComparison.OrdinalIgnoreCase) ? "Rappi" : "Didi y Rappi",
                            state.AvailableMonths ?? new List<DateTime>());

                default:
                    return IdleNudgeMessage;
            }
        }

        private string BuildGreetingMessage()
        {
            return "👋 ¡Hola! Soy el asistente de " + GetBusinessName() + ".\n\n" + BuildRootOptionsMessage();
        }

        private static string BuildRootOptionsMessage()
        {
            return "Responde con el número de una opción:\n"
                + "1️⃣ Ventas Local\n"
                + "2️⃣ Ventas Plataformas";
        }

        private static string BuildLocalMenuMessage()
        {
            return "Responde con el número de una opción:\n"
                + "1️⃣ Ventas de la semana\n"
                + "2️⃣ Tickets de la semana";
        }

        private static string BuildPlatformsMenuMessage()
        {
            return "Responde con el número de una opción:\n"
                + "1️⃣ Ganancias mensuales Didi\n"
                + "2️⃣ Ganancias mensuales Rappi\n"
                + "3️⃣ Ambas";
        }

        private string GetBusinessName()
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT IFNULL(trade_name, '') FROM business_profile WHERE id = 1 LIMIT 1;";
                var result = command.ExecuteScalar();
                var name = result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result, CultureInfo.InvariantCulture);
                return string.IsNullOrWhiteSpace(name) ? "FogonDesk POS" : name.Trim();
            }
        }

        private string BuildWeeklySalesSummaryMessage()
        {
            var weekStartUtc = GetCurrentWeekStartUtc();
            var totalSales = 0m;
            var ticketCount = 0;

            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT total FROM sales WHERE status = 1 AND sold_utc >= @start;";
                command.Parameters.AddWithValue("@start", weekStartUtc.ToString("o"));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        totalSales += Convert.ToDecimal(reader.GetValue(0), CultureInfo.InvariantCulture);
                        ticketCount += 1;
                    }
                }
            }

            if (ticketCount == 0)
            {
                return "📊 Todavía no hay ventas confirmadas esta semana.";
            }

            return "📊 Ventas de la semana\n\n"
                + "🧾 Tickets confirmados: " + ticketCount.ToString(CultureInfo.InvariantCulture) + "\n"
                + "💰 Total vendido: $" + totalSales.ToString("N2", CultureInfo.InvariantCulture);
        }

        private string BuildWeeklyTicketsMessage()
        {
            var weekStartUtc = GetCurrentWeekStartUtc();
            var buckets = new List<DayBucket>();
            var totalCount = 0;
            var totalAmount = 0m;

            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT sold_utc, total FROM sales WHERE status = 1 AND sold_utc >= @start ORDER BY sold_utc ASC;";
                command.Parameters.AddWithValue("@start", weekStartUtc.ToString("o"));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var soldUtc = DateTime.SpecifyKind(DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);
                        var soldLocal = soldUtc.ToLocalTime();
                        var amount = Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
                        var dayLabel = SpanishDayNames[(int)soldLocal.DayOfWeek] + " " + soldLocal.ToString("dd/MM", CultureInfo.InvariantCulture);

                        var bucket = buckets.Count > 0 && buckets[buckets.Count - 1].Label == dayLabel ? buckets[buckets.Count - 1] : null;
                        if (bucket == null)
                        {
                            bucket = new DayBucket { Label = dayLabel };
                            buckets.Add(bucket);
                        }

                        bucket.Count += 1;
                        bucket.Total += amount;
                        totalCount += 1;
                        totalAmount += amount;
                    }
                }
            }

            if (totalCount == 0)
            {
                return "🧾 Todavía no hay tickets confirmados esta semana.";
            }

            var builder = new StringBuilder();
            builder.Append("🧾 Tickets de la semana\n\n");
            foreach (var bucket in buckets)
            {
                builder.Append("📅 ").Append(bucket.Label).Append(": ").Append(bucket.Count).Append(" tickets, $").Append(bucket.Total.ToString("N2", CultureInfo.InvariantCulture)).Append("\n");
            }

            builder.Append("\n💰 Total semana: ").Append(totalCount).Append(" tickets, $").Append(totalAmount.ToString("N2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private List<PlatformSaleRecord> LoadPlatformSaleRecords()
        {
            var records = new List<PlatformSaleRecord>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT sold_utc, IFNULL(note, ''), IFNULL(payment_summary, '') FROM sales WHERE status = 1 AND order_kind = @orderKind;";
                command.Parameters.AddWithValue("@orderKind", (int)OrderKind.PlataformaDigital);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var soldUtc = DateTime.SpecifyKind(DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);
                        var note = reader.GetString(1);
                        records.Add(new PlatformSaleRecord
                        {
                            SoldLocal = soldUtc.ToLocalTime(),
                            PlatformName = ExtractPlatformName(note),
                            SecondaryAmount = ExtractSecondaryAmount(note),
                            PaymentMethod = reader.GetString(2)
                        });
                    }
                }
            }

            return records;
        }

        private static List<DateTime> GetAvailableMonths(List<PlatformSaleRecord> records, string platformKeyword)
        {
            return records
                .Where(item => RemoveDiacritics((item.PlatformName ?? string.Empty).ToLowerInvariant()).Contains(platformKeyword))
                .Select(item => new DateTime(item.SoldLocal.Year, item.SoldLocal.Month, 1))
                .Distinct()
                .OrderByDescending(item => item)
                .ToList();
        }

        private string BuildMonthSelectionMessage(string platformLabel, List<DateTime> months)
        {
            var nowLocal = this.clock.UtcNow.ToLocalTime();
            var builder = new StringBuilder();
            builder.Append("📅 ¿De qué mes quieres ver las ganancias de ").Append(platformLabel).Append("?\n\n");
            for (var index = 0; index < months.Count; index++)
            {
                var isCurrent = months[index].Year == nowLocal.Year && months[index].Month == nowLocal.Month;
                builder.Append(index + 1).Append(". ").Append(SpanishMonthNames[months[index].Month - 1]).Append(' ').Append(months[index].Year)
                    .Append(isCurrent ? " (mes actual)" : string.Empty).Append('\n');
            }

            return builder.ToString().TrimEnd();
        }

        private static string BuildRappiMonthlyReportMessage(List<PlatformSaleRecord> records, DateTime monthStart)
        {
            var matching = records.Where(item =>
                RemoveDiacritics((item.PlatformName ?? string.Empty).ToLowerInvariant()).Contains("rappi")
                && item.SoldLocal.Year == monthStart.Year
                && item.SoldLocal.Month == monthStart.Month).ToList();

            var total = matching.Sum(item => item.SecondaryAmount);
            var cashItems = matching.Where(item => string.Equals(item.PaymentMethod, PaymentMethod.Efectivo.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
            var transferItems = matching.Where(item => string.Equals(item.PaymentMethod, PaymentMethod.Transferencia.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
            var monthLabel = SpanishMonthNames[monthStart.Month - 1] + " " + monthStart.Year;

            return "🛵 Rappi — " + monthLabel + "\n\n"
                + "💰 Subtotal restaurante: $" + total.ToString("N2", CultureInfo.InvariantCulture) + "\n"
                + "💵 Efectivo: $" + cashItems.Sum(item => item.SecondaryAmount).ToString("N2", CultureInfo.InvariantCulture) + "\n"
                + "   Pedidos: " + cashItems.Count.ToString(CultureInfo.InvariantCulture) + "\n"
                + "💳 Transferencia: $" + transferItems.Sum(item => item.SecondaryAmount).ToString("N2", CultureInfo.InvariantCulture) + "\n"
                + "   Pedidos: " + transferItems.Count.ToString(CultureInfo.InvariantCulture) + "\n"
                + "🧾 Total pedidos: " + matching.Count.ToString(CultureInfo.InvariantCulture);
        }

        private static string BuildDidiMonthlyReportMessage(List<PlatformSaleRecord> records, DateTime monthStart)
        {
            var matching = records.Where(item =>
                RemoveDiacritics((item.PlatformName ?? string.Empty).ToLowerInvariant()).Contains("didi")
                && item.SoldLocal.Year == monthStart.Year
                && item.SoldLocal.Month == monthStart.Month).ToList();

            var total = matching.Sum(item => item.SecondaryAmount);
            var cashItems = matching.Where(item => string.Equals(item.PaymentMethod, PaymentMethod.Efectivo.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
            var transferItems = matching.Where(item => string.Equals(item.PaymentMethod, PaymentMethod.Transferencia.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
            var monthLabel = SpanishMonthNames[monthStart.Month - 1] + " " + monthStart.Year;

            return "🛵 Didi — " + monthLabel + "\n\n"
                + "💰 Ganancias por pedido: $" + total.ToString("N2", CultureInfo.InvariantCulture) + "\n"
                + "💵 Efectivo: $" + cashItems.Sum(item => item.SecondaryAmount).ToString("N2", CultureInfo.InvariantCulture) + "\n"
                + "   Pedidos: " + cashItems.Count.ToString(CultureInfo.InvariantCulture) + "\n"
                + "💳 Transferencia: $" + transferItems.Sum(item => item.SecondaryAmount).ToString("N2", CultureInfo.InvariantCulture) + "\n"
                + "   Pedidos: " + transferItems.Count.ToString(CultureInfo.InvariantCulture) + "\n"
                + "🧾 Total pedidos: " + matching.Count.ToString(CultureInfo.InvariantCulture);
        }

        private static decimal ExtractSecondaryAmount(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return 0m;
            }

            var segments = note.Split('|');
            var lastSegment = segments[segments.Length - 1];
            var dollarIndex = lastSegment.IndexOf('$');
            if (dollarIndex < 0)
            {
                return 0m;
            }

            var amountText = lastSegment.Substring(dollarIndex + 1).Trim();
            decimal amount;
            return decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) ? amount : 0m;
        }

        private static string ExtractPlatformName(string note)
        {
            const string prefix = "Plataforma: ";
            if (string.IsNullOrWhiteSpace(note) || !note.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return "Otra";
            }

            var rest = note.Substring(prefix.Length);
            var pipeIndex = rest.IndexOf('|');
            var name = pipeIndex >= 0 ? rest.Substring(0, pipeIndex) : rest;
            return name.Trim();
        }

        private DateTime GetCurrentWeekStartUtc()
        {
            var nowLocal = this.clock.UtcNow.ToLocalTime();
            var daysSinceMonday = ((int)nowLocal.DayOfWeek + 6) % 7;
            var weekStartLocal = DateTime.SpecifyKind(nowLocal.Date.AddDays(-daysSinceMonday), DateTimeKind.Local);
            return weekStartLocal.ToUniversalTime();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = (text ?? string.Empty).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private sealed class DayBucket
        {
            public string Label { get; set; }
            public int Count { get; set; }
            public decimal Total { get; set; }
        }

        private void SaveLastUpdateId(long lastUpdateId)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE telegram_settings
SET last_update_id = @last_update_id,
    updated_utc = @updated_utc
WHERE id = 1;";
                command.Parameters.AddWithValue("@last_update_id", lastUpdateId < 0 ? 0 : lastUpdateId);
                command.Parameters.AddWithValue("@updated_utc", this.clock.UtcNow.ToString("o"));
                command.ExecuteNonQuery();
            }
        }

        private IList<TelegramUpdate> GetUpdates(string token, long offset)
        {
            var requestUrl = "https://api.telegram.org/bot" + token + "/getUpdates?offset=" + offset.ToString(CultureInfo.InvariantCulture) + "&timeout=0&allowed_updates=%5B%22message%22%5D";
            var response = GetJson<TelegramApiResponse<List<TelegramUpdate>>>(requestUrl);
            if (response == null || !response.Ok || response.Result == null)
            {
                return new List<TelegramUpdate>();
            }

            return response.Result;
        }

        private bool TrySendMessage(string token, long chatId, string text)
        {
            try
            {
                var url = "https://api.telegram.org/bot" + token + "/sendMessage?chat_id="
                    + chatId.ToString(CultureInfo.InvariantCulture)
                    + "&text=" + WebUtility.UrlEncode(text ?? string.Empty);
                var response = GetJson<TelegramApiResponse<object>>(url);
                return response != null && response.Ok;
            }
            catch (Exception exception)
            {
                this.logger.Warn("No fue posible enviar mensaje Telegram: " + exception.Message);
                return false;
            }
        }

        private static string ExtractCodeFromMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var trimmed = text.Trim();
            if (trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 1 ? parts[1].Trim().ToUpperInvariant() : string.Empty;
            }

            return trimmed.ToUpperInvariant();
        }

        private static string BuildDisplayName(TelegramChat chat)
        {
            var first = chat.FirstName ?? string.Empty;
            var last = chat.LastName ?? string.Empty;
            var combined = (first + " " + last).Trim();
            if (!string.IsNullOrWhiteSpace(combined))
            {
                return combined;
            }

            return string.IsNullOrWhiteSpace(chat.Username) ? "Telegram" : chat.Username;
        }

        private static T GetJson<T>(string requestUrl)
        {
            var request = WebRequest.CreateHttp(requestUrl);
            request.Method = "GET";
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;

            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    return default(T);
                }

                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }

        private static string GenerateCode(int size)
        {
            var bytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var builder = new StringBuilder(size);
            for (var index = 0; index < size; index++)
            {
                builder.Append(CodeAlphabet[bytes[index] % CodeAlphabet.Length]);
            }

            return builder.ToString();
        }

        [DataContract]
        private sealed class TelegramApiResponse<T>
        {
            [DataMember(Name = "ok")]
            public bool Ok { get; set; }

            [DataMember(Name = "result")]
            public T Result { get; set; }
        }

        [DataContract]
        private sealed class TelegramUpdate
        {
            [DataMember(Name = "update_id")]
            public long UpdateId { get; set; }

            [DataMember(Name = "message")]
            public TelegramMessage Message { get; set; }
        }

        [DataContract]
        private sealed class TelegramMessage
        {
            [DataMember(Name = "text")]
            public string Text { get; set; }

            [DataMember(Name = "chat")]
            public TelegramChat Chat { get; set; }
        }

        [DataContract]
        private sealed class TelegramChat
        {
            [DataMember(Name = "id")]
            public long Id { get; set; }

            [DataMember(Name = "username")]
            public string Username { get; set; }

            [DataMember(Name = "first_name")]
            public string FirstName { get; set; }

            [DataMember(Name = "last_name")]
            public string LastName { get; set; }
        }
    }
}
