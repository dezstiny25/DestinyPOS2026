using DestinyPOS2026.Wpf.Models;
using DestinyPOS2026.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Linq;

namespace DestinyPOS2026.Wpf.ViewModels;

public class ReportsViewModel : BaseViewModel
{
    public ObservableCollection<SaleItem> ReportItems { get; set; } = new();

    public ReportsViewModel()
    {
        try
        {
            var record = DatabaseHelper.GetLastSale();
            if (record != null)
            {
                ReportItems = new ObservableCollection<SaleItem>(
                    record.Items.Select(x => new SaleItem
                    {
                        Name = x.Name,
                        Quantity = x.Quantity,
                        Price = x.Price
                    })
                );
            }
        }
        catch
        {
            // handle or log
        }
    }
}
