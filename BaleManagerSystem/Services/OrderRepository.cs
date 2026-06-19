using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;
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

        private string ConnectionString =>
            _configuration.GetConnectionString("SaleBotManagerDB")!;

        public async Task<int> SaveOrderAsync(CoffeeOrder order)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO CoffeeOrders
            (ChatId, DisplayName, DrinkType, ShotCount, WithChocolate, PriceInToman, CreatedAt)
            VALUES
            (@ChatId, @DisplayName, @DrinkType, @ShotCount, @WithChocolate, @PriceInToman, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", order.ChatId);
            cmd.Parameters.AddWithValue("@DisplayName", order.DisplayName);
            cmd.Parameters.AddWithValue("@DrinkType", order.DrinkType);
            cmd.Parameters.AddWithValue("@ShotCount", order.ShotCount);
            cmd.Parameters.AddWithValue("@WithChocolate", order.WithChocolate);
            cmd.Parameters.AddWithValue("@PriceInToman", order.PriceInToman);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task<List<CoffeeOrder>> GetOrdersAsync()
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT
                Id,
                ChatId,
                DisplayName,
                DrinkType,
                ShotCount,
                WithChocolate,
                PriceInToman,
                CreatedAt
            FROM CoffeeOrders
            ORDER BY CreatedAt DESC";

            var result = await conn.QueryAsync<CoffeeOrder>(sql);

            return result.ToList();
        }

        public async Task<int> GetOrderCountAsync()
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = "SELECT COUNT(*) FROM CoffeeOrders";

            return await conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<PaymentReportViewModel> GetPaymentReportAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            using var conn = new SqlConnection(ConnectionString);

            var toDateExclusive = toDate?.Date.AddDays(1);

            const string sql = @"
            SELECT
                o.Id,
                o.ChatId,
                o.DisplayName,
                o.DrinkType,
                o.ShotCount,
                o.WithChocolate,
                COALESCE(
                    NULLIF(o.PriceInToman, 0),
                    p.PriceInToman,
                    0) AS PriceInToman,
                o.CreatedAt
            FROM CoffeeOrders o
            LEFT JOIN CoffeePrices p
                ON p.DrinkType = o.DrinkType
               AND p.ShotCount = o.ShotCount
               AND p.WithChocolate = o.WithChocolate
            WHERE (@FromDate IS NULL OR o.CreatedAt >= @FromDate)
              AND (@ToDate IS NULL OR o.CreatedAt < @ToDate)
            ORDER BY o.DisplayName, o.CreatedAt DESC";

            var orders = (await conn.QueryAsync<CoffeeOrder>(sql, new
            {
                FromDate = fromDate?.Date,
                ToDate = toDateExclusive
            })).ToList();

            var summaries = orders
                .GroupBy(o => new { o.ChatId, o.DisplayName })
                .Select(g => new PersonPaymentSummary
                {
                    ChatId = g.Key.ChatId,
                    DisplayName = g.Key.DisplayName,
                    OrderCount = g.Count(),
                    TotalToman = g.Sum(o => o.PriceInToman),
                    Orders = g.ToList()
                })
                .OrderByDescending(s => s.TotalToman)
                .ThenBy(s => s.DisplayName)
                .ToList();

            return new PaymentReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Summaries = summaries,
                GrandTotalToman = summaries.Sum(s => s.TotalToman),
                TotalOrders = orders.Count
            };
        }

        public async Task<List<CoffeeOrder>> GetOrdersByChatAsync(
            long chatId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            using var conn = new SqlConnection(ConnectionString);

            var toDateExclusive = toDate?.Date.AddDays(1);

            const string sql = @"
            SELECT
                o.Id,
                o.ChatId,
                o.DisplayName,
                o.DrinkType,
                o.ShotCount,
                o.WithChocolate,
                COALESCE(
                    NULLIF(o.PriceInToman, 0),
                    p.PriceInToman,
                    0) AS PriceInToman,
                o.CreatedAt
            FROM CoffeeOrders o
            LEFT JOIN CoffeePrices p
                ON p.DrinkType = o.DrinkType
               AND p.ShotCount = o.ShotCount
               AND p.WithChocolate = o.WithChocolate
            WHERE o.ChatId = @ChatId
              AND (@FromDate IS NULL OR o.CreatedAt >= @FromDate)
              AND (@ToDate IS NULL OR o.CreatedAt < @ToDate)
            ORDER BY o.CreatedAt";

            var result = await conn.QueryAsync<CoffeeOrder>(sql, new
            {
                ChatId = chatId,
                FromDate = fromDate?.Date,
                ToDate = toDateExclusive
            });

            return result.ToList();
        }
    }
}
