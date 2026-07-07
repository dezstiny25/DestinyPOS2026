# Implementation Examples

## Example 1: Adding Printing Service to UI

### ViewModel
```csharp
public class PrintingServiceViewModel : BaseViewModel
{
    public ObservableCollection<string> PaperSizes { get; }
    public ObservableCollection<string> PrintTypes { get; }
    
    private string _selectedPaperSize = "Letter";
    public string SelectedPaperSize
    {
        get => _selectedPaperSize;
        set { _selectedPaperSize = value; OnPropertyChanged(); }
    }
    
    private string _selectedPrintType = "BW";
    public string SelectedPrintType
    {
        get => _selectedPrintType;
        set { _selectedPrintType = value; OnPropertyChanged(); }
    }
    
    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); }
    }
    
    public decimal TotalPrice
    {
        get
        {
            var option = PricingHelper.CalculatePrintingPrice(_selectedPaperSize, _selectedPrintType, _quantity);
            return option.TotalPrice;
        }
    }
    
    public RelayCommand AddToPosCommand { get; }
    
    private PosViewModel _posViewModel;
    
    public PrintingServiceViewModel(PosViewModel posViewModel)
    {
        _posViewModel = posViewModel;
        
        PaperSizes = new ObservableCollection<string>(PricingHelper.AvailablePaperSizes);
        PrintTypes = new ObservableCollection<string>(PricingHelper.AvailablePrintTypes);
        
        AddToPosCommand = new RelayCommand(_ => AddToPOS());
    }
    
    private void AddToPOS()
    {
        _posViewModel.AddPrintingService(_selectedPaperSize, _selectedPrintType, _quantity);
        // Close dialog
    }
}
```

### XAML View
```xaml
<Window x:Class="DestinyPOS2026.Wpf.Views.PrintingServiceWindow"
        Title="Add Printing Service" Width="400" Height="300">
    <StackPanel Padding="20">
        <TextBlock Text="Paper Size:" Margin="0,0,0,5"/>
        <ComboBox ItemsSource="{Binding PaperSizes}" 
                  SelectedItem="{Binding SelectedPaperSize}" 
                  Margin="0,0,0,15"/>
        
        <TextBlock Text="Print Type:" Margin="0,0,0,5"/>
        <ComboBox ItemsSource="{Binding PrintTypes}" 
                  SelectedItem="{Binding SelectedPrintType}" 
                  Margin="0,0,0,15"/>
        
        <TextBlock Text="Number of Pages:" Margin="0,0,0,5"/>
        <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged}" 
                 Margin="0,0,0,15"/>
        
        <Grid Margin="0,0,0,15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="Total Price:" VerticalAlignment="Center"/>
            <TextBlock Grid.Column="1" 
                       Text="{Binding TotalPrice, StringFormat='₱{0:N2}'}" 
                       FontSize="16" FontWeight="Bold"/>
        </Grid>
        
        <Button Command="{Binding AddToPosCommand}" 
                Content="Add to Sale" 
                Padding="10,8"/>
    </StackPanel>
</Window>
```

## Example 2: Adding Repair Service to UI

### ViewModel
```csharp
public class RepairServiceViewModel : BaseViewModel
{
    public ObservableCollection<string> RepairTypes { get; }
    public ObservableCollection<decimal> ComplexityFactors { get; }
    
    private string _selectedRepairType = "ComputerRepair";
    public string SelectedRepairType
    {
        get => _selectedRepairType;
        set { _selectedRepairType = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalCost)); }
    }
    
    private int _laborMinutes = 60;
    public int LaborMinutes
    {
        get => _laborMinutes;
        set { _laborMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalCost)); }
    }
    
    private decimal _complexityFactor = 1.0m;
    public decimal ComplexityFactor
    {
        get => _complexityFactor;
        set { _complexityFactor = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalCost)); }
    }
    
    public decimal TotalCost
    {
        get => PricingHelper.CalculateRepairCost(_selectedRepairType, _laborMinutes, _complexityFactor);
    }
    
    public RelayCommand AddToPosCommand { get; }
    
    private PosViewModel _posViewModel;
    
    public RepairServiceViewModel(PosViewModel posViewModel)
    {
        _posViewModel = posViewModel;
        
        RepairTypes = new ObservableCollection<string> { "ComputerRepair", "PrinterRepair" };
        ComplexityFactors = new ObservableCollection<decimal> { 1.0m, 1.5m, 2.0m };
        
        AddToPosCommand = new RelayCommand(_ => AddToPOS());
    }
    
    private void AddToPOS()
    {
        _posViewModel.AddRepairService(_selectedRepairType, _laborMinutes, _complexityFactor, TotalCost);
        // Close dialog
    }
}
```

