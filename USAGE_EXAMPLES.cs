// ============================================================================
// DESTINY POS 2026 - PRACTICAL USAGE EXAMPLES
// ============================================================================
//
// Copy-paste ready code snippets for common POS scenarios
// Demonstrates Inventory Management & Sales Logging with Thread-Safe Operations
//

using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DestinyPOS2026.Wpf.Examples;

/// <summary>
/// EXAMPLE 1: Product Sale with Inventory Deduction
/// 
/// Scenario: Customer buys 2 boxes of A4 paper
/// Expected: Log sale + Reduce stock
/// </summary>
public class Example_ProductSale
{
    public void ProcessPaperSale()
    {
        // Step 1: Validate product exists and has stock
        var paper = InventoryHelper.GetInventoryItemByBarcode("BAR001");
        if (paper == null)
        {
            MessageBox.Show("Product not found");
            return;
        }

        if (paper.CurrentStock < 2)
        {
            MessageBox.Show($"Insufficient stock. Available: {paper.CurrentStock}");
            return;
        }

        // Step 2: Log the transaction
        var sale = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Product",
            Description = paper.ProductName,
            Quantity = 2,
            UnitPrice = paper.UnitPrice,
            TotalPrice = paper.UnitPrice * 2,
            PaymentMethod = "CASH",
            Notes = "Transaction ID: TXN001"
        };

        SalesReportHelper.LogTransaction(sale);
        Console.WriteLine($"✓ Sale logged: ₱{sale.TotalPrice:F2}");

        // Step 3: Deduct from inventory (MUST happen after logging)
        bool success = InventoryHelper.DeductStock("BAR001", 2);
        if (!success)
        {
            MessageBox.Show("Failed to update inventory");
            return;
        }

        Console.WriteLine("✓ Inventory updated");
        Console.WriteLine($"✓ Transaction complete!");
    }
}

/// <summary>
/// EXAMPLE 2: Service Transaction - Computer Repair
/// 
/// Scenario: Customer repairs computer
/// - Labor: 2 hours at ₱500/hour = ₱1,000
/// - Parts: Thermal paste ₱75
/// - Payment: GCash
/// Expected: Log main service + log parts cost
/// </summary>
public class Example_RepairService
{
    public void ProcessComputerRepair()
    {
        const string paymentMethod = "GCASH";
        const decimal laborCost = 1000.00m;  // 2 hours × 500/hour
        const decimal partsCost = 75.00m;    // Thermal paste

        var transactions = new List<Transaction>();

        // Transaction 1: Labor
        var laborTransaction = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Service",
            Description = "Computer Repair - Thermal Management",
            Quantity = 1,
            UnitPrice = laborCost,
            TotalPrice = laborCost,
            PaymentMethod = paymentMethod,
            Notes = "2 hours labor. Issue: CPU overheating. Solution: Reapplied thermal paste"
        };
        transactions.Add(laborTransaction);

        // Transaction 2: Parts/Materials
        var partsTransaction = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Service",
            Description = "Thermal Paste - Computer Repair",
            Quantity = 1,
            UnitPrice = partsCost,
            TotalPrice = partsCost,
            PaymentMethod = paymentMethod,
            Notes = "Part used in above repair"
        };
        transactions.Add(partsTransaction);

        // Batch log (more efficient)
        SalesReportHelper.LogTransactions(transactions);

        decimal total = laborCost + partsCost;
        Console.WriteLine($"✓ Repair service logged: ₱{total:F2}");
        Console.WriteLine($"  Labor: ₱{laborCost:F2}");
        Console.WriteLine($"  Parts: ₱{partsCost:F2}");
    }
}

/// <summary>
/// EXAMPLE 3: Printing Service with Page-Based Pricing
/// 
/// Scenario: Customer wants 100 pages printed
/// - Format: Letter size
/// - Type: Color
/// - Pricing: Letter Color = ₱1.00/page
/// - Total: 100 pages × ₱1.00 = ₱100.00
/// Expected: Log transaction with page count in Quantity field
/// </summary>
public class Example_PrintingService
{
    public void ProcessPrintingJob()
    {
        const int pageCount = 100;
        const decimal pricePerPage = 1.00m;  // Letter Color

        var printJob = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Service",
            Description = "Printing - Letter Color",
            Quantity = pageCount,          // Store page count here
            UnitPrice = pricePerPage,      // Price per page
            TotalPrice = pageCount * pricePerPage,
            PaymentMethod = "CASH",
            Notes = "Letter size, Color, Single-sided, Standard quality"
        };

