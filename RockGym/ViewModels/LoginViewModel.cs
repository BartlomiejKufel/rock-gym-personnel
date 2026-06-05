using RockGym.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RockGym.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _login = string.Empty;
        private string _errorMessage = string.Empty;

        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        private bool CanExecuteLogin(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Login);
        }

        private void ExecuteLogin(object? parameter)
        {
            ErrorMessage = string.Empty;

            if (parameter is not PasswordBox passwordBox)
            {
                ErrorMessage = "Błąd systemowy: brak dostępu do pola hasła.";
                return;
            }

            string enteredPassword = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(Login))
            {
                ErrorMessage = "Wprowadź nazwę użytkownika.";
                return;
            }

            if (string.IsNullOrEmpty(enteredPassword))
            {
                ErrorMessage = "Wprowadź hasło.";
                return;
            }

            try
            {
                using (var context = new RockGym.Services.RockGymContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Login == Login);

                    if (user == null)
                    {
                        ErrorMessage = "W bazie nie ma takiego użytkownika.";
                        return;
                    }

                    if (!BCrypt.Net.BCrypt.Verify(enteredPassword, user.Password))
                    {
                        ErrorMessage = "Błędne hasło.";
                        return;
                    }

                    if (user.RoleId != 1 && user.RoleId != 2)
                    {
                        ErrorMessage = "Dany użytkownik nie jest pracownikiem.";
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = new RockGym.MainWindow(user);
                        mainWindow.Show();

                        Application.Current.MainWindow = mainWindow;

                        var loginWindow = Window.GetWindow(passwordBox);
                        loginWindow?.Close();
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Błąd bazy danych: {ex.Message}";
            }
        }
    }
}
