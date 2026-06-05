using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class User
    {
        public ulong UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public ulong RoleId { get; set; }


        // Właściwości nawigacyjne
        public virtual Role? Role { get; set; }
        public virtual ICollection<Entrance> Entrances { get; set; } = new List<Entrance>();
        public virtual ICollection<QrCard> QrCards { get; set; } = new List<QrCard>();
        public virtual ICollection<PurchaseHistory> CustomerPurchases { get; set; } = new List<PurchaseHistory>();
        public virtual ICollection<PurchaseHistory> EmployeePurchases { get; set; } = new List<PurchaseHistory>();
        public virtual ICollection<Event> CommandedEvents { get; set; } = new List<Event>();
        public virtual ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
    }
}
