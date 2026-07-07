using System;

namespace DestinyPOS2026.Wpf.Models;

/// <summary>
/// Represents a transaction for logging to SalesReport.xlsx
/// </summary>
public class Transaction
{
    public DateTime Timestamp { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "Product" or "Service"
    public string Description { get; set; } = string.Empty; // Product name or Service description
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // "CASH", "GCASH", "CARD"
    public string Notes { get; set; } = string.Empty;
}
