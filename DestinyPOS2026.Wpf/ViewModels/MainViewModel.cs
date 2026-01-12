using DestinyPOS2026.Wpf.Helpers;

namespace DestinyPOS2026.Wpf.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentView = null!;

    public BaseViewModel CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    // Commands for sidebar
    public RelayCommand ShowPosCommand { get; }
    public RelayCommand ShowInventoryCommand { get; }
    public RelayCommand ShowReportsCommand { get; }

    public MainViewModel()
    {
        // Start with POS
        CurrentView = new PosViewModel();

        // Commands
        ShowPosCommand = new RelayCommand(_ => CurrentView = new PosViewModel());
        ShowInventoryCommand = new RelayCommand(_ => CurrentView = new InventoryViewModel());
        ShowReportsCommand = new RelayCommand(_ => CurrentView = new ReportsViewModel());
    }
}
