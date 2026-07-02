using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;

namespace BaleManagerSystem.Services
{
    public interface IAccountRepository
    {
        Task<int> AddDebitAsync(
            long chatId,
            int amount,
            string description,
            int orderId);

        Task AddManualDebitAsync(
            long chatId,
            int amount,
            string description,
            string? createdBy);

        Task AddCreditAsync(
            long chatId,
            int amount,
            string description,
            int? receiptId,
            string? createdBy);

        Task UpdateOrderDebitAsync(int orderId, int newAmount, string newDescription);

        Task DeleteOrderDebitAsync(int orderId);

        Task<AccountsPageViewModel> GetAccountsAsync(
            DateTime? fromDate,
            DateTime? toDate);

        Task<List<PaymentReceipt>> GetReceiptsAsync(string? status = null);

        Task<PaymentReceipt?> GetReceiptByIdAsync(int id);

        Task<int> CreateReceiptAsync(PaymentReceipt receipt);

        Task UpdateReceiptFilePathAsync(int receiptId, string localFilePath);

        Task UpdateReceiptImageAsync(int receiptId, byte[] content, string contentType);

        Task ApproveReceiptAsync(int receiptId, int creditAmount, string? adminNote, string createdBy);

        Task RejectReceiptAsync(int receiptId, string? adminNote);

        Task<int> GetLifetimeRemainingAsync(long chatId);
    }
}
