using System;
using System.Windows;
using RockGym.Models;
using RockGym.Services;

namespace RockGym.Views
{
    public partial class NotificationEditWindow : Window
    {
        private readonly Notification _notification;

        public NotificationEditWindow(Notification notification, string title)
        {
            InitializeComponent();
            _notification = notification;
            HeaderTextBlock.Text = title;

            // Wypełnienie formularza danymi
            TitleTextBox.Text = _notification.Name;
            DescriptionTextBox.Text = _notification.Description;
            StartDatePicker.SelectedDate = _notification.StartDate;
            EndDatePicker.SelectedDate = _notification.EndDate;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja formularza
            string title = TitleTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (string.IsNullOrEmpty(title))
            {
                CustomMessageBox.Show("Tytuł powiadomienia nie może być pusty.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!startDate.HasValue || !endDate.HasValue)
            {
                CustomMessageBox.Show("Wybierz datę rozpoczęcia i zakończenia.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (startDate.Value > endDate.Value)
            {
                CustomMessageBox.Show("Data rozpoczęcia nie może być późniejsza niż data zakończenia.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Zapisanie danych z powrotem do obiektu
            _notification.Name = title;
            _notification.Description = description;
            _notification.StartDate = startDate.Value;
            _notification.EndDate = endDate.Value;

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
