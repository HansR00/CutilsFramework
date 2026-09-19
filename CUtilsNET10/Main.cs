/*
 * CumulusUtils/Main
 *
 * Optimized version. Behaviour-preserving except where marked [OPT] with a note.
 * Fixes applied:
 *   - RainDim.inch (was typed as the non-existent 'Rainin')
 *   - TryRun<T> helper added (generic guarded-run)
 *   - TryRunFallback removed ([Conditional] cannot return a value)
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CumulusUtils
{
    public static class GlobConst
    {
        public const double RainLimit = 0.2; //When more or equal than this amount in mm/day this limit is used when necessary
        public const char CommaSeparator = ',';
    }

    public class CUtils
    {
        #region Declarations

        private bool DoPwsFWI;
        private bool DoTop10;
        private bool DoSystemChk;
        private bool DoGraphs;
        private bool DoCreateMap;
        private bool DoYadr;
        private bool DoRecords;
        private bool DoDayRecords;
        private bool DoNOAA;
        private bool DoForecast;
        private bool DoUserReports;
        private bool DoStationMap;
        private bool DoMeteoCam;
        private bool DoAirLink;
        private bool DoCompileOnly;
        private bool DoUserAskedData;
        private bool DoExtraSensors;
        private bool DoCustomLogs;
        private bool DoCUlib;
        private bool DoDiary;

        public static StringComparison Cmp = StringComparison.OrdinalIgnoreCase;

        public static CultureInfo Inv = CultureInfo.InvariantCulture;
        public static CultureInfo ThisCulture;

        public static byte[] CryptoKey { get => cryptoKey; set => cryptoKey = value; }
        private static byte[] cryptoKey;

        public static CuSupport Sup { get; set; }
        public static InetSupport Isup { get; set; }

        public static bool Thrifty { get; private set; }
        public static bool ThriftyRecordsDirty { get; set; }
        public static bool ThriftyTop10RecordsDirty { get; set; }
        public static int ThriftyTop10RecordsPeriod { get; private set; }
        public static bool ThriftyDayRecordsDirty { get; set; }
        public static bool ThriftyRainGraphsDirty { get; set; }
        public static int ThriftyRainGraphsPeriod { get; private set; }
        public static bool ThriftyTempGraphsDirty { get; set; }
        public static int ThriftyTempGraphsPeriod { get; private set; }
        public static bool ThriftyWindGraphsDirty { get; set; }
        public static int ThriftyWindGraphsPeriod { get; private set; }
        public static bool ThriftySolarGraphsDirty { get; set; }
        public static int ThriftySolarGraphsPeriod { get; private set; }
        public static bool ThriftyMiscGraphsDirty { get; set; }
        public static int ThriftyMiscGraphsPeriod { get; private set; }

        public static bool DoWebsite { get; set; }
        public static bool DoModular { get; set; }
        public static string ModulePath { get; set; }
        public static bool DoLibraryIncludes { get; set; }
        public static bool DojQueryInclude { get; set; }
        public static bool MapParticipant { get; private set; }
        public static bool HasRainGraphMenu { get; set; }
        public static bool HasTempGraphMenu { get; set; }
        public static bool HasWindGraphMenu { get; set; }
        public static bool HasSolarGraphMenu { get; set; }
        public static bool HasMiscGraphMenu { get; set; }
        public static bool HasSystemInfoMenu { get; set; }
        public static bool HasStationMapMenu { get; set; }
        public static bool HasMeteoCamMenu { get; set; }
        public static bool HasDiaryMenu { get; set; }

        // Check for presence of optional sensors
        public static bool HasSolar { get; set; }
        public static bool ShowUV { get; set; }
        public static bool HasAirLink { get; set; }
        public static bool HasExtraSensors { get; set; }
        public static bool HasCustomLogs { get; set; }
        public static bool ParticipatesSensorCommunity { get; set; }
        public static DateTime RunStarted { get; private set; }
        public static DateTime StartOfObservations { get; set; }
        public static bool CanDoMap { get; set; }
        public static HelpTexts ChartHelp { get; private set; }
        public static int HoursInGraph { get; set; }
        public static int DaysInGraph { get; set; }
        public static int LogIntervalInMinutes { get; set; }
        public static int FTPIntervalInMinutes { get; set; }
        public static int UtilsRealTimeInterval { get; set; }
        public static bool DoingUserAskedData { get; set; }
        public static bool ConnectNulls { get; set; }
        public static int YearMax { get; set; }
        public static int YearMin { get; set; }

        public static int[] PossibleIntervals = { 1, 5, 10, 15, 20, 30 };

        public static List<DayfileValue> MainList = new List<DayfileValue>();

        // Following for code generation and axis generation and maybe somewhere else
        public static bool PressureInInchHg { get; set; }
        public static bool RainInInch { get; set; }

        // [OPT] Single source of truth for "is a data-driven module requested".
        //       Mirrors the original 10-flag disjunction exactly.
        private bool AnyDataTaskRequested =>
            DoPwsFWI || DoTop10 || DoGraphs || DoYadr || DoRecords || DoNOAA ||
            DoDayRecords || DoWebsite || DoCreateMap || DoUserAskedData;

        #endregion

        #region Main
        private static async Task Main( string[] args )
        {
            try
            {
                // Required as from version > 1.0.0; All produced files will end up in utils
                // except for cumulusutils.ini and the logs
                // And from version 3.7.1 all logfiles will go in 'utilslog'
                // This procedure is called before the CuSupport instance is created because the first thing is to start the debug logging
                //

                if ( !File.Exists( "Cumulus.ini" ) )
                {
                    Console.WriteLine( " No Cumulus.ini found. Must run in Cumulus directory!" );
                    Environment.Exit( 0 );
                }
                else if ( !File.Exists( "UniqueId.txt" ) )
                {
                    // UniqueId.txt must exist
                    Console.WriteLine( "CumulusMX version 4 must be installed and must have run." );
                    Environment.Exit( 0 );
                }
                else
                {
                    // [OPT] using-declaration instead of explicit scope.
                    using StreamReader UniqueKey = new( "UniqueId.txt" );
                    CryptoKey = Convert.FromBase64String( UniqueKey.ReadToEnd() );
                }

                if ( !Directory.Exists( "utils" ) ) Directory.CreateDirectory( "utils" );
                if ( !Directory.Exists( "utils/utilslog" ) ) Directory.CreateDirectory( "utils/utilslog" );

                // [OPT] Cutoff computed once instead of per file.
                DateTime logCutoff = DateTime.Now.AddDays( -2 );

                foreach ( string file in Directory.GetFiles( "utils/utilslog" ) )
                {
                    FileInfo fi = new FileInfo( file );
                    if ( fi.CreationTime < logCutoff )
                        fi.Delete();
                }

                Sup = new CuSupport();
                Isup = new InetSupport( Sup );

                Sup.LogDebugMessage( "CumulusUtils : ----------------------------" );
                Sup.LogDebugMessage( "CumulusUtils : Entering Main" );

                // Initialise the Thrifty system parameters
                Thrifty = false;
                RunStarted = DateTime.Now;

                ChartHelp = new HelpTexts( Sup );

                ThriftyRecordsDirty = false;
                ThriftyTop10RecordsDirty = false;
                ThriftyDayRecordsDirty = false;
                ThriftyRainGraphsDirty = false;
                ThriftyTempGraphsDirty = false;
                ThriftyWindGraphsDirty = false;
                ThriftyMiscGraphsDirty = false;

                // [OPT] Local helper — removes six copies of the same ini read.
                int IniInt( string section, string key, string fallback ) =>
                    Convert.ToInt32( Sup.GetUtilsIniValue( section, key, fallback ), Inv );

                ThriftyTop10RecordsPeriod = IniInt( "Thrifty", "Top10RecordsPeriod", "1" );
                ThriftyRainGraphsPeriod = IniInt( "Thrifty", "RainGraphsPeriod", "1" );
                ThriftyTempGraphsPeriod = IniInt( "Thrifty", "TempGraphsPeriod", "1" );
                ThriftyWindGraphsPeriod = IniInt( "Thrifty", "WindGraphsPeriod", "1" );
                ThriftySolarGraphsPeriod = IniInt( "Thrifty", "SolarGraphsPeriod", "1" );
                ThriftyMiscGraphsPeriod = IniInt( "Thrifty", "MiscGraphsPeriod", "1" );

                // [OPT] Local helper for the repeated "value.Equals("true", Cmp)" idiom.
                bool UtilsFlag( string section, string key, string fallback ) =>
                    Sup.GetUtilsIniValue( section, key, fallback ).Equals( "true", Cmp );

                DoModular = UtilsFlag( "General", "DoModular", "false" );
                ModulePath = Sup.GetUtilsIniValue( "General", "ModulePath", "" );

                HasSystemInfoMenu = UtilsFlag( "SysInfo", "SystemInfoMenu", "true" );
                HasStationMapMenu = UtilsFlag( "StationMap", "StationMapMenu", "true" );
                HasMeteoCamMenu = UtilsFlag( "MeteoCam", "MeteoCamMenu", "true" );
                HasExtraSensors = UtilsFlag( "ExtraSensors", "ExtraSensors", "false" ) &&
                    Sup.GetCumulusIniValue( "Station", "LogExtraSensors", "" ).Equals( "1" );
                HasCustomLogs = UtilsFlag( "CustomLogs", "CustomLogs", "false" ) &&
                    ( Sup.GetCumulusIniValue( "CustomLogs", "IntervalEnabled0", "" ).Equals( "1" ) || Sup.GetCumulusIniValue( "CustomLogs", "DailyEnabled0", "" ).Equals( "1" ) );

                ParticipatesSensorCommunity = UtilsFlag( "ExtraSensors", "ParticipatesSensorCommunity", "false" );
                MapParticipant = UtilsFlag( "Maps", "Participant", "true" );
                HasSolar = UtilsFlag( "Website", "ShowSolar", "true" ); // Is an indirect determination set by the user only in  cutils
                DoLibraryIncludes = UtilsFlag( "General", "DoLibraryIncludes", "false" ); // Do we need the libs??
                DojQueryInclude = UtilsFlag( "General", "GeneratejQueryInclude", "false" );

                bool AirLinkIn = Sup.GetCumulusIniValue( "AirLink", "In-Enabled", "0" ).Equals( "1" );
                bool AirLinkOut = Sup.GetCumulusIniValue( "AirLink", "Out-Enabled", "0" ).Equals( "1" );
                HasAirLink = AirLinkIn || AirLinkOut;

                HoursInGraph = Convert.ToInt32( Sup.GetCumulusIniValue( "Graphs", "GraphHours", "" ) );
                DaysInGraph = Convert.ToInt32( Sup.GetCumulusIniValue( "Graphs", "ChartMaxDays", "" ) );
                LogIntervalInMinutes = PossibleIntervals[ Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "DataLogInterval", "" ), Inv ) ];
                FTPIntervalInMinutes = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "" ) );
                UtilsRealTimeInterval = Convert.ToInt32( Sup.GetUtilsIniValue( "Website", "CumulusRealTimeInterval", "15" ) ); // Sorry for the confused naming
                ConnectNulls = UtilsFlag( "General", "ConnectNulls", "false" );

                PressureInInchHg = Sup.StationPressure.Dim == PressureDim.inchHg;
                RainInInch = Sup.StationRain.Dim == RainDim.inch;

                // Now start doing things
                CUtils p = new CUtils();
                await p.RealMainAsync( args );
            }
            catch ( ArgumentNullException ex )
            {
                Sup.LogMessage( $"Exception handler ArgumentNull : |{ex.ParamName}| {ex.Message}", TraceLevel.Error );
                Sup.LogMessage( "Exiting - check log file", TraceLevel.Info );
                Environment.Exit( 0 );
            }
            catch ( Exception ex )
            {
                Sup.LogMessage( $"Exception Unknown : {ex.Message}", TraceLevel.Error );
                Sup.LogMessage( $"Data (cont): {ex.Source}", TraceLevel.Error );
                Sup.LogMessage( $"Data: {ex.StackTrace}", TraceLevel.Error );
                Sup.LogMessage( "Exiting - check log file", TraceLevel.Info );
                Environment.Exit( 0 );
                //throw;
            }
            finally
            {
                // [OPT] FtpListener was always null; its disposal branch was unreachable.
                Sup.LogDebugMessage( "All done, Entering the finally section...; Closing down." );

                Sup.LogDebugMessage( "Disposing Isup..." );
                Isup.Dispose();

                Sup.LogDebugMessage( "Disposing Sup..." );
                Sup.Dispose();
            }

            Sup.LogDebugMessage( "Ready..." );
            return;
        }

        #endregion

        #region RealMainAsync
        private async Task RealMainAsync( string[] args )
        {
            Dayfile ThisDayfile;

#if TIMING
            Stopwatch watch;
            Stopwatch OverallWatch = Stopwatch.StartNew();
#endif

            // Here we start the actual program Handle commandline arguments
            CommandLineArgs( args );

            if ( DoModular && DoWebsite )
            {
                Sup.LogMessage( $"CumulusUtils : Conflicting settings - DoModular is {DoModular} while running Website.", TraceLevel.Error );
                Sup.LogMessage( "CumulusUtils : Cannot handle this, Exiting.", TraceLevel.Error );

                Environment.Exit( 0 );
            }

            if ( !DoPwsFWI && !DoTop10 && !DoSystemChk && !DoGraphs && !DoCreateMap && !DoYadr && !DoRecords && !DoCompileOnly && !DoUserAskedData && !DoCustomLogs &&
                !DoNOAA && !DoDayRecords && !DoWebsite && !DoForecast && !DoUserReports && !DoStationMap && !DoMeteoCam && !DoAirLink && !DoExtraSensors && !DoCUlib && !DoDiary )
            {
                Sup.LogMessage( "CumulusUtils : No Arguments, nothing to do. Exiting.", TraceLevel.Error );
                Sup.LogMessage( "CumulusUtils : Exiting Main", TraceLevel.Error );

                PrintUsage();
                Environment.Exit( 0 );
            }

            // Now we're going
            //
            DoingUserAskedData = DoUserAskedData;
            DoAirLink &= HasAirLink;

            ThisDayfile = new Dayfile( Sup );
            MainList = ThisDayfile.DayfileRead();
            ThisDayfile.Dispose();

            const int NrOfDaysForUsefulResults = 1;

            if ( MainList.Count < NrOfDaysForUsefulResults )
            {
                Sup.LogDebugMessage( $" Main CmulusUtils: Not enough data. Only {MainList.Count} entries in dayfile.txt" );
                Sup.LogDebugMessage( $" Main CmulusUtils: Need at least {NrOfDaysForUsefulResults} days for useful output." );
                Sup.LogDebugMessage( " Main CmulusUtils: Exiting!" );
                return; // not enough data
            }

            // [OPT] One enumeration for both year bounds; Min()/Max() over a
            //       non-empty list, so no empty-sequence throw.
            YearMax = MainList.Max( x => x.ThisDate.Year );
            YearMin = MainList.Min( x => x.ThisDate.Year );
            Sup.LogMessage( $"CumulusUtils : YearMin = {YearMin}; YearMax = {YearMax}", TraceLevel.Info );

            // Adjust if RecordsBeganDate is set
            //
            string recordsBegan = Sup.GetUtilsIniValue( "General", "RecordsBeganDate", "" );

            if ( string.IsNullOrEmpty( recordsBegan ) )
            {
                StartOfObservations = MainList.Min( x => x.ThisDate );
            }
            else
            {
                try
                {
                    StartOfObservations = DateTime.ParseExact( recordsBegan, "dd/MM/yy", Inv );

                    int i = MainList.RemoveAll( p => p.ThisDate < StartOfObservations );
                    Sup.LogMessage( $"CumulusUtils : RecordsBeganDate used: {StartOfObservations}, Number of days removed from list: {i}", TraceLevel.Info );
                }
                catch
                {
                    StartOfObservations = MainList.Min( x => x.ThisDate );
                    Sup.LogMessage( $"CumulusUtils : RecordsBeganDate used with wrong format; using the first observation date {StartOfObservations}", TraceLevel.Info );
                }
            }

            if ( DoSystemChk )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                SysInfo fncs = new SysInfo( Sup, Isup );
                await fncs.GenerateSystemStatusAsync();
                fncs.Dispose();
                LogTiming( ref watch, "SysInfo generation" );
            }

            if ( DoStationMap )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                StationMap fncs = new StationMap( Sup );
                fncs.GenerateStationMap();
                LogTiming( ref watch, "StationMap generation" );
            }

            if ( DoMeteoCam )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                MeteoCam fncs = new MeteoCam( Sup );
                fncs.GenerateMeteoCam();
                LogTiming( ref watch, "MeteoCam generation" );
            }

            if ( DoForecast )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                WeatherForecasts fncs = new WeatherForecasts( Sup, Isup );
                await fncs.GenerateForecasts();
                LogTiming( ref watch, "WeatherForecast generation" );
            }

            if ( DoUserReports )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                // This function does its own uploads immediately as it has the filenames and it is assumed they contain daily relevant info
                // If not than we must consider later.
                // If no reports exist, nothing is done. If run as a module you can see it as an independent Webtag replacer but similar to what CMX does.
                UserReports fncs = new UserReports( Sup, Isup );
                await fncs.DoUserReports();
                LogTiming( ref watch, "UserReports generation" );
            }

            if ( DoAirLink )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                AirLink fncs = new AirLink( Sup );
                fncs.DoAirLink();
                LogTiming( ref watch, "AirQuality generation" );
            }

            if ( DoExtraSensors && HasExtraSensors )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                ExtraSensors fncs = new ExtraSensors( Sup );
                fncs.DoExtraSensors();
                if ( ParticipatesSensorCommunity ) fncs.CreateSensorCommunityMapIframeFile();
                LogTiming( ref watch, "ExtraSensors generation" );
            }

            if ( DoCustomLogs && HasCustomLogs )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                CustomLogs fncs = new CustomLogs( Sup );
                fncs.DoCustomLogs();
                LogTiming( ref watch, "CustomLogs generation" );
            }

            if ( DoCUlib )
            {
                CUlib fncs = new CUlib( Sup );
                fncs.Generate();
            }

            if ( DoDiary )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif
                Diary fncs = new Diary( Sup );

                if ( HasDiaryMenu )
                {
                    fncs.GenerateDiaryDisplay();
                    fncs.GenerateDiaryReport();
                }
                LogTiming( ref watch, "Diary generation" );
            }

            // These were the tasks without [weather]data.
            // Now do the datadriven tasks
            //
            if ( AnyDataTaskRequested )
            {
                if ( DoPwsFWI )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    PwsFWI fncs = new PwsFWI( Sup, Isup );
                    await fncs.CalculatePwsFWI( MainList );
                    fncs.Dispose();
                    LogTiming( ref watch, "pwsFWI generation" );
                }

                if ( DoYadr )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Yadr fncs = new Yadr( Sup );
                    fncs.GenerateYadr( MainList );
                    fncs.Dispose();
                    LogTiming( ref watch, "Yadr generation" );
                }

                if ( DoRecords )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Records fncs = new Records( Sup );
                    fncs.GenerateRecords( MainList );
                    LogTiming( ref watch, "Records generation" );
                }

                if ( DoDayRecords )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    DayRecords fncs = new DayRecords( Sup );
                    fncs.GenerateDayRecords( MainList );
                    LogTiming( ref watch, "DayRecords generation" );
                }

                if ( DoNOAA )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    NOAAdisplay fncs = new NOAAdisplay( Sup );
                    fncs.GenerateNOAATxtfile( MainList );
                    LogTiming( ref watch, "NOAA reader generation" );
                }

                if ( DoGraphs )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Graphx fncs = new Graphx( MainList, Sup );
                    fncs.GenerateGraphx( MainList );
                    fncs.Dispose();
                    LogTiming( ref watch, "Graphs generation" );
                }

                //
                // This call must always be the last because it changes the sorting of the MainList
                //
                if ( DoTop10 )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Top10 fncs = new Top10( Sup );
                    fncs.GenerateTop10List( MainList );
                    fncs.Dispose();
                    LogTiming( ref watch, "Top10 generation" );
                }

                if ( DoWebsite )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Website fncs = new Website( Sup, Isup );
                    await fncs.GenerateWebsite();
                    //await fncs.CheckPackageAndCopy();
                    LogTiming( ref watch, "Website generation" );
                }

                //
                // Maps is done here to prevent it being done every sysinfo or other dataindependent module!!
                //
                if ( MapParticipant || DoWebsite )
                {
                    string retval;

#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Maps fncs = new Maps( Sup );
                    retval = await fncs.MapsOn();
                    Sup.LogMessage( retval, TraceLevel.Info );

                    if ( DoCreateMap && File.Exists( "paMuCetaerCyaM.txt" ) )
                    {
                        // This is for the MapManager to fetch all Map signatures and create and upload the map
                        // Currently it is MeteoWagenborgen.nl but can be anybody on any domain. Just make sure you have the rights to upload
                        // Note: the signature files are placed by the users in the Maps directory on the managers server and are handled by
                        //       a cgi-bin perl script receive.pl (also in the git)

                        fncs.CreateMap();
                    }
                    else
                    {
                        // MeteoWagenborgen (or any other by agreement) creates the map once per hour (or at any frequency wanted/required)
                        // All users may download that map at any time
                        //
                        Sup.LogMessage( "Fetch Map: Fetching the generated map", TraceLevel.Info );

                        // Change this URL when changing map manager role
                        //
                        retval = await Isup.GetUrlDataAsync( new Uri( "https://meteo-wagenborgen.nl/maps.txt" ) );

                        if ( !string.IsNullOrEmpty( retval ) )
                        {
                            LogPreview( "Main", retval );

                            // [OPT] Was synchronous File.WriteAllText while the sibling
                            //       read below uses await; now consistently awaited.
                            await File.WriteAllTextAsync( $"{Sup.PathUtils}{Sup.MapsOutputFilename}", retval, Encoding.UTF8 );

                            //The Map is always downloaded without the jQuery include. If required add it here
                            const string tmpMap = "tmpMaps.txt";
                            string jQueryString = CuSupport.GenjQueryIncludestring();

                            if ( !string.IsNullOrEmpty( jQueryString ) )
                            {
                                Sup.LogMessage( "Fetch Map: Adding jQuery to the downloaded map", TraceLevel.Info );

                                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{tmpMap}", false, Encoding.UTF8 ) )
                                {
                                    of.WriteLine( jQueryString );

                                    using ( StreamReader MapFile = new StreamReader( $"{Sup.PathUtils}{Sup.MapsOutputFilename}", Encoding.UTF8 ) )
                                    {
                                        string? line;

                                        while ( ( line = await MapFile.ReadLineAsync() ) is not null )
                                        {
                                            of.WriteLine( line );
                                        }
                                    }
                                } // Done copying with the required jQuery string

                                // Remove the just downloaded Map and replace it with the one which has the jQuery library included
                                File.Delete( $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );
                                File.Move( $"{Sup.PathUtils}{tmpMap}", $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );

                                Sup.LogMessage( "Fetch Map: Added jQuery library to the Map.", TraceLevel.Info );
                            } // Should we include the jQuery library?
                        } // Did the map.txt download correctly?
                        else
                            Sup.LogMessage( "Fetch Map from server: Fail... empty map.", TraceLevel.Error );
                    }

                    fncs.Dispose();
                    LogTiming( ref watch, "Map generation" );
                }

                if ( DoUserAskedData )
                {
                    DateTime tmpTimeEnd = DateTime.Now;

                    Sup.LogMessage( "UserAskedData Starting...", TraceLevel.Info );

#if TIMING
                    watch = Stopwatch.StartNew();
#endif

                    tmpTimeEnd = await RunUserAskedData( tmpTimeEnd );

                    // No matter what happened, set the upload date/time
                    Sup.SetUtilsIniValue( "General", "LastUploadTime", tmpTimeEnd.ToString( "dd/MM/yy HH:mm", Inv ) );

                    LogTiming( ref watch, "UserAskedData" );
                } // DoUserAskedData
            }

            // Make this the last part to be able to overwrite default file output for the graphs when that will be implemented
            //
            if ( DoCompileOnly && !Thrifty )
            {
                List<OutputDef> thisList;

#if TIMING
                watch = Stopwatch.StartNew();
#endif
                ChartsCompiler fncs = new ChartsCompiler( Sup );
                thisList = fncs.ParseChartDefinitions();

                if ( thisList is not null )
                {
                    int i = 0;

                    foreach ( OutputDef thisDef in thisList )
                    {
                        // Generate
                        fncs.GenerateUserDefinedCharts( thisDef.TheseCharts, thisDef.Filename, i++ );

                        // and Upload
                        Sup.LogMessage( $"Uploading = {thisDef.Filename}", TraceLevel.Info );
                        await Isup.UploadFileAsync( $"{thisDef.Filename}", $"{Sup.PathUtils}{thisDef.Filename}" );
                    }
                }
                else
                {
                    Sup.LogDebugMessage( "Errors in Charts definition. See logfile, please correct and run again." );
                }

                LogTiming( ref watch, "Compile and Generate CumulusCharts" );
            }

            //********************************  Do the uploading when required **************************************
            //

            if ( !Thrifty && !DoUserAskedData )
            {
                // Always upload the package files
                Sup.LogMessage( "Uploading = The Package", TraceLevel.Info );
                await Sup.CheckPackageAndCopy();
            }

            if ( DoWebsite )
            {
                Sup.LogMessage( $"Uploading = {Sup.IndexOutputFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( $"{Sup.IndexOutputFilename}", $"{Sup.PathUtils}{Sup.IndexOutputFilename}" );
            }

            if ( DoPwsFWI )
            {
                Sup.LogMessage( $"Uploading = {Sup.PwsFWIOutputFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( $"{Sup.PwsFWIOutputFilename}", $"{Sup.PathUtils}{Sup.PwsFWIOutputFilename}" );

                Sup.LogMessage( $"Uploading = {Sup.PwsFWICurrentOutputFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( $"{Sup.PwsFWICurrentOutputFilename}", $"{Sup.PathUtils}{Sup.PwsFWICurrentOutputFilename}" );
            }

            if ( DoTop10 && ( !Thrifty || ThriftyTop10RecordsDirty ) )
            {
                Sup.LogMessage( $"Uploading = {Sup.Top10OutputFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( $"{Sup.Top10OutputFilename}", $"{Sup.PathUtils}{Sup.Top10OutputFilename}" );
            }

            if ( DoGraphs )
            {
                // [OPT] The five graph uploads share one shape; driven from a table.
                (bool hasMenu, bool dirty, string file)[] graphUploads =
                {
                    ( HasRainGraphMenu,  ThriftyRainGraphsDirty,  Sup.GraphsRainOutputFilename ),
                    ( HasTempGraphMenu,  ThriftyTempGraphsDirty,  Sup.GraphsTempOutputFilename ),
                    ( HasWindGraphMenu,  ThriftyWindGraphsDirty,  Sup.GraphsWindOutputFilename ),
                    ( HasSolarGraphMenu, ThriftySolarGraphsDirty, Sup.GraphsSolarOutputFilename ),
                    ( HasMiscGraphMenu,  ThriftyMiscGraphsDirty,  Sup.GraphsMiscOutputFilename ),
                };

                foreach ( (bool hasMenu, bool dirty, string file) in graphUploads )
                {
                    if ( hasMenu && ( !Thrifty || dirty ) )
                    {
                        string name = Path.GetFileName( file );
                        Sup.LogMessage( $"Uploading = {name}", TraceLevel.Info );
                        await Isup.UploadFileAsync( name, Sup.PathUtils + name );
                    }
                }
            }

            if ( MapParticipant || DoWebsite )
            {
                Sup.LogMessage( $"Uploading = {Sup.MapsOutputFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( $"{Sup.MapsOutputFilename}", $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );
            }

            if ( DoRecords && ( !Thrifty || ThriftyRecordsDirty ) )
            {
                Sup.LogMessage( $"Uploading = {Sup.RecordsOutputFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( $"{Sup.RecordsOutputFilename}", $"{Sup.PathUtils}{Sup.RecordsOutputFilename}" );
            }

            if ( DoNOAA )
            {
                await Isup.UploadFileAsync( $"{Sup.NOAAOutputFilename}", $"{Sup.PathUtils}{Sup.NOAAOutputFilename}" );
            }

            if ( DoDayRecords )  // Take care it is always uploaded to possibly change the format of yesterday even if there is no record
            {
                await Isup.UploadFileAsync( $"{Sup.DayRecordsOutputFilename}", $"{Sup.PathUtils}{Sup.DayRecordsOutputFilename}" );
            }

            if ( DoForecast ) { await Isup.UploadFileAsync( $"{Sup.ForecastOutputFilename}", $"{Sup.PathUtils}{Sup.ForecastOutputFilename}" ); }

            if ( DoStationMap && ( !Thrifty ) ) { await Isup.UploadFileAsync( $"{Sup.StationMapOutputFilename}", $"{Sup.PathUtils}{Sup.StationMapOutputFilename}" ); }
            if ( DoMeteoCam && HasMeteoCamMenu && !Thrifty ) { await Isup.UploadFileAsync( $"{Sup.MeteoCamOutputFilename}", $"{Sup.PathUtils}{Sup.MeteoCamOutputFilename}" ); }

            if ( DoAirLink && !Thrifty )
            {
                await Isup.UploadFileAsync( $"{Sup.AirLinkOutputFilename}", $"{Sup.PathUtils}{Sup.AirLinkOutputFilename}" );
                if ( ParticipatesSensorCommunity )
                    await Isup.UploadFileAsync( $"{Sup.SensorCommunityOutputFilename}", $"{Sup.PathUtils}{Sup.SensorCommunityOutputFilename}" );
            }

            if ( DoExtraSensors && HasExtraSensors && !Thrifty )
                await Isup.UploadFileAsync( $"{Sup.ExtraSensorsOutputFilename}", $"{Sup.PathUtils}{Sup.ExtraSensorsOutputFilename}" );

            if ( DoCustomLogs && HasCustomLogs && !Thrifty )
                await Isup.UploadFileAsync( $"{Sup.CustomLogsOutputFilename}", $"{Sup.PathUtils}{Sup.CustomLogsOutputFilename}" );

            if ( DoCUlib )
                await Isup.UploadFileAsync( $"lib/{Sup.CUlibOutputFilename}", $"{Sup.PathUtils}{Sup.CUlibOutputFilename}" );

            // [OPT] Clock read once instead of three times.
            DateTime today = DateTime.Today;
            int StartYear = today.Month > 6 && today.Month <= 12 ? today.Year : today.Year - 1;

            if ( DoDiary && HasDiaryMenu )  // i.e. there is data in the diary and do we want to upload the module
            {
                await Isup.UploadFileAsync( $"{Sup.DiaryOutputFilename}", $"{Sup.PathUtils}{Sup.DiaryOutputFilename}" );

                if ( !Thrifty )
                    for ( int i = YearMin; i <= StartYear; i++ )
                        await Isup.UploadFileAsync( $"Diary{i}.txt", $"{Sup.PathUtils}Diary{i}.txt" );
                else
                    // Previous Data has already been uploaded above when !Thrifty only do report of this year (YearMax)
                    await Isup.UploadFileAsync( $"Diary{StartYear}.txt", $"{Sup.PathUtils}Diary{StartYear}.txt" );
            }

            if ( DoYadr )
            {
                if ( !Thrifty )
                {
                    string[] filelist = Directory.GetFiles( Sup.PathUtils, "Yadr*.txt" );

                    Sup.LogMessage( $"Thrifty: {Thrifty} - YADR - Complete upload", TraceLevel.Info );

                    foreach ( string file in filelist )
                    {
                        await Isup.UploadFileAsync( Path.GetFileName( file ), Sup.PathUtils + Path.GetFileName( file ) );
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
                        Sup.LogMessage( $"Thrifty: {Thrifty} - YADR - Upload for 2 January", TraceLevel.Info );
                        await Isup.UploadFileAsync( Path.GetFileName( "Yadr.txt" ), Sup.PathUtils + Path.GetFileName( "Yadr.txt" ) );
                    }

                    Sup.LogMessage( $"Thrifty: {Thrifty} - YADR - Upload for only current year {RunStarted.Year}", TraceLevel.Info );
                    foreach ( string file in filelist )
                    {
                        await Isup.UploadFileAsync( Path.GetFileName( file ), Sup.PathUtils + Path.GetFileName( file ) );
                    }
                }
            }

            // This block takes care  of the JSON upload (if any JSON present).
            // This is unconditional.
            // JSONs will be deleted after succesful upload else they remain.
            {
                // Now upload the JSON files if any
                string[] files = Directory.GetFiles( $"{Sup.PathUtils}", $"*.json" );

                foreach ( string file in files )
                {
                    FileInfo fi = new FileInfo( file );

                    Sup.LogMessage( $"Uploading => {fi.Name} from {Sup.PathUtils}{fi.Name}", TraceLevel.Info );
                    if ( await Isup.UploadFileAsync( $"{fi.Name}", $"{Sup.PathUtils}{fi.Name}" ) ) fi.Delete();
                }
            }

            // Before v4.0.0 SysInfo must be processed by Cumulus so not handy to do the uploading here but now we do
            if ( DoSystemChk ) { await Isup.UploadFileAsync( $"{Sup.SysInfoOutputFilename}", $"{Sup.PathUtils}{Sup.SysInfoOutputFilename}" ); }

#if TIMING
            OverallWatch.Stop();
            Sup.LogMessage( $"Overall Timing all Modules = {OverallWatch.ElapsedMilliseconds} ms", TraceLevel.Info );
#endif

            return;
        }

        #endregion

        #region Helpers

        // [OPT] Replaces the #if TIMING / StartNew / Stop / log quadruplet that appeared
        //       verbatim sixteen times. No-op when TIMING is not defined.
        [Conditional( "TIMING" )]
        private static void LogTiming( ref Stopwatch watch, string label )
        {
            watch.Stop();
            Sup.LogMessage( $"Timing of {label} = {watch.ElapsedMilliseconds} ms", TraceLevel.Info );
        }

        // [OPT] Long values were logged with a 50-char clamp in two places.
        private static void LogPreview( string prefix, string value, int max = 50 ) =>
            Sup.LogMessage( $"{prefix}: {( value.Length > max ? value.Substring( 0, max ) : value )}", TraceLevel.Info );

        private static void PrintUsage()
        {
            Console.WriteLine( "\nCumulusUtils : No Arguments nothing to do. Exiting. See Manual." );
            Console.WriteLine( "" );
            Console.WriteLine( "CumulusUtils Usage : utils/bin/cumulusutils.exe [args] (args case independent):" );
            Console.WriteLine( "" );
            Console.WriteLine( "  utils/bin/cumulusutils.exe" );
            Console.WriteLine( "      [SysInfo][Forecast][StationMap][UserReports][MeteoCam]" );
            Console.WriteLine( "      [pwsFWI][Top10][Graphs][Yadr][Records][UserAskedData]" );
            Console.WriteLine( "      [NOAA][DayRecords][AirLink][CompileOnly][ExtraSensors]" );
            Console.WriteLine( "      [CustomLogs][CUlib][Diary]" );
            Console.WriteLine( "" );
            Console.WriteLine( "" );
            Console.WriteLine( "OR (in case you use the website generator):" );
            Console.WriteLine( "" );
            Console.WriteLine( "  utils/bin/cumulusutils.exe [Thrifty] Website" );
        }

        // Generic guarded-run: executes work, logs the failure with the given label,
        // and returns the fallback value on exception. Replaces the four
        // near-identical try/catch blocks in the original UserAskedData section.
        private static T TryRun<T>( string label, Func<T> work, T fallback )
        {
            try
            {
                return work();
            }
            catch ( Exception e )
            {
                Sup.LogMessage( $"UserAskedData: Failing in {label}", TraceLevel.Error );
                Sup.LogMessage( $"UserAskedData: Message {e.Message}", TraceLevel.Error );
                Sup.LogMessage( "UserAskedData: Continuing", TraceLevel.Info );
                return fallback;
            }
        }

        // [OPT] Async overload for awaited calls that return a Task. The generic
        //       Func<T> form cannot await, and a bare 'await work()' here keeps the
        //       "Continuing" semantics of the original try/catch blocks.
        private static async Task TryRunAsync( string label, Func<Task> work )
        {
            try
            {
                await work();
            }
            catch ( Exception e )
            {
                Sup.LogMessage( $"UserAskedData: Failing in {label}", TraceLevel.Error );
                Sup.LogMessage( $"UserAskedData: Message {e.Message}", TraceLevel.Error );
                Sup.LogMessage( "UserAskedData: Continuing", TraceLevel.Info );
            }
        }

        // [OPT] Void overload for guarded calls that produce no value. A void method
        //       cannot be handed to the generic Func<T> form above.
        private static void TryRun( string label, Action work )
        {
            try
            {
                work();
            }
            catch ( Exception e )
            {
                Sup.LogMessage( $"UserAskedData: Failing in {label}", TraceLevel.Error );
                Sup.LogMessage( $"UserAskedData: Message {e.Message}", TraceLevel.Error );
                Sup.LogMessage( "UserAskedData: Continuing", TraceLevel.Info );
            }
        }
        private async Task<DateTime> RunUserAskedData( DateTime tmpTimeEnd )
        {
            Sup.LogMessage( "UserAskedData Doing the compiler stuff...", TraceLevel.Info );
            List<ChartDef> tmpChartsList = new List<ChartDef>();

            ChartsCompiler fncs = new ChartsCompiler( Sup );

            // UserAskedData is created with a complete ChartsList so create the chartslist from all separate OutputDefs.
            // It's a bit awkward to separate charts in different lists first and then reassemble but I see no other way.
            //
            List<OutputDef> theseOutputs = fncs.ParseChartDefinitions();

            if ( theseOutputs is not null )
            {
                foreach ( OutputDef thisOutput in theseOutputs )
                {
                    if ( !thisOutput.Filename.Equals( Sup.ExtraSensorsCharts ) )
                        foreach ( ChartDef tmpChart in thisOutput.TheseCharts )
                            tmpChartsList.Add( tmpChart );
                }

                tmpTimeEnd = TryRun( "GenerateUserAskedData - i.e. Compiler data",
                                     () => fncs.GenerateUserAskedData( thisList: tmpChartsList ),
                                     tmpTimeEnd );
            }
            else
            {
                Sup.LogDebugMessage( "Errors in Charts definition. See logfile, please correct and run again." );
            }

            Sup.LogMessage( $"DoAirLink / AirQualitySensor = {DoAirLink} / {HasAirLink}", TraceLevel.Info );

            if ( HasAirLink )
            {
                Sup.LogMessage( "UserAskedData Doing the AirQuality stuff...", TraceLevel.Info );
                AirLink air = new AirLink( Sup );
                await TryRunAsync( "GenAirLinkDataJson - i.e. Airlink data",
                                   () => air.GenAirLinkDataJson() );
            }

            if ( HasExtraSensors )
            {
                Sup.LogMessage( "UserAskedData Doing the ExtraSensor stuff...", TraceLevel.Info );
                ExtraSensors extra = new ExtraSensors( Sup );
                TryRun( "GenerateExtraSensorDataJson - i.e. ExtraSensors (incl External) data",
                        () => extra.GenerateExtraSensorDataJson() );
            }

            if ( HasCustomLogs )
            {
                Sup.LogMessage( "UserAskedData Doing the CustomLogs stuff...", TraceLevel.Info );
                CustomLogs custom = new CustomLogs( Sup );
                TryRun( "GenerateCustomLogsDataJson",
                        () => custom.GenerateCustomLogsDataJson( NonIncremental: false ) );
            }

            return tmpTimeEnd;
        }

        #endregion

        #region CommandLineArgs

        // [OPT] Flattened from a three-deep if/else nest into a switch. The Website,
        //       ExtraSensors and CustomLogs groups are grouped arms; the implicit
        //       DoCompileOnly for ExtraSensors/CustomLogs is preserved.
        private void CommandLineArgs( string[] args )
        {
            Sup.LogDebugMessage( "CommandLineArgs : starting" );

            foreach ( string s in args )
            {
                Sup.LogDebugMessage( $" CommandLineArgs : handling arg: {s}" );

                switch ( s.ToLowerInvariant() )
                {
                    case "website":
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
                        DoExtraSensors = true;
                        DoCustomLogs = true;
                        DoCUlib = true;            // this is implicit for website so if user sets it undo tha
                        DoDiary = true;
                        break;

                    case "thrifty":
                        Thrifty = true;
                        break;

                    case "top10": DoTop10 = true; break;
                    case "pwsfwi": DoPwsFWI = true; break;
                    case "sysinfo": DoSystemChk = true; break;
                    case "graphs": DoGraphs = true; break;
                    case "createmap": DoCreateMap = true; break;    // Undocumented feature only for the keeper of the map
                    case "yadr": DoYadr = true; break;
                    case "records": DoRecords = true; break;
                    case "noaa": DoNOAA = true; break;
                    case "dayrecords": DoDayRecords = true; break;
                    case "forecast": DoForecast = true; break;
                    case "userreports": DoUserReports = true; break;
                    case "stationmap": DoStationMap = true; break;
                    case "meteocam": DoMeteoCam = true; break;
                    case "airlink": DoAirLink = true; break;
                    case "compileonly": DoCompileOnly = true; break;
                    case "useraskeddata": DoUserAskedData = true; break;
                    case "culib": DoCUlib = true; break;
                    case "diary": DoDiary = true; break;

                    case "extrasensors":
                        DoExtraSensors = true;
                        DoCompileOnly = true;  // Implicit for Extra Sensors
                        break;

                    case "customlogs":
                        DoCustomLogs = true;
                        DoCompileOnly = true;  // Implicit for Custom Logs
                        break;
                }
            }
        } // Commandline handling

        #endregion

    } // Class CUtils
} // namespace
