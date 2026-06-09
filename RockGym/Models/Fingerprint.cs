using System;

namespace RockGym.Models
{
    public class Fingerprint
    {
        public ulong FingerprintId { get; set; }
        public ulong UserId { get; set; }
        public byte[] FingerprintImage { get; set; } = Array.Empty<byte>();
        public string FingerprintHash { get; set; } = string.Empty;
        public DateTime DateOfCreation { get; set; }

        public virtual User? User { get; set; }
    }
}
