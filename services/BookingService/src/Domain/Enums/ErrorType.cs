using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum ErrorType
    {
        None = 0,
        Validation,   
        NotFound,      
        Conflict,      
        InternalError  
    }
}
