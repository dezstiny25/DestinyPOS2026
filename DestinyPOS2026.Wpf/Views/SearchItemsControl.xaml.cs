using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DestinyPOS2026.Wpf.Helpers;
using DestinyPOS2026.Wpf.Models;

namespace DestinyPOS2026.Wpf.Views;

/// <summary>
/// Custom delegate for item selection event
/// </summary>
public delegate void ItemSelectedEventHandler(object sender, InventoryItem item);

/// <summary>
/// Search control with auto-suggest dropdown
/// Provides real-time filtering of inventory items by barcode or name
/// </summary>
public partial class SearchItemsControl : UserControl
{
    // Observable collections for data binding
    public ObservableCollection<InventoryItem> FilteredResults { get; } = new();
    public ObservableCollection<InventoryItem> AllItems { get; } = new();

    // Events
    public event ItemSelectedEventHandler? ItemSelected;

    // Properties
    public bool IsDropdownOpen
    {
        get => (bool)GetValue(IsDropdownOpenProperty);
        set => SetValue(IsDropdownOpenProperty, value);
    }

    public static readonly DependencyProperty IsDropdownOpenProperty =
        DependencyProperty.Register("IsDropdownOpen", typeof(bool), typeof(SearchItemsControl), new PropertyMetadata(false));

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register("SearchText", typeof(string), typeof(SearchItemsControl), new PropertyMetadata(string.Empty));

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public event EventHandler? SearchRequested;

    public InventoryItem? SelectedItem { get; private set; }

    public SearchItemsControl()
    {
        InitializeComponent();
        DataContext = this;
        
        // Load all inventory items on initialization
        LoadInventoryItems();
    }

    /// <summary>
    /// Loads all inventory items from the helper
    /// </summary>
    private void LoadInventoryItems()
    {
        try
        {
            var items = InventoryHelper.GetAllInventoryItems();
            AllItems.Clear();

            if (items.Count == 0)
            {
                AllItems.Add(new InventoryItem
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
                });
            }
            else
            {
                foreach (var item in items)
                {
                    AllItems.Add(item);
                }
            }

            if (FilteredResults.Count == 0)
            {
                FilteredResults.Clear();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles text changes in the search box - filters results
    /// </summary>
    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string searchTerm = SearchTextBox.Text.ToLower().Trim();

        // Update placeholder visibility
        PlaceholderText.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ? Visibility.Visible : Visibility.Hidden;

        if (string.IsNullOrEmpty(searchTerm))
        {
            DropdownBorder.Visibility = Visibility.Collapsed;
            NoResultsText.Visibility = Visibility.Collapsed;
            FilteredResults.Clear();
            return;
        }

        // Filter items by barcode or name
        var filtered = AllItems
            .Where(item =>
                item.Barcode.ToLower().Contains(searchTerm) ||
                item.ProductName.ToLower().Contains(searchTerm))
            .OrderBy(item =>
            {
                // Prioritize barcode matches
                if (item.Barcode.ToLower().StartsWith(searchTerm)) return 0;
                if (item.Barcode.ToLower().Contains(searchTerm)) return 1;
                if (item.ProductName.ToLower().StartsWith(searchTerm)) return 2;
                return 3;
            })
            .Take(10) // Limit to 10 results
            .ToList();

        FilteredResults.Clear();
        foreach (var item in filtered)
        {
            FilteredResults.Add(item);
        }

        // Show/hide dropdown and no results message
        DropdownBorder.Visibility = FilteredResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoResultsText.Visibility = (FilteredResults.Count == 0 && !string.IsNullOrEmpty(searchTerm))
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Select first result
        if (FilteredResults.Count > 0)
        {
            ResultsListBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Handles key down events in search box
    /// </summary>
    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (ResultsListBox.Items.Count > 0 && ResultsListBox.SelectedIndex < ResultsListBox.Items.Count - 1)
                {
                    ResultsListBox.SelectedIndex++;
                }
                e.Handled = true;
                break;

            case Key.Up:
                if (ResultsListBox.SelectedIndex > 0)
                {
                    ResultsListBox.SelectedIndex--;
                }
                e.Handled = true;
                break;

            case Key.Enter:
                if (ResultsListBox.SelectedItem is InventoryItem selectedItem)
                {
                    SelectItem(selectedItem);
                }
                else if (FilteredResults.Count > 0)
                {
                    SelectItem(FilteredResults[0]);
                }
                else
                {
                    SearchRequested?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;
                break;

            case Key.Escape:
                SearchTextBox.Clear();
                IsDropdownOpen = false;
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles selection in the dropdown list
    /// </summary>
    private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Scroll selected item into view
        if (ResultsListBox.SelectedItem != null)
        {
            ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
        }
    }

    /// <summary>
    /// Handles double-click on dropdown items
    /// </summary>
    private void ResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsListBox.SelectedItem is InventoryItem selectedItem)
        {
            SelectItem(selectedItem);
        }
    }

    /// <summary>
    /// Selects an item and triggers the ItemSelected event
    /// </summary>
    private void SelectItem(InventoryItem item)
    {
        SelectedItem = item;
        DropdownBorder.Visibility = Visibility.Collapsed;
        SearchTextBox.Clear();
        PlaceholderText.Visibility = Visibility.Visible;
        
        // Raise event for parent to handle
        ItemSelected?.Invoke(this, item);
    }

    /// <summary>
    /// Refreshes the inventory list (useful if inventory was updated elsewhere)
    /// </summary>
    public void RefreshInventory()
    {
        LoadInventoryItems();
        FilteredResults.Clear();
    }

    /// <summary>
    /// Clears the search and closes dropdown
    /// </summary>
    public void Clear()
    {
        SearchTextBox.Clear();
        SelectedItem = null;
        DropdownBorder.Visibility = Visibility.Collapsed;
        FilteredResults.Clear();
        PlaceholderText.Visibility = Visibility.Visible;
    }
}
