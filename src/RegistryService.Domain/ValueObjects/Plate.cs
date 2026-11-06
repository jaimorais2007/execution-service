using System.Text.RegularExpressions;
using RegistryService.Domain.Exceptions;

namespace RegistryService.Domain.ValueObjects;

public partial class Plate
{
    public string Value { get; private set; } = string.Empty;

    protected Plate() { }

    public Plate(string value)
    {
        if (!IsValid(value))
            throw new DomainException("Placa inválida");

        Value = value.Trim().ToUpperInvariant();
    }

    private static bool IsValid(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return false;

        var normalized = plate.Trim().ToUpperInvariant();
        return OldPlateRegex().IsMatch(normalized) || MercosulPlateRegex().IsMatch(normalized);
    }

    [GeneratedRegex(@"^[A-Z]{3}[0-9]{4}$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex OldPlateRegex();

    [GeneratedRegex(@"^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MercosulPlateRegex();

    public override string ToString() => Value;
}

