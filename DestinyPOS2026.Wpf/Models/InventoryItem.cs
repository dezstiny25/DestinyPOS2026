using System;

namespace DestinyPOS2026.Wpf.Models;

/// <summary>
/// Represents an item in the inventory (matches structure from Inventory.xlsx)
/// </summary>
public class InventoryItem
{
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Product", "Supplies", etc.
    public decimal UnitPrice { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; } // Minimum stock threshold for alerts
    public int ReorderQuantity { get; set; } // Quantity to order when reordering
    public string Supplier { get; set; } = string.Empty;
    public DateTime LastRestocked { get; set; }
}
