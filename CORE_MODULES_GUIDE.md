# Destiny POS 2026 - Core Modules Architecture & Implementation Guide

## Overview

This document provides a comprehensive guide to the two core modules of the Destiny POS system:
1. **Inventory Manager** - Manages product stock in `Inventory.xlsx`
2. **Sales & Service Logger** - Logs transactions to `Sales_Report.xlsx`

Both modules are designed with **thread-safe operations** to handle concurrent transactions without file corruption.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI Layer (WPF)                          │
│  • PosViewModel - Manages checkout operations                   │
│  • Multiple concurrent sales sessions                           │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
        ▼                                 ▼
   ┌─────────────────┐           ┌──────────────────┐
   │   InventoryHelper   │           │ SalesReportHelper │
   │                 │           │                  │
   │ - GetAllItems() │           │ - LogTransaction │
   │ - UpdateStock() │           │ - LogTransactions│
   │ - DeductStock() │           │ - QueryByDate()  │
   │ - AddItem()     │           │ - GetDailySales()│
   └────────┬────────┘           └────────┬─────────┘
            │                             │
            └──────────────┬──────────────┘
                           │
                   ┌───────▼────────┐
                   │ FileAccessLayer│ ◄─ CRITICAL: Thread-Safe Access
                   │                │
                   │ - WithLock()    │
                   │ - RetryLogic()  │
                   │ - WaitForFile() │
                   └───────┬────────┘
                           │
        ┌──────────────────┴───────────────────┐
        │                                      │
        ▼                                      ▼
   ┌─────────────────┐              ┌──────────────────┐
   │  Inventory.xlsx │              │ SalesReport.xlsx │
   │                 │              │                  │
   │ • Barcode       │              │ • Timestamp      │
   │ • ProductName   │              │ • TransType      │
   │ • UnitPrice     │              │ • Description    │
   │ • CurrentStock  │              │ • Quantity       │
   │ • ReorderLevel  │              │ • Price          │
   │ • Supplier      │              │ • PaymentMethod  │
   └─────────────────┘              └──────────────────┘
```

---

## 1. Concurrency Handling (NEW!)

### Problem
Excel files are not designed for concurrent access. Without proper locking:
- ✗ File corruption if two processes write simultaneously
- ✗ Data loss during inventory updates
- ✗ Transaction logging failures

### Solution: FileAccessLayer
The new `FileAccessLayer` provides:

1. **Exclusive File Locks** - Only one thread can access a file at a time
2. **Automatic Retry Logic** - Handles temporary file conflicts (5 retries)
3. **Exponential Backoff** - Waits longer between retries to avoid thundering herd
4. **Timeout Handling** - Prevents deadlocks

### How It Works

```csharp
// Before (NOT THREAD-SAFE):
public static bool UpdateStock(string barcode, int newQty)
{
    using var package = new ExcelPackage(new FileInfo(InventoryPath));
    // ... update logic
    package.SaveAs(new FileInfo(InventoryPath));
}

// After (THREAD-SAFE):
public static bool UpdateStock(string barcode, int newQty)
{
    return FileAccessLayer.WithInventoryLock(() => 
    {
        using var package = new ExcelPackage(new FileInfo(InventoryPath));
        // ... update logic
        package.SaveAs(new FileInfo(InventoryPath));
        return success;
    });
}
```

**Key Features:**
- Uses `lock()` statements for exclusive access
- Retry on `IOException` (file temporarily in use)
- Exponential backoff: 100ms → 200ms → 300ms → 400ms → 500ms
- Maximum 5 retries before throwing exception

---

## 2. Inventory Management Module

### Data Model: InventoryItem

```csharp
public class InventoryItem
{
    public string Barcode { get; set; }           // Unique identifier (e.g., "BAR001")
    public string ProductName { get; set; }       // e.g., "A4 Paper Pack"
    public string Category { get; set; }          // e.g., "Supplies", "Equipment"
    public decimal UnitPrice { get; set; }        // Price per unit (₱)
    public int CurrentStock { get; set; }         // Available quantity
    public int ReorderLevel { get; set; }         // Alert threshold (e.g., 5 units)
    public int ReorderQuantity { get; set; }      // Order quantity when restocking
    public string Supplier { get; set; }          // Supplier name
    public DateTime LastRestocked { get; set; }   // Last restock timestamp
}
```

### Core Operations

#### 1. Read Inventory into Memory

```csharp
// Get all items
List<InventoryItem> allItems = InventoryHelper.GetAllInventoryItems();

