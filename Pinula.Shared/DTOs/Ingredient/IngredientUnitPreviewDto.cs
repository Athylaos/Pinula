namespace Pinula.Shared.DTOs
{
    public class IngredientUnitPreviewDto
    {
        public decimal AmountInGrams { get; set; }

        public virtual IngredientPreviewDto Ingredient { get; set; } = null!;

        public virtual UnitPreviewDto Unit { get; set; } = null!;
    }
}
