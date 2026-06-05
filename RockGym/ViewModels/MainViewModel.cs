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
            // Placeholder dla widoku Klientów
            CurrentViewModel = new CustomersViewModel();
        }

        private void ShowTransactions()
        {
            // Placeholder dla widoku Transakcji
            CurrentViewModel = new TransactionsViewModel();
        }

        private void ShowOffers()
        {
            // Placeholder dla widoku Ofert
            CurrentViewModel = new OffersViewModel();
        }

        private void ShowEvents()
        {
            // Placeholder dla widoku Wydarzeń
            CurrentViewModel = new EventsViewModel();
        }

        private void ShowNotifications()
        {
            CurrentViewModel = new NotificationsViewModel(_currentUser);
        }

        private void ShowEmployees()
        {
            // Placeholder dla widoku Pracowników
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

    // Proste klasy placeholderów dla podwidoków, aby aplikacja mogła się skompilować i obsłużyć powiązania
    public class CustomersViewModel : ViewModelBase { }
    public class TransactionsViewModel : ViewModelBase { }
    public class OffersViewModel : ViewModelBase { }
    public class EventsViewModel : ViewModelBase { }
    public class EmployeesViewModel : ViewModelBase { }
}
