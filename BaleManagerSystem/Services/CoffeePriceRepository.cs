using BaleManagerSystem.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class CoffeePriceRepository : ICoffeePriceRepository
    {
        private readonly IConfiguration _configuration;

        public CoffeePriceRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("SaleBotManagerDB")!;

        public async Task<List<CoffeePrice>> GetAllAsync()
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT Id, DrinkType, ShotCount, WithChocolate, PriceInToman
            FROM CoffeePrices
            ORDER BY DrinkType, ShotCount, WithChocolate";

            var result = await conn.QueryAsync<CoffeePrice>(sql);

            return result.ToList();
        }

        public async Task<int?> GetPriceAsync(
            string drinkType,
            byte shotCount,
            bool withChocolate)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT PriceInToman
            FROM CoffeePrices
            WHERE DrinkType = @DrinkType
              AND ShotCount = @ShotCount
              AND WithChocolate = @WithChocolate";

            return await conn.ExecuteScalarAsync<int?>(sql, new
            {
                DrinkType = drinkType,
                ShotCount = shotCount,
                WithChocolate = withChocolate
            });
        }

        public async Task UpdatePricesAsync(IEnumerable<CoffeePrice> prices)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            foreach (var price in prices)
            {
                var cmd = new SqlCommand(@"
                UPDATE CoffeePrices
                SET PriceInToman = @PriceInToman
                WHERE Id = @Id
                ", conn);

                cmd.Parameters.AddWithValue("@Id", price.Id);
                cmd.Parameters.AddWithValue("@PriceInToman", price.PriceInToman);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
