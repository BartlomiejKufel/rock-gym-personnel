using RockGym.ViewModels;
using System.Windows;

namespace RockGym.Views
{
    public partial class FingerprintManagerWindow : Window
    {
        public FingerprintManagerWindow(ulong userId, string clientName)
        {
            InitializeComponent();
            DataContext = new FingerprintManagerViewModel(userId, clientName);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
