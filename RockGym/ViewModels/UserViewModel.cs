using Microsoft.EntityFrameworkCore;
using RockGym.Models;
using RockGym.Services;
using System.Collections.ObjectModel;

namespace RockGym.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        public ObservableCollection<User> Users { get; set; }

        public UsersViewModel()
        {
            // Wyciąganie danych przy użyciu EF Core
            using (var context = new RockGymContext())
            {
                var usersFromDb = context.Users
                    .Include(u => u.Role) // Dociąga obiekt roli (np. "Klient", "Instruktor")
                    .ToList();

                Users = new ObservableCollection<User>(usersFromDb);
            }
        }
    }
}
