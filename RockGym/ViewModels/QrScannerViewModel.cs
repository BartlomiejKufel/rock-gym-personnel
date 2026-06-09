using Microsoft.EntityFrameworkCore;
using RockGym.Models;
using RockGym.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
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
        private static readonly ConcurrentDictionary<string, ulong> _qrCodeCache =
            new ConcurrentDictionary<string, ulong>();

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
        }        /// <summary>
        /// Przetwarza zeskanowaną wartość kodu QR (z kamery lub pliku graficznego).
        /// Wykonuje autoryzację kodu, rejestruje wejście/wyjście oraz pobiera dane użytkownika.
        /// </summary>
        /// <param name="code">Zeskanowany ciąg tekstowy z kodu QR</param>
        private void HandleScannedCode(string code)
        {
            // Wstrzymaj skanowanie klatek na czas przetwarzania i wyświetlania wyniku
            IsScanningPaused = true;

            if (string.IsNullOrWhiteSpace(code))
            {
                CustomMessageBox.Show("Zeskanowano niepoprawny kod QR.", "Niepoprawny kod", MessageBoxButton.OK, MessageBoxImage.Warning);
                ResumeScanning();
                return;
            }

            ulong userId = 0;
            bool isDirectMatch = false;

            // KROK 1: Sprawdź format bezpośredni "ROCKGYM_{UserId}" (nowy standard)
            if (code.StartsWith("ROCKGYM_"))
            {
                string idPart = code.Substring(8);
                if (ulong.TryParse(idPart, out ulong parsedId))
                {
                    userId = parsedId;
                    isDirectMatch = true;
                }
            }

            // KROK 2: Jeśli brak dopasowania bezpośredniego, przeszukaj stare karty (legacy GUIDs).
            // Wykorzystywany jest statyczny słownik podręczny (cache) w celu uniknięcia kosztownego 
            // dekodowania grafik kodów QR w pętli przy każdym skanowaniu.
            if (!isDirectMatch)
            {
                if (_qrCodeCache.TryGetValue(code, out ulong cachedId))
                {
                    userId = cachedId;
                }
                else
                {
                    try
                    {
                        using (var context = new RockGymContext())
                        {
                            // Pobierz wszystkie obrazy kart z bazy danych
                            var qrCards = context.QrCards.Select(c => new { c.UserId, c.QrCode }).ToList();
                            foreach (var card in qrCards)
                            {
                                if (card.QrCode != null && card.QrCode.Length > 0)
                                {
                                    // Dekoduj BLOB graficzny w pamięci
                                    string? decodedDbCode = DecodeQrCodeBytes(card.QrCode);
                                    if (decodedDbCode != null)
                                    {
                                        // Zapisz w cache
                                        _qrCodeCache[decodedDbCode] = card.UserId;
                                        if (decodedDbCode == code)
                                        {
                                            userId = card.UserId;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Błąd odczytu bazy kodów QR: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        ResumeScanning();
                        return;
                    }
                }
            }

            // Obsługa braku dopasowania użytkownika
            if (userId == 0)
            {
                CustomMessageBox.Show("Zeskanowano nieznany kod QR lub kod nie jest przypisany do żadnego użytkownika.", "Nieznany kod", MessageBoxButton.OK, MessageBoxImage.Warning);
                ResumeScanning();
                return;
            }

            try
            {
                using (var context = new RockGymContext())
                {
                    // KROK 3: Rejestracja wejścia lub wyjścia (Check-in / Check-out).
                    // Wyszukujemy otwarty wpis (EndTime == null) na dzisiejszy dzień dla tego użytkownika.
                    var todayEntry = context.Entrances
                        .FirstOrDefault(e => e.UserId == userId && e.DateOfEntry.Date == DateTime.Today && e.EndTime == null);

                    if (todayEntry == null)
                    {
                        // Jeśli brak aktywnego wpisu, rejestrujemy NOWE WEJŚCIE (Check-in)
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
                        // Jeśli użytkownik jest już w klubie, rejestrujemy WYJŚCIE (Check-out) i wyliczamy czas pobytu
                        todayEntry.EndTime = DateTime.Now.TimeOfDay;
                        todayEntry.TimeSpent = todayEntry.EndTime.Value - todayEntry.StartTime;
                        context.Entrances.Update(todayEntry);
                    }
                    context.SaveChanges();

                    // KROK 4: Pobranie zaktualizowanych danych użytkownika wraz z rolami i karnetami
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

                    // Prezentacja danych użytkownika w UI
                    ClientName = $"{user.Name} {user.Surname}";
                    ClientRole = user.Role?.Name ?? "klient";
                    StyleRoleBadge(user.RoleId);

                    // Sprawdzenie statusu obecności w klubie (czy ma otwarty wpis dzisiaj)
                    bool isActive = user.Entrances.Any(e =>
                        e.DateOfEntry.Date == DateTime.Today &&
                        e.StartTime <= DateTime.Now.TimeOfDay &&
                        e.EndTime == null);

                    if (isActive)
                    {
                        ClientStatusBrush = new SolidColorBrush(Color.FromRgb(72, 187, 120)); // Zielony (Aktywny)
                        ClientStatusText = "Aktywny";
                    }
                    else
                    {
                        ClientStatusBrush = new SolidColorBrush(Color.FromRgb(160, 174, 192)); // Szary (Nieaktywny)
                        ClientStatusText = "Nieaktywny";
                    }

                    // Przypisanie awatara
                    ProfilePicture = user.ProfilePicture;

                    // KROK 5: Pobranie i filtrowanie aktywnych karnetów (ofert o duration > 0 i niewygaśniętych)
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

                    // Przełączenie paneli widoczności w oknie skanera
                    EmptyCardVisibility = Visibility.Collapsed;
                    ClientCardVisibility = Visibility.Visible;
                    ResumeButtonVisibility = Visibility.Visible;
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
            IsScanningPaused = false;
            PauseOverlayVisibility = Visibility.Collapsed;
            EmptyCardVisibility = Visibility.Visible;
            ClientCardVisibility = Visibility.Collapsed;
            ResumeButtonVisibility = Visibility.Collapsed;
        }

        private string? DecodeQrCodeBytes(byte[] qrBytes)
        {
            try
            {
                using (var stream = new MemoryStream(qrBytes))
                {
                    using (var bitmap = new Bitmap(stream))
                    {
                        var result = _barcodeReader.Decode(bitmap);
                        return result?.Text;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

    }

    public class ActiveOfferItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string ValidityText { get; set; } = string.Empty;
    }
}
