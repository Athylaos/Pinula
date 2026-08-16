using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Models;
using Pinula.API.Context;

namespace Pinula.API.Services;

public static class HelperFunctions
{
    public static string GetLocalizedName(IReadOnlyDictionary<string, string> dic, string languageCode)
    {
        return dic.GetValueOrDefault(languageCode) ?? dic.GetValueOrDefault("en") ?? "Unknow";
    }
    
    public enum ImageCategory
    {
        Ingredients = 1,
        Categories = 2,
        Recipes = 3,
        Users = 4
    }

    public static string GetImageUrl(HttpRequest request, ImageCategory imageCategory, string? imageUrl)
    {
        string imageDirectory;
        string defaultImage;
        switch (imageCategory)
        {
            case ImageCategory.Ingredients:
                imageDirectory = "ingredients";
                defaultImage = "default_ingredient.png";
                break;
            case ImageCategory.Categories:
                imageDirectory = "categories";
                defaultImage = "default_category.png";
                break;
            case ImageCategory.Recipes:
                imageDirectory = "recipes";
                defaultImage = "default_recipe.png";
                break;
            case ImageCategory.Users:
                imageDirectory = "avatars";
                defaultImage = "default_avatar.png";
                break;
            default:
                imageDirectory = "error";
                defaultImage = "error";
                break;
                
        }
        
        var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/{imageDirectory}/";
        return $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(imageUrl) ? defaultImage : imageUrl)}";
    }
    
    public static async Task<(IResult, User?, Group?)> AuthUserInGroup(ClaimsPrincipal user, PinulaDbContext db)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
        {
            return (Results.Unauthorized(), null, null);
        }
            
        var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (userDb is null)
        {
            return (Results.NotFound("User not found"), null, null);
        }

        var groupDb = await db.Groups.Include(g => g.Users).FirstOrDefaultAsync(g => g.Users.Contains(userDb));
        if (groupDb is null)
        {
            return (Results.NotFound("User is not in group"), userDb, null);
        }

        return (Results.Ok(), userDb, groupDb);
    }
}