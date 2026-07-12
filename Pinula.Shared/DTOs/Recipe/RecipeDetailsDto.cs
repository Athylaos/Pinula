using Pinula.Shared.Enums;

namespace Pinula.Shared.DTOs
{
    public class RecipeDetailsDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string OriginalLanguage { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;

        public short CookingTime { get; set; }

        public short ServingsAmount { get; set; }

        public DifficultyLevel Difficulty { get; set; }

        public decimal? Calories { get; set; }

        public decimal? Proteins { get; set; }

        public decimal? Fats { get; set; }

        public decimal? Carbohydrates { get; set; }

        public decimal? Fiber { get; set; }

        public DateTime RecipeCreated { get; set; }

        public decimal? Rating { get; set; }

        public int? UsersRated { get; set; }

        public List<CommentDisplayDto> Comments { get; set; } = new();

        public List<RecipeIngredientPreviewDto> RecipeIngredients { get; set; } = new();

        public List<RecipeStepDisplayDto> RecipeSteps { get; set; } = new();

        public UnitPreviewDto ServingUnit { get; set; } = new();

        public required string UserName { get; set; } = string.Empty;
        public required string UserSurname { get; set; } = string.Empty;

        public List<CategoryDisplayDto> Categories { get; set; } = new();



        public bool IsFavorite { get; set; }
        public bool UserAlreadyRated { get; set; } = false;

    }
}
