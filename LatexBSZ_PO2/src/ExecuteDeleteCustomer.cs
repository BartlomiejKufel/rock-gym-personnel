private void ExecuteDeleteCustomer(object? parameter)
{
    if (parameter is not UserDisplayModel displayModel) return;

    if (_currentUser.RoleId != 1)
    {
        CustomMessageBox.Show("Tylko administratorzy mogą usuwać użytkowników.", "Brak uprawnień", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    if (displayModel.UserId == _currentUser.UserId)
    {
        CustomMessageBox.Show("Nie możesz usunąć własnego konta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    var result = CustomMessageBox.Show(
        $"Czy na pewno chcesz bezpowrotnie usunąć użytkownika {displayModel.FullName}?", "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Question);

    if (result == MessageBoxResult.Yes)
    {
        try
        {
            using (var context = new RockGymContext())
            {
                var dbUser = context.Users.Find(displayModel.UserId);
                if (dbUser != null)
                {
                    context.Users.Remove(dbUser);
                    context.SaveChanges();
                }
            }
            LoadCustomers();
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Błąd usuwania użytkownika: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}