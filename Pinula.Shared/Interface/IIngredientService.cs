using Pinula.Shared.DTOs;
using Pinula.Shared.Services;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface IIngredientService
    {
        public Task<CreateIngredientResponse> CreateIngredientAsync(IngredientCreateDto ingredientDto);
        public Task<Ingredient?> GetIngredientAsync(Guid id);
        public Task RemoveIngredientAsync(Guid id);
        public Task UpdateIngredientAsync(Ingredient ingredient);

        public Task<List<IngredientPreview>> GetIngredientPreviewsAsync(int amount);
        public Task<List<IngredientPreview>> GetFilteredIngredientPreviewsAsync(string searchTerm, int? amount);
    }
}
