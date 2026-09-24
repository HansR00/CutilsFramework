using System.ComponentModel;
using System.Globalization;

namespace CumulusUtilsIniEditor;

public sealed class MainForm : Form
{
    private IniFile _ini = new();
    private HelpStore _help = new();
    private string? _path;
    private bool _dirty;
    private bool _loading;

    // Eén bindbare lijst; het raster is hier één keer aan gekoppeld.
    private readonly BindingList<SettingRow> _rows = new();
    private readonly Dictionary<string, List<SettingRow>> _bySection = new();
    private List<SettingRow> _currentSection = new();

    // Controls
    private readonly DataGridView _grid = new();
    private readonly ListBox _sections = new();
    private readonly TextBox _search = new();
    private readonly ToolStripStatusLabel _fileLabel = new();
    private readonly ToolStripStatusLabel _dirtyLabel = new();
    private readonly ToolStripStatusLabel _helpLabel = new();
    private readonly Panel _placeholder = new();

    private const int HelpColumnIndex = 0;
    private const int KeyColumnIndex = 1;
    private const int ValueColumnIndex = 2;
    private const int TypeColumnIndex = 3;

    // Herbruikbare lettertypen, zodat CellFormatting ze niet elke keer aanmaakt.
    private static readonly Font NormalFont = new("Segoe UI", 9f);
    private static readonly Font ExampleFont = new("Segoe UI", 9f, FontStyle.Italic);

    public MainForm()
    {
        Text = "CumulusUtils.ini Editor";
        Width = 1220;
        Height = 740;
        MinimumSize = new Size(880, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        BuildLayout();
        _help = HelpStore.Load();
    }

    // ---------------------------------------------------------------- layout

    private void BuildLayout()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(6, 7, 6, 4),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false
        };

        toolbar.Controls.Add(MakeButton("Open…", (_, _) => OpenFile()));
        toolbar.Controls.Add(MakeButton("Save", (_, _) => SaveFile(false)));
        toolbar.Controls.Add(MakeButton("Save as…", (_, _) => SaveFile(true)));
        toolbar.Controls.Add(new Label
        {
            Text = "|",
            Width = 16,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        });
        toolbar.Controls.Add(MakeButton("Revert", (_, _) => RevertAll()));
        toolbar.Controls.Add(MakeButton("Reload", (_, _) => ReloadFromDisk()));

        toolbar.Controls.Add(new Label
        {
            Text = "Search:",
            Width = 56,
            Height = 28,
            TextAlign = ContentAlignment.MiddleRight
        });
        _search.Width = 250;
        _search.Height = 26;
        _search.PlaceholderText = "filter on key, value or section…";
        _search.TextChanged += (_, _) => { if (!_loading) ApplyFilter(); };
        toolbar.Controls.Add(_search);

        // Section list
        _sections.Dock = DockStyle.Fill;
        _sections.IntegralHeight = false;
        _sections.BorderStyle = BorderStyle.None;
        _sections.Font = new Font("Segoe UI", 9.5f);
        _sections.ItemHeight = 22;
        _sections.SelectedIndexChanged += (_, _) => { if (!_loading) OnSectionChanged(); };

