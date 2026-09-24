using System.Globalization;

namespace CumulusUtilsIniEditor;

/// <summary>The kind of editor a setting gets.</summary>
public enum SettingType
{
    /// <summary>Free text (fallback).</summary>
    Text,
    /// <summary>true / false.</summary>
    Boolean,
    /// <summary>Whole number, optional min/max/step.</summary>
    Integer,
    /// <summary>Decimal number, optional min/max/step.</summary>
    Decimal,
    /// <summary>Fixed list of allowed values → drop-down.</summary>
    Choice,
    /// <summary>CSS / hex colour → colour picker + free text.</summary>
    Colour,
    /// <summary>Long single-line value (HTML, URL) → opens the large editor.</summary>
    LongText,
    /// <summary>Numerical comma-separated list, e.g. "1,2,3,4".</summary>
    CommaList,
    /// <summary>Displayed but not editable (e.g. DoneToday).</summary>
    ReadOnly
}

/// <summary>
/// Everything the UI needs to know about one setting: its type, and — for
/// <see cref="SettingType.Choice"/> — the limited set of allowed values.
/// </summary>
public sealed class SettingSpec
{
    public SettingType Type { get; init; } = SettingType.Text;

    /// <summary>Allowed values for a Choice; also seeds the drop-down for Text (editable).</summary>
    public IReadOnlyList<string>? Options { get; init; }

    /// <summary>When true the drop-down allows values outside <see cref="Options"/>.</summary>
    public bool AllowCustomValue { get; init; }

    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public decimal? Step { get; init; }

    /// <summary>Short hint shown in the tooltip.</summary>
    public string? Help { get; init; }

    // ---- factory helpers, so the schema table stays readable ----------------

    public static SettingSpec Text(string? help = null, string[]? suggestions = null) =>
        new() { Type = suggestions is { Length: > 0 } ? SettingType.Choice : SettingType.Text,
                Options = suggestions, AllowCustomValue = true, Help = help };

    /// <summary>Numerical comma-separated list, e.g. ExtraTemp=1,2,3,4.</summary>
    public static SettingSpec List(string? help = null) =>
        new() { Type = SettingType.CommaList, Help = help };

    /// <summary>Shown but not editable — CumulusUtils maintains it itself.</summary>
    public static SettingSpec ReadOnly(string? help = null) =>
        new() { Type = SettingType.ReadOnly, Help = help };

    public static SettingSpec Long(string? help = null) =>
        new() { Type = SettingType.LongText, Help = help };

    public static SettingSpec Bool(string? help = null) =>
        new() { Type = SettingType.Boolean, Help = help };

    public static SettingSpec Int(decimal? min = null, decimal? max = null, decimal? step = 1, string? help = null) =>
        new() { Type = SettingType.Integer, Min = min, Max = max, Step = step, Help = help };

    public static SettingSpec Dec(decimal? min = null, decimal? max = null, decimal? step = null, string? help = null) =>
        new() { Type = SettingType.Decimal, Min = min, Max = max, Step = step, Help = help };

    /// <summary>A closed list — the user can only pick one of these.</summary>
    public static SettingSpec Pick(string[] options, string? help = null) =>
        new() { Type = SettingType.Choice, Options = options, AllowCustomValue = false, Help = help };

    /// <summary>An open list — suggested values, but typing anything is allowed.</summary>
    public static SettingSpec Suggest(string[] options, string? help = null) =>
        new() { Type = SettingType.Choice, Options = options, AllowCustomValue = true, Help = help };

    /// <summary>A directory / file path.</summary>
    public static SettingSpec Path(string? help = null) =>
        new() { Type = SettingType.Text, Help = help };

    /// <summary>A URL.</summary>
    public static SettingSpec Url(string? help = null) =>
        new() { Type = SettingType.Text, Help = help };

    public static SettingSpec Colour(string? help = null) =>
        new() { Type = SettingType.Colour, Help = help };
}

