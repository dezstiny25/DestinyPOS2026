using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;
using DestinyPOS2026.Wpf.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DestinyPOS2026.Wpf.ViewModels;

/// <summary>
/// Enhanced sale item that supports both products and services
/// </summary>
public class SaleItem
{
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Product"; // "Product" or "Service"
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Price * Quantity;
    public string Notes { get; set; } = string.Empty; // For service details
}

public class PosViewModel : BaseViewModel
{
    private string _barcodeInput = string.Empty;
    public string BarcodeInput
    {
        get => _barcodeInput;
        set { _barcodeInput = value; OnPropertyChanged(); }
    }

    public ObservableCollection<SaleItem> SaleItems { get; } = new();

    public decimal Subtotal => SaleItems.Sum(x => x.Total);

    private decimal _discount;
    public decimal Discount
    {
        get => _discount;
        set { _discount = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
    }

    public decimal Total => Subtotal - Discount;

    // Commands
    public RelayCommand AddItemCommand { get; }
    public RelayCommand AddServiceCommand { get; }
    public RelayCommand PayCashCommand { get; }
    public RelayCommand RemoveItemCommand { get; }
    public RelayCommand ClearAllCommand { get; }

    public BitmapImage QrCodeImage { get; }

    private string SessionToken { get; } = Guid.NewGuid().ToString();

    public PosViewModel()
    {
        AddItemCommand = new RelayCommand(_ => AddItem());
        AddServiceCommand = new RelayCommand(_ => AddService());
        PayCashCommand = new RelayCommand(_ => CompleteSale("CASH"));
        RemoveItemCommand = new RelayCommand(obj => RemoveItem(obj));
        ClearAllCommand = new RelayCommand(_ => ClearAll());

        // Initialize inventory and sales logging systems
        InventoryHelper.InitializeInventoryFile();
        SalesReportHelper.InitializeSalesReportFile();
        DatabaseHelper.InitializeDatabase();

        // Subscribe to barcodes from pairing server
        PairingServerHelper.BarcodeReceived += ReceiveBarcodeFromPhone;

        // Generate a simple placeholder QR code (actual pairing page QR is generated server-side)
        QrCodeImage = null!;
    }

    private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var barcode = BarcodeInput.Trim();

        // Try to find as inventory item first
        var inventoryItem = InventoryHelper.GetInventoryItemByBarcode(barcode);
        if (inventoryItem != null)
        {
            AddInventoryItem(inventoryItem);
            return;
        }

        // Fallback to database for backward compatibility
        var product = DatabaseHelper.GetProductByBarcode(barcode);
        if (product != null)
        {
            AddProductFromDatabase(product);
            return;
        }

        MessageBox.Show($"Product with barcode {barcode} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Handles item selected from search control
    /// Called when user selects an item from the search dropdown
    /// </summary>
    public void OnItemSelected(InventoryItem inventoryItem)
    {
        AddInventoryItem(inventoryItem);
    }

    private void AddInventoryItem(InventoryItem inventoryItem)
    {
        // Check for low stock alert
        if (inventoryItem.CurrentStock <= inventoryItem.ReorderLevel)
        {
            MessageBox.Show(
                $"Low stock alert: {inventoryItem.ProductName}\nCurrent: {inventoryItem.CurrentStock}, Reorder Level: {inventoryItem.ReorderLevel}",
                "Stock Alert",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        var existing = SaleItems.FirstOrDefault(x => x.Barcode == inventoryItem.Barcode && x.ItemType == "Product");
        if (existing != null)
        {
            if (existing.Quantity + 1 > inventoryItem.CurrentStock)
            {
                MessageBox.Show("Insufficient stock.", "Stock Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existing.Quantity++;
        }
        else
        {
            if (inventoryItem.CurrentStock <= 0)
            {
                MessageBox.Show("Product out of stock.", "Stock Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaleItems.Add(new SaleItem
            {
                Barcode = inventoryItem.Barcode,
                Name = inventoryItem.ProductName,
                ItemType = "Product",
                Quantity = 1,
                Price = inventoryItem.UnitPrice
            });
        }

        BarcodeInput = string.Empty;
        RefreshTotals();
    }

    private void AddProductFromDatabase(Product product)
    {
        var existing = SaleItems.FirstOrDefault(x => x.Barcode == product.Barcode && x.ItemType == "Product");
        if (existing != null)
        {
            if (existing.Quantity + 1 > product.Stock)
            {
                MessageBox.Show("Insufficient stock.", "Stock Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existing.Quantity++;
        }
        else
        {
            if (product.Stock <= 0)
            {
                MessageBox.Show("Product out of stock.", "Stock Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaleItems.Add(new SaleItem
            {
                Barcode = product.Barcode,
                Name = product.Name,
                ItemType = "Product",
                Quantity = 1,
                Price = product.Price
            });
        }

        BarcodeInput = string.Empty;
        RefreshTotals();
    }

    /// <summary>
    /// Opens the Add Service modal window
    /// Allows user to enter service name, labor cost, and notes
    /// </summary>
    public void AddService()
    {
        var serviceWindow = new AddServiceWindow();
        
        // Show as modal dialog
        if (serviceWindow.ShowDialog() == true)
        {
            // User clicked Save
            string serviceName = serviceWindow.ServiceName;
            decimal laborCost = serviceWindow.LaborPrice;
            string notes = serviceWindow.Notes;

            // Add service to sale items
            var serviceItem = new SaleItem
            {
                Barcode = $"SERVICE-{Guid.NewGuid():N}".Substring(0, 20), // Unique service barcode
                Name = serviceName,
                ItemType = "Service",
                Quantity = 1,
                Price = laborCost,
                Notes = notes
            };

            SaleItems.Add(serviceItem);
            RefreshTotals();
        }
    }

    /// <summary>
    /// Adds a printing service to the sale
    /// </summary>
    public void AddPrintingService(string paperSize, string printType, int quantity)
    {
        var printingOption = PricingHelper.CalculatePrintingPrice(paperSize, printType, quantity);
        var description = $"Printing: {printingOption}";

        SaleItems.Add(new SaleItem
        {
            Barcode = $"PRINT-{paperSize}-{printType}",
            Name = description,
            ItemType = "Service",
            Quantity = quantity,
            Price = printingOption.PricePerUnit,
            Notes = $"Paper Size: {paperSize}, Type: {printType}"
        });

        RefreshTotals();
    }

    /// <summary>
    /// Adds a repair service to the sale
    /// </summary>
    public void AddRepairService(string repairType, int laborMinutes, decimal complexityFactor, decimal laborCost)
    {
        var description = $"{repairType}: {laborMinutes} minutes";

        SaleItems.Add(new SaleItem
        {
            Barcode = $"REPAIR-{repairType}",
            Name = description,
            ItemType = "Service",
            Quantity = 1,
            Price = laborCost,
            Notes = $"Type: {repairType}, Minutes: {laborMinutes}, Complexity: {complexityFactor}x"
        });

        RefreshTotals();
    }

    /// <summary>
    /// Removes a single item from the sale
    /// </summary>
    private void RemoveItem(object obj)
    {
        if (obj is SaleItem item)
        {
            SaleItems.Remove(item);
            RefreshTotals();
        }
    }

    /// <summary>
    /// Clears all items from the current transaction
    /// </summary>
    private void ClearAll()
    {
        var result = MessageBox.Show(
            "Clear all items from this transaction?",
            "Confirm Clear",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SaleItems.Clear();
            Discount = 0;
            RefreshTotals();
        }
    }

    /// <summary>
    /// Completes the sale and logs all transactions
    /// Uses Cash payment method (simplified as per requirements)
    /// Logs transactions to monthly files with daily sheet organization
    /// </summary>
    private void CompleteSale(string method)
    {
        if (!SaleItems.Any())
        {
            MessageBox.Show("No items to checkout.", "POS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var transactions = new System.Collections.Generic.List<Transaction>();

        // Process each sale item
        foreach (var item in SaleItems)
        {
            if (item.ItemType == "Product")
            {
                // Deduct stock from inventory
                if (!InventoryHelper.DeductStock(item.Barcode, item.Quantity))
                {
                    // Fallback to database if not in inventory
                    var product = DatabaseHelper.GetProductByBarcode(item.Barcode);
                    if (product != null)
                    {
                        DatabaseHelper.DeductProductStock(product.Id, item.Quantity);
                    }
                }
            }

            // Create transaction for logging
            transactions.Add(new Transaction
            {
                Timestamp = DateTime.Now,
                TransactionType = item.ItemType,
                Description = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                TotalPrice = item.Total,
                PaymentMethod = method,
                Notes = item.Notes
            });
        }

        // Log all transactions to monthly file with daily sheet organization
        SalesReportHelper.LogTransactionsMonthly(transactions);

        // Also log to legacy SalesReport.xlsx and database for backward compatibility
        SalesReportHelper.LogTransactions(transactions);

        // Also log to database for backward compatibility
        var sale = new SaleRecord
        {
            Date = DateTime.Now,
            PaymentMethod = method,
            Total = Total,
            Items = SaleItems.Select(x => new DestinyPOS2026.Wpf.Models.SaleItem
            {
                Name = x.Name,
                Quantity = x.Quantity,
                Price = x.Price
            }).ToList()
        };
        DatabaseHelper.LogSale(sale);

        MessageBox.Show($"Payment successful!\nMethod: {method}\nTotal: ₱{Total:N2}", "Sale Completed", MessageBoxButton.OK, MessageBoxImage.Information);

        SaleItems.Clear();
        Discount = 0;
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
    }

    private void ReceiveBarcodeFromPhone(string barcode)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            BarcodeInput = barcode;
            AddItemCommand.Execute(null);
        });
    }
}
