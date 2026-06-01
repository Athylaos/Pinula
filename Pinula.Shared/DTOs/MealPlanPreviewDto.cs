using Pinula.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class MealPlanPreviewDto
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public MealType MealType { get; set; }
        public int Servings { get; set; }

        public List<UserDisplayDto> UsersPreviews { get; set; }
        public Guid RecipeId { get; set; }
        public required string RecipeName { get; set; }
        public string? RecipePhotoUrl { get; set; }
    }
}
