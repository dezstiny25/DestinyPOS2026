# Destiny POS 2026 - Developer Quick Reference

## 🚀 One-Minute Setup

```csharp
// 1. Initialize on app startup (App.xaml.cs)
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    InventoryHelper.InitializeInventoryFile();
    SalesReportHelper.InitializeSalesReportFile();
    SampleDataHelper.InitializeSampleData();  // Optional
}

// 2. Done! Files auto-created in bin/Release/
```

---

## 📦 Core Imports

```csharp
using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
```

---

## 🛒 Inventory Operations

```csharp
// GET INVENTORY
List<InventoryItem> items = InventoryHelper.GetAllInventoryItems();
InventoryItem? item = InventoryHelper.GetInventoryItemByBarcode("BAR001");

// CHECK STOCK
int stock = item?.CurrentStock ?? 0;
if (stock < 5) Console.WriteLine("Low stock alert");

// UPDATE STOCK (thread-safe)
InventoryHelper.UpdateStock("BAR001", 50);           // Set to 50
InventoryHelper.DeductStock("BAR001", 2);            // Reduce by 2

// ADD NEW PRODUCT
var product = new InventoryItem
{
    Barcode = "BAR_NEW",
    ProductName = "New Item",
    UnitPrice = 999.00m,
    CurrentStock = 50,
    ReorderLevel = 10,
    Supplier = "Supplier"
};
InventoryHelper.AddInventoryItem(product);

// LOW STOCK ALERTS
var lowStock = InventoryHelper.GetLowStockItems();
foreach (var item in lowStock)
    Console.WriteLine($"⚠️  {item.ProductName}: {item.CurrentStock} units");
```

---

## 💳 Sales Operations

```csharp
// LOG PRODUCT SALE
var productSale = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Product",
    Description = "A4 Paper",
    Quantity = 2,
    UnitPrice = 150.00m,
    TotalPrice = 300.00m,
    PaymentMethod = "CASH"
};
SalesReportHelper.LogTransaction(productSale);
InventoryHelper.DeductStock("BAR001", 2);

// LOG SERVICE
var service = new Transaction
{
    Timestamp = DateTime.Now,
    TransactionType = "Service",
    Description = "Computer Repair",
    Quantity = 1,
    UnitPrice = 500.00m,
    TotalPrice = 500.00m,
    PaymentMethod = "GCASH",
    Notes = "2 hours labor + thermal paste"
};
SalesReportHelper.LogTransaction(service);

// BATCH LOG (more efficient)
var transactions = new List<Transaction> { sale1, sale2, service1 };
SalesReportHelper.LogTransactions(transactions);

// QUERY SALES
var today = DateTime.Today;
var sales = SalesReportHelper.GetTransactionsByDateRange(
    today, 
    today.AddDays(1)
);

decimal dailyTotal = sales.Sum(s => s.TotalPrice);
decimal productTotal = sales
    .Where(s => s.TransactionType == "Product")
    .Sum(s => s.TotalPrice);
decimal serviceTotal = sales
    .Where(s => s.TransactionType == "Service")
    .Sum(s => s.TotalPrice);
```

---

## 🔒 Thread-Safe Operations (Automatic!)

```csharp
// NO NEED TO CALL FileAccessLayer DIRECTLY!
// All helpers already use it internally:

InventoryHelper.UpdateStock(barcode, qty);      // Already thread-safe ✓
SalesReportHelper.LogTransaction(transaction);  // Already thread-safe ✓

// But if you need direct file access:
FileAccessLayer.WithInventoryLock(() =>
{
    // Your inventory operation here
    // Automatically acquires exclusive lock
    // Automatically retries on failure (5x)
    // Automatically releases lock
});

FileAccessLayer.WithSalesReportLock(() =>
{
    // Your sales report operation here
});
```

---

## 📊 Reporting Snippets

