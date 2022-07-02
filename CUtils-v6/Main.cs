/*
 * CumulusUtils/Main
 *
 * © Copyright 2019 - 2021 Hans Rottier <hans.rottier@gmail.com>
 *
 * If/When the code is made public domain the licence will be changed to the GNU 
 * General Public License as published by the Free Software Foundation;
 * Until then, the code of CumulusUtils is not public domain and only the executable is 
 * distributed under the  Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * As a consequence, this code should not be in your posession unless with explicit permission of the author
 * 
 * Remarks  with release: (keep one stable and one daring release)
 *   1) For users who like new things and take the risk of bugs
 *   2) For users who do not wish to be in the frontline
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI
 *              Initial release: pwsFWI             (version 1.0)
 *                               Website Generator  (version 3.0)
 *                               ChartsCompiler     (version 5.0)
 *              
 * Environment: Raspberry 3B+
 *              Raspbian / Linux 
 *              C# / Visual Studio / Windows
 * 
 * Files:       Main.cs
 *              Airlinklog.cs
 *              AirQuality.cs
 *              ChartsCompiler CodeGen.cs
 *              ChartsCompiler Decl.cs
 *              ChartsCompiler Eval.cs
 *              ChartsCompiler Parser.cs
 *              CmxIPC.cs
 *              Dayfile.cs
 *              DayRecords.cs
 *              ExtraSensors.cs
 *              ExtraSensorslog.cs
 *              Forecast.cs
 *              Graphs.cs
 *              GraphMisc.cs
 *              GraphRain.cs
 *              GraphSolar.cs
 *              GraphTemp.cs
 *              GraphWind.cs
 *              InetSupport.cs
 *              IniFile.cs
 *              Maps.cs
 *              Monthfile.cs
 *              NOAAdisplay.cs
 *              PwsFWI.cs
 *              Records.cs
 *              StationMap.cs
 *              Support.cs
 *              SysInfo.cs
 *              Top10.cs
 *              UnitsAndConversions.cs
 *              UserReports.cs
 *              Website.cs
 *              Yadr.cs
 */

