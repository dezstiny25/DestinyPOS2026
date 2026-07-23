using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace DestinyPOS2026.Wpf.ViewModels;

public class InventoryViewModel : BaseViewModel
{
    private readonly ObservableCollection<InventoryItem> _allInventoryItems = new();
    public ObservableCollection<InventoryItem> InventoryItems { get; } = new();

    private InventoryItem? _selectedInventoryItem;
    public InventoryItem? SelectedInventoryItem
    {
        get => _selectedInventoryItem;
        set { _selectedInventoryItem = value; OnPropertyChanged(); }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    private string _sortOption = "By Stocks";
    public string SortOption
    {
        get => _sortOption;
        set
        {
            _sortOption = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public InventoryViewModel()
    {
        InventoryHelper.InitializeInventoryFile();
        LoadInventoryItems();

        AddCommand = new RelayCommand(_ => Add());
        UpdateCommand = new RelayCommand(_ => Update(), _ => SelectedInventoryItem != null);
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedInventoryItem != null);
    }

    private void LoadInventoryItems()
    {
        _allInventoryItems.Clear();
        InventoryItems.Clear();
        var items = InventoryHelper.GetAllInventoryItems();

        if (items.Count == 0)
        {
            var fallback = new InventoryItem
            {
                Barcode = "NO-DATA",
                ProductName = "No inventory rows found",
                Category = "System",
                UnitPrice = 0,
                CurrentStock = 0,
                ReorderLevel = 0,
                ReorderQuantity = 0,
                Supplier = string.Empty,
                LastRestocked = DateTime.Now
            };
            _allInventoryItems.Add(fallback);
        }
        else
        {
            foreach (var item in items)
                _allInventoryItems.Add(item);
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allInventoryItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(item =>
                item.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Supplier.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SortOption switch
        {
            "By Manufacturers" => filtered.OrderBy(item => item.Supplier).ThenBy(item => item.ProductName),
            _ => filtered.OrderByDescending(item => item.CurrentStock).ThenBy(item => item.ProductName)
        };

        InventoryItems.Clear();
        foreach (var item in filtered)
            InventoryItems.Add(item);
    }

    private void Add()
    {
        var item = new InventoryItem
        {
            Barcode = $"NEW-{DateTime.Now:HHmmss}",
            ProductName = "New Product",
            Category = "General",
            UnitPrice = 0,
            CurrentStock = 0,
            ReorderLevel = 0,
            ReorderQuantity = 0,
            Supplier = string.Empty,
            LastRestocked = DateTime.Now
        };

        InventoryHelper.AddInventoryItem(item);
        LoadInventoryItems();
    }

    private void Update()
    {
        if (SelectedInventoryItem == null) return;
        InventoryHelper.UpdateInventoryItem(SelectedInventoryItem);
        LoadInventoryItems();
    }

    private void Delete()
    {
        if (SelectedInventoryItem == null) return;

        MessageBox.Show(
            "Delete is not supported for the Excel-backed inventory yet.",
            "Information",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
