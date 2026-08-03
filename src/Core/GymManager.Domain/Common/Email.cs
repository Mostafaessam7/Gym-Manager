using System.Text.RegularExpressions;
using GymManager.Domain.Common.Errors;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Common;

/// <summary>A validated, normalized (lower-cased) email address, shared across every bounded context.</summary>
public sealed partial class Email : ValueObject
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>(CommonErrors.EmailEmpty);

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 256 || !EmailRegex().IsMatch(normalized))
            return Result.Failure<Email>(CommonErrors.EmailInvalid);

        return Result.Success(new Email(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
