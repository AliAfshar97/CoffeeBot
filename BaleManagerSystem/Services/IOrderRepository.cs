using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;

namespace BaleManagerSystem.Services
{
    public interface IOrderRepository
    {
        Task<int> SaveOrderAsync(CoffeeOrder order);

        Task<List<CoffeeOrder>> GetOrdersAsync();

        Task<CoffeeOrder?> GetOrderByIdAsync(int id);

        Task UpdateOrderAsync(CoffeeOrder order);

        Task DeleteOrderAsync(int id);

        Task<int> GetOrderCountAsync();

        Task<PaymentReportViewModel> GetPaymentReportAsync(
            DateTime? fromDate,
            DateTime? toDate);

        Task<List<CoffeeOrder>> GetOrdersByChatAsync(
            long chatId,
            DateTime? fromDate,
            DateTime? toDate);
    }
}
