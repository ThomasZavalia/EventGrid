using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Events
{
    public record PaymentSucceededEvent
    {
        public Guid SeatId { get; init; }
        public Guid UserId { get; init; }
        public DateTime ProcessedAt { get; init; }
    }
}
