namespace Pinula.Shared.DTOs;

public class CreateCategoryResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CreatedCategoryId { get; set; }
}
