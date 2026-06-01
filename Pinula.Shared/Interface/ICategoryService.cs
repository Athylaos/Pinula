using Pinula.Shared.DTOs;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface ICategoryService
    {
        public Task<List<Category>> GetRecepieCategoriesAsync(Guid recepieId);

        public Task<List<Category>> GetAllCategoriesAsync();

        public Task<Category?> GetCategoryByIdAsync(Guid id);

        public Task<List<Category>> GetMainCategoriesAsync();

        public Task<List<Category>> GetChildCategoriesAsync(Guid parentId);

        public Task<Guid?> SaveCategoryAsync(CategoryCreateDto dto, Stream? photoStream, string? photoName, string? contentType);

        public Task<bool> DeleteCategoryAsync(Guid categoryId);
    }
}
