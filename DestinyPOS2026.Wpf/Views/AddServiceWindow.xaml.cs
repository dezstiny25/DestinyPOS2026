using System.Windows;

namespace DestinyPOS2026.Wpf.Views;

/// <summary>
/// Modal window for adding services to a transaction
/// Captures: Service Name, Labor Cost, and optional Notes
/// </summary>
public partial class AddServiceWindow : Window
{
    public string ServiceName { get; private set; } = string.Empty;
    public decimal LaborPrice { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    public AddServiceWindow()
    {
        InitializeComponent();
        
        // Set focus to first input
        ServiceNameTextBox.Focus();
    }

    /// <summary>
    /// Validates and saves the service details
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate Service Name
        if (string.IsNullOrWhiteSpace(ServiceNameTextBox.Text))
        {
            MessageBox.Show("Please enter a service name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ServiceNameTextBox.Focus();
            return;
        }

        // Validate Labor Price
        if (!decimal.TryParse(LaborPriceTextBox.Text, out var price) || price <= 0)
        {
            MessageBox.Show("Please enter a valid labor cost (must be greater than 0).", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            LaborPriceTextBox.Focus();
            LaborPriceTextBox.SelectAll();
            return;
        }

        // Set properties
        ServiceName = ServiceNameTextBox.Text.Trim();
        LaborPrice = price;
        Notes = NotesTextBox.Text.Trim();

        // Return OK result
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Closes the window without saving
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Allows Enter key to save when in text boxes
    /// </summary>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && SaveButton.IsVisible)
        {
            SaveButton_Click(null, null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelButton_Click(null, null);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}