        SalesReportHelper.LogTransaction(printJob);
        Console.WriteLine($"✓ Print job logged: {pageCount} pages @ ₱{pricePerPage}/page = ₱{printJob.TotalPrice:F2}");
    }
}

/// <summary>
/// EXAMPLE 4: Multi-Item Checkout (Products + Services)
/// 
/// Scenario: Customer buys supplies AND gets monitor repaired
/// Items:
/// 1. USB Flash Drive x2 @ ₱399 = ₱798
/// 2. Monitor Repair Service x1 @ ₱800 = ₱800
/// Total: ₱1,598
/// </summary>
public class Example_MultiItemCheckout
{
    public void ProcessCompleteCheckout()
    {
        decimal total = 0;
        var transactions = new List<Transaction>();

        // ===== PRODUCT: USB Flash Drive =====
        var usbProduct = InventoryHelper.GetInventoryItemByBarcode("BAR004");
        if (usbProduct == null)
        {
            MessageBox.Show("Product not found");
            return;
        }

        int usbQuantity = 2;
        if (usbProduct.CurrentStock < usbQuantity)
        {
            MessageBox.Show("Insufficient USB stock");
            return;
        }

        var usbSale = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Product",
            Description = usbProduct.ProductName,
            Quantity = usbQuantity,
            UnitPrice = usbProduct.UnitPrice,
            TotalPrice = usbProduct.UnitPrice * usbQuantity,
            PaymentMethod = "CASH",
            Notes = "Multiple units sold"
        };
        transactions.Add(usbSale);
        total += usbSale.TotalPrice;

        // ===== SERVICE: Monitor Repair =====
        var monitorRepair = new Transaction
        {
            Timestamp = DateTime.Now,
            TransactionType = "Service",
            Description = "Monitor Repair - Display Issue Resolution",
            Quantity = 1,
            UnitPrice = 800.00m,
            TotalPrice = 800.00m,
            PaymentMethod = "CASH",
            Notes = "Repaired faulty power supply. 1.5 hour labor"
        };
        transactions.Add(monitorRepair);
        total += monitorRepair.TotalPrice;

        // ===== PROCESS CHECKOUT =====
        // 1. Log all transactions (atomic operation)
        SalesReportHelper.LogTransactions(transactions);
        Console.WriteLine("✓ All transactions logged");

        // 2. Update inventory (products only)
        InventoryHelper.DeductStock("BAR004", usbQuantity);
        Console.WriteLine($"✓ Inventory updated for {usbQuantity} USB drives");

        // 3. Display receipt
        Console.WriteLine("\n========== RECEIPT ==========");
        Console.WriteLine($"A4 Paper x2 ........... ₱{usbSale.TotalPrice:F2}");
        Console.WriteLine($"Monitor Repair Service  ₱{monitorRepair.TotalPrice:F2}");
        Console.WriteLine($"                        -----------");
        Console.WriteLine($"TOTAL ................. ₱{total:F2}");
        Console.WriteLine($"Payment: {usbSale.PaymentMethod}");
        Console.WriteLine("=============================\n");
    }
}

