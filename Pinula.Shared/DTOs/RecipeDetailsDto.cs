using Pinula.Shared.Enums;
using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class RecipeDetailsDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; }

        public string PhotoUrl { get; set; }

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

        public virtual ICollection<CommentPreview> Comments { get; set; } = new List<CommentPreview>();

        public virtual ICollection<RecipeIngredientDetailDto> RecipeIngredients { get; set; } = new List<RecipeIngredientDetailDto>();

        public virtual ICollection<RecipeStep> RecipeSteps { get; set; } = new List<RecipeStep>();

        public virtual Unit ServingUnit { get; set; } = null!;

        public required string UserName { get; set; } = string.Empty;
        public required string UserSurname { get; set; } = string.Empty;

        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();



        public bool IsFavorite { get; set; }

    }
}
