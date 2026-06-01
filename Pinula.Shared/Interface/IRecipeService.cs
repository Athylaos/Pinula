using Pinula.Shared.Models;
using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IRecipeService
    {
        public Task<Guid?> SaveRecipeAsync(RecipeCreateDto createDto, Stream? photoStream, string? photoName, string? contentType);
        public Task<RecipeDetailsDto?> GetRecipeDetailsAsync(Guid id);
        public Task<Guid?> UpdateRecipeAsync(RecipeCreateDto createDto,Guid recipeId, Stream? photoStream, string? photoName, string? contentType);
        public Task DeleteRecipeAsync(Guid id);

        public Task<List<Recipe>> GetRecipesAsync(int amount);

        public Task<List<RecipePreviewDto>> GetRecipePreviewsAsync(int amount);
        public Task<List<RecipePreviewDto>> GetFilteredRecipePreviewsAsync(RecipeFilterParameters filterParametrs, CancellationToken? ct);

        public Task<bool?> ChangeFavoriteAsync(Guid recipeId);
        public Task<bool> IsFavoriteAsync(Guid recipeId, Guid userId);

        public Task<PostCommentResponse?> PostCommentAsync(Comment comment);
        public Task<PostCommentResponse?> GetRecipeCommentAsync(Guid recipeId, Guid? userId); // userId as an prep for future admin functions ??
        public Task<DeleteCommentResponse?> DeleteRecipeCommentAsync(Guid recipeId, Guid? userId); // userId as an prep for future admin functions ??

        Task<bool> AdminToggleRecipeApprovalAsync(Guid recipeId);
        Task<List<AdminCommentDto>> GetAdminCommentsAsync();
        Task<bool> AdminToggleCommentApprovalAsync(Guid commentId);

    }
}
