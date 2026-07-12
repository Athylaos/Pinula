namespace Pinula.Shared.DTOs
{
    public class AdminIngredientDisplayDto
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsApproved { get; set; }
        public bool Checked { get; set; }
        public DateTime IngredientCreated { get; set; }

        public Dictionary<string, string> Names { get; set; } = new();
        public string? ImageUrl { get; set; }
    }
}
