using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _configuration;

        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SaveUser(long chatId, string username)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            IF NOT EXISTS (
                SELECT 1 FROM BotUserTransactions WHERE ChatId=@ChatId
            )
            INSERT INTO BotUserTransactions
            (ChatId, Username, FirstSeen)
            VALUES
            (@ChatId, @Username, GETDATE())
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);
            cmd.Parameters.AddWithValue("@Username", username ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> GetUserCountAsync()
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM BotUserTransactions",
                conn);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task<ChatUser?> GetUserByChatIdAsync(long chatId)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT ChatId, Username, DisplayName, IsSubscriber, FirstSeen
            FROM BotUserTransactions
            WHERE ChatId = @ChatId
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new ChatUser
            {
                ChatId = reader.GetInt64(reader.GetOrdinal("ChatId")),
                Username = reader.IsDBNull(reader.GetOrdinal("Username"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Username")),
                DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("DisplayName")),
                IsSubscriber = reader.GetBoolean(reader.GetOrdinal("IsSubscriber")),
                FirstSeen = reader.GetDateTime(reader.GetOrdinal("FirstSeen"))
            };
        }

        public async Task<List<ChatUser>> GetAllUsersAsync()
        {
            var users = new List<ChatUser>();

            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT ChatId, Username, DisplayName, IsSubscriber, FirstSeen
            FROM BotUserTransactions
            ORDER BY IsSubscriber DESC, DisplayName, FirstSeen DESC
            ", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new ChatUser
                {
                    ChatId = reader.GetInt64(reader.GetOrdinal("ChatId")),
                    Username = reader.IsDBNull(reader.GetOrdinal("Username"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("Username")),
                    DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("DisplayName")),
                    IsSubscriber = reader.GetBoolean(reader.GetOrdinal("IsSubscriber")),
                    FirstSeen = reader.GetDateTime(reader.GetOrdinal("FirstSeen"))
                });
            }

            return users;
        }

        public async Task SetSubscriptionAsync(long chatId, bool isSubscriber)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            UPDATE BotUserTransactions
            SET IsSubscriber = @IsSubscriber
            WHERE ChatId = @ChatId
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);
            cmd.Parameters.AddWithValue("@IsSubscriber", isSubscriber);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateDisplayNameAsync(long chatId, string displayName)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            UPDATE BotUserTransactions
            SET DisplayName = @DisplayName
            WHERE ChatId = @ChatId
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);
            cmd.Parameters.AddWithValue("@DisplayName", displayName);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<UserChatIdViewModel>> GetAllChatIds()
        {
            List<UserChatIdViewModel> users = new();

            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(
                @"SELECT ChatId,
                 Username,
                 DisplayName,
                 FirstSeen
                 FROM BotUserTransactions",
                conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var user = new UserChatIdViewModel
                {
                    ChatId = reader.GetInt64(
                        reader.GetOrdinal("ChatId")),

                    Username = reader.IsDBNull(
                        reader.GetOrdinal("Username"))
                            ? string.Empty
                            : reader.GetString(
                                reader.GetOrdinal("Username")),

                    DisplayName = reader.IsDBNull(
                        reader.GetOrdinal("DisplayName"))
                            ? string.Empty
                            : reader.GetString(
                                reader.GetOrdinal("DisplayName")),

                    FirstSeen = reader.IsDBNull(
                        reader.GetOrdinal("FirstSeen"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(
                                reader.GetOrdinal("FirstSeen"))
                };

                users.Add(user);
            }

            return users;
        }
    }
}
