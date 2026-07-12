using Pinula.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Interface
{
    public interface IMealPlanService
    {
        Task<List<MealPlanPreviewDto>?> GetMyGroupMealPlansAsync(DateTime fromDate, DateTime toDate);
        Task<bool> AddRecipeToPlanAsync(MealPlanCreateDto dto);
        Task<bool> RemoveRecipeFromPlanAsync(Guid mealPlanId);
        Task<bool> UpdateMealPlanAsync(MealPlanUpdateDto dto);

        Task<GroupDetailDto?> CreateGroupAsync(GroupCreateDto dto);
        Task<bool> JoinGroupAsync(string code);
        Task<bool> RenameGroupAsync(string name);
        Task<GroupDetailDto?> GetMyGroupAsync();
        Task<bool> LeaveGroupAsync();
        Task<List<UserDisplayDto>> GetMembersAsync();
    }
}
