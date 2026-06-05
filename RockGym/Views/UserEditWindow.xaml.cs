using System;
using System.Linq;
using System.Windows;
using RockGym.Models;
using RockGym.Services;

namespace RockGym.Views
{
    public partial class UserEditWindow : Window
    {
        private readonly User _loggedInUser;
        private readonly User _userToEdit;

        public UserEditWindow(User loggedInUser, User userToEdit, string title)
        {
            InitializeComponent();
            _loggedInUser = loggedInUser;
            _userToEdit = userToEdit;
            HeaderTextBlock.Text = title;

            NameTextBox.Text = _userToEdit.Name;
            SurnameTextBox.Text = _userToEdit.Surname;
            EmailTextBox.Text = _userToEdit.Email;
            LoginTextBox.Text = _userToEdit.Login;

            if (_userToEdit.UserId == 0)
            {
                PasswordLabelTextBlock.Text = "HASŁO (WYMAGANE)";
            }
            else
            {
                PasswordLabelTextBlock.Text = "HASŁO (POZOSTAW PUSTE, ABY NIE ZMIENIAĆ)";
            }

            LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var allRoles = context.Roles.OrderBy(r => r.RoleId).ToList();

                    if (_loggedInUser.RoleId == 2)
                    {
                        var filteredRoles = allRoles.Where(r => r.RoleId == 3 || r.RoleId == 4).ToList();
                        RoleComboBox.ItemsSource = filteredRoles;
                    }
                    else
                    {
                        RoleComboBox.ItemsSource = allRoles;
                    }

                    if (_userToEdit.UserId != 0)
                    {
                        RoleComboBox.SelectedValue = _userToEdit.RoleId;
                    }
                    else
                    {
                        RoleComboBox.SelectedValue = (ulong)4;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas ładowania ról: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string surname = SurnameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string login = LoginTextBox.Text.Trim();
            string password = PasswordInput.Password;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(surname))
            {
                CustomMessageBox.Show("Imię i nazwisko nie mogą być puste.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
            {
                CustomMessageBox.Show("Podaj poprawny adres e-mail.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(login))
            {
                CustomMessageBox.Show("Login nie może być pusty.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_userToEdit.UserId == 0 && string.IsNullOrEmpty(password))
            {
                CustomMessageBox.Show("Hasło jest wymagane dla nowego użytkownika.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RoleComboBox.SelectedValue == null)
            {
                CustomMessageBox.Show("Wybierz rolę użytkownika.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ulong selectedRoleId = (ulong)RoleComboBox.SelectedValue;

            try
            {
                using (var context = new RockGymContext())
                {
                    bool loginExists = context.Users.Any(u => u.Login == login && u.UserId != _userToEdit.UserId);
                    if (loginExists)
                    {
                        CustomMessageBox.Show($"Login \"{login}\" jest już zajęty przez innego użytkownika.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd walidacji unikalności loginu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _userToEdit.Name = name;
            _userToEdit.Surname = surname;
            _userToEdit.Email = email;
            _userToEdit.Login = login;
            _userToEdit.RoleId = selectedRoleId;

            if (!string.IsNullOrEmpty(password))
            {
                _userToEdit.Password = BCrypt.Net.BCrypt.HashPassword(password);
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
