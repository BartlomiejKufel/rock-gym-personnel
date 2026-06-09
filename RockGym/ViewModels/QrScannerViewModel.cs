using Microsoft.EntityFrameworkCore;
using RockGym.Models;
using RockGym.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace RockGym.ViewModels
{
    public class QrScannerViewModel : ViewModelBase
    {
        private readonly ZXing.Windows.Compatibility.BarcodeReader _barcodeReader;

        // Stan skanowania
        private bool _isScanningPaused;
        public bool IsScanningPaused
        {
            get => _isScanningPaused;
            set
            {
                if (_isScanningPaused != value)
                {
                    _isScanningPaused = value;
                    OnPropertyChanged();
                }
            }
        }

        // Stan widoczności paneli
        private Visibility _emptyCardVisibility = Visibility.Visible;
        public Visibility EmptyCardVisibility
        {
            get => _emptyCardVisibility;
            set { _emptyCardVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _clientCardVisibility = Visibility.Collapsed;
        public Visibility ClientCardVisibility
        {
            get => _clientCardVisibility;
            set { _clientCardVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _resumeButtonVisibility = Visibility.Collapsed;
        public Visibility ResumeButtonVisibility
        {
            get => _resumeButtonVisibility;
            set { _resumeButtonVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _pauseOverlayVisibility = Visibility.Collapsed;
        public Visibility PauseOverlayVisibility
        {
            get => _pauseOverlayVisibility;
            set { _pauseOverlayVisibility = value; OnPropertyChanged(); }
        }

        // Teksty statusów wstrzymania
        private string _pauseStatusText = "ZESKANOWANO POPRAWNIE";
        public string PauseStatusText
        {
            get => _pauseStatusText;
            set { _pauseStatusText = value; OnPropertyChanged(); }
        }

        private string _pauseSubStatusText = "Skanowanie wstrzymane";
        public string PauseSubStatusText
        {
            get => _pauseSubStatusText;
            set { _pauseSubStatusText = value; OnPropertyChanged(); }
        }

        private Brush _pauseStatusForeground = new SolidColorBrush(Color.FromRgb(72, 187, 120)); // Domyślnie zielony
        public Brush PauseStatusForeground
        {
            get => _pauseStatusForeground;
            set { _pauseStatusForeground = value; OnPropertyChanged(); }
        }

        // Dane zalogowanego / zeskanowanego klienta
        private string _clientName = string.Empty;
        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); }
        }

        private string _clientRole = "klient";
        public string ClientRole
        {
            get => _clientRole;
            set { _clientRole = value; OnPropertyChanged(); }
        }

        private Brush _clientRoleBackground = System.Windows.Media.Brushes.Transparent;
        public Brush ClientRoleBackground
        {
            get => _clientRoleBackground;
            set { _clientRoleBackground = value; OnPropertyChanged(); }
        }

        private Brush _clientRoleBorderBrush = System.Windows.Media.Brushes.Transparent;
        public Brush ClientRoleBorderBrush
        {
            get => _clientRoleBorderBrush;
            set { _clientRoleBorderBrush = value; OnPropertyChanged(); }
        }

        private Brush _clientRoleForeground = System.Windows.Media.Brushes.White;
        public Brush ClientRoleForeground
        {
            get => _clientRoleForeground;
            set { _clientRoleForeground = value; OnPropertyChanged(); }
        }

        private Brush _clientStatusBrush = System.Windows.Media.Brushes.Gray;
        public Brush ClientStatusBrush
        {
            get => _clientStatusBrush;
            set { _clientStatusBrush = value; OnPropertyChanged(); }
        }

        private string _clientStatusText = "Nieaktywny";
        public string ClientStatusText
        {
            get => _clientStatusText;
            set { _clientStatusText = value; OnPropertyChanged(); }
        }

        private byte[]? _profilePicture;
        public byte[]? ProfilePicture
        {
            get => _profilePicture;
            set { _profilePicture = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ActiveOfferItemViewModel> ActiveOffers { get; }

        // Komendy
        public ICommand ProcessCodeCommand { get; }
        public ICommand ResumeScanningCommand { get; }

        public QrScannerViewModel(ZXing.Windows.Compatibility.BarcodeReader barcodeReader)
        {
            _barcodeReader = barcodeReader;
            ActiveOffers = new ObservableCollection<ActiveOfferItemViewModel>();

            ProcessCodeCommand = new RelayCommand(o =>
            {
                if (o is string code)
                {
                    HandleScannedCode(code);
                }
            });

            ResumeScanningCommand = new RelayCommand(o => ResumeScanning());
            
            ResumeScanning();
        }

        // Przetwarza zeskanowany kod QR. Wykonuje sprawdzenie kodu, rejestruje wejście/wyjście oraz pobiera dane użytkownika.
        private void HandleScannedCode(string code)
        {
            // Wstrzymanie skanowania klatek przy przetwarzaniu i wyświetlania wyników
            IsScanningPaused = true;

            if (string.IsNullOrWhiteSpace(code))
            {
                CustomMessageBox.Show("Zeskanowano niepoprawny kod QR.", "Niepoprawny kod", MessageBoxButton.OK, MessageBoxImage.Warning);
                ResumeScanning();
                return;
            }

            ulong userId = 0;

            // Sprawdź format
            if (code.StartsWith("ROCKGYM_"))
            {
                string idPart = code.Substring(8);
                if (ulong.TryParse(idPart, out ulong parsedId))
                {
                    userId = parsedId;
                }
            }

            // Brak dopasowania użytkownika
            if (userId == 0)
            {
                PauseStatusText = "BŁĘDNY KOD QR";
                PauseStatusForeground = new SolidColorBrush(Color.FromRgb(229, 62, 62));
                PauseSubStatusText = "Nieznany użytkownik lub niepoprawny kod";
                PauseOverlayVisibility = Visibility.Visible;

                EmptyCardVisibility = Visibility.Visible;
                ClientCardVisibility = Visibility.Collapsed;
                ResumeButtonVisibility = Visibility.Visible;

                CustomMessageBox.Show("Zeskanowano nieznany kod QR lub kod nie jest przypisany do żadnego użytkownika.", "Nieznany kod", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new RockGymContext())
                {
                    // Rejestracja wejścia lub wyjścia
                    // Wyszukujemy otwarty wpis (EndTime == null) na dzisiejszy dzień dla tego użytkownika
                    var todayEntry = context.Entrances
                        .FirstOrDefault(e => e.UserId == userId && e.DateOfEntry.Date == DateTime.Today && e.EndTime == null);

                    if (todayEntry == null)
                    {
                        // Jeśli nie ma aktywnego wpisu, rejestrujemy nowe wejście
                        var newEntry = new Entrance
                        {
                            UserId = userId,
                            DateOfEntry = DateTime.Today,
                            StartTime = DateTime.Now.TimeOfDay
                        };
                        context.Entrances.Add(newEntry);
                    }
                    else
                    {
                        // Jeśli użytkownik jest już w klubie, rejestrujemy wyjście i liczymy czas pobytu
                        todayEntry.EndTime = DateTime.Now.TimeOfDay;
                        todayEntry.TimeSpent = todayEntry.EndTime.Value - todayEntry.StartTime;
                        context.Entrances.Update(todayEntry);
                    }
                    context.SaveChanges();

                    // Pobranie danych użytkownika wraz z rolą i karnetami
                    var user = context.Users
                        .Include(u => u.Role)
                        .Include(u => u.Entrances)
                        .Include(u => u.CustomerPurchases)
                            .ThenInclude(p => p.Offer)
                        .FirstOrDefault(u => u.UserId == userId);

                    if (user == null)
                    {
                        CustomMessageBox.Show("Użytkownik przypisany do tego kodu QR nie istnieje w bazie danych.", "Brak użytkownika", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResumeScanning();
                        return;
                    }

                    ClientName = $"{user.Name} {user.Surname}";
                    ClientRole = user.Role?.Name ?? "klient";
                    StyleRoleBadge(user.RoleId);

                    // Sprawdzenie statusu aktywny/nieaktywny
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
                    PauseOverlayVisibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd pobierania danych użytkownika: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                ResumeScanning();
            }
        }

        private void StyleRoleBadge(ulong roleId)
        {
            switch (roleId)
            {
                case 1: // Admin
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(58, 30, 30));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(90, 44, 44));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(255, 142, 142));
                    break;
                case 2: // Pracownik
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(26, 46, 62));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(44, 76, 102));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(142, 197, 255));
                    break;
                case 3: // Instruktor
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(45, 30, 62));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(72, 44, 102));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(216, 142, 255));
                    break;
                case 4: // Klient
                default:
                    ClientRoleBackground = new SolidColorBrush(Color.FromRgb(30, 58, 36));
                    ClientRoleBorderBrush = new SolidColorBrush(Color.FromRgb(44, 94, 53));
                    ClientRoleForeground = new SolidColorBrush(Color.FromRgb(142, 255, 157));
                    break;
            }
        }

        private void ResumeScanning()
        {
            IsScanningPaused = false;
            PauseOverlayVisibility = Visibility.Collapsed;

            PauseStatusText = "ZESKANOWANO POPRAWNIE";
            PauseStatusForeground = new SolidColorBrush(Color.FromRgb(72, 187, 120));
            PauseSubStatusText = "Skanowanie wstrzymane";

            EmptyCardVisibility = Visibility.Visible;
            ClientCardVisibility = Visibility.Collapsed;
            ResumeButtonVisibility = Visibility.Collapsed;
        }

        }

    public class ActiveOfferItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string ValidityText { get; set; } = string.Empty;
    }
}
