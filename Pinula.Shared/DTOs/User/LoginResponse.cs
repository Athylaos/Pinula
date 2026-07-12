using Pinula.Shared.Models;

namespace Pinula.Shared.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; }

        public User User { get; set; } = null!;

    }
}
