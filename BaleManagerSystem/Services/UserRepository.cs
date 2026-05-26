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

        public async Task<List<long>> GetAllChatIds()
        {
            List<long> ids = new();

            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT ChatId FROM BotUserTransactions",
                conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt64(0));
            }

            return ids;
        }
    }
}
