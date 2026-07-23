using System;
using System.Windows.Controls;
using DestinyPOS2026.Wpf.Models;
using DestinyPOS2026.Wpf.ViewModels;

namespace DestinyPOS2026.Wpf.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles item selected from search control
    /// Passes the selected item to the ViewModel
    /// </summary>
    private void SearchItemsControl_ItemSelected(object sender, InventoryItem item)
    {
        if (DataContext is PosViewModel viewModel)
        {
            viewModel.OnItemSelected(item);
        }
    }

    private void SearchItemsControl_SearchRequested(object sender, EventArgs e)
    {
        if (DataContext is PosViewModel viewModel)
        {
            viewModel.AddItemCommand.Execute(null);
        }
    }
}
