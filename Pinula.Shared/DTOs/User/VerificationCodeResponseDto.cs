namespace Pinula.Shared.DTOs;

public class VerificationCodeResponseDto
{
    public bool Success { get; set; }
    public Guid? VerificationToken { get; set; }
    public bool RateLimited { get; set; }
    public bool WrongCode { get; set; }
    public bool CodeExpired { get; set; }
}