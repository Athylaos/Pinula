using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Pinula.API.Interface;

using SkiaSharp;

namespace Pinula.API.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageProcessingService> _logger;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public ImageProcessingService(IWebHostEnvironment env, ILogger<ImageProcessingService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<ImageProcessResult> SaveImageAsync(
        IFormFile? imageFile,
        string subfolder,
        int targetWidth,
        int targetHeight,
        ImageResizeType resizeType = ImageResizeType.Max,
        string? customFileNameWithoutExt = null,
        string? oldFileName = null,
        string? defaultFileName = null,
        int quality = 80)
    {
        if (imageFile is not { Length: > 0 })
            return new ImageProcessResult(false, 400, ErrorMessage: "No image file provided.");

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return new ImageProcessResult(false, 400, ErrorMessage: "Unsupported image format.");

        var uploadFolder = Path.Combine(_env.WebRootPath, "images", subfolder);
        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        var baseName = string.IsNullOrWhiteSpace(customFileNameWithoutExt)
            ? Guid.NewGuid().ToString()
            : customFileNameWithoutExt;

        var fileName = $"{baseName}.webp";
        var filePath = Path.Combine(uploadFolder, fileName);

        try
        {
            await using var stream = imageFile.OpenReadStream();
            using var originalBitmap = SKBitmap.Decode(stream);

            if (originalBitmap is null)
                return new ImageProcessResult(false, 400, ErrorMessage: "Invalid or corrupted image file.");

            using var processedBitmap = resizeType == ImageResizeType.Crop
                ? ResizeAndCrop(originalBitmap, targetWidth, targetHeight)
                : ResizeMax(originalBitmap, targetWidth, targetHeight);

            if (processedBitmap is null)
                return new ImageProcessResult(false, 500, ErrorMessage: "Failed to resize image.");

            using var skImage = SKImage.FromBitmap(processedBitmap);
            using var encodedData = skImage.Encode(SKEncodedImageFormat.Webp, quality);

            await using var outputStream = File.OpenWrite(filePath);
            encodedData.SaveTo(outputStream);

            if (!string.IsNullOrWhiteSpace(oldFileName))
            {
                var cleanOldFileName = Path.GetFileName(oldFileName);
                if (cleanOldFileName != fileName)
                {
                    DeleteImage(subfolder, cleanOldFileName, defaultFileName);
                }
            }

            return new ImageProcessResult(true, 200, FileName: fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing image in subfolder '{Subfolder}'", subfolder);
            return new ImageProcessResult(false, 500, ErrorMessage: "Unable to process uploaded image.");
        }
    }

    public void DeleteImage(string subfolder, string? fileName, string? defaultFileName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var cleanName = Path.GetFileName(fileName);
        if (cleanName == defaultFileName || cleanName.StartsWith("default_"))
            return;

        var fullPath = Path.Combine(_env.WebRootPath, "images", subfolder, cleanName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static SKBitmap? ResizeMax(SKBitmap source, int maxWidth, int maxHeight)
    {
        int newWidth = source.Width;
        int newHeight = source.Height;

        float ratioX = maxWidth > 0 ? (float)maxWidth / source.Width : float.MaxValue;
        float ratioY = maxHeight > 0 ? (float)maxHeight / source.Height : float.MaxValue;
        float ratio = Math.Min(ratioX, ratioY);

        if (ratio < 1.0f)
        {
            newWidth = (int)(source.Width * ratio);
            newHeight = (int)(source.Height * ratio);
        }

        return source.Resize(
            new SKImageInfo(newWidth, newHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
    }

    private static SKBitmap? ResizeAndCrop(SKBitmap source, int targetWidth, int targetHeight)
    {
        float scale = Math.Max((float)targetWidth / source.Width, (float)targetHeight / source.Height);
        int scaledWidth = (int)Math.Ceiling(source.Width * scale);
        int scaledHeight = (int)Math.Ceiling(source.Height * scale);

        using var scaled = source.Resize(
            new SKImageInfo(scaledWidth, scaledHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        if (scaled is null) return null;

        int cropX = (scaledWidth - targetWidth) / 2;
        int cropY = (scaledHeight - targetHeight) / 2;

        var cropped = new SKBitmap(targetWidth, targetHeight);
        using var canvas = new SKCanvas(cropped);
        using var scaledImage = SKImage.FromBitmap(scaled);

        canvas.DrawImage(
            scaledImage,
            new SKRect(cropX, cropY, cropX + targetWidth, cropY + targetHeight),
            new SKRect(0, 0, targetWidth, targetHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        return cropped;
    }
}
