using Domain.Enums;
using Domain.Primitives;
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

        public SeatStatus Status { get; private set; }
        public Guid? UserId { get; private set; }

        private Seat() { }

       
        public Seat(string section, string number, decimal price, Guid eventId)
        {
            Id = Guid.NewGuid();
            Section = section;
            Number = number;
            Price = price;
            EventId = eventId;
            Status = SeatStatus.Available;
        }


        public Result Reserve(Guid userId)
        {
            if (Status != SeatStatus.Available)
            {
                return Result.Fail($"El asiento {Number} no esta disponible.");
            }
            Status = SeatStatus.Reserved;
            UserId = userId;
            return Result.Success();
        }

        public void Realese() 
        {
        Status = SeatStatus.Available;
            UserId = Guid.Empty;
        }

    }
}
