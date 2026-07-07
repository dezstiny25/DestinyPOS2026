namespace DestinyPOS2026.Wpf.Models;

/// <summary>
/// Represents a printing option combination (Paper Size x Type x Quantity)
/// Used to calculate pricing for printing services dynamically
/// </summary>
public class PrintingOption
{
    public string PaperSize { get; set; } = string.Empty; // "Letter", "A4", "Legal"
    public string PrintType { get; set; } = string.Empty; // "BW" (Black & White), "Color"
    public int Quantity { get; set; } // Number of pages/copies
    
    /// <summary>
    /// Price per unit (per page/copy) for this combination
    /// </summary>
    public decimal PricePerUnit { get; set; }
    
    /// <summary>
    /// Total price for this printing option
    /// </summary>
    public decimal TotalPrice => PricePerUnit * Quantity;
    
    public override string ToString() => $"{PaperSize} {PrintType} x{Quantity}";
}
