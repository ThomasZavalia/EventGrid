using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Events
{
    public record PaymentFailedEvent
    {
        public Guid SeatId { get; init; }
        public Guid UserId { get; init; }
        public string Reason { get; init; }
        public DateTime FailedAt { get; init; }
    }
}
