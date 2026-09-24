# CumulusUtils.ini Editor

A Windows program (C# / WinForms / .NET 10) for viewing and editing
`cumulusutils.ini` — with a **typed editor**: every setting gets the control that
fits its kind of value, and where CumulusUtils defines a fixed set of values you
get a drop-down.

## Typed editor per setting

Each row is automatically given the right input control:

| Type | Control |
|---|---|
| **true/false** | checkbox |
| **choice** | drop-down with the allowed values |
| **choice (free)** | drop-down with suggestions, custom value allowed |
| **integer** | spinner with a range |
| **decimal** | spinner with decimals and a range |
| **colour** | colour swatch + picker, hex or CSS colour name |
| **number list** | text box for a comma-separated list, strictly validated |
| **long text** | text box; double-click for the large window |
| **read-only** | value shown greyed, cannot be edited |
| **text** | plain text box |

The **Type** column shows which editor you get, and the tooltip shows the allowed
values or the permitted range.

## Per-key help

Every row has a clickable **(i)** button. Clicking it opens a window with the
explanation of that setting, together with its section, key and current value.

The texts come from **`cumulusutils-help.ini`**, which must sit in the same
folder as the executable. It is re-read every time you open a file, so you can
edit it and simply reopen `cumulusutils.ini` — no restart needed.

The structure mirrors `cumulusutils.ini`:

```ini
[General]
Language=Language of the generated website.

[pwsFWI]
*=Settings for the Canadian Fire Weather Index module.
ResultFormat=Which FWI presentation the module produces.
```

Lookup order for a key:

1. `Section/Key` — the most specific text
2. a `*` line — section-wide text
3. bare `Key` — applies to that key in every section

A key without a text simply gets a greyed-out, unclickable (i). Leave the file
out entirely and the program still runs — every (i) is then disabled.

## Limited options per key

`SettingSchema.cs` holds, per key, the kind of value it is and — where
applicable — the **limited set of allowed values**:

```csharp
Add("Language", SettingSpec.Pick(new[]
{
    "en-GB", "en-US", "nl-NL", "de-DE", "fr-FR", "es-ES", "it-IT", …
}));

Add("CamType", SettingSpec.Pick(new[] { "None", "Generic", "EcowittHP10" }));

Add("ResultFormat", SettingSpec.Pick(new[] { "beteljuice", "standard" }));

// Panel-1 … Panel-24 all offer the same widget names
Add("Panel-1", SettingSpec.Pick(PanelChoices));
```

Two kinds of list:

- **`SettingSpec.Pick(...)`** — a closed list. Only one of the given values can be
  chosen; a non-editable drop-down appears.
- **`SettingSpec.Suggest(...)`** — an open list. Suggestions are offered, but
  typing your own value is still allowed.

Besides `Pick` and `Suggest` there are `Bool`, `Int`, `Dec`, `Colour`, `Text`,
`Long`, `Path`, `Url`, `List` (numerical comma list) and `ReadOnly`.

### Order of recognition

1. **Exact** on `Section/Key` — e.g. `FTP site/FtpLog`.
2. **Exact** on the bare key — e.g. `Language`.
3. **Name fragment** — anything with `Color`, `Colour`, `Threshold` or `Period`
   automatically gets the right type.
4. **From the value** — a key found nowhere in the schema is still recognised as
   `true`/`false`, integer, decimal or long text.

Point 4 means keys added by a newer CumulusUtils keep working — you only need to
extend the schema when you want to give them a drop-down.

## Other behaviour

- **Overview per section** — a list of every `[Section]` on the left, or
  *— All sections —*.
- **Search box** — filters live on key, value, section or type.
- **Example lines are preserved** — lines disabled with `;` (such as
  `;Language=en-GB`) stay visible in grey italics as reference; they cannot be
  edited and survive a save.
- **Order-independent** — sections and keys need not be in a fixed order; the
  program reads and writes them as they are.
- **Change tracking** — modified rows are highlighted; the status bar shows
  whether there are unsaved changes.
- **Automatic backup** — saving leaves a `.bak` next to the original.
- **Shortcuts** — `Ctrl+O` open, `Ctrl+S` save, `Ctrl+F` search.
- **Confirmation on exit** — a warning if there are unsaved changes.

## Validation

Values are checked strictly when you leave a cell:

- **true/false** — only `true` or `false`
- **integer** — whole numbers only
- **decimal** — numbers, with a dot as the decimal separator
- **number list** — digits separated by commas, no spaces, e.g. `1,2,3,4`.
  An empty value is allowed. `1, 2` and `1,,2` are rejected.

## Project structure

```
CumulusUtilsIniEditor/
├─ CumulusUtilsIniEditor.csproj   Project file (.NET 10, WinForms)
├─ Program.cs                     Entry point
├─ MainForm.cs                    Main window (grid, sections, search, save)
├─ SettingSchema.cs               Schema: type + limited options per key
├─ TypedValueColumn.cs            Typed editor cell (drop-down, checkbox, spinner, colour)
├─ ValueRules.cs                  Strict validation of values
├─ HelpStore.cs                   Reads cumulusutils-help.ini
├─ HelpDialog.cs                  The window behind the (i) button
├─ LongTextDialog.cs              Large window for long values
├─ SettingRow.cs                  Model for one setting in the grid
├─ ValueClassifier.cs             Fallback recognition from the value
├─ IniFile.cs                     Parser + writer for the .ini file
└─ cumulusutils-help.ini          Help texts (copy next to the executable)
```

## Building

Requires the **.NET 10 SDK** (or newer) on Windows.

```bash
dotnet build -c Release
dotnet run
```

Or open `CumulusUtilsIniEditor.csproj` in **Visual Studio 2022** and press F5.
The result lands in `bin\Release\net10.0-windows\CumulusUtilsIniEditor.exe`.

**Copy `cumulusutils-help.ini` next to the executable** (the build copies it for
you) — without it the (i) buttons stay disabled.

## Use

1. Start the program and choose **Open…** (`Ctrl+O`).
2. Browse to your `cumulusutils.ini` and open it.
3. Choose a section on the left, or leave *All sections* and use the search box.
4. Change values with the control you are given: checkbox, drop-down, spinner or
   text box. Double-click long values for the large window. Click **(i)** for
   help on a setting.
5. Click **Save** (`Ctrl+S`) — a `.bak` is made automatically.

## A note on the format

`cumulusutils.ini` is an ordinary INI file: sections in `[brackets]`, followed by
`key=value` lines. Lines starting with `;` are disabled examples; the program
leaves them alone and does not edit them. On save the file is written cleanly
(UTF-8 without BOM), preserving every key, value and comment line.
