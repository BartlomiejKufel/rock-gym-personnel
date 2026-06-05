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
        public ICommand ShowEmployeesCommand { get; }
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
            ShowEmployeesCommand = new RelayCommand(o => ShowEmployees());
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

        private void ShowEmployees()
        {
            CurrentViewModel = new EmployeesViewModel();
        }

        private void ExecuteLogout(object? parameter)
        {
            if (parameter is Window currentWindow)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var loginWindow = new RockGym.Views.LoginWindow();
                    loginWindow.Show();

                    Application.Current.MainWindow = loginWindow;

                    currentWindow.Close();
                });
            }
        }
    }

    public class EmployeesViewModel : ViewModelBase { }
}