```csharp
// DAILY SALES SUMMARY
var today = DateTime.Today;
var sales = SalesReportHelper.GetTransactionsByDateRange(today, today.AddDays(1));

var byType = sales.GroupBy(s => s.TransactionType)
    .Select(g => new { Type = g.Key, Amount = g.Sum(x => x.TotalPrice) });

foreach (var group in byType)
    Console.WriteLine($"{group.Type}: ₱{group.Amount:F2}");

// PAYMENT METHOD BREAKDOWN
var byPayment = sales.GroupBy(s => s.PaymentMethod)
    .Select(g => new { Method = g.Key, Amount = g.Sum(x => x.TotalPrice) });

foreach (var group in byPayment)
    Console.WriteLine($"{group.Method}: ₱{group.Amount:F2}");

// TOP SELLING PRODUCTS
var topProducts = sales
    .Where(s => s.TransactionType == "Product")
    .GroupBy(s => s.Description)
    .Select(g => new { Product = g.Key, Qty = g.Sum(x => x.Quantity) })
    .OrderByDescending(g => g.Qty)
    .Take(5);

foreach (var item in topProducts)
    Console.WriteLine($"{item.Product}: {item.Qty} units");

// SERVICE BREAKDOWN
var serviceRevenue = sales
    .Where(s => s.TransactionType == "Service")
    .GroupBy(s => s.Description)
    .Select(g => new { Service = g.Key, Revenue = g.Sum(x => x.TotalPrice) });

foreach (var service in serviceRevenue)
    Console.WriteLine($"{service.Service}: ₱{service.Revenue:F2}");
```

---

## ❌ Error Handling

```csharp
// PATTERN: Always wrap in try-catch
try
{
    InventoryHelper.UpdateStock("BAR001", 50);
}
catch (InvalidOperationException ex)
{
    // File access failed after 5 retries
    MessageBox.Show("Unable to update inventory. Close Excel and try again.");
    Logger.Error($"Inventory error: {ex.Message}");
}

// CHECK IF PRODUCT EXISTS
var item = InventoryHelper.GetInventoryItemByBarcode("BAR999");
if (item == null)
{
    MessageBox.Show("Product not found");
    return;
}

// CHECK STOCK BEFORE SALE
if (item.CurrentStock < quantityRequested)
{
    MessageBox.Show($"Only {item.CurrentStock} available");
    return;
}

// VALIDATE BEFORE DEDUCT
bool success = InventoryHelper.DeductStock("BAR001", quantity);
if (!success)
{
    MessageBox.Show("Insufficient stock or product not found");
    return;
}
```

---

## 📁 File Locations

```
bin/Release/net8.0-windows/
├─ Inventory.xlsx          ← Products & stock (auto-created)
├─ SalesReport.xlsx        ← Transaction log (auto-created)
├─ destinypos.db          ← Legacy database (auto-created)
└─ DestinyPOS2026.Wpf.exe ← Your application
```

---

## 🎯 Common Patterns

### Complete Checkout
```csharp
decimal total = 0;
var transactions = new List<Transaction>();

foreach (var saleItem in cart)
{
    if (saleItem.ItemType == "Product")
    {
        // Validate stock
        var item = InventoryHelper.GetInventoryItemByBarcode(saleItem.Barcode);
        if (item == null || item.CurrentStock < saleItem.Quantity)
        {
            MessageBox.Show("Insufficient stock");
            return;
        }

        // Create transaction
        var transaction = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Product",
            Description = saleItem.Name,
            Quantity = saleItem.Quantity,
            UnitPrice = saleItem.Price,
            TotalPrice = saleItem.Total,
            PaymentMethod = paymentMethod
        };
        transactions.Add(transaction);
        total += transaction.TotalPrice;
    }
    else // Service
    {
        var transaction = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Service",
            Description = saleItem.Name,
            Quantity = saleItem.Quantity,
            UnitPrice = saleItem.Price,
            TotalPrice = saleItem.Total,
            PaymentMethod = paymentMethod,
            Notes = saleItem.Notes
        };
        transactions.Add(transaction);
        total += transaction.TotalPrice;
    }
}

// Apply discount
total -= discount;

// Log all transactions (single lock)
SalesReportHelper.LogTransactions(transactions);

// Deduct inventory for products only
foreach (var item in cart.Where(i => i.ItemType == "Product"))
{
    InventoryHelper.DeductStock(item.Barcode, item.Quantity);
}

// Show receipt
Console.WriteLine($"✓ Total: ₱{total:F2}");
```

