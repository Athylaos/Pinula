using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class RecipeCreateDto
    {
        public string Title {  get; set; }

        public string PhotoUrl { get; set; } = "default_recipe_picture.png";

        public short CookingTime { get; set; }

        public short ServingsAmount { get; set; }

        public Guid ServingUnit { get; set; }

        public short Difficulty { get; set; }

        public List<RecipeIngredientPreviewDto> RecipeIngredients { get; set; } = new();

        public List<RecipeStepDisplayDto> RecipeSteps { get; set; } = new List<RecipeStepDisplayDto>();

        public List<Guid> CategoriesIds { get; set; } = new List<Guid>();

    }
}
