using Pinula.Shared.Enums;

namespace Pinula.API.Interface;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string email, string code, VerificationCodeType type, string culture);
}