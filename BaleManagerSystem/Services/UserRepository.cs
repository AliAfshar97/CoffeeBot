using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class UserRepository
    {
        private readonly IConfiguration _config;

        public UserRepository(IConfiguration config)
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
            string phone,
            string message,
            bool success,
            string? error = null)
        {
            using var conn =
                new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO BroadcastLogs
            (
                PhoneNumber,
                MessageText,
                IsSuccess,
                ErrorMessage
            )
            VALUES
            (
                @Phone,
                @Message,
                @Success,
                @Error
            )
        ", conn);

            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@Message", message);
            cmd.Parameters.AddWithValue("@Success", success);
            cmd.Parameters.AddWithValue("@Error", error ?? "");

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
