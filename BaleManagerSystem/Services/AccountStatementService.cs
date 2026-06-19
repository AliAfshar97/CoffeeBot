using BaleManagerSystem.Localization;
using BaleManagerSystem.Models;

namespace BaleManagerSystem.Services
{
    public class AccountStatementService
    {
        private readonly IAccountRepository _accounts;
        private readonly IOrderRepository _orders;

        public AccountStatementService(
            IAccountRepository accounts,
            IOrderRepository orders)
        {
            _accounts = accounts;
            _orders = orders;
        }

        public async Task<string> BuildStatementAsync(
            long chatId,
            DateTime fromDate,
            DateTime toDate,
            DateTime paymentDueDate)
        {
            var remaining = await _accounts.GetLifetimeRemainingAsync(chatId);
            var periodOrders = await _orders.GetOrdersByChatAsync(chatId, fromDate, toDate);
            var periodTotal = periodOrders.Sum(o => o.PriceInToman);

            var lines = new List<string>
            {
                "",
                "━━━━━━━━━━━━━━",
                "📋 صورت‌حساب",
                "",
                $"مانده کل حساب: {remaining:N0} تومان",
                "",
                $"سفارش‌ها ({fromDate:yyyy/MM/dd} تا {toDate:yyyy/MM/dd}):"
            };

            if (periodOrders.Count == 0)
            {
                lines.Add("— سفارشی در این بازه ثبت نشده —");
            }
            else
            {
                var index = 1;

                foreach (var order in periodOrders.OrderBy(o => o.CreatedAt))
                {
                    lines.Add(
                        $"{index}. {FormatOrderLine(order)} ({order.CreatedAt:yyyy/MM/dd})");
                    index++;
                }

                lines.Add("");
                lines.Add($"جمع این بازه: {periodTotal:N0} تومان");
            }

            lines.Add("");
            lines.Add($"مهلت پرداخت: {paymentDueDate:yyyy/MM/dd}");

            return string.Join("\n", lines);
        }

        private static string FormatOrderLine(CoffeeOrder order)
        {
            var drink = PersianLabels.Drink(order.DrinkType);

            if (order.DrinkType == "Chocolate")
                return $"{drink} — {order.PriceInToman:N0} تومان";

            if (order.DrinkType == "Milk")
            {
                return $"{drink} — {order.ShotCount} لیوان — {order.PriceInToman:N0} تومان";
            }

            var chocolate = order.WithChocolate ? " با شکلات" : string.Empty;
            return $"{drink} — {order.ShotCount} شات{chocolate} — {order.PriceInToman:N0} تومان";
        }
    }
}
