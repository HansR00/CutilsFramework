/*
 * CumulusUtils/Main
 * 
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
    // 
    // Use GlobConst as a store for constants as you would use C #defines...
    // That's what you get when you act as a hybrid between C and C#
    // 
    enum Months : int { Jan = 1, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec };

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
        public static bool HasStationMapMenu { get; set; }
        public static bool HasSystemInfoMenu { get; set; }
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

        #endregion

        #region Main
        private static async Task Main( string[] args )
        {
            TraceListener FtpListener = null;

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
                else if ( !File.Exists( "UniqueId.txt" ) )
                {
                    // UniqueId.txt must exist
                    Console.WriteLine( $"CumulusMX version 4 must be installed and must have run." );
                    Environment.Exit( 0 );
                }
                else
                {
                    string tmp;

                    using ( StreamReader UniqueKey = new( "UniqueId.txt" ) ) tmp = UniqueKey.ReadToEnd();
                    CryptoKey = Convert.FromBase64String( tmp );
                }

                if ( !Directory.Exists( "utils" ) ) Directory.CreateDirectory( "utils" );
                if ( !Directory.Exists( "utils/utilslog" ) ) Directory.CreateDirectory( "utils/utilslog" );

                string[] files = Directory.GetFiles( "utils/utilslog" );

                foreach ( string file in files )
                {
                    FileInfo fi = new FileInfo( file );
                    if ( fi.CreationTime < DateTime.Now.AddDays( -2 ) )
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

                ThriftyTop10RecordsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "Top10RecordsPeriod", "1" ), Inv );
                ThriftyRainGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "RainGraphsPeriod", "1" ), Inv );
                ThriftyTempGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "TempGraphsPeriod", "1" ), Inv );
                ThriftyWindGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "WindGraphsPeriod", "1" ), Inv );
                ThriftySolarGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "SolarGraphsPeriod", "1" ), Inv );
                ThriftyMiscGraphsPeriod = Convert.ToInt32( Sup.GetUtilsIniValue( "Thrifty", "MiscGraphsPeriod", "1" ), Inv );

                DoModular = Sup.GetUtilsIniValue( "General", "DoModular", "false" ).Equals( "true", Cmp );
                ModulePath = Sup.GetUtilsIniValue( "General", "ModulePath", "" );

                HasSystemInfoMenu = Sup.GetUtilsIniValue( "SysInfo", "SystemInfoMenu", "true" ).Equals( "true", Cmp );
                HasStationMapMenu = Sup.GetUtilsIniValue( "StationMap", "StationMapMenu", "true" ).Equals( "true", Cmp );
                HasMeteoCamMenu = Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamMenu", "true" ).Equals( "true", Cmp );
                HasExtraSensors = Sup.GetUtilsIniValue( "ExtraSensors", "ExtraSensors", "false" ).Equals( "true", Cmp ) &&
                    Sup.GetCumulusIniValue( "Station", "LogExtraSensors", "" ).Equals( "1" );
                HasCustomLogs = Sup.GetUtilsIniValue( "CustomLogs", "CustomLogs", "false" ).Equals( "true", Cmp ) &&
                    ( Sup.GetCumulusIniValue( "CustomLogs", "IntervalEnabled0", "" ).Equals( "1" ) || Sup.GetCumulusIniValue( "CustomLogs", "DailyEnabled0", "" ).Equals( "1" ) );

                ParticipatesSensorCommunity = Sup.GetUtilsIniValue( "ExtraSensors", "ParticipatesSensorCommunity", "false" ).Equals( "true", Cmp );
                MapParticipant = Sup.GetUtilsIniValue( "Maps", "Participant", "true" ).Equals( "true", Cmp );
                HasSolar = Sup.GetUtilsIniValue( "Website", "ShowSolar", "true" ).Equals( "true", Cmp ); // Is an indirect determination set by the user only in  cutils
                DoLibraryIncludes = Sup.GetUtilsIniValue( "General", "DoLibraryIncludes", "false" ).Equals( "true", Cmp ); // Do we need the libs??
                DojQueryInclude = Sup.GetUtilsIniValue( "General", "GeneratejQueryInclude", "false" ).Equals( "true", Cmp );

                bool AirLinkIn = Sup.GetCumulusIniValue( "AirLink", "In-Enabled", "0" ).Equals( "1" );
                bool AirLinkOut = Sup.GetCumulusIniValue( "AirLink", "Out-Enabled", "0" ).Equals( "1" );
                HasAirLink = AirLinkIn || AirLinkOut;

                HoursInGraph = Convert.ToInt32( Sup.GetCumulusIniValue( "Graphs", "GraphHours", "" ) );
                DaysInGraph = Convert.ToInt32( Sup.GetCumulusIniValue( "Graphs", "ChartMaxDays", "" ) );
                LogIntervalInMinutes = PossibleIntervals[ Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "DataLogInterval", "" ), Inv ) ];
                FTPIntervalInMinutes = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "" ) );
                UtilsRealTimeInterval = Convert.ToInt32( Sup.GetUtilsIniValue( "Website", "CumulusRealTimeInterval", "15" ) ); // Sorry for the confused naming
                ConnectNulls = Sup.GetUtilsIniValue( "General", "ConnectNulls", "false" ).Equals( "true", Cmp );

                // Now start doing things
                CUtils p = new CUtils();
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
                //throw;
            }
            finally
            {
                Sup.LogDebugMessage( $"All done, Entering the finally section...; Closing down." );

                if ( FtpListener is not null )
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

        #endregion

        #region RealMainAsync
        private async Task RealMainAsync( string[] args )  // 
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
                Sup.LogTraceErrorMessage( $"CumulusUtils : Conflicting settings - DoModular is {DoModular} while running Website." );
                Sup.LogTraceErrorMessage( $"CumulusUtils : Cannot handle this, Exiting." );

                Environment.Exit( 0 );
            }

            if ( !DoPwsFWI && !DoTop10 && !DoSystemChk && !DoGraphs && !DoCreateMap && !DoYadr && !DoRecords && !DoCompileOnly && !DoUserAskedData && !DoCustomLogs &&
                !DoNOAA && !DoDayRecords && !DoWebsite && !DoForecast && !DoUserReports && !DoStationMap && !DoMeteoCam && !DoAirLink && !DoExtraSensors && !DoCUlib && !DoDiary )
            {
                Sup.LogTraceErrorMessage( "CumulusUtils : No Arguments, nothing to do. Exiting." );
                Sup.LogTraceErrorMessage( "CumulusUtils : Exiting Main" );

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

            // Adjust if RecordsBeganDate is set
            //
            string tmp = Sup.GetUtilsIniValue( "General", "RecordsBeganDate", "" );

            if ( string.IsNullOrEmpty( tmp ) )
            {
                StartOfObservations = MainList.Select( x => x.ThisDate ).Min();
            }
            else
            {
                try
                {
                    StartOfObservations = DateTime.ParseExact( tmp, "dd/MM/yy", Inv );

                    int i = MainList.RemoveAll( p => p.ThisDate < StartOfObservations );
                    Sup.LogTraceInfoMessage( $"CumulusUtils : RecordsBeganDate used: {StartOfObservations}, Number of days removed from list: {i}" );
                }
                catch
                {
                    StartOfObservations = MainList.Select( x => x.ThisDate ).Min();
                    Sup.LogTraceInfoMessage( $"CumulusUtils : RecordsBeganDate used with wrong format; using the first observation date {StartOfObservations}" );
                }
            }

            YearMax = MainList.Select( x => x.ThisDate.Year ).Max();
            YearMin = MainList.Select( x => x.ThisDate.Year ).Min();
            Sup.LogTraceInfoMessage( $"CumulusUtils : YearMin = {YearMin}; YearMax = {YearMax}" );

            if ( DoSystemChk )
            {
                // Timing of the SysInfo
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                SysInfo fncs = new SysInfo( Sup, Isup );
                await fncs.GenerateSystemStatusAsync();
                fncs.Dispose();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of SysInfo generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            if ( DoStationMap )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                StationMap fncs = new StationMap( Sup );
                fncs.GenerateStationMap();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of StationMap generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            if ( DoMeteoCam )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                MeteoCam fncs = new MeteoCam( Sup );
                fncs.GenerateMeteoCam();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of MeteoCam generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            if ( DoForecast )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                WeatherForecasts fncs = new WeatherForecasts( Sup, Isup );
                await fncs.GenerateForecasts();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of WeatherForecast generation = {watch.ElapsedMilliseconds} ms" );
#endif
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

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of USerReports generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            if ( DoAirLink )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                AirLink fncs = new AirLink( Sup );
                fncs.DoAirLink();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of AirQuality generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            if ( DoExtraSensors && HasExtraSensors )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                ExtraSensors fncs = new ExtraSensors( Sup );
                fncs.DoExtraSensors();
                if ( ParticipatesSensorCommunity ) fncs.CreateSensorCommunityMapIframeFile();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of ExtraSensors generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            if ( DoCustomLogs && HasCustomLogs )
            {
#if TIMING
                watch = Stopwatch.StartNew();
#endif

                CustomLogs fncs = new CustomLogs( Sup );
                fncs.DoCustomLogs();

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of CustomLogs generation = {watch.ElapsedMilliseconds} ms" );
#endif
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

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of Diary generation = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            // These were the tasks without [weather]data.
            // Now do the datadriven tasks
            //
            if ( DoPwsFWI || DoTop10 || DoGraphs || DoYadr || DoRecords || DoNOAA || DoDayRecords || DoWebsite || DoCreateMap || DoUserAskedData )
            {
                //StartOfObservations = MainList.Select( x => x.ThisDate ).Min();
                if ( DoPwsFWI )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif

                    PwsFWI fncs = new PwsFWI( Sup, Isup );
                    await fncs.CalculatePwsFWI( MainList );
                    fncs.Dispose();

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of pwsFWI generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoYadr )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Yadr fncs = new Yadr( Sup );
                    fncs.GenerateYadr( MainList );
                    fncs.Dispose();

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Yadr generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoRecords )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    Records fncs = new Records( Sup );
                    fncs.GenerateRecords( MainList );

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Records generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoDayRecords )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    DayRecords fncs = new DayRecords( Sup );
                    fncs.GenerateDayRecords( MainList );

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of DayRecords generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoNOAA )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif
                    NOAAdisplay fncs = new NOAAdisplay( Sup );
                    fncs.GenerateNOAATxtfile( MainList );

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of NOAA reader generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoGraphs )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif

                    Graphx fncs = new Graphx( MainList, Sup );
                    fncs.GenerateGraphx( MainList );
                    fncs.Dispose();

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Graphs generation = {watch.ElapsedMilliseconds} ms" );
#endif
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

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Top10 generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoWebsite )
                {
#if TIMING
                    watch = Stopwatch.StartNew();
#endif

                    Website fncs = new Website( Sup, Isup );
                    await fncs.GenerateWebsite();
                    //await fncs.CheckPackageAndCopy();

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Website generation = {watch.ElapsedMilliseconds} ms" );
#endif
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
                    Sup.LogTraceInfoMessage( retval );

                    if ( DoCreateMap && File.Exists( "paMuCetaerCyaM.txt" ) )
                    {
                        // This is for the MapManager to fetch all Map signatures and create and upload the map
                        // Currently it is MeteoWagenborgen.nl but can be anybody on any domain. Just make sure you have the rights to upload
                        // Note: the signature files are placed by the users in the Maps directory on the managers server and are handled by 
                        //       a cgi-bin perl script receive.pl (also in the git)

                        fncs.CreateMap();
                    }
                    else if ( !File.Exists( "paMuCetaerCyaM.txt" ) )  // 
                    {
                        // MeteoWagenborgen (or any other by agreement) creates the map once per hour (or at any frequency wanted/required)
                        // All users may download that map at any time
                        //
                        Sup.LogTraceInfoMessage( $"Fetch Map: Fetching the generated map" );

                        // Change this URL when changing map manager role
                        //
                        retval = await Isup.GetUrlDataAsync( new Uri( "https://meteo-wagenborgen.nl/maps.txt" ) );

                        if ( !string.IsNullOrEmpty( retval ) )
                        {
                            if ( retval.Length > 50 )
                                Sup.LogTraceInfoMessage( $"Main: {retval.Substring( 0, 50 )}" );
                            else
                                Sup.LogTraceInfoMessage( $"Main: {retval}" );

                            File.WriteAllText( $"{Sup.PathUtils}{Sup.MapsOutputFilename}", retval, Encoding.UTF8 );

                            //The Map is always downloaded without the jQuery include. If required add it here
                            const string tmpMap = "tmpMaps.txt";
                            string jQueryString = CuSupport.GenjQueryIncludestring();

                            if ( !string.IsNullOrEmpty( jQueryString ) )
                            {
                                Sup.LogTraceInfoMessage( $"Fetch Map: Adding jQuery to the downloaded map" );

                                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{tmpMap}", false, Encoding.UTF8 ) )
                                {
                                    of.WriteLine( $"{jQueryString}" );

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

                                Sup.LogTraceInfoMessage( $"Fetch Map: Added jQuery library to the Map." );
                            } // Should we include the jQuery library?
                        } // Did the map.txt download correctly?
                        else
                            Sup.LogTraceErrorMessage( "Fetch Map from server: Fail... empty map." );
                    }

                    fncs.Dispose();

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of Map generation = {watch.ElapsedMilliseconds} ms" );
#endif
                }

                if ( DoUserAskedData )
                {
                    DateTime tmpTimeEnd = DateTime.Now;

                    Sup.LogTraceInfoMessage( $"UserAskedData Starting..." );

#if TIMING
                    watch = Stopwatch.StartNew();
#endif

                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the compiler stuff..." );
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

                            try
                            {
                                tmpTimeEnd = fncs.GenerateUserAskedData( thisList: tmpChartsList );  // 
                            }
                            catch ( Exception e )
                            {
                                Sup.LogTraceInfoMessage( $"UserAskedData: Failing in GenerateUSerAskedData - i.e. Compiler data)" );
                                Sup.LogTraceInfoMessage( $"UserAskedData: Message {e.Message})" );
                                Sup.LogTraceInfoMessage( $"UserAskedData: Continuing" );
                            }
                        }
                        else
                        {
                            Sup.LogDebugMessage( $"Errors in Charts definition. See logfile, please correct and run again." );
                        }
                    }

                    Sup.LogTraceInfoMessage( $"DoAirLink / AirQualitySensor  = {DoAirLink} / {HasAirLink}" );
                    if ( HasAirLink )
                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the AirQuality stuff..." );
                        AirLink fncs = new AirLink( Sup );

                        try
                        {
                            await fncs.GenAirLinkDataJson();
                        }
                        catch ( Exception e )
                        {
                            Sup.LogTraceInfoMessage( $"UserAskedData: Failing in GenAirLinkDataJson - i.e. Airlink data)" );
                            Sup.LogTraceInfoMessage( $"UserAskedData: Message {e.Message})" );
                            Sup.LogTraceInfoMessage( $"UserAskedData: Continuing" );
                        }
                    }

                    if ( HasExtraSensors )
                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the ExtraSensor stuff..." );
                        ExtraSensors fncs = new ExtraSensors( Sup );
                        try
                        {
                            fncs.GenerateExtraSensorDataJson();
                        }
                        catch ( Exception e )
                        {
                            Sup.LogTraceInfoMessage( $"UserAskedData: Failing in GenerateExtraSensorDataJson - i.e. ExtraSensors (incl External) data)" );
                            Sup.LogTraceInfoMessage( $"UserAskedData: Message - {e.Message})" );
                            Sup.LogTraceInfoMessage( $"UserAskedData: Continuing" );
                        }
                    }

                    if ( HasCustomLogs )
                    {
                        Sup.LogTraceInfoMessage( $"UserAskedData Doing the CustomLogs stuff..." );
                        CustomLogs fncs = new CustomLogs( Sup );
                        try
                        {
                            fncs.GenerateCustomLogsDataJson( NonIncremental: false );
                        }
                        catch ( Exception e )
                        {
                            Sup.LogTraceInfoMessage( $"UserAskedData: Failing in GenerateCustomLogsDataJson" );
                            Sup.LogTraceInfoMessage( $"UserAskedData: Message - {e.Message})" );
                            Sup.LogTraceInfoMessage( $"UserAskedData: Continuing" );
                        }
                    }

                    // No matter what happened, set the upload date/time
                    Sup.SetUtilsIniValue( "General", "LastUploadTime", tmpTimeEnd.ToString( "dd/MM/yy HH:mm", Inv ) );

#if TIMING
                    watch.Stop();
                    Sup.LogTraceInfoMessage( $"Timing of UserAskedData = {watch.ElapsedMilliseconds} ms" );
#endif
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
                        Sup.LogTraceInfoMessage( $"Uploading = {thisDef.Filename}" );
                        await Isup.UploadFileAsync( $"{thisDef.Filename}", $"{Sup.PathUtils}{thisDef.Filename}" );
                    }
                }
                else
                {
                    Sup.LogDebugMessage( $"Errors in Charts definition. See logfile, please correct and run again." );
                }

#if TIMING
                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of Compile and Generate CumulusCharts = {watch.ElapsedMilliseconds} ms" );
#endif
            }

            //********************************  Do the uploading when required **************************************
            //

            if ( !Thrifty && !DoUserAskedData )
            {
                // Always upload the package files
                Sup.LogTraceInfoMessage( $"Uploading = The Package" );
                await Sup.CheckPackageAndCopy();
            }

            if ( DoWebsite )
            {
                Sup.LogTraceInfoMessage( $"Uploading = {Sup.IndexOutputFilename}" );
                await Isup.UploadFileAsync( $"{Sup.IndexOutputFilename}", $"{Sup.PathUtils}{Sup.IndexOutputFilename}" );
            }

            if ( DoPwsFWI )
            {
                Sup.LogTraceInfoMessage( $"Uploading = {Sup.PwsFWIOutputFilename}" );
                await Isup.UploadFileAsync( $"{Sup.PwsFWIOutputFilename}", $"{Sup.PathUtils}{Sup.PwsFWIOutputFilename}" );

                Sup.LogTraceInfoMessage( $"Uploading = {Sup.PwsFWICurrentOutputFilename}" );
                await Isup.UploadFileAsync( $"{Sup.PwsFWICurrentOutputFilename}", $"{Sup.PathUtils}{Sup.PwsFWICurrentOutputFilename}" );
            }

            if ( DoTop10 && ( !Thrifty || ThriftyTop10RecordsDirty ) )
            {
                Sup.LogTraceInfoMessage( $"Thrifty: DoTop10 && (!Thrifty || ThriftyTop10RecordsDirty ) - " +
                  $"{DoTop10 && ( !Thrifty || ThriftyTop10RecordsDirty )} | Uploading = {Sup.Top10OutputFilename}" );
                await Isup.UploadFileAsync( $"{Sup.Top10OutputFilename}", $"{Sup.PathUtils}{Sup.Top10OutputFilename}" );
            }

            if ( DoGraphs )
            { // 
                if ( HasRainGraphMenu && ( !Thrifty || ThriftyRainGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyRainGraphsDirty - {!Thrifty || ThriftyRainGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsRainOutputFilename )}" );
                    await Isup.UploadFileAsync( Path.GetFileName( Sup.GraphsRainOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsRainOutputFilename ) );
                }

                if ( HasTempGraphMenu && ( !Thrifty || ThriftyTempGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyTempGraphsDirty - {!Thrifty || ThriftyTempGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsTempOutputFilename )}" );
                    await Isup.UploadFileAsync( Path.GetFileName( Sup.GraphsTempOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsTempOutputFilename ) );
                }

                if ( HasWindGraphMenu && ( !Thrifty || ThriftyWindGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyWindGraphsDirty) - {!Thrifty || ThriftyWindGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsWindOutputFilename )}" );
                    await Isup.UploadFileAsync( Path.GetFileName( Sup.GraphsWindOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsWindOutputFilename ) );
                }

                if ( HasSolarGraphMenu && ( !Thrifty || ThriftySolarGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftySolarGraphsDirty) - {!Thrifty || ThriftySolarGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsSolarOutputFilename )}" );
                    await Isup.UploadFileAsync( Path.GetFileName( Sup.GraphsSolarOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsSolarOutputFilename ) );
                }

                if ( HasMiscGraphMenu && ( !Thrifty || ThriftyMiscGraphsDirty ) )
                {
                    Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyMiscGraphsDirty - {!Thrifty || ThriftyMiscGraphsDirty} => Uploading = {Path.GetFileName( Sup.GraphsMiscOutputFilename )}" );
                    await Isup.UploadFileAsync( Path.GetFileName( Sup.GraphsMiscOutputFilename ), Sup.PathUtils + Path.GetFileName( Sup.GraphsMiscOutputFilename ) );
                }
            }

            if ( MapParticipant || DoWebsite )
            {
                Sup.LogTraceInfoMessage( $"Uploading = {Sup.MapsOutputFilename}" );
                await Isup.UploadFileAsync( $"{Sup.MapsOutputFilename}", $"{Sup.PathUtils}{Sup.MapsOutputFilename}" );
            }

            if ( DoRecords && ( !Thrifty || ThriftyRecordsDirty ) )
            {
                Sup.LogTraceInfoMessage( $"Thrifty: DoRecords && (!Thrifty || ThriftyRecordsDirty) - {DoRecords && ( !Thrifty || ThriftyRecordsDirty )} => Uploading = {Sup.RecordsOutputFilename}" );
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

            int StartYear = DateTime.Now.Month > 6 && DateTime.Now.Month <= 12 ? DateTime.Now.Year : DateTime.Now.Year - 1;

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

                    Sup.LogTraceInfoMessage( $"Thrifty: {Thrifty} - YADR - Complete upload" );

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
                        Sup.LogTraceInfoMessage( $"Thrifty: {Thrifty} - YADR - Upload for 2 January" );
                        await Isup.UploadFileAsync( Path.GetFileName( "Yadr.txt" ), Sup.PathUtils + Path.GetFileName( "Yadr.txt" ) );
                    }

                    Sup.LogTraceInfoMessage( $"Thrifty: {Thrifty} - YADR - Upload for only current year {RunStarted.Year}" );
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

                    Sup.LogTraceInfoMessage( $"Uploading => {fi.Name} from {Sup.PathUtils}{fi.Name}" );
                    if ( await Isup.UploadFileAsync( $"{fi.Name}", $"{Sup.PathUtils}{fi.Name}" ) ) fi.Delete();
                }
            }

            // Before v4.0.0 SysInfo must be processed by Cumulus so not handy to do the uploading here but now we do
            if ( DoSystemChk ) { await Isup.UploadFileAsync( $"{Sup.SysInfoOutputFilename}", $"{Sup.PathUtils}{Sup.SysInfoOutputFilename}" ); }

#if TIMING
            OverallWatch.Stop();
            Sup.LogTraceInfoMessage( $"Overall Timing all Modules = {OverallWatch.ElapsedMilliseconds} ms" );
#endif

            return;
        }

        #endregion

        #region CommandLineArgs
        private void CommandLineArgs( string[] args )
        {
            Sup.LogDebugMessage( "CommandLineArgs : starting" );

            foreach ( string s in args )
            {
                Sup.LogDebugMessage( $" CommandLineArgs : handling arg: {s}" );

                if ( s.Equals( "Website", Cmp ) )
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
                    DoExtraSensors = true;
                    DoCustomLogs = true;
                    DoCUlib = true;            // this is implicit for website so if user sets it undo tha
                    DoDiary = true;

                    break;
                }
                else
                {
                    if ( s.Equals( "Thrifty", Cmp ) )
                    {
                        Thrifty = true;
                    }
                    else
                    {
                        if ( s.Equals( "Top10", Cmp ) ) DoTop10 = true;
                        if ( s.Equals( "pwsFWI", Cmp ) ) DoPwsFWI = true;
                        if ( s.Equals( "Sysinfo", Cmp ) ) DoSystemChk = true;
                        if ( s.Equals( "Graphs", Cmp ) ) DoGraphs = true;
                        if ( s.Equals( "CreateMap", Cmp ) ) DoCreateMap = true;    // Undocumented feature only for the keeper of the map
                        if ( s.Equals( "Yadr", Cmp ) ) DoYadr = true;
                        if ( s.Equals( "Records", Cmp ) ) DoRecords = true;
                        if ( s.Equals( "NOAA", Cmp ) ) DoNOAA = true;
                        if ( s.Equals( "DayRecords", Cmp ) ) DoDayRecords = true;
                        if ( s.Equals( "Forecast", Cmp ) ) DoForecast = true;
                        if ( s.Equals( "UserReports", Cmp ) ) DoUserReports = true;
                        if ( s.Equals( "StationMap", Cmp ) ) DoStationMap = true;
                        if ( s.Equals( "MeteoCam", Cmp ) ) DoMeteoCam = true;
                        if ( s.Equals( "AirLink", Cmp ) ) DoAirLink = true;
                        if ( s.Equals( "CompileOnly", Cmp ) ) DoCompileOnly = true;
                        if ( s.Equals( "ExtraSensors", Cmp ) )
                        {
                            DoExtraSensors = true;
                            DoCompileOnly = true;  // Implicit for Extra Sensors
                        }
                        if ( s.Equals( "UserAskedData", Cmp ) ) DoUserAskedData = true;
                        if ( s.Equals( "CustomLogs", Cmp ) )
                        {
                            DoCustomLogs = true;
                            DoCompileOnly = true;  // Implicit for Custom Logs
                        }
                        if ( s.Equals( "CUlib", Cmp ) ) DoCUlib = true;
                        if ( s.Equals( "Diary", Cmp ) ) DoDiary = true;
                    }
                }
            }
        } // Commandline handling

        #endregion

    } // Class CUtils
} // namespace