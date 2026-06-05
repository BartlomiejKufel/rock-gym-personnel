using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class Event
    {
        public ulong EventId { get; set; }
        public ulong? InstructorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventColor { get; set; } = "#e3e3e3";
        public int ParticipantsLimit { get; set; }
        public ulong? OfferId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual User? Instructor { get; set; }
        public virtual Offer? Offer { get; set; }
        public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

        public string ParticipantsRatio => $"{EventParticipants?.Count ?? 0}/{ParticipantsLimit}";
    }
}