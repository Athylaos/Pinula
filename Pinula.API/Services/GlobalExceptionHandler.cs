using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Pinula.Shared.DTOs;

namespace Pinula.API.Services;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";
        
        var response = new GeneralResponse
        {
            Successful = false,
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = "An unexpected server error occurred. Please try again later."
        };
        
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        
        return true;
    }
}