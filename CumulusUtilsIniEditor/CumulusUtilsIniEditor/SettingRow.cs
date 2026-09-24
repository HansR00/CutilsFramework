using System.ComponentModel;
using System.Drawing;

namespace CumulusUtilsIniEditor;

/// <summary>
/// De rol van een regel in het raster. Bepaalt of er bewerkt mag worden en
/// welke achtergrondkleur de regel krijgt. Dit is de enige bron van waarheid:
/// zowel de cel (bewerkbaar of niet) als het raster (kleur) lezen hieruit.
/// </summary>
public enum RowKind
{
    /// <summary>Gewone instelling: bewerkbaar, witte achtergrond.</summary>
    Editable,
    /// <summary>Gewijzigd maar nog niet opgeslagen: geel.</summary>
    Modified,
    /// <summary>Door CumulusUtils bijgehouden: grijs, niet te bewerken.</summary>
    ReadOnly,
    /// <summary>Uitgeschakelde voorbeeldregel met ';': lichtgrijs en cursief.</summary>
    Comment
}

/// <summary>
/// Eén regel in het raster. Een instelling, of een commentaarregel op zijn eigen
/// plek in het bestand.
///
/// De regel leidt uit zijn <see cref="Type"/> af wat zijn <see cref="Kind"/> is.
/// Daaruit volgen twee dingen: of de waarde bewerkt mag worden, en welke
/// achtergrondkleur de regel heeft. Het raster vraagt dat bij elke tekening
/// opnieuw op, zodat de kleur klopt bij cursor-beweging, Enter en muisklik.
/// </summary>
public sealed class SettingRow : INotifyPropertyChanged
{
    private string _baseline;
    private string _value;

    public IniEntry Entry { get; }
    public string Section { get; }

    private readonly SettingSpec _spec;

    public SettingRow(string section, IniEntry entry)
    {
        Section = section;
        Entry = entry;
        _baseline = entry.IsComment ? string.Empty : entry.Value;
        _value = _baseline;
        _spec = entry.IsComment
            ? SettingSpec.Text()
            : SettingSchema.Resolve(section, entry.Key, entry.Value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- wat voor regel is dit? --------------------------------------------

    public bool IsComment => Entry.IsComment;

    /// <summary>Het type van deze regel, afgeleid uit het schema.</summary>
    public SettingType Type => Spec.Type;

    /// <summary>
    /// De rol van deze regel. Dit is dé eigenschap waarop het raster zijn
    /// gedrag en kleur baseert.
    /// </summary>
    public RowKind Kind
    {
        get
        {
            if (IsComment) return RowKind.Comment;
            if (Type == SettingType.ReadOnly) return RowKind.ReadOnly;
            return IsDirty ? RowKind.Modified : RowKind.Editable;
        }
    }

    /// <summary>
    /// Mag de waarde bewerkt worden? Alleen een gewone of gewijzigde instelling.
    /// Een read-only-regel en een commentaarregel niet.
    /// </summary>
    public bool CanEdit => Kind is RowKind.Editable or RowKind.Modified;

    // ---- weergave -----------------------------------------------------------

    /// <summary>
    /// De sleutel voor weergave. Voor een commentaarregel is dat de hele regel
    /// (inclusief ';'), zodat hij op zijn eigen plek in het bestand staat.
    /// </summary>
    public string Key => IsComment ? Entry.RawComment : Entry.Key;

    /// <summary>De sleutel waar dit commentaar bij hoort, of leeg.</summary>
    public string AttachedTo => IsComment ? Entry.AttachedToKey : string.Empty;

    /// <summary>Commentaar dat bij de regel hierboven hoort, in plaats van vrijstaand.</summary>
    public bool IsAttachedComment => IsComment && AttachedTo.Length > 0;

    /// <summary>De tekst van het commentaar zonder het leidende ';' of '#'.</summary>
    public string CommentText
    {
        get
        {
            var s = Entry.RawComment.TrimStart();
            return s.Length > 0 && (s[0] == ';' || s[0] == '#') ? s[1..].TrimStart() : s;
        }
    }

    /// <summary>
    /// De spec van deze rij. Voor een commentaarregel tonen we het type van de
    /// échte sleutel, zodat het Type-label klopt.
    /// </summary>
    public SettingSpec Spec => IsComment
        ? new SettingSpec
        {
            Type = SettingSchema.Resolve(Section, SpecKey, string.Empty).Type,
            AllowCustomValue = true
        }
        : _spec;

    /// <summary>De sleutel waarop een commentaarregel zijn type baseert.</summary>
    private string SpecKey =>
        AttachedTo.Length > 0 ? AttachedTo : StripCommentMarker(Entry.RawComment);

    /// <summary>Haalt ';' of '#' en de '=' weg, zodat de sleutelnaam overblijft.</summary>
    private static string StripCommentMarker(string raw)
    {
        var s = raw.TrimStart(';', '#', ' ', '\t');
        int eq = s.IndexOf('=');
        return eq > 0 ? s[..eq].Trim() : s.Trim();
    }

    // ---- waarde -------------------------------------------------------------

    public bool BoolValue
    {
        get => ValueClassifier.AsBool(_value);
        set => Value = value ? "true" : "false";
    }

    public string Value
    {
        get => IsComment ? string.Empty : _value;
        set
        {
            if (IsComment) return;
            var v = value ?? string.Empty;
            if (_value == v) return;
            _value = v;
            Entry.Value = v;
            Raise(nameof(Value));
            Raise(nameof(IsDirty));
            Raise(nameof(Kind));      // de kleur kan veranderen
            Raise(nameof(CanEdit));
        }
    }

    public bool IsDirty =>
        !IsComment && !string.Equals(_value, _baseline, StringComparison.Ordinal);

    /// <summary>Na een geslaagde opslag vervalt de wijzigingsmarkering.</summary>
    public void Snapshot()
    {
        if (IsComment) return;
        _baseline = Entry.Value;
        _value = Entry.Value;
        Raise(nameof(IsDirty));
        Raise(nameof(Kind));
        Raise(nameof(CanEdit));
    }

    // ---- kleur --------------------------------------------------------------

    /// <summary>De achtergrondkleur die bij deze rol hoort.</summary>
    public static Color BackgroundFor(RowKind kind) => kind switch
    {
        RowKind.Modified => Color.FromArgb(255, 246, 200),   // lichtgeel
        RowKind.ReadOnly => Color.FromArgb(242, 242, 242),   // lichtgrijs
        RowKind.Comment => Color.FromArgb(250, 250, 250),    // zeer lichtgrijs
        _ => SystemColors.Window                             // wit
    };

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
