using System;
using System.Collections.Generic;
using DestinyPOS2026.Wpf.Models;

namespace DestinyPOS2026.Wpf.Helpers;

/// <summary>
/// Handles dynamic pricing logic for Printing services and Repairs
/// </summary>
public static class PricingHelper
{
    /// <summary>
    /// Pricing matrix for printing services
    /// Key: "PaperSize-PrintType" (e.g., "Letter-BW", "A4-Color")
    /// Value: Price per unit (per page/copy)
    /// </summary>
    private static readonly Dictionary<string, decimal> PrintingPriceMatrix = new()
    {
        // Letter Size
        { "Letter-BW", 0.50m },        // ₱0.50 per page (B&W)
        { "Letter-Color", 1.00m },     // ₱1.00 per page (Color)

        // A4 Size
        { "A4-BW", 0.50m },            // ₱0.50 per page (B&W)
        { "A4-Color", 1.00m },         // ₱1.00 per page (Color)

        // Legal Size
        { "Legal-BW", 0.75m },         // ₱0.75 per page (B&W)
        { "Legal-Color", 1.50m }       // ₱1.50 per page (Color)
    };

    /// <summary>
    /// Available paper sizes for printing
    /// </summary>
    public static readonly string[] AvailablePaperSizes = { "Letter", "A4", "Legal" };

    /// <summary>
    /// Available print types
    /// </summary>
    public static readonly string[] AvailablePrintTypes = { "BW", "Color" };

    /// <summary>
    /// Calculates printing service price
    /// </summary>
    /// <param name="paperSize">Paper size: "Letter", "A4", or "Legal"</param>
    /// <param name="printType">Print type: "BW" (Black & White) or "Color"</param>
    /// <param name="quantity">Number of pages/copies</param>
    /// <returns>PrintingOption with calculated total price</returns>
    public static PrintingOption CalculatePrintingPrice(string paperSize, string printType, int quantity)
    {
        var key = $"{paperSize}-{printType}";

        if (!PrintingPriceMatrix.TryGetValue(key, out var pricePerUnit))
            throw new ArgumentException($"Invalid combination: {paperSize} + {printType}");

        return new PrintingOption
        {
            PaperSize = paperSize,
            PrintType = printType,
            Quantity = quantity,
            PricePerUnit = pricePerUnit
        };
    }

    /// <summary>
    /// Gets the price per unit for a specific paper size and print type combination
    /// </summary>
    public static decimal GetPrintingPricePerUnit(string paperSize, string printType)
    {
        var key = $"{paperSize}-{printType}";
        return PrintingPriceMatrix.TryGetValue(key, out var price) ? price : 0m;
    }

    /// <summary>
    /// Gets all available printing options with their prices
    /// </summary>
    public static List<(string PaperSize, string PrintType, decimal PricePerUnit)> GetAllPrintingOptions()
    {
        var options = new List<(string, string, decimal)>();

        foreach (var size in AvailablePaperSizes)
        {
            foreach (var type in AvailablePrintTypes)
            {
                var key = $"{size}-{type}";
                if (PrintingPriceMatrix.TryGetValue(key, out var price))
                {
                    options.Add((size, type, price));
                }
            }
        }

        return options;
    }

    /// <summary>
    /// Calculates labor cost for computer/printer repairs
    /// Uses a base rate plus multiplier based on complexity
    /// </summary>
    /// <param name="repairType">Type of repair: "ComputerRepair" or "PrinterRepair"</param>
    /// <param name="laborMinutes">Time spent on repair in minutes</param>
    /// <param name="complexityFactor">Multiplier based on difficulty (1.0 = normal, 1.5 = complex, 2.0 = very complex)</param>
    /// <returns>Total labor cost</returns>
    public static decimal CalculateRepairCost(string repairType, int laborMinutes, decimal complexityFactor = 1.0m)
    {
        // Base hourly rates by repair type
        var baseHourlyRate = repairType switch
        {
            "ComputerRepair" => 500m,    // ₱500/hour base rate
            "PrinterRepair" => 400m,     // ₱400/hour base rate
            _ => throw new ArgumentException($"Unknown repair type: {repairType}")
        };

        // Calculate cost with minimum charge of 0.5 hours
        var hours = Math.Max(0.5m, laborMinutes / 60m);
        return baseHourlyRate * hours * complexityFactor;
    }

    /// <summary>
    /// Applies a discount to a price (percentage-based)
    /// </summary>
    /// <param name="originalPrice">Original price</param>
    /// <param name="discountPercentage">Discount as percentage (e.g., 10 for 10%)</param>
    /// <returns>Final price after discount</returns>
    public static decimal ApplyDiscount(decimal originalPrice, decimal discountPercentage)
    {
        var discountAmount = (originalPrice * discountPercentage) / 100m;
        return Math.Max(0, originalPrice - discountAmount);
    }

    /// <summary>
    /// Looks up product price from inventory
    /// </summary>
    public static decimal? LookupProductPrice(string barcode)
    {
        var item = InventoryHelper.GetInventoryItemByBarcode(barcode);
        return item?.UnitPrice;
    }

    /// <summary>
    /// Updates printing prices (useful for price adjustments)
    /// </summary>
    public static void UpdatePrintingPrice(string paperSize, string printType, decimal newPrice)
    {
        var key = $"{paperSize}-{printType}";
        if (PrintingPriceMatrix.ContainsKey(key))
        {
            PrintingPriceMatrix[key] = newPrice;
        }
    }
}
