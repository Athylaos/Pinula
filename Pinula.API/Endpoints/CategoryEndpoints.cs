using DeepL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Pinula.API.Interface;

namespace Pinula.API.Endpoints
{
    public static class CategoryEndpoints
    {
        public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/categories").RequireRateLimiting("GeneralApiPolicy");
            
            group.MapGet("/getAll", async (HttpRequest request, ICategoryBackendService categoryService) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var categoriesTree = await categoryService.GetCategoriesTree(imageBaseUrl, languageCode);
                
                return Results.Ok(categoriesTree);
            });

            //---------------------------------------------------------------Get category
            group.MapGet("/get/{categoryId:guid}", async (HttpRequest request, Guid categoryId, ICategoryBackendService categoryService) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var categoryDto = await categoryService.GetCategoryWithChildren(imageBaseUrl, languageCode, categoryId);

                return categoryDto is null ? Results.NotFound("Category not found") : Results.Ok(categoryDto);
            });

            //---------------------------------------------------------------Create category
            group.MapPost("/create", async (HttpRequest request, ICategoryBackendService categoryService, IWebHostEnvironment env) =>
            {
                var form = await request.ReadFormAsync();
                var dtoStr = form["categoryData"];

                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Category data missing.");

                var dto = JsonSerializer.Deserialize<CategoryCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Unvalid category data.");
                
                var file = form.Files.GetFile("image");

                var response = await categoryService.CreateCategory(dto, file, env);

                return response.StatusCode switch
                {
                    201 => Results.Created($"/categories/get/{response.CreatedCategoryId}", response),
                    400 => Results.BadRequest(response),
                    500 => Results.InternalServerError(response),
                    _   => Results.Json(response, statusCode: response.StatusCode)
                };
                
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Delete category
            group.MapDelete("/delete/{id:guid}", async (Guid id, ICategoryBackendService categoryService, IWebHostEnvironment env) =>
            {
                var response = await categoryService.DeleteCategory(id, env);

                return response.StatusCode switch
                {
                    204 => Results.NoContent(),
                    400 => Results.BadRequest(response),
                    404 => Results.NotFound(response),
                    _   => Results.Json(response, statusCode: response.StatusCode)
                };
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get all categories admin (Flattened with Level)
            group.MapGet("/getAllAdmin", async (HttpRequest request, ICategoryBackendService categoryService) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";

                var categories = await categoryService.GetAllAdminCategoriesFlattened(imageBaseUrl);
                return Results.Ok(categories);
            }).RequireAuthorization("AdminOnly");



        }
    }
}