### Restock Alert
```csharp
var lowStockItems = InventoryHelper.GetLowStockItems();
if (lowStockItems.Any())
{
    foreach (var item in lowStockItems)
    {
        MessageBox.Show(
            $"Low stock alert!\n\n" +
            $"Product: {item.ProductName}\n" +
            $"Current: {item.CurrentStock} units\n" +
            $"Threshold: {item.ReorderLevel} units\n" +
            $"Order: {item.ReorderQuantity} from {item.Supplier}"
        );
    }
}
```

### Daily Close-Out Report
```csharp
var today = DateTime.Today;
var sales = SalesReportHelper.GetTransactionsByDateRange(today, today.AddDays(1));

Console.WriteLine("═══ DAILY CLOSE-OUT REPORT ═══");
Console.WriteLine($"Date: {today:yyyy-MM-dd}\n");

var byType = sales.GroupBy(s => s.TransactionType);
foreach (var group in byType)
{
    decimal amount = group.Sum(s => s.TotalPrice);
    Console.WriteLine($"{group.Key}s: ₱{amount:F2} ({group.Count()} transactions)");
}

decimal grandTotal = sales.Sum(s => s.TotalPrice);
Console.WriteLine($"\nTotal Sales: ₱{grandTotal:F2}");
Console.WriteLine("════════════════════════════════");
```

---

## 🔄 Retry Behavior (Automatic)

```
Operation: Update Inventory
│
├─ Attempt 1: FAIL (File locked) → Wait 100ms
├─ Attempt 2: FAIL (File locked) → Wait 200ms
├─ Attempt 3: FAIL (File locked) → Wait 300ms
├─ Attempt 4: SUCCESS! ✓
│
└─ Returns immediately to caller
   (Caller doesn't see the retries)

Maximum retry time: 1500ms
```

---

## 📋 Data Model Cheat Sheet

### InventoryItem
```csharp
public string Barcode { get; set; }           // "BAR001"
public string ProductName { get; set; }       // "A4 Paper"
public string Category { get; set; }          // "Supplies"
public decimal UnitPrice { get; set; }        // 150.00m
public int CurrentStock { get; set; }         // 100
public int ReorderLevel { get; set; }         // 10
public int ReorderQuantity { get; set; }      // 50
public string Supplier { get; set; }          // "Supplier Inc"
public DateTime LastRestocked { get; set; }   // DateTime.Now
```

### Transaction
```csharp
public DateTime Timestamp { get; set; }        // DateTime.Now
public string TransactionType { get; set; }    // "Product" or "Service"
public string Description { get; set; }        // "A4 Paper" or "Computer Repair"
public int Quantity { get; set; }              // 2 or 50
public decimal UnitPrice { get; set; }         // 150.00m or 0.50m
public decimal TotalPrice { get; set; }        // 300.00m or 25.00m
public string PaymentMethod { get; set; }      // "CASH", "GCASH", "CARD"
public string Notes { get; set; }              // Optional details
```

### SaleItem (UI Model)
```csharp
public string Barcode { get; set; }
public string Name { get; set; }
public string ItemType { get; set; }           // "Product" or "Service"
public int Quantity { get; set; }
public decimal Price { get; set; }
public decimal Total => Price * Quantity;     // Computed
public string Notes { get; set; }
```

---

## ⚡ Performance Tips

