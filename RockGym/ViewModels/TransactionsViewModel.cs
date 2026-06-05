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
    public class TransactionsViewModel : ViewModelBase
    {
        private readonly User _currentUser;
        private ObservableCollection<PurchaseHistory> _transactions = new();
        private string _searchText = string.Empty;
        private bool _isEmpty;

        public ObservableCollection<PurchaseHistory> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;
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
                LoadTransactions();
            }
        }

        private string _selectedSortingOption = "Od najnowszych";

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
                LoadTransactions();
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

        public ICommand AddTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }

        public TransactionsViewModel(User currentUser)
        {
            _currentUser = currentUser;

            AddTransactionCommand = new RelayCommand(o => ExecuteAddTransaction());
            EditTransactionCommand = new RelayCommand(ExecuteEditTransaction);

            LoadTransactions();
        }

        private void LoadTransactions()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var query = context.PurchaseHistories
                        .Include(t => t.Customer)
                        .Include(t => t.Employee)
                        .Include(t => t.Offer)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        string search = SearchText.Trim().ToLower();
                        query = query.Where(t => 
                            t.Customer != null && (
                                t.Customer.Name.ToLower().Contains(search) || 
                                t.Customer.Surname.ToLower().Contains(search) ||
                                (t.Customer.Name + " " + t.Customer.Surname).ToLower().Contains(search)
                            )
                        );
                    }

                    if (SelectedSortingOption == "Od najnowszych")
                    {
                        query = query.OrderByDescending(t => t.PurchaseDate);
                    }
                    else
                    {
                        query = query.OrderBy(t => t.PurchaseDate);
                    }

                    var list = query.ToList();
                    Transactions = new ObservableCollection<PurchaseHistory>(list);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas ładowania transakcji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIsEmpty()
        {
            IsEmpty = Transactions.Count == 0;
        }

        private void ExecuteAddTransaction()
        {
            var newTransaction = new PurchaseHistory
            {
                EmployeeId = _currentUser.UserId,
                PurchaseDate = DateTime.Now
            };

            var editWindow = new TransactionEditWindow(newTransaction, "Dodaj transakcję");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.PurchaseHistories.Add(newTransaction);
                        context.SaveChanges();
                    }
                    LoadTransactions();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu transakcji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditTransaction(object? parameter)
        {
            if (parameter is not PurchaseHistory transaction) return;

            var copy = new PurchaseHistory
            {
                PurchaseId = transaction.PurchaseId,
                CustomerId = transaction.CustomerId,
                EmployeeId = transaction.EmployeeId,
                Price = transaction.Price,
                PurchaseDate = transaction.PurchaseDate,
                OfferId = transaction.OfferId
            };

            var editWindow = new TransactionEditWindow(copy, "Edytuj transakcję");
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new RockGymContext())
                    {
                        context.PurchaseHistories.Update(copy);
                        context.SaveChanges();
                    }
                    LoadTransactions();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Błąd zapisu zmian: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
