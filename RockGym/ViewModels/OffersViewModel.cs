using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RockGym.Models;
using RockGym.Services;

namespace RockGym.ViewModels
{
    public class OffersViewModel : ViewModelBase
    {
        private readonly User _currentUser;
        private ObservableCollection<Offer> _offers = new();
        private bool _isEmpty;

        public ObservableCollection<Offer> Offers
        {
            get => _offers;
            set
            {
                _offers = value;
                OnPropertyChanged();
                UpdateIsEmpty();
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

        public ICommand AddOfferCommand { get; }
        public ICommand EditOfferCommand { get; }
        public ICommand DeleteOfferCommand { get; }

        public OffersViewModel(User currentUser)
        {
            _currentUser = currentUser;

            AddOfferCommand = new RelayCommand(o => ExecuteAddOffer());
            EditOfferCommand = new RelayCommand(ExecuteEditOffer);
            DeleteOfferCommand = new RelayCommand(ExecuteDeleteOffer);

            LoadOffers();
        }

        private void LoadOffers()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var list = context.Offers.OrderBy(o => o.Name).ToList();
                    Offers = new ObservableCollection<Offer>(list);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas ładowania ofert: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIsEmpty()
        {
            IsEmpty = Offers.Count == 0;
        }

        private void ExecuteAddOffer()
        {
            var newOffer = new Offer();
            var editWindow = new RockGym.Views.OfferEditWindow(newOffer, "Dodaj ofertę");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Offers.Add(newOffer);
                        context.SaveChanges();
                    }
                    LoadOffers();
                    CustomMessageBox.ShowSuccess($"Oferta '{newOffer.Name}' została pomyślnie dodana.", "Sukces");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu oferty: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditOffer(object? parameter)
        {
            if (parameter is not Offer offer) return;

            var copy = new Offer
            {
                OfferId = offer.OfferId,
                Name = offer.Name,
                Price = offer.Price,
                Duration = offer.Duration
            };

            var editWindow = new RockGym.Views.OfferEditWindow(copy, "Edytuj ofertę");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Offers.Update(copy);
                        context.SaveChanges();
                    }
                    LoadOffers();
                    CustomMessageBox.ShowSuccess($"Oferta '{copy.Name}' została pomyślnie zaktualizowana.", "Sukces");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu zmian: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteDeleteOffer(object? parameter)
        {
            if (parameter is not Offer offer) return;

            var result = CustomMessageBox.Show(
                $"Czy na pewno chcesz usunąć ofertę '{offer.Name}'?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.Offers.Remove(offer);
                        context.SaveChanges();
                    }
                    LoadOffers();
                    CustomMessageBox.ShowSuccess($"Oferta '{offer.Name}' została pomyślnie usunięta.", "Sukces");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd usuwania oferty: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
