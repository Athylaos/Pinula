namespace Pinula.Shared.DTOs
{
    public class RecipeIngredientDisplayDto
    {
        public UnitPreviewDto Unit { get; set; } = new();
        public IngredientPreviewDto Ingredient { get; set; } = new();

        public decimal? Quantity { get; set; }
        public decimal? ConversionFactor { get; set; }
    }
}