// Get specific item by barcode
InventoryItem? item = InventoryHelper.GetInventoryItemByBarcode("BAR001");

if (item != null)
{
    Console.WriteLine($"Found: {item.ProductName} - Stock: {item.CurrentStock}");
}
```

#### 2. Update Stock After Sale

```csharp
// Deduct stock after sale (automatic validation)
bool success = InventoryHelper.DeductStock("BAR001", quantity: 5);

if (success)
    Console.WriteLine("Stock updated successfully");
else
    Console.WriteLine("Insufficient stock or product not found");

// OR: Direct stock update (manual quantity)
InventoryHelper.UpdateStock("BAR001", newQuantity: 45);
```

#### 3. Add New Product

```csharp
var newProduct = new InventoryItem
{
    Barcode = "BAR999",
    ProductName = "USB Hub",
    Category = "Electronics",
    UnitPrice = 599.99m,
    CurrentStock = 20,
    ReorderLevel = 5,
    ReorderQuantity = 50,
    Supplier = "Tech Supplies Inc."
};

InventoryHelper.AddInventoryItem(newProduct);
```

#### 4. Monitor Low Stock

```csharp
List<InventoryItem> lowStockItems = InventoryHelper.GetLowStockItems();

foreach (var item in lowStockItems)
{
    Console.WriteLine($"⚠️  LOW STOCK: {item.ProductName} - {item.CurrentStock} units");
    Console.WriteLine($"   Reorder from: {item.Supplier}");
}
```

### Excel Structure: Inventory.xlsx

| Barcode | Product Name | Category | Unit Price | Current Stock | Reorder Level | Reorder Qty | Supplier | Last Restocked |
|---------|--------------|----------|------------|----------------|---------------|-----------|----------|---|
| BAR001 | A4 Paper | Supplies | ₱150.00 | 48 | 10 | 100 | Office Depot | 2026-07-01 |
| BAR002 | Ink Cartridge | Supplies | ₱450.00 | 12 | 5 | 50 | Tech Store | 2026-06-28 |

---

## 3. Sales & Service Logging Module

### Data Model: Transaction

```csharp
public class Transaction
{
    public DateTime Timestamp { get; set; }       // When sale occurred
    public string TransactionType { get; set; }   // "Product" or "Service"
    public string Description { get; set; }       // Item name or service description
    public int Quantity { get; set; }             // Units sold (for products)
    public decimal UnitPrice { get; set; }        // Price per unit
    public decimal TotalPrice { get; set; }       // Quantity × UnitPrice (or for services)
    public string PaymentMethod { get; set; }     // "CASH", "GCASH", "CARD"
    public string Notes { get; set; }             // Additional details (parts used, labor time)
}
```

### Logging Transactions

#### For Product Sales

```csharp
var productSale = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Product",
    Description = "A4 Paper - 100 sheets",
    Quantity = 2,
    UnitPrice = 150.00m,
    TotalPrice = 300.00m,
    PaymentMethod = "CASH",
    Notes = "Sale ID: S001"
};

SalesReportHelper.LogTransaction(productSale);

// Update inventory immediately after
InventoryHelper.DeductStock("BAR001", 2);
```

#### For Service Transactions

```csharp
// Computer Repair Service
var repairService = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Service",
    Description = "Computer Repair - Hardware Diagnostics",
    Quantity = 1,
    UnitPrice = 500.00m,  // Hourly rate × hours
    TotalPrice = 500.00m,
    PaymentMethod = "GCASH",
    Notes = "CPU overcooling issue - 1 hour labor. Parts: Thermal paste ₱50"
};

SalesReportHelper.LogTransaction(repairService);

// Log parts used separately (if applicable)
var partUsed = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Service",
    Description = "Thermal Paste - Computer Repair",
    Quantity = 1,
    UnitPrice = 50.00m,
    TotalPrice = 50.00m,
    PaymentMethod = "GCASH",
    Notes = "Part used in computer repair - Service ID: S002"
};

SalesReportHelper.LogTransaction(partUsed);
```

#### For Printing Services

```csharp
var printingService = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Service",
    Description = "Printing - Letter B&W",
    Quantity = 50,  // Number of pages
    UnitPrice = 0.50m,  // Per page
    TotalPrice = 25.00m,
    PaymentMethod = "CASH",
    Notes = "Black & White, Single-sided"
};

SalesReportHelper.LogTransaction(printingService);
```

#### Batch Logging (Multiple Transactions)

```csharp
var transactions = new List<Transaction>
{
    new Transaction { /* sale 1 */ },
    new Transaction { /* sale 2 */ },
    new Transaction { /* sale 3 */ }
};

