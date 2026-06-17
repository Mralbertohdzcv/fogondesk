using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Infrastructure.Data
{
    public sealed class SqliteUserRepository : IUserRepository
    {
        private const string DeletedUserPrefix = "deleted:";
        private readonly SqliteConnectionFactory connectionFactory;

        public SqliteUserRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public UserAccountRecord FindByUsername(string username)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, username, display_name, password_hash, password_salt, role_code, is_active
FROM users
WHERE username = @username
LIMIT 1;";
                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new UserAccountRecord
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        DisplayName = reader.GetString(2),
                        PasswordHashBase64 = reader.GetString(3),
                        PasswordSaltBase64 = reader.GetString(4),
                        RoleCode = reader.GetString(5),
                        IsActive = reader.GetInt32(6) == 1,
                        LastLoginUtc = null
                    };
                }
            }
        }

        public UserAccountRecord FindById(int userId)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, username, display_name, password_hash, password_salt, role_code, is_active
FROM users
WHERE id = @id
LIMIT 1;";
                command.Parameters.AddWithValue("@id", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new UserAccountRecord
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        DisplayName = reader.GetString(2),
                        PasswordHashBase64 = reader.GetString(3),
                        PasswordSaltBase64 = reader.GetString(4),
                        RoleCode = reader.GetString(5),
                        IsActive = reader.GetInt32(6) == 1,
                        LastLoginUtc = null
                    };
                }
            }
        }

        public IList<UserManagementView> LoadUsers()
        {
            var users = new List<UserManagementView>();
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, username, display_name, role_code, is_active, last_login_utc
FROM users
WHERE username NOT LIKE @deleted_prefix
ORDER BY is_active DESC, username ASC;";
                command.Parameters.AddWithValue("@deleted_prefix", DeletedUserPrefix + "%");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserManagementView
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            DisplayName = reader.GetString(2),
                            RoleCode = reader.GetString(3),
                            IsActive = reader.GetInt32(4) == 1,
                            LastLoginUtc = reader.IsDBNull(5) ? (DateTime?)null : DateTime.Parse(reader.GetString(5))
                        });
                    }
                }
            }

            return users;
        }

        public void CreateUser(UserAccountSeed user, bool isActive, DateTime createdUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO users (username, display_name, password_hash, password_salt, role_code, is_active, created_utc, updated_utc)
VALUES (@username, @display_name, @password_hash, @password_salt, @role_code, @is_active, @created_utc, @updated_utc);";
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@display_name", user.DisplayName);
                command.Parameters.AddWithValue("@password_hash", user.PasswordHashBase64);
                command.Parameters.AddWithValue("@password_salt", user.PasswordSaltBase64);
                command.Parameters.AddWithValue("@role_code", user.RoleCode);
                command.Parameters.AddWithValue("@is_active", isActive ? 1 : 0);
                command.Parameters.AddWithValue("@created_utc", createdUtc.ToString("o"));
                command.Parameters.AddWithValue("@updated_utc", createdUtc.ToString("o"));
                command.ExecuteNonQuery();
            }
        }

        public void UpdateUser(UpdateUserRequest request, string passwordHashBase64, string passwordSaltBase64, DateTime updatedUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                if (string.IsNullOrWhiteSpace(passwordHashBase64) || string.IsNullOrWhiteSpace(passwordSaltBase64))
                {
                    command.CommandText = @"UPDATE users
SET username = @username,
    display_name = @display_name,
    role_code = @role_code,
    is_active = @is_active,
    updated_utc = @updated_utc
WHERE id = @id;";
                }
                else
                {
                    command.CommandText = @"UPDATE users
SET username = @username,
    display_name = @display_name,
    password_hash = @password_hash,
    password_salt = @password_salt,
    role_code = @role_code,
    is_active = @is_active,
    updated_utc = @updated_utc
WHERE id = @id;";
                    command.Parameters.AddWithValue("@password_hash", passwordHashBase64);
                    command.Parameters.AddWithValue("@password_salt", passwordSaltBase64);
                }

                command.Parameters.AddWithValue("@id", request.UserId);
                command.Parameters.AddWithValue("@username", request.Username.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("@display_name", request.DisplayName.Trim());
                command.Parameters.AddWithValue("@role_code", request.RoleCode.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("@is_active", request.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@updated_utc", updatedUtc.ToString("o"));
                command.ExecuteNonQuery();
            }
        }

        public void DeleteUser(int userId, DateTime updatedUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE users
SET username = @deleted_username,
    display_name = @deleted_display_name,
    is_active = 0,
    updated_utc = @updated_utc
WHERE id = @id;";
                command.Parameters.AddWithValue("@id", userId);
                command.Parameters.AddWithValue("@deleted_username", DeletedUserPrefix + userId);
                command.Parameters.AddWithValue("@deleted_display_name", "Usuario eliminado");
                command.Parameters.AddWithValue("@updated_utc", updatedUtc.ToString("o"));
                command.ExecuteNonQuery();
            }
        }

        public void UpdateLastLogin(int userId, DateTime lastLoginUtc)
        {
            using (var connection = this.connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "UPDATE users SET last_login_utc = @last_login_utc, updated_utc = @updated_utc WHERE id = @id;",
                    new SQLiteParameter("@last_login_utc", lastLoginUtc.ToString("o")),
                    new SQLiteParameter("@updated_utc", lastLoginUtc.ToString("o")),
                    new SQLiteParameter("@id", userId));

                ExecuteNonQuery(
                    connection,
                    transaction,
                    @"INSERT INTO audit_log (event_type, entity_name, entity_id, user_name, details, created_utc)
VALUES ('auth.login', 'users', @entity_id, NULL, 'Inicio de sesión exitoso.', @created_utc);",
                    new SQLiteParameter("@entity_id", userId.ToString()),
                    new SQLiteParameter("@created_utc", lastLoginUtc.ToString("o")));

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
    }
}
