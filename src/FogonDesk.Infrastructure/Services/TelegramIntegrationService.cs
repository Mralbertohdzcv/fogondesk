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
using FogonDesk.Infrastructure.Data;

namespace FogonDesk.Infrastructure.Services
{
    public sealed class TelegramIntegrationService : ITelegramIntegrationService
    {
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly SqliteConnectionFactory connectionFactory;
        private readonly IClock clock;
        private readonly IAppLogger logger;

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
            responseMessage = "Codigo invalido o vencido.";
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
                        responseMessage = "Ese codigo ya fue usado.";
                        transaction.Commit();
                        return false;
                    }

                    if (expiresUtc < this.clock.UtcNow)
                    {
                        responseMessage = "Ese codigo ya vencio. Genera uno nuevo.";
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
                    responseMessage = "Chat vinculado correctamente a FogonDesk POS.";
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
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
