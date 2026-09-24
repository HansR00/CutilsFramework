using System.Text;

namespace CumulusUtilsIniEditor;

/// <summary>
/// Eén regel uit het .ini-bestand: een instelling of een commentaarregel.
///
/// Commentaar hoort vaak bij de regel erbóven — het is een uitgeschakeld
/// alternatief (";Language=en-GB" bij "Language=nl-NL") of een vervolg van
/// de waarde erboven (";&lt;br/&gt;Donations welcome…" bij de HeaderLeftText).
/// Die relatie wordt in <see cref="AttachedToKey"/> bewaard.
/// </summary>
public sealed class IniEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    /// <summary>True wanneer deze regel commentaar is.</summary>
    public bool IsComment { get; set; }

    /// <summary>
    /// De oorspronkelijke regel, inclusief het leidende ';' of '#'. Alleen gezet
    /// voor commentaarregels; zo blijft de opmaak exact zoals in het bestand.
    /// </summary>
    public string RawComment { get; set; } = string.Empty;

    /// <summary>
    /// De sleutel van de regel direct erboven, wanneer deze commentaar daar
    /// logisch bij hoort. Leeg voor vrijstaand commentaar (bijv. sectiekopjes).
    /// </summary>
    public string AttachedToKey { get; set; } = string.Empty;

    /// <summary>Free-standing comment lines that appeared directly above this entry.</summary>
    public List<string> LeadingComments { get; } = new();

    /// <summary>Trailing comment on the same line, e.g. "Key=value ; note".</summary>
    public string? InlineComment { get; set; }

    public IniEntry Clone() => new()
    {
        Key = Key,
        Value = Value,
        IsComment = IsComment,
        RawComment = RawComment,
        AttachedToKey = AttachedToKey,
        InlineComment = InlineComment,
    };
}

/// <summary>An [Section] and its ordered entries.</summary>
public sealed class IniSection
{
    public string Name { get; set; } = string.Empty;
    public List<IniEntry> Entries { get; } = new();
    public List<string> LeadingComments { get; } = new();
}

/// <summary>
/// Parse / write cumulusutils.ini while preserving order, comments and quirks.
/// The format is treated as order-independent on load; output is normalised.
/// </summary>
public sealed class IniFile
{
    public List<IniSection> Sections { get; } = new();

    public static IniFile Load(string path)
    {
        var ini = new IniFile();
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        IniSection? current = null;
        var pendingComments = new List<string>();

        // De laatst gelezen instelling in deze sectie; commentaar dat er direct
        // op volgt hoort erbij.
        IniEntry? lastSetting = null;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            if (line.Length == 0)
                continue;

            var trimmed = line.TrimStart();

            // Sectiekop
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var name = trimmed[1..^1].Trim();
                current = ini.Sections.FirstOrDefault(
                    s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

                if (current is null)
                {
                    current = new IniSection { Name = name };
                    ini.Sections.Add(current);
                }
                current.LeadingComments.AddRange(pendingComments);
                pendingComments.Clear();
                lastSetting = null;
                continue;
            }

            bool isComment = trimmed.StartsWith(';') || trimmed.StartsWith('#');

            if (isComment || line.IndexOf('=') < 0)
            {
                current ??= GetOrAdd(ini, string.Empty);

                // Commentaar dat direct onder een instelling staat hoort daarbij.
                // Dat is de laatst gelezen instelling in deze sectie; commentaar
                // dat zelf weer op commentaar volgt, erft dezelfde band.
                var entry = new IniEntry
                {
                    IsComment = true,
                    RawComment = line,
                    AttachedToKey = lastSetting?.Key ?? string.Empty,
                };
                entry.LeadingComments.AddRange(pendingComments);
                pendingComments.Clear();
                current.Entries.Add(entry);
                continue;
            }

            current ??= GetOrAdd(ini, string.Empty);

            int eq = line.IndexOf('=');
            var key = line[..eq];
            var value = line[(eq + 1)..];

            string? inlineComment = null;
            int commentIdx = FindInlineComment(value);
            if (commentIdx >= 0)
            {
                inlineComment = value[commentIdx..].TrimStart();
                value = value[..commentIdx];
            }

            var setting = new IniEntry
            {
                Key = key.Trim(),
                Value = value.Trim(),
                InlineComment = inlineComment,
            };
            setting.LeadingComments.AddRange(pendingComments);
            pendingComments.Clear();
            current.Entries.Add(setting);
            lastSetting = setting;
        }

        return ini;
    }

    /// <summary>
    /// Find an inline comment separator (space followed by ; or #) that is not inside quotes.
    /// </summary>
    private static int FindInlineComment(string value)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 1; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (!inSingle && !inDouble && (c == ';' || c == '#'))
            {
                if (char.IsWhiteSpace(value[i - 1]))
                    return i;
            }
        }
        return -1;
    }

    private static IniSection GetOrAdd(IniFile ini, string name)
    {
        var s = ini.Sections.FirstOrDefault(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (s is null)
        {
            s = new IniSection { Name = name };
            ini.Sections.Add(s);
        }
        return s;
    }

    public void Save(string path)
    {
        var sb = new StringBuilder();
        bool firstSection = true;

        foreach (var section in Sections)
        {
            if (!firstSection) sb.AppendLine();
            firstSection = false;

            foreach (var c in section.LeadingComments)
                if (c.Trim().Length > 0) sb.AppendLine(c);

            if (section.Name.Length > 0)
                sb.Append('[').Append(section.Name).Append(']').AppendLine();

            foreach (var entry in section.Entries)
            {
                foreach (var c in entry.LeadingComments)
                    if (c.Trim().Length > 0) sb.AppendLine(c);

                if (entry.IsComment)
                {
                    // Exact de oorspronkelijke regel terugschrijven.
                    sb.AppendLine(entry.RawComment);
                    continue;
                }

                sb.Append(entry.Key).Append('=').Append(entry.Value);

                if (!string.IsNullOrEmpty(entry.InlineComment))
                    sb.Append(' ').Append(entry.InlineComment);

                sb.AppendLine();
            }
        }

        // UTF-8 without BOM — cumulusutils is tolerant, this keeps things clean.
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    /// <summary>Look up a value by section + key (case-insensitive). Returns null when absent.</summary>
    public string? GetValue(string section, string key)
    {
        var s = Sections.FirstOrDefault(
            x => string.Equals(x.Name, section, StringComparison.OrdinalIgnoreCase));
        var e = s?.Entries.FirstOrDefault(
            x => !x.IsComment && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        return e?.Value;
    }

    public void SetValue(string section, string key, string value)
    {
        var s = GetOrAdd(this, section);
        var e = s.Entries.FirstOrDefault(
            x => !x.IsComment && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (e is null)
        {
            e = new IniEntry { Key = key };
            s.Entries.Add(e);
        }
        e.Value = value;
    }

    public IEnumerable<IniEntry> AllEntries() =>
        Sections.SelectMany(s => s.Entries);
}
