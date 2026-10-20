using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Pinula.API.Context;
using Pinula.API.Endpoints;
using Pinula.API.Interface;
using Pinula.API.Services;
using Mapster;
using System.Reflection;
using Pinula.Shared.Interface;
using Pinula.Shared.Services;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();


builder.Services.AddDbContext<PinulaDbContext>(options =>
    options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention());

TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        return Task.CompletedTask;
    });
});

var allowedOrigins = new[] 
    { 
        "https://stage.pinula.app", 
        "https://pinula.app", 
        "http://localhost:5119" 
    };

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
    policy.WithOrigins(allowedOrigins)
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("AuthStrictPolicy", httpContext =>
    {
        var clientIp = httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                       ?? httpContext.Connection.RemoteIpAddress?.ToString()
                       ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    
    options.AddPolicy("GeneralApiPolicy", httpContext =>
    {
        var clientIp = httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                       ?? httpContext.Connection.RemoteIpAddress?.ToString()
                       ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 2
        });
    });
});

builder.Services.AddLocalization();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddHttpClient<IEmailService, EmailService>();

builder.Services.AddScoped<ICategoryBackendService, CategoryBackendService>();
builder.Services.AddScoped<IIngredientBackendService, IngredientBackendService>();
builder.Services.AddScoped<IInventoryBackendService, InventoryBackendService>();
builder.Services.AddScoped<IMealPlanBackendService, MealPlanBackendService>();
builder.Services.AddScoped<IRecipeBackendService, RecipeBackendService>();
builder.Services.AddScoped<IUnitBackendService, UnitBackendService>();
builder.Services.AddScoped<IUserBackendService, UserBackendService>();

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
app.UseRouting();
app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


app.MapUserEndpoints();
app.MapCategoryEndpoints();
app.MapRecipeEndpoints();
app.MapUnitEndpoints();
app.MapIngredientEndpoints();
app.MapMealPlanEndpoints();
app.MapInventoryEndpoints();
app.MapScriptEndpoints();


/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PinulaDbContext>();

        await NutriDbSeeder.MakeDefaultUnitsAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
*/


/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var emailService = services.GetRequiredService<IEmailService>();
        
        var testEmail = "davidhykys88@gmail.com"; 
        
        await emailService.SendVerificationCodeAsync(
            testEmail, 
            "123456", 
            Pinula.Shared.Enums.VerificationCodeType.Registration,
            "en"
        );

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Test email successfully sent to email: {Email}", testEmail);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error while sending test email");
    }
}
*/
app.Run();


