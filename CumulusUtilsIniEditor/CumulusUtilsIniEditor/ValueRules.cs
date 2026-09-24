using System.Globalization;

namespace CumulusUtilsIniEditor;

/// <summary>
/// Validation and normalisation for the setting values, kept in one place so the
/// grid, the typed editors and the large-text dialog all agree on the rules.
/// </summary>
public static class ValueRules
{
    /// <summary>
    /// Strict validation of a comma-separated numerical list, e.g. "1,2,3,4".
    /// No spaces around the items: an empty value is allowed (the sensor is off),
    /// anything else must be digits and single commas only.
    /// </summary>
    public static bool IsValidCommaList(string value)
    {
        var v = value.Trim();
        if (v.Length == 0) return true;   // leeg = uitgeschakeld

        // Splitsen en elk item volledig numeriek controleren.
        var parts = v.Split(',');
        foreach (var part in parts)
        {
            if (part.Length == 0) return false;             // "1,,2" of een komma op het eind
            if (!part.All(char.IsAsciiDigit)) return false;  // geen spaties, letters of tekens
        }
        return true;
    }

    /// <summary>Normalises a comma list, used when writing a corrected value back.</summary>
    public static string NormaliseCommaList(string value) =>
        value.Trim().Replace(" ", string.Empty);

    /// <summary>Human-readable failure message for a comma list.</summary>
    public const string CommaListError =
        "Use digits separated by commas, e.g. 1,2,3,4 — no spaces.";

    public static bool IsValidBoolean(string value) =>
        value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase);

    public static bool IsValidInteger(string value) =>
        value.Trim().Length == 0 ||
        long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    public static bool IsValidDecimal(string value) =>
        value.Trim().Length == 0 ||
        double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _);
}
