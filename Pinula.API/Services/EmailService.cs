using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Pinula.Shared.Enums;
using Pinula.API.Interface;
using Pinula.API.Resources;

namespace Pinula.API.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IStringLocalizer<App> _localizer;

    public EmailService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailService> logger, IStringLocalizer<App> localizer)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _localizer = localizer;

        var apiKey = _configuration["Resend:ApiKey"];
        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task SendVerificationCodeAsync(string email, string code, VerificationCodeType type, string culture = "en")
    {
        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
        
        var subject = GetSubjectForType(type);
        var htmlContent = GetEmailTemplate(code, type, cultureInfo);
        var fromEmail = _configuration["Resend:FromEmail"] ?? "Pinula <noreply@pinula.app>";

        var payload = new
        {
            from = fromEmail,
            to = new[] { email },
            subject = subject,
            html = htmlContent
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync("emails", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error while sending trough Resend. Status: {Status}, Detail: {Error}", response.StatusCode, errorResponse);
                throw new InvalidOperationException("Sending email failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception for email: {Email}", email);
            throw;
        }
    }

    private string GetSubjectForType(VerificationCodeType type) => type switch
    {
        VerificationCodeType.Registration => _localizer["EmailSubject_Registration"],
        VerificationCodeType.PasswordReset => _localizer["EmailSubject_PasswordReset"],
        VerificationCodeType.EmailChange => _localizer["EmailSubject_EmailChange"],
        VerificationCodeType.SensitiveAction => _localizer["EmailSubject_SensitiveAction"],
        _ => _localizer["EmailSubject_Default"]
    };

   private static string GetEmailTemplate(string code, VerificationCodeType type, CultureInfo culture)
    {
        var actionName = type switch
        {
            VerificationCodeType.Registration => App.ResourceManager.GetString("ActionName_Registration", culture),
            VerificationCodeType.PasswordReset => App.ResourceManager.GetString("ActionName_PasswordReset", culture),
            VerificationCodeType.EmailChange => App.ResourceManager.GetString("ActionName_EmailChange", culture),
            _ => App.ResourceManager.GetString("ActionName_Default", culture)
        };

        var title = App.ResourceManager.GetString("EmailTemplate_Title", culture);
        var bodyPrefix = string.Format(App.ResourceManager.GetString("EmailTemplate_BodyPrefix", culture) ?? "Use the following code for {0}:", actionName);
        
        var rawValidity = App.ResourceManager.GetString("EmailTemplate_Validity", culture) ?? "";
        var validity = rawValidity.Replace("&lt;", "<").Replace("&gt;", ">");

        var rights = App.ResourceManager.GetString("EmailTemplate_Rights", culture);
        var year = DateTime.UtcNow.Year;

        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                 <head>
                     <meta charset="utf-8">
                     <meta name="viewport" content="width=device-width, initial-scale=1.0">
                     <style>
                         @import url('https://fonts.googleapis.com/css2?family=Alegreya:wght@700;800&family=Nunito:wght@400;600;700;800&display=swap');
                         
                         body { 
                             font-family: 'Nunito', 'Segoe UI', Tahoma, sans-serif; 
                             background-color: #1f1f1f; 
                             color: #f1f2f6; 
                             margin: 0; 
                             padding: 40px 20px; 
                             -webkit-font-smoothing: antialiased;
                         }
                         .card { 
                             max-width: 500px; 
                             margin: 0 auto; 
                             background-color: #212121; 
                             border-radius: 20px; 
                             border: 1px solid rgba(255, 255, 255, 0.05); 
                             padding: 40px 30px; 
                             text-align: center; 
                             box-shadow: 0 10px 40px rgba(0,0,0,0.4);
                         }
                         .logo-img {
                             width: 64px;
                             height: 64px;
                             border-radius: 50%;
                             object-fit: cover;
                             display: inline-block;
                             margin-bottom: 20px;
                             border: 0;
                             outline: none;
                         }
                         h2 {
                             font-family: 'Alegreya', Georgia, serif; 
                             font-size: 1.8rem;
                             margin-top: 0;
                             margin-bottom: 12px;
                             color: #ffffff;
                         }
                         p {
                             color: #a0a5b5;
                             font-size: 1.05rem;
                             line-height: 1.5;
                             margin-bottom: 25px;
                         }
                         .code-box { 
                             background-color: #112718; 
                             border: 1px dashed rgba(99, 240, 189, 0.6); 
                             border-radius: 16px; 
                             padding: 20px 25px; 
                             font-size: 2.5rem; 
                             font-weight: 800; 
                             color: #63f0bd; 
                             letter-spacing: 8px; 
                             margin: 0 auto 30px auto; 
                             display: inline-block; 
                             box-shadow: inset 0 0 15px rgba(29, 73, 52, 0.3);
                         }
                         .validity-box {
                             background: rgba(255, 255, 255, 0.02);
                             border: 1px solid rgba(255, 255, 255, 0.05);
                             border-radius: 12px;
                             padding: 15px;
                             font-size: 0.9rem;
                             color: #888;
                         }
                         .validity-box strong {
                             color: #e0e0e0;
                         }
                         .footer { 
                             font-size: 0.8rem; 
                             color: #666; 
                             margin-top: 40px; 
                             text-transform: uppercase;
                             letter-spacing: 1px;
                         }
                     </style>
                 </head>
                 <body>
                     <div class="card">
                         <img src="https://api-pinula.hykys.eu/images/icons/pinula_logo_1024.png" 
                              alt="Pinula logo" 
                              class="logo-img" 
                              width="64" 
                              height="64" />

                         <h2>{{title}}</h2>
                         <p>{{bodyPrefix}}</p>
                         
                         <div class="code-box">{{code}}</div>
                         
                         <div class="validity-box">
                             {{validity}}
                         </div>
                         
                         <div class="footer">&copy; {{year}} Pinula. {{rights}}</div>
                     </div>
                 </body>
                 </html>
                 """;
    }
}