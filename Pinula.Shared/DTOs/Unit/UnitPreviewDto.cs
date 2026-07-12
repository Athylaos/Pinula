namespace Pinula.Shared.DTOs
{
    public class UnitPreviewDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; }
    }
}
