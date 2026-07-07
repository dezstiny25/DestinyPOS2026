# Destiny POS 2026 - Refactored System Documentation

## Overview

The refactored POS system now supports two primary sales categories:
- **Product Sales** (Inventory-based) with automatic stock tracking
- **Services** (Labor-based) including Printing and Repairs with dynamic pricing

## New Features

### 1. Inventory Management (`InventoryHelper`)

**File**: `Inventory.xlsx` in the application's base directory

#### Capabilities:
- Read/Write inventory data from Excel
- Track stock counts per product
- Automatic low stock alerts
- Update stock on successful sales
- Supplier and reorder information

#### Key Properties per Item:
- **Barcode**: Unique product identifier
- **Product Name**: Item description
- **Category**: Type of product (Supplies, Products, Accessories)
- **Unit Price**: ₱ selling price
- **Current Stock**: Current quantity in inventory
- **Reorder Level**: Minimum threshold before alert
- **Reorder Quantity**: How many to order when reordering
- **Supplier**: Supplier name/contact
- **Last Restocked**: Date of last inventory update

#### Usage in Code:

```csharp
// Initialize the inventory file
InventoryHelper.InitializeInventoryFile();

// Get all inventory items
var items = InventoryHelper.GetAllInventoryItems();

// Find item by barcode
var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");

// Get low stock items
var lowStockItems = InventoryHelper.GetLowStockItems();

// Update stock (automatic on sale)
InventoryHelper.UpdateStock("BAR001", newQuantity);

// Deduct stock (called during checkout)
bool success = InventoryHelper.DeductStock("BAR001", quantitySold);

// Add new item
var newItem = new InventoryItem { ... };
InventoryHelper.AddInventoryItem(newItem);
```

### 2. Sales Logging (`SalesReportHelper`)

**File**: `SalesReport.xlsx` in the application's base directory

#### Capabilities:
- Log all transactions (Products and Services)
- Track payment methods
- Generate daily summaries
- Analyze sales by transaction type
- Analyze sales by payment method

#### Fields per Transaction:
- **Timestamp**: Date and time of transaction
- **Transaction Type**: "Product" or "Service"
- **Description**: Item name or service description
- **Quantity**: Number of units sold
- **Unit Price**: Price per unit
- **Total Price**: Quantity × Unit Price
- **Payment Method**: CASH, GCASH, CARD
- **Notes**: Additional information (for services)

#### Usage in Code:

```csharp
// Initialize sales report file
SalesReportHelper.InitializeSalesReportFile();

// Log a single transaction
var transaction = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Product",
    Description = "A4 Paper",
    Quantity = 1,
    UnitPrice = 150m,
    TotalPrice = 150m,
    PaymentMethod = "CASH"
};
SalesReportHelper.LogTransaction(transaction);

// Log multiple transactions (batch)
var transactions = new List<Transaction> { ... };
SalesReportHelper.LogTransactions(transactions);

// Get daily totals
decimal dailyTotal = SalesReportHelper.GetDailySalesTotal(DateTime.Today);

// Get sales breakdown by type
var breakdown = SalesReportHelper.GetSalesBreakdown(DateTime.Today);
// Returns: { "Product": 5000m, "Service": 2500m }

// Get payment method breakdown
var paymentBreakdown = SalesReportHelper.GetPaymentMethodBreakdown(DateTime.Today);
// Returns: { "CASH": 5000m, "GCASH": 2000m, "CARD": 500m }

// Get transactions for a date range
var transactions = SalesReportHelper.GetTransactionsByDateRange(startDate, endDate);
```

### 3. Dynamic Pricing Logic (`PricingHelper`)

#### A. Printing Services

**Selection Matrix**: Paper Size × Print Type

| Paper Size | B&W | Color |
|-----------|-----|-------|
| Letter    | ₱0.50 | ₱1.00 |
| A4        | ₱0.50 | ₱1.00 |
| Legal     | ₱0.75 | ₱1.50 |

#### Usage:

```csharp
// Calculate printing price
var printOption = PricingHelper.CalculatePrintingPrice("Letter", "BW", 100);
// Returns: PrintingOption with TotalPrice = ₱50.00 (0.50 × 100)

// Get price per unit
var pricePerUnit = PricingHelper.GetPrintingPricePerUnit("A4", "Color");
// Returns: 1.00m

// Get all printing options
var options = PricingHelper.GetAllPrintingOptions();
```

#### B. Repair Services (Computer & Printer)

**Base Rates**:
- Computer Repair: ₱500/hour
- Printer Repair: ₱400/hour
- Minimum charge: 0.5 hours (₱250 for computer, ₱200 for printer)
- Complexity multipliers: 1.0x (normal), 1.5x (complex), 2.0x (very complex)

