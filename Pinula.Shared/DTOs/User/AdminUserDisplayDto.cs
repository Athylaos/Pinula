using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class AdminUserDisplayDto
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public DateTime UserCreated { get; set; }
        public required string Role { get; set; } = "user";
        public string? AvatarUrl { get; set; }
        public bool CanComment { get; set; } = true;
        public bool CanCreateRecipes { get; set; } = true;
    }
}
