using System;
using System.Collections.Generic;

namespace DestinyPOS2026.Wpf.Models;

public class SaleRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<SaleItem> Items { get; set; } = new();
}
