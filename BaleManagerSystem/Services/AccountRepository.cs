using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaleManagerSystem.Services
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IConfiguration _configuration;

        public AccountRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("SaleBotManagerDB")!;

        public async Task<int> AddDebitAsync(
            long chatId,
            int amount,
            string description,
            int orderId)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO AccountLedger
            (ChatId, TransactionType, Amount, Description, OrderId, CreatedAt)
            VALUES
            (@ChatId, 'Debit', @Amount, @Description, @OrderId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task AddManualDebitAsync(
            long chatId,
            int amount,
            string description,
            string? createdBy)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO AccountLedger
            (ChatId, TransactionType, Amount, Description, CreatedBy, CreatedAt)
            VALUES
            (@ChatId, 'Debit', @Amount, @Description, @CreatedBy, GETDATE())
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddCreditAsync(
            long chatId,
            int amount,
            string description,
            int? receiptId,
            string? createdBy)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO AccountLedger
            (ChatId, TransactionType, Amount, Description, ReceiptId, CreatedBy, CreatedAt)
            VALUES
            (@ChatId, 'Credit', @Amount, @Description, @ReceiptId, @CreatedBy, GETDATE())
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", chatId);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@ReceiptId", (object?)receiptId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateOrderDebitAsync(int orderId, int newAmount, string newDescription)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            UPDATE AccountLedger
            SET Amount = @Amount, Description = @Description
            WHERE OrderId = @OrderId AND TransactionType = 'Debit'";

            await conn.ExecuteAsync(sql, new
            {
                OrderId = orderId,
                Amount = newAmount,
                Description = newDescription
            });
        }

        public async Task DeleteOrderDebitAsync(int orderId)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            DELETE FROM AccountLedger
            WHERE OrderId = @OrderId AND TransactionType = 'Debit'";

            await conn.ExecuteAsync(sql, new { OrderId = orderId });
        }

        public async Task<AccountsPageViewModel> GetAccountsAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            using var conn = new SqlConnection(ConnectionString);

            var toDateExclusive = toDate?.Date.AddDays(1);

            const string usersSql = @"
            SELECT ChatId, DisplayName
            FROM BotUserTransactions
            WHERE DisplayName IS NOT NULL AND DisplayName <> ''";

            var users = (await conn.QueryAsync<(long ChatId, string DisplayName)>(usersSql)).ToList();
            var usersDict = users.ToDictionary(u => u.ChatId, u => u.DisplayName);

            const string orderNamesSql = @"
            SELECT ChatId, MAX(DisplayName) AS DisplayName
            FROM CoffeeOrders
            GROUP BY ChatId";

            var orderNames = (await conn.QueryAsync<(long ChatId, string DisplayName)>(orderNamesSql))
                .ToDictionary(x => x.ChatId, x => x.DisplayName);

            const string ledgerSql = @"
            SELECT
                l.Id,
                l.ChatId,
                l.TransactionType,
                l.Amount,
                l.Description,
                l.OrderId,
                l.ReceiptId,
                l.CreatedBy,
                l.CreatedAt
            FROM AccountLedger l
            WHERE (@FromDate IS NULL OR l.CreatedAt >= @FromDate)
              AND (@ToDate IS NULL OR l.CreatedAt < @ToDate)
            ORDER BY l.CreatedAt DESC";

            var entries = (await conn.QueryAsync<AccountLedgerEntry>(ledgerSql, new
            {
                FromDate = fromDate?.Date,
                ToDate = toDateExclusive
            })).ToList();

            var chatIdsFromLedger = entries.Select(e => e.ChatId).Distinct();
            var allChatIds = users.Select(u => u.ChatId).Union(chatIdsFromLedger).Distinct();

            var balances = new List<AccountBalanceSummary>();

            foreach (var chatId in allChatIds)
            {
                var userTransactions = entries.Where(e => e.ChatId == chatId).ToList();

                var displayName = usersDict.GetValueOrDefault(chatId);
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = orderNames.GetValueOrDefault(chatId);
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = $"کاربر {chatId}";

                var totalDebit = userTransactions
                    .Where(t => t.TransactionType == AccountTransactionTypes.Debit)
                    .Sum(t => t.Amount);

                var totalCredit = userTransactions
                    .Where(t => t.TransactionType == AccountTransactionTypes.Credit)
                    .Sum(t => t.Amount);

                if (totalDebit == 0 && totalCredit == 0 && !users.Any(u => u.ChatId == chatId))
                    continue;

                balances.Add(new AccountBalanceSummary
                {
                    ChatId = chatId,
                    DisplayName = displayName,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    Transactions = userTransactions
                });
            }

            return new AccountsPageViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Balances = balances
                    .OrderByDescending(b => b.Remaining)
                    .ThenBy(b => b.DisplayName)
                    .ToList(),
                GrandTotalDebit = balances.Sum(b => b.TotalDebit),
                GrandTotalCredit = balances.Sum(b => b.TotalCredit),
                GrandTotalRemaining = balances.Sum(b => b.Remaining)
            };
        }

        public async Task<List<PaymentReceipt>> GetReceiptsAsync(string? status = null)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT
                Id, ChatId, DisplayName, TelegramFileId, LocalFilePath,
                UserCaption, Status, CreditAmount, AdminNote, CreatedAt, ProcessedAt
            FROM PaymentReceipts
            WHERE (@Status IS NULL OR Status = @Status)
            ORDER BY CreatedAt DESC";

            var result = await conn.QueryAsync<PaymentReceipt>(sql, new { Status = status });

            return result.ToList();
        }

        public async Task<PaymentReceipt?> GetReceiptByIdAsync(int id)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT
                Id, ChatId, DisplayName, TelegramFileId, LocalFilePath,
                ImageContent, ImageContentType,
                UserCaption, Status, CreditAmount, AdminNote, CreatedAt, ProcessedAt
            FROM PaymentReceipts
            WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<PaymentReceipt>(sql, new { Id = id });
        }

        public async Task<int> CreateReceiptAsync(PaymentReceipt receipt)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO PaymentReceipts
            (ChatId, DisplayName, TelegramFileId, UserCaption, Status, CreatedAt)
            VALUES
            (@ChatId, @DisplayName, @TelegramFileId, @UserCaption, 'Pending', GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", conn);

            cmd.Parameters.AddWithValue("@ChatId", receipt.ChatId);
            cmd.Parameters.AddWithValue("@DisplayName", receipt.DisplayName);
            cmd.Parameters.AddWithValue("@TelegramFileId", receipt.TelegramFileId);
            cmd.Parameters.AddWithValue("@UserCaption", (object?)receipt.UserCaption ?? DBNull.Value);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task UpdateReceiptFilePathAsync(int receiptId, string localFilePath)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            UPDATE PaymentReceipts
            SET LocalFilePath = @LocalFilePath
            WHERE Id = @Id";

            await conn.ExecuteAsync(sql, new { Id = receiptId, LocalFilePath = localFilePath });
        }

        public async Task UpdateReceiptImageAsync(int receiptId, byte[] content, string contentType)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            UPDATE PaymentReceipts
            SET ImageContent = @ImageContent,
                ImageContentType = @ImageContentType
            WHERE Id = @Id";

            await conn.ExecuteAsync(sql, new
            {
                Id = receiptId,
                ImageContent = content,
                ImageContentType = contentType
            });
        }

        public async Task ApproveReceiptAsync(
            int receiptId,
            int creditAmount,
            string? adminNote,
            string createdBy)
        {
            using var conn = new SqlConnection(ConnectionString);

            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                var receipt = await conn.QueryFirstOrDefaultAsync<PaymentReceipt>(@"
                SELECT Id, ChatId, DisplayName, Status
                FROM PaymentReceipts
                WHERE Id = @Id",
                    new { Id = receiptId },
                    transaction);

                if (receipt == null || receipt.Status != ReceiptStatuses.Pending)
                    throw new InvalidOperationException("Receipt is not pending.");

                await conn.ExecuteAsync(@"
                UPDATE PaymentReceipts
                SET Status = 'Approved',
                    CreditAmount = @CreditAmount,
                    AdminNote = @AdminNote,
                    ProcessedAt = GETDATE()
                WHERE Id = @Id",
                    new
                    {
                        Id = receiptId,
                        CreditAmount = creditAmount,
                        AdminNote = adminNote
                    },
                    transaction);

                await conn.ExecuteAsync(@"
                INSERT INTO AccountLedger
                (ChatId, TransactionType, Amount, Description, ReceiptId, CreatedBy, CreatedAt)
                VALUES
                (@ChatId, 'Credit', @Amount, @Description, @ReceiptId, @CreatedBy, GETDATE())",
                    new
                    {
                        ChatId = receipt.ChatId,
                        Amount = creditAmount,
                        Description = $"تایید رسید پرداخت #{receiptId}",
                        ReceiptId = receiptId,
                        CreatedBy = createdBy
                    },
                    transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task RejectReceiptAsync(int receiptId, string? adminNote)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            UPDATE PaymentReceipts
            SET Status = 'Rejected',
                AdminNote = @AdminNote,
                ProcessedAt = GETDATE()
            WHERE Id = @Id AND Status = 'Pending'";

            await conn.ExecuteAsync(sql, new { Id = receiptId, AdminNote = adminNote });
        }

        public async Task<int> GetLifetimeRemainingAsync(long chatId)
        {
            using var conn = new SqlConnection(ConnectionString);

            const string sql = @"
            SELECT
                ISNULL(SUM(CASE WHEN TransactionType = 'Debit' THEN Amount ELSE 0 END), 0)
              - ISNULL(SUM(CASE WHEN TransactionType = 'Credit' THEN Amount ELSE 0 END), 0)
            FROM AccountLedger
            WHERE ChatId = @ChatId";

            return await conn.ExecuteScalarAsync<int>(sql, new { ChatId = chatId });
        }
    }
}
