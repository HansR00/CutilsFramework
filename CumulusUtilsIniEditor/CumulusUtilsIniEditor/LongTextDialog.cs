namespace CumulusUtilsIniEditor;

/// <summary>
/// Small modal editor for long single-line values (HTML fragments, URLs, colour lists).
/// </summary>
public sealed class LongTextDialog : Form
{
    private readonly TextBox _box = new();

    public string Result { get; private set; } = string.Empty;

    public LongTextDialog(string key, string value)
    {
        Text = $"Waarde bewerken — {key}";
        Width = 760;
        Height = 420;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        Font = new Font("Segoe UI", 9f);

        _box.Multiline = true;
        _box.ScrollBars = ScrollBars.Both;
        _box.WordWrap = true;
        _box.Dock = DockStyle.Fill;
        _box.Font = new Font("Consolas", 9.5f);
        _box.Text = value;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90, Height = 28 };
        var cancel = new Button { Text = "Annuleren", DialogResult = DialogResult.Cancel, Width = 90, Height = 28 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(_box);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;

        ok.Click += (_, _) => Result = _box.Text;
        Shown += (_, _) => { _box.Focus(); _box.SelectAll(); };
    }
}
