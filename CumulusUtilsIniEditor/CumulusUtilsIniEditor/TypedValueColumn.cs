using System.ComponentModel;
using System.Globalization;

namespace CumulusUtilsIniEditor;

/// <summary>
/// Kolom voor de waarde. Gebruikt <see cref="TypedValueCell"/>, die per rij
/// bepaalt welk besturingselement verschijnt en of er bewerkt mag worden.
/// </summary>
public sealed class TypedValueColumn : DataGridViewColumn
{
    public TypedValueColumn() : base(new TypedValueCell()) { }

    public override object Clone()
    {
        var c = (TypedValueColumn)base.Clone();
        return c;
    }
}

/// <summary>
/// Cell voor de waarde-kolom. Geeft het raster per rij door of er bewerkt mag
/// worden; een read-only- of voorbeeldregel komt niet in bewerkmodus.
/// </summary>
public sealed class TypedValueCell : DataGridViewTextBoxCell
{
    public override Type ValueType => typeof(string);

    public override Type EditType => typeof(TypedValueEditingControl);

    public override object Clone() => base.Clone();

    /// <summary>
    /// Alleen bewerkbaar als de rij dat toestaat. De beslissing komt van
    /// <see cref="SettingRow.CanEdit"/>, dat uit het type van de rij volgt.
    /// We kijken via <c>OwningRow</c> en niet via <c>RowIndex</c>: op het moment
    /// dat het raster dit opvraagt, is <c>RowIndex</c> vaak nog -1.
    /// </summary>
    public override bool ReadOnly
    {
        get
        {
            var owner = Owner;
            if (owner is not null && !owner.CanEdit)
                return true;

            return base.ReadOnly;
        }
        set => base.ReadOnly = value;
    }

    /// <summary>De rij die deze cel bezit, of null wanneer de cel zweeft.</summary>
    private SettingRow? Owner
    {
        get
        {
            if (OwningRow?.DataBoundItem is SettingRow r) return r;
            if (DataGridView is not null && RowIndex >= 0 &&
                DataGridView.Rows[RowIndex].DataBoundItem is SettingRow r2)
                return r2;
            return null;
        }
    }

    /// <summary>
    /// Weigert het wissen of wijzigen van de celinhoud wanneer de rij read-only
    /// of commentaar is. Het raster handelt Delete en Backspace namelijk zelf af
    /// op de geselecteerde cel, buiten de editor om.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e, int rowIndex)
    {
        var owner = Owner;
        if (owner is not null && !owner.CanEdit &&
            e.KeyCode is Keys.Delete or Keys.Back or Keys.Space)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e, rowIndex);
    }

    protected override void OnKeyPress(KeyPressEventArgs e, int rowIndex)
    {
        var owner = Owner;
        if (owner is not null && !owner.CanEdit)
        {
            e.Handled = true;
            return;
        }

        base.OnKeyPress(e, rowIndex);
    }

    /// <summary>
    /// Blokkeert ook het starten van een bewerking. Zonder deze override opent
    /// het raster alsnog de editor, ongeacht wat <see cref="ReadOnly"/> meldt.
    /// </summary>
    public override void InitializeEditingControl(
        int rowIndex, object? initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
        if (DataGridView?.EditingControl is TypedValueEditingControl editor &&
            DataGridView.Rows[rowIndex].DataBoundItem is SettingRow row)
        {
            editor.Bind(row, this);
        }
    }
}

/// <summary>
/// A single editing control that hosts whichever child widget the row needs.
/// </summary>
public sealed class TypedValueEditingControl : UserControl, IDataGridViewEditingControl
{
    private DataGridView? _grid;
    private int _rowIndex;
    private bool _valueChanged;
    private SettingRow? _row;
    private DataGridViewCell? _cell;
    private bool _binding;

    private readonly ComboBox _combo = new();
    private readonly NumericUpDown _number = new();
    private readonly TextBox _text = new();
    private readonly Panel _colourSwatch = new();
    private readonly TextBox _colourText = new();
    private readonly Button _colourPick = new();

