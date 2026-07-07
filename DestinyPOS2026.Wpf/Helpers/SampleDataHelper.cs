using System;
using System.Collections.Generic;
using DestinyPOS2026.Wpf.Models;

namespace DestinyPOS2026.Wpf.Helpers;

/// <summary>
/// Initializes sample data for testing the POS system
/// Run this once to populate Inventory.xlsx and create sample data
/// </summary>
public static class SampleDataHelper
{
    public static void InitializeSampleData()
    {
        InventoryHelper.InitializeInventoryFile();

        // Only add if inventory is empty
        var items = InventoryHelper.GetAllInventoryItems();
        if (items.Count > 0)
            return;

        // Add sample products
        var products = new List<InventoryItem>
        {
            // Supplies
            new InventoryItem
            {
                Barcode = "BAR001",
                ProductName = "A4 Paper (500 sheets)",
                Category = "Supplies",
                UnitPrice = 150m,
                CurrentStock = 25,
                ReorderLevel = 5,
                ReorderQuantity = 10,
                Supplier = "Office Plus",
                LastRestocked = DateTime.Now.AddDays(-7)
            },
            new InventoryItem
            {
                Barcode = "BAR002",
                ProductName = "Ink Cartridge (Black)",
                Category = "Supplies",
                UnitPrice = 450m,
                CurrentStock = 15,
                ReorderLevel = 3,
                ReorderQuantity = 6,
                Supplier = "Tech Supplies Inc",
                LastRestocked = DateTime.Now.AddDays(-5)
            },
            new InventoryItem
            {
                Barcode = "BAR003",
                ProductName = "Ink Cartridge (Color)",
                Category = "Supplies",
                UnitPrice = 650m,
                CurrentStock = 12,
                ReorderLevel = 3,
                ReorderQuantity = 6,
                Supplier = "Tech Supplies Inc",
                LastRestocked = DateTime.Now.AddDays(-5)
            },
            new InventoryItem
            {
                Barcode = "BAR004",
                ProductName = "USB Flash Drive (32GB)",
                Category = "Products",
                UnitPrice = 399m,
                CurrentStock = 8,
                ReorderLevel = 2,
                ReorderQuantity = 5,
                Supplier = "Electronics Hub",
                LastRestocked = DateTime.Now.AddDays(-10)
            },
            new InventoryItem
            {
                Barcode = "BAR005",
                ProductName = "External Hard Drive (1TB)",
                Category = "Products",
                UnitPrice = 2500m,
                CurrentStock = 3,
                ReorderLevel = 2,
                ReorderQuantity = 3,
                Supplier = "Electronics Hub",
                LastRestocked = DateTime.Now.AddDays(-15)
            },
            new InventoryItem
            {
                Barcode = "BAR006",
                ProductName = "Keyboard (Mechanical)",
                Category = "Products",
                UnitPrice = 1200m,
                CurrentStock = 5,
                ReorderLevel = 2,
                ReorderQuantity = 3,
                Supplier = "Tech Supplies Inc",
                LastRestocked = DateTime.Now.AddDays(-20)
            },
            new InventoryItem
            {
                Barcode = "BAR007",
                ProductName = "Mouse (Wireless)",
                Category = "Products",
                UnitPrice = 599m,
                CurrentStock = 10,
                ReorderLevel = 5,
                ReorderQuantity = 8,
                Supplier = "Tech Supplies Inc",
                LastRestocked = DateTime.Now.AddDays(-12)
            },
            new InventoryItem
            {
                Barcode = "BAR008",
                ProductName = "HDMI Cable (2m)",
                Category = "Accessories",
                UnitPrice = 199m,
                CurrentStock = 20,
                ReorderLevel = 10,
                ReorderQuantity = 15,
                Supplier = "Electronics Hub",
                LastRestocked = DateTime.Now.AddDays(-3)
            }
        };

        foreach (var product in products)
        {
            InventoryHelper.AddInventoryItem(product);
        }
    }

    /// <summary>
    /// Generates a sample receipt message
    /// </summary>
    public static string GenerateSampleReceipt()
    {
        var receipt = @"
╔══════════════════════════════════════╗
║     DESTINY POS - SAMPLE RECEIPT     ║
╚══════════════════════════════════════╝

Date: 2026-07-07 14:35:42

Product Items:
────────────────────────────────────
A4 Paper (500 sheets)     x1    ₱150.00
Ink Cartridge (Black)     x1    ₱450.00

Service Items:
────────────────────────────────────
Printing: Letter BW x100 pages       ₱50.00
Computer Repair (120 min, 1.5x)      ₱1,200.00

────────────────────────────────────
Subtotal:                           ₱1,850.00
Discount (5%):                      (₱92.50)
────────────────────────────────────
TOTAL:                              ₱1,757.50

Payment Method: CASH
════════════════════════════════════════

Thank you for your business!
";
        return receipt;
    }
}
