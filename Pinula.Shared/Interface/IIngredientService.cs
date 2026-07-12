using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IIngredientService
    {
        public Task<GeneralResponse> CreateIngredientAsync(IngredientCreateDto? ingredientDto, string? barcode, Stream? photoStream, string? photoName, string? contentType);
        public Task<List<IngredientPreviewDto>> GetFilteredIngredientPreviewsAsync(IngredientFilterParameters filter);

        public Task<List<AdminIngredientPreviewDto>> AdminGetIngredients(int amount, int skip);
        public Task<AdminIngredientDisplayDto?> AdminGetIngredientDetailsAsync(Guid id);
        public Task<GeneralResponse> AdminUpdateIngredientAsync(IngredientCreateDto ingredient, Stream? photoStream, string? photoName, string? contentType);
        public Task<GeneralResponse> DeleteIngredientAsync(Guid id);
        public Task<bool> AdminToggleIngredientApprovalAsync(Guid id);
        public Task<bool> AdminToggleIngredientCheckedAsync(Guid id);

    }
}
