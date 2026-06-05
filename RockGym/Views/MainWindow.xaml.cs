using System.Windows;
using RockGym.Models;
using RockGym.ViewModels;

namespace RockGym
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(User loggedInUser)
        {
            InitializeComponent();
            DataContext = new MainViewModel(loggedInUser);
        }
    }
}