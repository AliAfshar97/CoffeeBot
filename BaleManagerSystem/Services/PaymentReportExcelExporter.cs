using BaleManagerSystem.Localization;
using BaleManagerSystem.Models.ViewModels;
using ClosedXML.Excel;

namespace BaleManagerSystem.Services
{
    public class PaymentReportExcelExporter
    {
        public byte[] Export(PaymentReportViewModel report)
        {
            using var workbook = new XLWorkbook();

            WriteSummarySheet(workbook, report);
            WriteOrdersSheet(workbook, report);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        private static void WriteSummarySheet(
            XLWorkbook workbook,
            PaymentReportViewModel report)
        {
            var sheet = workbook.Worksheets.Add("خلاصه");

            sheet.Cell(1, 1).Value = "خلاصه گزارش پرداخت";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            sheet.Cell(2, 1).Value = "از تاریخ:";
            sheet.Cell(2, 2).Value = report.FromDate?.ToString("yyyy-MM-dd") ?? PersianLabels.AllDates;

            sheet.Cell(3, 1).Value = "تا تاریخ:";
            sheet.Cell(3, 2).Value = report.ToDate?.ToString("yyyy-MM-dd") ?? PersianLabels.AllDates;

            sheet.Cell(4, 1).Value = "تعداد سفارش:";
            sheet.Cell(4, 2).Value = report.TotalOrders;

            sheet.Cell(5, 1).Value = "جمع کل (تومان):";
            sheet.Cell(5, 2).Value = report.GrandTotalToman;

            var headerRow = 7;
            sheet.Cell(headerRow, 1).Value = "نام";
            sheet.Cell(headerRow, 2).Value = "شناسه چت";
            sheet.Cell(headerRow, 3).Value = "تعداد سفارش";
            sheet.Cell(headerRow, 4).Value = "مبلغ قابل پرداخت (تومان)";

            var headerRange = sheet.Range(headerRow, 1, headerRow, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = headerRow + 1;

            foreach (var person in report.Summaries)
            {
                sheet.Cell(row, 1).Value = person.DisplayName;
                sheet.Cell(row, 2).Value = person.ChatId;
                sheet.Cell(row, 3).Value = person.OrderCount;
                sheet.Cell(row, 4).Value = person.TotalToman;
                row++;
            }

            sheet.Columns().AdjustToContents();
        }

        private static void WriteOrdersSheet(
            XLWorkbook workbook,
            PaymentReportViewModel report)
        {
            var sheet = workbook.Worksheets.Add("سفارش‌ها");

            sheet.Cell(1, 1).Value = "نام";
            sheet.Cell(1, 2).Value = "شناسه چت";
            sheet.Cell(1, 3).Value = "نوشیدنی";
            sheet.Cell(1, 4).Value = "شات";
            sheet.Cell(1, 5).Value = "مبلغ (تومان)";
            sheet.Cell(1, 6).Value = "تاریخ";

            var headerRange = sheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;

            foreach (var person in report.Summaries)
            {
                foreach (var order in person.Orders)
                {
                    sheet.Cell(row, 1).Value = person.DisplayName;
                    sheet.Cell(row, 2).Value = person.ChatId;
                    sheet.Cell(row, 3).Value = order.DrinkNamePersian ?? PersianLabels.Drink(order.DrinkType);
                    sheet.Cell(row, 4).Value = order.ShotCount;
                    sheet.Cell(row, 5).Value = order.PriceInToman;
                    sheet.Cell(row, 6).Value = order.CreatedAt;
                    sheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                    row++;
                }
            }

            sheet.Columns().AdjustToContents();
        }
    }
}
