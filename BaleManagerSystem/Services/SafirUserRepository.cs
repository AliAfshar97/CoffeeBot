using BaleManagerSystem.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class SafirUserRepository
    {
        private readonly IConfiguration _config;

        public SafirUserRepository(IConfiguration config)
        {
            _config = config;
        }

        private string ConnectionString =>
            _config.GetConnectionString("SaleBotManagerDB")!;

        // ================= SAVE USER =================
        public async Task SaveUserAsync(
            string phoneNumber,
            string? username = null)
        {
            using var conn =
                new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            IF NOT EXISTS
            (
                SELECT 1
                FROM BotUsers
                WHERE PhoneNumber=@Phone
            )
            INSERT INTO BotUsers
            (
                PhoneNumber,
                Username
            )
            VALUES
            (
                @Phone,
                @Username
            )
        ", conn);

            cmd.Parameters.AddWithValue("@Phone", phoneNumber);
            cmd.Parameters.AddWithValue("@Username", username ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        // ================= GET USERS =================
        public async Task<List<string>> GetAllPhonesAsync()
        {
            List<string> phones = new();

            using var conn =
                new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT PhoneNumber FROM BotUsers",
                conn);

            using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                phones.Add(reader.GetString(0));
            }

            return phones;
        }

        // ================= LOG RESULT =================
        public async Task SaveLogAsync(
            string message,
            bool success,
            string? error = null,
            long? chatId = null,
            string? phone = null)
        {
            using var conn =
                new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO BroadcastLogs
            (
                PhoneNumber,
                ChatId,
                MessageText,
                IsSuccess,
                ErrorMessage
            )
            VALUES
            (
                @Phone,
                @ChatId,
                @Message,
                @Success,
                @Error
            )
        ", conn);

            cmd.Parameters.AddWithValue("@Phone",
                (object?)phone ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@ChatId",
                (object?)chatId ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@Message", message);

            cmd.Parameters.AddWithValue("@Success", success);

            cmd.Parameters.AddWithValue("@Error",
                (object?)error ?? DBNull.Value);


            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<dynamic>> GetLogsAsync()
        {
            using var connection =
                new SqlConnection(ConnectionString);

            var sql = @"
                        SELECT
                            PhoneNumber,
                            MessageText,
                            IsSuccess,
                            SentAt
                        FROM BroadcastLogs
                        ORDER BY Id DESC";

            var result =
                await connection.QueryAsync(sql);

            return result.ToList<dynamic>();
        }

        public async Task<List<UserModel>> GetUsersAsync()
        {
            using var connection =
                new SqlConnection(ConnectionString);

            var sql = @"
                        SELECT
                            Id,
                            PhoneNumber,
                            Username,
                            FirstSeen
                        FROM BotUsers
                        ORDER BY Id DESC";

            var result =
                await connection.QueryAsync<UserModel>(sql);

            return result.ToList();
        }

        public async Task DeleteUserAsync(int id)
        {
            using var connection =
                new SqlConnection(ConnectionString);

            var sql = @"
                        DELETE FROM BotUsers
                        WHERE Id = @Id";

            await connection.ExecuteAsync(sql, new
            {
                Id = id
            });
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            using var connection =
                new SqlConnection(ConnectionString);

            var sql = @"
                    SELECT COUNT(*)
                    FROM BotUsers";

            return await connection.ExecuteScalarAsync<int>(sql);
        }


        public async Task<int> GetSuccessLogsCountAsync()
        {
            using var connection =
                new SqlConnection(ConnectionString);

            var sql = @"
                    SELECT COUNT(*)
                    FROM BroadcastLogs
                    WHERE IsSuccess = 1";

            return await connection.ExecuteScalarAsync<int>(sql);
        }


        public async Task<int> GetFailedLogsCountAsync()
        {
            using var connection =
                new SqlConnection(ConnectionString);

            var sql = @"
                        SELECT COUNT(*)
                        FROM BroadcastLogs
                        WHERE IsSuccess = 0";

            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }

}