/* 
 * Important supporting sites (information, weather Forecasts whatever...)
 * 1) https://www.i18nqa.com/debug/utf8-debug.html
 * 2) https://www.wmo.int/pages/prog/www/IMOP/CIMO-Guide.html
 * 3) http://yourweather.co.uk/ (for the API)
 * 4) https://jsfiddle.net/
 * 5) https://highcharts.com/
 * 6) https://www.technical-recipes.com/2016/how-to-run-processes-and-obtain-the-output-in-c/
 * 7) https://www.encodedna.com/webapi/webapi-controller-for-file-upload-procedure.htm
 * 8) https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
 * 9) https://port135.com/public-key-token-assembly-dll-file/ // When you have to map a version of a dll to another in cumulusutils.exe.config
 * 
 * Read this on HttpClient and async/await: 
 * https://stackoverflow.com/questions/42235677/httpclient-this-instance-has-already-started
 * https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d
 * https://stackoverflow.com/questions/47944400/how-do-i-use-httpclient-postasync-parameters-properly
 * https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
 * https://devblogs.microsoft.com/dotnet/configureawait-faq/
 * 
 * Read this on how to stop bubbling up the async thing:
 * (NOTE: this is relevant because of the GetUtilsIniValue which would really be a major problem if that needs to be async)
 * https://stackoverflow.com/questions/48116738/stop-bubbling-up-async-await-task-to-caller-without-getting-deadlock
 * https://stackoverflow.com/questions/54388796/how-can-i-stop-async-await-from-bubbling-up-in-functions
 *
 * On colouring and Graphing:
 * https://stackoverflow.com/questions/309149/generate-distinctly-different-rgb-colors-in-graphs
 * https://sashamaps.net/docs/resources/20-colors/
 * https://graphicdesign.stackexchange.com/questions/3682/where-can-i-find-a-large-palette-set-of-contrasting-colors-for-coloring-many-d
 *
 * Never used but you never know:
 * 1) https://www.hanselman.com/blog/RemoteDebuggingWithVSCodeOnWindowsToARaspberryPiUsingNETCoreOnARM.aspx
 * 2) https://www.csharp-examples.net/
 * 3) https://jstween.blogspot.com/2007/01/javascript-motion-tween.html
 * 4) https://jsfiddle.net/gh/get/library/pure/highcharts/highcharts/tree/master/samples/highcharts/demo/combo-meteogram
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;

namespace CumulusUtils
{
    public class CMXutils
    {
        private bool DoPwsFWI;
        private bool DoTop10;
        private bool DoSystemChk;
        private bool DoGraphs;
        private bool DoCreateMap;
        private bool DoYadr;
        private bool DoRecords;
        private bool DoDayRecords;
        private bool DoNOAA;
        private bool DoCheckOnly;
        private bool DoForecast;
        private bool DoUserReports;
        private bool DoStationMap;
        private bool DoMeteoCam;
        private bool DoAirLink;
        private bool DoCompileOnly;
        private bool DoUserAskedData;
        private bool DoExtraSensors;

        public static CuSupport Sup { get; set; }
        public static InetSupport Isup { get; set; }

        static public bool Thrifty { get; private set; }
        static public bool ThriftyRecordsDirty { get; set; }
        static public bool ThriftyTop10RecordsDirty { get; set; }
        static public int ThriftyTop10RecordsPeriod { get; private set; }
        static public bool ThriftyDayRecordsDirty { get; set; }
        static public bool ThriftyRainGraphsDirty { get; set; }
        static public int ThriftyRainGraphsPeriod { get; private set; }
        static public bool ThriftyTempGraphsDirty { get; set; }
        static public int ThriftyTempGraphsPeriod { get; private set; }
        static public bool ThriftyWindGraphsDirty { get; set; }
        static public int ThriftyWindGraphsPeriod { get; private set; }
        static public bool ThriftySolarGraphsDirty { get; set; }
        static public int ThriftySolarGraphsPeriod { get; private set; }
        static public bool ThriftyMiscGraphsDirty { get; set; }
        static public int ThriftyMiscGraphsPeriod { get; private set; }

        static public bool DoWebsite { get; set; }
        static public bool DoLibraryIncludes { get; set; }
        static public bool DojQueryInclude { get; set; }
        static public bool MapParticipant { get; private set; }
        static public bool HasRainGraphMenu { get; set; }
        static public bool HasTempGraphMenu { get; set; }
        static public bool HasWindGraphMenu { get; set; }
        static public bool HasSolarGraphMenu { get; set; }
        static public bool HasMiscGraphMenu { get; set; }
        static public bool HasStationMapMenu { get; set; }
        static public bool HasMeteoCamMenu { get; set; }
        static public bool CheckOnlyAsked { get; set; }

        // Check for presence of optional sensors 
        static public bool HasSolar { get; set; }
        static public bool ShowUV { get; set; }
        static public bool HasAirLink { get; set; }
        static public bool ParticipatesSensorCommunity { get; set; }
        static public bool HasExtraSensors { get; set; }
        static public DateTime RunStarted { get; private set; }
        static public DateTime StartOfObservations { get; set; }
        static public bool CanDoMap { get; set; }

        static internal List<DayfileValue> MainList = new List<DayfileValue>();

        private static async Task Main( string[] args )
        {
            TraceListener FtpListener = null;

            // For research puposes:
            //string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //assemblyVersion = Assembly.LoadFile("your assembly file").GetName().Version.ToString();
            //string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            //string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            try
            {
                // Required as from version > 1.0.0; All produced files will end up in utils
                // except for cumulusutils.ini and the logs
                // And from version 3.7.1 all logfiles will go in 'utilslog' 
                // This procedure is called before the CuSupport instance is created because the first thing is to start the debug logging
                //

                if ( !File.Exists( "Cumulus.ini" ) )
                {
                    Console.WriteLine( $" No Cumulus.ini found. Must run in Cumulus directory!" );
                    Environment.Exit( 0 );
                }

                if ( !Directory.Exists( "utils" ) )
                    Directory.CreateDirectory( "utils" );
                if ( !Directory.Exists( "utils/utilslog" ) )
                    Directory.CreateDirectory( "utils/utilslog" );

                string[] files = Directory.GetFiles( "utils/utilslog" );

                foreach ( string file in files )
                {
                    FileInfo fi = new FileInfo( file );
                    if ( fi.CreationTime < DateTime.Now.AddDays( -2 ) )
                        fi.Delete();
                }

                Sup = new CuSupport();

                // So, here we go... for FluentFTP
                // The only time CuSupport is instantiated; Can't be in the different classes
                if ( Sup.GetUtilsIniValue( "FTP site", "FtpLog", "Off" ).Equals( "On", StringComparison.OrdinalIgnoreCase ) )
                {
                    FtpTrace.LogPassword = false;
                    FtpTrace.LogUserName = false;
                    FtpTrace.LogIP = false;

                    FtpListener = new TextWriterTraceListener( $"utils/utilslog/{DateTime.Now.ToString( "yyMMddHHmm", CultureInfo.InvariantCulture )}FTPlog.txt" );
                    FtpTrace.AddListener( FtpListener );
                }

                Isup = new InetSupport( Sup );

                Sup.LogDebugMessage( "CumulusUtils : ----------------------------" );
                Sup.LogDebugMessage( "CumulusUtils : Entering Main" );

                // Initialise the Thrifty system parameters
                Thrifty = false;
                RunStarted = DateTime.Now;

                ThriftyRecordsDirty = false;
                ThriftyTop10RecordsDirty = false;
                ThriftyDayRecordsDirty = false;
                ThriftyRainGraphsDirty = false;
                ThriftyTempGraphsDirty = false;
                ThriftyWindGraphsDirty = false;
                ThriftyMiscGraphsDirty = false;

                ThriftyTop10RecordsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "Top10RecordsPeriod", "1" ), CultureInfo.InvariantCulture );
                ThriftyRainGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "RainGraphsPeriod", "1" ), CultureInfo.InvariantCulture );
                ThriftyTempGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "TempGraphsPeriod", "1" ), CultureInfo.InvariantCulture );
                ThriftyWindGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "WindGraphsPeriod", "1" ), CultureInfo.InvariantCulture );
                ThriftySolarGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "SolarGraphsPeriod", "1" ), CultureInfo.InvariantCulture );
                ThriftyMiscGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "MiscGraphsPeriod", "1" ), CultureInfo.InvariantCulture );

                if ( Environment.OSVersion.Platform.Equals( PlatformID.Unix ) )
                {
                    Sup.LogDebugMessage( "Checking Mono Version on Linux/Unix" );
                    SysInfo tmp = new SysInfo( Sup, Isup );

                    CanDoMap = tmp.CheckMonoVersion();
                }
                else
                    CanDoMap = true;


                HasStationMapMenu = Sup.GetUtilsIniValue( "StationMap", "StationMapMenu", "true" ).Equals( "true", StringComparison.OrdinalIgnoreCase );
                HasMeteoCamMenu = Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamMenu", "true" ).Equals( "true", StringComparison.OrdinalIgnoreCase );
                HasExtraSensors = Sup.GetUtilsIniValue( "ExtraSensors", "ExtraSensors", "false" ).Equals( "true", StringComparison.OrdinalIgnoreCase ) &&
                    Sup.GetCumulusIniValue( "Station", "LogExtraSensors", "" ).Equals( "1" );
                ParticipatesSensorCommunity = Sup.GetUtilsIniValue( "ExtraSensors", "ParticipatesSensorCommunity", "false" ).Equals( "true", StringComparison.OrdinalIgnoreCase );
                MapParticipant = Sup.GetUtilsIniValue( "Maps", "Participant", "true" ).Equals( "true", StringComparison.OrdinalIgnoreCase );
                HasSolar = Sup.GetUtilsIniValue( "Website", "ShowSolar", "true" ).Equals( "true", StringComparison.OrdinalIgnoreCase ); // Is an indirect determination set by the user only in  cutils
                DoLibraryIncludes = Sup.GetUtilsIniValue( "General", "DoLibraryIncludes", "false" ).Equals( "true", StringComparison.OrdinalIgnoreCase ); // Do we need the libs??
                DojQueryInclude = Sup.GetUtilsIniValue( "General", "GeneratejQueryInclude", "false" ).Equals( "true", StringComparison.OrdinalIgnoreCase );

                bool AirLinkIn = Sup.GetCumulusIniValue( "AirLink", "In-Enabled", "0" ).Equals( "1" );
                bool AirLinkOut = Sup.GetCumulusIniValue( "AirLink", "Out-Enabled", "0" ).Equals( "1" );
                HasAirLink = AirLinkIn || AirLinkOut;

                // Now start doing things
                CMXutils p = new CMXutils();
                await p.RealMainAsync( args );
            }
            catch ( ArgumentNullException ex )
            {
                Sup.LogTraceErrorMessage( $"Exception handler ArgumentNull : |{ex.ParamName}| {ex.Message}" );
                Sup.LogTraceErrorMessage( "Exiting - check log file" );
                Environment.Exit( 0 );
            }
            catch ( Exception ex )
            {
                Sup.LogTraceErrorMessage( $"Exception Unknown : {ex.Message}" );
                Sup.LogTraceErrorMessage( $"Data (cont): {ex.Source}" );
                Sup.LogTraceErrorMessage( $"Data: {ex.StackTrace}" );
                Sup.LogTraceErrorMessage( "Exiting - check log file" );
                Environment.Exit( 0 );
                throw;
            }
            finally
            {
                Sup.LogDebugMessage( $"All done, Entering the finally section...; Closing down." );

                if ( FtpListener != null )
                {
                    Sup.LogDebugMessage( "Disposing FtpListener..." );
                    FtpListener.Dispose();
                }

                Sup.LogDebugMessage( "Disposing Isup..." );
                Isup.Dispose();

                Sup.LogDebugMessage( "Disposing Sup..." );
                Sup.Dispose();
            }

            Sup.LogDebugMessage( "Ready..." );
            return;
        }

        private async Task RealMainAsync( string[] args )  // 
        {
            Dayfile ThisDayfile;

            Stopwatch watch;
            Stopwatch OverallWatch = Stopwatch.StartNew();

            // Here we start the actual program Handle commandline arguments
            CommandLineArgs( args );

            if ( !DoPwsFWI && !DoTop10 && !DoSystemChk && !DoGraphs && !DoCreateMap && !DoYadr && !DoRecords && !DoCompileOnly && !DoUserAskedData &&
                !DoNOAA && !DoDayRecords && !DoCheckOnly && !DoWebsite && !DoForecast && !DoUserReports && !DoStationMap && !DoMeteoCam && !DoAirLink && !DoExtraSensors )
            {
                Sup.LogTraceErrorMessage( "CumulusUtils : No Arguments, nothing to do. Exiting." );
                Sup.LogTraceErrorMessage( "CumulusUtils : Exiting Main" );

                Console.WriteLine( "\nCumulusUtils : No Arguments nothing to do. Exiting. See Manual.\n" );
                Console.WriteLine( "CumulusUtils Usage : utils/bin/cumulusutils.exe [args] (args case independent):\n" );
                Console.WriteLine( "  utils/bin/cumulusutils.exe \n" +
                                  "      [SysInfo][Forecast][StationMap][UserReports][MeteoCam]\n" +
                                  "      [pwsFWI][Top10][Graphs][Yadr][Records][UserAskedData]\n" +
                                  "      [NOAA][DayRecords][AirLink][CompileOnly][ExtraSensors]\n" +
                                  //"      | [Thrifty] All\n" +
                                  "      | CheckOnly" );
                Console.WriteLine( "" );
                Console.WriteLine( "OR (in case you use the website generator):\n" );
                Console.WriteLine( "  utils/bin/cumulusutils.exe [Thrifty] Website\n" );

                Environment.Exit( 0 );
            }

            // Now we're going
            //
            DoAirLink &= HasAirLink;

            ThisDayfile = new Dayfile( Sup );
            MainList = ThisDayfile.DayfileRead();
            ThisDayfile.Dispose();

            // Reading the Monthly logfile has no Async and is independent of InetSupport!!
            if ( DoCheckOnly )
            {
                watch = Stopwatch.StartNew();

                CheckOnlyAsked = DoCheckOnly;

                Monthfile fncs = new Monthfile( Sup );
                fncs.ReadMonthlyLogs();
                fncs.Dispose();
                watch.Stop();

                Sup.LogTraceInfoMessage( $"Timing DocheckOnly = {watch.ElapsedMilliseconds} ms" );
                Sup.LogTraceInfoMessage( "CheckOnly Done" );
                Sup.LogTraceInfoMessage( "Main CmulusUtils: Exiting!" );

                return;
            }

            if ( DoSystemChk )
            {
                // Timing of the SysInfo
                watch = Stopwatch.StartNew();

                SysInfo fncs = new SysInfo( Sup, Isup );
                await fncs.GenerateSystemStatusAsync();
                fncs.Dispose();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of SysInfo generation = {watch.ElapsedMilliseconds} ms" );
            }

            if ( DoStationMap )
            {
                watch = Stopwatch.StartNew();

                StationMap fncs = new StationMap( Sup );
                fncs.GenerateStationMap();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of StationMap generation = {watch.ElapsedMilliseconds} ms" );
            }

            if ( DoMeteoCam )
            {
                watch = Stopwatch.StartNew();

                MeteoCam fncs = new MeteoCam( Sup );
                fncs.GenerateMeteoCam();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of MeteoCam generation = {watch.ElapsedMilliseconds} ms" );
            }

            if ( DoForecast )
            {
                watch = Stopwatch.StartNew();

                WeatherForecasts fncs = new WeatherForecasts( Sup, Isup );
                await fncs.GenerateForecasts();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of WeatherForecast generation = {watch.ElapsedMilliseconds} ms" );
            }

            if ( DoUserReports )
            {
                watch = Stopwatch.StartNew();

                // This function does its own uploads immediately as it has the filenames and it is assumed they contain daily relevant info
                // If not than we must consider later. 
                // If no reports exist, nothing is done. If run as a module you can see it as an independent Webtag replacer but similar to what CMX does.
                UserReports fncs = new UserReports( Sup, Isup );
                await fncs.DoUserReports();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of USerReports generation = {watch.ElapsedMilliseconds} ms" );
            }

            if ( DoAirLink )
            {
                watch = Stopwatch.StartNew();

                AirLink fncs = new AirLink( Sup );
                fncs.DoAirLink();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of AirQuality generation = {watch.ElapsedMilliseconds} ms" );
            }

            if ( DoExtraSensors && HasExtraSensors )
            {
                watch = Stopwatch.StartNew();
                ExtraSensors fncs = new ExtraSensors( Sup );

                fncs.DoExtraSensors();
                if ( ParticipatesSensorCommunity )
                    fncs.CreateSensorCommunityMapIframeFile();

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of ExtraSensors generation = {watch.ElapsedMilliseconds} ms" );
            }

            // These were the tasks without [weather]data.
            // Now do the datadriven tasks
            //
            if ( DoPwsFWI || DoTop10 || DoGraphs || DoYadr || DoRecords || DoNOAA || DoDayRecords || DoCheckOnly || DoWebsite || DoCreateMap || DoUserAskedData )
            {
                StartOfObservations = MainList.Select( x => x.ThisDate ).Min();
                const int NrOfDaysForUsefulResults = 32;

                if ( MainList.Count < NrOfDaysForUsefulResults )
                {
                    Sup.LogTraceInfoMessage( $" Main CmulusUtils: Not enough data. Only {MainList.Count} lines in dayfile.txt" );
                    Sup.LogTraceInfoMessage( $" Main CmulusUtils: Need at least {NrOfDaysForUsefulResults} days for useful output." );
                    Sup.LogTraceInfoMessage( "Main CmulusUtils: Exiting!" );
                    return; // not enough data
                }

                if ( DoPwsFWI )
                {
                    watch = Stopwatch.StartNew();

                    PwsFWI fncs = new PwsFWI( Sup, Isup );
                    await fncs.CalculatePwsFWI( MainList );
                    fncs.Dispose();

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of pwsFWI generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // YADR has no Async and is independent of InetSupport!!
                if ( DoYadr )
                {
                    watch = Stopwatch.StartNew();

                    Yadr fncs = new Yadr( Sup );
                    fncs.GenerateYadr( MainList );
                    fncs.Dispose();

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Yadr generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // Records has no Async and is independent of InetSupport!!
                //
                if ( DoRecords )
                {
                    watch = Stopwatch.StartNew();

                    Records fncs = new Records( Sup );
                    fncs.GenerateRecords( MainList );

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Records generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // DayRecords  has no Async and is independent of InetSupport!!
                //
                if ( DoDayRecords )
                {
                    watch = Stopwatch.StartNew();

                    DayRecords fncs = new DayRecords( Sup );
                    fncs.GenerateDayRecords( MainList );

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of DayRecords generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // NOAA has no Async and is independent of InetSupport!!
                //
                if ( DoNOAA && ( !Thrifty || RunStarted.Day == 2 ) )
                {
                    watch = Stopwatch.StartNew();

                    NOAAdisplay fncs = new NOAAdisplay( Sup );
                    fncs.GenerateNOAATxtfile( MainList );

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of NOAA reader generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // Graphs has no Async and is independent of InetSupport!!
                if ( DoGraphs )
                {
                    watch = Stopwatch.StartNew();

                    Graphx fncs = new Graphx( MainList, Sup );
                    fncs.GenerateGraphx( MainList );
                    fncs.Dispose();

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Graphs generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // Top10 has no Async and is independent of InetSupport!!
                // This call must always be the last because it changes the sorting of the MainList
                //
                if ( DoTop10 )
                {
                    watch = Stopwatch.StartNew();

                    Top10 fncs = new Top10( Sup );
                    fncs.GenerateTop10List( MainList );
                    fncs.Dispose();

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Top10 generation = {watch.ElapsedMilliseconds} ms" );
                }

                if ( DoWebsite )
                {
                    watch = Stopwatch.StartNew();

                    Website fncs = new Website( Sup, Isup );
                    await fncs.GenerateWebsite();
                    fncs.CheckPackageAndCopy();

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Website generation = {watch.ElapsedMilliseconds} ms" );
                }

                //
                // Maps is done here to prevent it being done every sysinfo or other dataindependent module!!
                //

                if ( CanDoMap && MapParticipant || DoWebsite )
                {
                    string retval;

                    watch = Stopwatch.StartNew();
                    Maps fncs = new Maps( Sup );

                    retval = await fncs.MapsOn();
                    Sup.LogTraceInfoMessage( retval );

                    if ( DoCreateMap && File.Exists( "paMuCetaerCyaM.txt" ) )
                        fncs.CreateMap();
                    else if ( !File.Exists( "paMuCetaerCyaM.txt" ) )  // 
                    {
                        Sup.LogTraceInfoMessage( $"Fetch Map: Fetching the generated map" );

                        retval = await Isup.GetUrlDataAsync( new Uri( "https://meteo-wagenborgen.nl/maps.txt" ) );

                        if ( !string.IsNullOrEmpty( retval ) )
                        {
                            if ( retval.Length > 50 )
                                Sup.LogTraceInfoMessage( $"Main: {retval.Substring( 0, 50 )}" );
                            else
                                Sup.LogTraceInfoMessage( $"Main: {retval}" );

                            File.WriteAllText( $"{ Sup.PathUtils}{ Sup.MapsOutputFilename}", retval, Encoding.UTF8 );

                            //The Map is always downloaded without the jQuery include. If required add it here
                            const string tmpMap = "tmpMaps.txt";
                            string jQueryString = Sup.GenjQueryIncludestring();

                            if ( !string.IsNullOrEmpty( jQueryString ) )
                            {
                                Sup.LogTraceInfoMessage( $"Fetch Map: Adding jQuery to the downloaded map" );

                                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{tmpMap}", false, Encoding.UTF8 ) )
                                {
                                    of.WriteLine( $"{jQueryString}" );

                                    using ( StreamReader MapFile = new StreamReader( $"{Sup.PathUtils}{Sup.MapsOutputFilename}", Encoding.UTF8 ) )
                                    {
                                        do
                                        {
                                            string line = MapFile.ReadLine();
                                            of.WriteLine( line );
                                        }
                                        while ( !MapFile.EndOfStream );
                                    }
                                } // Done copying with the required jQuery string

                                // Remove the just downloaded Map and replace it with the one which has the jQuery library included
                                File.Delete( $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );
                                File.Move( $"{Sup.PathUtils}{tmpMap}", $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );

                                Sup.LogTraceInfoMessage( $"Fetch Map: Added jQuery library to the Map." );
                            } // Should we include the jQuery library?
                        } // Did the map.txt download correctly?
                        else
                            Sup.LogTraceErrorMessage( "Fetch Map from server: Fail... empty map." );
                    }

                    fncs.Dispose();
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Map generation = {watch.ElapsedMilliseconds} ms" );
                }

                if ( DoUserAskedData )
                {
                    Sup.LogTraceInfoMessage( $"UserAskedData Start..." );
                    watch = Stopwatch.StartNew();

                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the compiler stuff..." );
                        List<ChartDef> tmpChartsList = new List<ChartDef>();

                        ChartsCompiler fncs = new ChartsCompiler( Sup );

                        // USerAskedData is created with a complete ChartsList so create the chartslist from all separate OutputDefs.
                        // It's a bit awkward to separate charts in different lists first and then reassemble but I see no other way.
                        List<OutputDef> theseOutputs = fncs.ParseChartDefinitions();

                        if ( theseOutputs != null )
                        {
                            foreach ( OutputDef thisOutput in theseOutputs )
                            {
                                if ( !thisOutput.Filename.Equals( Sup.ExtraSensorsCharts ) )
                                    foreach ( ChartDef tmpChart in thisOutput.TheseCharts )
                                        tmpChartsList.Add( tmpChart );
                            }

                            fncs.GenerateUserAskedData( tmpChartsList );
                        }
                    }

                    Sup.LogDebugMessage( $"DoAirLink / AirQualitySensor  = { DoAirLink } / {HasAirLink}" );
                    if ( HasAirLink )
                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the AirQuality stuff..." );
                        AirLink fncs = new AirLink( Sup );
                        fncs.GenAirLinkDataJson();
                    }

                    if ( HasExtraSensors )
                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the ExtraSensor stuff..." );
                        ExtraSensors fncs = new ExtraSensors( Sup );
                        fncs.GenerateExtraSensorDataJson();
                    }

                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of UserAskedData = {watch.ElapsedMilliseconds} ms" );
                }
            }

            // Make this the last part to be able to overwrite default file output for the graphs when that will be implemented
            //
            if ( DoCompileOnly && !Thrifty )
            {
                List<OutputDef> thisList;

                watch = Stopwatch.StartNew();

                ChartsCompiler fncs = new ChartsCompiler( Sup );
                thisList = fncs.ParseChartDefinitions();

                if ( thisList != null )
                {
                    int i = 0;

                    foreach ( OutputDef thisDef in thisList )
                    {
                        // Generate
                        fncs.GenerateUserDefinedCharts( thisDef.TheseCharts, thisDef.Filename, i++ );

                        // and Upload
                        Sup.LogTraceInfoMessage( $"Uploading = {thisDef.Filename}" );
                        Isup.UploadFile( $"{thisDef.Filename}", $"{Sup.PathUtils}{thisDef.Filename}" );
                    }
                }
                else
                {
                    Sup.LogTraceErrorMessage( $"Errors in Charts definition. See logfile, please correct and run again." );
                }

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of Compile and Generate CumulusCharts = {watch.ElapsedMilliseconds} ms" );
            }

            //********************************  Do the uploading when required **************************************
            //
            if ( DoPwsFWI )
            {
                Sup.LogTraceInfoMessage( $"Uploading = {Sup.PwsFWIOutputFilename}" );
                Isup.UploadFile( $"{Sup.PwsFWIOutputFilename}", $"{Sup.PathUtils}{Sup.PwsFWIOutputFilename}" );

                Sup.LogTraceInfoMessage( $"Uploading = {Sup.PwsFWICurrentOutputFilename}" );
                Isup.UploadFile( $"{Sup.PwsFWICurrentOutputFilename}", $"{Sup.PathUtils}{Sup.PwsFWICurrentOutputFilename}" );
            }

            if ( DoTop10 && ( !Thrifty || ThriftyTop10RecordsDirty ) )
            {
                Sup.LogTraceInfoMessage( $"Thrifty: DoTop10 && (!Thrifty || ThriftyTop10RecordsDirty ) - " +
                  $"{DoTop10 && ( !Thrifty || ThriftyTop10RecordsDirty )} | Uploading = {Sup.Top10OutputFilename}" );
                Isup.UploadFile( $"{ Sup.Top10OutputFilename}", $"{Sup.PathUtils}{Sup.Top10OutputFilename}" );
            }
            //else Sup.LogTraceInfoMessage($"Thrifty: DoTop10 && (!Thrifty || ThriftyTop10RecordsDirty ) - " +
            //  $"{DoTop10 && (!Thrifty || ThriftyTop10RecordsDirty)} => Uploading Top10 None");

            if ( DoGraphs )
            { // 
                if ( HasRainGraphMenu && ( !Thrifty || ThriftyRainGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyRainGraphsDirty - {!Thrifty || ThriftyRainGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsRainOutputFilename )}" );
                    Isup.UploadFile( Path.GetFileName( Sup.GraphsRainOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsRainOutputFilename ) );
                }
                //else Sup.LogTraceInfoMessage($"Thrifty: !Thrifty || ThriftyRainGraphsDirty - {!Thrifty || ThriftyRainGraphsDirty} => {Sup.GraphsRainOutputFilename} not uploaded");

                if ( HasTempGraphMenu && ( !Thrifty || ThriftyTempGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyTempGraphsDirty - {!Thrifty || ThriftyTempGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsTempOutputFilename )}" );
                    Isup.UploadFile( Path.GetFileName( Sup.GraphsTempOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsTempOutputFilename ) );
                }
                //else Sup.LogTraceInfoMessage($"Thrifty: !Thrifty || ThriftyTempGraphsDirty - {!Thrifty || ThriftyTempGraphsDirty} => {Sup.GraphsTempOutputFilename} not uploaded");

                if ( HasWindGraphMenu && ( !Thrifty || ThriftyWindGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyWindGraphsDirty) - {!Thrifty || ThriftyWindGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsWindOutputFilename )}" );
                    Isup.UploadFile( Path.GetFileName( Sup.GraphsWindOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsWindOutputFilename ) );
                }
                //else Sup.LogTraceInfoMessage($"Thrifty: !Thrifty || ThriftyWindGraphsDirty) - {!Thrifty || ThriftyWindGraphsDirty} => {Sup.GraphsWindOutputFilename} not uploaded");

                if ( HasSolarGraphMenu && ( !Thrifty || ThriftySolarGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftySolarGraphsDirty) - {!Thrifty || ThriftySolarGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsSolarOutputFilename )}" );
                    Isup.UploadFile( Path.GetFileName( Sup.GraphsSolarOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsSolarOutputFilename ) );
                }
                //else Sup.LogTraceInfoMessage($"Thrifty: !Thrifty || ThriftySolarGraphsDirty) - {!Thrifty || ThriftySolarGraphsDirty} => {Sup.GraphsSolarOutputFilename} not uploaded");

                if ( HasMiscGraphMenu && ( !Thrifty || ThriftyMiscGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyMiscGraphsDirty - {!Thrifty || ThriftyMiscGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsMiscOutputFilename )}" );
                    Isup.UploadFile( Path.GetFileName( Sup.GraphsMiscOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsMiscOutputFilename ) );
                }
                //else Sup.LogTraceInfoMessage($"Thrifty: !Thrifty || ThriftyMiscGraphsDirty - {!Thrifty || ThriftyMiscGraphsDirty} => {Sup.GraphsMiscOutputFilename} not uploaded");
            }

            if ( MapParticipant || DoWebsite )
            {
                Sup.LogTraceInfoMessage( $"Uploading = {Sup.MapsOutputFilename}" );
                Isup.UploadFile( $"{Sup.MapsOutputFilename}", $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );
            }

            if ( DoRecords && ( !Thrifty || ThriftyRecordsDirty ) )
            {
                Sup.LogTraceInfoMessage( $"Thrifty: DoRecords && (!Thrifty || ThriftyRecordsDirty) - {DoRecords && ( !Thrifty || ThriftyRecordsDirty )} => Uploading = {Sup.RecordsOutputFilename}" );
                Isup.UploadFile( $"{Sup.RecordsOutputFilename}", $"{Sup.PathUtils}{Sup.RecordsOutputFilename}" );
            }
            // else Sup.LogTraceInfoMessage($"Thrifty: DoRecords && (!Thrifty || ThriftyRecordsDirty) - {DoRecords && (!Thrifty || ThriftyRecordsDirty)} => Uploading Records None");

            if ( DoNOAA && ( !Thrifty || RunStarted.Day == 2 ) ) // Only useful on second day of month
            {
                Sup.LogTraceInfoMessage( $"Thrifty: DoNOAA && (!Thrifty || RunStarted.Day == 2) - {!Thrifty || RunStarted.Day == 2} => Uploading = {Sup.NOAAOutputFilename}" );
                Isup.UploadFile( $"{Sup.NOAAOutputFilename}", $"{Sup.PathUtils}{Sup.NOAAOutputFilename}" );
            }
            // else Sup.LogTraceInfoMessage($"Thrifty: DoNOAA && (!Thrifty || RunStarted.Day == 2) - {DoNOAA && (!Thrifty || RunStarted.Day == 2)} => Uploading NOAA None");

            if ( DoDayRecords && ( !Thrifty || ThriftyDayRecordsDirty ) )
            {
                Sup.LogTraceInfoMessage( $"Thrifty: DoDayRecords && (!Thrifty || ThriftyDayRecordsDirty) - {DoDayRecords && ( !Thrifty || ThriftyDayRecordsDirty )} => Uploading = {Sup.DayRecordsOutputFilename}" );
                Isup.UploadFile( $"{Sup.DayRecordsOutputFilename}", $"{Sup.PathUtils}{Sup.DayRecordsOutputFilename}" );
            }
            // else Sup.LogTraceInfoMessage($"Thrifty: DoDayRecords && (!Thrifty || ThriftyDayRecordsDirty) - {DoDayRecords && (!Thrifty || ThriftyDayRecordsDirty)} => Uploading DayRecords None");

            if ( DoForecast ) { Isup.UploadFile( $"{Sup.ForecastOutputFilename}", $"{Sup.PathUtils}{Sup.ForecastOutputFilename}" ); }

            if ( DoStationMap && ( !Thrifty || RunStarted.DayOfYear == 2 ) ) { Isup.UploadFile( $"{Sup.StationMapOutputFilename}", $"{Sup.PathUtils}{Sup.StationMapOutputFilename}" ); }
            if ( DoMeteoCam && HasMeteoCamMenu && ( !Thrifty || RunStarted.DayOfYear == 2 ) ) { Isup.UploadFile( $"{Sup.MeteoCamOutputFilename}", $"{Sup.PathUtils}{Sup.MeteoCamOutputFilename}" ); }

            if ( DoAirLink && !Thrifty )
            {
                Isup.UploadFile( $"{Sup.AirLinkOutputFilename}", $"{Sup.PathUtils}{Sup.AirLinkOutputFilename}" );
            }

            if ( DoExtraSensors && HasExtraSensors && !Thrifty )
            {
                Isup.UploadFile( $"{Sup.ExtraSensorsOutputFilename}", $"{Sup.PathUtils}{Sup.ExtraSensorsOutputFilename}" );
                if ( ParticipatesSensorCommunity )
                    Isup.UploadFile( $"{Sup.SensorCommunityOutputFilename}", $"{Sup.PathUtils}{Sup.SensorCommunityOutputFilename}" );
            }

            if ( DoYadr )
            {
                if ( !Thrifty )
                {
                    string[] filelist = Directory.GetFiles( Sup.PathUtils, "Yadr*.txt" );

                    Sup.LogTraceInfoMessage( $"Thrifty: {Thrifty} - YADR - Complete upload" );

                    foreach ( string file in filelist )
                    {
                        Isup.UploadFile( Path.GetFileName( file ), Sup.PathUtils + Path.GetFileName( file ) );
                    }
                }
                else
                {
                    string[] filelist;

                    if ( RunStarted.DayOfYear == 1 )
                        filelist = Directory.GetFiles( Sup.PathUtils, $"Yadr*{RunStarted.Year - 1}.txt" );
                    else
                        filelist = Directory.GetFiles( Sup.PathUtils, $"Yadr*{RunStarted.Year}.txt" );

                    if ( RunStarted.DayOfYear == 2 )
                    {
                        Sup.LogTraceInfoMessage( $"Thrifty: {Thrifty} - YADR - Upload for 2 January" );
                        Isup.UploadFile( Path.GetFileName( "Yadr.txt" ), Sup.PathUtils + Path.GetFileName( "Yadr.txt" ) );
                    }

                    Sup.LogTraceInfoMessage( $"Thrifty: {Thrifty} - YADR - Upload for only current year {RunStarted.Year}" );
                    foreach ( string file in filelist )
                    {
                        Isup.UploadFile( Path.GetFileName( file ), Sup.PathUtils + Path.GetFileName( file ) );
                    }
                }
            }

            // This block takes care  of the JSON upload (if any JSON present).
            // This is unconditional.
            // JSONs will be deleted after upload else they remain.
            {
                // Now upload the JSON files if any
                string[] files = Directory.GetFiles( $"{Sup.PathUtils}", $"*.json" );

                foreach ( string file in files )
                {
                    FileInfo fi = new FileInfo( file );

                    Sup.LogTraceInfoMessage( $"Uploading => {fi.Name} from {Sup.PathUtils}{fi.Name}" );
                    if ( Isup.UploadFile( $"{fi.Name}", $"{Sup.PathUtils}{fi.Name}" ) )
                        fi.Delete();
                    // else leave the files so they can be uploaded manually
                }
            }

            // Before v4.0.0 SysInfo must be processed by Cumulus so not handy to do the uploading here but now we do
            if ( DoSystemChk ) { Isup.UploadFile( $"{Sup.SysInfoOutputFilename}", $"{Sup.PathUtils}{Sup.SysInfoOutputFilename}" ); }

            OverallWatch.Stop();
            Sup.LogTraceInfoMessage( $"Overall Timing all Modules = {OverallWatch.ElapsedMilliseconds} ms" );

            return;
        }

        private void CommandLineArgs( string[] args )
        {
            Sup.LogDebugMessage( "CommandLineArgs : starting" );

            foreach ( string s in args )
            {
                Sup.LogDebugMessage( $" CommandLineArgs : handling arg: {s}" );

                if ( s.Equals( "Website", StringComparison.OrdinalIgnoreCase ) )
                {
                    DoSystemChk = true;
                    DoTop10 = true;
                    DoPwsFWI = true;
                    DoGraphs = true;
                    DoYadr = true;
                    DoRecords = true;
                    DoNOAA = true;
                    DoDayRecords = true;
                    DoWebsite = true;
                    DoForecast = true;
                    DoUserReports = true;
                    DoStationMap = true;
                    DoMeteoCam = true;
                    DoAirLink = true;
                    //DoCompileOnly = true; // This is done during website generation so is not required here, would be double work
                    DoExtraSensors = true;

                    break;
                }
                //else if ( s.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
                //{
                //    DoSystemChk = true;
                //    DoTop10 = true;
                //    DoPwsFWI = true;
                //    DoGraphs = true;
                //    DoYadr = true;
                //    DoRecords = true;
                //    DoNOAA = true;
                //    DoDayRecords = true;
                //    DoForecast = true;
                //    DoUserReports = true;
                //    DoStationMap = true;
                //    DoMeteoCam = true;
                //    DoAirLink = true;
                //    DoCompileOnly = true;
                //    DoExtraSensors = true;

                //    break;
                //}
                else
                {
                    if ( s.Equals( "Thrifty", StringComparison.OrdinalIgnoreCase ) )
                    {
                        Thrifty = true;
                    }
                    else
                    {
                        if ( s.Equals( "Top10", StringComparison.OrdinalIgnoreCase ) )
                            DoTop10 = true;
                        if ( s.Equals( "pwsFWI", StringComparison.OrdinalIgnoreCase ) )
                            DoPwsFWI = true;
                        if ( s.Equals( "Sysinfo", StringComparison.OrdinalIgnoreCase ) )
                            DoSystemChk = true;
                        if ( s.Equals( "Graphs", StringComparison.OrdinalIgnoreCase ) )
                            DoGraphs = true;
                        if ( s.Equals( "CreateMap", StringComparison.OrdinalIgnoreCase ) )
                            DoCreateMap = true;    // Undocumented feature only for the keeper of the map
                        if ( s.Equals( "Yadr", StringComparison.OrdinalIgnoreCase ) )
                            DoYadr = true;
                        if ( s.Equals( "Records", StringComparison.OrdinalIgnoreCase ) )
                            DoRecords = true;
                        if ( s.Equals( "NOAA", StringComparison.OrdinalIgnoreCase ) )
                            DoNOAA = true;
                        if ( s.Equals( "DayRecords", StringComparison.OrdinalIgnoreCase ) )
                            DoDayRecords = true;
                        if ( s.Equals( "CheckOnly", StringComparison.OrdinalIgnoreCase ) )
                            DoCheckOnly = true;
                        if ( s.Equals( "Forecast", StringComparison.OrdinalIgnoreCase ) )
                            DoForecast = true;
                        if ( s.Equals( "UserReports", StringComparison.OrdinalIgnoreCase ) )
                            DoUserReports = true;
                        if ( s.Equals( "StationMap", StringComparison.OrdinalIgnoreCase ) )
                            DoStationMap = true;
                        if ( s.Equals( "MeteoCam", StringComparison.OrdinalIgnoreCase ) )
                            DoMeteoCam = true;
                        if ( s.Equals( "AirLink", StringComparison.OrdinalIgnoreCase ) )
                            DoAirLink = true;
                        if ( s.Equals( "CompileOnly", StringComparison.OrdinalIgnoreCase ) )
                            DoCompileOnly = true;
                        if ( s.Equals( "ExtraSensors", StringComparison.OrdinalIgnoreCase ) )
                        {
                            DoExtraSensors = true;
                            DoCompileOnly = true;  // Implicit for Extra Sensors
                        }
                        if ( s.Equals( "UserAskedData", StringComparison.OrdinalIgnoreCase ) )
                            DoUserAskedData = true;
                    }
                }
            }

            Sup.LogDebugMessage( "CommandLineArgs : End" );
        } // Commandline handling
    } // Class CMXutils
} // namespace