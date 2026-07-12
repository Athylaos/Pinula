namespace Pinula.Shared.DTOs
{
    public class IngredientPreviewDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public UnitPreviewDto DefaultUnit { get; set; } = new();

        public UnitPreviewDto SelectedUnit { get; set; } = null!;

        public List<UnitPreviewDto> IngredientUnits { get; set; } = new List<UnitPreviewDto>();

    }
}
