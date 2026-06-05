using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RockGym.Models;
using RockGym.Services;

namespace RockGym.Views
{
    public class EventColorItem
    {
        public string Name { get; set; } = string.Empty;
        public string Hex { get; set; } = string.Empty;
        public Brush ColorBrush { get; set; } = Brushes.Gray;
    }

    public partial class EventEditWindow : Window
    {
        private readonly Event _event;
        private readonly List<User> _instructors;
        private readonly List<Offer> _offers;
        private readonly List<EventColorItem> _colors = new()
        {
            new EventColorItem { Name = "Pomarańczowy", Hex = "#FF7F3F", ColorBrush = new SolidColorBrush(Color.FromRgb(255, 127, 63)) },
            new EventColorItem { Name = "Niebieski", Hex = "#3182CE", ColorBrush = new SolidColorBrush(Color.FromRgb(49, 130, 206)) },
            new EventColorItem { Name = "Zielony", Hex = "#38A169", ColorBrush = new SolidColorBrush(Color.FromRgb(56, 161, 105)) },
            new EventColorItem { Name = "Czerwony", Hex = "#E53E3E", ColorBrush = new SolidColorBrush(Color.FromRgb(229, 62, 62)) },
            new EventColorItem { Name = "Fioletowy", Hex = "#805AD5", ColorBrush = new SolidColorBrush(Color.FromRgb(128, 90, 213)) },
            new EventColorItem { Name = "Różowy", Hex = "#D53F8C", ColorBrush = new SolidColorBrush(Color.FromRgb(213, 63, 140)) },
            new EventColorItem { Name = "Szary", Hex = "#718096", ColorBrush = new SolidColorBrush(Color.FromRgb(113, 128, 150)) }
        };

        public EventEditWindow(Event ev, string title)
        {
            InitializeComponent();
            _event = ev;
            HeaderTextBlock.Text = title;

            try
            {
                using (var context = new RockGymContext())
                {
                    _instructors = context.Users.Where(u => u.RoleId == 3).ToList();
                    _offers = context.Offers.Where(o => o.Duration == 0).ToList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas wczytywania danych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _instructors = new List<User>();
                _offers = new List<Offer>();
            }

            InstructorComboBox.ItemsSource = _instructors;
            OfferComboBox.ItemsSource = _offers;
            ColorComboBox.ItemsSource = _colors;

            NameTextBox.Text = _event.Name;
            DescriptionTextBox.Text = _event.Description;
            LimitTextBox.Text = _event.ParticipantsLimit > 0 ? _event.ParticipantsLimit.ToString() : "15";

            InstructorComboBox.SelectedItem = _instructors.FirstOrDefault(i => i.UserId == _event.InstructorId);
            OfferComboBox.SelectedItem = _offers.FirstOrDefault(o => o.OfferId == _event.OfferId);
            
            var matchedColor = _colors.FirstOrDefault(c => c.Hex.Equals(_event.EventColor, StringComparison.OrdinalIgnoreCase));
            ColorComboBox.SelectedItem = matchedColor ?? _colors.FirstOrDefault(c => c.Hex == "#FF7F3F");

            if (_event.EventId > 0)
            {
                StartDatePicker.SelectedDate = _event.StartDate.Date;
                StartTimeTextBox.Text = _event.StartDate.ToString("HH:mm");
                EndDatePicker.SelectedDate = _event.EndDate.Date;
                EndTimeTextBox.Text = _event.EndDate.ToString("HH:mm");
            }
            else
            {
                StartDatePicker.SelectedDate = DateTime.Today.AddDays(1);
                StartTimeTextBox.Text = "12:00";
                EndDatePicker.SelectedDate = DateTime.Today.AddDays(1);
                EndTimeTextBox.Text = "14:00";
            }
        }

        private void InstructorComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InstructorComboBox.Template.FindName("PART_EditableTextBox", InstructorComboBox) is TextBox textBox)
            {
                int selectionStart = textBox.SelectionStart;
                int selectionLength = textBox.SelectionLength;
                string filterText = textBox.Text;

                if (InstructorComboBox.SelectedItem is User selectedUser && selectedUser.FullName == filterText)
                {
                    return;
                }

                ICollectionView view = CollectionViewSource.GetDefaultView(InstructorComboBox.ItemsSource);
                if (view != null)
                {
                    view.Filter = item =>
                    {
                        if (string.IsNullOrEmpty(filterText)) return true;
                        if (item is User u)
                        {
                            return u.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                                   u.Surname.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                                   u.FullName.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    };
                    view.Refresh();
                }

                InstructorComboBox.IsDropDownOpen = true;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();
            string limitText = LimitTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                CustomMessageBox.Show("Nazwa wydarzenia nie może być pusta.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (InstructorComboBox.SelectedItem is not User selectedInstructor)
            {
                CustomMessageBox.Show("Wybierz prowadzącego z listy.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ColorComboBox.SelectedItem is not EventColorItem selectedColor)
            {
                CustomMessageBox.Show("Wybierz kolor wydarzenia.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate is not DateTime startDateOnly)
            {
                CustomMessageBox.Show("Wybierz datę rozpoczęcia.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(StartTimeTextBox.Text.Trim(), out TimeSpan startTime))
            {
                CustomMessageBox.Show("Wpisz poprawną godzinę rozpoczęcia (np. 12:00).", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EndDatePicker.SelectedDate is not DateTime endDateOnly)
            {
                CustomMessageBox.Show("Wybierz datę zakończenia.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(EndTimeTextBox.Text.Trim(), out TimeSpan endTime))
            {
                CustomMessageBox.Show("Wpisz poprawną godzinę zakończenia (np. 14:00).", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startCombined = startDateOnly.Date.Add(startTime);
            DateTime endCombined = endDateOnly.Date.Add(endTime);

            if (startCombined >= endCombined)
            {
                CustomMessageBox.Show("Data rozpoczęcia musi być wcześniejsza niż data zakończenia.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(limitText) || !int.TryParse(limitText, out int limit) || limit <= 0)
            {
                CustomMessageBox.Show("Wpisz poprawny, dodatni limit miejsc (np. 15).", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Offer? selectedOffer = OfferComboBox.SelectedItem as Offer;

            _event.Name = name;
            _event.Description = string.IsNullOrEmpty(description) ? null : description;
            _event.InstructorId = selectedInstructor.UserId;
            _event.EventColor = selectedColor.Hex;
            _event.StartDate = startCombined;
            _event.EndDate = endCombined;
            _event.ParticipantsLimit = limit;
            _event.OfferId = selectedOffer?.OfferId;

            _event.Instructor = null;
            _event.Offer = null;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InstructorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
