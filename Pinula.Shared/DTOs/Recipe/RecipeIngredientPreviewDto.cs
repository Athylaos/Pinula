namespace Pinula.Shared.DTOs
{
    public class RecipeIngredientPreviewDto
    {
        public decimal Quantity { get; set; }
        public decimal ConversionFactor { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; }

        public Guid IngredientId { get; set; }
        public Guid UnitId { get; set; }
        
        public decimal? QuantityInInventory { get; set; }
    }
}
