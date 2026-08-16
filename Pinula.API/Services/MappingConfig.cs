using Mapster;
using Pinula.API.Models;
using Pinula.Shared.DTOs;

namespace Pinula.API.Services;

public class MappingConfig : IRegister
{
    private static HttpRequest? GetHttpRequest()
    {
        if (MapContext.Current != null &&
            MapContext.Current.Parameters.TryGetValue("httpRequest", out var reqObj) &&
            reqObj is HttpRequest request)
        {
            return request;
        }

        return null;
    }
    
    private static string GetLanguageCode()
    {
        var request = GetHttpRequest();
        if (request != null)
        {
            var acceptLanguage = request.Headers.AcceptLanguage.ToString();
            if (!string.IsNullOrWhiteSpace(acceptLanguage))
            {
                var firstLang = acceptLanguage.Split(',')[0].Split(';')[0].Split('-')[0];
                if (!string.IsNullOrWhiteSpace(firstLang))
                {
                    return firstLang;
                }
            }
        }

        return "en";
    }
    
    public void Register(TypeAdapterConfig config)
    {
        #region Category

        config.NewConfig<Category, CategoryDisplayDto>()
            .Map(d => d.Name,
                src => HelperFunctions.GetLocalizedName(src.Names, GetLanguageCode()))
            .Map(d => d.PictureUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Categories,
                    src.PictureUrl))
            .PreserveReference(true)
            .MaxDepth(20);

        #endregion
        
        #region Comment

        config.NewConfig<Comment, CommentDisplayDto>()
            .Map(d => d.UserName,
                src => src.User.Name)
            .Map(d => d.UserSurname,
                src => src.User.Surname);

        #endregion
        
        #region Ingredient

        config.NewConfig<Ingredient, AdminIngredientDisplayDto>()
            .Map(d => d.UserEmail,
                src => src.Creator.Email)
            .Map(d => d.ImageUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Ingredients,
                    src.ImageUrl))
            .Map(d => d.AdditionalUnits,
                src => src.IngredientUnits);

        config.NewConfig<Ingredient, AdminIngredientPreviewDto>()
            .Map(d => d.ImageUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Ingredients,
                    src.ImageUrl));

        config.NewConfig<Ingredient, IngredientPreviewDto>()
            .Map(d => d.Name,
                src => HelperFunctions.GetLocalizedName(src.Names, GetLanguageCode()))
            .Map(d => d.ImageUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Ingredients,
                    src.ImageUrl))
            .Map(d => d.SelectedUnit,
                src => src.DefaultUnit);

        #endregion
        
        #region Inventory

        config.NewConfig<InventoryItem, InventoryItemDisplayDto>()
            .Map(d => d.ShoppingCategory,
                src => src.Ingredient.ShoppingCategory);

        #endregion

        #region MealPlan

        config.NewConfig<MealPlan, MealPlanPreviewDto>()
            .Map(d => d.RecipeName,
                src => HelperFunctions.GetLocalizedName(src.Recipe.Titles, GetLanguageCode()))
            .Map(d => d.RecipePhotoUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Recipes,src.Recipe.PhotoUrl))
            .Map(d => d.Ingredients,
                src => src.MealPlanIngredients);

        #endregion

        #region Recipe

        config.NewConfig<Recipe, RecipeDetailsDto>()
            .Map(d => d.Title,
                src => HelperFunctions.GetLocalizedName(src.Titles, GetLanguageCode()))
            .Map(d => d.PhotoUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Recipes,
                    src.PhotoUrl))
            .Map(d => d.UserName,
                src => src.User.Name)
            .Map(d => d.UserSurname,
                src => src.User.Surname);
        
        config.NewConfig<RecipeIngredient, RecipeIngredientPreviewDto>()
            .Map(d => d.IngredientName,
                src => HelperFunctions.GetLocalizedName(src.Ingredient.Names, GetLanguageCode()))
            .Map(d => d.UnitName,
                src => HelperFunctions.GetLocalizedName(src.Unit.Names, GetLanguageCode()));

        config.NewConfig<Recipe, RecipePreviewDto>()
            .Map(d => d.Title,
                src => HelperFunctions.GetLocalizedName(src.Titles, GetLanguageCode()))
            .Map(d => d.PhotoUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Recipes, src.PhotoUrl));

        config.NewConfig<RecipeStep, RecipeStepDisplayDto>()
            .Map(d => d.Description,
                src => HelperFunctions.GetLocalizedName(src.Descriptions, GetLanguageCode()));

        #endregion
        
        #region Unit

        config.NewConfig<Unit, UnitPreviewDto>()
            .Map(d => d.Name,
                src => HelperFunctions.GetLocalizedName(src.Names, GetLanguageCode()));

        #endregion

        #region User

        config.NewConfig<User, AdminUserDisplayDto>()
            .Map(d => d.AvatarUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Users, src.AvatarUrl));
        
        config.NewConfig<User, UserDisplayDto>()
            .Map(d => d.AvatarUrl,
                src => HelperFunctions.GetImageUrl(GetHttpRequest(), HelperFunctions.ImageCategory.Users, src.AvatarUrl));

        #endregion
        
    }
}