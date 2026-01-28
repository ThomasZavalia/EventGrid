using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity; 

namespace Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
