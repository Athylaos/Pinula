using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Models;
using Pinula.API.Context;
using Pinula.Shared.Constants;

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

    public static string GetImageUrl(string? baseUrl, ImageCategory imageCategory, string? imageUrl)
    {
        string imageDirectory = imageCategory switch
        {
            ImageCategory.Ingredients => "ingredients",
            ImageCategory.Categories  => "categories",
            ImageCategory.Recipes     => "recipes",
            ImageCategory.Users       => "avatars",
            _                         => "error"
        };

        string defaultImage = imageCategory switch
        {
            ImageCategory.Ingredients => ImageConstants.DefaultIngredient,
            ImageCategory.Categories  => ImageConstants.DefaultCategory,
            ImageCategory.Recipes     => ImageConstants.DefaultRecipe,
            ImageCategory.Users       => "default_avatar.png",
            _                         => "error.png"
        };

        var filename = string.IsNullOrWhiteSpace(imageUrl) ? defaultImage : imageUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return $"/images/{imageDirectory}/{filename}";
        }

        var cleanBaseUrl = baseUrl.TrimEnd('/');
        return $"{cleanBaseUrl}/images/{imageDirectory}/{filename}";
    }

    public static string GetLocalizedName(Dictionary<string, string>? names, string lang)
    {
        if (names == null || names.Count == 0) return string.Empty;
        
        return names.GetValueOrDefault(lang) 
               ?? names.GetValueOrDefault("en") 
               ?? names.Values.FirstOrDefault() 
               ?? string.Empty;
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