using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Pinula.WASM;
using Pinula.WASM.Services;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ITokenStorage, BlazorTokenStorage>();
builder.Services.AddTransient<AuthHttpMessageHandler>();

builder.Services.AddHttpClient("CookApi", client =>
    client.BaseAddress = new Uri("http://10.0.1.160:5017/api/"))
    .AddHttpMessageHandler<AuthHttpMessageHandler>();

builder.Services.AddScoped<IIngredientService>(sp => new IngredientService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<IngredientService>>()));
builder.Services.AddScoped<IRecipeService>(sp => new RecipeService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<RecipeService>>()));
builder.Services.AddScoped<ICategoryService>(sp => new CategoryService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<CategoryService>>()));
builder.Services.AddScoped<IUserService>(sp => new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ITokenStorage>(), sp.GetRequiredService<ILogger<UserService>>()));
builder.Services.AddScoped<IUnitService>(sp => new UnitService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<UnitService>>()));



await builder.Build().RunAsync();
