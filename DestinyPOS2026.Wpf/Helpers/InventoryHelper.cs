using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DestinyPOS2026.Wpf.Models;
using OfficeOpenXml;

namespace DestinyPOS2026.Wpf.Helpers;

/// <summary>
/// Manages inventory data in CLM Inventory.xlsx file
/// Handles reading, writing, stock updates, and low stock alerts
/// </summary>
public static class InventoryHelper
{
    private static string InventoryPath
    {
        get
        {
            var candidatePaths = new List<string>();
            var currentDir = Directory.GetCurrentDirectory();

            void AddDirectoryCandidates(string? directory)
            {
                if (string.IsNullOrWhiteSpace(directory)) return;

                candidatePaths.Add(Path.Combine(directory, "CLM Inventory.xlsx"));

                var parentDir = new DirectoryInfo(directory);
                while (parentDir?.Parent != null)
                {
                    parentDir = parentDir.Parent;
                    candidatePaths.Add(Path.Combine(parentDir.FullName, "CLM Inventory.xlsx"));
                }
            }

            AddDirectoryCandidates(currentDir);
            AddDirectoryCandidates(AppContext.BaseDirectory);

            foreach (var candidatePath in candidatePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            return Path.Combine(currentDir, "CLM Inventory.xlsx");
        }
    }

    static InventoryHelper()
    {
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Initializes the Inventory.xlsx file with headers if it doesn't exist
    /// </summary>
    public static void InitializeInventoryFile()
    {
        if (File.Exists(InventoryPath)) return;

        using var package = new ExcelPackage(new FileInfo(InventoryPath));
        var ws = package.Workbook.Worksheets.Add("Inventory");

        // Create headers
        ws.Cells[1, 1].Value = "Barcode";
        ws.Cells[1, 2].Value = "Product Name";
        ws.Cells[1, 3].Value = "Category";
        ws.Cells[1, 4].Value = "Unit Price";
        ws.Cells[1, 5].Value = "Current Stock";
        ws.Cells[1, 6].Value = "Reorder Level";
        ws.Cells[1, 7].Value = "Reorder Quantity";
        ws.Cells[1, 8].Value = "Supplier";
        ws.Cells[1, 9].Value = "Last Restocked";

        // Format header row
        var headerRow = ws.Cells[1, 1, 1, 9];
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        headerRow.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Set column widths
        ws.Column(1).Width = 15;
        ws.Column(2).Width = 25;
        ws.Column(3).Width = 15;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 14;
        ws.Column(6).Width = 13;
        ws.Column(7).Width = 17;
        ws.Column(8).Width = 15;
        ws.Column(9).Width = 15;

        package.SaveAs(new FileInfo(InventoryPath));
    }

    private static ExcelWorksheet? GetInventoryWorksheet(ExcelPackage package)
    {
        return package.Workbook.Worksheets["Inventory"]
            ?? package.Workbook.Worksheets.FirstOrDefault();
    }

    /// <summary>
    /// Gets all inventory items from the CLM Inventory.xlsx file (thread-safe)
    /// </summary>
    public static List<InventoryItem> GetAllInventoryItems()
    {
        return FileAccessLayer.WithInventoryLock(() =>
        {
            InitializeInventoryFile();
            var items = new List<InventoryItem>();

            using var package = new ExcelPackage(new FileInfo(InventoryPath));
            var ws = GetInventoryWorksheet(package);
            if (ws == null) return items;

            for (int row = 2; row <= ws.Dimension?.Rows; row++)
            {
                var barcode = ws.Cells[row, 1].Value?.ToString();
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                items.Add(new InventoryItem
                {
                    Barcode = barcode,
                    ProductName = ws.Cells[row, 2].Value?.ToString() ?? string.Empty,
                    Category = ws.Cells[row, 3].Value?.ToString() ?? string.Empty,
                    UnitPrice = Convert.ToDecimal(ws.Cells[row, 4].Value ?? 0),
                    CurrentStock = Convert.ToInt32(ws.Cells[row, 5].Value ?? 0),
                    ReorderLevel = Convert.ToInt32(ws.Cells[row, 6].Value ?? 0),
                    ReorderQuantity = Convert.ToInt32(ws.Cells[row, 7].Value ?? 0),
                    Supplier = ws.Cells[row, 8].Value?.ToString() ?? string.Empty,
                    LastRestocked = ws.Cells[row, 9].Value is DateTime dt ? dt : DateTime.MinValue
                });
            }

            return items;
        });
    }

    /// <summary>
    /// Gets an inventory item by barcode
    /// </summary>
    public static InventoryItem? GetInventoryItemByBarcode(string barcode)
    {
        return GetAllInventoryItems().FirstOrDefault(x => x.Barcode == barcode);
    }

    /// <summary>
    /// Updates stock quantity for a product by barcode (thread-safe)
    /// Returns true if successful, false if product not found
    /// </summary>
    public static bool UpdateStock(string barcode, int newQuantity)
    {
        return FileAccessLayer.WithInventoryLock(() =>
        {
            InitializeInventoryFile();

            using var package = new ExcelPackage(new FileInfo(InventoryPath));
            var ws = GetInventoryWorksheet(package);
            if (ws == null) return false;

            for (int row = 2; row <= ws.Dimension?.Rows; row++)
            {
                if (ws.Cells[row, 1].Value?.ToString() == barcode)
                {
                    ws.Cells[row, 5].Value = newQuantity;
                    package.SaveAs(new FileInfo(InventoryPath));
                    return true;
                }
            }

            return false;
        });
    }

    /// <summary>
    /// Deducts stock quantity from inventory (called after a sale)
    /// Returns true if successful, false if insufficient stock or product not found
    /// </summary>
    public static bool DeductStock(string barcode, int quantity)
    {
        var item = GetInventoryItemByBarcode(barcode);
        if (item == null || item.CurrentStock < quantity)
            return false;

        return UpdateStock(barcode, item.CurrentStock - quantity);
    }

    /// <summary>
    /// Gets all items that are below their reorder level
    /// </summary>
    public static List<InventoryItem> GetLowStockItems()
    {
        return GetAllInventoryItems()
            .Where(x => x.CurrentStock <= x.ReorderLevel)
            .ToList();
    }

    /// <summary>
    /// Adds a new inventory item to the Excel file (thread-safe)
    /// </summary>
    public static void AddInventoryItem(InventoryItem item)
    {
        FileAccessLayer.WithInventoryLock(() =>
        {
            InitializeInventoryFile();

            using var package = new ExcelPackage(new FileInfo(InventoryPath));
            var ws = GetInventoryWorksheet(package);
            if (ws == null)
            {
                ws = package.Workbook.Worksheets.Add("Inventory");
            }

            var nextRow = (ws.Dimension?.Rows ?? 1) + 1;

            ws.Cells[nextRow, 1].Value = item.Barcode;
            ws.Cells[nextRow, 2].Value = item.ProductName;
            ws.Cells[nextRow, 3].Value = item.Category;
            ws.Cells[nextRow, 4].Value = item.UnitPrice;
            ws.Cells[nextRow, 5].Value = item.CurrentStock;
            ws.Cells[nextRow, 6].Value = item.ReorderLevel;
            ws.Cells[nextRow, 7].Value = item.ReorderQuantity;
            ws.Cells[nextRow, 8].Value = item.Supplier;
            ws.Cells[nextRow, 9].Value = DateTime.Now;

            // Format numbers
            ws.Cells[nextRow, 4].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 9].Style.Numberformat.Format = "mm/dd/yyyy";

            package.SaveAs(new FileInfo(InventoryPath));
        });
    }

    /// <summary>
    /// Updates an existing inventory item in the Excel file
    /// </summary>
    public static void UpdateInventoryItem(InventoryItem item)
    {
        InitializeInventoryFile();

        using var package = new ExcelPackage(new FileInfo(InventoryPath));
        var ws = GetInventoryWorksheet(package);
        if (ws == null) return;

        for (int row = 2; row <= ws.Dimension?.Rows; row++)
        {
            if (ws.Cells[row, 1].Value?.ToString() == item.Barcode)
            {
                ws.Cells[row, 2].Value = item.ProductName;
                ws.Cells[row, 3].Value = item.Category;
                ws.Cells[row, 4].Value = item.UnitPrice;
                ws.Cells[row, 5].Value = item.CurrentStock;
                ws.Cells[row, 6].Value = item.ReorderLevel;
                ws.Cells[row, 7].Value = item.ReorderQuantity;
                ws.Cells[row, 8].Value = item.Supplier;
                ws.Cells[row, 9].Value = DateTime.Now;

                package.SaveAs(new FileInfo(InventoryPath));
                return;
            }
        }
    }

    /// <summary>
    /// Checks if a barcode already exists in inventory (thread-safe)
    /// Returns true if barcode is unique (not found), false if duplicate
    /// </summary>
    public static bool IsBarcodeUnique(string barcode)
    {
        return GetInventoryItemByBarcode(barcode) == null;
    }

    /// <summary>
    /// Updates stock for existing item or adds new item if it doesn't exist (thread-safe)
    /// Validates item code uniqueness before adding
    /// Returns tuple: (success, message, itemBarcode)
    /// </summary>
    public static (bool success, string message, string barcode) UpdateOrAddInventoryItem(InventoryItem item)
    {
        return FileAccessLayer.WithInventoryLock(() =>
        {
            InitializeInventoryFile();

            using var package = new ExcelPackage(new FileInfo(InventoryPath));
            var ws = GetInventoryWorksheet(package);
            if (ws == null) return (false, "Inventory worksheet not found.", item.Barcode);

            // Check if item already exists
            for (int row = 2; row <= ws.Dimension?.Rows; row++)
            {
                var existingBarcode = ws.Cells[row, 1].Value?.ToString();
                if (existingBarcode == item.Barcode)
                {
                    // Update existing item - increment stock
                    var currentStock = Convert.ToInt32(ws.Cells[row, 5].Value ?? 0);
                    ws.Cells[row, 5].Value = currentStock + item.CurrentStock;
                    ws.Cells[row, 9].Value = DateTime.Now;

                    package.SaveAs(new FileInfo(InventoryPath));
                    return (true, $"Stock updated: {item.ProductName} (Added {item.CurrentStock} units)", item.Barcode);
                }
            }

            // Item doesn't exist - add as new
            var nextRow = (ws.Dimension?.Rows ?? 1) + 1;

            ws.Cells[nextRow, 1].Value = item.Barcode;
            ws.Cells[nextRow, 2].Value = item.ProductName;
            ws.Cells[nextRow, 3].Value = item.Category;
            ws.Cells[nextRow, 4].Value = item.UnitPrice;
            ws.Cells[nextRow, 5].Value = item.CurrentStock;
            ws.Cells[nextRow, 6].Value = item.ReorderLevel;
            ws.Cells[nextRow, 7].Value = item.ReorderQuantity;
            ws.Cells[nextRow, 8].Value = item.Supplier;
            ws.Cells[nextRow, 9].Value = DateTime.Now;

            // Format numbers
            ws.Cells[nextRow, 4].Style.Numberformat.Format = "₱#,##0.00";
            ws.Cells[nextRow, 9].Style.Numberformat.Format = "mm/dd/yyyy";

            package.SaveAs(new FileInfo(InventoryPath));
            return (true, $"New item added: {item.ProductName}", item.Barcode);
        });
    }

    /// <summary>
    /// Increments stock quantity for an item (for restocking operations)
    /// If item doesn't exist, it adds it with the quantity as initial stock
    /// Returns tuple: (success, message)
    /// </summary>
    public static (bool success, string message) IncrementStock(string barcode, int quantityToAdd, string productName = "")
    {
        return FileAccessLayer.WithInventoryLock(() =>
        {
            var existingItem = GetInventoryItemByBarcode(barcode);
            if (existingItem != null)
            {
                // Item exists - increment its stock
                var newStock = existingItem.CurrentStock + quantityToAdd;
                UpdateStock(barcode, newStock);
                return (true, $"{existingItem.ProductName}: Stock increased by {quantityToAdd} (New: {newStock})");
            }

            // Item doesn't exist - this shouldn't happen in normal workflow
            return (false, $"Product with barcode {barcode} not found in inventory.");
        });
    }
}
