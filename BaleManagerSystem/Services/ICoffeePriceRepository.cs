using BaleManagerSystem.Models;

namespace BaleManagerSystem.Services
{
    public interface ICoffeePriceRepository
    {
        Task<List<CoffeePrice>> GetAllAsync();

        Task<int?> GetPriceAsync(string drinkType, byte shotCount, bool withChocolate);

        Task UpdatePricesAsync(IEnumerable<CoffeePrice> prices);
    }
}
