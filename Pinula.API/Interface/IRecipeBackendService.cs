using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;
public interface IRecipeBackendService
{
    Task<List<RecipePreviewDto>> GetFilteredPreviewsAsync(RecipeFilterParameters filter, Guid? currentUserId, string baseUrl, string lang);
    Task<RecipeDetailsDto?> GetRecipeDetailsAsync(Guid recipeId, Guid? currentUserId, string baseUrl, string lang);
    Task<GeneralResponse> CreateRecipeAsync(Guid userId, RecipeCreateDto dto, IFormFile? imageFile, string lang);
    Task<GeneralResponse> UpdateRecipeAsync(Guid id, Guid userId, RecipeCreateDto dto, IFormFile? imageFile, string lang);
    Task<GeneralResponse> DeleteRecipeAsync(Guid id, Guid userId);

    Task<ToggleFavoriteResponse> ToggleFavoriteAsync(Guid recipeId, Guid userId);
    
    Task<CommentPostResponse> PostCommentAsync(Guid userId, CommentCreateDto dto, string lang);
    Task<CommentDeleteResponse> DeleteCommentAsync(Guid commentId, Guid userId);
    Task<CommentDeleteResponse> UpdateCommentAsync(Guid commentId, Guid userId, CommentCreateDto dto);

    Task<ToggleBoolResponse> AdminToggleApprovalAsync(Guid recipeId);
    Task<ToggleBoolResponse> AdminToggleCheckedAsync(Guid recipeId);
    Task<List<AdminCommentDto>> AdminGetAllCommentsAsync(string lang);
    Task<GeneralResponse> AdminToggleCommentApprovalAsync(Guid commentId);
}