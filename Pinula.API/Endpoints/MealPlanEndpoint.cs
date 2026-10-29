using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Models;
using System.Globalization;
using System.Security.Claims;
using Pinula.API.Interface;

namespace Pinula.API.Endpoints
{
    public static class MealPlanEndpoint
    {
        public static void MapMealPlanEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/mealplan").RequireRateLimiting("GeneralApiPolicy");
            
            //---------------------------------------------------------------Get meal plan 
            group.MapGet("/get/{id:guid}", async (HttpRequest request, Guid id, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                string baseUrl = $"{request.Scheme}://{request.Host}";

                var mealplan = await mealPlanService.GetMealPlanAsync(groupId, id, baseUrl, lang);
            
                return mealplan is not null 
                    ? Results.Ok(mealplan) 
                    : Results.NotFound("MealPlan not found");
            }).RequireAuthorization();

            //---------------------------------------------------------------Get meal plans
            group.MapGet("/get", async (HttpRequest request, DateTime fromDate, DateTime toDate, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                string baseUrl = $"{request.Scheme}://{request.Host}";

                var mealplans = await mealPlanService.GetMealPlansAsync(groupId, fromDate, toDate, baseUrl, lang);
                return Results.Ok(mealplans);
            }).RequireAuthorization();

            //---------------------------------------------------------------Create meal plan
            group.MapPost("/add", async (MealPlanCreateDto dto, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                var result = await mealPlanService.CreateMealPlanAsync(groupId, dto);
            
                return result.StatusCode == 201 
                    ? Results.Created($"/mealplan/get/{result.Id}", result) 
                    : Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Delete meal plan
            group.MapDelete("/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                var result = await mealPlanService.DeleteMealPlanAsync(groupId, id);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Update meal plan
            group.MapPut("/update/{id:guid}", async (Guid id, MealPlanUpdateDto dto, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                var result = await mealPlanService.UpdateMealPlanAsync(groupId, id, dto);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();
            
            //---------------------------------------------------------------Toggle as cooked
            group.MapPost("/markAsCooked/{id:guid}", async (Guid id, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                var result = await mealPlanService.MarkAsCookedAsync(groupId, id);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Create group
            group.MapPost("/group/create", async (GroupCreateDto dto, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var userId = user.GetUserId();
                var result = await mealPlanService.CreateGroupAsync(userId, dto);
            
                if (result.StatusCode == 201) return Results.Ok(result.Group);
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Join group
            group.MapPost("/group/join/{code}", async (string code, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var userId = user.GetUserId();
                var result = await mealPlanService.JoinGroupAsync(userId, code);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Get my group
            group.MapGet("/group/my", async (ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var userId = user.GetUserId();
                var groupDetail = await mealPlanService.GetMyGroupAsync(userId);
            
                return groupDetail is not null 
                    ? Results.Ok(groupDetail) 
                    : Results.BadRequest("User is not in group or group not found");
            }).RequireAuthorization();

            //---------------------------------------------------------------Leave my group
            group.MapPost("/group/leave", async (ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var userId = user.GetUserId();
                var result = await mealPlanService.LeaveGroupAsync(userId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Get members
            group.MapGet("/group/members", async (HttpRequest request, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                string baseUrl = $"{request.Scheme}://{request.Host}";
                var members = await mealPlanService.GetGroupMembersAsync(groupId, baseUrl);
                return Results.Ok(members);
            }).RequireAuthorization();

            //---------------------------------------------------------------Rename my group
            group.MapPost("/group/rename/{name}", async (string name, ClaimsPrincipal user, IMealPlanBackendService mealPlanService) =>
            {
                var (authResult, groupId) = await mealPlanService.ValidateAuthUserInGroup(user);
                if (authResult != Results.Ok()) return authResult;

                var result = await mealPlanService.RenameGroupAsync(groupId, name);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();


        }
    }
}
