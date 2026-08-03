using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Members;

/// <summary>Free-text medical/safety information staff should know about before training a member — every
/// field is optional since completeness varies member to member.</summary>
public sealed class MedicalInfo : ValueObject
{
    private MedicalInfo()
    {
    }

    private MedicalInfo(string? bloodType, string? conditions, string? allergies, string? medications, string? notes)
    {
        BloodType = bloodType;
        Conditions = conditions;
        Allergies = allergies;
        Medications = medications;
        Notes = notes;
    }

    public string? BloodType { get; private set; }

    public string? Conditions { get; private set; }

    public string? Allergies { get; private set; }

    public string? Medications { get; private set; }

    public string? Notes { get; private set; }

    public static MedicalInfo Create(string? bloodType, string? conditions, string? allergies, string? medications, string? notes) =>
        new(bloodType?.Trim(), conditions?.Trim(), allergies?.Trim(), medications?.Trim(), notes?.Trim());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BloodType;
        yield return Conditions;
        yield return Allergies;
        yield return Medications;
        yield return Notes;
    }
}
