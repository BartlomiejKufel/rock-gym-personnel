using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class Role
    {
        public ulong RoleId { get; set; }
        public string Name { get; set; } = string.Empty;

        // Relacja 1-do-wielu: Jedna rola może być przypisana do wielu użytkowników
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
