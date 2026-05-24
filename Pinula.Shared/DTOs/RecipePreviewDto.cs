using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class RecipePreviewDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = "default_recipe_picture.png";
        public int CookingTime { get; set; }
        public int ServingsAmount { get; set; }
        public int Difficulty { get; set; }
        public decimal? Rating { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal? Calories { get; set; }

        public bool IsFavorite { get; set; } = false;
        public bool IsApproved { get; set; } = true;
    }
}
