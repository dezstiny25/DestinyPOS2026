# Quick Start Guide - Destiny POS 2026

## Setup (First Time Only)

1. **Build and Run the Application**
   ```
   dotnet build
   dotnet run
   ```

2. **Initialize Sample Data**
   - In `App.xaml.cs` or `MainViewModel.cs`, call once:
   ```csharp
   SampleDataHelper.InitializeSampleData();
   ```

   This creates:
   - `Inventory.xlsx` with 8 sample products
   - `SalesReport.xlsx` ready for transactions
   - `destinypos.db` for legacy compatibility

## Daily Operations

### Adding Product Sales

1. **Scan Barcode or Enter Manually**
   - System checks `Inventory.xlsx` first
   - Falls back to database if needed

2. **Stock Check**
   - System prevents selling more than available
   - Low stock alerts appear automatically

3. **Example Barcodes (from sample data)**:
   - `BAR001`: A4 Paper (₱150)
   - `BAR002`: Ink Cartridge Black (₱450)
   - `BAR003`: Ink Cartridge Color (₱650)
   - `BAR004`: USB Flash Drive (₱399)
   - `BAR005`: External HDD 1TB (₱2,500)

### Adding Printing Services

**To implement printing service UI:**

```csharp
// User selects: Paper Size, Type, Quantity
// Then call:
posViewModel.AddPrintingService("Letter", "BW", 100);

// Pricing:
// Letter BW: ₱0.50/page → 100 pages = ₱50.00
// A4 Color: ₱1.00/page → 50 pages = ₱50.00
// Legal Color: ₱1.50/page → 100 pages = ₱150.00
```

### Adding Repair Services

**To implement repair service dialog:**

```csharp
// User inputs: Repair type, minutes spent, complexity factor
// System calculates:
var cost = PricingHelper.CalculateRepairCost("ComputerRepair", 90, 1.5m);

// Then add to sale:
posViewModel.AddRepairService("ComputerRepair", 90, 1.5m, cost);

// Examples:
// Computer Repair, 60 min, 1.0x (normal) = ₱500.00
// Computer Repair, 90 min, 1.5x (complex) = ₱1,125.00
// Printer Repair, 30 min, 1.0x (normal) = ₱200.00 (minimum 0.5 hr)
```

### Checkout Process

1. **Review Sale Items**
   - Products with quantities
   - Services with details

2. **Apply Discount (Optional)**
   - Enter discount amount or percentage

3. **Select Payment Method**
   - Click: CASH, GCASH, or CARD

4. **System Automatically**:
   - Deducts inventory stock
   - Logs transaction to `SalesReport.xlsx`
   - Logs to database (legacy)
   - Displays confirmation

## Monitoring & Reporting

### Daily Sales Summary

```csharp
// Get total sales for today
decimal dailyTotal = SalesReportHelper.GetDailySalesTotal(DateTime.Today);

// Get breakdown by type
var breakdown = SalesReportHelper.GetSalesBreakdown(DateTime.Today);
// {"Product": 5000, "Service": 2500}

// Get breakdown by payment
var payments = SalesReportHelper.GetPaymentMethodBreakdown(DateTime.Today);
// {"CASH": 5000, "GCASH": 2000, "CARD": 500}

// Get all transactions for a date
var transactions = SalesReportHelper.GetTransactionsByDate(DateTime.Today);
```

### Inventory Alerts

```csharp
// Get items below reorder level
var lowStock = InventoryHelper.GetLowStockItems();

foreach (var item in lowStock)
{
    Console.WriteLine($"Low Stock: {item.ProductName}");
    Console.WriteLine($"Current: {item.CurrentStock}, Reorder Level: {item.ReorderLevel}");
    Console.WriteLine($"Reorder: {item.ReorderQuantity} units from {item.Supplier}");
}
```

## Managing Inventory

### Adding New Products

```csharp
var newProduct = new InventoryItem
{
    Barcode = "BAR009",
    ProductName = "Thermal Paper (80x80mm)",
    Category = "Supplies",
    UnitPrice = 299m,
    CurrentStock = 10,
    ReorderLevel = 3,
    ReorderQuantity = 5,
    Supplier = "Office Plus",
    LastRestocked = DateTime.Now
};

InventoryHelper.AddInventoryItem(newProduct);
```

### Updating Inventory Manually

```csharp
// Update stock after manual count
InventoryHelper.UpdateStock("BAR001", 25);

// Or update entire item details
var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");
item.UnitPrice = 160m; // Price increase
item.ReorderLevel = 10; // Adjust reorder threshold
InventoryHelper.UpdateInventoryItem(item);
```

## Pricing Reference

### Printing (Per Page/Copy)

| Size | B&W | Color |
|------|-----|-------|
| Letter | ₱0.50 | ₱1.00 |
| A4 | ₱0.50 | ₱1.00 |
| Legal | ₱0.75 | ₱1.50 |

### Repairs (Hourly, Min. 0.5 hrs)

| Service | Rate | Complexity |
|---------|------|-----------|
| Computer Repair | ₱500/hr | 1.0x-2.0x |
| Printer Repair | ₱400/hr | 1.0x-2.0x |

## Files Generated

### Application Folder
```
bin/
  Debug/
    DestinyPOS2026.Wpf.exe
    Inventory.xlsx          ← Product inventory
    SalesReport.xlsx        ← Transaction log
    destinypos.db          ← Database (legacy)
```

## Troubleshooting

### Issue: Barcode not found
- Check if it exists in `Inventory.xlsx`
- Verify barcode spelling
- Check database if using legacy products

### Issue: Stock shows incorrect
- Verify last stock count in `Inventory.xlsx`
- Check recent sales in `SalesReport.xlsx`
- Manual recount may be needed

### Issue: Pricing incorrect
- Check `PricingHelper` base rates
- Verify complexity factor for repairs
- Confirm paper size and type for printing

### Issue: Files not created
- Check folder permissions
- Ensure EPPlus license context is set
- Verify no file access conflicts

## Tips

✓ Scan barcodes quickly with barcode scanner
✓ Check low stock alerts before opening
✓ Use GCASH/CARD for card transactions (faster)
✓ Export SalesReport.xlsx to Excel for analysis
✓ Backup Inventory.xlsx weekly
✓ Review daily totals at shift end

## Support

For issues or questions, check:
- `REFACTORING_GUIDE.md` - Detailed technical documentation
- Code comments in helper classes
- `SampleDataHelper.cs` - Example data structure
