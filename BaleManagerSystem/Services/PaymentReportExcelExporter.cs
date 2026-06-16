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
            var sheet = workbook.Worksheets.Add("Summary");

            sheet.Cell(1, 1).Value = "Payment Report Summary";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            sheet.Cell(2, 1).Value = "From Date:";
            sheet.Cell(2, 2).Value = report.FromDate?.ToString("yyyy-MM-dd") ?? "All";

            sheet.Cell(3, 1).Value = "To Date:";
            sheet.Cell(3, 2).Value = report.ToDate?.ToString("yyyy-MM-dd") ?? "All";

            sheet.Cell(4, 1).Value = "Total Orders:";
            sheet.Cell(4, 2).Value = report.TotalOrders;

            sheet.Cell(5, 1).Value = "Grand Total (Toman):";
            sheet.Cell(5, 2).Value = report.GrandTotalToman;

            var headerRow = 7;
            sheet.Cell(headerRow, 1).Value = "Name";
            sheet.Cell(headerRow, 2).Value = "Chat ID";
            sheet.Cell(headerRow, 3).Value = "Order Count";
            sheet.Cell(headerRow, 4).Value = "Total to Pay (Toman)";

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
            var sheet = workbook.Worksheets.Add("Orders");

            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Chat ID";
            sheet.Cell(1, 3).Value = "Drink";
            sheet.Cell(1, 4).Value = "Shots";
            sheet.Cell(1, 5).Value = "Chocolate";
            sheet.Cell(1, 6).Value = "Price (Toman)";
            sheet.Cell(1, 7).Value = "Date";

            var headerRange = sheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;

            foreach (var person in report.Summaries)
            {
                foreach (var order in person.Orders)
                {
                    sheet.Cell(row, 1).Value = person.DisplayName;
                    sheet.Cell(row, 2).Value = person.ChatId;
                    sheet.Cell(row, 3).Value = order.DrinkType;
                    sheet.Cell(row, 4).Value = order.ShotCount;
                    sheet.Cell(row, 5).Value = order.WithChocolate ? "Yes" : "No";
                    sheet.Cell(row, 6).Value = order.PriceInToman;
                    sheet.Cell(row, 7).Value = order.CreatedAt;
                    sheet.Cell(row, 7).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                    row++;
                }
            }

            sheet.Columns().AdjustToContents();
        }
    }
}
