using Microsoft.EntityFrameworkCore;
using RockGym.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RockGym.Views;
using RockGym.Services;

namespace RockGym.ViewModels
{
    public class NotificationsViewModel : ViewModelBase
    {
        private readonly User _currentUser;
        private ObservableCollection<Notification> _notifications = new();
        private string _selectedSortingOption = "Od najnowszych";
        private bool _isEmpty;

        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set
            {
                _notifications = value;
                OnPropertyChanged();
                UpdateIsEmpty();
            }
        }

        public ObservableCollection<string> SortingOptions { get; } = new()
        {
            "Od najnowszych",
            "Od najstarszych"
        };

        public string SelectedSortingOption
        {
            get => _selectedSortingOption;
            set
            {
                _selectedSortingOption = value;
                OnPropertyChanged();
                LoadNotifications();
            }
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                _isEmpty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowTable));
            }
        }

        public bool ShowTable => !IsEmpty;

        public ICommand AddNotificationCommand { get; }
        public ICommand EditNotificationCommand { get; }
        public ICommand DeleteNotificationCommand { get; }

        public NotificationsViewModel(User currentUser)
        {
            _currentUser = currentUser;

            AddNotificationCommand = new RelayCommand(o => ExecuteAddNotification());
            EditNotificationCommand = new RelayCommand(ExecuteEditNotification);
            DeleteNotificationCommand = new RelayCommand(ExecuteDeleteNotification);

            LoadNotifications();
        }

        private void LoadNotifications()
        {
            try
            {
                using (var context = new RockGym.Services.RockGymContext())
                {
                    var query = context.Notifications.Include(n => n.Creator).AsQueryable();

                    if (SelectedSortingOption == "Od najnowszych")
                    {
                        query = query.OrderByDescending(n => n.StartDate);
                    }
                    else
                    {
                        query = query.OrderBy(n => n.StartDate);
                    }

                    var list = query.ToList();
                    Notifications = new ObservableCollection<Notification>(list);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas ładowania powiadomień: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIsEmpty()
        {
            IsEmpty = Notifications.Count == 0;
        }

        private void ExecuteAddNotification()
        {
            var newNotification = new Notification
            {
                CreatorId = _currentUser.UserId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            var editWindow = new RockGym.Views.NotificationEditWindow(newNotification, "Dodaj powiadomienie");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGym.Services.RockGymContext())
                    {
                        context.Notifications.Add(newNotification);
                        context.SaveChanges();
                    }
                    LoadNotifications();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditNotification(object? parameter)
        {
            if (parameter is not Notification notification) return;

            var copy = new Notification
            {
                NotificationId = notification.NotificationId,
                CreatorId = notification.CreatorId,
                Name = notification.Name,
                Description = notification.Description,
                StartDate = notification.StartDate,
                EndDate = notification.EndDate
            };

            var editWindow = new RockGym.Views.NotificationEditWindow(copy, "Edytuj powiadomienie");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGym.Services.RockGymContext())
                    {
                        context.Notifications.Update(copy);
                        context.SaveChanges();
                    }
                    LoadNotifications();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd edycji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteDeleteNotification(object? parameter)
        {
            if (parameter is not Notification notification) return;

            var result = CustomMessageBox.Show(
                $"Czy na pewno chcesz usunąć powiadomienie \"{notification.Name}\"?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new RockGym.Services.RockGymContext())
                    {
                        context.Notifications.Remove(notification);
                        context.SaveChanges();
                    }
                    LoadNotifications();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd usuwania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
