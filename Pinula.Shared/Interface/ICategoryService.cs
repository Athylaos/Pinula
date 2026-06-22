using Pinula.Shared.DTOs;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface ICategoryService
    {
        public Task<List<CategoryDisplayDto>> GetAllCategoriesAsync();

        public Task<CategoryDisplayDto?> GetCategoryByIdAsync(Guid id);

        public Task<List<CategoryDisplayDto>> GetMainCategoriesAsync();

        public Task<Guid?> SaveCategoryAsync(CategoryCreateDto dto, Stream? photoStream, string? photoName, string? contentType);

        public Task<bool> DeleteCategoryAsync(Guid categoryId);

        public Task<List<AdminCategoryDisplayDto>> GetAllCategoriesAdminAsync();
    }
}