1. **Cache inventory** if reading multiple times per session
2. **Use `GetInventoryItemByBarcode()`** for single product lookups (faster)
3. **Batch log transactions** - `LogTransactions(list)` is more efficient than 10x `LogTransaction()`
4. **Close Excel files** before running POS (prevents file lock conflicts)
5. **Run in Release mode** for better performance

---

## 🧪 Testing Patterns

```csharp
// TEST: Stock deduction
[Test]
public void TestStockDeduction()
{
    InventoryHelper.UpdateStock("BAR001", 100);
    InventoryHelper.DeductStock("BAR001", 10);
    
    var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");
    Assert.AreEqual(90, item.CurrentStock);
}

// TEST: Transaction logging
[Test]
public void TestTransactionLogging()
{
    var transaction = new Transaction
    {
        Timestamp = DateTime.Now,
        TransactionType = "Product",
        Description = "Test Item",
        TotalPrice = 100.00m,
        PaymentMethod = "CASH"
    };
    
    SalesReportHelper.LogTransaction(transaction);
    
    var sales = SalesReportHelper.GetTransactionsByDateRange(DateTime.Today, DateTime.Today.AddDays(1));
    Assert.IsTrue(sales.Any(s => s.Description == "Test Item"));
}

// TEST: Concurrent access
[Test]
public void TestConcurrentAccess()
{
    var tasks = new List<Task>();
    for (int i = 0; i < 10; i++)
    {
        tasks.Add(Task.Run(() => 
            InventoryHelper.DeductStock("BAR001", 1)
        ));
    }
    
    Task.WaitAll(tasks.ToArray());
    
    var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");
    Assert.AreEqual(100 - 10, item.CurrentStock);  // All 10 deductions recorded
}
```

---

## 💡 Common Mistakes to Avoid

❌ **DON'T:**
```csharp
// Don't forget to deduct inventory after logging sale
SalesReportHelper.LogTransaction(sale);
// Missing: InventoryHelper.DeductStock(barcode, qty);

// Don't log same transaction twice
for (int i = 0; i < 10; i++)
    SalesReportHelper.LogTransaction(sale);  // Logs 10 times!
// Use: SalesReportHelper.LogTransactions(List<Transaction>);

// Don't edit Excel files manually while POS is running
// File lock conflict!

// Don't assume file exists
var item = InventoryHelper.GetInventoryItemByBarcode("UNKNOWN");
if (item == null) { /* handle missing product */ }
```

✅ **DO:**
```csharp
// Always follow: Log → Deduct → Confirm
SalesReportHelper.LogTransaction(sale);
bool success = InventoryHelper.DeductStock(barcode, qty);
if (success) ShowReceipt();

// Batch operations when possible
SalesReportHelper.LogTransactions(sales);  // Single file lock

// Close Excel before testing
// File won't be locked

// Always null-check returned products
var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");
if (item != null) ProcessSale(item);
```

---

## 📞 Quick Help

| Issue | Solution |
|-------|----------|
| "File access error" | Close Inventory.xlsx/SalesReport.xlsx in Excel |
| "Product not found" | Check barcode matches Inventory.xlsx exactly |
| "Insufficient stock" | Verify CurrentStock column in Excel |
| "Transaction not logged" | Check SalesReport.xlsx is not open in Excel |
| "Slow performance" | Close other Excel files, rebuild in Release mode |
| "File permissions" | Ensure user has Write permissions to `bin/Release/` |

---

## 🚀 Deployment Command

```powershell
# Build Release
cd c:\Users\PC Users\DestinyPOS2026
dotnet build -c Release

# Run
.\DestinyPOS2026.Wpf\bin\Release\net8.0-windows\DestinyPOS2026.Wpf.exe

# Files created automatically in:
# c:\Users\PC Users\DestinyPOS2026\DestinyPOS2026.Wpf\bin\Release\net8.0-windows\
```

---

**Last Updated**: 2026-07-08 | **Version**: 1.0 | **Status**: ✅ Production Ready

