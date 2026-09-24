namespace CumulusUtilsIniEditor;

/// <summary>
/// Shows the help text for one setting, with its section, key, current value
/// and type. Opened by the (i) button in the grid.
/// </summary>
public sealed class HelpDialog : Form
{
    public HelpDialog(string section, string key, string value, string helpText)
    {
        Text = $"Help — {key}";
        Width = 620;
        Height = 460;
        MinimumSize = new Size(420, 300);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(14, 12, 14, 8),
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Section
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Key
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Value
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Help text

        layout.Controls.Add(MakeCaption("Section:"), 0, 0);
        layout.Controls.Add(MakeValue(section, bold: false), 1, 0);
        layout.Controls.Add(MakeCaption("Key:"), 0, 1);
        layout.Controls.Add(MakeValue(key, bold: true), 1, 1);
        layout.Controls.Add(MakeCaption("Value:"), 0, 2);
        layout.Controls.Add(MakeValue(value.Length == 0 ? "(empty)" : value, bold: false), 1, 2);

        var body = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Window,
            Font = new Font("Segoe UI", 9.5f),
            Text = helpText,
            Margin = new Padding(3, 8, 3, 3)
        };
        layout.Controls.Add(body, 0, 3);
        layout.SetColumnSpan(body, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10, 8, 10, 8)
        };
        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Width = 90,
            Height = 28
        };
        buttons.Controls.Add(close);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = close;
        CancelButton = close;
    }

    private static Label MakeCaption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(3, 3, 8, 3)
    };

    private static Label MakeValue(string text, bool bold) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(480, 0),
        Font = new Font("Segoe UI", 9f, bold ? FontStyle.Bold : FontStyle.Regular),
        Margin = new Padding(3, 3, 3, 3)
    };
}
