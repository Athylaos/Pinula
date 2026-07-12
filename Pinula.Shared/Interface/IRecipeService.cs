using Pinula.Shared.Models;
using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IRecipeService
    {
        public Task<Guid?> SaveRecipeAsync(RecipeCreateDto createDto, Stream? photoStream, string? photoName, string? contentType);
        public Task<RecipeDetailsDto?> GetRecipeDetailsAsync(Guid id);
        public Task<Guid?> UpdateRecipeAsync(RecipeCreateDto createDto,Guid recipeId, Stream? photoStream, string? photoName, string? contentType);
        public Task<bool> DeleteRecipeAsync(Guid id);
        public Task<List<RecipePreviewDto>> GetFilteredRecipePreviewsAsync(RecipeFilterParameters filterParametrs, CancellationToken? ct);

        public Task<bool?> ChangeFavoriteAsync(Guid recipeId);

        public Task<CommentPostResponse?> PostCommentAsync(Comment comment);
        public Task<CommentDeleteResponse?> DeleteCommentAsync(Guid commentId);
        public Task<CommentDeleteResponse?> UpdateCommentAsync(Comment comment);

        Task<bool> AdminToggleRecipeApprovalAsync(Guid recipeId);
        Task<bool> AdminToggleRecipeCheckedAsync(Guid recipeId);
        Task<List<AdminCommentDto>> GetAdminCommentsAsync();
        Task<bool> AdminToggleCommentApprovalAsync(Guid commentId);

    }
}
