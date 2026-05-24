using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IDbSeeder
    {
        Task<(bool IsSuccess, string Message, Guid? EventId, int SeatsCreated)> SeedAsync();
    }
}
