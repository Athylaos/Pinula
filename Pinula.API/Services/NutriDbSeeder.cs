using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Models;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;

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
        
        public static async Task ShowDefaultUnitsAsync(PinulaDbContext db)
        {
            var ingredients = await db.Ingredients.AsNoTracking().ToListAsync();
            foreach (var ingredient in ingredients)
            {
                if (ingredient.DefaultUnit != null)
                {
                    Console.WriteLine($"Ingredient: {ingredient.Names["en"]} === Default Unit: {ingredient.DefaultUnit.Code}");
                }
                else
                {
                    Console.WriteLine($"Ingredient: {ingredient.Names["en"]} === Default Unit: doesnt have a default unit");
                }
            }

        }
        
        public static async Task MakeDefaultUnitsAsync(PinulaDbContext db)
        {
            var gUnit =  db.Units.FirstOrDefault(u => u.Code == "g");
            
            var ingredients = await db.Ingredients.Include(i => i.IngredientUnits).ThenInclude(iu => iu.Unit).ToListAsync();
            foreach (var ingredient in ingredients)
            {
                Console.WriteLine("================================================================================================}");
                if (ingredient.IngredientUnits.Count == 0)
                {
                    db.IngredientUnits.Add(new IngredientUnit
                    {
                        AmountInGrams = 1,
                        IngredientId = ingredient.Id,
                        UnitId = gUnit.Id,
                    });
                    Console.WriteLine($"**************Ingredient: {ingredient.Names["en"]} === Added new ingredient unit: {gUnit.Code}");
                }
                else
                {
                    Console.WriteLine($"Ingredient: {ingredient.Names["en"]} === Ingredient units:");
                    foreach (var unit in ingredient.IngredientUnits)
                    {
                        Console.WriteLine($"Name: {unit.Unit.Names["en"]}");
                    }
                }
                
                
                if (ingredient.DefaultUnit is null)
                {
                    ingredient.DefaultUnitId = gUnit.Id;
                    Console.WriteLine($"#####################Ingredient: {ingredient.Names["en"]} === Added new default unit: {gUnit.Code}");
                }
                else
                {
                    Console.WriteLine($"Ingredient: {ingredient.Names["en"]} === Default Unit: {ingredient.DefaultUnit.Code}");
                }
            }
            db.SaveChanges();

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

        public static async Task MatchCategoryTags(PinulaDbContext db)
        {
            var ingredientsToMap = await db.Ingredients
                .Where(i => i.OffCategoryTag == null && i.BaseIngredientId == null)
                .ToListAsync();

            Console.WriteLine($"Linking started. {ingredientsToMap.Count} ingredients found.");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Pinula - Version 3.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            int processedCount = 0;

            foreach (var ingredient in ingredientsToMap)
            {
                string queryName = string.Empty;

                if (ingredient.Names.ContainsKey("en") && !string.IsNullOrWhiteSpace(ingredient.Names["en"]))
                    queryName = ingredient.Names["en"];
                else if (ingredient.Names.ContainsKey("cs") && !string.IsNullOrWhiteSpace(ingredient.Names["cs"]))
                    queryName = ingredient.Names["cs"];

                if (string.IsNullOrEmpty(queryName)) continue;

                string cleanQuery = queryName;
                if (cleanQuery.Contains(",")) cleanQuery = cleanQuery.Split(',')[0];
                if (cleanQuery.Contains("(")) cleanQuery = cleanQuery.Split('(')[0];
                cleanQuery = cleanQuery.Trim();

                
                string url = $"https://world.openfoodfacts.org/cgi/search.pl" +
                             $"?search_terms={Uri.EscapeDataString(cleanQuery)}" +
                             $"&search_simple=1" +
                             $"&action=process" +
                             $"&json=1" +
                             $"&sort_by=unique_scans_n" +
                             $"&fields=categories_hierarchy,product_name,brands" +
                             $"&page_size=5";

                int retryCount = 0;
                bool requestSuccess = false;
                HttpResponseMessage? response = null;

                while (retryCount < 3 && !requestSuccess)
                {
                    try
                    {
                        response = await client.GetAsync(url);

                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429)
                        {
                            retryCount++;
                            int backoffDelay = retryCount * 1500;
                            Console.WriteLine($"[503/429] OFF overloaded. Waiting {backoffDelay / 1000}s...");
                            await Task.Delay(backoffDelay);
                            continue;
                        }

                        if (!response.IsSuccessStatusCode) break;
                        requestSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        Console.WriteLine($"[NET] Error: {ex.Message}. Try {retryCount}/3...");
                        await Task.Delay(2000);
                    }
                }

                if (!requestSuccess || response == null || !response.IsSuccessStatusCode)
                {
                    await Task.Delay(600);
                    continue;
                }

                try
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    if (doc.RootElement.TryGetProperty("products", out var productsProp) && productsProp.ValueKind == JsonValueKind.Array && productsProp.GetArrayLength() > 0)
                    {
                        string? bestOffTag = null;
                        int maxProductsToCheck = Math.Min(productsProp.GetArrayLength(), 5);

                        for (int p = 0; p < maxProductsToCheck; p++)
                        {
                            var product = productsProp[p];

                            string productName = product.TryGetProperty("product_name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                            string brands = product.TryGetProperty("brands", out var brandsProp) ? brandsProp.GetString() ?? "" : "";

                            string lowerQuery = cleanQuery.ToLower();
                            if (!productName.ToLower().Contains(lowerQuery) && !brands.ToLower().Contains(lowerQuery))
                            {
                                continue;
                            }

                            if (product.TryGetProperty("categories_hierarchy", out var hierarchyProp) && hierarchyProp.ValueKind == JsonValueKind.Array)
                            {
                                for (int i = hierarchyProp.GetArrayLength() - 1; i >= 0; i--)
                                {
                                    string? tag = hierarchyProp[i].GetString();
                                    if (tag != null && tag.StartsWith("en:"))
                                    {
                                        string lowerTag = tag.ToLower();
                                        if (lowerTag.Contains("groceries") || lowerTag.Contains("meals") || lowerTag.Contains("dishes"))
                                        {
                                            continue;
                                        }

                                        bestOffTag = tag;
                                        Console.WriteLine($"   -> Linked trough product: '{productName}' ({brands})");
                                        break;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(bestOffTag)) break;
                        }

                        if (!string.IsNullOrEmpty(bestOffTag))
                        {
                            ingredient.OffCategoryTag = bestOffTag;
                            Console.WriteLine($"Linked: {queryName} -> {bestOffTag}");
                        }
                        else
                        {
                            Console.WriteLine($"No valid product for: {queryName}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No searched products for: {queryName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JSON Error] {queryName}: {ex.Message}");
                }

                processedCount++;

                if (processedCount % 25 == 0)
                {
                    await db.SaveChangesAsync();
                    Console.WriteLine($"---> Saved 25 ingredients");
                }
                Console.WriteLine($"-------------------------------------------------------------------------------------------");
                await Task.Delay(700);
            }

            await db.SaveChangesAsync();
            Console.WriteLine("=== Finish ===");
        }


    }
}
