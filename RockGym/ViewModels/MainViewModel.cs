using RockGym.Models;
using System.Windows;
using System.Windows.Input;

namespace RockGym.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object? _currentViewModel;
        private string _loggedInUserName = string.Empty;
        private readonly User _currentUser;

        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        public string LoggedInUserName
        {
            get => _loggedInUserName;
            set
            {
                _loggedInUserName = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdmin => _currentUser.RoleId == 1;

        public ICommand ShowCustomersCommand { get; }
        public ICommand ShowTransactionsCommand { get; }
        public ICommand ShowOffersCommand { get; }
        public ICommand ShowEventsCommand { get; }
        public ICommand ShowNotificationsCommand { get; }
        public ICommand ShowSummaryCommand { get; }
        public ICommand OpenScannerCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel(User user)
        {
            _currentUser = user;
            LoggedInUserName = $"{user.Name} {user.Surname}";

            ShowCustomersCommand = new RelayCommand(o => ShowCustomers());
            ShowTransactionsCommand = new RelayCommand(o => ShowTransactions());
            ShowOffersCommand = new RelayCommand(o => ShowOffers());
            ShowEventsCommand = new RelayCommand(o => ShowEvents());
            ShowNotificationsCommand = new RelayCommand(o => ShowNotifications());
            ShowSummaryCommand = new RelayCommand(o => ShowSummary());
            OpenScannerCommand = new RelayCommand(o => OpenScanner());
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        private void ShowCustomers()
        {
            CurrentViewModel = new CustomersViewModel(_currentUser);
        }

        private void ShowTransactions()
        {
            CurrentViewModel = new TransactionsViewModel(_currentUser);
        }

        private void ShowOffers()
        {
            CurrentViewModel = new OffersViewModel(_currentUser);
        }

        private void ShowEvents()
        {
            CurrentViewModel = new EventsViewModel(_currentUser);
        }

        private void ShowNotifications()
        {
            CurrentViewModel = new NotificationsViewModel(_currentUser);
        }

        private void ShowSummary()
        {
            CurrentViewModel = new SummaryViewModel();
        }

        private RockGym.Views.QrScannerWindow? _activeScannerWindow;

        private void OpenScanner()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Jeśli skaner jest już otwarty, przywróć go i przenieś na pierwszy plan
                if (_activeScannerWindow != null)
                {
                    if (_activeScannerWindow.WindowState == WindowState.Minimized)
                    {
                        _activeScannerWindow.WindowState = WindowState.Normal;
                    }
                    _activeScannerWindow.Activate();
                    _activeScannerWindow.Focus();
                    return;
                }

                // Inicjalizacja nowego okna skanera i podpięcie resetu referencji przy zamknięciu
                _activeScannerWindow = new RockGym.Views.QrScannerWindow();
                _activeScannerWindow.Owner = Application.Current.MainWindow;
                _activeScannerWindow.Closed += (sender, e) => _activeScannerWindow = null;
                _activeScannerWindow.Show();
            });
        }

        private void ExecuteLogout(object? parameter)
        {
            if (parameter is Window currentWindow)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Zamknij okno skanera przy wylogowywaniu
                    if (_activeScannerWindow != null)
                    {
                        _activeScannerWindow.Close();
                        _activeScannerWindow = null;
                    }

                    var loginWindow = new RockGym.Views.LoginWindow();
                    loginWindow.Show();

                    Application.Current.MainWindow = loginWindow;

                    currentWindow.Close();
                });
            }
        }
    }

}
