using GymManager.Application.Abstractions;
using GymManager.Domain.Files.Errors;
using GymManager.SharedKernel.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace GymManager.Infrastructure.Files;

/// <inheritdoc cref="IFileStorageService"/>
public sealed class LocalFileStorageService(FileStorageOptions options) : IFileStorageService
{
    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/bmp", "image/gif",
    };

    // Non-image uploads (e.g. member documents) are restricted to this allow-list — anything else (in
    // particular .html/.svg/.js, which the browser would happily execute if served back same-origin via
    // the public /uploads static path) is rejected rather than stored, regardless of the claimed content type.
    private static readonly HashSet<string> AllowedNonImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
    };

    public async Task<Result<string>> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(options.RootPath);

        var isImage = ImageContentTypes.Contains(contentType);
        var extension = isImage ? ".jpg" : Path.GetExtension(fileName);

        if (!isImage && !AllowedNonImageExtensions.Contains(extension))
            return Result.Failure<string>(FileErrors.UnsupportedFileType);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(options.RootPath, storedFileName);

        if (isImage)
        {
            using var image = await Image.LoadAsync(content, cancellationToken);

            if (image.Width > options.MaxImageDimensionPixels || image.Height > options.MaxImageDimensionPixels)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(options.MaxImageDimensionPixels, options.MaxImageDimensionPixels),
                }));
            }

            await image.SaveAsync(fullPath, new JpegEncoder { Quality = 80 }, cancellationToken);
        }
        else
        {
            await using var fileStream = File.Create(fullPath);
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return Result.Success($"{options.PublicPathPrefix.TrimEnd('/')}/{storedFileName}");
    }

    public void Delete(string relativeUrl)
    {
        var fileName = Path.GetFileName(relativeUrl);
        var fullPath = Path.Combine(options.RootPath, fileName);

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