    public TypedValueEditingControl()
    {
        _combo.Visible = false;
        _number.Visible = false;
        _text.Visible = false;
        _colourSwatch.Visible = false;
        _colourText.Visible = false;
        _colourPick.Visible = false;

        // Het kleurvakje zelf opent de kleurkiezer; de knop ernaast doet hetzelfde.
        _colourSwatch.Cursor = Cursors.Hand;
        _colourSwatch.Click += (_, _) => PickColour();
        _colourPick.Click += (_, _) => PickColour();
        _colourPick.Text = "…";
        _colourPick.FlatStyle = FlatStyle.System;

        _combo.SelectedIndexChanged += (_, _) => MarkChanged(_combo.Text);
        _combo.TextChanged += (_, _) => MarkChanged(_combo.Text);
        _number.ValueChanged += (_, _) => MarkChanged(FormatNumber(_number.Value));
        _text.TextChanged += (_, _) => MarkChanged(_text.Text);
        _colourText.TextChanged += (_, _) => { UpdateSwatch(_colourText.Text); MarkChanged(_colourText.Text); };

        // Pijltjes Omhoog/Omlaag moeten naar het raster, ook als het kind-
        // besturingselement ze normaal zelf afhandelt. Een ComboBox en een
        // NumericUpDown verwerken ze via hun eigen ProcessDialogKey; daarom
        // hangen we de afhandeling direct aan de widgets.
        _combo.KeyDown += Widget_KeyDown;
        _number.KeyDown += Widget_KeyDown;
        _text.KeyDown += Widget_KeyDown;
        _colourText.KeyDown += Widget_KeyDown;

        Controls.Add(_combo);
        Controls.Add(_number);
        Controls.Add(_text);
        Controls.Add(_colourSwatch);
        Controls.Add(_colourText);
        Controls.Add(_colourPick);
    }

    // ---- bind a row to the right widget ------------------------------------

    public void Bind(SettingRow row, DataGridViewCell cell)
    {
        // Tijdens het opzetten mogen de change-events van de widgets niets doen.
        _binding = true;
        try
        {
            _row = row;
            _cell = cell;
            HideAll();

            var spec = row.Spec;
            switch (spec.Type)
            {
                case SettingType.Boolean:
                    // true/false als keuzelijst.
                    Show(_combo);
                    _combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    _combo.Items.Clear();
                    _combo.Items.Add("true");
                    _combo.Items.Add("false");
                    _combo.SelectedItem = ValueClassifier.AsBool(row.Value) ? "true" : "false";
                    break;

                case SettingType.Choice:
                    Show(_combo);
                    _combo.DropDownStyle = spec.AllowCustomValue
                        ? ComboBoxStyle.DropDown
                        : ComboBoxStyle.DropDownList;
                    _combo.Items.Clear();
                    if (spec.Options is not null)
                        foreach (var o in spec.Options) _combo.Items.Add(o);
                    _combo.Text = row.Value;
                    if (!spec.AllowCustomValue && _combo.SelectedIndex < 0 &&
                        _combo.Items.Count > 0)
                        _combo.SelectedIndex = 0;
                    break;

                case SettingType.Integer:
                case SettingType.Decimal:
                    Show(_number);
                    _number.DecimalPlaces = spec.Type == SettingType.Decimal ? 2 : 0;
                    _number.Minimum = spec.Min ?? -1000000m;
                    _number.Maximum = spec.Max ?? 1000000m;
                    _number.Increment = spec.Step ?? (spec.Type == SettingType.Decimal ? 0.1m : 1m);
                    _number.ThousandsSeparator = false;
                    if (decimal.TryParse(row.Value, NumberStyles.Float,
                                         CultureInfo.InvariantCulture, out var d))
                        _number.Value = Clamp(d, _number.Minimum, _number.Maximum);
                    break;

                case SettingType.Colour:
                    Show(_colourSwatch);
                    Show(_colourText);
                    Show(_colourPick);
                    _colourText.Text = row.Value;
                    UpdateSwatch(row.Value);
                    break;

                default:
                    Show(_text);
                    _text.Text = row.Value;
                    break;
            }
        }
        finally
        {
            _binding = false;
            _valueChanged = false;
        }
    }

