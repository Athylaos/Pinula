using DeepL;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Pinula.API.Interface;

namespace Pinula.API.Endpoints
{
    public static class IngredientEndpoint
    {
        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        
        
        public static void MapIngredientEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/ingredients").RequireRateLimiting("GeneralApiPolicy");

            //---------------------------------------------------------------Get filtered previews
            group.MapGet("/getFilteredPreviews", async ([AsParameters] IngredientFilterParameters filter, IIngredientBackendService ingredientService, HttpRequest request) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                filter.Amount = (filter.Amount > 0) ? filter.Amount : 20;
                filter.Amount = (filter.Amount < 25) ? filter.Amount : 25;
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/ingredients/";

                var result = await ingredientService.GetFilteredPreviews(filter, imageBaseUrl, languageCode);
                    
                return Results.Ok(result);
            });
            
            //---------------------------------------------------------------Create ingredient
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, IIngredientBackendService ingredientService, IWebHostEnvironment env) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var userId = user.GetUserId();

                var form = await request.ReadFormAsync();
                var dtoStr = form["ingredientData"];
    
                if (string.IsNullOrEmpty(dtoStr)) 
                    return Results.BadRequest(new CreateIngredientResponse { StatusCode = 400, Message = "Missing ingredient data." });

                var dto = JsonSerializer.Deserialize<IngredientCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) 
                    return Results.BadRequest(new CreateIngredientResponse { StatusCode = 400, Message = "Invalid ingredient data payload." });

                var file = form.Files.GetFile("image");

                var result = await ingredientService.Create(dto, file, env, languageCode, userId);

                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization();

            //---------------------------------------------------------------Get filtered previews
            group.MapGet("/getAdminPreviews", async (int amount, int skip, HttpRequest request, IIngredientBackendService ingredientService) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/ingredients/";

                var list = await ingredientService.AdminGetPreviews(amount, skip, imageBaseUrl);
                
                return Results.Ok(list);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get ingredient detail admin
            group.MapGet("/getAdmin/{ingredientId:guid}", async (Guid ingredientId, HttpRequest request, IIngredientBackendService ingredientService) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/ingredients/";
                
                var dto = await ingredientService.AdminGetDetails(ingredientId, imageBaseUrl, languageCode);
                if (dto is null) return Results.NotFound("Ingredient not found");
                
                return Results.Ok(dto);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Delete ingredient admin
            group.MapDelete("/deleteAdmin/{ingredientId:guid}", async (Guid ingredientId, IIngredientBackendService ingredientService) =>
            {
                var result = await ingredientService.AdminDelete(ingredientId);
                
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Update ingredient admin
            group.MapPut("/updateAdmin", async (HttpRequest request, ClaimsPrincipal user, IIngredientBackendService ingredientService, IWebHostEnvironment env) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var userId = user.GetUserId();

                var form = await request.ReadFormAsync();
                var dtoStr = form["ingredientData"];

                if (string.IsNullOrEmpty(dtoStr)) 
                {
                    return Results.BadRequest(new GeneralResponse 
                    { 
                        Successful = false, 
                        StatusCode = 400, 
                        Message = "Missing ingredient data." 
                    });
                }

                var dto = JsonSerializer.Deserialize<IngredientCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
                if (dto == null) 
                {
                    return Results.BadRequest(new GeneralResponse 
                    { 
                        Successful = false, 
                        StatusCode = 400, 
                        Message = "Invalid ingredient data payload." 
                    });
                }

                var file = form.Files.GetFile("image");

                var result = await ingredientService.AdminUpdate(dto, file, env, languageCode, userId);

                return Results.Json(result, statusCode: result.StatusCode);

            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle ingredient approval
            group.MapPost("/admin/toggleApproval/{ingredientId:guid}", async (Guid ingredientId, IIngredientBackendService ingredientService) =>
            {
                var result = await ingredientService.AdminToggleApproval(ingredientId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle ingredient checked
            group.MapPost("/admin/toggleChecked/{ingredientId:guid}", async (Guid ingredientId, IIngredientBackendService ingredientService) =>
            {
                var result = await ingredientService.AdminToggleChecked(ingredientId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");
            
            //---------------------------------------------------------------Get shopping categories
            group.MapGet("/shoppingCategories/getAll", async (HttpRequest request, IIngredientBackendService ingredientService) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                string baseUrl = $"{request.Scheme}://{request.Host}";

                var categories = await ingredientService.GetShoppingCategories(baseUrl, languageCode);

                return Results.Ok(categories);
            });

        }

    }
}
