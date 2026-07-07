using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DestinyPOS2026.Wpf.ViewModels;

public class SaleItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Price * Quantity;
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

    public RelayCommand AddItemCommand { get; }
    public RelayCommand PayCashCommand { get; }
    public RelayCommand PayGcashCommand { get; }
    public RelayCommand PayCardCommand { get; }

    public BitmapImage QrCodeImage { get; }

    private string SessionToken { get; } = Guid.NewGuid().ToString();

    public PosViewModel()
    {
        AddItemCommand = new RelayCommand(_ => AddItem());
        PayCashCommand = new RelayCommand(_ => CompleteSale("CASH"));
        PayGcashCommand = new RelayCommand(_ => CompleteSale("GCASH"));
        PayCardCommand = new RelayCommand(_ => CompleteSale("CARD"));

        DatabaseHelper.InitializeDatabase();

        // Subscribe to barcodes from pairing server
        PairingServerHelper.BarcodeReceived += ReceiveBarcodeFromPhone;

        // Generate a simple placeholder QR code (actual pairing page QR is generated server-side)
        QrCodeImage = null!;
    }

    private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var product = DatabaseHelper.GetProductByBarcode(BarcodeInput.Trim());
        if (product == null)
        {
            MessageBox.Show($"Product with barcode {BarcodeInput} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existing = SaleItems.FirstOrDefault(x => x.ProductId == product.Id);
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
                ProductId = product.Id,
                Name = product.Name,
                Quantity = 1,
                Price = product.Price
            });
        }

        BarcodeInput = string.Empty;
        RefreshTotals();
    }

    private void CompleteSale(string method)
    {
        if (!SaleItems.Any())
        {
            MessageBox.Show("No items to checkout.", "POS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Deduct stock
        foreach (var item in SaleItems)
            DatabaseHelper.DeductProductStock(item.ProductId, item.Quantity);

        // Log sale
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
