using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
