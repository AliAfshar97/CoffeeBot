using BaleManagerSystem.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class MenuRepository : IMenuRepository
    {
        private readonly IConfiguration _configuration;

        public MenuRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("SaleBotManagerDB")!;

        private const string SelectColumns = @"
            Id, ItemKey, NamePersian, SupportsShots, Unit, DisplayOrder, IsActive";

        public async Task<List<MenuItem>> GetAllAsync()
        {
            using var conn = new SqlConnection(ConnectionString);

            var sql = $@"
            SELECT {SelectColumns}
            FROM MenuItems
            ORDER BY DisplayOrder, NamePersian";

            var result = await conn.QueryAsync<MenuItem>(sql);

            return result.ToList();
        }

        public async Task<List<MenuItem>> GetActiveOrderedAsync()
        {
            using var conn = new SqlConnection(ConnectionString);

            var sql = $@"
            SELECT {SelectColumns}
            FROM MenuItems
            WHERE IsActive = 1
            ORDER BY DisplayOrder, NamePersian";

            var result = await conn.QueryAsync<MenuItem>(sql);

            return result.ToList();
        }

        public async Task<MenuItem?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(ConnectionString);

            var sql = $@"
            SELECT {SelectColumns}
            FROM MenuItems
            WHERE Id = @Id";

            return await conn.QuerySingleOrDefaultAsync<MenuItem>(sql, new { Id = id });
        }

        public async Task<MenuItem?> GetByKeyAsync(string itemKey)
        {
            using var conn = new SqlConnection(ConnectionString);

            var sql = $@"
            SELECT {SelectColumns}
            FROM MenuItems
            WHERE ItemKey = @ItemKey";

            return await conn.QuerySingleOrDefaultAsync<MenuItem>(sql, new { ItemKey = itemKey });
        }

        public async Task<bool> KeyExistsAsync(string itemKey, int? excludeId = null)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT COUNT(1)
            FROM MenuItems
            WHERE ItemKey = @ItemKey
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";

            var count = await conn.ExecuteScalarAsync<int>(sql, new
            {
                ItemKey = itemKey,
                ExcludeId = excludeId
            });

            return count > 0;
        }

        public async Task<int> CreateAsync(MenuItem item)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            INSERT INTO MenuItems
                (ItemKey, NamePersian, SupportsShots, Unit, DisplayOrder, IsActive)
            VALUES
                (@ItemKey, @NamePersian, @SupportsShots, @Unit, @DisplayOrder, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(sql, item);
        }

        public async Task UpdateAsync(MenuItem item)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            UPDATE MenuItems
            SET NamePersian   = @NamePersian,
                SupportsShots = @SupportsShots,
                Unit          = @Unit,
                DisplayOrder  = @DisplayOrder,
                IsActive      = @IsActive
            WHERE Id = @Id";

            await conn.ExecuteAsync(sql, item);
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            UPDATE MenuItems
            SET IsActive = @IsActive
            WHERE Id = @Id";

            await conn.ExecuteAsync(sql, new { Id = id, IsActive = isActive });
        }
    }
}
