using GymManager.SharedKernel.Results;

namespace GymManager.Application.Abstractions;

/// <summary>Stores uploaded files (profile photos, receipts, product images) and serves them back by URL.</summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves <paramref name="content"/> under a generated, collision-free name and returns the relative
    /// URL clients should use to retrieve it. Image content types are automatically resized and re-encoded.
    /// Non-image files are restricted to an allow-list — fails with <c>File.UnsupportedFileType</c> otherwise.
    /// </summary>
    Task<Result<string>> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    void Delete(string relativeUrl);
}
