/*
 * CuSupport - Part of CumulusUtils
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CumulusUtils
{
    #region CuSupport

    public class CuSupport : IDisposable
    {
        // Is it a version number beta shown at users?
        private const string beta = ".NET 10 beta1";

        private static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
        private static readonly Regex WhitespaceRegex = new Regex( @"\s+", RegexOptions.Compiled );

        #region declarations
        public Wind StationWind { get; set; }
        public Pressure StationPressure { get; set; }
        public Rain StationRain { get; set; }
        public Temp StationTemp { get; set; }
        public Distance StationDistance { get; set; }
        public LaserDist StationLaser { get; set; }
        public Height StationHeight { get; set; }
        public string PerHour { get; set; }

        public string PathUtils { get; } = "utils/";
        public string PathUtilsLog { get; } = "utils/utilslog/";

        public string CUlibOutputFilename { get; } = "cumulusutils.js";
        public string PwsFWIOutputFilename { get; } = "pwsFWI.txt";
        public string PwsFWICurrentOutputFilename { get; } = "pwsFWIcurrent.txt";
        public string GraphsRainOutputFilename { get; } = "graphsrain.txt";
        public string GraphsTempOutputFilename { get; } = "graphstemp.txt";
        public string GraphsWindOutputFilename { get; } = "graphswind.txt";
        public string GraphsSolarOutputFilename { get; } = "graphssolar.txt";
        public string GraphsMiscOutputFilename { get; } = "graphsmisc.txt";
        public string Top10OutputFilename { get; } = "top10Table.txt";
        public string SysInfoOutputFilename { get; } = "systeminfoTable.txt";
        public string MapsOutputFilename { get; } = "maps.txt";
        public string RecordsOutputFilename { get; } = "records.txt";
        public string DayRecordsOutputFilename { get; } = "dayrecords.txt";
        public string NOAAOutputFilename { get; } = "noaa.txt";
        public string ForecastOutputFilename { get; } = "forecast.txt";
        public string StationMapOutputFilename { get; } = "stationmap.txt";
        public string MeteoCamOutputFilename { get; } = "meteocam.txt";
        public string MeteocamRealtimeFilename { get; } = "meteocamrealtime.txt";
        public string AirLinkOutputFilename { get; } = "airlink.txt";
        public string AirLinkStandaloneOutputFilename { get; } = "airlink.html";
        public string AirLinkRealtimeFilename { get; } = "airlinkrealtime.txt";
        public string AirlinkJSONpart { get; } = "airlinkdata";
        public string ExtraSensorsOutputFilename { get; } = "extrasensors.txt";
        public string ExtraSensorsRealtimeFilename { get; } = "extrasensorsrealtime.txt";
        public string ExtraSensorsCharts { get; } = "extrasensorscharts.txt";
        public string CustomLogsOutputFilename { get; } = "customlogs.txt";
        public string CustomLogsRealtimeFilename { get; } = "customlogsrealtime.txt";
        public string CustomLogsCharts { get; } = "customlogscharts.txt";
        public string SensorCommunityOutputFilename { get; } = "sensorcommunity.txt";
        public string DiaryOutputFilename { get; } = "diary.txt";
        public string CUserdataRECENT { get; } = "CUserdataRECENT.json";
        public string CUserdataDAILY { get; } = "CUserdataDAILY.json";
        public string CUserdataALL { get; } = "CUserdataALL.json";
        public string ExtraSensorsJSON { get; } = "extrasensorsdata.json";
        public string CustomLogsRecentJSON { get; } = "customlogsRecentdata.json";
        public string CustomLogsDailyJSON { get; } = "customlogsDailydata.json";
        public string CutilsChartsDef { get; } = "CutilsCharts.def";
        public string CutilsMenuDef { get; } = "CutilsMenu.def";
        public string CutilsHeadDef { get; } = "CutilsHead.def";
        public string CUhelptexts { get; } = "CUhelptexts.txt";

        public string IndexOutputFilename { get; } = "index.html";

        public bool LoggingOn { get; set; }
        public TraceSwitch CUTraceSwitch { get; set; }

        public string DemarcationLineExtraSensors { get; } = "; ExtraSensorCharts";
        public string DemarcationLineCustomLogs { get; } = "; CustomLogsCharts";

        private readonly IniFile Ini;           // that is: Cumulus.ini
        private readonly IniFile AtrIni;        // that is: data/alltime.ini
        private readonly IniFile StringsIni;    // that is: strings.ini
        private readonly IniFile MyIni;         // that is: cumulusutils.ini
        private readonly IniFile CUstringIni;   // that is: CUstrings.ini

        public string Locale { get; private set; }
        public string Language { get; private set; }
        public string Country { get; private set; }

        #endregion

        #region Initialisation
        public CuSupport()
        {
            if ( !File.Exists( "Cumulus.ini" ) )
            {
                Console.WriteLine( " No Cumulus.ini found. Must run in Cumulus directory!" );
                Environment.Exit( 0 );
            }
            else
            {
                const string filenameCopy = "copy_Cumulus.ini";
                File.Copy( "Cumulus.ini", filenameCopy, overwrite: true );
                Ini = new IniFile( filenameCopy, this );
            }

            if ( !File.Exists( "cumulusutils.ini" ) )
            {
                // All entries will be created when called for because I changed the IniFiles library. Search for: HAR
                File.WriteAllText( "cumulusutils.ini", string.Empty );
            }

            MyIni = new IniFile( "cumulusutils.ini", this );

            // Get AlltimeRecords ready for the possible Axis ranges of the graphs
            if ( File.Exists( "data/alltime.ini" ) )
            {
                AtrIni = new IniFile( "data/alltime.ini", this );
            }

            // We need strings.ini entries for the ExtraSensor module
            if ( !File.Exists( "strings.ini" ) && File.Exists( "samplestrings.ini" ) )
                File.Copy( "samplestrings.ini", "strings.ini" );
            if ( File.Exists( "strings.ini" ) )
                StringsIni = new IniFile( "strings.ini", this );

            InitLogging();

            // Do the locale thing
            // See https://docs.microsoft.com/en-gb/openspecs/windows_protocols/ms-lcid/70feba9f-294e-491e-b6eb-56532684c37f
            // Using only ISO two letter codes

            Locale = GetUtilsIniValue( "General", "Language", "en-GB" );

            try
            {
                ApplyLocale( Locale );
            }
            catch ( Exception e ) when ( e is CultureNotFoundException || e is ArgumentException )
            {
                LogDebugMessage( $" Invalid Locale : {Locale}" );
                LogMessage( $" Invalid Culture : {e.Message}", TraceLevel.Error );
                LogMessage( $" Invalid Culture for the system : {Locale}", TraceLevel.Error );
                LogMessage( " Using English GB locale : en-GB", TraceLevel.Warning );
                ApplyLocale( "en-GB" );
            }
            catch ( Exception e )
            {
                LogDebugMessage( $" Unknown exception - Invalid Culture : {e.Message}" );
                throw;
            }

            if ( !CUtils.Thrifty )
            {
                WriteHighchartsLanguageFile();
            }

            LogDebugMessage( $" CUstrings[xx].ini : CUstrings{Language}.ini looked for." );

            string cuStringsPath = $"CUstrings{Language}.ini";
            if ( !File.Exists( cuStringsPath ) )
            {
                LogMessage( $" No CUstrings{Language}.ini found.", TraceLevel.Warning );
                File.WriteAllText( cuStringsPath, string.Empty );
            }

            CUstringIni = new IniFile( cuStringsPath, this );

            PerHour = GetCUstringValue( "General", "PerHour", "/hr", false );

            WindDim windDim = ParseStationUnit<WindDim>( "WindUnit", "2" );
            StationWind = new Wind( windDim, this );
            StationDistance = new Distance( (DistanceDim) windDim ); // CMX has no Distance unit; reuse WindUnit

            StationPressure = new Pressure( ParseStationUnit<PressureDim>( "PressureUnit", "1" ) );
            StationRain = new Rain( ParseStationUnit<RainDim>( "RainUnit", "0" ) );
            StationTemp = new Temp( ParseStationUnit<TempDim>( "TempUnit", "0" ) );
            // CMX key is historically misspelled as LaserDistancehUnit
            StationLaser = new LaserDist( ParseStationUnit<LaserDim>( "LaserDistancehUnit", "0" ) );
            StationHeight = new Height( ParseStationUnit<HeightDim>( "CloudBaseInFeet", "0" ) );

            LogDebugMessage( $" CumulusUtils version: {UnformattedVersion()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Wind (m/s, mph, km/h, kts): {StationWind.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Pressure (mb,hPa,inHg): {StationPressure.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Rain (mm,in): {StationRain.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit T (C,F): {StationTemp.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Distance (m, mi, km, kn): {StationDistance.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Laser (cm, in): {StationLaser.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Height (m, ft): {StationHeight.Text()}" );
        }

        private void ApplyLocale( string locale )
        {
            Locale = locale;
            string[] parts = locale.Split( '-', '_' );
            Language = parts.Length > 0 && parts[ 0 ].Length >= 2
                ? parts[ 0 ][ ..2 ].ToUpper( CUtils.Inv )
                : "EN";
            Country = parts.Length > 1 && parts[ 1 ].Length >= 2
                ? parts[ 1 ][ ..2 ].ToUpper( CUtils.Inv )
                : "GB";

            CUtils.ThisCulture = CultureInfo.GetCultureInfo( locale );
        }

        private void WriteHighchartsLanguageFile()
        {
            using StreamWriter of = new StreamWriter( $"{PathUtils}HighchartsLanguage.js", false, Encoding.UTF8 );
            // This file is from version 8.0 also used for time/timezone defs.

            DateTimeFormatInfo dtf = CUtils.ThisCulture.DateTimeFormat;
            StringBuilder str = new StringBuilder();

            str.AppendLine( "Highcharts.lang = {" );
            str.AppendLine( "  lang:{" );

            str.Append( "    months:[" );
            str.Append( string.Join( ",", Enumerable.Range( 1, 12 ).Select( i => $"'{dtf.GetMonthName( i )}'" ) ) );
            str.AppendLine( "]," );

            str.Append( "    shortMonths:[" );
            str.Append( string.Join( ",", Enumerable.Range( 0, 12 ).Select( i => $"'{dtf.AbbreviatedMonthNames[ i ]}'" ) ) );
            str.AppendLine( "]," );

            str.Append( "    weekdays:[" );
            str.Append( string.Join( ",", Enumerable.Range( 0, 7 ).Select( i => $"'{dtf.DayNames[ i ]}'" ) ) );
            str.AppendLine( "]," );
            str.AppendLine( "    thousandsSep: \"\"" );
            str.AppendLine( "  }," );

            str.AppendLine( $"time:{{timezone: '{GetCumulusIniValue( "Station", "TimeZone", "" )}'}}" );
            str.AppendLine( "};" );

            of.WriteLine( $"{str}" );
            of.WriteLine( "highchartsOptions = Highcharts.setOptions(Highcharts.lang);" );
        }

        #endregion

        #region Methods INI
        public string GetCumulusIniValue( string section, string key, string def ) => Ini.GetValue( section, key, def );
        public string GetStringsIniValue( string section, string key, string def ) => StringsIni.GetValue( section, key, def );
        public string GetAlltimeRecordValue( string section, string key, string def ) => AtrIni.GetValue( section, key, def );

        public string GetUtilsIniValue( string section, string key, string def )
        {
            string tmp = MyIni.GetValue( section, key, def );

            if ( !string.IsNullOrEmpty( tmp ) && tmp.Contains( "<#" ) )
            {
                // Not empty AND contains a webtag — only then replace
                CmxIPC thisIPC = new CmxIPC( this, CUtils.Isup );

                Task<string> asyncTask = thisIPC.ReplaceWebtagsPostAsync( tmp );
                asyncTask.Wait();
                tmp = asyncTask.Result;
            }

            LogMessage( DateTime.Now + $" GetUtilsIniValue {key} / {tmp}", TraceLevel.Verbose );

            return tmp;
        }

        public void SetUtilsIniValue( string section, string key, string def ) => MyIni.SetValue( section, key, def );

        public string GetCUstringValue( string section, string key, string def, bool javaScript )
        {
            string tmp = CUstringIni.GetValue( section, key, def );

            if ( string.IsNullOrEmpty( tmp ) )
                return tmp;

            if ( javaScript && tmp.Contains( '\'' ) )
                tmp = tmp.Replace( "'", @"\'" );

            LogMessage( DateTime.Now + $" GetCUstringValue {key} / {tmp}", TraceLevel.Verbose );

            return tmp;
        }

        public void SetCUstringValue( string section, string key, string def ) => CUstringIni.SetValue( section, key, def );

        private T ParseStationUnit<T>( string key, string fallback ) where T : struct, Enum
        {
            string raw = GetCumulusIniValue( "Station", key, fallback );
            if ( int.TryParse( raw, NumberStyles.Integer, CUtils.Inv, out int n )
                && Enum.IsDefined( typeof( T ), n ) )
                return (T) (object) n;

            LogMessage( $" Invalid Station.{key} value '{raw}', using {fallback}.", TraceLevel.Warning );
            return (T) (object) int.Parse( fallback, CUtils.Inv );
        }

        private static void EndMyIniFile() { }

        #endregion

        #region Methods Highcharts

        public int HighChartsWindBarbSpacing()
        {
            // Make sure all windbarb charts have an acceptable and correct spacing:
            //   Note: using illegal unit numbers - like 5 - causes the chart not to display
            //   See: https://api.highcharts.com/highstock/plotOptions.series.dataGrouping.units
            //   ['hour',[1, 2, 3, 4, 6, 8, 12] ]
            if ( !int.TryParse( GetCumulusIniValue( "Graphs", "GraphHours", "0" ), NumberStyles.Integer, CUtils.Inv, out int hours ) )
                hours = 0;

            int tmp = hours / 24;
            return tmp <= 4 ? tmp : 6;
        }

        public string HighchartsAllowBackgroundImage( string one = "", string two = "" )
        {
            // The optional parameters one and two accommodate the AirLink module which charts the In/Out (one) and pm2p5/pm10 (two) combinations
            // All other calls to HighchartsAllowBackgroundImage will be without parameters.
            string s = GetUtilsIniValue( "General", "ChartBackgroundImage", "" );

            if ( !string.IsNullOrEmpty( s ) )
            {
                s = $"#chartcontainer{one}{two} {{background-image: url(\"{s}\"); }}";
                s += ".highcharts-background{fill: none;}";
            }

            return s;
        }

        #endregion

        #region ChartInfoFuncs

        public string ActivateChartInfo( string chartId )
        {
            chartId = "HT_" + chartId;

            if ( string.IsNullOrEmpty( CUtils.ChartHelp.GetHelpText( chartId ) ) )
            {
                return ""; // No helptext present so do nothing
            }

            StringBuilder tmp = new StringBuilder();

            string Info = $"{GetCUstringValue( "General", "Info", "Info", true )}";

            // See: https://stackoverflow.com/a/79749908/11931424

            tmp.AppendLine( "chart.update({" );
            tmp.AppendLine( "  chart:{events:{render() {const chart = this; if ( !chart.exporting.group ){return;}const { x, y, width } = chart.exporting.group.getBBox();" );

            tmp.AppendLine( "  if ( !this.customText ){" ); // Create a customText if it doesn't exist
            tmp.AppendLine( $"    this.customText = this.renderer.text( '{Info}', x - width - 15, y + 15 )" );
            tmp.AppendLine( "      .add()" +
                ".css({ color: this.title && this.title.styles ? this.title.styles.color : '#333', cursor: 'pointer' })" +
                $".on('click', () => $('#{chartId}').modal( 'show') );" );
            tmp.AppendLine( "  } else {" ); // Update the label position on render event (i.e on window resize)
            tmp.AppendLine( "    this.customText.attr({x: x - width - 15, y: y + 15}); } } } } });" );

            return tmp.ToString();
        }

        public string GenerateChartInfoModal( string chartId, string Title )
        {
            chartId = "HT_" + chartId;

            if ( string.IsNullOrEmpty( CUtils.ChartHelp.GetHelpText( chartId ) ) )
            {
                return ""; // No helptext present so do nothing
            }

            StringBuilder tmp = new StringBuilder();

            if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
            {
                // Use the jQuery modal, by setting the DoLibraryIncludes to false the user has control whether or not to use the
                // supplied includes or do it all by her/himself
                tmp.AppendLine(
                    $"<div class='modal' id='{chartId}' style='font-family: Verdana, Geneva, Tahoma, sans-serif;font-size: 120%;'>" +
                    "      <div>" +
                    $"        <h5 class='modal-title'>{Title}</h5>" +
                    "      </div>" +
                    "      <div style='text-align: left;'>" +
                    $"        {CUtils.ChartHelp.GetHelpText( chartId )}" +
                    "      </div>" +
                    "</div>" );
            }
            else
            {
                // Use the bootstrap modal --- tabindex='-1'
                tmp.AppendLine( $"<div class='modal fade' id='{chartId}' role='dialog' aria-hidden='true'>" +
                "  <div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" +
                "    <div class='modal-content'>" +
                "      <div class='modal-header'>" +
                $"        <h5 class='modal-title'>{Title}</h5>" +
                "        <button type='button' class='close' data-bs-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" +
                "      </div>" +
                "      <div class='modal-body text-start'>" +
                $"       {CUtils.ChartHelp.GetHelpText( chartId )}" +
                "      </div>" +
                "      <div class='modal-footer'>" +
                $"       <button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{GetCUstringValue( "Website", "Close", "Close", false )}</button>" +
                "      </div>" +
                "    </div>" +
                "  </div>" +
                "</div>" );
            }

            return tmp.ToString();
        }

        #endregion

        #region Methods Utilities

        public static string StringRemoveWhiteSpace( string InputWithSpaces, string ReplacementOfSpaces )
            => WhitespaceRegex.Replace( InputWithSpaces, ReplacementOfSpaces );

        public static string StationInUse( int i )
        {
            string[] StationDesc =
            {
                "Davis Vantage Pro",            // 0
                "Davis Vantage Pro2",           // 1
                "Oregon Scientific WMR-928",    // 2
                "Oregon Scientific WM-918",     // 3
                "EasyWeather",                  // 4
                "Fine Offset",                  // 5
                "LaCrosse WS2300",              // 6
                "Fine Offset with Solar",       // 7
                "Oregon Scientific WMR100",     // 8
                "Oregon Scientific WMR200",     // 9
                "Instromet",                    // 10
                "Davis WLL",                    // 11
                "GW1000",                       // 12
                "HTTP WUnderground",            // 13
                "HTTP Ecowitt",                 // 14
                "HTTP Ambient",                 // 15
                "WeatherFlow Tempest",          // 16
                "Simulator",                    // 17
                "Ecowitt Cloud",                // 18
                "Davis Cloud (WLL/WLC)",        // 19
                "Davis Cloud (VP2)",            // 20
                "JSON Data",                    // 21
                "Ecowitt HTTP API"              // 22
            };

            if ( i < 0 || i >= StationDesc.Length )
                return "Unknown Station";

            return StationDesc[ i ];
        }

        private static string AssemblyVersionCore()
        {
            Version v = typeof( CuSupport ).Assembly.GetName().Version;
            return $"{v.Major.ToString( CUtils.Inv )}.{v.Minor.ToString( CUtils.Inv )}.{v.Build.ToString( CUtils.Inv )}";
        }

        public static string FormattedVersion()
        {
            string _ver = AssemblyVersionCore();

            _ver = string.Format( CUtils.Inv, "<a href='https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=17998' target='_blank'>CumulusUtils</a> " +
                                  $"Version {_ver} " + beta +
                                  $" - generated at " + DateTime.Now.ToString( "g", CUtils.ThisCulture ) );

            return _ver;
        }

        public static string UnformattedVersion() => AssemblyVersionCore() + " " + beta;

        public static string Copyright() => "&copy; GNU GPL v3";

        public static StringBuilder CopyrightForGeneratedFiles()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine( "<!--" );
            result.AppendLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
            result.AppendLine( " This header must not be removed and the user must comply to the GNU General Public License" );
            result.AppendLine( " See also License conditions of CumulusUtils at https://meteo-wagenborgen.nl/" );
            result.AppendLine( "-->" );

            return result;
        }

        #endregion

        #region Methods Includes

        public static string GenjQueryIncludestring() => ( CUtils.DojQueryInclude && !CUtils.DoWebsite ) ?
                "<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/3.6.0/jquery.min.js\" type=\"text/javascript\"></script>" : "";

        public StringBuilder GenHighchartsIncludes()
        {
            StringBuilder sb = new StringBuilder();

            string SpecificHighchartsVersion = GetUtilsIniValue( "General", "UseSpecificHighchartsVersion", "12.3" );
            bool UseHighchartsBoostModule = GetUtilsIniValue( "Graphs", "UseHighchartsBoostModule", "true" ).Equals( "true", CUtils.Cmp );

            if ( !string.IsNullOrEmpty( SpecificHighchartsVersion ) )
            {
                SpecificHighchartsVersion += '/';
            }

            sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}highstock.js'></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}highcharts-more.js\"></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}indicators/indicators.js\"></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}modules/exporting.js\" ></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}modules/heatmap.js\"></script>" );
            sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}modules/windbarb.js'></script>" );
            sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}indicators/trendline.js'></script>" );
            sb.AppendLine( $"<script defer src='https://code.highcharts.com/{SpecificHighchartsVersion}modules/accessibility.js'></script>" );

            if ( UseHighchartsBoostModule )
                sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}/modules/boost.js\"></script>" );

            sb.AppendLine( "  <script src='lib/HighchartsLanguage.js'></script>" );
            sb.AppendLine( "  <script src='lib/HighchartsDefaults.js'></script>" );

            return sb;
        }

        public static StringBuilder GenLeafletIncludes()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine( "<link rel='stylesheet' href='https://unpkg.com/leaflet@1.5.1/dist/leaflet.css' integrity='sha512-xwE/Az9zrjBIphAcBb3F6JVqxf46+CDLwfLMHloNu6KEQCAWi6HcDUbeOfBIptF7tcCzusKFjFw2yuvEpDL9wQ==' crossorigin='' />" );
            sb.AppendLine( "<script src='https://unpkg.com/leaflet@1.5.1/dist/leaflet.js' integrity='sha512-GffPMF3RvMeYyc1LWMHtK8EbPv0iNZ8/oTtHPx9/cc2ILxQ+u905qIwdpULaqDkyBKgOaB57QTMg7ztg8Jm2Og==' crossorigin=''></script>" );

            return sb;
        }

        #endregion

        #region Methods Incremental Dates

        public bool DateIsToday( DateTime thisDate )
        {
            TimeSpan thisSpan = DateTime.Now - thisDate;

            LogMessage( $"DateIsToday for thisDate: {thisDate} | thisDate.DayOfYear: {thisDate.DayOfYear} versus Now.DayOfYear: {DateTime.Now.DayOfYear})", TraceLevel.Info );
            LogMessage( $"DateIsToday: thisSpan: {thisSpan} | thisSpan.TotalDays = {thisSpan.TotalDays}", TraceLevel.Info );

            return thisSpan.TotalDays <= 1;
        }

        public void SetStartAndEndForData( out DateTime Start, out DateTime End )
        {
            DateTime now = DateTime.Now;
            now = new DateTime( now.Year, now.Month, now.Day, now.Hour, now.Minute, 0 );

            End = now.AddMinutes( -now.Minute % Math.Max( CUtils.FTPIntervalInMinutes, CUtils.LogIntervalInMinutes ) );

            if ( CUtils.Isup.IsIncrementalAllowed() )
            {
                try
                {
                    Start = DateTime.ParseExact( GetUtilsIniValue( "General", "LastUploadTime", "" ), "dd/MM/yy HH:mm", CUtils.Inv ).AddMinutes( 1 );
                }
                catch
                {
                    Start = End.AddHours( -CUtils.HoursInGraph );
                }
            }
            else
            {
                Start = End.AddHours( -CUtils.HoursInGraph );
            }
        }

        #endregion

        #region Diagnostics

        TextWriterTraceListener ThisListener;
        bool NormalMessageToConsole;

        public void InitLogging()
        {
            CUTraceSwitch = new TraceSwitch( "CUTraceSwitch", "Tracing switch for CumulusUtils" )
            {
                Level = TraceLevel.Verbose
            };

            LoggingOn = GetUtilsIniValue( "General", "LoggingOn", "true" ).Equals( "true", CUtils.Cmp );
            NormalMessageToConsole = GetUtilsIniValue( "General", "NormalMessageToConsole", "true" ).Equals( "true", CUtils.Cmp );
            string thisTrace = GetUtilsIniValue( "General", "TraceInfoLevel", "Info" );     // Verbose, Information, Warning, Error, Off

            LogMessage( $"Initial {CUTraceSwitch} => Error: {CUTraceSwitch.TraceError}, Warning: {CUTraceSwitch.TraceWarning}, Info: {CUTraceSwitch.TraceInfo}, Verbose: {CUTraceSwitch.TraceVerbose}" );

            try
            {
                CUTraceSwitch.Level = (TraceLevel) Enum.Parse( typeof( TraceLevel ), thisTrace, true );
            }
            catch ( Exception e ) when ( e is ArgumentException || e is ArgumentNullException )
            {
                LogMessage( $"Initial: Exception parsing the TraceLevel - {e.Message}", TraceLevel.Error );
                LogMessage( "Initial: Setting level to Warning.", TraceLevel.Warning );
                CUTraceSwitch.Level = TraceLevel.Warning;
            }

            if ( LoggingOn )
            {
                ThisListener = new TextWriterTraceListener( $"utils/utilslog/{DateTime.Now.ToString( "yyMMddHHmm", CUtils.Inv )}cumulusutils.log" );
                Trace.Listeners.Add( ThisListener );
                Trace.AutoFlush = true;
            }

            LogMessage( $"According to Inifile {thisTrace} => Error: {CUTraceSwitch.TraceError}, Warning: {CUTraceSwitch.TraceWarning}, Info: {CUTraceSwitch.TraceInfo}, Verbose: {CUTraceSwitch.TraceVerbose}, ", TraceLevel.Info );

            if ( Environment.OSVersion.Platform.Equals( PlatformID.Unix ) )
            {
                LogDebugMessage( "CumulusUtils Initial: Shutting down the default listener" );
                LogMessage( "CumulusUtils Initial: Shutting down the default listener", TraceLevel.Info );
                if ( Trace.Listeners.Count > 0 )
                    Trace.Listeners.RemoveAt( 0 );
            }
        }

        public void LogDebugMessage( string message )
        {
            string stamp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " );
            if ( NormalMessageToConsole ) Console.WriteLine( stamp + message );
            if ( LoggingOn ) Debug.WriteLine( stamp + message );
        }

        public void LogMessage( string message, TraceLevel level = TraceLevel.Info )
        {
            if ( CUTraceSwitch.Level >= level )
            {
                string prefix = level switch
                {
                    TraceLevel.Error => "❌ Error :",
                    TraceLevel.Warning => "Warning :",
                    TraceLevel.Info => "Information :",
                    TraceLevel.Verbose => "Verbose :",
                    _ => ""
                };

                Trace.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + prefix + message );
            }
        }

        #endregion

        #region Javascript / Unix time conversions

        public static long DateTimeToJS( DateTime timestamp ) => (long) ( timestamp - UnixEpoch ).TotalSeconds * 1000;
        public static long DateTimeToUnix( DateTime timestamp ) => (long) ( timestamp - UnixEpoch ).TotalSeconds;
        public static long DateTimeToJSUTC( DateTime timestamp ) => (long) ( timestamp.ToUniversalTime() - UnixEpoch ).TotalSeconds * 1000;
        public static long DateTimeToUnixUTC( DateTime timestamp ) => (long) ( timestamp.ToUniversalTime() - UnixEpoch ).TotalSeconds;
        public static DateTime UnixTimestampToDateTime( string unixTime ) => UnixEpoch.AddSeconds( Convert.ToInt64( unixTime ) ).ToLocalTime();

        #endregion

        #region Upload Package

        private static readonly string[] Package =
        [
            "CUgauges.js", "HighchartsDefaults.js", "HighchartsLanguage.js",
            "suncalc.js", "CUtween.min.js", "CUsteelseries.min.js", "CURGraph.rose.js",
            "CURGraph.common.core.js", "CUlanguage.js", "CUgauges-ss.css"
        ];

        public async Task<bool> CheckPackageAndCopy()
        {
            foreach ( string file in Package )
            {
                string filename = PathUtils + file;

                if ( !File.Exists( filename ) )
                {
                    LogMessage( $"CheckPackageAndCopy: File {filename} is missing.", TraceLevel.Info );
                    LogMessage( "CheckPackageAndCopy: Website may not be [fully] operational but file may still exist from previous installation.", TraceLevel.Info );
                    continue;
                }

                string FTPfilename = Path.GetExtension( filename ) switch
                {
                    ".txt" => file,
                    ".js" => "lib/" + file,
                    ".css" => "css/" + file,
                    _ => ""
                };

                if ( string.IsNullOrEmpty( FTPfilename ) )
                {
                    LogMessage( "CheckPackageAndCopy: File (IsNullOrEmpty) can't be copied. Cancelling operation.", TraceLevel.Warning );
                    LogMessage( "CheckPackageAndCopy: Website not created/updated. Website may not be [fully] operational", TraceLevel.Warning );
                    LogMessage( "CheckPackageAndCopy: NOTE: This has no influence on the operation of Cumulus itself.", TraceLevel.Warning );
                    return false;
                }

                if ( await CUtils.Isup.UploadFileAsync( FTPfilename, filename ) )
                    LogMessage( $"CheckPackageAndCopy: Uploaded {filename} to {FTPfilename}", TraceLevel.Info );
                else
                {
                    LogMessage( $"CheckPackageAndCopy: Upload of {filename} to {FTPfilename} failed.", TraceLevel.Error );
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region IDisposable CuSupport

        private bool disposedValue;

        protected virtual void Dispose( bool disposing )
        {
            if ( disposedValue )
                return;

            if ( disposing )
            {
                if ( LoggingOn )
                    ThisListener?.Dispose();
            }

            EndMyIniFile();

            MyIni.CheckAndCleanUp();
            SetUtilsIniValue( "General", "ParamCleanUp", "true" ); // make sure it works for the language as well
            CUstringIni.CheckAndCleanUp();

            // Reset to false so the user has explicitely to enable (=true) it again
            SetUtilsIniValue( "General", "ParamCleanUp", "false" );
            CUstringIni.SaveToFile();
            MyIni.SaveToFile();

            const string filenameCopy = "copy_Cumulus.ini";
            if ( File.Exists( filenameCopy ) )
                File.Delete( filenameCopy );

            disposedValue = true;
        }

        ~CuSupport()
        {
            Dispose( false );
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable CuSupport
    }

    #endregion

    #region Extensions
    public static class CuExtensions
    {
        // StdDev extension
        // From: https://stackoverflow.com/questions/2253874/standard-deviation-in-linq
        //
        public static float StdDev( this IEnumerable<float> values )
        {
            if ( values is ICollection<float> coll )
            {
                int count = coll.Count;
                if ( count < 2 )
                    return 0;

                double avg = 0;
                foreach ( float d in coll )
                    avg += d;
                avg /= count;

                double sum = 0;
                foreach ( float d in coll )
                    sum += ( d - avg ) * ( d - avg );

                return (float) Math.Sqrt( sum / ( count - 1 ) );
            }

            int n = 0;
            double mean = 0;
            double m2 = 0;
            foreach ( float x in values )
            {
                n++;
                double delta = x - mean;
                mean += delta / n;
                m2 += delta * ( x - mean );
            }

            return n >= 2 ? (float) Math.Sqrt( m2 / ( n - 1 ) ) : 0;
        }

        public static T[] RemoveAt<T>( this T[] source, int index )
        {
            ArgumentNullException.ThrowIfNull( source );

            T[] dest = new T[ source.Length - 1 ];
            if ( index > 0 )
                Array.Copy( source, 0, dest, 0, index );

            if ( index < source.Length - 1 )
                Array.Copy( source, index + 1, dest, index, source.Length - index - 1 );

            return dest;
        }

        // Used for the [flags] in the axis for the User Defined Graphs
        // https://stackoverflow.com/questions/677204/counting-the-number-of-flags-set-on-an-enumeration
        // https://en.wikipedia.org/wiki/Hamming_weight

        public static UInt64 CountFlags( this AxisType axis )
        {
            UInt32 v = (UInt32) axis;
            v = v - ( ( v >> 1 ) & 0x55555555 );
            v = ( v & 0x33333333 ) + ( ( v >> 2 ) & 0x33333333 );
            UInt32 c = unchecked(( ( v + ( v >> 4 ) ) & 0xF0F0F0F ) * 0x1010101) >> 24;
            return c;
        }

        public static bool Contains( this string source, string toCheck, StringComparison comp )
        {
            return source?.IndexOf( toCheck, comp ) >= 0;
        }
    }

    #endregion

    #region Random Generator
    public static class RandomGenerator
    {
        public static int RandomNumber( int min, int max )
        {
            return Random.Shared.Next( min, max );
        }

        public static string RandomString( int size, bool lowerCase = false )
        {
            Span<char> buffer = size <= 256 ? stackalloc char[ size ] : new char[ size ];
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26;

            for ( int i = 0; i < size; i++ )
                buffer[ i ] = (char) Random.Shared.Next( offset, offset + lettersOffset );

            return new string( buffer );
        }

        public static string RandomPassword()
        {
            return RandomString( 4, true ) + RandomNumber( 1000, 9999 ) + RandomString( 2 );
        }
    }

    #endregion

    #region Encryption

    public static class Crypto
    {
        public static byte[] GenerateKey()
        {
            var key = new byte[ 256 / 8 ];
            RandomNumberGenerator.Fill( key );
            return key;
        }

        public static string EncryptString( string plainText, byte[] key )
        {
            try
            {
                if ( string.IsNullOrEmpty( plainText ) )
                    return string.Empty;

                using var aes = Aes.Create();
                aes.Key = key;
                var cryptoTransform = aes.CreateEncryptor( aes.Key, aes.IV );
                var cipherText = Encrypt( plainText, cryptoTransform );
                var data = new byte[ cipherText.Length + aes.IV.Length + 1 ];
                data[ 0 ] = (byte) aes.IV.Length;
                Array.Copy( aes.IV, 0, data, 1, aes.IV.Length );
                Array.Copy( cipherText, 0, data, aes.IV.Length + 1, cipherText.Length );
                return Convert.ToBase64String( data );
            }
            catch ( Exception )
            {
                return null;
            }
        }

        public static string DecryptString( string encryptedText, byte[] key )
        {
            try
            {
                if ( string.IsNullOrEmpty( encryptedText ) )
                    return string.Empty;

                var data = Convert.FromBase64String( encryptedText );
                byte ivSize = data[ 0 ];
                var iv = new byte[ ivSize ];
                Array.Copy( data, 1, iv, 0, ivSize );
                var encrypted = new byte[ data.Length - ivSize - 1 ];
                Array.Copy( data, ivSize + 1, encrypted, 0, encrypted.Length );

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                var cryptoTransform = aes.CreateDecryptor( aes.Key, aes.IV );
                return Decrypt( encrypted, cryptoTransform );
            }
            catch ( Exception )
            {
                return null;
            }
        }

        private static byte[] Encrypt( string data, ICryptoTransform cryptoTransform )
        {
            ArgumentException.ThrowIfNullOrEmpty( data );

            using var memoryStream = new MemoryStream();
            using ( var cryptoStream = new CryptoStream( memoryStream, cryptoTransform, CryptoStreamMode.Write ) )
            using ( var writer = new StreamWriter( cryptoStream ) )
            {
                writer.Write( data );
            }

            return memoryStream.ToArray();
        }

        private static string Decrypt( byte[] data, ICryptoTransform cryptoTransform )
        {
            if ( data is null || data.Length == 0 )
                throw new ArgumentException( "Invalid data", nameof( data ) );

            using var memoryStream = new MemoryStream( data );
            using var cryptoStream = new CryptoStream( memoryStream, cryptoTransform, CryptoStreamMode.Read );
            using var reader = new StreamReader( cryptoStream );

            return reader.ReadToEnd();
        }
    }

    #endregion
}
