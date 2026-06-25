using Pinula.API.Context;
using Pinula.Shared.Models;
using System.Globalization;
using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Pinula.API.Services
{
    public class NutriDbSeeder
    {
        public class NutriDbSeedDto
        {
            public string cs { get; set; } = string.Empty;
            public string en { get; set; } = string.Empty;
            public JsonElement Edible { get; set; }
            public JsonElement Calories { get; set; }
            public JsonElement Fats { get; set; }
            public JsonElement SaturatedFats { get; set; }
            public JsonElement Carbohydrates { get; set; }
            public JsonElement Sugars { get; set; }
            public JsonElement Fiber { get; set; }
            public JsonElement Proteins { get; set; }
            public JsonElement Salt { get; set; }



            public Guid? ShoppingCategoryId { get; set; }
            public string? OffCategoryTag {  get; set; }
        }

        public static async Task SeedNutriDatabaseAsync(PinulaDbContext db)
        {
            var gramUnit = await db.Units.FirstOrDefaultAsync(u => u.Code.ToLower() == "g");
            if (gramUnit == null) throw new Exception("No unit with code 'g'");

            var categories = await db.ShoppingCategories.ToDictionaryAsync(c => c.Code.ToLower(), c => c.Id);
            var defaultCategoryId = categories.ContainsKey("other") ? categories["other"] : categories.Values.First();

            var filePath = Path.Combine(AppContext.BaseDirectory, "data", "seed", "nutridatabaze_seed.json");
            if (!File.Exists(filePath)) return;

            var jsonString = await File.ReadAllTextAsync(filePath);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seedData = JsonSerializer.Deserialize<List<NutriDbSeedDto>>(jsonString, options);

            if (seedData == null) return;

            decimal ParseDecimal(JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetDecimal();
                }

                if (element.ValueKind == JsonValueKind.String)
                {
                    var value = element.GetString();
                    if (string.IsNullOrWhiteSpace(value)) return 0m;

                    var cleaned = value.Trim();
                    if (cleaned == "-" || cleaned.ToLower() == "null" || cleaned.ToLower() == "nd")
                        return 0m;

                    if (decimal.TryParse(cleaned, CultureInfo.InvariantCulture, out decimal result))
                    {
                        return result;
                    }
                }

                return 0m;
            }

            foreach (var item in seedData)
            {
                var czechName = item.cs?.Trim();
                var englishName = item.en?.Trim();

                if (string.IsNullOrEmpty(czechName)) continue;

                var ingredientNames = new Dictionary<string, string> { { "cs", czechName } };
                if (!string.IsNullOrEmpty(englishName))
                {
                    ingredientNames["en"] = englishName;
                }

                var ingredient = new Ingredient
                {
                    Id = Guid.NewGuid(),
                    Names = ingredientNames,
                    DefaultUnitId = gramUnit.Id,
                    ShoppingCategoryId = defaultCategoryId,
                    BaseIngredient = null,

                    BaseIngredientId = null,
                    OffCategoryTag = null,

                    Calories = ParseDecimal(item.Calories),
                    Proteins = ParseDecimal(item.Proteins),
                    Fats = ParseDecimal(item.Fats),
                    SaturatedFats = ParseDecimal(item.SaturatedFats),
                    Carbohydrates = ParseDecimal(item.Carbohydrates),
                    Sugars = ParseDecimal(item.Sugars),
                    Fiber = ParseDecimal(item.Fiber),
                    Salt = ParseDecimal(item.Salt)
                };

                ingredient.EdibleRatio = ParseDecimal(item.Edible);

                db.Ingredients.Add(ingredient);
            }

            await db.SaveChangesAsync();
        }


    }
}
