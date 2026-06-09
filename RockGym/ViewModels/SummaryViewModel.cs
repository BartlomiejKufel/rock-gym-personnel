using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using RockGym.Models;
using RockGym.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RockGym.ViewModels
{
    public class SummaryViewModel : ViewModelBase
    {
        private double _todayRevenue;
        private int _todayTransactionsCount;
        private int _todayEntriesCount;
        private int _activeGymLoad;
        private double _monthRevenue;
        private int _monthTransactionsCount;
        private string _mostPopularOfferName = "Brak transakcji";
        private ObservableCollection<EmployeeRevenueDisplayModel> _employeeRevenues = new();
        private DateTime _selectedDate = DateTime.Today;
        private string _selectedDayName = string.Empty;
        private string _selectedMonthYearName = string.Empty;

        /// <summary>
        /// Wybrana data filtrowania statystyk i zestawień na pulpicie.
        /// Zawiera wbudowaną logikę cofania wyboru (rollback), jeśli użytkownik spróbuje
        /// wybrać lub wpisać ręcznie datę z przyszłości (bądź wyczyścić pole).
        /// </summary>
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (value == null || value.Value > DateTime.Today)
                {
                    // Wymuszenie odświeżenia powiązania w widoku w celu cofnięcia niepoprawnej wartości
                    OnPropertyChanged(nameof(SelectedDate));
                    return;
                }
                _selectedDate = value.Value;
                OnPropertyChanged();
                LoadSummaryData(); // Przeładuj wszystkie dane statystyczne dla nowej daty
            }
        }

        public DateTime MaxDate => DateTime.Today;

        public double TodayRevenue
        {
            get => _todayRevenue;
            set { _todayRevenue = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTodayRevenue)); }
        }

        public string FormattedTodayRevenue => TodayRevenue.ToString("C2", new CultureInfo("pl-PL"));

        public int TodayTransactionsCount
        {
            get => _todayTransactionsCount;
            set { _todayTransactionsCount = value; OnPropertyChanged(); }
        }

        public int TodayEntriesCount
        {
            get => _todayEntriesCount;
            set { _todayEntriesCount = value; OnPropertyChanged(); }
        }

        public int ActiveGymLoad
        {
            get => _activeGymLoad;
            set { _activeGymLoad = value; OnPropertyChanged(); }
        }

        public double MonthRevenue
        {
            get => _monthRevenue;
            set { _monthRevenue = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedMonthRevenue)); }
        }

        public string FormattedMonthRevenue => MonthRevenue.ToString("C2", new CultureInfo("pl-PL"));

        public int MonthTransactionsCount
        {
            get => _monthTransactionsCount;
            set { _monthTransactionsCount = value; OnPropertyChanged(); }
        }

        public string MostPopularOfferName
        {
            get => _mostPopularOfferName;
            set { _mostPopularOfferName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EmployeeRevenueDisplayModel> EmployeeRevenues
        {
            get => _employeeRevenues;
            set { _employeeRevenues = value; OnPropertyChanged(); }
        }

        public string SelectedDayName
        {
            get => _selectedDayName;
            set { _selectedDayName = value; OnPropertyChanged(); }
        }

        public string SelectedMonthYearName
        {
            get => _selectedMonthYearName;
            set { _selectedMonthYearName = value; OnPropertyChanged(); }
        }

        public ICommand DownloadMonthlyTransactionsCommand { get; }

        public SummaryViewModel()
        {
            DownloadMonthlyTransactionsCommand = new RelayCommand(o => DownloadMonthlyTransactions());
            LoadSummaryData();
        }

        /// <summary>
        /// Pobiera z bazy danych za pomocą EF Core wszystkie zestawienia statystyczne dla wybranego dnia
        /// oraz odpowiadającego mu miesiąca, a następnie aktualizuje właściwości powiązane z UI.
        /// </summary>
        private void LoadSummaryData()
        {
            try
            {
                using (var context = new RockGymContext())
                {
                    var targetDate = _selectedDate.Date;
                    var targetYear = targetDate.Year;
                    var targetMonth = targetDate.Month;

                    // Dynamiczne budowanie lokalizowanych nazw nagłówków sekcji (np. "Czerwiec 2026")
                    SelectedDayName = targetDate.ToString("dd.MM.yyyy");
                    var monthName = targetDate.ToString("MMMM yyyy", new CultureInfo("pl-PL"));
                    SelectedMonthYearName = char.ToUpper(monthName[0]) + monthName.Substring(1);

                    // 1. DZIENNY UTARG: Sumowanie przychodu ze sprzedaży karnetów/ofert
                    TodayRevenue = context.PurchaseHistories
                        .Where(p => p.PurchaseDate.Date == targetDate)
                        .Sum(p => (double?)p.Price) ?? 0.0;

                    // 2. DZIENNA LICZBA TRANSAKCJI
                    TodayTransactionsCount = context.PurchaseHistories
                        .Count(p => p.PurchaseDate.Date == targetDate);

                    // 3. DZIENNE WEJŚCIA: Suma wejść zarejestrowanych dzisiaj
                    TodayEntriesCount = context.Entrances
                        .Count(e => e.DateOfEntry.Date == targetDate);

                    // 4. OBECNI W KLUBIE: Wejścia dzisiejsze, które nie mają jeszcze wpisanego wyjścia (EndTime == null)
                    ActiveGymLoad = context.Entrances
                        .Count(e => e.DateOfEntry.Date == targetDate && e.EndTime == null);

                    // 5. MIESIĘCZNY UTARG
                    MonthRevenue = context.PurchaseHistories
                        .Where(p => p.PurchaseDate.Year == targetYear && p.PurchaseDate.Month == targetMonth)
                        .Sum(p => (double?)p.Price) ?? 0.0;

                    // 6. MIESIĘCZNA LICZBA TRANSAKCJI
                    MonthTransactionsCount = context.PurchaseHistories
                        .Where(p => p.PurchaseDate.Year == targetYear && p.PurchaseDate.Month == targetMonth)
                        .Count();

                    // 7. NAJPOPULARNIEJSZA OFERTA W MIESIĄCU:
                    // Grupowanie według nazwy oferty i wyliczenie liczby zakupów, pomijając oferty jednorazowe/specjalne (duration = 0)
                    var popularOfferGroup = context.PurchaseHistories
                        .Where(p => p.PurchaseDate.Year == targetYear && 
                                    p.PurchaseDate.Month == targetMonth && 
                                    p.OfferId != null && 
                                    p.Offer!.Duration != 0)
                        .GroupBy(p => p.Offer!.Name)
                        .Select(g => new { OfferName = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .FirstOrDefault();

                    MostPopularOfferName = popularOfferGroup?.OfferName ?? "Brak transakcji";

                    // 8. UTARG PERSONELU: Zestawienie sprzedaży dokonanej przez poszczególnych pracowników
                    var todayPurchases = context.PurchaseHistories
                        .Where(p => p.PurchaseDate.Date == targetDate)
                        .ToList();

                    // Pobierz tylko administratorów i pracowników (role_id = 1 lub 2)
                    var staffUsers = context.Users
                        .Include(u => u.Role)
                        .Where(u => u.RoleId == 1 || u.RoleId == 2)
                        .ToList();

                    var list = new List<EmployeeRevenueDisplayModel>();

                    foreach (var staff in staffUsers)
                    {
                        double staffRevenue = todayPurchases
                            .Where(p => p.EmployeeId == staff.UserId)
                            .Sum(p => p.Price);

                        list.Add(new EmployeeRevenueDisplayModel
                        {
                            Name = staff.FullName,
                            RoleName = staff.Role?.Name ?? "Brak",
                            RoleId = staff.RoleId,
                            Revenue = staffRevenue,
                            ProfilePicture = staff.ProfilePicture,
                            IsInternet = false
                        });
                    }

                    // Oblicz zakupy online (gdzie transakcja nie ma przypisanego EmployeeId - zakup internetowy)
                    double internetRevenue = todayPurchases
                        .Where(p => p.EmployeeId == null)
                        .Sum(p => p.Price);

                    list.Add(new EmployeeRevenueDisplayModel
                    {
                        Name = "Zakup przez internet",
                        RoleName = "Internet",
                        RoleId = 0,
                        Revenue = internetRevenue,
                        IsInternet = true
                    });

                    // Sortowanie listy od najwyższego utargu i zasilenie kolekcji dla DataGrid
                    EmployeeRevenues = new ObservableCollection<EmployeeRevenueDisplayModel>(
                        list.OrderByDescending(r => r.Revenue)
                    );
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas ładowania danych podsumowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Generuje plik arkusza Excel (.xlsx) za pomocą biblioteki ClosedXML.
        /// Eksportuje listę wszystkich transakcji z wybranego miesiąca z nagłówkami i formatowaniem walutowym.
        /// </summary>
        private void DownloadMonthlyTransactions()
        {
            try
            {
                var targetDate = _selectedDate.Date;
                var targetYear = targetDate.Year;
                var targetMonth = targetDate.Month;

                using (var context = new RockGymContext())
                {
                    // Pobranie transakcji wraz z danymi powiązanymi (pracownik, klient, oferta)
                    var transactions = context.PurchaseHistories
                        .Include(p => p.Employee)
                        .Include(p => p.Customer)
                        .Include(p => p.Offer)
                        .Where(p => p.PurchaseDate.Year == targetYear && p.PurchaseDate.Month == targetMonth)
                        .OrderBy(p => p.PurchaseDate)
                        .ToList();

                    if (transactions.Count == 0)
                    {
                        CustomMessageBox.Show("Brak transakcji w wybranym miesiącu.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                        Title = "Zapisz wyciąg z transakcji",
                        FileName = $"Wyciag_transakcji_{targetDate:MMMM_yyyy}.xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var filePath = saveFileDialog.FileName;

                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Transakcje");

                            // Nagłówki kolumn tabeli wyciągu
                            worksheet.Cell(1, 1).Value = "Numer operacji";
                            worksheet.Cell(1, 2).Value = "Pracownik";
                            worksheet.Cell(1, 3).Value = "Klient";
                            worksheet.Cell(1, 4).Value = "Id Klienta";
                            worksheet.Cell(1, 5).Value = "Nazwa oferty";
                            worksheet.Cell(1, 6).Value = "Cena";

                            // Stylizacja nagłówków
                            var headerRange = worksheet.Range("A1:F1");
                            headerRange.Style.Font.Bold = true;
                            headerRange.Style.Fill.BackgroundColor = XLColor.Black;
                            headerRange.Style.Font.FontColor = XLColor.White;
                            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            int row = 2;
                            foreach (var tx in transactions)
                            {
                                worksheet.Cell(row, 1).Value = tx.PurchaseId;
                                worksheet.Cell(row, 2).Value = tx.EmployeeDisplayName;
                                worksheet.Cell(row, 3).Value = tx.ClientDisplayName;
                                
                                if (tx.CustomerId.HasValue)
                                    worksheet.Cell(row, 4).Value = tx.CustomerId.Value;
                                else
                                    worksheet.Cell(row, 4).Value = "-";

                                worksheet.Cell(row, 5).Value = tx.OfferDisplayName;
                                worksheet.Cell(row, 6).Value = tx.Price;
                                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00\" zł\"";
                                
                                // Wyrównanie do środka dla identyfikatorów
                                worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                
                                row++;
                            }

                            // Krawędzie tabeli
                            var dataRange = worksheet.Range($"A1:F{row - 1}");
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                            worksheet.Columns().AdjustToContents();

                            workbook.SaveAs(filePath);
                        }

                        CustomMessageBox.Show("Wyciąg został pomyślnie zapisany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Błąd podczas generowania wyciągu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class EmployeeRevenueDisplayModel
    {
        public string Name { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public ulong RoleId { get; set; }
        public double Revenue { get; set; }
        public string FormattedRevenue => Revenue.ToString("C2", new CultureInfo("pl-PL"));
        public byte[]? ProfilePicture { get; set; }
        public bool IsInternet { get; set; }
    }
}
