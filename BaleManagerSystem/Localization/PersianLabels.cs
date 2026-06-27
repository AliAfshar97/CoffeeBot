namespace BaleManagerSystem.Localization
{
    public static class PersianLabels
    {
        public static string Drink(string? name) => name switch
        {
            "Espresso" => "اسپرسو",
            "Latte" => "لاته",
            "Cappuccino" => "کاپوچینو",
            "Milk" => "شیر",
            "Chocolate" => "شکلات",
            _ => name ?? string.Empty
        };

        public static string YesNo(bool value) => value ? "بله" : "خیر";

        public static string Subscription(bool isSubscriber) =>
            isSubscriber ? "مشترک" : "غیرمشترک";

        public static string Visibility(Models.MenuVisibility visibility) => visibility switch
        {
            Models.MenuVisibility.SubscribersOnly => "فقط مشترکین",
            Models.MenuVisibility.NonSubscribersOnly => "فقط غیرمشترکین",
            _ => "همه"
        };

        public static string TransactionType(string type) => type switch
        {
            "Debit" => "بدهکار",
            "Credit" => "بستانکار",
            _ => type
        };

        public static string ReceiptStatus(string status) => status switch
        {
            "Pending" => "در انتظار",
            "Approved" => "تایید شده",
            "Rejected" => "رد شده",
            _ => status
        };

        public static string AllDates => "همه";
    }
}
