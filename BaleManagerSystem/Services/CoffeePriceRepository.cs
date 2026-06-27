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

        public async Task<List<CoffeePrice>> GetByDrinkAsync(string drinkType)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT Id, DrinkType, ShotCount, WithChocolate, PriceInToman
            FROM CoffeePrices
            WHERE DrinkType = @DrinkType
            ORDER BY ShotCount, WithChocolate";

            var result = await conn.QueryAsync<CoffeePrice>(sql, new { DrinkType = drinkType });

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

        public async Task SyncItemPricesAsync(
            string drinkType,
            bool supportsShots)
        {
            var shotCounts = supportsShots
                ? new byte[] { 1, 2 }
                : new byte[] { 1 };

            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            // Insert any missing valid combination at price 0 (chocolate add-on removed).
            foreach (var shot in shotCounts)
            {
                const string insertSql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM CoffeePrices
                    WHERE DrinkType = @DrinkType
                      AND ShotCount = @ShotCount
                      AND WithChocolate = 0)
                INSERT INTO CoffeePrices (DrinkType, ShotCount, WithChocolate, PriceInToman)
                VALUES (@DrinkType, @ShotCount, 0, 0);";

                await conn.ExecuteAsync(insertSql, new
                {
                    DrinkType = drinkType,
                    ShotCount = shot
                });
            }

            // Remove combinations that are no longer valid for this item
            // (extra shot counts, or any leftover chocolate rows).
            const string deleteSql = @"
            DELETE FROM CoffeePrices
            WHERE DrinkType = @DrinkType
              AND (ShotCount NOT IN @ShotCounts OR WithChocolate = 1);";

            await conn.ExecuteAsync(deleteSql, new
            {
                DrinkType = drinkType,
                ShotCounts = shotCounts.Select(s => (int)s).ToArray()
            });
        }

        public async Task UpsertPriceAsync(
            string drinkType,
            byte shotCount,
            bool withChocolate,
            int priceInToman)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            IF EXISTS (
                SELECT 1 FROM CoffeePrices
                WHERE DrinkType = @DrinkType
                  AND ShotCount = @ShotCount
                  AND WithChocolate = @WithChocolate)
                UPDATE CoffeePrices
                SET PriceInToman = @PriceInToman
                WHERE DrinkType = @DrinkType
                  AND ShotCount = @ShotCount
                  AND WithChocolate = @WithChocolate;
            ELSE
                INSERT INTO CoffeePrices (DrinkType, ShotCount, WithChocolate, PriceInToman)
                VALUES (@DrinkType, @ShotCount, @WithChocolate, @PriceInToman);";

            await conn.ExecuteAsync(sql, new
            {
                DrinkType = drinkType,
                ShotCount = shotCount,
                WithChocolate = withChocolate,
                PriceInToman = priceInToman
            });
        }
    }
}
