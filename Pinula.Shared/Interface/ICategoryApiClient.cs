using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface ICategoryApiClient
    {
        public Task<List<CategoryDisplayDto>> GetAllCategoriesAsync();

        public Task<CategoryDisplayDto?> GetCategoryByIdAsync(Guid id);

        public Task<Guid?> SaveCategoryAsync(CategoryCreateDto dto, Stream? photoStream, string? photoName, string? contentType);

        public Task<bool> DeleteCategoryAsync(Guid categoryId);

        public Task<List<AdminCategoryDisplayDto>> GetAllCategoriesAdminAsync();
    }
}
