using System;
using System.Globalization;
using System.Windows;
using RockGym.Models;
using RockGym.Services;

namespace RockGym.Views
{
    /// <summary>
    /// Logika interakcji dla klasy OfferEditWindow.xaml
    /// </summary>
    public partial class OfferEditWindow : Window
    {
        private readonly Offer _offer;

        public OfferEditWindow(Offer offer, string title)
        {
            InitializeComponent();
            _offer = offer;
            HeaderTextBlock.Text = title;

            // Wypełnienie pól formularza danymi
            NameTextBox.Text = _offer.Name;
            PriceTextBox.Text = _offer.Price > 0 ? _offer.Price.ToString("F2", CultureInfo.InvariantCulture) : string.Empty;
            DurationTextBox.Text = _offer.Duration.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string priceText = PriceTextBox.Text.Trim().Replace(",", "."); // Zabezpieczenie przed przecinkiem/kropką dziesiętną
            string durationText = DurationTextBox.Text.Trim();

            // 1. Walidacja nazwy oferty
            if (string.IsNullOrEmpty(name))
            {
                CustomMessageBox.Show("Nazwa oferty nie może być pusta.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Walidacja ceny
            if (string.IsNullOrEmpty(priceText) || !double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out double price) || price <= 0)
            {
                CustomMessageBox.Show("Wpisz poprawną, dodatnią cenę (np. 120.00).", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Walidacja czasu trwania
            if (string.IsNullOrEmpty(durationText) || !int.TryParse(durationText, out int duration) || duration < 0)
            {
                CustomMessageBox.Show("Czas trwania musi być nieujemną liczbą całkowitą wyrażoną w dniach (np. 0 lub 30).", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Zapisanie zwalidowanych wartości z powrotem do obiektu
            _offer.Name = name;
            _offer.Price = price;
            _offer.Duration = duration;

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