    private static decimal Clamp(decimal v, decimal min, decimal max) =>
        v < min ? min : v > max ? max : v;

    private void HideAll()
    {
        _combo.Visible = false;
        _number.Visible = false;
        _text.Visible = false;
        _colourSwatch.Visible = false;
        _colourText.Visible = false;
        _colourPick.Visible = false;
    }

    private void Show(Control c) => c.Visible = true;

    // ---- layout -------------------------------------------------------------

    private void LayoutChildren()
    {
        int h = Math.Max(Height, 20);

        if (_text.Visible)
        {
            _text.Bounds = new Rectangle(0, 0, Width, h);
        }
        else if (_combo.Visible)
        {
            _combo.Bounds = new Rectangle(0, 0, Width, h);
        }
        else if (_number.Visible)
        {
            _number.Bounds = new Rectangle(0, 0, Width, h);
        }
        else if (_colourText.Visible)
        {
            // Kleurvakje ruim: de volledige naam of hex-waarde moet erin passen.
            int swatch = 90;
            _colourSwatch.Bounds = new Rectangle(0, 2, swatch, h - 4);
            _colourPick.Bounds = new Rectangle(Width - 30, 2, 30, h - 4);
            _colourText.Bounds = new Rectangle(swatch + 4, 0, Width - swatch - 36, h);
        }
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        LayoutChildren();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutChildren();
    }

    // ---- colour helpers -----------------------------------------------------

    // Sluit de systeemkleuren (ActiveBorder, …) uit: die zijn geen geldige CSS-kleuren.
    private static readonly Dictionary<int, string> HtmlNames = BuildHtmlNames();

