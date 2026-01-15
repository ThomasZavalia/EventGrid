using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public record ReserveSeatRequest(Guid SeatId, Guid UserId);
}
