using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Events
{
    public record PaymentInitiatedEvent
    {
        public Guid SeatId { get; init; } 
        public Guid UserId { get; init; }
        public decimal Amount { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
