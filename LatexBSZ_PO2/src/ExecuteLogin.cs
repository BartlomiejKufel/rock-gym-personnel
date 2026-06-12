private void ExecuteLogin(object? parameter)
{
    ErrorMessage = string.Empty;

    if (parameter is not PasswordBox passwordBox)
    {
        ErrorMessage = "Błąd systemowy: brak dostępu do pola hasła."; return;
    }

    string enteredPassword = passwordBox.Password;

    if (string.IsNullOrWhiteSpace(Login))
    {
        ErrorMessage = "Wprowadź nazwę użytkownika."; return;
    }

    if (string.IsNullOrEmpty(enteredPassword))
    {
        ErrorMessage = "Wprowadź hasło."; return;
    }

    try
    {
        using (var context = new RockGym.Services.RockGymContext())
        {
            var user = context.Users.FirstOrDefault(u => u.Login == Login);

            if (user == null)
            {
                ErrorMessage = "W bazie nie ma takiego użytkownika."; return;
            }

            if (!BCrypt.Net.BCrypt.Verify(enteredPassword, user.Password))
            {
                ErrorMessage = "Błędne hasło."; return;
            }

            if (user.RoleId != 1 && user.RoleId != 2)
            {
                ErrorMessage = "Dany użytkownik nie jest pracownikiem."; return;
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