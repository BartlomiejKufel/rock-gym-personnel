using AForge.Video;
using AForge.Video.DirectShow;
using RockGym.ViewModels;
using RockGym.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ZXing;

namespace RockGym.Views
{
    public partial class QrScannerWindow : Window
    {
        private FilterInfoCollection? _videoDevices;
        private VideoCaptureDevice? _videoSource;
        private readonly ZXing.Windows.Compatibility.BarcodeReader _barcodeReader;
        private readonly QrScannerViewModel _viewModel;
        private volatile bool _isLocalPaused;

        public QrScannerWindow()
        {
            InitializeComponent();

            _barcodeReader = new ZXing.Windows.Compatibility.BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            _viewModel = new QrScannerViewModel(_barcodeReader);
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = _viewModel;

            LoadCameraDevices();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QrScannerViewModel.IsScanningPaused))
            {
                if (!_viewModel.IsScanningPaused)
                {
                    _isLocalPaused = false;
                    Dispatcher.Invoke(() =>
                    {
                        if (_videoSource == null || !_videoSource.IsRunning)
                        {
                            CameraPreview.Source = null;
                        }
                    });
                }
            }
        }

        private void LoadCameraDevices()
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                CameraComboBox.Items.Clear();

                if (_videoDevices.Count == 0)
                {
                    CameraComboBox.Items.Add("Brak kamer");
                    CameraComboBox.SelectedIndex = 0;
                    CameraComboBox.IsEnabled = false;
                    return;
                }

                foreach (FilterInfo device in _videoDevices)
                {
                    CameraComboBox.Items.Add(device.Name);
                }

                CameraComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd inicjalizacji urządzeń wideo: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_videoDevices == null || _videoDevices.Count == 0) return;

            StopCamera();

            int selectedIndex = CameraComboBox.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _videoDevices.Count)
            {
                string monikerString = _videoDevices[selectedIndex].MonikerString;
                _videoSource = new VideoCaptureDevice(monikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
                
                _isLocalPaused = false;
                _viewModel.ResumeScanningCommand.Execute(null);
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (_isLocalPaused) return;

                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    var bitmapImage = ConvertToBitmapImage(bitmap);

                    Dispatcher.Invoke(() =>
                    {
                        CameraPreview.Source = bitmapImage;
                    });

                    var result = _barcodeReader.Decode(bitmap);
                    if (result != null)
                    {
                        _isLocalPaused = true;
                        
                        Dispatcher.Invoke(() =>
                        {
                            _viewModel.ProcessCodeCommand.Execute(result.Text);
                        });
                    }
                }
            }
            catch
            {
                // Ignorowanie błędów konwersji pojedynczych klatek
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Pliki obrazów (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Wszystkie pliki (*.*)|*.*",
                    Title = "Wybierz obraz z kodem QR"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    using (Bitmap bitmap = new Bitmap(filePath))
                    {
                        var result = _barcodeReader.Decode(bitmap);
                        if (result != null)
                        {
                            _isLocalPaused = true;

                            var bitmapImage = ConvertToBitmapImage(bitmap);
                            CameraPreview.Source = bitmapImage;

                            _viewModel.ProcessCodeCommand.Execute(result.Text);
                        }
                        else
                        {
                            CustomMessageBox.Show("Nie wykryto kodu QR w wybranym pliku.", "Brak kodu QR", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd odczytu pliku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource = null;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
            base.OnClosing(e);
        }

        private BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
    }
}
