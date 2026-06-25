using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class RecipeIngredientPreviewDto
    {
        public UnitPreviewDto Unit { get; set; } = new();
        public IngredientPreviewDto Ingredient { get; set; } = new();

        public decimal? Quantity { get; set; }
        public decimal? ConversionFactor { get; set; }
    }
}
