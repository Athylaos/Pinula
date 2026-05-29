using Pinula.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Interface
{
    public interface IMealPlanService
    {
        Task<List<MealPlanPreviewDto>?> GetMyGroupMealPlansAsync(DateTime fromDate, DateTime toDate);
        Task<bool> AddRecipeToPlanAsync(CreateMealPlanDto dto);
        Task<bool> RemoveRecipeFromPlanAsync(Guid mealPlanId);
        Task<bool> UpdateMealPlanAsync(UpdateMealPlanDto dto);

        Task<GroupDetailDto?> CreateGroupAsync(CreateGroupDto dto);
        Task<bool> JoinGroupAsync(string code);
        Task<GroupDetailDto?> GetMyGroupAsync();
        Task<bool> LeaveGroupAsync();
        Task<List<UserDisplayDto>> GetMembersAsync();
    }
}
