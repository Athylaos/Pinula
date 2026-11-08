using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pinula.API.Context;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.API.Services;
using Pinula.Shared.DTOs;
using Pinula.Shared.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Pinula.Shared.Constants;

namespace Pinula.API.Services
{
    public class UserBackendService : IUserBackendService
    {
        private readonly PinulaDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<UserBackendService> _logger;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public UserBackendService(
            PinulaDbContext db, 
            IConfiguration config, 
            ILogger<UserBackendService> logger, 
            IEmailService emailService, 
            IWebHostEnvironment env)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _emailService = emailService;
            _env = env;
        }

        public async Task<bool> VerifyTokenAsync(Guid userId)
        {
            return await _db.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> CheckEmailAvailableAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var cleanEmail = email.Trim().ToLower();
            return !await _db.Users.AnyAsync(u => u.Email == cleanEmail);
        }

        public async Task<GeneralResponse> RegisterAsync(UserRegistrationDto dto)
        {
            try
            {
                var cleanEmail = dto.Email.Trim().ToLower();
                if (await _db.Users.AnyAsync(u => u.Email == cleanEmail))
                {
                    return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Email already in use" };
                }

                var verificationCode = await _db.VerificationCodes.FirstOrDefaultAsync(c => 
                    c.Id == dto.VerificationToken && c.IsUsed && c.ExpiresAt > DateTime.UtcNow && c.Email == cleanEmail && c.Type == VerificationCodeType.Registration);
                
                if (verificationCode is null) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Wrong verification token" };

                _db.VerificationCodes.Remove(verificationCode);

                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = cleanEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Name = dto.Name,
                    Surname = dto.Surname,
                    UserCreated = DateTime.UtcNow,
                    Role = "user"
                };

                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();

                return new GeneralResponse { Successful = true, StatusCode = 201, Id = newUser.Id, Message = "User registered successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Email}", dto.Email);
                return new GeneralResponse { Successful = false, StatusCode = 500, Message = "Registration failed." };
            }
        }

        public async Task<LoginResponse> LoginAsync(UserLoginDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLower());
                
                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return new LoginResponse { Successful = false, StatusCode = 401, Message = "Invalid credentials." };
                }

                var token = GenerateJwtToken(user);
                
                return new LoginResponse
                {
                    Successful = true,
                    StatusCode = 200,
                    Token = token,
                    UserName = user.Name,
                    UserSurname = user.Surname
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user {Email}", dto.Email);
                return new LoginResponse { Successful = false, StatusCode = 500, Message = "Login failed." };
            }
        }

        public async Task<UserDisplayDto?> GetMeAsync(Guid userId, string baseUrl)
        {
            var imageBaseUrl = $"{baseUrl}/images/avatars/";
            
            var userData = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new UserDisplayDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                Email = u.Email,
                AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? ImageConstants.DefaultAvatar : u.AvatarUrl)}",
                UserCreated = u.UserCreated,
                CanComment = u.CanComment,
                CanCreateRecipes = u.CanCreateRecipes,
                Role = u.Role,
            }).FirstOrDefaultAsync();

            if (userData == null) return null;

            userData.PostedRecipes = await _db.Recipes.AsNoTracking().CountAsync(r => r.UserId == userId);
            userData.PostedComments = await _db.Comments.AsNoTracking().CountAsync(c => c.UserId == userId);
            
            var ratingsQuery = _db.Recipes.AsNoTracking().Where(r => r.UserId == userId);
            userData.AvgRating = await ratingsQuery.AnyAsync() ? await ratingsQuery.AverageAsync(r => r.Rating) ?? 0 : 0;

            return userData;
        }

        public async Task<UserDisplayDto?> GetUserDisplayAsync(Guid userId, string baseUrl)
        {
            var imageBaseUrl = $"{baseUrl}/images/avatars/";
            
            return await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new UserDisplayDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? ImageConstants.DefaultAvatar : u.AvatarUrl)}",
                UserCreated = u.UserCreated,
                CanComment = u.CanComment,
                CanCreateRecipes = u.CanCreateRecipes,
                Role = u.Role,
            }).FirstOrDefaultAsync();
        }

        public async Task<GeneralResponse> UpdateUserAsync(Guid userId, UserUpdateDto dto, IFormFile? imageFile)
        {
            try
            {
                var dbUser = await _db.Users.FindAsync(userId);
                if (dbUser == null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "User not found" };

                if (imageFile is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension)) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Unsupported image format." };

                    var uploadFolder = Path.Combine(_env.WebRootPath, "images", "avatars");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{dbUser.Id}_{Guid.NewGuid()}.webp";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Crop, Size = new Size(600, 600) }));
                        await image.SaveAsWebpAsync(filePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder() { Quality = 80 });
                    }
                    
                    if (!string.IsNullOrEmpty(dbUser.AvatarUrl) && dbUser.AvatarUrl != ImageConstants.DefaultAvatar)
                    {
                        var oldPath = Path.Combine(uploadFolder, dbUser.AvatarUrl);
                        if (File.Exists(oldPath)) File.Delete(oldPath);
                    }

                    dbUser.AvatarUrl = fileName;
                }

                if (!string.IsNullOrWhiteSpace(dto.Name)) dbUser.Name = dto.Name.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Surname)) dbUser.Surname = dto.Surname.Trim();

                await _db.SaveChangesAsync();
                return new GeneralResponse { Successful = true, StatusCode = 200, Message = "User updated." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return new GeneralResponse { Successful = false, StatusCode = 500, Message = "Update failed." };
            }
        }

        public async Task<GeneralResponse> ChangePasswordAsync(PasswordChangeDto dto)
        {
            try
            {
                var cleanEmail = dto.Email.Trim().ToLower();
                var code = await _db.VerificationCodes.FirstOrDefaultAsync(c =>
                    c.Id == dto.VerificationToken && c.Email == cleanEmail && c.Type == VerificationCodeType.PasswordReset && c.IsUsed && c.ExpiresAt > DateTime.UtcNow);

                if (code is null) return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Invalid or expired token." };
                
                var userDb = await _db.Users.FirstOrDefaultAsync(u => u.Email == cleanEmail);
                if (userDb is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "User not found." };
                
                userDb.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                _db.VerificationCodes.Remove(code);
                await _db.SaveChangesAsync();

                return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Password changed." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for {Email}", dto.Email);
                return new GeneralResponse { Successful = false, StatusCode = 500, Message = "Password change failed." };
            }
        }

        public async Task<List<AdminUserDisplayDto>> AdminGetAllUsersAsync()
        {
            return await _db.Users.AsNoTracking().Select(u => new AdminUserDisplayDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Surname = u.Surname,
                Role = u.Role,
                CanComment = u.CanComment,
                CanCreateRecipes = u.CanCreateRecipes,
                UserCreated = u.UserCreated
            }).ToListAsync();
        }

        public async Task<GeneralResponse> AdminChangePasswordAsync(AdminPasswordChangeDto dto)
        {
            try
            {
                var userDb = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (userDb is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Not found" };

                userDb.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _db.SaveChangesAsync();

                return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Password changed." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin changing password for {UserId}", dto.UserId);
                return new GeneralResponse { Successful = false, StatusCode = 500, Message = "Error changing password" };
            }
        }

        public async Task<ToggleBoolResponse> AdminToggleCommentPermissionAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return new ToggleBoolResponse { Successful = false, StatusCode = 404, Message = "Not found" };

            user.CanComment = !user.CanComment;
            await _db.SaveChangesAsync();
            return new ToggleBoolResponse { Successful = true, StatusCode = 200, State = user.CanComment };
        }

        public async Task<ToggleBoolResponse> AdminToggleRecipePermissionAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return new ToggleBoolResponse { Successful = false, StatusCode = 404, Message = "Not found" };

            user.CanCreateRecipes = !user.CanCreateRecipes;
            await _db.SaveChangesAsync();
            return new ToggleBoolResponse { Successful = true, StatusCode = 200, State = user.CanCreateRecipes };
        }

        public async Task<GeneralResponse> SendVerificationCodeAsync(VerificationCodeRequestDto dto, string lang)
        {
            try
            {
                var cleanEmail = dto.Email.Trim().ToLower();
                var latestCode = await _db.VerificationCodes.Where(c => c.Email == cleanEmail && c.Type == dto.CodeType)
                    .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync();

                if (latestCode is not null && latestCode.CreatedAt.AddSeconds(60) > DateTime.UtcNow)
                {
                    return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Please wait before requesting a new code." };
                }

                var oldCodes = await _db.VerificationCodes.Where(c => c.Email == cleanEmail && c.Type == dto.CodeType).ToListAsync();
                _db.VerificationCodes.RemoveRange(oldCodes);

                var generatedCode = Random.Shared.Next(100000, 999999).ToString();
                await _emailService.SendVerificationCodeAsync(cleanEmail, generatedCode, dto.CodeType, lang);

                _db.VerificationCodes.Add(new VerificationCode
                {
                    Email = cleanEmail,
                    Code = generatedCode,
                    Type = dto.CodeType,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false,
                    AttemptCount = 0
                });
                await _db.SaveChangesAsync();

                return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Code sent." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code to {Email}", dto.Email);
                return new GeneralResponse { Successful = false, StatusCode = 500, Message = "Failed to send code." };
            }
        }

        public async Task<VerificationCodeResponseDto> VerifyCodeAsync(VerificationCodeVerifyDto dto)
        {
            try
            {
                var cleanEmail = dto.Email.Trim().ToLower();
                var codeRecord = await _db.VerificationCodes
                    .Where(c => c.Email == cleanEmail && c.Type == dto.CodeType && !c.IsUsed)
                    .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync();

                if (codeRecord is null) return new VerificationCodeResponseDto { Successful = false, StatusCode = 400, CodeExpired = true };
                
                if (codeRecord.ExpiresAt < DateTime.UtcNow || codeRecord.AttemptCount >= 5)
                {
                    codeRecord.IsUsed = true;
                    await _db.SaveChangesAsync();
                    return new VerificationCodeResponseDto { Successful = false, StatusCode = 400, CodeExpired = codeRecord.ExpiresAt < DateTime.UtcNow, RateLimited = codeRecord.AttemptCount >= 5 };
                }
                
                if (codeRecord.Code != dto.Code.Trim())
                {
                    codeRecord.AttemptCount++;
                    await _db.SaveChangesAsync();
                    return new VerificationCodeResponseDto { Successful = false, StatusCode = 400, WrongCode = true };
                }
                
                codeRecord.IsUsed = true;
                await _db.SaveChangesAsync();
                
                return new VerificationCodeResponseDto { Successful = true, StatusCode = 200, VerificationToken = codeRecord.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying code for {Email}", dto.Email);
                return new VerificationCodeResponseDto { Successful = false, StatusCode = 500, Message = "Error verifying code." };
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _config["JWTKey:Default"];
            if (string.IsNullOrEmpty(keyString)) throw new Exception("JWT Key is missing in appsettings.json");

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
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}