/// <summary>
/// The schema for cumulusutils.ini: which editor each key gets, and — where the
/// program defines a fixed set of choices — the allowed values.
///
/// Everything not listed here falls back to <see cref="ValueClassifier"/>,
/// so keys added by a newer CumulusUtils keep working without code changes.
/// </summary>
public static class SettingSchema
{
    private static readonly Dictionary<string, SettingSpec> Exact =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<(string Fragment, SettingSpec Spec)> ByFragment = new();

    static SettingSchema()
    {
        Build();
    }

    public static SettingSpec Resolve(string section, string key, string currentValue)
    {
        // 1. Exact match on "Section/Key" wins.
        if (Exact.TryGetValue($"{section}/{key}", out var bySection))
            return bySection;

        // 2. Exact match on bare key.
        if (Exact.TryGetValue(key, out var byKey))
            return byKey;

        // 3. Key-name fragment match (e.g. any "…Color…").
        foreach (var (fragment, spec) in ByFragment)
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return spec;

        // 4. Value-based fallback.
        return ValueClassifier.Guess(key, currentValue);
    }

    private static void Add(string key, SettingSpec spec) => Exact[key] = spec;

    private static void Add(string section, string key, SettingSpec spec) =>
        Exact[$"{section}/{key}"] = spec;

    private static void Fragment(string fragment, SettingSpec spec) =>
        ByFragment.Add((fragment, spec));