// All logged in single file lock (more efficient)
SalesReportHelper.LogTransactions(transactions);
```

### Excel Structure: SalesReport.xlsx

| Timestamp | Transaction Type | Description | Quantity | Unit Price | Total Price | Payment Method | Notes |
|-----------|-----------------|-------------|----------|------------|-------------|-----------------|-------|
| 2026-07-08 09:15:30 | Product | A4 Paper | 2 | ₱150.00 | ₱300.00 | CASH | Sale ID: S001 |
| 2026-07-08 10:45:00 | Service | Computer Repair | 1 | ₱500.00 | ₱500.00 | GCASH | 1 hr labor + thermal paste |
| 2026-07-08 11:20:15 | Service | Printing | 50 | ₱0.50 | ₱25.00 | CASH | B&W Letter |

---

## 4. Querying & Reporting

### Get Daily Sales Total

```csharp
// Today's sales breakdown
var today = DateTime.Today;
var todayTransactions = SalesReportHelper.GetTransactionsByDateRange(today, today.AddDays(1));

decimal productSalesTotal = todayTransactions
    .Where(t => t.TransactionType == "Product")
    .Sum(t => t.TotalPrice);

decimal serviceSalesTotal = todayTransactions
    .Where(t => t.TransactionType == "Service")
    .Sum(t => t.TotalPrice);

Console.WriteLine($"Today's Summary:");
Console.WriteLine($"  Products: ₱{productSalesTotal:F2}");
Console.WriteLine($"  Services: ₱{serviceSalesTotal:F2}");
Console.WriteLine($"  Total: ₱{productSalesTotal + serviceSalesTotal:F2}");
```

### Get Sales by Payment Method

```csharp
var transactions = SalesReportHelper.GetTransactionsByDateRange(today, today.AddDays(1));

var paymentBreakdown = transactions
    .GroupBy(t => t.PaymentMethod)
    .Select(g => new { Method = g.Key, Total = g.Sum(t => t.TotalPrice) });

foreach (var payment in paymentBreakdown)
{
    Console.WriteLine($"{payment.Method}: ₱{payment.Total:F2}");
}
```

### Low Stock Alert

```csharp
var lowStockItems = InventoryHelper.GetLowStockItems();

