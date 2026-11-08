namespace Pinula.Shared.DTOs
{
    public class LoginResponse : GeneralResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserSurname { get; set; } = string.Empty;

    }
}
