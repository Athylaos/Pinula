using Pinula.Shared.DTOs;
using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface ICategoryBackendService
{
    public Task<List<CategoryDisplayDto>> GetCategoriesTree(string baseUrl, string lang);
    public Task<CategoryDisplayDto?> GetCategoryWithChildren(string baseUrl, string lang, Guid id);
    public Task<List<AdminCategoryDisplayDto>> GetAllAdminCategoriesFlattened(string baseUrl);

    public Task<CreateCategoryResponse> CreateCategory(CategoryCreateDto createDto, IFormFile? imageFile, IWebHostEnvironment webHostEnvironment);

    public Task<GeneralResponse> DeleteCategory(Guid id, IWebHostEnvironment webHostEnvironment);
}