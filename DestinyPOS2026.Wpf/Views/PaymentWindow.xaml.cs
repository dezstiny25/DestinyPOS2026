using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DestinyPOS2026.Wpf.Views;

public partial class PaymentWindow : Window, INotifyPropertyChanged
{
    private decimal _amountDue;
    private decimal _cashTendered;
    private decimal _changeDue;
    private string _paymentMethod = "CASH";
    private string _paymentStatus = "Not Paid";
    private Visibility _gcashQrVisibility = Visibility.Collapsed;
    private ImageSource? _gcashQrImage;

    public decimal AmountDue
    {
        get => _amountDue;
        set { _amountDue = value; OnPropertyChanged(nameof(AmountDue)); }
    }

    public decimal CashTendered
    {
        get => _cashTendered;
        set
        {
            _cashTendered = value;
            RecalculatePayment();
            OnPropertyChanged(nameof(CashTendered));
        }
    }

    public decimal ChangeDue
    {
        get => _changeDue;
        set { _changeDue = value; OnPropertyChanged(nameof(ChangeDue)); }
    }

    public string PaymentMethod
{
    get => _paymentMethod;
    set
    {
        if (_paymentMethod == value)
            return;

        _paymentMethod = value;

        if (_paymentMethod.Equals("GCASH", StringComparison.OrdinalIgnoreCase))
        {
            // Automatically tender the exact amount
            CashTendered = AmountDue;
        }

        RecalculatePayment();

        OnPropertyChanged(nameof(PaymentMethod));
        OnPropertyChanged(nameof(IsCashTenderedReadOnly));
    }
}

    public string PaymentStatus
    {
        get => _paymentStatus;
        set { _paymentStatus = value; OnPropertyChanged(nameof(PaymentStatus)); }
    }

    public Visibility GcashQrVisibility
    {
        get => _gcashQrVisibility;
        set { _gcashQrVisibility = value; OnPropertyChanged(nameof(GcashQrVisibility)); }
    }

    public ImageSource? GcashQrImage
    {
        get => _gcashQrImage;
        set { _gcashQrImage = value; OnPropertyChanged(nameof(GcashQrImage)); }
    }

public bool IsCashTenderedReadOnly =>
    PaymentMethod.Equals("GCASH", StringComparison.OrdinalIgnoreCase);
    public PaymentWindow(decimal amountDue)
    {
        InitializeComponent();
        AmountDue = amountDue;
        CashTendered = amountDue;
        LoadGcashQr();
        DataContext = this;
    }

    private void RecalculatePayment()
    {
        if (PaymentMethod.Equals("CASH", StringComparison.OrdinalIgnoreCase))
        {
            GcashQrVisibility = Visibility.Collapsed;
            ChangeDue = Math.Max(0, CashTendered - AmountDue);
            PaymentStatus = CashTendered >= AmountDue ? "Paid" : "Insufficient Payment";
        }
        else
        {
            GcashQrVisibility = PaymentMethod.Equals("GCASH", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
            ChangeDue = 0;
            PaymentStatus = "Paid";
        }
    }

    private void LoadGcashQr()
    {
        try
        {
            var root = AppContext.BaseDirectory;
            var currentDir = Directory.GetCurrentDirectory();

            var candidates = new[]
            {
                Path.Combine(currentDir, "DestinyPOS2026.Wpf", "assets", "gcashQR.png"),
                Path.Combine(currentDir, "assets", "gcashQR.png"),
                Path.Combine(root, "assets", "gcashQR.png"),
                Path.Combine(root, "gcashQR.png"),
                Path.Combine(currentDir, "gcashQR.png"),
                Path.Combine(root, "..", "..", "..", "..", "DestinyPOS2026.Wpf", "assets", "gcashQR.png"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    var bitmap = new BitmapImage(new Uri(Path.GetFullPath(candidate), UriKind.Absolute));
                    GcashQrImage = bitmap;
                    return;
                }
            }

            GcashQrImage = null;
        }
        catch
        {
            GcashQrImage = null;
        }
    }

    private void Complete_Click(object sender, RoutedEventArgs e)
    {
        RecalculatePayment();

        if (!PaymentMethod.Equals("CASH", StringComparison.OrdinalIgnoreCase) || CashTendered >= AmountDue)
        {
            DialogResult = true;
            Close();
            return;
        }

        MessageBox.Show("Cash tendered must be equal to or greater than the total due.", "Payment Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
