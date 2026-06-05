using System.Windows;
using System.Windows.Controls;
using RockGym.ViewModels;

namespace RockGym.Views
{
    /// <summary>
    /// Logika interakcji dla klasy OffersView.xaml
    /// </summary>
    public partial class OffersView : UserControl
    {
        public OffersView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is OffersViewModel vm)
            {
                // Pokaż lub ukryj kolumnę akcji w zależności od roli (tylko admin o role_id = 1 widzi)
                if (ActionsColumn != null)
                {
                    ActionsColumn.Visibility = vm.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                }

                // Dostosuj tekst pomocniczy w pustej karcie
                if (InstructionsTextBlock != null)
                {
                    InstructionsTextBlock.Text = vm.IsAdmin 
                        ? "Użyj przycisku u góry, aby dodać pierwszą ofertę." 
                        : "Poczekaj na dodanie ofert przez administratora.";
                }
            }
        }
    }
}