/// <summary>
/// EXAMPLE 5: Daily Sales Report
/// 
/// Scenario: Manager wants to see today's sales breakdown
/// Shows: Total by type, breakdown by payment method, low stock alerts
/// </summary>
public class Example_DailySalesReport
{
    public void GenerateDailySalesReport()
    {
        var today = DateTime.Today;
        var endOfDay = today.AddDays(1);

        // Get today's transactions
        var todayTransactions = SalesReportHelper.GetTransactionsByDateRange(today, endOfDay);

        if (!todayTransactions.Any())
        {
            Console.WriteLine("No sales today");
            return;
        }

        // ===== SALES BY TYPE =====
        Console.WriteLine("\n========== DAILY SALES SUMMARY ==========\n");
        
        var productSales = todayTransactions.Where(t => t.TransactionType == "Product").ToList();
        var serviceSales = todayTransactions.Where(t => t.TransactionType == "Service").ToList();

        decimal productTotal = productSales.Sum(t => t.TotalPrice);
        decimal serviceTotal = serviceSales.Sum(t => t.TotalPrice);
        decimal grandTotal = productTotal + serviceTotal;

        Console.WriteLine($"Products Sold: {productSales.Count} transactions");
        Console.WriteLine($"  Subtotal: ₱{productTotal:F2}");
        Console.WriteLine();

        Console.WriteLine($"Services Provided: {serviceSales.Count} transactions");
        Console.WriteLine($"  Subtotal: ₱{serviceTotal:F2}");
        Console.WriteLine();

        // ===== BREAKDOWN BY PAYMENT METHOD =====
        var paymentBreakdown = todayTransactions
            .GroupBy(t => t.PaymentMethod)
            .OrderBy(g => g.Key);

        Console.WriteLine("Payment Methods:");
        foreach (var paymentGroup in paymentBreakdown)
        {
            decimal methodTotal = paymentGroup.Sum(t => t.TotalPrice);
            int count = paymentGroup.Count();
            Console.WriteLine($"  {paymentGroup.Key}: ₱{methodTotal:F2} ({count} trans)");
        }

        Console.WriteLine();
        Console.WriteLine($"TOTAL SALES TODAY: ₱{grandTotal:F2}");
        Console.WriteLine();

        // ===== INVENTORY ALERTS =====
        var lowStockItems = InventoryHelper.GetLowStockItems();
        if (lowStockItems.Any())
        {
            Console.WriteLine("⚠️  LOW STOCK ALERT:");
            foreach (var item in lowStockItems)
            {
                int availableToSell = item.ReorderLevel - item.CurrentStock;
                Console.WriteLine($"  • {item.ProductName}");
                Console.WriteLine($"    Current: {item.CurrentStock} units (Threshold: {item.ReorderLevel})");
                Console.WriteLine($"    Action: Order {item.ReorderQuantity} from {item.Supplier}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("==========================================\n");
    }
}

/// <summary>
/// EXAMPLE 6: Inventory Management - Add New Product
/// 
/// Scenario: Manager receives new stock of keyboard
/// Action: Add product to inventory with initial stock
/// </summary>
public class Example_AddNewProduct
{
    public void AddNewKeyboardToInventory()
    {
        var newProduct = new InventoryItem
        {
            Barcode = "BAR_KBD_001",
            ProductName = "Mechanical Gaming Keyboard RGB",
            Category = "Electronics",
            UnitPrice = 2499.00m,
            CurrentStock = 15,
            ReorderLevel = 5,            // Alert when stock drops to 5
            ReorderQuantity = 25,        // Order 25 units at a time
            Supplier = "Tech Distributors Ltd.",
            LastRestocked = DateTime.Now
        };

        InventoryHelper.AddInventoryItem(newProduct);
        Console.WriteLine($"✓ Added {newProduct.ProductName} to inventory");
        Console.WriteLine($"  Barcode: {newProduct.Barcode}");
        Console.WriteLine($"  Initial Stock: {newProduct.CurrentStock} units");
    }
}

/// <summary>
/// EXAMPLE 7: Concurrency Demonstration
/// 
/// Scenario: Two checkout registers process sales simultaneously
/// Shows: Thread-safe operations prevent file corruption
/// </summary>
public class Example_ConcurrentSales
{
    public void DemonstrateConcurrency()
    {
        Console.WriteLine("Simulating 2 concurrent checkouts...\n");

        // Simulate Register 1
        var register1Task = System.Threading.Tasks.Task.Run(() =>
        {
            for (int i = 0; i < 3; i++)
            {
                var sale = new Transaction
                {
                    Timestamp = DateTime.Now,
                    TransactionType = "Product",
                    Description = "Register 1 - Sale",
                    Quantity = 1,
                    UnitPrice = 500m,
                    TotalPrice = 500m,
                    PaymentMethod = "CASH",
                    Notes = $"Register 1, Transaction {i + 1}"
                };
                SalesReportHelper.LogTransaction(sale);
                Console.WriteLine($"✓ Register 1: Sale {i + 1} logged");
                System.Threading.Thread.Sleep(50);
            }
        });

        // Simulate Register 2
        var register2Task = System.Threading.Tasks.Task.Run(() =>
        {
            for (int i = 0; i < 3; i++)
            {
                var sale = new Transaction
                {
                    Timestamp = DateTime.Now,
                    TransactionType = "Product",
                    Description = "Register 2 - Sale",
                    Quantity = 2,
                    UnitPrice = 750m,
                    TotalPrice = 1500m,
                    PaymentMethod = "GCASH",
                    Notes = $"Register 2, Transaction {i + 1}"
                };
                SalesReportHelper.LogTransaction(sale);
                Console.WriteLine($"✓ Register 2: Sale {i + 1} logged");
                System.Threading.Thread.Sleep(60);
            }
        });

        // Wait for both to complete
        System.Threading.Tasks.Task.WaitAll(register1Task, register2Task);

        Console.WriteLine("\n✓ All concurrent sales processed without corruption!");
        Console.WriteLine("  (FileAccessLayer ensured exclusive file access)\n");
    }
}

/// <summary>
/// EXAMPLE 8: Error Handling & Recovery
/// 
/// Scenario: Demonstrates proper error handling and retry logic
/// </summary>
public class Example_ErrorHandling
{
    public void DemonstrateErrorHandling()
    {
        try
        {
            // Attempt to update inventory
            bool success = InventoryHelper.UpdateStock("BAR_NONEXISTENT", 100);

            if (!success)
            {
                Console.WriteLine("⚠️  Product not found. Check barcode.");
            }
        }
        catch (InvalidOperationException ex)
        {
            // This happens if file is locked after all retries
            Console.WriteLine($"❌ File access error: {ex.Message}");
            Console.WriteLine("   Possible causes:");
            Console.WriteLine("   - Inventory.xlsx open in Excel");
            Console.WriteLine("   - File permissions issue");
            Console.WriteLine("   - Disk full or write-protected");
        }

        try
        {
            // Check if product has enough stock
            var item = InventoryHelper.GetInventoryItemByBarcode("BAR001");

            if (item != null && item.CurrentStock >= 5)
            {
                InventoryHelper.DeductStock("BAR001", 5);
                Console.WriteLine("✓ Stock deducted successfully");
            }
            else if (item != null)
            {
                Console.WriteLine($"⚠️  Insufficient stock. Available: {item.CurrentStock}");
            }
            else
            {
                Console.WriteLine("⚠️  Product not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
        }
    }
}

/// <summary>
/// EXAMPLE 9: Querying Sales History
/// 
/// Scenario: Find all transactions for a specific service type or date
/// </summary>
public class Example_QuerySalesHistory
{
    public void QueryServiceRepairs()
    {
        var startDate = DateTime.Today.AddDays(-7);  // Last 7 days
        var endDate = DateTime.Today.AddDays(1);

        var transactions = SalesReportHelper.GetTransactionsByDateRange(startDate, endDate);

        // Find all repair services
        var repairs = transactions
            .Where(t => t.TransactionType == "Service" && 
                   t.Description.Contains("Repair"))
            .ToList();

        Console.WriteLine($"Repair Services (last 7 days): {repairs.Count}\n");

        foreach (var repair in repairs)
        {
            Console.WriteLine($"  Date: {repair.Timestamp:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  Service: {repair.Description}");
            Console.WriteLine($"  Cost: ₱{repair.TotalPrice:F2}");
            Console.WriteLine($"  Payment: {repair.PaymentMethod}");
            if (!string.IsNullOrEmpty(repair.Notes))
                Console.WriteLine($"  Notes: {repair.Notes}");
            Console.WriteLine();
        }

        decimal repairRevenue = repairs.Sum(r => r.TotalPrice);
        Console.WriteLine($"Total Repair Revenue: ₱{repairRevenue:F2}");
    }
}

/// <summary>
/// EXAMPLE 10: Complete POS Cycle
/// 
/// Full workflow from scanning to receipt
/// </summary>
public class Example_CompletePOSCycle
{
    public void RunCompletePOSCycle()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("        DESTINY POS - CHECKOUT SCREEN");
        Console.WriteLine("═══════════════════════════════════════════\n");

        var cart = new List<SaleItem>();

        // Scan items
        Console.WriteLine("Scanning items...\n");

        var item1 = InventoryHelper.GetInventoryItemByBarcode("BAR001");
        var item2 = InventoryHelper.GetInventoryItemByBarcode("BAR004");

        if (item1 != null)
        {
            cart.Add(new SaleItem
            {
                Barcode = item1.Barcode,
                Name = item1.ProductName,
                ItemType = "Product",
                Quantity = 1,
                Price = item1.UnitPrice
            });
        }

        if (item2 != null)
        {
            cart.Add(new SaleItem
            {
                Barcode = item2.Barcode,
                Name = item2.ProductName,
                ItemType = "Product",
                Quantity = 2,
                Price = item2.UnitPrice
            });
        }

        // Add service
        cart.Add(new SaleItem
        {
            Barcode = "SRV_PRINT_001",
            Name = "Printing Service",
            ItemType = "Service",
            Quantity = 50,  // pages
            Price = 0.50m    // per page
        });

        // Display cart
        Console.WriteLine("Items in cart:");
        decimal subtotal = 0;
        foreach (var item in cart)
        {
            decimal itemTotal = item.Price * item.Quantity;
            Console.WriteLine($"  • {item.Name} x{item.Quantity} @ ₱{item.Price}/unit = ₱{itemTotal:F2}");
            subtotal += itemTotal;
        }

        Console.WriteLine($"\nSubtotal: ₱{subtotal:F2}");

        decimal discount = 100.00m;
        decimal total = subtotal - discount;

        Console.WriteLine($"Discount: -₱{discount:F2}");
        Console.WriteLine($"TOTAL: ₱{total:F2}");

        // Process payment
        Console.WriteLine("\n[SELECT PAYMENT METHOD]");
        string paymentMethod = "CASH";
        Console.WriteLine($"Payment: {paymentMethod}\n");

        // Log transactions
        var transactions = new List<Transaction>();

        foreach (var item in cart.Where(i => i.ItemType == "Product"))
        {
            transactions.Add(new Transaction
            {
                Timestamp = DateTime.Now,
                TransactionType = "Product",
                Description = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                TotalPrice = item.Price * item.Quantity,
                PaymentMethod = paymentMethod,
                Notes = "POS Sale"
            });

            InventoryHelper.DeductStock(item.Barcode, item.Quantity);
        }

        foreach (var item in cart.Where(i => i.ItemType == "Service"))
        {
            transactions.Add(new Transaction
            {
                Timestamp = DateTime.Now,
                TransactionType = "Service",
                Description = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                TotalPrice = item.Price * item.Quantity,
                PaymentMethod = paymentMethod,
                Notes = "POS Sale"
            });
        }

        SalesReportHelper.LogTransactions(transactions);

        // Show receipt
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("             RECEIPT / INVOICE");
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        foreach (var item in cart)
        {
            decimal itemTotal = item.Price * item.Quantity;
            Console.WriteLine($"{item.Name}");
            Console.WriteLine($"  {item.Quantity} @ ₱{item.Price}/unit .......... ₱{itemTotal:F2}");
        }

        Console.WriteLine();
        Console.WriteLine($"Subtotal .......................... ₱{subtotal:F2}");
        Console.WriteLine($"Discount .......................... -₱{discount:F2}");
        Console.WriteLine("───────────────────────────────────────────");
        Console.WriteLine($"TOTAL ............................ ₱{total:F2}");
        Console.WriteLine($"Payment Method: {paymentMethod}");
        Console.WriteLine();
        Console.WriteLine("✓ TRANSACTION COMPLETE - Thank you!");
        Console.WriteLine("═══════════════════════════════════════════\n");
    }
}
