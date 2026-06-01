using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Pinula.Shared.Services;
using Pinula.View;
using Pinula.View.Popups;
using Pinula.ViewModel;
using Pinula.ViewModel.Popups;
using Pinula.Shared.Interface;
using Microsoft.Extensions.Logging;
using Sharpnado.MaterialFrame;
using Sharpnado.Shades;
using UraniumUI;
using System.Diagnostics;
using Pinula.Maui.Service;


namespace Pinula
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitCore()
                .UseSharpnadoShadows()
                .UseSharpnadoMaterialFrame(loggerEnable: false)
                .UseUraniumUIBlurs()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Alegreya-VariableFont_wght.ttf", "Alegreya");
                    fonts.AddFont("Nunito-VariableFont_wght.ttf", "Nunito");
                });

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Placeholder", (h, v) =>
            {
#if ANDROID
                h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("Placeholder", (h, v) =>
            {
#if ANDROID
                h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<ITokenStorage, MauiTokenStorage>();
            builder.Services.AddTransient<AuthHttpMessageHandler>();

            builder.Services.AddHttpClient("CookApi", client =>
            {
                client.BaseAddress = new Uri("http://10.0.1.160:5017/api/");
            })
            .AddHttpMessageHandler<AuthHttpMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
#if ANDROID
                var handler = new Xamarin.Android.Net.AndroidMessageHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                return handler;
#else
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return handler;
#endif
            });



            builder.Services.AddSingleton<IIngredientService>(sp => new IngredientService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<IngredientService>>()));
            builder.Services.AddSingleton<IRecipeService>(sp => new RecipeService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<RecipeService>>()));
            builder.Services.AddSingleton<ICategoryService>(sp => new CategoryService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger<CategoryService>>()));
            builder.Services.AddSingleton<IUserService>(sp => new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ITokenStorage>(), sp.GetRequiredService<ILogger<UserService>>()));
            builder.Services.AddSingleton<IUnitService>(sp => new UnitService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CookApi"), sp.GetRequiredService<ILogger>()));

            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();

            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddTransient<RegisterViewModel>();

            builder.Services.AddSingleton<RecipesMainPage>();
            builder.Services.AddTransient<RecipesMainViewModel>();

            builder.Services.AddSingleton<TestPage>();
            builder.Services.AddTransient<TestViewModel>();

            builder.Services.AddSingleton<AddRecipePage>();
            builder.Services.AddTransient<AddRecipeViewModel>();

            builder.Services.AddSingleton<AddIngredientPopup>();
            builder.Services.AddTransient<AddIngredientPopupViewModel>();

            builder.Services.AddSingleton<RecipesCategoryPage>();
            builder.Services.AddTransient<RecipesCategoryViewModel>();

            builder.Services.AddSingleton<RecipeDetailsPage>();
            builder.Services.AddTransient<RecipeDetailsViewModel>();

            builder.Services.AddSingleton<DashboardPage>();
            builder.Services.AddTransient<DashboardViewModel>();

            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddTransient<ProfileViewModel>();

                        
            try
            {
                var app = builder.Build();
                return app;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }
    }
}
