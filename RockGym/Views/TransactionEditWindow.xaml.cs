using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RockGym.Models;
using RockGym.Services;

namespace RockGym.Views
{
    public partial class TransactionEditWindow : Window
    {
        private readonly PurchaseHistory _transaction;
        private readonly List<User> _clients;
        private readonly List<Offer> _offers;

        public TransactionEditWindow(PurchaseHistory transaction, string title)
        {
            InitializeComponent();
            _transaction = transaction;
            HeaderTextBlock.Text = title;

            // Wczytanie klientów i ofert z bazy danych
            try
            {
                using (var context = new RockGymContext())
                {
                    // W bazie osób do obsłużenia mają być wszyscy użytkownicy serwisu
                    _clients = context.Users.ToList();
                    _offers = context.Offers.ToList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd wczytywania danych z bazy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _clients = new List<User>();
                _offers = new List<Offer>();
            }

            ClientComboBox.ItemsSource = _clients;
            OfferComboBox.ItemsSource = _offers;

            // Wypełnienie pól formularza danymi (dla edycji) lub wartościami domyślnymi (dla dodawania)
            if (_transaction.PurchaseId > 0)
            {
                ClientComboBox.SelectedItem = _clients.FirstOrDefault(c => c.UserId == _transaction.CustomerId);
                OfferComboBox.SelectedItem = _offers.FirstOrDefault(o => o.OfferId == _transaction.OfferId);
                PriceTextBox.Text = _transaction.Price.ToString("F2", CultureInfo.InvariantCulture);
                TransactionDatePicker.SelectedDate = _transaction.PurchaseDate;
            }
            else
            {
                TransactionDatePicker.SelectedDate = DateTime.Now;
            }
        }

        private void ClientComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientComboBox.Template.FindName("PART_EditableTextBox", ClientComboBox) is TextBox textBox)
            {
                int selectionStart = textBox.SelectionStart;
                int selectionLength = textBox.SelectionLength;
                string filterText = textBox.Text;

                // Pomijamy filtrowanie, jeśli tekst zgadza się z wybranym elementem (użytkownik wybrał coś z listy)
                if (ClientComboBox.SelectedItem is User selectedUser && selectedUser.FullName == filterText)
                {
                    return;
                }

                ICollectionView view = CollectionViewSource.GetDefaultView(ClientComboBox.ItemsSource);
                if (view != null)
                {
                    view.Filter = item =>
                    {
                        if (string.IsNullOrEmpty(filterText)) return true;
                        if (item is User c)
                        {
                            return c.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                                   c.Surname.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                                   c.FullName.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    };
                    view.Refresh();
                }

                ClientComboBox.IsDropDownOpen = true;
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = selectionLength;
            }
        }

        private void OfferComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (OfferComboBox.Template.FindName("PART_EditableTextBox", OfferComboBox) is TextBox textBox)
            {
                int selectionStart = textBox.SelectionStart;
                int selectionLength = textBox.SelectionLength;
                string filterText = textBox.Text;

                // Pomijamy filtrowanie, jeśli tekst zgadza się z wybraną ofertą
                if (OfferComboBox.SelectedItem is Offer selectedOffer && selectedOffer.Name == filterText)
                {
                    return;
                }

                ICollectionView view = CollectionViewSource.GetDefaultView(OfferComboBox.ItemsSource);
                if (view != null)
                {
                    view.Filter = item =>
                    {
                        if (string.IsNullOrEmpty(filterText)) return true;
                        if (item is Offer o)
                        {
                            return o.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    };
                    view.Refresh();
                }

                OfferComboBox.IsDropDownOpen = true;
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = selectionLength;
            }
        }

        private void OfferComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OfferComboBox.SelectedItem is Offer selectedOffer)
            {
                // Cena pobiera się automatycznie z wybranej oferty
                PriceTextBox.Text = selectedOffer.Price.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Walidacja Klienta
            if (ClientComboBox.SelectedItem is not User selectedClient)
            {
                CustomMessageBox.Show("Wybierz klienta z listy rozwijanej.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Walidacja Oferty
            if (OfferComboBox.SelectedItem is not Offer selectedOffer)
            {
                CustomMessageBox.Show("Wybierz ofertę z listy rozwijanej.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Walidacja Ceny
            string priceText = PriceTextBox.Text.Trim().Replace(",", ".");
            if (string.IsNullOrEmpty(priceText) || !double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out double price) || price < 0)
            {
                CustomMessageBox.Show("Wpisz poprawną, nieujemną cenę (np. 120.00 lub 0).", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 4. Walidacja Daty
            if (TransactionDatePicker.SelectedDate is not DateTime selectedDate)
            {
                CustomMessageBox.Show("Wybierz poprawną datę transakcji.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Zapisanie zwalidowanych wartości z powrotem do obiektu
            _transaction.CustomerId = selectedClient.UserId;
            _transaction.OfferId = selectedOffer.OfferId;
            _transaction.Price = price;
            _transaction.PurchaseDate = selectedDate;

            // Ustawiamy właściwości nawigacyjne na null, aby zapobiec próbom ponownego dodawania/aktualizacji powiązanych encji przez EF Core
            _transaction.Customer = null;
            _transaction.Offer = null;
            _transaction.Employee = null;

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
