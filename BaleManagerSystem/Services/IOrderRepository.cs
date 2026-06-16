using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;

namespace BaleManagerSystem.Services
{
    public interface IOrderRepository
    {
        Task SaveOrderAsync(CoffeeOrder order);

        Task<List<CoffeeOrder>> GetOrdersAsync();

        Task<int> GetOrderCountAsync();

        Task<PaymentReportViewModel> GetPaymentReportAsync(
            DateTime? fromDate,
            DateTime? toDate);
    }
}
