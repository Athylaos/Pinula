using Pinula.Shared.Enums;

namespace Pinula.Shared.DTOs;

public class VerificationCodeVerifyDto
{
    public string Email { get; set; }
    public VerificationCodeType CodeType { get; set; }
    public string Code { get; set; }
}