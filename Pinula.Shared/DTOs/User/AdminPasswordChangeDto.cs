namespace Pinula.Shared.DTOs
{
    public class AdminPasswordChangeDto
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
