using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class Offer
    {
        public ulong OfferId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Duration { get; set; }


        public virtual ICollection<PurchaseHistory> PurchaseHistories { get; set; } = new List<PurchaseHistory>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
