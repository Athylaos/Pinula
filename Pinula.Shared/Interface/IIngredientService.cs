using Pinula.Shared.DTOs;
using Pinula.Shared.Services;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface IIngredientService
    {
        public Task<StatusResponse> CreateIngredientAsync(IngredientCreateDto? ingredientDto, string? barcode, Stream? photoStream, string? photoName, string? contentType);
        public Task<List<IngredientPreviewDto>> GetFilteredIngredientPreviewsAsync(IngredientFilterParameters filter);

        public Task<List<AdminIngredientDisplayDto>> AdminGetIngredients(int amount, int skip);
        public Task<Ingredient?> AdminGetIngredientDetailsAsync(Guid id);
        public Task<StatusResponse> AdminUpdateIngredientAsync(Ingredient ingredient, Stream? photoStream, string? photoName, string? contentType);
        public Task<StatusResponse> DeleteIngredientAsync(Guid id);

    }
}
