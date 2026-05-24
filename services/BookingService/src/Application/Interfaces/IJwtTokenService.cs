using Domain.Entities;

namespace Application.Interfaces
{
    public interface IJwtTokenService
    {
       
        string GenerateUserToken(ApplicationUser user);

      
        string GenerateQueuePassToken(string userId);
    }
}
