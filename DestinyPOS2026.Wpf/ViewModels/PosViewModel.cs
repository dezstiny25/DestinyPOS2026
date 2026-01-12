using DestinyPOS2026.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System;

namespace DestinyPOS2026.Wpf.ViewModels;

public class SaleItem
{
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

    public ObservableCollection<SaleItem> SaleItems { get; set; } = new();

    public decimal Subtotal => SaleItems.Sum(x => x.Total);
    public decimal Discount { get; set; } = 0m;
    public decimal Total => Subtotal - Discount;

    public RelayCommand AddItemCommand { get; }
    public RelayCommand PayCashCommand { get; }
    public RelayCommand PayGcashCommand { get; }
    public RelayCommand PayCardCommand { get; }

    public string SessionToken { get; private set; } = Guid.NewGuid().ToString();
    public BitmapImage QrCodeImage { get; private set; }

    public PosViewModel()
    {
        AddItemCommand = new RelayCommand(_ => AddItem());
        PayCashCommand = new RelayCommand(_ => Pay("CASH"));
        PayGcashCommand = new RelayCommand(_ => Pay("GCash"));
        PayCardCommand = new RelayCommand(_ => Pay("CARD"));

        DatabaseHelper.InitializeDatabase();

        // Start WebSocket server
        Task.Run(() => WebSocketServerHelper.StartServer(5000, SessionToken, ReceiveBarcodeFromPhone));

        // Generate HTTP URL for phone web app
        string pcIp = NetworkHelper.GetLocalIPAddress(); // should return LAN IP
        string pairingUrl = $"http://{pcIp}:8000/?token={SessionToken}";
        QrCodeImage = QrCodeHelper.GenerateQRCode(pairingUrl);

    }

    private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var product = DatabaseHelper.GetProductByBarcode(BarcodeInput);
        if (product == null)
        {
            MessageBox.Show($"Product with barcode {BarcodeInput} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var item = new SaleItem
        {
            Name = product.Value.Name,
            Quantity = 1,
            Price = product.Value.Price
        };
        SaleItems.Add(item);

        BarcodeInput = string.Empty;

        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
    }

    private void Pay(string method)
    {
        MessageBox.Show($"Payment method: {method}\nTotal: ₱{Total:N2}", "Payment", MessageBoxButton.OK, MessageBoxImage.Information);
        SaleItems.Clear();
        Discount = 0m;
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
