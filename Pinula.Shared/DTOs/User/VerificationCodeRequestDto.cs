using Pinula.Shared.Enums;

namespace Pinula.Shared.DTOs;

public class VerificationCodeRequestDto
{
    public string Email { get; set; }
    public VerificationCodeType CodeType { get; set; }
}