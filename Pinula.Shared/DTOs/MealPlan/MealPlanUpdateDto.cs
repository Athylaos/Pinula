using Pinula.Shared.Enums;

namespace Pinula.Shared.DTOs
{
    public class MealPlanUpdateDto
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public MealType MealType { get; set; }
        public int Servings { get; set; }
        public List<Guid> UsersIds { get; set; } = new();
        public List<RecipeIngredientDisplayDto> Ingredients { get; set; }
    }
}
