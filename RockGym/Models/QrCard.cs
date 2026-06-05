using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class QrCard
    {
        public ulong CardId { get; set; }
        public ulong UserId { get; set; }
        public byte[]? QrCode { get; set; } // Zmapowane z blob
        public DateTime DateOfCreation { get; set; }

        // Właściwości nawigacyjne
        public virtual User? User { get; set; }
    }
}
