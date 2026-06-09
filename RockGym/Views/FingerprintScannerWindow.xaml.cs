using Microsoft.Win32;
using RockGym.ViewModels;
using System;
using System.IO;
using System.Windows;

namespace RockGym.Views
{
    public partial class FingerprintScannerWindow : Window
    {
        private readonly FingerprintScannerViewModel _viewModel;

        public FingerprintScannerWindow()
        {
            InitializeComponent();
            _viewModel = new FingerprintScannerViewModel();
            DataContext = _viewModel;
        }

        private void UploadSampleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Obrazy linii papilarnych (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Wszystkie pliki (*.*)|*.*",
                    Title = "Wybierz próbkę odcisku palca"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    byte[] imgBytes = File.ReadAllBytes(filePath);

                    // Wyślij surowe bajty obrazka do ViewModela w celu przetworzenia haszu
                    if (_viewModel.ProcessFingerprintCommand.CanExecute(imgBytes))
                    {
                        _viewModel.ProcessFingerprintCommand.Execute(imgBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odczytu pliku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