### XAML View
```xaml
<Window x:Class="DestinyPOS2026.Wpf.Views.RepairServiceWindow"
        Title="Add Repair Service" Width="450" Height="350">
    <StackPanel Padding="20">
        <TextBlock Text="Repair Type:" Margin="0,0,0,5"/>
        <ComboBox ItemsSource="{Binding RepairTypes}" 
                  SelectedItem="{Binding SelectedRepairType}" 
                  Margin="0,0,0,15"/>
        
        <TextBlock Text="Labor Time (minutes):" Margin="0,0,0,5"/>
        <TextBox Text="{Binding LaborMinutes, UpdateSourceTrigger=PropertyChanged}" 
                 Margin="0,0,0,15"/>
        
        <TextBlock Text="Complexity Factor:" Margin="0,0,0,5"/>
        <Grid Margin="0,0,0,15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <ComboBox Grid.Column="0" 
                      ItemsSource="{Binding ComplexityFactors}" 
                      SelectedItem="{Binding ComplexityFactor}"/>
            <StackPanel Grid.Column="1" Margin="10,0,0,0" VerticalAlignment="Center">
                <TextBlock Text="1.0x = Normal" FontSize="10"/>
                <TextBlock Text="1.5x = Complex" FontSize="10"/>
                <TextBlock Text="2.0x = Very Complex" FontSize="10"/>
            </StackPanel>
        </Grid>
        
        <Border BorderThickness="0,1,0,0" BorderBrush="LightGray" Padding="0,10,0,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Text="Total Cost:" VerticalAlignment="Center" FontSize="14" FontWeight="Bold"/>
                <TextBlock Grid.Column="1" 
                           Text="{Binding TotalCost, StringFormat='₱{0:N2}'}" 
                           FontSize="18" FontWeight="Bold" Foreground="Green"/>
            </Grid>
        </Border>
        
        <Button Command="{Binding AddToPosCommand}" 
                Content="Add to Sale" 
                Padding="10,10"
                Margin="0,15,0,0"
                FontSize="14"/>
    </StackPanel>
</Window>
```

## Example 3: Inventory Management View

### ViewModel
```csharp
public class InventoryViewModel : BaseViewModel
{
    private ObservableCollection<InventoryItem> _inventoryItems;
    public ObservableCollection<InventoryItem> InventoryItems
    {
        get => _inventoryItems;
        set { _inventoryItems = value; OnPropertyChanged(); }
    }
    
    private ObservableCollection<InventoryItem> _lowStockItems;
    public ObservableCollection<InventoryItem> LowStockItems
    {
        get => _lowStockItems;
        set { _lowStockItems = value; OnPropertyChanged(); }
    }
    
    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddItemCommand { get; }
    public RelayCommand EditItemCommand { get; }
    
    public InventoryViewModel()
    {
        InventoryItems = new ObservableCollection<InventoryItem>();
        LowStockItems = new ObservableCollection<InventoryItem>();
        
        RefreshCommand = new RelayCommand(_ => RefreshInventory());
        AddItemCommand = new RelayCommand(_ => AddNewItem());
        EditItemCommand = new RelayCommand(item => EditItem((InventoryItem)item));
        
        RefreshInventory();
    }
    
    private void RefreshInventory()
    {
        var items = InventoryHelper.GetAllInventoryItems();
        InventoryItems = new ObservableCollection<InventoryItem>(items);
        
        var lowStock = InventoryHelper.GetLowStockItems();
        LowStockItems = new ObservableCollection<InventoryItem>(lowStock);
    }
    
    private void AddNewItem()
    {
        var dialog = new InventoryItemDialog();
        if (dialog.ShowDialog() == true)
        {
            InventoryHelper.AddInventoryItem(dialog.Item);
            RefreshInventory();
        }
    }
    
    private void EditItem(InventoryItem item)
    {
        var dialog = new InventoryItemDialog(item);
        if (dialog.ShowDialog() == true)
        {
            InventoryHelper.UpdateInventoryItem(dialog.Item);
            RefreshInventory();
        }
    }
}
```

