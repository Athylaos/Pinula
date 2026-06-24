using Microsoft.Extensions.Logging;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;


namespace Pinula.Shared.Services
{
    public class OFFService
    {
        #region DTOs
        public class OffSearchResponse
        {
            [JsonPropertyName("products")]
            public List<OffProductDto> Products { get; set; } = new();
        }

        public class OffProductResponse
        {
            [JsonPropertyName("product")]
            public OffProductDto? Product { get; set; }
        }

        public class OffProductDto
        {
            [JsonPropertyName("code")]
            public string? Code { get; set; }

            [JsonPropertyName("product_name")]
            public string? ProductName { get; set; }

            [JsonPropertyName("product_name_cs")]
            public string? ProductNameCs { get; set; }

            [JsonPropertyName("product_name_en")]
            public string? ProductNameEn { get; set; }

            [JsonPropertyName("image_url")]
            public string? ImageUrl { get; set; }

            [JsonPropertyName("nutriscore_grade")]
            public string? NutriScore { get; set; }

            [JsonPropertyName("nova_group")]
            public int? NovaGroup { get; set; }

            [JsonPropertyName("nutriments")]
            public OffNutrimentsDto? Nutriments { get; set; }

            [JsonPropertyName("categories_tags")]
            public List<string>? CategoriesTags { get; set; } = new();

            [JsonPropertyName("ingredients_analysis_tags")]
            public List<string>? AnalysisTags { get; set; } = new();

            [JsonPropertyName("allergens_tags")]
            public List<string>? AllergensTags { get; set; } = new();
        }

        public class OffNutrimentsDto
        {
            [JsonPropertyName("energy-kcal_100g")]
            public decimal? Calories { get; set; }

            [JsonPropertyName("proteins_100g")]
            public decimal? Proteins { get; set; }

            [JsonPropertyName("fat_100g")]
            public decimal? Fats { get; set; }

            [JsonPropertyName("saturated-fat_100g")]
            public decimal? SaturatedFats { get; set; }

            [JsonPropertyName("carbohydrates_100g")]
            public decimal? Carbohydrates { get; set; }

            [JsonPropertyName("sugars_100g")]
            public decimal? Sugars { get; set; }

            [JsonPropertyName("fiber_100g")]
            public decimal? Fiber { get; set; }

            [JsonPropertyName("salt_100g")]
            public decimal? Salt { get; set; }
        }

        private class OffPreviewDto
        {
            [JsonPropertyName("code")]
            public string? Code { get; set; }

            [JsonPropertyName("product_name")]
            public string? ProductName { get; set; }

            [JsonPropertyName("product_name_en")]
            public string? ProductNameEn { get; set; }

            [JsonPropertyName("product_name_cs")]
            public string? ProductNameCs { get; set; }

            [JsonPropertyName("image_url")]
            public string? ImageUrl { get; set; }

            [JsonPropertyName("quantity")]
            public string? Quantity { get; set; }
        }

        private class OffSearchPreviewResponse
        {
            [JsonPropertyName("products")]
            public List<OffPreviewDto> Products { get; set; } = new();
        }
        #endregion


        private readonly ILogger<OFFService> _logger;
        private readonly HttpClient _httpClient;


