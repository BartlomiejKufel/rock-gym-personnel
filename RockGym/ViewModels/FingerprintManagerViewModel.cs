using RockGym.Models;
using RockGym.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;

namespace RockGym.ViewModels
{
    public class FingerprintManagerViewModel : ViewModelBase
    {
        private readonly ulong _userId;
        private string _clientName = string.Empty;
        private ObservableCollection<FingerprintItemViewModel> _fingerprints = new();

        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FingerprintItemViewModel> Fingerprints
        {
            get => _fingerprints;
            set
            {
                _fingerprints = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAdd));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public bool CanAdd => Fingerprints.Count < 3;

        public string StatusText => $"Zarejestrowano {Fingerprints.Count} z 3 odcisków palców";

        public ICommand AddFingerprintCommand { get; }
        public ICommand DeleteFingerprintCommand { get; }

        public FingerprintManagerViewModel(ulong userId, string clientName)
        {
            _userId = userId;
            ClientName = clientName;

            AddFingerprintCommand = new RelayCommand(o => ExecuteAddFingerprint(), o => CanAdd);
            DeleteFingerprintCommand = new RelayCommand(ExecuteDeleteFingerprint);

            LoadFingerprints();
        }

        private void LoadFingerprints()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var list = context.Fingerprints
                        .Where(f => f.UserId == _userId)
                        .OrderByDescending(f => f.DateOfCreation)
                        .Select(f => new FingerprintItemViewModel
                        {
                            FingerprintId = f.FingerprintId,
                            FingerprintImage = f.FingerprintImage,
                            FingerprintHash = f.FingerprintHash,
                            DateOfCreation = f.DateOfCreation
                        })
                        .ToList();

                    Fingerprints = new ObservableCollection<FingerprintItemViewModel>(list);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd wczytywania odcisków palców: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteAddFingerprint()
        {
            if (!CanAdd) return;

            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Obrazy linii papilarnych (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Wszystkie pliki (*.*)|*.*",
                    Title = $"Wybierz próbkę odcisku palca dla {ClientName}"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    byte[] imgBytes = File.ReadAllBytes(filePath);

                    // Wyliczenie haszu
                    string hashStr = FingerprintHashing.CalculateAverageHash(imgBytes);

                    if (string.IsNullOrEmpty(hashStr))
                    {
                        CustomMessageBox.Show("Nie udało się wygenerować haszu z wybranego pliku. Upewnij się, że to poprawny plik graficzny.", "Błąd przetwarzania", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (var context = new RockGymContext())
                    {
                        var newFingerprint = new Fingerprint
                        {
                            UserId = _userId,
                            FingerprintImage = imgBytes,
                            FingerprintHash = hashStr,
                            DateOfCreation = DateTime.Now
                        };

                        context.Fingerprints.Add(newFingerprint);
                        context.SaveChanges();
                    }

                    CustomMessageBox.Show("Pomyślnie dodano nowy odcisk palca.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadFingerprints();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd dodawania odcisku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteFingerprint(object? parameter)
        {
            if (parameter is not FingerprintItemViewModel fp) return;

            var result = CustomMessageBox.Show(
                "Czy na pewno chcesz usunąć ten odcisk palca?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        var dbFp = context.Fingerprints.Find(fp.FingerprintId);
                        if (dbFp != null)
                        {
                            context.Fingerprints.Remove(dbFp);
                            context.SaveChanges();
                        }
                    }

                    CustomMessageBox.Show("Pomyślnie usunięto odcisk palca.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadFingerprints();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd usuwania odcisku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class FingerprintItemViewModel
    {
        public ulong FingerprintId { get; set; }
        public byte[] FingerprintImage { get; set; } = Array.Empty<byte>();
        public string FingerprintHash { get; set; } = string.Empty;
        public DateTime DateOfCreation { get; set; }
        public string FormattedDate => DateOfCreation.ToString("dd.MM.yyyy HH:mm");
    }
}
