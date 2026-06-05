using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class Notification
    {
        public ulong NotificationId { get; set; }
        public ulong? CreatorId { get; set; } // Klucz obcy do users (admin/pracownik)
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Właściwości nawigacyjne
        public virtual User? Creator { get; set; }
    }
}
