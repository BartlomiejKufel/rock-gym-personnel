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
    public class EventsViewModel : ViewModelBase
    {
        private readonly User _currentUser;
        private ObservableCollection<Event> _events = new();
        private string _selectedSortingOption = "Od najnowszych";
        private string _searchText = string.Empty;
        private bool _isEmpty;

        public ObservableCollection<Event> Events
        {
            get => _events;
            set
            {
                _events = value;
                OnPropertyChanged();
                UpdateIsEmpty();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadEvents();
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
                LoadEvents();
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

        public bool IsAdmin => _currentUser.RoleId == 1;

        public ICommand AddEventCommand { get; }
        public ICommand EditEventCommand { get; }
        public ICommand DeleteEventCommand { get; }

        public EventsViewModel(User currentUser)
        {
            _currentUser = currentUser;

            AddEventCommand = new RelayCommand(o => ExecuteAddEvent());
            EditEventCommand = new RelayCommand(ExecuteEditEvent);
            DeleteEventCommand = new RelayCommand(ExecuteDeleteEvent);

            LoadEvents();
        }

        private void LoadEvents()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var query = context.Events
                        .Include(e => e.Instructor)
                        .Include(e => e.Offer)
                        .Include(e => e.EventParticipants)
                            .ThenInclude(ep => ep.Participant)
                        .AsQueryable();

                    if (_currentUser.RoleId == 2)
                    {
                        var now = DateTime.Now;
                        query = query.Where(e => e.EndDate >= now);
                    }

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        string search = SearchText.Trim().ToLower();
                        query = query.Where(e =>
                            e.Instructor != null && (
                                e.Instructor.Name.ToLower().Contains(search) ||
                                e.Instructor.Surname.ToLower().Contains(search) ||
                                (e.Instructor.Name + " " + e.Instructor.Surname).ToLower().Contains(search) ||
                                (e.Instructor.Surname + " " + e.Instructor.Name).ToLower().Contains(search)
                            )
                        );
                    }

                    if (SelectedSortingOption == "Od najnowszych")
                    {
                        query = query.OrderByDescending(e => e.StartDate);
                    }
                    else
                    {
                        query = query.OrderBy(e => e.StartDate);
                    }

                    var list = query.ToList();
                    Events = new ObservableCollection<Event>(list);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas ładowania wydarzeń: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIsEmpty()
        {
            IsEmpty = Events.Count == 0;
        }

        private void ExecuteAddEvent()
        {
            if (!IsAdmin)
            {
                CustomMessageBox.Show("Brak uprawnień. Tylko administrator może dodawać wydarzenia.", "Brak uprawnień", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newEvent = new Event
            {
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(2),
                ParticipantsLimit = 15,
                EventColor = "#FF7F3F"
            };

            var editWindow = new EventEditWindow(newEvent, "Dodaj wydarzenie");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Events.Add(newEvent);
                        context.SaveChanges();
                    }
                    LoadEvents();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu wydarzenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditEvent(object? parameter)
        {
            if (parameter is not Event ev) return;

            var copy = new Event
            {
                EventId = ev.EventId,
                InstructorId = ev.InstructorId,
                Name = ev.Name,
                Description = ev.Description,
                EventColor = ev.EventColor,
                ParticipantsLimit = ev.ParticipantsLimit,
                OfferId = ev.OfferId,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate
            };

            var editWindow = new EventEditWindow(copy, "Edytuj wydarzenie");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Events.Update(copy);
                        context.SaveChanges();
                    }
                    LoadEvents();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu zmian: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteDeleteEvent(object? parameter)
        {
            if (!IsAdmin)
            {
                CustomMessageBox.Show("Brak uprawnień. Tylko administrator może usuwać wydarzenia.", "Brak uprawnień", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (parameter is not Event ev) return;

            var result = CustomMessageBox.Show(
                $"Czy na pewno chcesz usunąć wydarzenie \"{ev.Name}\"?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Events.Remove(ev);
                        context.SaveChanges();
                    }
                    LoadEvents();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd podczas usuwania wydarzenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
