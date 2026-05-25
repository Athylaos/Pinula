using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class GroupDetailDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string InviteCode { get; set; }
    }
}
