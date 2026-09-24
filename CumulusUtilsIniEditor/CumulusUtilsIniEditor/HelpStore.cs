using System.Text;

namespace CumulusUtilsIniEditor;

/// <summary>
/// Per-key help text, loaded from an INI file next to the executable
/// (cumulusutils-help.ini). Structure mirrors cumulusutils.ini itself:
///
///     [General]
///     Language=Taal van de gegenereerde website.
///     TraceInfoLevel=Hoeveel detail CumulusUtils wegschrijft.
///
/// A missing file is not an error — the (i) buttons simply stay disabled.
/// The file is re-read on every Open, so edits show up without a restart.
/// </summary>
public sealed class HelpStore
{
    public const string FileName = "cumulusutils-help.ini";

    private readonly Dictionary<string, string> _bySectionKey =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _byKey =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Path the texts were loaded from, or null when no file was found.</summary>
    public string? SourcePath { get; private set; }

    public bool HasAnyText => _byKey.Count > 0 || _bySectionKey.Count > 0;

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, FileName);

    public static HelpStore Load(string? path = null)
    {
        var store = new HelpStore();
        var file = path ?? DefaultPath;

        if (!File.Exists(file))
            return store;

        try
        {
            store.Parse(file);
            store.SourcePath = file;
        }
        catch
        {
            // Een onleesbaar help-bestand mag het programma nooit blokkeren.
            store.SourcePath = null;
        }

        return store;
    }

    private void Parse(string file)
    {
        string currentSection = string.Empty;

        foreach (var raw in File.ReadAllLines(file, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith(';') || line.StartsWith('#')) continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq < 0) continue;

            var key = line[..eq].Trim();
            var text = line[(eq + 1)..].Trim();
            if (key.Length == 0 || text.Length == 0) continue;

            if (key == "*")
            {
                // Sectie-brede uitleg.
                _sections[currentSection] = text;
                continue;
            }

            if (currentSection.Length > 0)
                _bySectionKey[$"{currentSection}/{key}"] = text;

            // Kale sleutel als terugval, zodat één tekst voor alle secties kan gelden.
            if (!_byKey.ContainsKey(key))
                _byKey[key] = text;
        }
    }

    /// <summary>
    /// Zoekt de uitleg voor een sleutel: eerst Sectie/Sleutel, dan de sectie-brede
    /// tekst, dan de kale sleutelnaam. Leeg wanneer niets gevonden is.
    /// </summary>
    public string? Lookup(string section, string key)
    {
        if (_bySectionKey.TryGetValue($"{section}/{key}", out var exact))
            return exact;

        if (_sections.TryGetValue(section, out var sectionText))
            return sectionText;

        if (_byKey.TryGetValue(key, out var byKey))
            return byKey;

        return null;
    }

    public bool Has(string section, string key) =>
        !string.IsNullOrWhiteSpace(Lookup(section, key));
}
