using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DestinyPOS2026.Wpf.Models;
using OfficeOpenXml;

namespace DestinyPOS2026.Wpf.Helpers;

/// <summary>
/// Manages sales report logging to SalesReport.xlsx file
/// Records all transactions (Products and Services) with detailed information
/// </summary>
public static class SalesReportHelper
{
    private static readonly string SalesReportPath = 
        Path.Combine(AppContext.BaseDirectory, "SalesReport.xlsx");

    static SalesReportHelper()
    {
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Initializes the SalesReport.xlsx file with headers if it doesn't exist
    /// </summary>
    public static void InitializeSalesReportFile()
    {
        if (File.Exists(SalesReportPath)) return;

        using var package = new ExcelPackage(new FileInfo(SalesReportPath));
        var ws = package.Workbook.Worksheets.Add("Sales");

        // Create headers
        ws.Cells[1, 1].Value = "Timestamp";
        ws.Cells[1, 2].Value = "Transaction Type";
        ws.Cells[1, 3].Value = "Description";
        ws.Cells[1, 4].Value = "Quantity";
        ws.Cells[1, 5].Value = "Unit Price";
        ws.Cells[1, 6].Value = "Total Price";
        ws.Cells[1, 7].Value = "Payment Method";
        ws.Cells[1, 8].Value = "Cash Tendered";
        ws.Cells[1, 9].Value = "Change Due";
        ws.Cells[1, 10].Value = "Payment Status";
        ws.Cells[1, 11].Value = "Notes";

        // Format header row
        var headerRow = ws.Cells[1, 1, 1, 11];
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        headerRow.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

        // Set column widths
        ws.Column(1).Width = 20;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 10;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 15;
        ws.Column(8).Width = 14;
        ws.Column(9).Width = 14;
        ws.Column(10).Width = 16;
        ws.Column(11).Width = 20;

        // Create Summary sheet
        var summaryWs = package.Workbook.Worksheets.Add("Summary");
        summaryWs.Cells[1, 1].Value = "Daily Sales Summary";
        summaryWs.Cells[1, 1].Style.Font.Bold = true;
        summaryWs.Cells[1, 1].Style.Font.Size = 14;

        package.SaveAs(new FileInfo(SalesReportPath));
    }

    /// <summary>
    /// Logs a single transaction to the sales report (thread-safe)
    /// </summary>
    public static void LogTransaction(Transaction transaction)
    {
        FileAccessLayer.WithSalesReportLock(() =>
        {
            InitializeSalesReportFile();

            using var package = new ExcelPackage(new FileInfo(SalesReportPath));
            var ws = package.Workbook.Worksheets["Sales"];

            var nextRow = (ws.Dimension?.Rows ?? 1) + 1;

            ws.Cells[nextRow, 1].Value = transaction.Timestamp;
            ws.Cells[nextRow, 2].Value = transaction.TransactionType;
            ws.Cells[nextRow, 3].Value = transaction.Description;
            ws.Cells[nextRow, 4].Value = transaction.Quantity;
            ws.Cells[nextRow, 5].Value = transaction.UnitPrice;
            ws.Cells[nextRow, 6].Value = transaction.TotalPrice;
            ws.Cells[nextRow, 7].Value = transaction.PaymentMethod;
            ws.Cells[nextRow, 8].Value = transaction.CashTendered;
            ws.Cells[nextRow, 9].Value = transaction.ChangeDue;
            ws.Cells[nextRow, 10].Value = transaction.PaymentStatus;
            ws.Cells[nextRow, 11].Value = transaction.Notes;

            // Format cells
            ws.Cells[nextRow, 1].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss";
            ws.Cells[nextRow, 5].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 6].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 8].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 9].Style.Numberformat.Format = "₱#,##0.00";

            package.SaveAs(new FileInfo(SalesReportPath));
        });
    }

    /// <summary>
    /// Logs multiple transactions at once (batch logging - thread-safe)
    /// </summary>
    public static void LogTransactions(List<Transaction> transactions)
    {
        FileAccessLayer.WithSalesReportLock(() =>
        {
            InitializeSalesReportFile();

            using var package = new ExcelPackage(new FileInfo(SalesReportPath));
            var ws = package.Workbook.Worksheets["Sales"];

            var startRow = (ws.Dimension?.Rows ?? 1) + 1;

            for (int i = 0; i < transactions.Count; i++)
            {
                var row = startRow + i;
                var transaction = transactions[i];

                ws.Cells[row, 1].Value = transaction.Timestamp;
                ws.Cells[row, 2].Value = transaction.TransactionType;
                ws.Cells[row, 3].Value = transaction.Description;
                ws.Cells[row, 4].Value = transaction.Quantity;
                ws.Cells[row, 5].Value = transaction.UnitPrice;
                ws.Cells[row, 6].Value = transaction.TotalPrice;
                ws.Cells[row, 7].Value = transaction.PaymentMethod;
                ws.Cells[row, 8].Value = transaction.CashTendered;
                ws.Cells[row, 9].Value = transaction.ChangeDue;
                ws.Cells[row, 10].Value = transaction.PaymentStatus;
                ws.Cells[row, 11].Value = transaction.Notes;

                // Format cells
                ws.Cells[row, 1].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss";
                ws.Cells[row, 5].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 6].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 8].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 9].Style.Numberformat.Format = "₱#,##0.00";
            }

            package.SaveAs(new FileInfo(SalesReportPath));
        });
    }

    /// <summary>
    /// Gets all transactions within a date range
    /// </summary>
    public static List<Transaction> GetTransactionsByDateRange(DateTime startDate, DateTime endDate)
    {
        InitializeSalesReportFile();
        var transactions = new List<Transaction>();

        using var package = new ExcelPackage(new FileInfo(SalesReportPath));
        var ws = package.Workbook.Worksheets["Sales"];

        for (int row = 2; row <= ws.Dimension?.Rows; row++)
        {
            if (ws.Cells[row, 1].Value is not DateTime timestamp) continue;
            if (timestamp < startDate || timestamp > endDate) continue;

            transactions.Add(new Transaction
            {
                Timestamp = timestamp,
                TransactionType = ws.Cells[row, 2].Value?.ToString() ?? string.Empty,
                Description = ws.Cells[row, 3].Value?.ToString() ?? string.Empty,
                Quantity = Convert.ToInt32(ws.Cells[row, 4].Value ?? 0),
                UnitPrice = Convert.ToDecimal(ws.Cells[row, 5].Value ?? 0),
                TotalPrice = Convert.ToDecimal(ws.Cells[row, 6].Value ?? 0),
                PaymentMethod = ws.Cells[row, 7].Value?.ToString() ?? string.Empty,
                CashTendered = Convert.ToDecimal(ws.Cells[row, 8].Value ?? 0),
                ChangeDue = Convert.ToDecimal(ws.Cells[row, 9].Value ?? 0),
                PaymentStatus = ws.Cells[row, 10].Value?.ToString() ?? string.Empty,
                Notes = ws.Cells[row, 11].Value?.ToString() ?? string.Empty
            });
        }

        return transactions;
    }

    /// <summary>
    /// Gets daily sales summary
    /// </summary>
    public static decimal GetDailySalesTotal(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

        return GetTransactionsByDateRange(startOfDay, endOfDay)
            .Sum(x => x.TotalPrice);
    }

    /// <summary>
    /// Gets sales breakdown by transaction type
    /// </summary>
    public static Dictionary<string, decimal> GetSalesBreakdown(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

        var transactions = GetTransactionsByDateRange(startOfDay, endOfDay);

        return transactions
            .GroupBy(x => x.TransactionType)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));
    }

    /// <summary>
    /// Gets sales breakdown by payment method
    /// </summary>
    public static Dictionary<string, decimal> GetPaymentMethodBreakdown(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

        var transactions = GetTransactionsByDateRange(startOfDay, endOfDay);

        return transactions
            .GroupBy(x => x.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));
    }

    /// <summary>
    /// Gets all transactions for a specific date
    /// </summary>
    public static List<Transaction> GetTransactionsByDate(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);
        return GetTransactionsByDateRange(startOfDay, endOfDay);
    }

    // ============================================================================
    // NEW: Monthly/Daily Filing Methods
    // ============================================================================

    /// <summary>
    /// Generates monthly file name based on date (e.g., "Sales_July_2026.xlsx")
    /// </summary>
    private static string GetMonthlyFileName(DateTime date)
    {
        string monthName = date.ToString("MMMM");
        string year = date.Year.ToString();
        return Path.Combine(AppContext.BaseDirectory, $"Sales_{monthName}_{year}.xlsx");
    }

    /// <summary>
    /// Gets sheet name for a specific day (e.g., "08" for the 8th)
    /// </summary>
    private static string GetDaySheetName(DateTime date)
    {
        return date.Day.ToString("00"); // "01", "08", "15", "25", etc.
    }

    /// <summary>
    /// Initializes a monthly sales file for a specific date
    /// Creates it if it doesn't exist
    /// </summary>
    private static void InitializeMonthlyFile(DateTime date)
    {
        var monthlyPath = GetMonthlyFileName(date);
        
        if (File.Exists(monthlyPath)) return;

        using var package = new ExcelPackage(new FileInfo(monthlyPath));
        
        // Create headers template
        var ws = package.Workbook.Worksheets.Add("Template");
        ws.Cells[1, 1].Value = "Timestamp";
        ws.Cells[1, 2].Value = "Transaction Type";
        ws.Cells[1, 3].Value = "Description";
        ws.Cells[1, 4].Value = "Quantity";
        ws.Cells[1, 5].Value = "Unit Price";
        ws.Cells[1, 6].Value = "Total Price";
        ws.Cells[1, 7].Value = "Payment Method";
        ws.Cells[1, 8].Value = "Cash Tendered";
        ws.Cells[1, 9].Value = "Change Due";
        ws.Cells[1, 10].Value = "Payment Status";
        ws.Cells[1, 11].Value = "Notes";

        var headerRow = ws.Cells[1, 1, 1, 11];
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        headerRow.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 10;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 15;
        ws.Column(8).Width = 14;
        ws.Column(9).Width = 14;
        ws.Column(10).Width = 16;
        ws.Column(11).Width = 20;

        // Create Summary sheet
        var summaryWs = package.Workbook.Worksheets.Add("Summary");
        summaryWs.Cells[1, 1].Value = $"Monthly Sales Summary - {date:MMMM yyyy}";
        summaryWs.Cells[1, 1].Style.Font.Bold = true;
        summaryWs.Cells[1, 1].Style.Font.Size = 14;

        package.SaveAs(new FileInfo(monthlyPath));
    }

    /// <summary>
    /// Gets or creates a day sheet in the monthly file
    /// Returns the worksheet for the day (e.g., "08" for the 8th)
    /// </summary>
    private static ExcelWorksheet GetOrCreateDaySheet(ExcelPackage package, DateTime date)
    {
        string dayName = GetDaySheetName(date);
        
        var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == dayName);
        
        if (worksheet == null)
        {
            // Create new sheet for this day
            worksheet = package.Workbook.Worksheets.Add(dayName);
            
            // Add headers
            worksheet.Cells[1, 1].Value = "Timestamp";
            worksheet.Cells[1, 2].Value = "Transaction Type";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Quantity";
            worksheet.Cells[1, 5].Value = "Unit Price";
            worksheet.Cells[1, 6].Value = "Total Price";
            worksheet.Cells[1, 7].Value = "Payment Method";
            worksheet.Cells[1, 8].Value = "Cash Tendered";
            worksheet.Cells[1, 9].Value = "Change Due";
            worksheet.Cells[1, 10].Value = "Payment Status";
            worksheet.Cells[1, 11].Value = "Notes";

            var headerRow = worksheet.Cells[1, 1, 1, 11];
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRow.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

            worksheet.Column(1).Width = 20;
            worksheet.Column(2).Width = 18;
            worksheet.Column(3).Width = 30;
            worksheet.Column(4).Width = 10;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 12;
            worksheet.Column(7).Width = 15;
            worksheet.Column(8).Width = 14;
            worksheet.Column(9).Width = 14;
            worksheet.Column(10).Width = 16;
            worksheet.Column(11).Width = 20;
        }

        return worksheet;
    }

    /// <summary>
    /// Logs a transaction to the monthly file with daily sheet organization
    /// Automatically creates monthly file and daily sheet as needed (thread-safe)
    /// </summary>
    public static void LogTransactionMonthly(Transaction transaction)
    {
        FileAccessLayer.WithSalesReportLock(() =>
        {
            DateTime transactionDate = transaction.Timestamp;
            InitializeMonthlyFile(transactionDate);

            var monthlyPath = GetMonthlyFileName(transactionDate);

            using var package = new ExcelPackage(new FileInfo(monthlyPath));
            var ws = GetOrCreateDaySheet(package, transactionDate);

            var nextRow = (ws.Dimension?.Rows ?? 1) + 1;

            ws.Cells[nextRow, 1].Value = transaction.Timestamp;
            ws.Cells[nextRow, 2].Value = transaction.TransactionType;
            ws.Cells[nextRow, 3].Value = transaction.Description;
            ws.Cells[nextRow, 4].Value = transaction.Quantity;
            ws.Cells[nextRow, 5].Value = transaction.UnitPrice;
            ws.Cells[nextRow, 6].Value = transaction.TotalPrice;
            ws.Cells[nextRow, 7].Value = transaction.PaymentMethod;
            ws.Cells[nextRow, 8].Value = transaction.CashTendered;
            ws.Cells[nextRow, 9].Value = transaction.ChangeDue;
            ws.Cells[nextRow, 10].Value = transaction.PaymentStatus;
            ws.Cells[nextRow, 11].Value = transaction.Notes;

            // Format cells
            ws.Cells[nextRow, 1].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss";
            ws.Cells[nextRow, 5].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 6].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 8].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 9].Style.Numberformat.Format = "₱#,##0.00";

            package.SaveAs(new FileInfo(monthlyPath));
        });
    }

    /// <summary>
    /// Logs multiple transactions to the monthly file with daily sheet organization
    /// Batch operation - more efficient than individual logging (thread-safe)
    /// </summary>
    public static void LogTransactionsMonthly(List<Transaction> transactions)
    {
        FileAccessLayer.WithSalesReportLock(() =>
        {
            // Group transactions by month and day
            var groupedByMonthDay = transactions
                .GroupBy(t => new { Month = t.Timestamp.Month, Year = t.Timestamp.Year, Day = t.Timestamp.Day })
                .ToList();

            foreach (var monthDayGroup in groupedByMonthDay)
            {
                var firstTransaction = monthDayGroup.First();
                var date = firstTransaction.Timestamp.Date;

                InitializeMonthlyFile(date);
                var monthlyPath = GetMonthlyFileName(date);

                using var package = new ExcelPackage(new FileInfo(monthlyPath));
                var ws = GetOrCreateDaySheet(package, date);

                var startRow = (ws.Dimension?.Rows ?? 1) + 1;

                for (int i = 0; i < monthDayGroup.Count(); i++)
                {
                    var row = startRow + i;
                    var transaction = monthDayGroup.ElementAt(i);

                    ws.Cells[row, 1].Value = transaction.Timestamp;
                    ws.Cells[row, 2].Value = transaction.TransactionType;
                    ws.Cells[row, 3].Value = transaction.Description;
                    ws.Cells[row, 4].Value = transaction.Quantity;
                    ws.Cells[row, 5].Value = transaction.UnitPrice;
                    ws.Cells[row, 6].Value = transaction.TotalPrice;
                    ws.Cells[row, 7].Value = transaction.PaymentMethod;
                    ws.Cells[row, 8].Value = transaction.CashTendered;
                    ws.Cells[row, 9].Value = transaction.ChangeDue;
                    ws.Cells[row, 10].Value = transaction.PaymentStatus;
                    ws.Cells[row, 11].Value = transaction.Notes;

                    // Format cells
                    ws.Cells[row, 1].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss";
                    ws.Cells[row, 5].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[row, 6].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[row, 8].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[row, 9].Style.Numberformat.Format = "₱#,##0.00";
                }

                package.SaveAs(new FileInfo(monthlyPath));
            }
        });
    }

    /// <summary>
    /// Gets daily sales total from monthly file for a specific date
    /// </summary>
    public static decimal GetDailySalesTotalMonthly(DateTime date)
    {
        var monthlyPath = GetMonthlyFileName(date);
        if (!File.Exists(monthlyPath)) return 0;

        try
        {
            using var package = new ExcelPackage(new FileInfo(monthlyPath));
            var dayName = GetDaySheetName(date);
            var ws = package.Workbook.Worksheets.FirstOrDefault(w => w.Name == dayName);

            if (ws == null) return 0;

            decimal total = 0;
            for (int row = 2; row <= ws.Dimension?.Rows; row++)
            {
                if (ws.Cells[row, 6].Value is decimal price)
                    total += price;
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets transactions from monthly file for a specific date
    /// </summary>
    public static List<Transaction> GetTransactionsByDateMonthly(DateTime date)
    {
        var transactions = new List<Transaction>();
        var monthlyPath = GetMonthlyFileName(date);

        if (!File.Exists(monthlyPath)) return transactions;

        try
        {
            using var package = new ExcelPackage(new FileInfo(monthlyPath));
            var dayName = GetDaySheetName(date);
            var ws = package.Workbook.Worksheets.FirstOrDefault(w => w.Name == dayName);

            if (ws == null) return transactions;

            for (int row = 2; row <= ws.Dimension?.Rows; row++)
            {
                transactions.Add(new Transaction
                {
                    Timestamp = ws.Cells[row, 1].Value is DateTime ts ? ts : DateTime.Now,
                    TransactionType = ws.Cells[row, 2].Value?.ToString() ?? string.Empty,
                    Description = ws.Cells[row, 3].Value?.ToString() ?? string.Empty,
                    Quantity = Convert.ToInt32(ws.Cells[row, 4].Value ?? 0),
                    UnitPrice = Convert.ToDecimal(ws.Cells[row, 5].Value ?? 0),
                    TotalPrice = Convert.ToDecimal(ws.Cells[row, 6].Value ?? 0),
                    PaymentMethod = ws.Cells[row, 7].Value?.ToString() ?? string.Empty,
                    Notes = ws.Cells[row, 8].Value?.ToString() ?? string.Empty
                });
            }
        }
        catch
        {
            // Return empty list if file access fails
        }

        return transactions;
    }
}
