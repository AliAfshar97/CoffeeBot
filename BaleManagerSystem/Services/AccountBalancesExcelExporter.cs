using BaleManagerSystem.Localization;
using BaleManagerSystem.Models.ViewModels;
using ClosedXML.Excel;

namespace BaleManagerSystem.Services
{
    public class AccountBalancesExcelExporter
    {
        public byte[] Export(AccountsPageViewModel report)
        {
            using var workbook = new XLWorkbook();

            WriteSummarySheet(workbook, report);
            WriteLedgerSheet(workbook, report);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        private static void WriteSummarySheet(
            XLWorkbook workbook,
            AccountsPageViewModel report)
        {
            var sheet = workbook.Worksheets.Add("خلاصه");

            sheet.Cell(1, 1).Value = "خلاصه مانده حساب‌ها";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            sheet.Cell(2, 1).Value = "از تاریخ:";
            sheet.Cell(2, 2).Value = report.FromDate?.ToString("yyyy-MM-dd") ?? PersianLabels.AllDates;

            sheet.Cell(3, 1).Value = "تا تاریخ:";
            sheet.Cell(3, 2).Value = report.ToDate?.ToString("yyyy-MM-dd") ?? PersianLabels.AllDates;

            sheet.Cell(4, 1).Value = "جمع بدهکار (تومان):";
            sheet.Cell(4, 2).Value = report.GrandTotalDebit;

            sheet.Cell(5, 1).Value = "جمع بستانکار (تومان):";
            sheet.Cell(5, 2).Value = report.GrandTotalCredit;

            sheet.Cell(6, 1).Value = "جمع مانده (تومان):";
            sheet.Cell(6, 2).Value = report.GrandTotalRemaining;

            var headerRow = 8;
            sheet.Cell(headerRow, 1).Value = "نام";
            sheet.Cell(headerRow, 2).Value = "شناسه چت";
            sheet.Cell(headerRow, 3).Value = "بدهکار (تومان)";
            sheet.Cell(headerRow, 4).Value = "بستانکار (تومان)";
            sheet.Cell(headerRow, 5).Value = "مانده (تومان)";

            var headerRange = sheet.Range(headerRow, 1, headerRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = headerRow + 1;

            foreach (var person in report.Balances)
            {
                sheet.Cell(row, 1).Value = person.DisplayName;
                sheet.Cell(row, 2).Value = person.ChatId;
                sheet.Cell(row, 3).Value = person.TotalDebit;
                sheet.Cell(row, 4).Value = person.TotalCredit;
                sheet.Cell(row, 5).Value = person.Remaining;
                row++;
            }

            sheet.Columns().AdjustToContents();
        }

        private static void WriteLedgerSheet(
            XLWorkbook workbook,
            AccountsPageViewModel report)
        {
            var sheet = workbook.Worksheets.Add("تراکنش‌ها");

            sheet.Cell(1, 1).Value = "نام";
            sheet.Cell(1, 2).Value = "شناسه چت";
            sheet.Cell(1, 3).Value = "نوع";
            sheet.Cell(1, 4).Value = "مبلغ (تومان)";
            sheet.Cell(1, 5).Value = "توضیحات";
            sheet.Cell(1, 6).Value = "ثبت توسط";
            sheet.Cell(1, 7).Value = "تاریخ";

            var headerRange = sheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;

            foreach (var person in report.Balances)
            {
                foreach (var entry in person.Transactions)
                {
                    sheet.Cell(row, 1).Value = person.DisplayName;
                    sheet.Cell(row, 2).Value = person.ChatId;
                    sheet.Cell(row, 3).Value = PersianLabels.TransactionType(entry.TransactionType);
                    sheet.Cell(row, 4).Value = entry.Amount;
                    sheet.Cell(row, 5).Value = entry.Description;
                    sheet.Cell(row, 6).Value = entry.CreatedBy;
                    sheet.Cell(row, 7).Value = entry.CreatedAt;
                    sheet.Cell(row, 7).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                    row++;
                }
            }

            sheet.Columns().AdjustToContents();
        }
    }
}
