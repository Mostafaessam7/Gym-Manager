using Asp.Versioning;
using GymManager.Api.Extensions;
using GymManager.Application.Abstractions;
using GymManager.Domain.Files.Errors;
using GymManager.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Generic file upload (profile images, member documents, etc.) backed by
/// <see cref="IFileStorageService"/>. Bypasses the CQRS dispatcher since it's a single infrastructure
/// operation with no domain logic beyond the file-type allow-list enforced by the storage service.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
public sealed class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public sealed record UploadResponse(string Url);

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return Result.Failure(FileErrors.Empty).ToProblemDetails();

        if (file.Length > MaxFileSizeBytes)
            return Result.Failure(FileErrors.TooLarge).ToProblemDetails();

        await using var stream = file.OpenReadStream();
        var result = await fileStorageService.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);

        return result.IsSuccess ? Ok(new UploadResponse(result.Value)) : result.ToProblemDetails();
    }
}
