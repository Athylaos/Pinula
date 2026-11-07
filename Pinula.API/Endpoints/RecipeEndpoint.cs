using DeepL;
using DeepL.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Enums;
using Pinula.API.Interface;
using Pinula.API.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;

namespace Pinula.API.Endpoints
{
    public static class RecipeEndpoint
    {

        public static void MapRecipeEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/recipes").RequireRateLimiting("GeneralApiPolicy");

            //---------------------------------------------------------------Get previews filtered
            group.MapGet("/getPreviews/filtered", async (HttpRequest request, [AsParameters] RecipeFilterParameters filter, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                string baseUrl = $"{request.Scheme}://{request.Host}";
                Guid? currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : null;

                var results = await service.GetFilteredPreviewsAsync(filter, currentUserId, baseUrl, lang);
                return Results.Ok(results);
            });

            //---------------------------------------------------------------Toggle favorite recipe
            group.MapPost("/toggleFavorite/{recipeId:guid}", async (Guid recipeId, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                var result = await service.ToggleFavoriteAsync(recipeId, user.GetUserId());
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Get recipe details
            group.MapGet("/getRecipeDetails/{recipeId:guid}", async (HttpRequest request, Guid recipeId, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                string baseUrl = $"{request.Scheme}://{request.Host}";
                Guid? currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : null;

                var recipe = await service.GetRecipeDetailsAsync(recipeId, currentUserId, baseUrl, lang);
                return recipe != null ? Results.Ok(recipe) : Results.NotFound();
            });
            
            //---------------------------------------------------------------Create recipe
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var form = await request.ReadFormAsync();
                var dtoStr = form["recipeData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing recipe data.");

                var dto = JsonSerializer.Deserialize<RecipeCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid recipe data.");

                var file = form.Files.GetFile("image");
            
                var result = await service.CreateRecipeAsync(user.GetUserId(), dto, file, lang);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();
            
            //---------------------------------------------------------------Update recipe
            group.MapPut("/update/{id:guid}", async (Guid id, HttpRequest request, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var form = await request.ReadFormAsync();
                var dtoStr = form["recipeData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing recipe data.");

                var dto = JsonSerializer.Deserialize<RecipeCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid recipe data.");

                var file = form.Files.GetFile("image");

                var result = await service.UpdateRecipeAsync(id, user.GetUserId(), dto, file, lang);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();
            
            //---------------------------------------------------------------Delete recipe
            group.MapDelete("/delete/{recipeId:guid}", async (Guid recipeId, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                var result = await service.DeleteRecipeAsync(recipeId, user.GetUserId());
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();
            
            //---------------------------------------------------------------Post comment
            group.MapPost("/postComment", async (CommentCreateDto dto, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var result = await service.PostCommentAsync(user.GetUserId(), dto, lang);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Delete comment
            group.MapDelete("/deleteComment/{commentId:guid}", async (Guid commentId, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                var result = await service.DeleteCommentAsync(commentId, user.GetUserId());
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Update comment
            group.MapPut("/updateComment/{commentId:guid}", async (Guid commentId, CommentCreateDto dto, ClaimsPrincipal user, IRecipeBackendService service) =>
            {
                var result = await service.UpdateCommentAsync(commentId, user.GetUserId(), dto);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Toggle recipe approval
            group.MapPost("/admin/toggleApproval/{recipeId:guid}", async (Guid recipeId, IRecipeBackendService service) =>
            {
                var result = await service.AdminToggleApprovalAsync(recipeId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle recipe checked
            group.MapPost("/admin/toggleChecked/{recipeId:guid}", async (Guid recipeId, IRecipeBackendService service) =>
            {
                var result = await service.AdminToggleCheckedAsync(recipeId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get all comments
            group.MapGet("/admin/allComments", async (IRecipeBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var comments = await service.AdminGetAllCommentsAsync(lang);
                return Results.Ok(comments);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle commentDto approval
            group.MapPost("/admin/toggleCommentApproval/{commentId:guid}", async (Guid commentId, IRecipeBackendService service) =>
            {
                var result = await service.AdminToggleCommentApprovalAsync(commentId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");
            
        }

        public static string GetMacrosLabel(decimal? Calories, decimal? Proteins, decimal? Fats, decimal? Carbohydrates)
        {
            if (!Calories.HasValue || !Proteins.HasValue || !Fats.HasValue || !Carbohydrates.HasValue)
            {
                return "Unknown";
            }

            var calories = Calories.Value;
            var proteins = Proteins.Value;
            var fats = Fats.Value;
            var carbs = Carbohydrates.Value;

            if (calories == 0) return "Light";

            if (calories > 800 || (fats > 30 && carbs > 80))
            {
                return "Cheat Meal";
            }
            if (proteins > 25 && fats < 15)
            {
                return "High Protein";
            }

            if (carbs < 20 && fats > 10)
            {
                return "Low Carb";
            }

            if (carbs > 60 && fats < 10)
            {
                return "Energy";
            }

            return "Balanced";
        }
    } 
}
