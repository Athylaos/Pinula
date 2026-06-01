using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public enum SortBy
    {
        Newest,
        Oldest,
        Rating,
        CookingTime,
        Calories,
    }

    public class RecipeFilterParameters
    {
        public string? SearchTerm { get; set; }
        public Guid? CategoryId { get; set; }
        public int? MaxCookingTime { get; set; }
        public int? MinRating { get; set; }
        public int? MaxDifficulty { get; set; }
        public bool OnlyFavorites { get; set; } = false;
        public bool OnlyMine { get; set; } = false;
        public int Amount { get; set; } = 10;
        public int Skip { get; set; } = 0;
        public int? MaxCalories { get; set; }
        public SortBy Sort { get; set; } = SortBy.Newest;
        public bool SortDescending { get; set; } = true;

        public bool IncludeUnapproved { get; set; } = false;
    
        public RecipeFilterParameters Clone()
        {
            return (RecipeFilterParameters)this.MemberwiseClone();
        }

    }
}
