using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace DestinyPOS2026.Wpf.ViewModels;

public class InventoryViewModel : BaseViewModel
{
    public ObservableCollection<Product> Products { get; set; } = new();

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set { _selectedProduct = value; OnPropertyChanged(); }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public InventoryViewModel()
    {
        DatabaseHelper.InitializeDatabase();
        LoadProducts();

        AddCommand = new RelayCommand(_ => Add());
        UpdateCommand = new RelayCommand(_ => Update(), _ => SelectedProduct != null);
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedProduct != null);
    }

    private void LoadProducts()
    {
        Products.Clear();
        foreach (var p in DatabaseHelper.GetProducts())
            Products.Add(p);
    }

    private void Add()
    {
        var p = new Product
        {
            Barcode = "NEW-BARCODE",
            Name = "New Product",
            Price = 0,
            Stock = 0
        };

        DatabaseHelper.AddProduct(p);
        LoadProducts();
    }

    private void Update()
    {
        if (SelectedProduct == null) return;
        DatabaseHelper.UpdateProduct(SelectedProduct);
        LoadProducts();
    }

    private void Delete()
    {
        if (SelectedProduct == null) return;

        if (MessageBox.Show("Delete product?", "Confirm",
            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            DatabaseHelper.DeleteProduct(SelectedProduct.Id);
            LoadProducts();
        }
    }
}
