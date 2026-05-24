using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; }

        public User User { get; set; } = null!;

    }
}
