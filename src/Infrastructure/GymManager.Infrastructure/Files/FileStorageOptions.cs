namespace GymManager.Infrastructure.Files;

/// <summary>Binds the <c>FileStorage</c> configuration section used to persist uploaded files to local disk.</summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute or app-relative directory files are written to.</summary>
    public required string RootPath { get; init; }

    /// <summary>The public URL prefix files are served from (wired to <c>UseStaticFiles</c>).</summary>
    public required string PublicPathPrefix { get; init; }

    public int MaxImageDimensionPixels { get; init; } = 1024;
}
