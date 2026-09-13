using BCrypt.Net;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Mapster;
using Pinula.API.Services;
using Pinula.Shared.Enums;
using System.Globalization;
using Pinula.API.Interface;

namespace Pinula.API.Endpoints
{
    public static class UserEndpoint
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/users");
            
            //---------------------------------------------------------------Check email availability
            group.MapGet("/checkEmail", async (string email, PinulaDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(email))
                    return Results.BadRequest("Email is required.");

                var cleanEmail = email.Trim().ToLower();

                var exists = await db.Users.AnyAsync(u => u.Email == cleanEmail);

                return exists ? Results.NotFound() : Results.Ok();
            });

            //---------------------------------------------------------------UserRegistration
            group.MapPost("/register", async (UserRegistrationDto registrationDto, PinulaDbContext db) =>
            {
                if (await db.Users.AnyAsync(u => u.Email == registrationDto.Email))
                {
                    return Results.BadRequest("Email already in use");
                }
                var cleanEmail = registrationDto.Email.Trim().ToLower();
                var verificationCode = await db.VerificationCodes.FirstOrDefaultAsync(c => c.Id == registrationDto.VerificationToken && c.IsUsed && c.ExpiresAt > DateTime.UtcNow && c.Email == cleanEmail && c.Type == VerificationCodeType.Registration);
                if (verificationCode is null)
                {
                    return Results.BadRequest("Wrong verification token");
                }
                else
                {
                    db.VerificationCodes.Remove(verificationCode);
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password);

                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = registrationDto.Email,
                    PasswordHash = passwordHash,
                    Name = registrationDto.Name,
                    Surname = registrationDto.Surname,
                    UserCreated = DateTime.UtcNow,
                    Role = "user"
                };

                db.Users.Add(newUser);
                await db.SaveChangesAsync();

                return Results.Ok(new { newUser.Id, newUser.Email });
            });

            //---------------------------------------------------------------UserLogin
            group.MapPost("/login", async (UserLoginDto loginDto, PinulaDbContext db, IConfiguration config) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return Results.Unauthorized();
                }

                var token = GenerateJwtToken(user, config);

                return Results.Ok(new LoginResponse
                {
                    Token = token,
                    UserName = user.Name,
                    UserSurname = user.Surname,
                });
            });


            //---------------------------------------------------------------GetMe
            group.MapGet("/getMe", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var defaultImage = "default_avatar.png";

                var userId = user.GetUserId();

                UserDisplayDto? userData = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new UserDisplayDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? defaultImage : u.AvatarUrl)}",
                    UserCreated = u.UserCreated,
                    CanComment = u.CanComment,
                    CanCreateRecipes = u.CanCreateRecipes,
                    Role = u.Role,
                }).FirstOrDefaultAsync();

                if (userData == null) return Results.NotFound("User not found");

                userData.PostedRecipes = await db.Recipes.AsNoTracking().Where(r => r.UserId == userId).CountAsync();
                userData.PostedComments = await db.Comments.AsNoTracking().Where(c => c.UserId == userId).CountAsync();
                var ratingsQuery = db.Recipes.AsNoTracking().Where(r => r.UserId == userId);

                if (await ratingsQuery.AnyAsync())
                {
                    userData.AvgRating = await ratingsQuery.AverageAsync(r => r.Rating)??0;
                }
                else
                {
                    userData.AvgRating = 0;
                }


                return Results.Ok(userData);
            }).RequireAuthorization();


            //---------------------------------------------------------------GetUserDisplay
            group.MapGet("/getUserDisplay/{userId:guid}", async (HttpRequest request, Guid userId,PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var defaultImage = "default_avatar.png";

                var userData = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new UserDisplayDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Surname = u.Surname,
                    AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? defaultImage : u.AvatarUrl)}",
                    UserCreated = u.UserCreated,
                    CanComment = u.CanComment,
                    CanCreateRecipes = u.CanCreateRecipes,
                    Role = u.Role,
                }).FirstOrDefaultAsync();

                return Results.Ok(userData);
            });


            //---------------------------------------------------------------Update user
            group.MapPut("/update", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                var form = await request.ReadFormAsync();

                var dtoStr = form["userData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing user data.");

                var dto = JsonSerializer.Deserialize<UserUpdateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid user data.");

                var uId = user.GetUserId();
                var dbUser = await db.Users.FindAsync(uId);
                if (dbUser == null) return Results.NotFound("User not found");

                var file = form.Files.GetFile("image");
                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Results.BadRequest("Unsupported image format.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "avatars");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    try
                    {
                        using (var image = await Image.LoadAsync(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Crop,
                                Size = new Size(600, 600)
                            }));

                            await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 });
                        }
                        if (!string.IsNullOrEmpty(dbUser.AvatarUrl) && dbUser.AvatarUrl != "default_avatar.png")
                        {
                            var oldPath = Path.Combine(uploadFolder, dbUser.AvatarUrl);
                            if (File.Exists(oldPath)) File.Delete(oldPath);
                        }

                        dbUser.AvatarUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Image Processing Error: {ex.Message}");
                    }
                }
                if (!string.IsNullOrWhiteSpace(dto.Name)) dbUser.Name = dto.Name.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Surname)) dbUser.Surname = dto.Surname.Trim();

                await db.SaveChangesAsync();
                return Results.Ok();
            }).DisableAntiforgery();

            // --------------------------------------------------------------- Reset / Change password
            group.MapPost("/changePassword", async (PasswordChangeDto dto, PinulaDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    return Results.BadRequest("Email and new password are required.");
                }

                if (dto.NewPassword.Length < 6)
                {
                    return Results.BadRequest("Password is too short.");
                }

                var cleanEmail = dto.Email.Trim().ToLower();
                
                var verificationCode = await db.VerificationCodes.FirstOrDefaultAsync(c =>
                    c.Id == dto.VerificationToken &&
                    c.Email == cleanEmail &&
                    c.Type == VerificationCodeType.PasswordReset &&
                    c.IsUsed &&
                    c.ExpiresAt > DateTime.UtcNow);

                if (verificationCode is null)
                {
                    return Results.BadRequest("Invalid or expired verification token.");
                }
                
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Email == cleanEmail);
                if (userDb is null)
                {
                    return Results.NotFound("User not found.");
                }
                
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                userDb.PasswordHash = passwordHash;
    
                db.VerificationCodes.Remove(verificationCode);
                await db.SaveChangesAsync();

                return Results.Ok();
            }).AllowAnonymous();


            //---------------------------------------------------------------Get all users
            group.MapGet("/admin/all", async (HttpRequest request, PinulaDbContext db) =>
            {
                var users = await db.Users.ToListAsync();

                var usersDb = users.AdaptWithRequest<List<AdminUserDisplayDto>>(request);

                return usersDb;

            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change password
            group.MapPost("/admin/changePassword", async (ClaimsPrincipal user, AdminPasswordChangeDto dto, PinulaDbContext db) =>
            {
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (userDb is null) return Results.NotFound();

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                userDb.PasswordHash = passwordHash;
                db.SaveChanges();

                return Results.Ok();


            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change comment permission
            group.MapPost("/admin/toggleCommentPermission/{userId:guid}", async (Guid userId, PinulaDbContext db) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null) return Results.NotFound("User not found.");

                user.CanComment = !user.CanComment;

                await db.SaveChangesAsync();
                return Results.Ok(new { canComment = user.CanComment });
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change recipe creation permission
            group.MapPost("/admin/toggleRecipePermission/{userId:guid}", async (Guid userId, PinulaDbContext db) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null) return Results.NotFound("User not found.");

                user.CanCreateRecipes = !user.CanCreateRecipes;

                await db.SaveChangesAsync();
                return Results.Ok(new { canCreateRecipes = user.CanCreateRecipes });
            }).RequireAuthorization("AdminOnly");
            
            
        // --------------------------------------------------------------- Send verification code
            group.MapPost("/sendVerificationCode", async (VerificationCodeRequestDto dto, PinulaDbContext db, IEmailService emailService) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return Results.BadRequest("Email is required.");
                
                if(dto.CodeType == VerificationCodeType.Registration && await db.Users.AnyAsync(u => u.Email == dto.Email))
                    return Results.BadRequest("Email already registered.");

                var cleanEmail = dto.Email.Trim().ToLower();

                var latestCode = await db.VerificationCodes
                    .Where(c => c.Email == cleanEmail && c.Type == dto.CodeType)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestCode is not null && latestCode.CreatedAt.AddSeconds(60) > DateTime.UtcNow)
                {
                    return Results.BadRequest("Please wait before requesting a new code.");
                }

                var oldCodes = await db.VerificationCodes
                    .Where(c => c.Email == cleanEmail && c.Type == dto.CodeType)
                    .ToListAsync();
                db.VerificationCodes.RemoveRange(oldCodes);

                var generatedCode = Random.Shared.Next(100000, 999999).ToString();
                var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                await emailService.SendVerificationCodeAsync(cleanEmail, generatedCode, dto.CodeType, culture);

                var newVerificationCode = new VerificationCode
                {
                    Email = cleanEmail,
                    Code = generatedCode,
                    Type = dto.CodeType,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false,
                    AttemptCount = 0
                };

                db.VerificationCodes.Add(newVerificationCode);
                await db.SaveChangesAsync();

                return Results.Ok();
            });

        // --------------------------------------------------------------- Verify code
        group.MapPost("/verifyCode", async (VerificationCodeVerifyDto dto, PinulaDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
                return Results.BadRequest("Email and code are required.");

            var cleanEmail = dto.Email.Trim().ToLower();
            var cleanCode = dto.Code.Trim();
            
            var codeRecord = await db.VerificationCodes
                .Where(c => c.Email == cleanEmail && c.Type == dto.CodeType && !c.IsUsed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (codeRecord is null)
                return Results.Ok(new VerificationCodeResponseDto { Success = false, VerificationToken = null, RateLimited = false, WrongCode = false, CodeExpired = true });
            
            if (codeRecord.ExpiresAt < DateTime.UtcNow)
            {
                codeRecord.IsUsed = true;
                await db.SaveChangesAsync();
                return Results.Ok(new VerificationCodeResponseDto(){Success = false, VerificationToken = null, RateLimited = false, WrongCode = false, CodeExpired = true});
            }
            
            if (codeRecord.AttemptCount >= 5)
            {
                codeRecord.IsUsed = true;
                await db.SaveChangesAsync();
                return Results.Ok(new VerificationCodeResponseDto(){Success = false, VerificationToken = null, RateLimited = true, WrongCode = false, CodeExpired = false});
            }
            
            if (codeRecord.Code != cleanCode)
            {
                codeRecord.AttemptCount++;
                await db.SaveChangesAsync();
                return Results.Ok(new VerificationCodeResponseDto(){Success = false, VerificationToken = null, RateLimited = false, WrongCode = true, CodeExpired = false});
            }
            
            codeRecord.IsUsed = true;
            await db.SaveChangesAsync();
            
            return Results.Ok(new VerificationCodeResponseDto(){Success = true, VerificationToken = codeRecord.Id, RateLimited = false, WrongCode = false, CodeExpired = false});
        });
    }

        

        
        private static string GenerateJwtToken(User user, IConfiguration config)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = config["JWTKey:Default"];

            if (string.IsNullOrEmpty(keyString))
                throw new Exception("JWT Key is missing in appsettings.json");

            var key = Encoding.ASCII.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "user")
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}