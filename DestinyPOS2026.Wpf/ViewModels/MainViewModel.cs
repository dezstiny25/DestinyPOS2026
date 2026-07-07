using DestinyPOS2026.Wpf.Helpers;
using System;
using System.Threading.Tasks;

namespace DestinyPOS2026.Wpf.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentView = null!;
    private string _serverStatus = string.Empty;

    public BaseViewModel CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public string ServerStatus
    {
        get => _serverStatus;
        set { _serverStatus = value; OnPropertyChanged(); }
    }

    // Commands for sidebar
    public RelayCommand ShowPosCommand { get; }
    public RelayCommand ShowInventoryCommand { get; }
    public RelayCommand ShowReportsCommand { get; }

    public MainViewModel()
    {
        // Subscribe to pairing server status updates and marshal to UI thread
        PairingServerHelper.StatusChanged += s =>
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => ServerStatus = s);
            }
            catch
            {
                ServerStatus = s;
            }
        };

        // Start with POS
        CurrentView = new PosViewModel();

        // Start pairing server on app launch
        Task.Run(async () =>
        {
            try
            {
                await PairingServerHelper.StartAsync();
            }
            catch (Exception ex)
            {
                ServerStatus = $"Pairing server error: {ex.Message}";
            }
        });

        // Commands
        ShowPosCommand = new RelayCommand(_ => CurrentView = new PosViewModel());
        ShowInventoryCommand = new RelayCommand(_ => CurrentView = new InventoryViewModel());
        ShowReportsCommand = new RelayCommand(_ => CurrentView = new ReportsViewModel());
    }
}
