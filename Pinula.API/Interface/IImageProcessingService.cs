using Microsoft.AspNetCore.Http;

namespace Pinula.API.Interface;

public enum ImageResizeType
{
    Max,
    Crop
}

public record ImageProcessResult(
    bool Successful,
    int StatusCode = 200,
    string? FileName = null,
    string? ErrorMessage = null);

public interface IImageProcessingService
{
    Task<ImageProcessResult> SaveImageAsync(
        IFormFile? imageFile,
        string subfolder,
        int targetWidth,
        int targetHeight,
        ImageResizeType resizeType = ImageResizeType.Max,
        string? customFileNameWithoutExt = null,
        string? oldFileName = null,
        string? defaultFileName = null,
        int quality = 80);

    void DeleteImage(string subfolder, string? fileName, string? defaultFileName = null);
}
