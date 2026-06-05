using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class PurchaseHistory
    {
        public ulong PurchaseId { get; set; }
        public ulong? CustomerId { get; set; }
        public ulong? EmployeeId { get; set; }
        public double Price { get; set; }
        public DateTime PurchaseDate { get; set; }
        public ulong? OfferId { get; set; }

        // Właściwości nawigacyjne
        public virtual User? Customer { get; set; }
        public virtual User? Employee { get; set; }
        public virtual Offer? Offer { get; set; }
    }
}