    private static Dictionary<int, string> BuildHtmlNames()
    {
        var map = new Dictionary<int, string>();
        foreach (var prop in typeof(Color).GetProperties(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (prop.PropertyType != typeof(Color)) continue;
            if (prop.GetValue(null) is not Color c) continue;
            if (!c.IsKnownColor) continue;

            var name = c.Name;
            if (name.StartsWith("Active", StringComparison.Ordinal) ||
                name.StartsWith("Control", StringComparison.Ordinal) ||
                name.StartsWith("Info", StringComparison.Ordinal) ||
                name.StartsWith("Menu", StringComparison.Ordinal) ||
                name.StartsWith("Window", StringComparison.Ordinal) ||
                name.StartsWith("Scroll", StringComparison.Ordinal) ||
                name.StartsWith("GrayText", StringComparison.Ordinal) ||
                name.StartsWith("Highlight", StringComparison.Ordinal) ||
                name.StartsWith("Button", StringComparison.Ordinal) ||
                name.StartsWith("App", StringComparison.Ordinal) ||
                name is "Desktop" or "GradientActiveCaption" or "GradientInactiveCaption" or
                        "InactiveBorder" or "InactiveCaption" or "InactiveCaptionText" or
                        "HotTrack" or "AppWorkspace")
                continue;

            int rgb = (c.R << 16) | (c.G << 8) | c.B;
            map[rgb] = name;   // laatste wint; CSS-namen zijn eenduidig
        }
        return map;
    }

    /// <summary>
    /// Kleurnaam als die bestaat voor deze exacte RGB, anders #rrggbb. Zo blijft
    /// "Lightgrey" een naam en wordt #f0f0f0 niet onnodig omgezet.
    /// </summary>
    public static string ColourToIniValue(Color c)
    {
        int rgb = (c.R << 16) | (c.G << 8) | c.B;
        return HtmlNames.TryGetValue(rgb, out var name)
            ? name
            : $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    private void UpdateSwatch(string value)
    {
        _colourSwatch.BackColor = TryParseColour(value, out var c) ? c : SystemColors.Control;
        _colourSwatch.BorderStyle = BorderStyle.FixedSingle;
    }

    private void PickColour()
    {
        using var dlg = new ColorDialog
        {
            FullOpen = true,
            Color = TryParseColour(_colourText.Text, out var c) ? c : Color.White
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _colourText.Text = ColourToIniValue(dlg.Color);
    }

    public static bool TryParseColour(string value, out Color colour)
    {
        colour = Color.Empty;
        var v = value.Trim();
        if (v.Length == 0) return false;

        if (v.StartsWith('#'))
        {
            var hex = v[1..];
            if (hex.Length == 3)
                hex = string.Concat(hex.Select(ch => new string(ch, 2)));
            if (hex.Length == 6 &&
                int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                colour = Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                return true;
            }
            return false;
        }

        var known = Color.FromName(v);
        if (known.IsKnownColor || known.A == 255 && v.Equals(known.Name, StringComparison.OrdinalIgnoreCase))
        {
            colour = known;
            return known.IsKnownColor;
        }
        return false;
    }

    private static string FormatNumber(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    // ---- change tracking ----------------------------------------------------

    /// <summary>
    /// Houdt bij of de gebruiker iets gewijzigd heeft. De waarde wordt NIET bij
    /// elke toetsaanslag naar de rij geschreven: dat laat het raster de cel
    /// opnieuw binden en gooit de editor terug op de oude tekst. Het wegschrijven
    /// gebeurt in <see cref="CommitValue"/>, bij Enter of het verlaten van de cel.
    /// </summary>
    private void MarkChanged(string newText)
    {
        if (_binding) return;
        _valueChanged = true;
    }

    /// <summary>Schrijft de huidige editorwaarde naar de rij.</summary>
    private void CommitValue()
    {
        if (_row is null || _binding) return;

        string value = CurrentEditorText();

        // Getallen normaliseren naar invariant, zodat "5" geen "5,00" wordt.
        if (_number.Visible)
            value = FormatNumber(_number.Value);

        if (string.Equals(_row.Value, value, StringComparison.Ordinal)) return;

        _row.Value = value;
    }

    /// <summary>De tekst zoals die nu in het actieve kind-besturingselement staat.</summary>
    private string CurrentEditorText()
    {
        if (_combo.Visible) return _combo.Text;
        if (_number.Visible) return FormatNumber(_number.Value);
        if (_colourText.Visible) return _colourText.Text;
        if (_text.Visible) return _text.Text;
        return string.Empty;
    }

    // ---- IDataGridViewEditingControl ---------------------------------------

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DataGridView? EditingControlDataGridView
    {
        get => _grid;
        set => _grid = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? EditingControlFormattedValue
    {
        get => _number.Visible ? FormatNumber(_number.Value) : _row?.Value ?? string.Empty;
        set
        {
            if (_row is null) return;

            var text = value?.ToString() ?? string.Empty;

            if (_combo.Visible) _combo.Text = text;
            else if (_number.Visible)
            {
                if (decimal.TryParse(text, NumberStyles.Float,
                                     CultureInfo.InvariantCulture, out var d))
                    _number.Value = Clamp(d, _number.Minimum, _number.Maximum);
            }
            else if (_colourText.Visible) _colourText.Text = text;
            else if (_text.Visible) _text.Text = text;
        }
    }

    /// <summary>
    /// Bepaalt welke toetsen de editor zelf afhandelt. Alleen Links/Rechts (en
    /// Home/End) hebben cursor-betekenis; Omhoog/Omlaag gaan naar het raster,
    /// zodat je regel voor regel kunt navigeren. Tab en Escape verlaten de cel.
    /// </summary>
    public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
    {
        var key = keyData & Keys.KeyCode;

        if (key is Keys.Tab or Keys.Escape)
            return false;

        if (_combo.Visible)
        {
            // Open keuzelijst: pijltjes kiezen in de lijst zelf.
            if (_combo.DroppedDown)
                return key is not (Keys.Tab or Keys.Escape);

            return key is Keys.Up or Keys.Down or Keys.Enter;
        }

        return key is Keys.Left or Keys.Right or Keys.Home or Keys.End;
    }

    /// <summary>
    /// Vangt Omhoog/Omlaag af op het kind-besturingselement zelf. Een ComboBox
    /// en een NumericUpDown slikken die toetsen anders in, waardoor je met een
    /// true/false-veld niet naar de volgende regel kon.
    /// </summary>
    private void Widget_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.Up or Keys.Down)) return;

        // Bij een open keuzelijst horen de pijltjes bij de lijst zelf.
        if (ReferenceEquals(sender, _combo) && _combo.DroppedDown) return;

        CommitValue();
        _valueChanged = false;
        _grid?.EndEdit();
        e.Handled = true;

        // Zelf de cel verplaatsen: EndEdit laat het raster nog niet navigeren.
        MoveRow(e.KeyCode == Keys.Down ? 1 : -1);
    }

    /// <summary>
    /// Schuift de selectie één regel op, binnen de grenzen van het raster.
    /// Alleen een bewerkbare regel gaat daarna in bewerkmodus.
    /// </summary>
    private void MoveRow(int delta)
    {
        if (_grid is null || _rowIndex < 0) return;

        int target = _rowIndex + delta;
        if (target < 0 || target >= _grid.Rows.Count) return;

        int col = _grid.CurrentCell?.ColumnIndex ?? _grid.Columns.Count - 1;
        _grid.CurrentCell = _grid.Rows[target].Cells[col];

        if (_grid.Rows[target].DataBoundItem is SettingRow next && next.CanEdit)
            _grid.BeginEdit(true);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int EditingControlRowIndex
    {
        get => _rowIndex;
        set => _rowIndex = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EditingControlValueChanged
    {
        get => _valueChanged;
        set => _valueChanged = value;
    }

    /// <summary>
    /// Wordt door het raster aangeroepen zodra de bewerking klaar is.
    /// </summary>
    public bool CommitValueAndConfirm()
    {
        CommitValue();
        return _valueChanged;
    }

    public Cursor EditingPanelCursor => Cursors.Default;

    public bool RepositionEditingControlOnValueChange => false;

    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) =>
        EditingControlFormattedValue;

    public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
    {
        Font = style.Font;
        ForeColor = style.ForeColor;
        BackColor = style.BackColor;
        _text.Font = style.Font;
        _text.ForeColor = style.ForeColor;
        _combo.Font = style.Font;
        _number.Font = style.Font;
        _colourText.Font = style.Font;
    }

    public void PrepareEditingControlForEdit(bool selectAll)
    {
        LayoutChildren();

        if (_text.Visible)
        {
            _text.Focus();
            if (selectAll) _text.SelectAll();
        }
        else if (_combo.Visible)
        {
            _combo.Focus();
            if (_combo.DropDownStyle == ComboBoxStyle.DropDown && selectAll)
                _combo.SelectAll();
        }
        else if (_number.Visible)
        {
            _number.Focus();
            _number.Select(0, _number.Text.Length);
        }
        else if (_colourText.Visible)
        {
            _colourText.Focus();
            if (selectAll) _colourText.SelectAll();
        }
    }

    /// <summary>
    /// Enter legt de waarde vast en sluit de cel af. Omhoog/Omlaag idem, maar
    /// laat het raster daarna een regel opschuiven; zo accepteert de cursor-
    /// beweging altijd de bewerking.
    /// </summary>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        var key = keyData & Keys.KeyCode;

        if (key == Keys.Enter)
        {
            CommitValue();
            _valueChanged = false;
            _grid?.EndEdit();
            return true;
        }

        if (key is Keys.Up or Keys.Down)
        {
            // Waarde eerst vastleggen, dan de navigatie aan het raster overlaten.
            CommitValue();
            _valueChanged = false;
            _grid?.EndEdit();
            return false;
        }

        return base.ProcessDialogKey(keyData);
    }

    public override string ToString() => _row?.Value ?? string.Empty;
}
