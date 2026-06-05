using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class Entrance
    {
        public ulong EntranceId { get; set; }
        public ulong UserId { get; set; }
        public DateTime DateOfEntry { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? TimeSpent { get; set; }

        // Relacja: Wejście należy do konkretnego użytkownika
        public virtual User? User { get; set; }
    }
}
