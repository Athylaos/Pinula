namespace Pinula.Shared.DTOs
{
    public class PasswordChangeDto
    {
        public string Email { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public Guid VerificationToken { get; set; }
    }
}
