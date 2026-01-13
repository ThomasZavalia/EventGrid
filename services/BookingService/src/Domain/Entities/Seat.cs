using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Seat
    {
        public Guid Id { get; set; }
        public string Section { get; set; }=string.Empty;

        public string Number { get; set; }=string.Empty;

        public decimal Price { get; set; }

        public Guid EventId { get; set; }
        public Event? Event {  get; set; }

        public SeatStatus Status { get; set; }
        public Guid UserId { get; set; }

        public uint RowVersion { get; set; }
    }
}
