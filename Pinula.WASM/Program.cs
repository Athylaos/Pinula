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

builder.Services.AddScoped<IIngredientService>(sp => new IngredientService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<IngredientService>>()));
builder.Services.AddScoped<IRecipeService>(sp => new RecipeService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<RecipeService>>()));
builder.Services.AddScoped<ICategoryService>(sp => new CategoryService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<CategoryService>>()));
builder.Services.AddScoped<IUserService>(sp => new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILocalStorage>(), sp.GetRequiredService<ILogger<UserService>>()));
builder.Services.AddScoped<IUnitService>(sp => new UnitService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<UnitService>>()));
builder.Services.AddScoped<IMealPlanService>(sp => new MealPlanService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<MealPlanService>>()));

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