## Example 4: Daily Sales Report

### ViewModel
```csharp
public class SalesReportViewModel : BaseViewModel
{
    private DateTime _selectedDate = DateTime.Today;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set 
        { 
            _selectedDate = value; 
            OnPropertyChanged(); 
            RefreshReport();
        }
    }
    
    private decimal _dailyTotal;
    public decimal DailyTotal
    {
        get => _dailyTotal;
        set { _dailyTotal = value; OnPropertyChanged(); }
    }
    
    private ObservableCollection<Transaction> _transactions;
    public ObservableCollection<Transaction> Transactions
    {
        get => _transactions;
        set { _transactions = value; OnPropertyChanged(); }
    }
    
    private Dictionary<string, decimal> _salesBreakdown;
    public Dictionary<string, decimal> SalesBreakdown
    {
        get => _salesBreakdown;
        set { _salesBreakdown = value; OnPropertyChanged(); }
    }
    
    private Dictionary<string, decimal> _paymentBreakdown;
    public Dictionary<string, decimal> PaymentBreakdown
    {
        get => _paymentBreakdown;
        set { _paymentBreakdown = value; OnPropertyChanged(); }
    }
    
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ExportCommand { get; }
    
    public SalesReportViewModel()
    {
        RefreshCommand = new RelayCommand(_ => RefreshReport());
        ExportCommand = new RelayCommand(_ => ExportToExcel());
        
        RefreshReport();
    }
    
    private void RefreshReport()
    {
        DailyTotal = SalesReportHelper.GetDailySalesTotal(_selectedDate);
        
        var transactions = SalesReportHelper.GetTransactionsByDate(_selectedDate);
        Transactions = new ObservableCollection<Transaction>(transactions);
        
        SalesBreakdown = SalesReportHelper.GetSalesBreakdown(_selectedDate);
        PaymentBreakdown = SalesReportHelper.GetPaymentMethodBreakdown(_selectedDate);
    }
    
    private void ExportToExcel()
    {
        var saveDialog = new SaveFileDialog 
        { 
            FileName = $"DailyReport_{_selectedDate:yyyy-MM-dd}.xlsx",
            Filter = "Excel Files|*.xlsx"
        };
        
        if (saveDialog.ShowDialog() == true)
        {
            // Export logic here
            MessageBox.Show("Report exported successfully!");
        }
    }
}
```

## Example 5: Calling from App.xaml.cs

```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize all systems
        InventoryHelper.InitializeInventoryFile();
        SalesReportHelper.InitializeSalesReportFile();
        DatabaseHelper.InitializeDatabase();
        
        // Optional: Load sample data on first run
        // SampleDataHelper.InitializeSampleData();
    }
}
```

## Example 6: Low Stock Alert Handler

```csharp
public void CheckLowStockAndNotify()
{
    var lowStockItems = InventoryHelper.GetLowStockItems();
    
    if (lowStockItems.Count > 0)
    {
        var message = "Low Stock Alert:\n\n";
        foreach (var item in lowStockItems)
        {
            message += $"• {item.ProductName}\n";
            message += $"  Current: {item.CurrentStock} (Reorder: {item.ReorderLevel})\n";
            message += $"  Supplier: {item.Supplier}\n\n";
        }
        
        MessageBox.Show(message, "Inventory Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

// Call at startup or on demand
CheckLowStockAndNotify();
```
