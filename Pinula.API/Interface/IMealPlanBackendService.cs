using System.Security.Claims;
using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface IMealPlanBackendService
{
    Task<(IResult AuthResult, Guid GroupId)> ValidateAuthUserInGroup(ClaimsPrincipal user);
    
    Task<MealPlanPreviewDto?> GetMealPlanAsync(Guid groupId, Guid id, string baseUrl, string lang);
    Task<List<MealPlanPreviewDto>> GetMealPlansAsync(Guid groupId, DateTime fromDate, DateTime toDate, string baseUrl, string lang);
    Task<GeneralResponse> CreateMealPlanAsync(Guid groupId, MealPlanCreateDto dto);
    Task<GeneralResponse> UpdateMealPlanAsync(Guid groupId, Guid id, MealPlanUpdateDto dto);
    Task<GeneralResponse> DeleteMealPlanAsync(Guid groupId, Guid id);
    Task<GeneralResponse> MarkAsCookedAsync(Guid groupId, Guid id);
    
    Task<(int StatusCode, string? Message, GroupDetailDto? Group)> CreateGroupAsync(Guid userId, GroupCreateDto dto);
    Task<GeneralResponse> JoinGroupAsync(Guid userId, string code);
    Task<GroupDetailDto?> GetMyGroupAsync(Guid userId);
    Task<GeneralResponse> LeaveGroupAsync(Guid userId);
    Task<List<UserDisplayDto>> GetGroupMembersAsync(Guid groupId, string baseUrl);
    Task<GeneralResponse> RenameGroupAsync(Guid groupId, string name);
}