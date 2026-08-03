using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Files.Errors;

public static class FileErrors
{
    public static readonly Error UnsupportedFileType = Error.Validation(
        "File.UnsupportedFileType", "This file type is not allowed. Upload an image or a PDF.");

    public static readonly Error Empty = Error.Validation("File.Empty", "The uploaded file is empty.");

    public static readonly Error TooLarge = Error.Validation("File.TooLarge", "The uploaded file exceeds the 5 MB limit.");
}
