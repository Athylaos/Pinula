namespace Pinula.Shared.DTOs
{
    public class RecipeIngredientPreviewDto
    {
        public decimal Quantity { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;

        public Guid IngredientId { get; set; }
        public Guid UnitId { get; set; }
    }
}
