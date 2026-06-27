using BaleManagerSystem.Models;

namespace BaleManagerSystem.Services
{
    public interface ICoffeePriceRepository
    {
        Task<List<CoffeePrice>> GetByDrinkAsync(string drinkType);

        Task<int?> GetPriceAsync(string drinkType, byte shotCount, bool withChocolate);

        // Ensure CoffeePrices has exactly the combinations a menu item needs
        // (creates missing rows at price 0, removes combinations no longer valid).
        Task SyncItemPricesAsync(string drinkType, bool supportsShots);

        Task UpsertPriceAsync(string drinkType, byte shotCount, bool withChocolate, int priceInToman);
    }
}
