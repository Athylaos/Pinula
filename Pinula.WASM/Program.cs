using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Pinula.WASM;
using Pinula.WASM.Services;
using System.Globalization;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ILocalStorage, BlazorLocalStorage>();
builder.Services.AddTransient<AuthHttpMessageHandler>();
builder.Services.AddAuthorizationCore();

#if DEBUG
builder.Services.AddHttpClient("CookApi", client =>
    client.BaseAddress = new Uri("http://10.0.1.160:5017/"))
    .AddHttpMessageHandler<AuthHttpMessageHandler>();
#else
builder.Services.AddHttpClient("CookApi", client =>
    client.BaseAddress = new Uri("https://api-pinula.hykys.eu/"))
    .AddHttpMessageHandler<AuthHttpMessageHandler>();
#endif


builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddHttpClient<IIngredientService, IngredientService>("CookApi");
builder.Services.AddHttpClient<IRecipeService, RecipeService>("CookApi");
builder.Services.AddHttpClient<ICategoryService, CategoryService>("CookApi");
builder.Services.AddHttpClient<IUserService, UserService>("CookApi");
builder.Services.AddHttpClient<IUnitService, UnitService>("CookApi");
builder.Services.AddHttpClient<IMealPlanService, MealPlanService>("CookApi");
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
