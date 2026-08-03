using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Common;

/// <summary>A postal address. Every component is optional except the country, since data completeness varies by region.</summary>
public sealed class Address : ValueObject
{
    private Address()
    {
        Country = string.Empty;
    }

    private Address(string? street, string? city, string? state, string? postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string? Street { get; private set; }

    public string? City { get; private set; }

    public string? State { get; private set; }

    public string? PostalCode { get; private set; }

    public string Country { get; private set; }

    public static Address Create(string country, string? street = null, string? city = null, string? state = null, string? postalCode = null) =>
        new(street?.Trim(), city?.Trim(), state?.Trim(), postalCode?.Trim(), country.Trim());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() =>
        string.Join(", ", new[] { Street, City, State, PostalCode, Country }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
