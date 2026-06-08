using Microsoft.EntityFrameworkCore;
using RockGym.Models;
using RockGym.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QRCoder;

namespace RockGym.ViewModels
{
    public class CustomersViewModel : ViewModelBase
    {
        private readonly User _currentUser;
        private List<UserDisplayModel> _allCustomers = new();
        private ObservableCollection<UserDisplayModel> _customers = new();
        private string _searchText = string.Empty;
        private bool _isEmpty;

        public ObservableCollection<UserDisplayModel> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged();
                UpdateIsEmpty();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                _isEmpty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowTable));
            }
        }

        public bool ShowTable => !IsEmpty;
        public bool IsAdmin => _currentUser.RoleId == 1;

        public ICommand AddCustomerCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand GenerateQrCodeCommand { get; }

        public CustomersViewModel(User currentUser)
        {
            _currentUser = currentUser;

            AddCustomerCommand = new RelayCommand(o => ExecuteAddCustomer());
            EditCustomerCommand = new RelayCommand(ExecuteEditCustomer);
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer);
            GenerateQrCodeCommand = new RelayCommand(ExecuteGenerateQrCode);

            LoadCustomers();
        }

        private void LoadCustomers()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var users = context.Users
                        .Include(u => u.Role)
                        .Include(u => u.Entrances)
                        .Include(u => u.CustomerPurchases)
                            .ThenInclude(p => p.Offer)
                        .ToList();

                    _allCustomers = users.Select(u => new UserDisplayModel(u, _currentUser)).ToList();
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas pobierania użytkowników: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Customers = new ObservableCollection<UserDisplayModel>(_allCustomers);
            }
            else
            {
                string query = SearchText.ToLower().Trim();
                var filtered = _allCustomers.Where(u =>
                    u.Name.ToLower().Contains(query) ||
                    u.Surname.ToLower().Contains(query) ||
                    u.FullName.ToLower().Contains(query)
                ).ToList();
                Customers = new ObservableCollection<UserDisplayModel>(filtered);
            }
        }

        private void UpdateIsEmpty()
        {
            IsEmpty = Customers.Count == 0;
        }

        private void ExecuteAddCustomer()
        {
            var newUser = new User();
            var editWindow = new RockGym.Views.UserEditWindow(_currentUser, newUser, "Dodaj użytkownika");
            
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Users.Add(newUser);
                        context.SaveChanges();
                    }
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd dodawania użytkownika: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditCustomer(object? parameter)
        {
            if (parameter is not UserDisplayModel displayModel) return;

            if (displayModel.RoleId == 1 && _currentUser.RoleId != 1)
            {
                CustomMessageBox.Show("Tylko administratorzy mogą edytować konta innych administratorów.", "Brak uprawnień", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RockGymContext())
                {
                    var dbUser = context.Users.Find(displayModel.UserId);
                    if (dbUser == null) return;

                    var tempUser = new User
                    {
                        UserId = dbUser.UserId,
                        Name = dbUser.Name,
                        Surname = dbUser.Surname,
                        Email = dbUser.Email,
                        Login = dbUser.Login,
                        Password = dbUser.Password,
                        RoleId = dbUser.RoleId,
                        DateOfBirth = dbUser.DateOfBirth,
                        ProfilePicture = dbUser.ProfilePicture
                    };

                    var editWindow = new RockGym.Views.UserEditWindow(_currentUser, tempUser, "Edytuj użytkownika");
                    if (editWindow.ShowDialog() == true)
                    {
                        dbUser.Name = tempUser.Name;
                        dbUser.Surname = tempUser.Surname;
                        dbUser.Email = tempUser.Email;
                        dbUser.Login = tempUser.Login;
                        dbUser.RoleId = tempUser.RoleId;
                        if (tempUser.Password != dbUser.Password)
                        {
                            dbUser.Password = tempUser.Password;
                        }
                        context.SaveChanges();
                        LoadCustomers();
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd edycji użytkownika: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                $"Czy na pewno chcesz bezpowrotnie usunąć użytkownika {displayModel.FullName}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

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

        private void ExecuteGenerateQrCode(object? parameter)
        {
            if (parameter is not UserDisplayModel displayModel) return;

            var result = CustomMessageBox.Show(
                $"Czy chcesz wygenerować nowy kod QR dla użytkownika {displayModel.FullName}?",
                "Potwierdzenie generowania",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string qrText = $"ROCKGYM_{displayModel.UserId}";
                    byte[] qrBytes;

                    using (var qrGenerator = new QRCodeGenerator())
                    {
                        using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
                        {
                            using (var qrCode = new PngByteQRCode(qrCodeData))
                            {
                                qrBytes = qrCode.GetGraphic(20);
                            }
                        }
                    }

                    using (var context = new RockGymContext())
                    {
                        var existingCard = context.QrCards.FirstOrDefault(c => c.UserId == displayModel.UserId);
                        if (existingCard != null)
                        {
                            existingCard.QrCode = qrBytes;
                            existingCard.DateOfCreation = DateTime.Now;
                            context.QrCards.Update(existingCard);
                        }
                        else
                        {
                            var newCard = new QrCard
                            {
                                UserId = displayModel.UserId,
                                QrCode = qrBytes,
                                DateOfCreation = DateTime.Now
                            };
                            context.QrCards.Add(newCard);
                        }
                        context.SaveChanges();
                    }

                    CustomMessageBox.Show($"Pomyślnie wygenerowano nowy kod QR dla {displayModel.FullName}.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd generowania kodu QR: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class UserDisplayModel
    {
        public User User { get; }
        public ulong UserId => User.UserId;
        public string Name => User.Name;
        public string Surname => User.Surname;
        public string FullName => User.FullName;
        public string Email => User.Email;
        public string RoleName => User.Role?.Name ?? "Brak";
        public ulong RoleId => User.RoleId;
        public byte[]? ProfilePicture => User.ProfilePicture;

        public bool IsActive { get; }
        public string Status => IsActive ? "Aktywny" : "Nieaktywny";

        public int ActiveOffersCount => ActivePurchases.Count;
        public List<ActivePurchaseDisplayModel> ActivePurchases { get; }

        public bool CanEdit { get; }

        public UserDisplayModel(User user, User loggedInUser)
        {
            User = user;

            IsActive = user.Entrances.Any(e =>
                e.DateOfEntry.Date == DateTime.Today &&
                e.StartTime <= DateTime.Now.TimeOfDay &&
                e.EndTime == null);

            ActivePurchases = user.CustomerPurchases
                .Where(p => p.Offer != null && p.Offer.Duration != 0 && p.PurchaseDate.AddDays(p.Offer.Duration) >= DateTime.Now)
                .Select(p => new ActivePurchaseDisplayModel(p))
                .ToList();

            CanEdit = (loggedInUser.RoleId == 1) || (user.RoleId != 1);
        }
    }

    public class ActivePurchaseDisplayModel
    {
        public string OfferName { get; }
        public DateTime PurchaseDate { get; }
        public DateTime ExpirationDate { get; }

        public string FormattedPurchaseDate => PurchaseDate.ToString("dd.MM.yyyy");
        public string FormattedExpirationDate => ExpirationDate.ToString("dd.MM.yyyy");

        public ActivePurchaseDisplayModel(PurchaseHistory purchase)
        {
            OfferName = purchase.Offer?.Name ?? "Brak";
            PurchaseDate = purchase.PurchaseDate;
            ExpirationDate = purchase.PurchaseDate.AddDays(purchase.Offer?.Duration ?? 0);
        }
    }
}
