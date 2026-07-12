using Pinula.Shared.DTOs;
using Pinula.Shared.Services;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface IIngredientService
    {
        public Task<GeneralResponse> CreateIngredientAsync(IngredientCreateDto? ingredientDto, string? barcode, Stream? photoStream, string? photoName, string? contentType);
        public Task<List<IngredientPreviewDto>> GetFilteredIngredientPreviewsAsync(IngredientFilterParameters filter);

        public Task<List<AdminIngredientDisplayDto>> AdminGetIngredients(int amount, int skip);
        public Task<Ingredient?> AdminGetIngredientDetailsAsync(Guid id);
        public Task<GeneralResponse> AdminUpdateIngredientAsync(Ingredient ingredient, Stream? photoStream, string? photoName, string? contentType);
        public Task<GeneralResponse> DeleteIngredientAsync(Guid id);
        public Task<bool> AdminToggleIngredientApprovalAsync(Guid id);
        public Task<bool> AdminToggleIngredientCheckedAsync(Guid id);

    }
}
