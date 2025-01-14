/*
 * CuSupport - Part of CumulusUtils
 *
 * © Copyright 2019-2024 Hans Rottier <hans.rottier@gmail.com>
 *
 * The code of CumulusUtils is public domain and distributed under the  
 * Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * (Note: this is different license than for CumulusMX itself, it is basically is usage license)
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI .NET Framework 4.8
 *              Initial release: pwsFWI                 (version 1.0)
 *                               Website Generator      (version 3.0)
 *                               ChartsCompiler         (version 5.0)
 *                               Maintenance releases   (version 6.x) including CustomLogs
 *              Startdate : 16 november 2021 start of conversion to .NET 5, 6 and 7
 *              Startdate : 15 january 2024 start of conversion to .NET 8
 *              
 * Environment: Raspberry Pi 4B and up
 *              Raspberry Pi OS
 *              C# / Visual Studio / Windows for development
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
        const string beta = "";

        #region declarations
        public Wind StationWind { get; set; }
        public Pressure StationPressure { get; set; }
        public Rain StationRain { get; set; }
        public Temp StationTemp { get; set; }
        public Distance StationDistance { get; set; }
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
        //public string DiaryOutputFilename { get; } = "diary.txt";
        public string DiaryOutputFilename { get; } = "diary.txt";
        public string CUserdataRECENT { get; } = "CUserdataRECENT.json";
        public string CUserdataDAILY { get; } = "CUserdataDAILY.json";
        public string CUserdataALL { get; } = "CUserdataALL.json";
        public string ExtraSensorsJSON { get; } = "extrasensorsdata.json";
        public string CustomLogsRecentJSON { get; } = "customlogsRecentdata.json";
        public string CustomLogsDailyJSON { get; } = "customlogsDailydata.json";
        public string CutilsChartsDef { get; } = "CutilsCharts.def";
        public string CutilsMenuDef { get; } = "CutilsMenu.def";
        public string CUhelptexts { get; } = "CUhelptexts.txt";
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
                Console.WriteLine( $" No Cumulus.ini found. Must run in Cumulus directory!" );
                Environment.Exit( 0 );
            }
            else
            {
                string filenameCopy = "copy_Cumulus.ini";
                if ( File.Exists( filenameCopy ) )
                    File.Delete( filenameCopy );
                File.Copy( "Cumulus.ini", filenameCopy );

                Ini = new IniFile( filenameCopy, this );
            }

            if ( !File.Exists( "cumulusutils.ini" ) )
            {
                // All entries will be created when called for because I changed the IniFiles library. Search for: HAR
                StreamWriter of = new StreamWriter( "cumulusutils.ini" );
                of.Dispose();
            }

            MyIni = new IniFile( "cumulusutils.ini", this );

            // Get AlltimeRecords ready for the possible Axis ranges of the graphs
            if ( File.Exists( "data/alltime.ini" ) )
            {
                AtrIni = new IniFile( "data/alltime.ini", this );
            }

            // We need strings.ini entries for the ExtraSensor module
            if ( !File.Exists( "strings.ini" ) ) File.Copy( "samplestrings.ini", "strings.ini" );
            if ( File.Exists( "strings.ini" ) ) StringsIni = new IniFile( "strings.ini", this );

            // Init the logging
            //
            InitLogging();

            // Do the locale thing
            // See https://docs.microsoft.com/en-gb/openspecs/windows_protocols/ms-lcid/70feba9f-294e-491e-b6eb-56532684c37f
            // Using only ISO two letter codes

            Locale = GetUtilsIniValue( "General", "Language", "en-GB" );

            try
            {
                Language = Locale.Substring( 0, 2 ).ToUpper( CUtils.Inv );
                Country = Locale.Substring( 3, 2 ).ToUpper( CUtils.Inv );

                CUtils.ThisCulture = CultureInfo.GetCultureInfo( Locale );
            }
            catch ( Exception e ) when ( e is CultureNotFoundException )
            {
                LogDebugMessage( $" Invalid Locale : {Locale}" );
                LogTraceErrorMessage( $" Invalid Culture : {e.Message}" );
                LogTraceErrorMessage( $" Invalid Culture for the system : {Locale}" );
                LogTraceWarningMessage( $" Using English GB locale : en-GB" );
                Locale = "en-GB";
                Language = "EN";
                Country = "GB";

                CUtils.ThisCulture = CultureInfo.GetCultureInfo( Locale );
            }
            catch ( Exception e )
            {
                // Hope we never get here
                LogDebugMessage( $" Unknown exceeption - Invalid Culture : {e.Message}" );
                throw;
            }

            if ( !CUtils.Thrifty )
            {
                using ( StreamWriter of = new StreamWriter( $"{PathUtils}HighchartsLanguage.js", false, Encoding.UTF8 ) )
                {
                    StringBuilder str = new StringBuilder();

                    str.AppendLine( "Highcharts.lang = {" );
                    str.AppendLine( "  lang:{" );
                    str.Append( "    months:[" );
                    for ( int i = 0; i < 12; i++ )
                    {
                        str.Append( $"'{CUtils.ThisCulture.DateTimeFormat.GetMonthName( i + 1 )}'," );
                    }
                    str.Remove( str.Length - 1, 1 );
                    str.AppendLine( "]," );

                    str.Append( "    shortMonths:[" );
                    for ( int i = 0; i < 12; i++ )
                    {
                        str.Append( $"'{CUtils.ThisCulture.DateTimeFormat.AbbreviatedMonthNames[ i ]}'," );
                    }
                    str.Remove( str.Length - 1, 1 );
                    str.AppendLine( "]," );

                    str.Append( "    weekdays:[" );
                    for ( int i = 0; i < 7; i++ )
                    {
                        str.Append( $"'{CUtils.ThisCulture.DateTimeFormat.DayNames[ i ]}'," );
                    }
                    str.Remove( str.Length - 1, 1 );
                    str.AppendLine( "]," );
                    str.AppendLine( "    thousandsSep: \"\"" );
                    str.AppendLine( "  }" );
                    str.AppendLine( "};" );

                    of.WriteLine( $"{str}" );
                    of.WriteLine( $"highchartsOptions = Highcharts.setOptions(Highcharts.lang);" );
                }
            }

            // And include the file highchartsLanguage.js after the HighchartsOptions.js
            LogDebugMessage( $" CUstrings[xx].ini : CUstrings{Language}.ini looked for." );

            if ( !File.Exists( $"CUstrings{Language}.ini" ) )
            {
                LogTraceWarningMessage( $" No CUstrings{Language}.ini found." );

                StreamWriter of = new StreamWriter( $"CUstrings{Language}.ini" );
                of.Dispose();
            }

            CUstringIni = new IniFile( $"CUstrings{Language}.ini", this );

            PerHour = GetCUstringValue( "General", "PerHour", "/hr", false );

            StationWind = new Wind( (WindDim) Ini.GetValue( "Station", "WindUnit", 2 ), this );             // default does not count: comes from CMX, for me km/h
            StationPressure = new Pressure( (PressureDim) Ini.GetValue( "Station", "PressureUnit", 1 ) );   // default does not count: comes from CMX, for me hPa
            StationRain = new Rain( (RainDim) Ini.GetValue( "Station", "RainUnit", 0 ) );                   // default does not count: comes from CMX, for me mm
            StationTemp = new Temp( (TempDim) Ini.GetValue( "Station", "TempUnit", 0 ) );                   // default does not count: comes from CMX, for me C

            int tmpDim = Ini.GetValue( "Station", "WindUnit", 2 ) == 0 ? 2 : Ini.GetValue( "Station", "WindUnit", 2 );
            StationDistance = new Distance( (DistanceDim) tmpDim );                                         // CMX does not know Distance(unit) but Wind can be misused for this

            StationHeight = new Height( (HeightDim) Ini.GetValue( "Station", "CloudBaseInFeet", 0 ) );      // We use the CloudBaseInFeet param of CMX as default.
                                                                                                            // We'll see later if that needs modification

            LogDebugMessage( $" CumulusUtils version: {UnformattedVersion()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Wind (m/s, mph, km/h, kts): {StationWind.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Pressure (mb,hPa,inHg): {StationPressure.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Rain (mm,in): {StationRain.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit T (C,F): {StationTemp.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Distance (m, mi, km, kn): {StationDistance.Text()}" );
            LogDebugMessage( $" CuSupport constructor : Unit Height (m, ft): {StationHeight.Text()}" );

        }

        #endregion

        #region Methods INI
        public string GetCumulusIniValue( string section, string key, string def ) => Ini.GetValue( section, key, def );
        public string GetStringsIniValue( string section, string key, string def ) => StringsIni.GetValue( section, key, def );
        public string GetAlltimeRecordValue( string section, string key, string def ) => AtrIni.GetValue( section, key, def );

        public string GetUtilsIniValue( string section, string key, string def )
        {
            string tmp;

            tmp = MyIni.GetValue( section, key, def );

            if ( !string.IsNullOrEmpty( tmp ) && tmp.Contains( "<#" ) )
            {
                // Not Empty string AND must contain a webtag only then we will go replace
                // This will not be often and many so I take the performance risk to create the IPC object locally and use 
                CmxIPC thisIPC = new CmxIPC( this, CUtils.Isup );

                // Prevent having to make this method async by using AsyncTask
                Task<string> AsyncTask = thisIPC.ReplaceWebtagsGetAsync( tmp );
                AsyncTask.Wait();
                tmp = AsyncTask.Result;
            }

            LogTraceVerboseMessage( DateTime.Now + $" GetUtilsIniValue {key} / {tmp}" );

            return ( tmp );
        }

        public void SetUtilsIniValue( string section, string key, string def ) => MyIni.SetValue( section, key, def );

        public string GetCUstringValue( string section, string key, string def, bool javaScript )
        {
            // From this one: https://stackoverflow.com/questions/7265315/replace-multiple-characters-in-a-c-sharp-string
            //
            string tmp = CUstringIni.GetValue( section, key, def );

            if ( string.IsNullOrEmpty( tmp ) )
                return ( tmp );

            if ( javaScript && tmp.Contains( '\'' ) )
            {
                char[] separators = new char[] { '\'' };
                string[] temp = tmp.Split( separators, StringSplitOptions.None );
                tmp = string.Join( @"\'", temp );
            }

            LogTraceVerboseMessage( DateTime.Now + $" GetCUstringValue {key} / {tmp}" );

            return ( tmp );
        }

        public void SetCUstringValue( string section, string key, string def ) => CUstringIni.SetValue( section, key, def );

        private void EndMyIniFile() { if ( MyIni is not null ) { MyIni.Flush(); MyIni.Refresh(); } if ( CUstringIni is not null ) { CUstringIni.Flush(); CUstringIni.Refresh(); } }

        #endregion

        #region Methods Highcharts

        public int HighChartsWindBarbSpacing()
        {
            // Make sure all windbarb charts have an acceptable and correct spacing:
            //   Note: using illegal unit numbers - like 5 - causes the chart not to display
            //   See: https://api.highcharts.com/highstock/plotOptions.series.dataGrouping.units
            //   ['hour',[1, 2, 3, 4, 6, 8, 12] ]
            int tmp = Convert.ToInt32( GetCumulusIniValue( "Graphs", "GraphHours", "" ) ) / 24;
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

        #region Methods Utilities

        // Replace white space with either nothing (empty replacement string) or with whatever you want (mostly single space)
        private static readonly Regex sWhitespace = new Regex( @"\s+" );
        public static string StringRemoveWhiteSpace( string InputWithSpaces, string ReplacementOfSpaces ) => sWhitespace.Replace( InputWithSpaces, ReplacementOfSpaces );

        public static string StationInUse( int i )
        {
            string[] StationDesc =
            {
                "Davis Vantage Pro",			// 0
			    "Davis Vantage Pro2",			// 1
			    "Oregon Scientific WMR-928",	// 2
			    "Oregon Scientific WM-918",		// 3
			    "EasyWeather",					// 4
			    "Fine Offset",					// 5
			    "LaCrosse WS2300",				// 6
			    "Fine Offset with Solar",		// 7
			    "Oregon Scientific WMR100",		// 8
			    "Oregon Scientific WMR200",		// 9
			    "Instromet",					// 10
			    "Davis WLL",					// 11
			    "GW1000/Ecowitt Local API",		// 12 - the only name changed, CMX uses GW1000 and local API in the interface
			    "HTTP WUnderground",			// 13
			    "HTTP Ecowitt",					// 14
			    "HTTP Ambient",					// 15
			    "WeatherFlow Tempest",			// 16
			    "Simulator",					// 17
                "Davis WeatherLink Cloud (WLL/WLC)",  // 18
                "Davis WeatherLink Cloud (VP2)",      // 19
                "Ecowitt Cloud"                 // 20
            };

            if ( i < 0 || i > StationDesc.Length - 1 )
                return "Unknown Station";
            else
                return StationDesc[ i ];
        }

        public bool DateIsToday( DateTime thisDate )
        {
            bool retval;

            TimeSpan thisSpan = DateTime.Now - thisDate;

            LogTraceInfoMessage( $"DateIsToday for thisDate: {thisDate} | thisDate.DayOfYear: {thisDate.DayOfYear} versus Now.DayOfYear: {DateTime.Now.DayOfYear})" );
            LogTraceInfoMessage( $"DateIsToday: thisSpan: {thisSpan} | thisSpan.TotalDays = {thisSpan.TotalDays}" );

            if ( thisSpan.TotalDays > 1 ) retval = false;
            else retval = true;

            return retval;
        }

        public static string FormattedVersion()
        {
            string _ver;

            _ver = typeof( CuSupport ).Assembly.GetName().Version.Major.ToString( CUtils.Inv ) + "." +
                          typeof( CuSupport ).Assembly.GetName().Version.Minor.ToString( CUtils.Inv ) + "." +
                          typeof( CuSupport ).Assembly.GetName().Version.Build.ToString( CUtils.Inv );

            _ver = String.Format( CUtils.Inv, $"<a href='https://cumulus.hosiene.co.uk/viewtopic.php?f=44&t=17998' target='_blank'>CumulusUtils</a> " +
                                  $"Version {_ver} " + beta +
                                  $" - generated at " + DateTime.Now.ToString( "g", CUtils.ThisCulture ) );  // .ToString( "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture )

            return _ver;
        }

        public static string UnformattedVersion()
        {
            string _ver;

            _ver = typeof( CuSupport ).Assembly.GetName().Version.Major.ToString( CUtils.Inv ) + "." +
                          typeof( CuSupport ).Assembly.GetName().Version.Minor.ToString( CUtils.Inv ) + "." +
                          typeof( CuSupport ).Assembly.GetName().Version.Build.ToString( CUtils.Inv );

            return _ver + " " + beta;
        }

        public static string Copyright() => "&copy; Hans Rottier";

        public static StringBuilder CopyrightForGeneratedFiles()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine( "<!--" );
            result.AppendLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
            result.AppendLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
            result.AppendLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
            result.AppendLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
            result.AppendLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
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

            string SpecificHighchartsVersion = GetUtilsIniValue( "General", "UseSpecificHighchartsVersion", "" );
            bool UseHighchartsBoostModule = GetUtilsIniValue( "Graphs", "UseHighchartsBoostModule", "true" ).Equals( "true", CUtils.Cmp );

            if ( string.IsNullOrEmpty( SpecificHighchartsVersion ) )
            {
                SpecificHighchartsVersion = "11.2";
            }

            sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}/highstock.js'></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}/highcharts-more.js\"></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}/indicators/indicators.js\"></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}/modules/exporting.js\" ></script>" );
            sb.AppendLine( $"<script src=\"https://code.highcharts.com/stock/{SpecificHighchartsVersion}/modules/heatmap.js\"></script>" );
            sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}/modules/windbarb.js'></script>" );
            //sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}/indicators/indicators.js'></script>" );
            //sb.AppendLine( $"<script src='https://code.highcharts.com/stock/{SpecificHighchartsVersion}/indicators/trendline.js'></script>" );
            sb.AppendLine( $"<script defer src='https://code.highcharts.com/{SpecificHighchartsVersion}/modules/accessibility.js'></script>" );

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

        public void SetStartAndEndForData( out DateTime Start, out DateTime End )
        {
            DateTime Now = DateTime.Now;
            Now = new DateTime( Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, 0 );

            End = Now.AddMinutes( -Now.Minute % Math.Max( CUtils.FTPIntervalInMinutes, CUtils.LogIntervalInMinutes ) );

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

            return;
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

            LogTraceInfoMessage( $"Initial {CUTraceSwitch} => Error: {CUTraceSwitch.TraceError}, Warning: {CUTraceSwitch.TraceWarning}, Info: {CUTraceSwitch.TraceInfo}, Verbose: {CUTraceSwitch.TraceInfo}" );

            try
            {
                CUTraceSwitch.Level = (TraceLevel) Enum.Parse( typeof( TraceLevel ), thisTrace, true );
            }
            catch ( Exception e ) when ( e is ArgumentException || e is ArgumentNullException )
            {
                LogTraceErrorMessage( $"Initial: Exception parsing the TraceLevel - {e.Message}" );
                LogTraceErrorMessage( $"Initial: Setting level to Warning." );
                CUTraceSwitch.Level = TraceLevel.Warning;
            }

            if ( LoggingOn )
            {
                ThisListener = new TextWriterTraceListener( $"utils/utilslog/{DateTime.Now.ToString( "yyMMddHHmm", CUtils.Inv )}cumulusutils.log" );
                Trace.Listeners.Add( ThisListener );  // Used for messages under the conditions of the Switch: None, Error, Warning, Information, Verbose
                Trace.AutoFlush = true;
            }

            LogTraceInfoMessage( $"According to Inifile {thisTrace} => Error: {CUTraceSwitch.TraceError}, Warning: {CUTraceSwitch.TraceWarning}, Info: {CUTraceSwitch.TraceInfo}, Verbose: {CUTraceSwitch.TraceVerbose}, " );

            if ( Environment.OSVersion.Platform.Equals( PlatformID.Unix ) )
            {
                // Shut up the default listener
                LogDebugMessage( "CumulusUtils Initial: Shutting down the default listener" );
                LogTraceInfoMessage( "CumulusUtils Initial: Shutting down the default listener" );
                Trace.Listeners.RemoveAt( 0 );
            }
        }

        public void LogDebugMessage( string message )
        {
            if ( NormalMessageToConsole ) Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + message );
            if ( LoggingOn ) Debug.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + message );
        }

        public void LogTraceErrorMessage( string message ) => Trace.WriteLineIf( CUTraceSwitch.TraceError, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Error " + message );
        public void LogTraceWarningMessage( string message ) => Trace.WriteLineIf( CUTraceSwitch.TraceWarning, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Warning " + message );
        public void LogTraceInfoMessage( string message ) => Trace.WriteLineIf( CUTraceSwitch.TraceInfo, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Information " + message );
        public void LogTraceVerboseMessage( string message ) => Trace.WriteLineIf( CUTraceSwitch.TraceVerbose, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff " ) + "Verbose " + message );

        #endregion

        #region Javascript / Unix time conversions

        public static long DateTimeToJS( DateTime timestamp ) => (long) ( timestamp - new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds * 1000;
        public static long DateTimeToUnix( DateTime timestamp ) => (long) ( timestamp - new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds;
        public static long DateTimeToUnixUTC( DateTime timestamp ) => (long) ( timestamp.ToUniversalTime() - new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds;

        #endregion

        #region IDisposable CuSupport

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                EndMyIniFile();
                //Console.WriteLine( "After EndMyIniFile" );

                string filenameCopy = "copy_Cumulus.ini";
                if ( File.Exists( filenameCopy ) )
                    File.Delete( filenameCopy );

                if ( LoggingOn ) ThisListener.Dispose();
                //Console.WriteLine( "After Disposing ThisListener" );

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~CuSupport()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( false );
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( true );
            // TODO: uncomment the following line if the finalizer is overridden above.
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
            double ret = 0;
            int count = values.Count();
            if ( count >= 2 )
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum( d => ( d - avg ) * ( d - avg ) );

                //Put it all together
                ret = Math.Sqrt( sum / ( count - 1 ) ); // Must be N-1 for the estimator of sigma so, count must be >=2
            }
            return (float) ret;
        }

        //Check, not sure this works because of the i-- required to stay on the position of the deleted item.
        public static T[] RemoveAt<T>( this T[] source, int index )
        {
            if ( source is null )
                throw new ArgumentNullException( paramName: nameof( source ), "RemoveAt method used with array argument Null." );

            T[] dest = new T[ source.Length - 1 ];
            if ( index > 0 )
                Array.Copy( source, 0, dest, 0, index );

            if ( index < source.Length - 1 )
                Array.Copy( source, index + 1, dest, index, source.Length - index - 1 );

            return dest;
        }


        // Used for the [flags] in the axis for the USer Defined Graphs
        // https://stackoverflow.com/questions/677204/counting-the-number-of-flags-set-on-an-enumeration
        // https://en.wikipedia.org/wiki/Hamming_weight

        public static UInt64 CountFlags( this AxisType axis )
        {
            UInt32 c;

            //try
            //{  // Maybe required for overflow testing, howver the unchecke seems to take care of it.

            UInt32 v = (UInt32) axis;
            v = v - ( ( v >> 1 ) & 0x55555555 ); // reuse input as temporary
            v = ( v & 0x33333333 ) + ( ( v >> 2 ) & 0x33333333 ); // temp
            c = unchecked(( ( v + ( v >> 4 ) ) & 0xF0F0F0F ) * 0x1010101) >> 24; // count

            // When needed: the 64 bit version
            //UInt64 i = (UInt64)axis;
            //i = i - ((i >> 1) & 0x5555555555555555UL);
            //i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
            //return unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56;

            //Console.WriteLine($"CountFlags result 1 {c}");
            //}
            //catch (Exception e)
            //{
            //  Console.WriteLine($"CountFlags Exception {e.Message}");
            //  Console.WriteLine($"CountFlags result 3 {c}");
            //}

            //Console.WriteLine($"CountFlags result 2 {c}");
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
        // Instantiate random number generator.  
        // It is better to keep a single Random instance 
        // and keep using Next on the same instance.  
        private static readonly Random _random = new Random();

        // Generates a random number within a range.      
        public static int RandomNumber( int min, int max )
        {
            return _random.Next( min, max );
        }

        // Generates a random string with a given size.    
        public static string RandomString( int size, bool lowerCase = false )
        {
            var builder = new StringBuilder( size );

            // Unicode/ASCII Letters are divided into two blocks
            // (Letters 65–90 / 97–122):   
            // The first group containing the uppercase letters and
            // the second group containing the lowercase.  

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length = 26  

            for ( var i = 0; i < size; i++ )
            {
                var @char = (char) _random.Next( offset, offset + lettersOffset );
                builder.Append( @char );
            }

            return lowerCase ? builder.ToString().ToLower( CultureInfo.CurrentCulture ) : builder.ToString();
        }

        // Generates a random password.  
        // 4-LowerCase + 4-Digits + 2-UpperCase  
        public static string RandomPassword()
        {
            var passwordBuilder = new StringBuilder();

            // 4-Letters lower case   
            passwordBuilder.Append( RandomString( 4, true ) );

            // 4-Digits between 1000 and 9999  
            passwordBuilder.Append( RandomNumber( 1000, 9999 ) );

            // 2-Letters upper case  
            passwordBuilder.Append( RandomString( 2 ) );
            return passwordBuilder.ToString();
        }
    }

    #endregion

    #region Encryption

    public static class Crypto
    {
        public static byte[] GenerateKey()
        {
            var key = new byte[ 256 / 8 ]; // use 256 bits
            var rnd = new Random();
            rnd.NextBytes( key );
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
                if ( encryptedText.Length == 0 )
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
            if ( data is null || data.Length <= 0 )
                throw new ArgumentException( "Invalid data", nameof( data ) );

            using var memoryStream = new MemoryStream();
            using ( var cryptoStream = new CryptoStream( memoryStream, cryptoTransform, CryptoStreamMode.Write ) )
            {
                using var writer = new StreamWriter( cryptoStream );
                writer.Write( data );
            }

            return memoryStream.ToArray();
        }

        private static string Decrypt( byte[] data, ICryptoTransform cryptoTransform )
        {
            if ( data is null || data.Length <= 0 )
                throw new ArgumentException( "Invalid data", nameof( data ) );

            using var memoryStream = new MemoryStream( data );
            using var cryptoStream = new CryptoStream( memoryStream, cryptoTransform, CryptoStreamMode.Read );
            using var reader = new StreamReader( cryptoStream );

            return reader.ReadToEnd();
        }

    }

    /*
        /// <summary>
        /// Cipher class 
        /// Stole from https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
        /// Initially for Maps, getting the stationswithutils.xml file, changing it and putting it back
        /// </summary>
        /// <autogeneratedoc />
        /// Done
        public static class StringCipher
        {
            // This constant is used to determine the keysize of the encryption algorithm in bits.
            // We divide this by 8 within the code below to get the equivalent number of bytes.
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string Encrypt( string plainText, string passPhrase )
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes( plainText );
                using ( var password = new Rfc2898DeriveBytes( passPhrase, saltStringBytes, DerivationIterations ) )
                {
                    var keyBytes = password.GetBytes( Keysize / 8 );
                    using ( var symmetricKey = new RijndaelManaged() )
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using ( var encryptor = symmetricKey.CreateEncryptor( keyBytes, ivStringBytes ) )
                        {
                            using ( var memoryStream = new MemoryStream() )
                            {
                                using ( var cryptoStream = new CryptoStream( memoryStream, encryptor, CryptoStreamMode.Write ) )
                                {
                                    cryptoStream.Write( plainTextBytes, 0, plainTextBytes.Length );
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat( ivStringBytes ).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat( memoryStream.ToArray() ).ToArray();
                                    //                memoryStream.Close();
                                    //                cryptoStream.Close();
                                    return Convert.ToBase64String( cipherTextBytes );
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt( string cipherText, string passPhrase )
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String( cipherText );
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take( Keysize / 8 ).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip( Keysize / 8 ).Take( Keysize / 8 ).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip( ( Keysize / 8 ) * 2 ).Take( cipherTextBytesWithSaltAndIv.Length - ( ( Keysize / 8 ) * 2 ) ).ToArray();

                using ( var password = new Rfc2898DeriveBytes( passPhrase, saltStringBytes, DerivationIterations ) )
                {
                    var keyBytes = password.GetBytes( Keysize / 8 );
                    using ( var symmetricKey = new RijndaelManaged() )
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using ( var decryptor = symmetricKey.CreateDecryptor( keyBytes, ivStringBytes ) )
                        {
                            using ( var memoryStream = new MemoryStream( cipherTextBytes ) )
                            {
                                using ( var cryptoStream = new CryptoStream( memoryStream, decryptor, CryptoStreamMode.Read ) )
                                {
                                    var plainTextBytes = new byte[ cipherTextBytes.Length ];
                                    var decryptedByteCount = cryptoStream.Read( plainTextBytes, 0, plainTextBytes.Length );
                                    //                memoryStream.Close();
                                    //                cryptoStream.Close();
                                    return Encoding.UTF8.GetString( plainTextBytes, 0, decryptedByteCount );
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[ 32 ]; // 32 Bytes will give us 256 bits.
                using ( var rngCsp = new RNGCryptoServiceProvider() )
                {
                    // Fill the array with cryptographically secure random bytes.
                    rngCsp.GetBytes( randomBytes );
                }
                return randomBytes;
            }
        }
    */

    #endregion
}