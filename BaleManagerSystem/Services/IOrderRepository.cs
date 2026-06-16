using BaleManagerSystem.Models;

namespace BaleManagerSystem.Services
{
    public interface IOrderRepository
    {
        Task SaveOrderAsync(CoffeeOrder order);

        Task<List<CoffeeOrder>> GetOrdersAsync();

        Task<int> GetOrderCountAsync();
    }
}
