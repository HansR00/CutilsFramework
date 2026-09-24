using System.Globalization;

namespace CumulusUtilsIniEditor;

/// <summary>
/// Value-based fallback used when a key is not in the schema. Keeps the app working
/// for settings a newer CumulusUtils introduces without any code change.
/// </summary>
public static class ValueClassifier
{
    public static SettingSpec Guess(string key, string value)
    {
        if (IsBool(value)) return SettingSpec.Bool();
        if (IsInt(value)) return SettingSpec.Int();
        if (IsDecimal(value) && !key.Contains("Date", StringComparison.OrdinalIgnoreCase))
            return SettingSpec.Dec();

        if (value.Length > 120 || value.Contains('\n')) return SettingSpec.Long();

        return SettingSpec.Text();
    }

    public static bool IsBool(string v)
    {
        var t = v.Trim();
        return t.Equals("true", StringComparison.OrdinalIgnoreCase)
            || t.Equals("false", StringComparison.OrdinalIgnoreCase)
            || t.Equals("on", StringComparison.OrdinalIgnoreCase)
            || t.Equals("off", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInt(string v) =>
        long.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    public static bool IsDecimal(string v) =>
        double.TryParse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    public static bool AsBool(string v) =>
        v.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
        v.Trim().Equals("on", StringComparison.OrdinalIgnoreCase);
}