#### Usage:

```csharp
// Calculate repair cost
// Computer repair: 90 minutes, 1.5x complexity (complex job)
var cost = PricingHelper.CalculateRepairCost("ComputerRepair", 90, 1.5m);
// Calculation: (₱500/hr) × 1.5 hours × 1.5 = ₱1,125

// Printer repair: 30 minutes (minimum 30 min charge = 0.5 hr), normal complexity
var cost = PricingHelper.CalculateRepairCost("PrinterRepair", 30, 1.0m);
// Calculation: (₱400/hr) × 0.5 hours × 1.0 = ₱200
```

#### C. Product Price Lookup

```csharp
// Get product price from inventory by barcode
var price = PricingHelper.LookupProductPrice("BAR001");
// Returns: 150m (or null if not found)
```

#### D. Discount Application

```csharp
// Apply percentage discount
var originalPrice = 1000m;
var discountedPrice = PricingHelper.ApplyDiscount(originalPrice, 10); // 10% discount
// Result: ₱900m
```

### 4. Enhanced PosViewModel

#### Adding Product Items:

```csharp
// Automatically handles both Inventory.xlsx and legacy database
// Barcodes are scanned and the system:
// 1. Checks Inventory.xlsx first
// 2. Falls back to database if not found
// 3. Prevents overselling with stock checks
// 4. Alerts on low stock situations
```

#### Adding Printing Services:

```csharp
// From UI, user selects:
// - Paper Size: Letter, A4, or Legal
// - Print Type: BW or Color
// - Quantity: Number of pages

posViewModel.AddPrintingService("Letter", "BW", 100);
// Adds a printing service to the sale
```

#### Adding Repair Services:

```csharp
// For repairs, calculate cost first
var laborCost = PricingHelper.CalculateRepairCost("ComputerRepair", 120, 1.5m);

// Then add to sale
posViewModel.AddRepairService("ComputerRepair", 120, 1.5m, laborCost);
```

#### Completing a Sale:

```csharp
// Upon checkout:
// 1. All products are deducted from inventory
// 2. All transactions logged to SalesReport.xlsx
// 3. All sales logged to database (for backward compatibility)
// 4. UI displays confirmation with total and payment method
```

## Initialization Guide

### First-Time Setup:

```csharp
// In App.xaml.cs or MainViewModel constructor:
InventoryHelper.InitializeInventoryFile();
SalesReportHelper.InitializeSalesReportFile();
SampleDataHelper.InitializeSampleData(); // Optional: adds sample products

DatabaseHelper.InitializeDatabase();
```

### Populating Sample Data:

Run this once to populate test inventory:

```csharp
SampleDataHelper.InitializeSampleData();
```

This creates sample products including:
- A4 Paper (500 sheets)
- Ink Cartridges (Black & Color)
- USB Flash Drives
- External Hard Drives
- Keyboards & Mice
- HDMI Cables

## File Locations

All Excel files are created in the application's base directory:
- **Inventory.xlsx**: Product inventory database
- **SalesReport.xlsx**: Transaction log with daily summaries

To customize locations, modify the path variables in:
- `InventoryHelper.cs`: Line ~16 `InventoryPath`
- `SalesReportHelper.cs`: Line ~16 `SalesReportPath`

## Backward Compatibility

The refactored system maintains backward compatibility with the existing SQLite database:
- **destinypos.db**: Still used for legacy data and session information
- All database operations continue to work alongside the new Excel-based system
- This allows for gradual migration of existing data

## Error Handling

### Common Scenarios:

```csharp
// Check for low stock before adding
var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");
if (item?.CurrentStock <= item?.ReorderLevel)
{
    // Show reorder notification
}

// Verify sufficient stock before deducting
if (!InventoryHelper.DeductStock(barcode, quantity))
{
    MessageBox.Show("Insufficient stock to complete sale");
}

// Validate pricing parameters
try 
{
    var option = PricingHelper.CalculatePrintingPrice(paperSize, type, qty);
}
catch (ArgumentException ex)
{
    MessageBox.Show($"Invalid printing option: {ex.Message}");
}
```

## Performance Considerations

- Excel files are read/written each time (consider caching for high-volume operations)
- For heavy usage, consider migrating to a database backend
- EPPlus Non-Commercial License is used (free for non-commercial use)

## Data Export

Use Excel's built-in features to:
- Create pivot tables from sales data
- Generate charts for analysis
- Export to other formats (PDF, CSV)
- Share reports with management

## Future Enhancements

Potential improvements:
- Real-time dashboard from SalesReport.xlsx
- Inventory forecasting based on sales trends
- Multi-location inventory tracking
- Barcode generation for new products
- Service templates library
- Customer history tracking
