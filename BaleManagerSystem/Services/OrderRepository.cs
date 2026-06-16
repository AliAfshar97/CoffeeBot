using BaleManagerSystem.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IConfiguration _configuration;

        public OrderRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SaveOrderAsync(CoffeeOrder order)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO CoffeeOrders
            (ChatId, DisplayName, DrinkType, ShotCount, WithChocolate, CreatedAt)
            VALUES
            (@ChatId, @DisplayName, @DrinkType, @ShotCount, @WithChocolate, GETDATE())
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", order.ChatId);
            cmd.Parameters.AddWithValue("@DisplayName", order.DisplayName);
            cmd.Parameters.AddWithValue("@DrinkType", order.DrinkType);
            cmd.Parameters.AddWithValue("@ShotCount", order.ShotCount);
            cmd.Parameters.AddWithValue("@WithChocolate", order.WithChocolate);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<CoffeeOrder>> GetOrdersAsync()
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            const string sql = @"
            SELECT
                Id,
                ChatId,
                DisplayName,
                DrinkType,
                ShotCount,
                WithChocolate,
                CreatedAt
            FROM CoffeeOrders
            ORDER BY CreatedAt DESC";

            var result = await conn.QueryAsync<CoffeeOrder>(sql);

            return result.ToList();
        }

        public async Task<int> GetOrderCountAsync()
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("SaleBotManagerDB"));

            const string sql = "SELECT COUNT(*) FROM CoffeeOrders";

            return await conn.ExecuteScalarAsync<int>(sql);
        }
    }
}
