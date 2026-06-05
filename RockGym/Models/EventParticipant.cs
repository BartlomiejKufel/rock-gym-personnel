using System;
using System.Collections.Generic;
using System.Text;

namespace RockGym.Models
{
    public class EventParticipant
    {
        public ulong EventId { get; set; }
        public ulong ParticipantId { get; set; }
        public DateTime DateOfRegistration { get; set; }

        public virtual Event? Event { get; set; }
        public virtual User? Participant { get; set; }
    }
}
