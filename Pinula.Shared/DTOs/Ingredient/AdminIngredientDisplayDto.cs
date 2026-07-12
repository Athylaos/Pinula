namespace Pinula.Shared.DTOs
{
    public class AdminIngredientDisplayDto
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsApproved { get; set; }
        public bool Checked { get; set; } = false;
        public DateTime IngredientCreated { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;

        public Dictionary<string, string> Names { get; set; } = new();
        public Guid DefaultUnitId { get; set; }
        public Guid ShoppingCategoryId { get; set; }
        public Guid? BaseIngredientId { get; set; }
        public string? OffCategoryTag { get; set; }
        public decimal? EdibleRatio { get; set; }

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

        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }

        public string? NutriScore { get; set; }
        public int? NovaClassification { get; set; }

        public virtual UnitPreviewDto DefaultUnit { get; set; } = null!;
        public virtual AdminShoppingCategoryDisplayDto ShoppingCategory { get; set; } = null!;
        public virtual List<IngredientUnitPreviewDto> AdditionalUnits { get; set; } = new();
    }
}
