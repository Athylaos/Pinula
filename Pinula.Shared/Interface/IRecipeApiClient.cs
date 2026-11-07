using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IRecipeApiClient
    {
        Task<List<RecipePreviewDto>> GetFilteredRecipePreviewsAsync(RecipeFilterParameters filter, CancellationToken? ct = null);
        Task<RecipeDetailsDto?> GetRecipeDetailsAsync(Guid recipeId);
        
        Task<GeneralResponse> SaveRecipeAsync(RecipeCreateDto createDto, Stream? photoStream, string? photoName, string? contentType);
        Task<GeneralResponse> UpdateRecipeAsync(RecipeCreateDto createDto, Guid recipeId, Stream? photoStream, string? photoName, string? contentType);
        Task<GeneralResponse> DeleteRecipeAsync(Guid id);

        Task<ToggleFavoriteResponse> ChangeFavoriteAsync(Guid recipeId);

        Task<CommentPostResponse> PostCommentAsync(CommentCreateDto comment);
        Task<CommentDeleteResponse> UpdateCommentAsync(CommentCreateDto comment);
        Task<CommentDeleteResponse> DeleteCommentAsync(Guid commentId);

        Task<List<AdminCommentDto>> GetAdminCommentsAsync();
        Task<ToggleBoolResponse> AdminToggleRecipeApprovalAsync(Guid recipeId);
        Task<ToggleBoolResponse> AdminToggleRecipeCheckedAsync(Guid recipeId);
        Task<GeneralResponse> AdminToggleCommentApprovalAsync(Guid commentId);
    }
}
