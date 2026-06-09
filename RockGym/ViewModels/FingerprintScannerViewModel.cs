using Microsoft.EntityFrameworkCore;
using RockGym.Models;
using RockGym.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace RockGym.ViewModels
{
    public class FingerprintScannerViewModel : ViewModelBase
    {
        private byte[]? _fingerprintPreview;
        private string _matchProbabilityText = string.Empty;
        private Brush _matchProbabilityColor = System.Windows.Media.Brushes.Gray;

        // Stan widoczności paneli
        private Visibility _emptyCardVisibility = Visibility.Visible;
        private Visibility _clientCardVisibility = Visibility.Collapsed;
        private Visibility _resumeButtonVisibility = Visibility.Collapsed;

        // Dane klienta
        private string _clientName = string.Empty;
        private string _clientRole = "klient";
        private Brush _clientRoleBackground = System.Windows.Media.Brushes.Transparent;
        private Brush _clientRoleBorderBrush = System.Windows.Media.Brushes.Transparent;
        private Brush _clientRoleForeground = System.Windows.Media.Brushes.White;
        private Brush _clientStatusBrush = System.Windows.Media.Brushes.Gray;
        private string _clientStatusText = "Nieaktywny";
        private byte[]? _profilePicture;
        private ulong _currentUserId;

        // Bindowane właściwości
        public byte[]? FingerprintPreview
        {
            get => _fingerprintPreview;
            set { _fingerprintPreview = value; OnPropertyChanged(); }
        }

        public string MatchProbabilityText
        {
            get => _matchProbabilityText;
            set { _matchProbabilityText = value; OnPropertyChanged(); }
        }

        public Brush MatchProbabilityColor
        {
            get => _matchProbabilityColor;
            set { _matchProbabilityColor = value; OnPropertyChanged(); }
        }

        public Visibility EmptyCardVisibility
        {
            get => _emptyCardVisibility;
            set { _emptyCardVisibility = value; OnPropertyChanged(); }
        }

        public Visibility ClientCardVisibility
        {
            get => _clientCardVisibility;
            set { _clientCardVisibility = value; OnPropertyChanged(); }
        }

        public Visibility ResumeButtonVisibility
        {
            get => _resumeButtonVisibility;
            set { _resumeButtonVisibility = value; OnPropertyChanged(); }
        }

        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); }
        }

        public string ClientRole
        {
            get => _clientRole;
            set { _clientRole = value; OnPropertyChanged(); }
        }

        public Brush ClientRoleBackground
        {
            get => _clientRoleBackground;
            set { _clientRoleBackground = value; OnPropertyChanged(); }
        }

        public Brush ClientRoleBorderBrush
        {
            get => _clientRoleBorderBrush;
            set { _clientRoleBorderBrush = value; OnPropertyChanged(); }
        }

        public Brush ClientRoleForeground
        {
            get => _clientRoleForeground;
            set { _clientRoleForeground = value; OnPropertyChanged(); }
        }

        public Brush ClientStatusBrush
        {
            get => _clientStatusBrush;
            set { _clientStatusBrush = value; OnPropertyChanged(); }
        }

        public string ClientStatusText
        {
            get => _clientStatusText;
            set { _clientStatusText = value; OnPropertyChanged(); }
        }

        public byte[]? ProfilePicture
        {
            get => _profilePicture;
            set { _profilePicture = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ActiveOfferItemViewModel> ActiveOffers { get; }

        public ICommand ProcessFingerprintCommand { get; }
        public ICommand RegisterEntranceCommand { get; }
        public ICommand ResumeScanningCommand { get; }

        public FingerprintScannerViewModel()
        {
            ActiveOffers = new ObservableCollection<ActiveOfferItemViewModel>();

            ProcessFingerprintCommand = new RelayCommand(o =>
            {
                if (o is byte[] imgBytes)
                {
                    HandleFingerprintProcessing(imgBytes);
                }
            });

            RegisterEntranceCommand = new RelayCommand(o => ExecuteRegisterEntrance());
            ResumeScanningCommand = new RelayCommand(o => ResumeScanning());
            
            ResumeScanning();
        }

        // Funkcja porównuje wygenerowany hasz próbki wejściowej z haszami zapisanymi w bazie danych i znajduje klienta z największym prawdopodobieństwem
        private void HandleFingerprintProcessing(byte[] imgBytes)
        {
            FingerprintPreview = imgBytes;

            // Wyliczenie haszu próbki wejściowej
            string inputHash = FingerprintHashing.CalculateAverageHash(imgBytes);

            if (string.IsNullOrEmpty(inputHash))
            {
                CustomMessageBox.Show("Nie można odczytać próbki linii papilarnych z pliku.", "Błąd odczytu", MessageBoxButton.OK, MessageBoxImage.Error);
                ResumeScanning();
                return;
            }

            try
            {
                using (var context = new RockGymContext())
                {
                    // Pobierz wszystkie zarejestrowane odciski z bazy
                    var dbFingerprints = context.Fingerprints
                        .Select(f => new { f.UserId, f.FingerprintHash })
                        .ToList();

                    if (!dbFingerprints.Any())
                    {
                        CustomMessageBox.Show("Brak jakichkolwiek zarejestrowanych wzorców odcisków palców w bazie danych.", "Baza pusta", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResumeScanning();
                        return;
                    }

                    ulong bestMatchUserId = 0;
                    double bestMatchProbability = 0.0;

                    // Porównanie z każdym wzorcem z bazy
                    foreach (var fp in dbFingerprints)
                    {
                        double probability = FingerprintHashing.CalculateMatchProbability(inputHash, fp.FingerprintHash);
                        if (probability > bestMatchProbability)
                        {
                            bestMatchProbability = probability;
                            bestMatchUserId = fp.UserId;
                        }
                    }

                    MatchProbabilityText = $"Dopasowanie: {bestMatchProbability:0.00}%";

                    // Jeśli dopasowanie jest poniżej progu 82% - odrzucamy
                    if (bestMatchProbability < 82.0 || bestMatchUserId == 0)
                    {
                        MatchProbabilityColor = new SolidColorBrush(Color.FromRgb(229, 62, 62));
                        CustomMessageBox.Show($"Nie odnaleziono pasującego użytkownika w bazie danych.\nNajlepsze dopasowanie: {bestMatchProbability:0.00}%.", "Brak dopasowania", MessageBoxButton.OK, MessageBoxImage.Warning);
                        
                        EmptyCardVisibility = Visibility.Visible;
                        ClientCardVisibility = Visibility.Collapsed;
                        ResumeButtonVisibility = Visibility.Visible;
                        return;
                    }

                    // Sukces
                    MatchProbabilityColor = new SolidColorBrush(Color.FromRgb(72, 187, 120));
                    _currentUserId = bestMatchUserId;

                    // Pobranie danych pasującego klienta
                    var user = context.Users
                        .Include(u => u.Role)
                        .Include(u => u.Entrances)
                        .Include(u => u.CustomerPurchases)
                            .ThenInclude(p => p.Offer)
                        .FirstOrDefault(u => u.UserId == bestMatchUserId);

                    if (user == null)
                    {
                        CustomMessageBox.Show("Pasujący użytkownik nie istnieje w bazie danych.", "Błąd spójności", MessageBoxButton.OK, MessageBoxImage.Error);
                        ResumeScanning();
                        return;
                    }

                    ClientName = user.FullName;
                    ClientRole = user.Role?.Name ?? "klient";
                    StyleRoleBadge(user.RoleId);

                    bool isActive = user.Entrances.Any(e =>
                        e.DateOfEntry.Date == DateTime.Today &&
                        e.StartTime <= DateTime.Now.TimeOfDay &&
                        e.EndTime == null);

                    if (isActive)
                    {
                        ClientStatusBrush = new SolidColorBrush(Color.FromRgb(72, 187, 120));
                        ClientStatusText = "Aktywny";
                    }
                    else
                    {
                        ClientStatusBrush = new SolidColorBrush(Color.FromRgb(160, 174, 192));
                        ClientStatusText = "Nieaktywny";
                    }

                    ProfilePicture = user.ProfilePicture;

                    // Pobranie aktywnych karnetów
                    ActiveOffers.Clear();
                    var activePurchases = user.CustomerPurchases
                        .Where(p => p.Offer != null && p.Offer.Duration != 0 && p.PurchaseDate.AddDays(p.Offer.Duration) >= DateTime.Now)
                        .ToList();

                    foreach (var purchase in activePurchases)
                    {
                        DateTime expirationDate = purchase.PurchaseDate.AddDays(purchase.Offer?.Duration ?? 0);
                        ActiveOffers.Add(new ActiveOfferItemViewModel
                        {
                            Name = purchase.Offer?.Name ?? "Oferta",
                            ValidityText = $"Ważny od: {purchase.PurchaseDate:dd.MM.yyyy} do: {expirationDate:dd.MM.yyyy}"
                        });
                    }

                    EmptyCardVisibility = Visibility.Collapsed;
                    ClientCardVisibility = Visibility.Visible;
                    ResumeButtonVisibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas przetwarzania odcisku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                ResumeScanning();
            }
        }

        /// <summary>
        /// Loguje wejście lub wyjście klienta w bazie danych (Check-in / Check-out).
        /// Wyzwalana po kliknięciu przycisku "Wpuść klienta".
        /// </summary>
        private void ExecuteRegisterEntrance()
        {
            if (_currentUserId == 0) return;

            try
            {
                using (var context = new RockGymContext())
                {
                    // Wyszukaj otwarte dzisiejsze wejście
                    var todayEntry = context.Entrances
                        .FirstOrDefault(e => e.UserId == _currentUserId && e.DateOfEntry.Date == DateTime.Today && e.EndTime == null);

                    if (todayEntry == null)
                    {
                        var newEntry = new Entrance
                        {
                            UserId = _currentUserId,
                            DateOfEntry = DateTime.Today,
                            StartTime = DateTime.Now.TimeOfDay
                        };
                        context.Entrances.Add(newEntry);
                        CustomMessageBox.Show("Zarejestrowano WEJŚCIE klienta.", "Wejście zarejestrowane", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        todayEntry.EndTime = DateTime.Now.TimeOfDay;
                        todayEntry.TimeSpent = todayEntry.EndTime.Value - todayEntry.StartTime;
                        context.Entrances.Update(todayEntry);
                        CustomMessageBox.Show("Zarejestrowano WYJŚCIE klienta.", "Wyjście zarejestrowane", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    context.SaveChanges();

                    // Przeładuj stan, aby odświeżyć status
                    var user = context.Users
                        .Include(u => u.Entrances)
                        .FirstOrDefault(u => u.UserId == _currentUserId);

                    if (user != null)
                    {
                        bool isActive = user.Entrances.Any(e =>
                            e.DateOfEntry.Date == DateTime.Today &&
                            e.StartTime <= DateTime.Now.TimeOfDay &&
                            e.EndTime == null);

                        if (isActive)
                        {
                            ClientStatusBrush = new SolidColorBrush(Color.FromRgb(72, 187, 120));
                            ClientStatusText = "Aktywny";
                        }
                        else
                        {
                            ClientStatusBrush = new SolidColorBrush(Color.FromRgb(160, 174, 192));
                            ClientStatusText = "Nieaktywny";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd rejestracji obecności: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StyleRoleBadge(ulong roleId)
        {
            switch (roleId)
            {
                case 1: // Admin: Czerwony
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(58, 30, 30));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(90, 44, 44));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(255, 142, 142));
                    break;
                case 2: // Pracownik: Niebieski
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(26, 46, 62));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(44, 76, 102));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(142, 197, 255));
                    break;
                case 3: // Instruktor: Fioletowy
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(45, 30, 62));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(72, 44, 102));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(216, 142, 255));
                    break;
                case 4: // Klient: Zielony
                default:
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(30, 58, 36));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(44, 94, 53));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(142, 255, 157));
                    break;
            }
        }

        private void ResumeScanning()
        {
            FingerprintPreview = null;
            MatchProbabilityText = string.Empty;
            MatchProbabilityColor = System.Windows.Media.Brushes.Gray;
            _currentUserId = 0;

            EmptyCardVisibility = Visibility.Visible;
            ClientCardVisibility = Visibility.Collapsed;
            ResumeButtonVisibility = Visibility.Collapsed;
        }
    }
}