        public OFFService(HttpClient httpClient, ILogger<OFFService> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<List<IngredientPreview>> SearchPreviewsAsync(IngredientFilterParameters filter, List<UnitPreviewDto> allLocalUnits, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(filter.SearchTerm) && string.IsNullOrWhiteSpace(filter.Barcode))
                return new List<IngredientPreview>();

            var gramUnit = allLocalUnits.FirstOrDefault(u => u.Code.ToLower() == "g");
            var mlUnit = allLocalUnits.FirstOrDefault(u => u.Code.ToLower() == "ml");

            string baseOffUrl = (languageCode?.ToLower() == "cs")
                ? "https://cz.openfoodfacts.org"
                : "https://world.openfoodfacts.org";

            string url;

            if (!string.IsNullOrWhiteSpace(filter.Barcode))
            {
                url = $"{baseOffUrl}/api/v2/search?code={filter.Barcode}&fields=code,product_name,product_name_cs,product_name_en,image_url,quantity&page_size={filter.Amount}";
            }
            else
            {
                string cleanTerm = filter.SearchTerm.Trim();

                if (!cleanTerm.EndsWith(" ") && cleanTerm.Length > 2)
                {
                    cleanTerm += "*";
                }

                url = $"{baseOffUrl}/cgi/search.pl" +
                      $"?search_terms={Uri.EscapeDataString(cleanTerm)}" +
                      $"&search_simple=1" +
                      $"&action=process" +
                      $"&json=1" +
                      $"&fields=code,product_name,product_name_cs,product_name_en,image_url,quantity" +
                      $"&page_size={filter.Amount}";
            }

            var response = await _httpClient.GetFromJsonAsync<OffSearchPreviewResponse>(url);
            if (response?.Products == null) return new List<IngredientPreview>();

            var previews = response.Products.Select(p =>
            {
                var isLiquid = p.Quantity != null && (p.Quantity.ToLower().Contains("ml") || p.Quantity.ToLower().Contains(" l"));
                var defaultUnit = (isLiquid ? mlUnit : gramUnit) ?? allLocalUnits.First();

                return new IngredientPreview
                {
                    Id = Guid.Empty,
                    Name = (languageCode == "cs")
                        ? (p.ProductNameCs ?? p.ProductNameEn ?? p.ProductName ?? "Neznámý produkt")
                        : (p.ProductNameEn ?? p.ProductNameCs ?? p.ProductName ?? "Unknown product"),
                    ImageUrl = p.ImageUrl,
                    DefaultUnit = defaultUnit,
                    SelectedUnit = defaultUnit,
                    IngredientUnits = new List<UnitPreviewDto> { defaultUnit }
                };
            }).ToList();

            return previews;
        }

        public async Task<IngredientCreateDto?> GetFullIngredientDetailsAsync(string barcode, Guid defaultUnitId)
        {
            string url = $"https://world.openfoodfacts.org/api/v2/product/{barcode}?fields=code,product_name,product_name_cs,product_name_en,image_url,nutriscore_grade,nova_group,categories_tags,ingredients_analysis_tags,allergens_tags,nutriments";

            var response = await _httpClient.GetFromJsonAsync<OffProductResponse>(url);
            if (response?.Product == null) return null;

            var prod = response.Product;

            var names = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(prod.ProductNameCs)) names["cs"] = prod.ProductNameCs;
            if (!string.IsNullOrWhiteSpace(prod.ProductNameEn)) names["en"] = prod.ProductNameEn;
            if (names.Count == 0) names["en"] = prod.ProductName ?? "Unknown";

            bool isVegan = prod.AnalysisTags?.Contains("en:vegan") == true;
            bool isVegetarian = prod.AnalysisTags?.Contains("en:vegetarian") == true;
            bool isGlutenFree = prod.AllergensTags?.Contains("en:gluten") == false;
            bool isLactoseFree = prod.AllergensTags?.Contains("en:milk") == false && prod.AllergensTags?.Contains("en:lactose") == false;

            return new IngredientCreateDto
            {
                Names = names,
                Barcode = prod.Code,
                ImageUrl = prod.ImageUrl,
                DefaultUnitId = defaultUnitId,
                CategoryTags = prod.CategoriesTags?.Select(tag => tag.Replace("en:", "").Trim()).ToList() ?? new List<string>(),

                NutriScore = prod.NutriScore?.ToLower(),
                NovaClassification = prod.NovaGroup,

                Calories = prod.Nutriments?.Calories ?? 0m,
                Proteins = prod.Nutriments?.Proteins ?? 0m,
                Fats = prod.Nutriments?.Fats ?? 0m,
                SaturatedFats = prod.Nutriments?.SaturatedFats ?? 0m,
                Carbohydrates = prod.Nutriments?.Carbohydrates ?? 0m,
                Sugars = prod.Nutriments?.Sugars ?? 0m,
                Fiber = prod.Nutriments?.Fiber ?? 0m,
                Salt = prod.Nutriments?.Salt ?? 0m,

                IsVegan = isVegan,
                IsVegetarian = isVegetarian,
                IsGlutenFree = isGlutenFree,
                IsLactoseFree = isLactoseFree
            };
        }


    }
}
