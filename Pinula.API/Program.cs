using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Pinula.API.Context;
using Pinula.API.Endpoints;
using Pinula.API.Services;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();


builder.Services.AddDbContext<PinulaDbContext>(options =>
    options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention());

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        return Task.CompletedTask;
    });
});

var allowedOrigin = builder.Configuration["AllowedCORS"] ?? "https://pinula.hykys.eu";

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
    policy.WithOrigins(allowedOrigin)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    });
});

var jwtKey = builder.Configuration["JWTKey:Default"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing in configuration!");
}

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

builder.Services.AddLocalization();
builder.Services.AddSingleton<ITranslationService, TranslationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var supportedCultures = new[] { "cs", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();


app.MapUserEndpoints();
app.MapCategoryEndpoints();
app.MapRecipeEndpoints();
app.MapUnitEndpoints();
app.MapIngredientEndpoints();
app.MapMealPlanEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PinulaDbContext>();

        await NutriDbSeeder.SeedNutriDatabaseAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}


app.Run();


