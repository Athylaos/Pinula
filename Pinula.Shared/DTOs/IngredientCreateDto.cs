using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class IngredientCreateDto
    {
        public Dictionary<string, string> Names { get; set; } = new();
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public Guid DefaultUnitId { get; set; }

        public string? NutriScore { get; set; }
        public int? NovaClassification { get; set; }

        public decimal Calories { get; set; }
        public decimal Proteins { get; set; }
        public decimal Fats { get; set; }
        public decimal SaturatedFats { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Sugars { get; set; }
        public decimal Fiber { get; set; }
        public decimal Salt { get; set; }

        public bool IsVegan { get; set; }
        public bool IsVegetarian { get; set; }
        public bool IsGlutenFree { get; set; }
        public bool IsLactoseFree { get; set; }

        public List<CreateIngredientUnitDto> AdditionalUnits { get; set; } = new();
        public List<string>? CategoryTags;

    }

    public class CreateIngredientUnitDto
    {
        public Guid UnitId { get; set; }
        public decimal ToDefaultUnit { get; set; }
    }
}
