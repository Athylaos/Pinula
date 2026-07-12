namespace Pinula.Shared.DTOs
{
    public class UserDisplayDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime UserCreated { get; set; }
        public int PostedRecipes { get; set; }
        public int PostedComments { get; set; }
        public decimal AvgRating { get; set; }
        public bool CanComment { get; set; }
        public bool CanCreateRecipes { get; set; }
    }
}
