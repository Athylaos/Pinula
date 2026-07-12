namespace Pinula.Shared.DTOs
{
    public class GroupDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
    }
}
