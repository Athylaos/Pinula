using Pinula.Shared.Enums;

namespace Pinula.Shared.DTOs
{
    public class MealPlanPreviewDto
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public MealType MealType { get; set; }
        public int Servings { get; set; }

        public List<UserDisplayDto> UsersPreviews { get; set; } = new();
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string? RecipePhotoUrl { get; set; }
    }
}
