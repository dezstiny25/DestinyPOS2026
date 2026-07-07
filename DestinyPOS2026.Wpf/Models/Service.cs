namespace DestinyPOS2026.Wpf.Models;

/// <summary>
/// Represents a service offering (labor-based): Computer Repair, Printer Repair, Printing, etc.
/// </summary>
public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Printing", "ComputerRepair", "PrinterRepair"
    public string Description { get; set; } = string.Empty;
    public decimal? BasePrice { get; set; } // For printing services with predefined pricing
    public bool AllowCustomPrice { get; set; } // For repair services where price is negotiable
}
