using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface IIngredientBackendService
{
    public Task<List<IngredientPreviewDto>> GetFilteredPreviews(IngredientFilterParameters filter, string baseUrl, string lang);
    public Task<CreateIngredientResponse> Create(IngredientCreateDto createDto, IFormFile? imageFile, IWebHostEnvironment webHostEnvironment, string lang, Guid userId);
    

    public Task<List<AdminIngredientPreviewDto>> AdminGetPreviews(int amount, int skip, string baseUrl);
    public Task<AdminIngredientDisplayDto?> AdminGetDetails(Guid id, string baseUrl, string lang);
    public Task<GeneralResponse> AdminDelete(Guid id);
    public Task<GeneralResponse> AdminUpdate(IngredientCreateDto dto, IFormFile? imageFile, IWebHostEnvironment webHostEnvironment, string lang, Guid userId);

    public Task<GeneralResponse> AdminToggleApproval(Guid id);
    public Task<GeneralResponse> AdminToggleChecked(Guid id);

    public Task<List<ShoppingCategoryDisplayDto>> GetShoppingCategories(string baseUrl, string lang);
}