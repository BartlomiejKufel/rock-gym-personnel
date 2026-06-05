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
        public virtual User? Customer { get; set; }
        public virtual User? Employee { get; set; }
        public virtual Offer? Offer { get; set; }

        public string ClientDisplayName => Customer != null ? $"{Customer.Name} {Customer.Surname}" : "Brak";
        public string EmployeeDisplayName => Employee != null ? $"{Employee.Name} {Employee.Surname}" : "Internet";
        public string OfferDisplayName => Offer != null ? Offer.Name : "Brak oferty";
    }
}
