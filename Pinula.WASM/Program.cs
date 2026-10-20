using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Pinula.WASM;
using Pinula.WASM.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ILocalStorage, BlazorLocalStorage>();
builder.Services.AddTransient<AuthHttpMessageHandler>();
builder.Services.AddAuthorizationCore();

builder.Services.AddHttpClient("CookApi", client =>
    {
        string configUrl;
        {
            var currentHost = builder.HostEnvironment.BaseAddress;
        
            if (currentHost.Contains("stage.pinula.app"))
            {
                configUrl = "https://stage-api.pinula.app";
            }
            else if (currentHost.Contains("pinula.app") && !currentHost.Contains("stage"))
            {
                configUrl = "https://api.pinula.app";
            }
            else
            {
                configUrl = "http://10.0.20.2:5017/";
            }
        }

        client.BaseAddress = new Uri(configUrl);
    })
    .AddHttpMessageHandler<AuthHttpMessageHandler>();


builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddHttpClient<IIngredientApiClient, IngredientApiClient>("CookApi");
builder.Services.AddHttpClient<IRecipeApiClient, RecipeApiClient>("CookApi");
builder.Services.AddHttpClient<ICategoryApiClient, CategoryApiClient>("CookApi");
builder.Services.AddHttpClient<IUserApiClient, UserApiClient>("CookApi");
builder.Services.AddHttpClient<IUnitApiClient, UnitApiClient>("CookApi");
builder.Services.AddHttpClient<IMealPlanApiClient, MealPlanApiClient>("CookApi");
builder.Services.AddHttpClient<IInventoryApiClient, InventoryApiClient>("CookApi");
builder.Services.AddHttpClient<OFFService>(client =>
{
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add("User-Agent", "PinulaApp/0.4 (davidhykys88@gmail.com)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});


builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

var host = builder.Build();

var localStorage = host.Services.GetRequiredService<ILocalStorage>();

var localCulture = await localStorage.GetStringAsync("culture");

var cultureCode = string.IsNullOrEmpty(localCulture) ? "en" : localCulture;

var culture = new CultureInfo(cultureCode);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