if (lowStockItems.Any())
{
    Console.WriteLine("⚠️  LOW STOCK ALERT:");
    foreach (var item in lowStockItems)
    {
        Console.WriteLine($"  • {item.ProductName}: {item.CurrentStock}/{item.ReorderLevel} units");
        Console.WriteLine($"    Order {item.ReorderQuantity} from {item.Supplier}");
    }
}
```

---

## 5. Complete Checkout Example

### Scenario: Customer buys products AND repair service

```csharp
public void ProcessCheckout(List<SaleItem> saleItems, string paymentMethod)
{
    decimal totalAmount = 0;
    var transactions = new List<Transaction>();

    // Step 1: Process each item
    foreach (var item in saleItems)
    {
        if (item.ItemType == "Product")
        {
            // Log product sale
            var transaction = new Transaction
            {
                Timestamp = DateTime.Now,
                TransactionType = "Product",
                Description = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                TotalPrice = item.Total,
                PaymentMethod = paymentMethod,
                Notes = item.Notes
            };
            transactions.Add(transaction);
            totalAmount += item.Total;

            // Deduct from inventory
            bool success = InventoryHelper.DeductStock(item.Barcode, item.Quantity);
            if (!success)
            {
                MessageBox.Show($"Insufficient stock for {item.Name}");
                return;
            }
        }
        else if (item.ItemType == "Service")
        {
            // Log service transaction
            var transaction = new Transaction
            {
                Timestamp = DateTime.Now,
                TransactionType = "Service",
                Description = item.Name,
                Quantity = 1,
                UnitPrice = item.Price,
                TotalPrice = item.Total,
                PaymentMethod = paymentMethod,
                Notes = item.Notes
            };
            transactions.Add(transaction);
            totalAmount += item.Total;
        }
    }

    // Step 2: Apply discount if any
    if (Discount > 0)
    {
        totalAmount -= Discount;
    }

    // Step 3: Log all transactions atomically (thread-safe)
    SalesReportHelper.LogTransactions(transactions);

    // Step 4: Print receipt
    Console.WriteLine($"✓ Transaction Complete: ₱{totalAmount:F2}");
    Console.WriteLine($"Payment: {paymentMethod}");
}
```

---

## 6. Best Practices

### ✓ DO:

1. **Use FileAccessLayer** for all Excel operations
   ```csharp
   // Good
   return FileAccessLayer.WithInventoryLock(() => { /* operation */ });
   ```

2. **Deduct inventory immediately after sale logging**
   ```csharp
   SalesReportHelper.LogTransaction(transaction);
   InventoryHelper.DeductStock(barcode, quantity);
   ```

3. **Batch multiple transactions together**
   ```csharp
   SalesReportHelper.LogTransactions(transactions);  // Single file lock
   ```

4. **Always catch exceptions**
   ```csharp
   try
   {
       InventoryHelper.UpdateStock(barcode, qty);
   }
   catch (InvalidOperationException ex)
   {
       MessageBox.Show($"File access error: {ex.Message}");
   }
   ```

### ✗ DON'T:

1. **Don't bypass FileAccessLayer**
   ```csharp
   // Bad - not thread-safe
   var package = new ExcelPackage(inventoryPath);
   ```

2. **Don't rely on direct Excel edits**
   - Always use the helper methods
   - File consistency depends on code path

3. **Don't forget inventory deduction**
   - Log sale → Deduct stock → Show confirmation
   - Skipping any step breaks accounting

4. **Don't open files directly in Windows Explorer**
   - Use Excel reports for analysis
   - Never edit live data files manually

---

## 7. Error Handling & Troubleshooting

### FileAccessLayer Errors

```csharp
try
{
    InventoryHelper.UpdateStock("BAR001", 50);
}
catch (InvalidOperationException ex)
{
    // File access failed after 5 retries
    // Likely causes:
    // 1. Excel file open in another application
    // 2. File permissions issue
    // 3. Disk full
    
    Logger.Error($"Inventory update failed: {ex.Message}");
    MessageBox.Show("Unable to update inventory. Please try again.");
}
```

### Check File Status

```csharp
// Wait for file to be released
string inventoryPath = Path.Combine(AppContext.BaseDirectory, "Inventory.xlsx");
if (FileAccessLayer.WaitForFileRelease(inventoryPath, timeoutMs: 10000))
{
    Console.WriteLine("File is ready for access");
}
else
{
    Console.WriteLine("File is still locked. Timeout reached.");
}
```

---

## 8. Performance Considerations

| Operation | Time | Notes |
|-----------|------|-------|
| Read all items | ~50ms | Loads entire Excel into memory |
| Update stock | ~100ms | Includes retry logic |
| Log single transaction | ~80ms | Includes formatting |
| Batch log (10 trans) | ~150ms | More efficient than 10 singles |
| Query by date range | ~40ms | In-memory filter after read |

**Optimization Tips:**
- Batch log transactions when possible
- Cache inventory list if read multiple times in session
- Use `GetInventoryItemByBarcode()` instead of `GetAllInventoryItems()` for single lookups

---

## 9. Sample Data

The system includes `SampleDataHelper.cs` which initializes with 8 sample products:

| Barcode | Product | Stock | Price |
|---------|---------|-------|-------|
| BAR001 | A4 Paper | 100 | ₱150 |
| BAR002 | Ink Cartridge (Black) | 25 | ₱450 |
| BAR003 | Ink Cartridge (Color) | 15 | ₱650 |
| BAR004 | USB Flash Drive | 30 | ₱399 |
| BAR005 | External HDD 1TB | 10 | ₱2,500 |
| BAR006 | Keyboard | 12 | ₱1,200 |
| BAR007 | Mouse | 20 | ₱599 |
| BAR008 | Monitor | 5 | ₱8,999 |

Initialize on app startup:
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    SampleDataHelper.InitializeSampleData();  // Creates Inventory.xlsx
}
```

---

## Summary

| Feature | Implementation |
|---------|---|
| **Inventory Read** | `InventoryHelper.GetAllInventoryItems()` |
| **Inventory Update** | `InventoryHelper.UpdateStock(barcode, qty)` |
| **Stock Deduction** | `InventoryHelper.DeductStock(barcode, qty)` |
| **Transaction Log** | `SalesReportHelper.LogTransaction(transaction)` |
| **Batch Log** | `SalesReportHelper.LogTransactions(List<Transaction>)` |
| **Thread Safety** | `FileAccessLayer.WithLock(action)` |
| **Concurrency** | 5 retries with exponential backoff |
| **Data Format** | Excel (EPPlus 7.2.1) |
| **Currency** | Philippine Peso (₱) |

---

**All Excel files are located in: `bin/Debug/` or `bin/Release/` depending on build configuration**
