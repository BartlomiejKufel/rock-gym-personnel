using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class QrCard
    {
        public ulong CardId { get; set; }
        public ulong UserId { get; set; }
        public byte[]? QrCode { get; set; }
        public DateTime DateOfCreation { get; set; }

        public virtual User? User { get; set; }
    }
}
