namespace Pinula.Shared.DTOs.DataGenerator;

public class DGIngredientUpdateDto
{
    public Guid Id { get; set; }
    public Guid DefaultUnitId { get; set; }
    public Guid ShoppingCategoryId { get; set; }
    
    public bool IsVegan { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsLactoseFree { get; set; }
    
    public string ImageUrl { get; set; } 
    
    public string? NutriScore { get; set; }
    public int? NovaClassification { get; set; }

    public List<DGIngredientUpdateDto.IngredientUnitUpdateDto> AdditionalUnits { get; set; } = new();
    
    
    public class IngredientUnitUpdateDto
    {
        public Guid UnitId { get; set; }
        public decimal AmountInGrams { get; set; }
    }
}