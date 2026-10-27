namespace Pinula.Shared.DTOs;

public class CreateIngredientResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CreatedIngredientId { get; set; }
}