        var sidePanel = new Panel { Dock = DockStyle.Left, Width = 210, Padding = new Padding(8, 6, 4, 6) };
        sidePanel.Controls.Add(_sections);
        sidePanel.Controls.Add(new Label
        {
            Text = "Sections",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 9.5f)
        });

        // Grid
        ConfigureGrid();

        _placeholder.Dock = DockStyle.Fill;
        _placeholder.Visible = true;
        _placeholder.Controls.Add(new Label
        {
            Text = "Open cumulusutils.ini to begin.\n\nFile → Open… (Ctrl+O)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
            Font = new Font("Segoe UI", 11f)
        });

        var gridHost = new Panel { Dock = DockStyle.Fill };
        gridHost.Controls.Add(_grid);
        gridHost.Controls.Add(_placeholder);
        _grid.Visible = false;

        var statusStrip = new StatusStrip();
        _fileLabel.Text = "No file opened";
        _dirtyLabel.Text = string.Empty;
        _dirtyLabel.ForeColor = Color.Firebrick;
        _helpLabel.ForeColor = SystemColors.GrayText;
        statusStrip.Items.Add(_fileLabel);
        statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        statusStrip.Items.Add(_helpLabel);
        statusStrip.Items.Add(_dirtyLabel);

        Controls.Add(gridHost);
        Controls.Add(sidePanel);
        Controls.Add(toolbar);
        Controls.Add(statusStrip);

        FormClosing += OnFormClosing;
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S) { SaveFile(false); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.O) { OpenFile(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.F) { _search.Focus(); _search.SelectAll(); e.Handled = true; }
        };
    }

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 28,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 0, 6, 0)
        };
        b.Click += onClick;
        return b;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.MultiSelect = false;
        _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.BorderStyle = BorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;

        // Rustige selectie: geen felle systeemkleur, en de stippellijn uit.
        _grid.DefaultCellStyle.SelectionBackColor = SelectionTint;
        _grid.DefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionTint;
        _grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.WindowText;

        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = 30;
        _grid.RowTemplate.Height = 28;
        _grid.ShowCellToolTips = true;

        // De klikbare (i) per sleutel.
        var colHelp = new DataGridViewButtonColumn
        {
            Name = "colHelp",
            HeaderText = string.Empty,
            Text = "i",
            UseColumnTextForButtonValue = true,
            ReadOnly = true,
            Width = 30,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        var colKey = new DataGridViewTextBoxColumn
        {
            HeaderText = "Key",
            DataPropertyName = nameof(SettingRow.Key),
            ReadOnly = true,
            Width = 300,
            // Niet sorteerbaar: de bestandsvolgorde is betekenisvol, juist omdat
            // commentaar bij de regel erboven hoort.
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        var colVal = new TypedValueColumn
        {
            HeaderText = "Value",
            DataPropertyName = nameof(SettingRow.Value),
            // Vaste breedte: in Fill-modus negeert het raster de MinimumWidth,
            // waardoor kleurvelden op 200 px bleven hangen.
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 420,
            MinimumWidth = 400,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        // De kolom blijft bewerkbaar: de cel bepaalt per rij of er bewerkt mag
        // worden. De kolom op true zetten zou alles read-only maken.
        colVal.ReadOnly = false;
        var colType = new DataGridViewTextBoxColumn
        {
            HeaderText = "Type",
            Name = "colType",
            ReadOnly = true,
            Width = 100
        };
        var colSec = new DataGridViewTextBoxColumn
        {
            HeaderText = "Section",
            DataPropertyName = nameof(SettingRow.Section),
            ReadOnly = true,
            Width = 130,
            SortMode = DataGridViewColumnSortMode.Automatic
        };

        _grid.Columns.Add(colHelp);
        _grid.Columns.Add(colKey);
        _grid.Columns.Add(colVal);
        _grid.Columns.Add(colType);
        _grid.Columns.Add(colSec);

        // Eén vaste koppeling: het raster kijkt rechtstreeks naar _rows.
        _grid.DataSource = _rows;

        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellContentClick += Grid_CellContentClick;
        _grid.CellValidating += Grid_CellValidating;
        _grid.CellDoubleClick += Grid_CellDoubleClick;
        _grid.CellValueChanged += (_, _) => { RefreshRowStyles(); UpdateDirtyUi(); };
        _grid.CellEndEdit += (_, _) =>
        {
            // Nu pas de getypte waarde naar de rij schrijven.
            if (_grid.EditingControl is TypedValueEditingControl editor)
                editor.CommitValueAndConfirm();
            RefreshRowStyles();
            UpdateDirtyUi();
        };

        // De kleur per regel moet meebewegen met cursor, Enter en muisklik.
        _grid.CurrentCellChanged += (_, _) => RefreshRowStyles();
        _grid.SelectionChanged += (_, _) => RefreshRowStyles();
        _grid.CellMouseDown += (_, _) => _grid.Invalidate();

        // Kopiëren van sleutel en waarde, ook uit niet-bewerkbare cellen.
        _grid.KeyDown += Grid_KeyDown;
        _grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
    }

    /// <summary>
    /// Laat het raster alle regels opnieuw tekenen. De kleur wordt bij elke
    /// tekening uit de rol van de rij gehaald, dus hiermee loopt hij mee met
    /// cursor-beweging, Enter en het aanklikken van een andere regel.
    /// </summary>
    private void RefreshRowStyles()
    {
        _grid.Invalidate();
    }

    /// <summary>
    /// Ctrl+C kopieert de geselecteerde cellen. Voor read-only- en commentaar-
    /// regels, waar geen editor opent, is dit de enige manier om de tekst over
    /// te nemen; vandaar dat we het zelf afhandelen in plaats van te leunen op
    /// het standaardgedrag van het raster.
    /// </summary>
    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        // Wissen van een niet-bewerkbare cel tegenhouden. Een read-only cel
        // krijgt geen eigen toetsaanroep, dus het raster handelt Delete en
        // Backspace zelf af — hier dus blokkeren, anders wist DoneToday alsnog.
        if (e.KeyCode is Keys.Delete or Keys.Back or Keys.Space)
        {
            var current = _grid.CurrentCell;
            if (current is not null &&
                _grid.Rows[current.RowIndex].DataBoundItem is SettingRow target &&
                !target.CanEdit)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
        }

        if (!e.Control || e.KeyCode != Keys.C) return;

        var sb = new System.Text.StringBuilder();
        foreach (DataGridViewCell cell in _grid.SelectedCells.Cast<DataGridViewCell>()
                     .OrderBy(c => c.RowIndex).ThenBy(c => c.ColumnIndex))
        {
            var text = cell.Value?.ToString() ?? string.Empty;
            if (text.Length == 0 &&
                _grid.Rows[cell.RowIndex].DataBoundItem is SettingRow commentRow &&
                commentRow.IsComment)
                text = commentRow.CommentText;

            if (sb.Length > 0) sb.Append('\t');
            sb.Append(text);
        }

        if (sb.Length > 0)
        {
            try { Clipboard.SetText(sb.ToString()); }
            catch { /* klembord bezet — stil doorgaan */ }
            e.Handled = true;
        }
    }

    // ---------------------------------------------------------------- data

    private void OpenFile()
    {
        if (!ConfirmDiscardChanges()) return;
        using var dlg = new OpenFileDialog
        {
            Title = "Open cumulusutils.ini",
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            FileName = "cumulusutils.ini"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            LoadFile(dlg.FileName);
    }

    private void LoadFile(string path)
    {
        try
        {
            _loading = true;

            _ini = IniFile.Load(path);
            _path = path;
            _dirty = false;

            // Help opnieuw inlezen, zodat aanpassingen meteen zichtbaar zijn.
            _help = HelpStore.Load();

            BuildSectionIndex();
            PopulateSectionsList();

            _placeholder.Visible = false;
            _grid.Visible = true;

            _currentSection = _ini.Sections
                .SelectMany(s => _bySection[s.Name])
                .ToList();
            _sections.SelectedIndex = 0;   // "— All sections —"
            RebuildRows();

            _fileLabel.Text = path;
            Text = $"CumulusUtils.ini Editor — {Path.GetFileName(path)}";
            _helpLabel.Text = _help.HasAnyText
                ? $"Help: {Path.GetFileName(_help.SourcePath!)}"
                : "No help file found";
            UpdateDirtyUi();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open the file:\n\n" + ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void ReloadFromDisk()
    {
        if (_path is null) return;
        if (!ConfirmDiscardChanges()) return;
        LoadFile(_path);
    }

    private void BuildSectionIndex()
    {
        _bySection.Clear();
        foreach (var section in _ini.Sections)
        {
            var list = new List<SettingRow>();
            foreach (var entry in section.Entries)
                list.Add(new SettingRow(section.Name, entry));
            _bySection[section.Name] = list;
        }
    }

    private void PopulateSectionsList()
    {
        _sections.BeginUpdate();
        _sections.Items.Clear();
        _sections.Items.Add("— All sections —");
        foreach (var s in _ini.Sections)
        {
            // Commentaarregels tellen niet mee als instelling.
            int count = s.Entries.Count(e => !e.IsComment);
            int comments = s.Entries.Count(e => e.IsComment);
            var label = comments > 0
                ? $"{s.Name}  ({count} + {comments})"
                : $"{s.Name}  ({count})";
            _sections.Items.Add(label);
        }
        _sections.EndUpdate();
    }

    private void OnSectionChanged()
    {
        if (_sections.SelectedIndex <= 0)
        {
            _currentSection = _ini.Sections
                .SelectMany(s => _bySection[s.Name])
                .ToList();
        }
        else if (_sections.SelectedIndex - 1 < _ini.Sections.Count)
        {
            var name = _ini.Sections[_sections.SelectedIndex - 1].Name;
            _currentSection = _bySection.TryGetValue(name, out var rows)
                ? rows
                : new List<SettingRow>();
        }

        RebuildRows();
    }

    /// <summary>
    /// Vult de gebonden lijst opnieuw. Dit is het enige punt waar de inhoud van
    /// het raster verandert — nooit door DataSource te wisselen.
    /// </summary>
    private void RebuildRows()
    {
        var items = Filtered(_currentSection);

        _rows.RaiseListChangedEvents = false;
        _rows.Clear();
        foreach (var r in items) _rows.Add(r);
        _rows.RaiseListChangedEvents = true;
        _rows.ResetBindings();

        StyleRows();
        RefreshRowStyles();
        UpdateDirtyUi();
    }

    private void ApplyFilter() => RebuildRows();

    private List<SettingRow> Filtered(IEnumerable<SettingRow> source)
    {
        var q = _search.Text.Trim();
        if (q.Length == 0) return source.ToList();

        return source.Where(r =>
            r.Key.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Value.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Section.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            Describe(r).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void StyleRows()
    {
        // De kleuren en lettertypen worden in Grid_CellFormatting gezet, per cel
        // en bij elke tekening. Hier alleen de afgeleide kolomwaarden die niet
        // uit de binding komen.
        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            if (_grid.Rows[i].DataBoundItem is not SettingRow row) continue;

            _grid.Rows[i].Cells["colType"].Value = Describe(row);

            var helpCell = _grid.Rows[i].Cells["colHelp"];
            bool hasHelp = _help.Has(row.Section, row.Key);
            helpCell.Value = hasHelp ? "i" : string.Empty;
            helpCell.ToolTipText = hasHelp
                ? "Show help for this setting"
                : "No help text available";
        }
    }

    /// <summary>Engelse typenaam, zoals die in het Type-kolom komt.</summary>
    private static string Describe(SettingRow row)
    {
        if (row.IsComment) return "example";
        return row.Type switch
        {
            SettingType.Boolean => "true/false",
            SettingType.Integer => "integer",
            SettingType.Decimal => "decimal",
            SettingType.Choice => row.Spec.AllowCustomValue ? "choice (free)" : "choice",
            SettingType.Colour => "colour",
            SettingType.LongText => "long text",
            SettingType.CommaList => "number list",
            SettingType.ReadOnly => "read-only",
            _ => "text"
        };
    }

    private static string BuildTooltip(SettingRow row)
    {
        var parts = new List<string> { $"Section: {row.Section}" };
        if (row.Spec.Help is { Length: > 0 })
            parts.Add(row.Spec.Help);

        if (row.Type == SettingType.Choice && row.Spec.Options is not null)
        {
            var list = string.Join(", ", row.Spec.Options);
            parts.Add(row.Spec.AllowCustomValue
                ? $"Suggestions: {list}\n(custom value allowed)"
                : $"Allowed values: {list}");
        }

        if (row.Spec.Min is not null || row.Spec.Max is not null)
            parts.Add($"Range: {row.Spec.Min?.ToString(CultureInfo.InvariantCulture) ?? "…"} – " +
                      $"{row.Spec.Max?.ToString(CultureInfo.InvariantCulture) ?? "…"}");

        if (row.Type == SettingType.CommaList)
            parts.Add("Digits separated by commas, e.g. 1,2,3,4");

        if (row.Type == SettingType.LongText)
            parts.Add("Double-click for the large text window.");

        return string.Join("\n", parts);
    }

    // Rustige selectie-achtergrond. De kleur per regel komt uit SettingRow.Kind.
    private static readonly Color SelectionTint = Color.FromArgb(214, 229, 245);

    // ---------------------------------------------------------------- help

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != HelpColumnIndex) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is not SettingRow row) return;

        var text = _help.Lookup(row.Section, row.Key);
        if (string.IsNullOrWhiteSpace(text)) return;   // geen tekst → knop doet niets

        using var dlg = new HelpDialog(row.Section, row.Key, row.Value, text);
        dlg.ShowDialog(this);
    }

    // ---------------------------------------------------------------- editing

    private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.ColumnIndex != ValueColumnIndex || e.RowIndex < 0) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is not SettingRow row) return;
        if (!row.CanEdit) return;

        var text = e.FormattedValue?.ToString() ?? string.Empty;

        switch (row.Type)
        {
            case SettingType.Boolean when !ValueRules.IsValidBoolean(text):
                Fail(e, "Use true or false.");
                return;

            case SettingType.Integer when !ValueRules.IsValidInteger(text):
                Fail(e, "Enter a whole number.");
                return;

            case SettingType.Decimal when !ValueRules.IsValidDecimal(text):
                Fail(e, "Enter a number (use a dot as decimal separator).");
                return;

            case SettingType.CommaList when !ValueRules.IsValidCommaList(text):
                Fail(e, ValueRules.CommaListError);
                return;
        }

        _grid.Rows[e.RowIndex].ErrorText = string.Empty;
    }

    private void Fail(DataGridViewCellValidatingEventArgs e, string message)
    {
        _grid.Rows[e.RowIndex].ErrorText = message;
        e.Cancel = true;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is not SettingRow row) return;

        // Kleur en stijl komen van de rol van de rij. Dit wordt bij elke tekening
        // opnieuw bepaald, dus ook na cursor-beweging, Enter of een muisklik.
        var kind = row.Kind;

        e.CellStyle.BackColor = SettingRow.BackgroundFor(kind);
        e.CellStyle.SelectionBackColor = SelectionTint;
        e.CellStyle.SelectionForeColor = SystemColors.WindowText;
        e.CellStyle.Font = row.IsComment ? ExampleFont : NormalFont;

        switch (kind)
        {
            case RowKind.Comment:
                e.CellStyle.ForeColor = SystemColors.GrayText;
                e.CellStyle.SelectionForeColor = SystemColors.GrayText;
                break;

            case RowKind.ReadOnly:
                e.CellStyle.ForeColor = SystemColors.GrayText;
                break;

            default:
                e.CellStyle.ForeColor = SystemColors.WindowText;
                break;
        }

        // Booleans tonen als true/false; de celinhoud blijft "true"/"false".
        if (e.ColumnIndex == ValueColumnIndex && row.Type == SettingType.Boolean &&
            e.Value is string s)
            e.Value = ValueClassifier.AsBool(s) ? "true" : "false";
    }

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != ValueColumnIndex) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is not SettingRow row) return;
        if (!row.CanEdit) return;

        if (row.Type == SettingType.LongText || row.Value.Length > 120)
        {
            using var dlg = new LongTextDialog(row.Key, row.Value);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                row.Value = dlg.Result;
                _grid.Refresh();
                RefreshRowStyles();
                UpdateDirtyUi();
            }
        }
    }

    // ---------------------------------------------------------------- save

    private void SaveFile(bool forceDialog)
    {
        if (_path is null)
        {
            MessageBox.Show(this, "Open a cumulusutils.ini file first.",
                "No file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _grid.EndEdit();

        string target = _path;
        if (forceDialog)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Save as",
                Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                FileName = Path.GetFileName(_path),
                InitialDirectory = Path.GetDirectoryName(_path) ?? string.Empty
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            target = dlg.FileName;
        }

        try
        {
            if (File.Exists(target))
                File.Copy(target, $"{target}.bak", overwrite: true);

            _ini.Save(target);
            _path = target;
            _dirty = false;

            foreach (var row in _ini.Sections.SelectMany(s => _bySection[s.Name]))
                row.Snapshot();

            _fileLabel.Text = target;
            Text = $"CumulusUtils.ini Editor — {Path.GetFileName(target)}";
            _grid.Refresh();
            RefreshRowStyles();
            UpdateDirtyUi();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save the file:\n\n" + ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RevertAll()
    {
        if (_path is null) return;
        if (MessageBox.Show(this, "Discard all unsaved changes?",
                "Revert", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _search.Text = string.Empty;
        LoadFile(_path);
    }

    private void UpdateDirtyUi()
    {
        _dirty = _ini.Sections
            .SelectMany(s => _bySection.ContainsKey(s.Name) ? _bySection[s.Name] : Enumerable.Empty<SettingRow>())
            .Any(r => r.IsDirty);

        _dirtyLabel.Text = _dirty ? "● Unsaved changes" : string.Empty;
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_dirty) return true;
        var res = MessageBox.Show(this,
            "There are unsaved changes. Continue and discard them?",
            "Unsaved changes",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        return res == DialogResult.Yes;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!ConfirmDiscardChanges()) e.Cancel = true;
    }
}
