using Pinula.Shared.Enums;

namespace Pinula.Shared.DTOs
{
    public class MealPlanCreateDto
    {
        public DateOnly Date { get; set; }
        public MealType MealType { get; set; }
        public Guid RecipeId { get; set; }
        public Guid GroupId { get; set; }
        public int Servings { get; set; } = 1;
        public List<Guid> UsersId { get; set; } = new();
    }
}