    private static void Build()
    {
        // ------------------------------------------------------------ General
        Add("LoggingOn", SettingSpec.Bool());
        Add("NormalMessageToConsole", SettingSpec.Bool());
        Add("TraceInfoLevel", SettingSpec.Pick(new[] { "error", "warning", "info", "debug", "trace" }));
        Add("ChartContainerHeight", SettingSpec.Int(100, 5000, 10));
        Add("ChartBackgroundImage", SettingSpec.Long("Pad of URL naar een achtergrondafbeelding"));
        Add("IgnoreDataErrors", SettingSpec.Bool());
        Add("GeneratejQueryInclude", SettingSpec.Bool());
        Add("DoLibraryIncludes", SettingSpec.Bool());
        Add("Language", SettingSpec.Pick(new[]
        {
            "en-GB", "en-US", "nl-NL", "de-DE", "fr-FR", "es-ES", "it-IT",
            "da-DK", "sv-SE", "no-NO", "pl-PL", "pt-PT", "cs-CZ", "hu-HU"
        }, "Taal van de gegenereerde website"));
        Add("UseSQL", SettingSpec.Bool());
        Add("UseScrollableTables", SettingSpec.Bool());
        Add("RecordsBeganDate", SettingSpec.Text("Datum als dd/MM/jj"));
        Add("MaxErrors", SettingSpec.Int(1, 1000, 1));
        Add("CMXport", SettingSpec.Int(1, 65535, 1, "Poort van CumulusMX"));
        Add("UseSpecificHighchartsVersion", SettingSpec.Suggest(
            new[] { "11.4.8", "11.4.7", "10.3.3", "9.3.3" }, "Leeg = meegeleverde versie"));
        Add("DoModular", SettingSpec.Bool());
        Add("ModulePath", SettingSpec.Path());
        Add("NeedSolarEnergyDailyValuesInCSV", SettingSpec.Bool());
        Add("CheckDateOrder", SettingSpec.Bool());
        Add("ConnectNulls", SettingSpec.Bool());
        Add("ParamCleanUp", SettingSpec.Bool());
        Add("LastUploadTime", SettingSpec.ReadOnly("Maintained automatically by CumulusUtils"));

        // ------------------------------------------------------------- pwsFWI
        Add("Analyse", SettingSpec.Int(1, 365, 1, "Aantal dagen voor de analyse"));
        Add("WarningLevel", SettingSpec.Int(1, 10, 1));
        Add("ResultFormat", SettingSpec.Pick(new[] { "beteljuice", "standard" }));
        Add("FireImage", SettingSpec.Bool());
        Add("PredictionBackground", SettingSpec.Colour());
        Add("CurrentIndexFormat", SettingSpec.Pick(new[] { "Standard", "Betel-Kocher" }));
        Add("CurrentPwsFWI", SettingSpec.Long("HTML-fragment voor de weergave"));
        Add("CurrentIndexDay", SettingSpec.Pick(new[] { "today", "yesterday" }));

        // ------------------------------------------------------------- Graphs
        Add("UseHighchartsBoostModule", SettingSpec.Bool());
        Add("UseNormalTempReference", SettingSpec.Pick(new[] { "both", "normal", "station", "none" }));
        Add("UseNormalRainReference", SettingSpec.Pick(new[] { "both", "normal", "station", "none" }));
        Add("MaxNrOfSeriesVisibileInGraph", SettingSpec.Int(1, 50, 1));
        Add("PeriodMovingAverage", SettingSpec.Int(0, 3650, 1));
        Add("WindRoseNrOfWindforceClasses", SettingSpec.Int(2, 16, 1));
        Add("WindRoseMaxWindSpeed", SettingSpec.Int(1, 200, 1));
        Add("WindRoseInversed", SettingSpec.Bool());
        Add("WindrunClassWidth", SettingSpec.Int(1, 1000, 1));
        Add("HeatmapNumberOfYearsPerPage", SettingSpec.Int(1, 50, 1));
        Add("GrowingDegreeDaysReferenceTemp", SettingSpec.Dec(-10, 40, 0.5m));
        Add("WinterToSpringTemperatureLimit", SettingSpec.Dec(-20, 40, 0.5m));
        Add("SpringToSummerTemperatureLimit", SettingSpec.Dec(-20, 50, 0.5m));

        // ------------------------------------------------------------ FTP site
        Add("FTP site", "DoUploadFTP", SettingSpec.Bool());
        Add("FTP site", "UploadDir", SettingSpec.Path());
        Add("FTP site", "FtpLog", SettingSpec.Pick(new[] { "off", "on", "error", "info" }));
        Add("FTP site", "MaxConcurrentUploads", SettingSpec.Int(1, 100, 1, "Leeg = standaard"));
        Add("FTP site", "delayMilliSeconds", SettingSpec.Int(0, 60000, 10));

        // --------------------------------------------------------- Thrifty
        Add("Top10RecordsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));
        Add("RainGraphsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));
        Add("TempGraphsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));
        Add("WindGraphsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));
        Add("MiscGraphsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));
        Add("MapsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));
        Add("SolarGraphsPeriod", SettingSpec.Int(0, 3650, 1, "Dagen"));

        // -------------------------------------------------------- StationMap
        Add("StationMapMenu", SettingSpec.Bool());
        Add("Zoomlevel", SettingSpec.Int(1, 20, 1));
        Add("CompassRosePosition", SettingSpec.Pick(new[]
        {
            "topleft", "topright", "bottomleft", "bottomright"
        }));
        Add("CompassRoseType", SettingSpec.Pick(new[] { "1", "2", "3" }));
        Add("WindArrowType", SettingSpec.Pick(new[] { "1", "2" }));
        Add("ArrowLatitude", SettingSpec.Dec(-90, 90, 0.0001m));
        Add("ArrowLongitude", SettingSpec.Dec(-180, 180, 0.0001m));

        // ----------------------------------------------------------- Website
        Add("StatisticsType", SettingSpec.Pick(new[] { "None", "Google", "Matomo" }));
        Add("GoogleStatsId", SettingSpec.Text("Bijv. G-XXXXXXXXXX"));
        Add("MatomoTrackerUrl", SettingSpec.Url());
        Add("MatomoSiteId", SettingSpec.Text());
        Add("PermitGoogleOptout", SettingSpec.Bool());
        Add("CumulusRealTimeLocation", SettingSpec.Url());
        Add("ShowInsideMeasurements", SettingSpec.Bool());
        Add("CumulusRealTimeInterval", SettingSpec.Int(1, 3600, 1, "Seconden"));
        Add("PwsfwiButtonInHeader", SettingSpec.Bool());
        Add("ShowUV", SettingSpec.Bool());
        Add("ShowSolar", SettingSpec.Bool());
        Add("HeaderLeftText", SettingSpec.Long("HTML-fragment"));
        Add("HeaderRightText", SettingSpec.Long("HTML-fragment"));
        Add("SiteTitleAddition", SettingSpec.Text());
        Add("UseCMXMoonImage", SettingSpec.Bool());
        Add("MoonImageLocation", SettingSpec.Url());
        Add("FooterCenterText", SettingSpec.Long("HTML-fragment"));

        Add("SteelseriesFramedesign", SettingSpec.Pick(new[]
        {
            "SHINY_METAL", "BLACK_METAL", "FLAT", "STANDARD"
        }));
        Add("SteelseriesBackgroundColor", SettingSpec.Colour());
        Add("SteelseriesPointerColour", SettingSpec.Colour());
        Add("SteelseriesPointerType", SettingSpec.Pick(new[] { "type1", "type2", "type3", "type4", "type5" }));
        Add("SteelseriesDirAvgPointertype", SettingSpec.Pick(new[] { "TYPE1", "TYPE2", "TYPE3", "TYPE4", "TYPE5" }));
        Add("SteelseriesDirAvgPointerColour", SettingSpec.Colour());
        Add("SteelseriesLcdColour", SettingSpec.Colour());
        Add("SteelseriesForegroundType", SettingSpec.Pick(new[] { "type1", "type2", "type3" }));
        Add("SteelseriesKnobType", SettingSpec.Pick(new[]
        {
            "STANDARD_KNOB", "METAL_KNOB", "BLACK_KNOB"
        }));
        Add("SteelseriesKnobStyle", SettingSpec.Colour());
        Add("SteelseriesRainUseSectionColours", SettingSpec.Bool());
        Add("SteelseriesRainUseGradientColours", SettingSpec.Bool());
        Add("SteelseriesTempThresholdValue", SettingSpec.Int(-50, 100, 1));
        Add("SteelseriesLedVisible", SettingSpec.Bool());

        foreach (var sensor in new[] { "Temp", "Humidity", "Wind", "Rain", "RRate", "UV" })
        {
            Add($"Threshold{sensor}Visible", SettingSpec.Bool());
            Add($"Threshold{sensor}Value", SettingSpec.Dec(0, 500, 0.1m));
        }

        Add("Panel-1", SettingSpec.Pick(PanelChoices));   // seed one; all Panels share the list below

        // Panel-1 … Panel-24 all offer the same widget names.
        for (int i = 1; i <= 24; i++)
            Add($"Panel-{i}", SettingSpec.Pick(PanelChoices));

        // ------------------------------------------------------------ SysInfo
        Add("ReportWidth", SettingSpec.Int(200, 3000, 10, "Pixels"));
        Add("SystemInfoLinesToSkip", SettingSpec.Text("Komma-gescheiden regelnummers"));
        Add("Tx", SettingSpec.Int(1, 20, 1));
        Add("ExtraStationInfo", SettingSpec.Text());
        Add("SystemInfoMenu", SettingSpec.Bool());

        // ----------------------------------------------------------- MeteoCam
        Add("MeteoCamMenu", SettingSpec.Bool());
        Add("MeteoCamDir", SettingSpec.Path());
        Add("TimelapseExtension", SettingSpec.Pick(new[] { "mp4", "webm", "gif" }));
        Add("MeteoCamName", SettingSpec.Text());
        Add("CamType", SettingSpec.Pick(new[] { "None", "Generic", "EcowittHP10" }));
        Add("FontSize", SettingSpec.Int(6, 200, 1));
        Add("FontWeight", SettingSpec.Pick(new[] { "normal", "bold", "lighter", "bolder" }));
        Add("BottomOffset", SettingSpec.Int(0, 2000, 1));
        Add("BlockLeftOrRight", SettingSpec.Pick(new[] { "left", "right" }));
        Add("BorderOffset", SettingSpec.Int(0, 2000, 1));
        Add("TextAlign", SettingSpec.Pick(new[] { "left", "center", "right" }));
        Add("TextColor", SettingSpec.Colour());
        Add("WantToSeeLines", SettingSpec.Text("Letters, bijv. WTPHR"));
        Add("AdditionalViewerCSS", SettingSpec.Long("CSS"));

        // ------------------------------------------------------------ AirLink
        Add("CountrySelected", SettingSpec.Pick(new[]
        {
            "EU", "US", "CA", "AU", "GB", "DE", "NL", "FR", "BE"
        }));
        Add("WantToSeeNow", SettingSpec.Bool());
        Add("WantToSeeNowCast", SettingSpec.Bool());
        Add("WantToSee1hr", SettingSpec.Bool());
        Add("WantToSee3hr", SettingSpec.Bool());
        Add("WantToSee24hr", SettingSpec.Bool());
        Add("WantToSeeWind", SettingSpec.Bool());
        Add("CleanupAirlinkLogs", SettingSpec.Bool());
        Add("StandAloneModule", SettingSpec.Bool());
        Add("ReferenceLineThickness", SettingSpec.Int(1, 10, 1));

        // ------------------------------------------------------- ExtraSensors
        Add("ExtraSensors", SettingSpec.Bool());
        Add("ParticipatesSensorCommunity", SettingSpec.Bool());
        Add("CleanupExtraSensorslog", SettingSpec.Bool());
        Add("UserModificationExtraSensorCharts", SettingSpec.Bool());

        // Numerieke komm-lijsten met kanaalnummers, bijv. "1,2,3,4".
        Add("ExternalExtraSensors", SettingSpec.List("Comma-separated channel numbers, e.g. 1,2,3"));
        Add("ExtraTemp", SettingSpec.List("Comma-separated channel numbers, e.g. 1,2,3,4"));
        Add("ExtraHum", SettingSpec.List("Comma-separated channel numbers, e.g. 1,2,3,4"));
        Add("ExtraDP", SettingSpec.List("Comma-separated channel numbers, e.g. 1,2,3,4"));
        Add("SoilTemp", SettingSpec.List("Comma-separated channel numbers"));
        Add("SoilMoisture", SettingSpec.List("Comma-separated channel numbers"));
        Add("AirQuality", SettingSpec.List("Comma-separated channel numbers (Ecowitt WH41)"));
        Add("SoilTemperature", SettingSpec.List("Comma-separated channel numbers"));
        Add("UserTemp", SettingSpec.List("Comma-separated channel numbers"));
        Add("LeafWetness", SettingSpec.List("Comma-separated channel numbers"));
        Add("LeafTemp", SettingSpec.List("Comma-separated channel numbers"));
        Add("CO2", SettingSpec.List("Comma-separated channel numbers"));
        Add("LaserDist", SettingSpec.List("Comma-separated channel numbers"));
        Add("LaserDepth", SettingSpec.List("Comma-separated channel numbers"));

        // Deze is wél een gewone schakelaar.
        Add("LightningSensor", SettingSpec.Bool());

        // ------------------------------------------------------- kleurlijsten
        // Een bracketed lijst met kleurcodes, geen enkele kleur — dus lange tekst.
        Add("GraphColors", SettingSpec.Long("Bracketed list of quoted colours, see the example line"));
        Add("WindRoseColors", SettingSpec.Long("Bracketed list of quoted colours, see the example line"));

        // ------------------------------------------------------------- paden
        Add("NOAA", "FTPDirectory", SettingSpec.Path("Folder on the site for the NOAA reports"));

        // --------------------------------------------------------- CustomLogs
        Add("CustomLogs", SettingSpec.Bool());
        Add("UserModificationCustomLogsCharts", SettingSpec.Bool());
        Add("ExcludedCustomLogs", SettingSpec.Text("Komma-gescheiden"));

        // ------------------------------------------------------------- Diary
        Add("Diary", SettingSpec.Bool());
        Add("ColorDiaryText", SettingSpec.Colour());
        Add("ColorDiaryBackground", SettingSpec.Colour());
        Add("ColorDiaryChartSnowDepth", SettingSpec.Colour());
        Add("ColorDiaryChartSnow24h", SettingSpec.Colour());

        // --------------------------------------------------------- Forecasts
        Add("ForecastSystem", SettingSpec.Pick(new[]
        {
            "CUtils", "None", "WU", "YrNo", "MetOffice", "Wunderground"
        }));

        // ---------------------------------------------- door CUtils bijgehouden
        // Deze krijgen in elke sectie waar ze voorkomen het ReadOnly-type.
        foreach (var section in new[] { "Maps", "Compiler", "CustomLogs" })
            Add(section, "DoneToday", SettingSpec.ReadOnly("Maintained automatically by CumulusUtils"));

        Add("SmaPeriod", SettingSpec.Int(0, 3650, 1, "Days"));

        // ------------------------------------------------------------- Maps
        Add("Website", SettingSpec.Url());
        Add("Participant", SettingSpec.Bool());

        // ----------------------------------------------------- resterende vaste sleutels
        // Deze ontbraken en vielen terug op de waardegok; nu expliciet.
        Add("Top10", "NumberOfColumns", SettingSpec.Int(1, 12, 1, "Aantal kolommen van de Top 10-tabel"));
        Add("NOAA", "StartInCurrentMonth", SettingSpec.Bool("Open het NOAA-rapport op de huidige maand"));
        Add("FTP site", "DoUploadFTP", SettingSpec.Bool());
        Add("FTP site", "delayMilliSeconds", SettingSpec.Int(0, 60000, 10));

        // Threshold-schakelaars: de fragmentregel hieronder maakt van elke
        // "Threshold…"-sleutel een decimaal, ook van de …Visible-schakelaars.
        // Die zetten we expliciet, net als de bijbehorende waarden.
        foreach (var sensor in new[] { "Temp", "Humidity", "Wind", "Rain", "RRate", "UV" })
        {
            Add($"Threshold{sensor}Visible", SettingSpec.Bool());
            Add($"Threshold{sensor}Value", SettingSpec.Dec(0, 500, 0.1m));
        }

        // -------------------------------------------------- fragment matches
        // Fragmenten vangen hele families in één keer: alle kleursleutels, alle
        // periode-instellingen. Ze staan ná de expliciete regels, zodat een
        // specifieke sleutel altijd wint.
        Fragment("Color", SettingSpec.Colour());
        Fragment("Colour", SettingSpec.Colour());
        Fragment("Background", SettingSpec.Colour());
        Fragment("Period", SettingSpec.Int(0, 3650, 1, "Days"));

        Fragment("Minute", SettingSpec.Int());
        Fragment("Value", SettingSpec.Dec(0, 500, 0.1m));

        // -------------------------------------------------- schakelaars
        // Alles hier is true/false, ook wanneer de naam op een fragment lijkt
        // (MonthlyTemp, TempSum, Threshold…Visible).
        foreach (var key in new[]
                 {
                     "MonthlyTemp", "YearTempstats", "YearMonthTempstats", "TempSum",
                     "DailyRain", "MonthlyRain", "YearRainstats", "YearMonthRainstats",
                     "WarmerDays", "HeatMap", "WindRose", "Windrun",
                     "SolarHours", "SolarEnergy", "SolarHoursYearMonth", "SolarEnergyYearMonth",
                     "GrowingDegreeDays", "Seasons", "DailyEVT", "MonthlyEVT",
                     "AverageClash", "EVTvsRAIN", "RAINvsEVT", "FrostDays"
                 })
            Add(key, SettingSpec.Bool());
    }

    /// <summary>Widget names usable in the Panel-1 … Panel-24 dashboard slots.</summary>
    public static readonly string[] PanelChoices =
    {
        "TemperatureText", "PressureText", "RainText", "WindText", "HumidityText",
        "SolarText", "SolarDisc", "LunarDisc", "Clocks",
        "TemperatureGauge", "OtherTempsGauge", "PressureGauge", "HumidityGauge",
        "WindGauge1", "WindGauge2",
        "WindDirGauge1", "WindDirGauge2",
        "WindRoseGauge1", "WindRoseGauge2",
        "CloudBaseGauge", "RainGauge", "RainSpeedGauge", "SolarGauge", "UVGauge"
    };
}
