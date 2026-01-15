using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum ErrorType
    {
        None = 0,
        Validation,    // 400
        NotFound,      // 404
        Conflict,      // 409
        InternalError  // 500
    }
}
