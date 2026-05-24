using Pinula.API.Context;
using Pinula.API.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<PinulaDbContext>(options =>
    options.UseNpgsql(connectionString).UseUpperSnakeCaseNamingConvention());

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        return Task.CompletedTask;
    });
});

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
    policy.WithOrigins("http://localhost:5119")  // BLAZOR WASM url 
          .AllowAnyHeader()
          .AllowAnyMethod();
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



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.MapUserEndpoints();

app.MapCategoryEndpoints();

app.MapRecipeEndpoints();

app.MapUnitEndpoints();

app.MapIngredientEndpoints();




app.Run();


