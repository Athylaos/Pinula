namespace Pinula.Shared.DTOs
{
    public class GeneralResponse
    {
        public bool Successful { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
