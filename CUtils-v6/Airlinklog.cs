/*
 * Airlinklog - Part of CumulusUtils
 *
 * © Copyright 2019 - 2021 Hans Rottier <hans.rottier@gmail.com>
 *
 * When the code is made public domain the licence will be changed to the GNU 
 * General Public License as published by the Free Software Foundation;
 * Until then, the code of CumulusUtils is not public domain and only the executable is 
 * distributed under the  Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * As a consequence, this code should not be in your posession unless with explicit permission by Hans Rottier
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
 *              C# / Visual Studio
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace CumulusUtils
{
    internal enum AirlinklogType
    {
        DashSemicolonComma,   // date separator, ; fieldseparator, , decimal fraction
        SlashSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        PointSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        DashCommaPoint,       // - date separator, , fieldseparator, . decimal fraction
        SlashCommaPoint       // date separator, , fieldseparator, . decimal fraction
    };

    internal enum AirlinklogFieldName
    {
        thisDate, thisTime,
        In_temp, In_hum,
        In_pm1, In_pm2p5, In_pm2p5_1hr, In_pm2p5_3hr, In_pm2p5_24hr, In_pm2p5_nowcast, In_pm10, In_pm10_1hr, In_pm10_3hr, In_pm10_24hr, In_pm10_nowcast,
        In_pct_1hr, In_pct_3hr, In_pct_24hr, In_pct_nowcast,
        In_AQIpm2p5, In_AQIpm2p5_1hr, In_AQIpm2p5_3hr, In_AQIpm2p5_24hr, In_AQIpm2p5_nowcast, In_AQIPm10, In_AQIPm10_1hr, In_AQIPm10_3hr, In_AQIPm10_24hr, In_AQIPm10_nowcast,
        Out_temp, Out_hum,
        Out_pm1, Out_pm2p5, Out_pm2p5_1hr, Out_pm2p5_3hr, Out_pm2p5_24hr, Out_pm2p5_nowcast, Out_pm10, Out_pm10_1hr, Out_pm10_3hr, Out_pm10_24hr, Out_pm10_nowcast,
        Out_pct_1hr, Out_pct_3hr, Out_pct_24hr, Out_pct_nowcast,
        Out_AQIpm2p5, Out_AQIpm2p5_1hr, Out_AQIpm2p5_3hr, Out_AQIpm2p5_24hr, Out_AQIpm2p5_nowcast, Out_AQIPm10, Out_AQIPm10_1hr, Out_AQIPm10_3hr, Out_AQIPm10_24hr, Out_AQIPm10_nowcast
    };

    internal struct AirlinklogValue
    {
        public DateTime ThisDate { get; set; }
        public double In_temp { get; set; }
        public int In_hum { get; set; }

        // In
        public double In_pm1 { get; set; }
        public double In_pm2p5 { get; set; }
        public double In_pm2p5_1hr { get; set; }
        public double In_pm2p5_3hr { get; set; }
        public double In_pm2p5_24hr { get; set; }
        public double In_pm2p5_nowcast { get; set; }

        public double In_pm10 { get; set; }
        public double In_pm10_1hr { get; set; }
        public double In_pm10_3hr { get; set; }
        public double In_pm10_24hr { get; set; }
        public double In_pm10_nowcast { get; set; }

        public int In_pct_1hr { get; set; }
        public int In_pct_3hr { get; set; }
        public int In_pct_24hr { get; set; }
        public int In_pct_nowcast { get; set; }

        public double In_AQIpm2p5 { get; set; }
        public double In_AQIpm2p5_1hr { get; set; }
        public double In_AQIpm2p5_3hr { get; set; }
        public double In_AQIpm2p5_24hr { get; set; }
        public double In_AQIpm2p5_nowcast { get; set; }
        public double In_AQIPm10 { get; set; }
        public double In_AQIPm10_1hr { get; set; }
        public double In_AQIPm10_3hr { get; set; }
        public double In_AQIPm10_24hr { get; set; }
        public double In_AQIPm10_nowcast { get; set; }

        // Out
        public double Out_temp { get; set; }
        public double Out_hum { get; set; }
        public double Out_pm1 { get; set; }
        public double Out_pm2p5 { get; set; }
        public double Out_pm2p5_1hr { get; set; }
        public double Out_pm2p5_3hr { get; set; }
        public double Out_pm2p5_24hr { get; set; }
        public double Out_pm2p5_nowcast { get; set; }
        public double Out_pm10 { get; set; }
        public double Out_pm10_1hr { get; set; }
        public double Out_pm10_3hr { get; set; }
        public double Out_pm10_24hr { get; set; }
        public double Out_pm10_nowcast { get; set; }

        public int Out_pct_1hr { get; set; }
        public int Out_pct_3hr { get; set; }
        public int Out_pct_24hr { get; set; }
        public int Out_pct_nowcast { get; set; }

        public double Out_AQIpm2p5 { get; set; }
        public double Out_AQIpm2p5_1hr { get; set; }
        public double Out_AQIpm2p5_3hr { get; set; }
        public double Out_AQIpm2p5_24hr { get; set; }
        public double Out_AQIpm2p5_nowcast { get; set; }
        public double Out_AQIPm10 { get; set; }
        public double Out_AQIPm10_1hr { get; set; }
        public double Out_AQIPm10_3hr { get; set; }
        public double Out_AQIPm10_24hr { get; set; }
        public double Out_AQIPm10_nowcast { get; set; }

        public bool Valid { get; set; }
    }

    internal class Airlinklog : IDisposable
    {
        private readonly AirlinklogType type;
        private readonly CuSupport Sup;
        private readonly bool IgnoreDataErrors;
        private readonly string[] enumFieldTypeNames;
        private readonly string[] AirlinklogList;

        private bool disposed;
        private string filenameCopy;

        const int MaxErrors = 10;
        int ErrorCount;

        internal Airlinklog( CuSupport s )
        {
            string line;

            Sup = s;
            Sup.LogDebugMessage( $"Airlinklog constructor: Using fixed path: | data/ |; file: | *log.txt" );

            // Get the list of Airlink logfile in the datadirectory and check what type of delimeters we have
            AirlinklogList = Directory.GetFiles( "data/", "AirLink*.txt" );

            if ( AirlinklogList.Length >= 0 )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( AirlinklogList[ 0 ] );
                Sup.LogDebugMessage( $"Airlinklog constructor: Using {filenameCopy}" );
            }
            else return;

            if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
            File.Copy( AirlinklogList[ 0 ], filenameCopy );

            // Not sure about encoding of this file, let it be handled by the system. No presumtions.
            //
            using ( StreamReader mf = new StreamReader( filenameCopy ) )
            {
                Sup.LogTraceVerboseMessage( $"Airlinklog constructor: Working on: {filenameCopy}" );

                line = mf.ReadLine();

                if ( line[ 2 ] == '-' && line[ 8 ] == ';' ) type = AirlinklogType.DashSemicolonComma;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ';' ) type = AirlinklogType.SlashSemicolonComma;
                else if ( line[ 2 ] == '.' && line[ 8 ] == ';' ) type = AirlinklogType.PointSemicolonComma;
                else if ( line[ 2 ] == '-' && line[ 8 ] == ',' ) type = AirlinklogType.DashCommaPoint;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ',' ) type = AirlinklogType.SlashCommaPoint;
                else
                {
                    Sup.LogTraceErrorMessage( "Airlinklog constructor: Internal Error - Unkown format of inputfile. Please notify programmer." );
                    Environment.Exit( 0 );
                }
            }

            File.Delete( filenameCopy );

            Sup.LogTraceInfoMessage( $"Airlinklog constructor: AirlinklogType is {type}" );
            enumFieldTypeNames = Enum.GetNames( typeof( AirlinklogFieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true" );

            if ( AirlinklogList.Length >= 0 && Sup.GetUtilsIniValue( "AirLink", "CleanupAirlinkLogs", "false" ).Equals( "true" ) )
            {
                // We keep two month of data, the rest can be discarded
                foreach ( string thisFile in AirlinklogList )
                {
                    if ( CMXutils.RunStarted.Month - File.GetLastWriteTime( thisFile ).Month > 2 )
                    {
                        try { File.Delete( thisFile ); }
                        catch { Sup.LogTraceInfoMessage( $"Airlinklog constructor: Can't clean up / delete {thisFile}" ); }
                    }
                }
            }

            return;
        }

        internal List<AirlinklogValue> MainAirLinkList;

        internal List<AirlinklogValue> ReadAirlinklog()
        {
            // Get the list of values starting datetim NOW to Now - period by user definition GraphHours in section Graphs in Cumulus.ini
            //
            Sup.LogDebugMessage( $"ReadAirlinklog: start." );

            Stopwatch watch = Stopwatch.StartNew();

            bool PeriodComplete = false;

            string Filename;
            double HoursInGraph = Convert.ToDouble( Sup.GetCumulusIniValue( "Graphs", "GraphHours", "" ) );

            // This is shared with GraphSolar... should become some common function to get this interval from the Cumulus.ini file
            // Also shared with ChartsCompiler... have to clean this up and do a shared function somewhere
            // Will do at some time.
            int[] PossibleIntervals = { 1, 5, 10, 15, 20, 30 };
            int LogIntervalInMinutes = PossibleIntervals[ Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "DataLogInterval", "" ), CultureInfo.InvariantCulture ) ];
            int FTPIntervalInMinutes = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "" ) );

            //
            // Take the FTP frequency or the LogInterval (whichever is the largest) and use the minute value being a multiple of that one cycle below the now time as the end time
            // Then go the hours in Graphs back to complete the full cycle. 
            // So with a 10 min FTP cycle and Now = 08h09 the endtime must be 08h00 -> the minute value MOD FTP frequency
            // This should give it the same starttime as the CMX JSONS, this is relevant for the wind addition later on.
            // This is also shared with the ChartsCompiler -> make some shared function for start and endtime related to the intervals.
            //
            DateTime Now = DateTime.Now;
            DateTime timeEnd = Now.AddMinutes( -Now.Minute % Math.Max( FTPIntervalInMinutes, LogIntervalInMinutes ) );
            DateTime timeStart = timeEnd.AddHours( -HoursInGraph );

            Sup.LogDebugMessage( $"AirLinklog: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            AirlinklogValue tmp;
            MainAirLinkList = new List<AirlinklogValue>();

            Filename = $"data/AirLink{timeStart:yyyy}{timeStart:MM}log.txt";
            if ( !File.Exists( Filename ) )
            {
                Sup.LogDebugMessage( $"AirLinklog: Require {Filename} to start but it does not exist, aborting AirLinkLog" );
                return MainAirLinkList;
            }

            Sup.LogDebugMessage( $"AirLinklog: Require {Filename} to start" );

            while ( !PeriodComplete )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );
                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                File.Copy( Filename, filenameCopy );

                using ( StreamReader af = new StreamReader( filenameCopy ) )
                {
                    string line = ReadLine( af, SanityCheck: true );

                    // Loop over all lines in file
                    do
                    {
                        // OK, continue here : check date and create only the list  we need (actually only read the files we need
                        tmp = SetValues( line, timeStart );
                        if ( tmp.Valid )
                            MainAirLinkList.Add( tmp );
                        if ( tmp.ThisDate >= timeEnd ) break; // we have our set of data required
                        line = ReadLine( af, false );
                    } while ( !string.IsNullOrEmpty( line ) );
                } // End Using the AirLink Log to Read

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                if ( tmp.ThisDate.Month == timeEnd.Month )
                {
                    Sup.LogDebugMessage( $"AirLinklog: Finished reading the log at {tmp.ThisDate}" );
                    PeriodComplete = true;
                }
                else
                {
                    Filename = $"data/AirLink{timeEnd:yyyy}{timeEnd:MM}log.txt";  // Take care of a period passing month boundary
                    Sup.LogDebugMessage( $"AirLinklog: Require the  next logfile: {Filename}" );
                }
            } // Loop over all files in AirlinkfileList

            Sup.LogTraceInfoMessage( $"ReadAirlinklog: MainMonthList created: {MainAirLinkList.Count} records." );
            watch.Stop();
            Sup.LogTraceInfoMessage( $"ReadAirlinklog: Timing of Airlinklogfile read = {watch.ElapsedMilliseconds} ms" );
            Sup.LogTraceInfoMessage( $"ReadAirlinklog: End" );

            return MainAirLinkList;
        } // End ReadAirlinklog

        private string ReadLine( StreamReader af, bool SanityCheck )
        {
            //int i, separatorCount = 0;
            StringBuilder tmpLine = new StringBuilder();

            if ( af.EndOfStream )
            {
                Sup.LogTraceInfoMessage( "Airlinklog : EOF detected" ); // nothing to do;
            }
            else
            {
                bool SeparatorInconsistencyFound = false;

                tmpLine.Append( af.ReadLine() );

                if ( SanityCheck )
                {
                    // Do a sanity check on the presence of the correct separators which we determined when starting the reading process
                    //
                    switch ( type )
                    {
                        case AirlinklogType.DashSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ';' ) ) ) SeparatorInconsistencyFound = true; break;

                        case AirlinklogType.PointSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '.' ) && ( tmpLine[ 8 ] == ';' ) ) ) SeparatorInconsistencyFound = true; break;

                        case AirlinklogType.SlashSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ';' ) ) ) SeparatorInconsistencyFound = true; break;

                        case AirlinklogType.DashCommaPoint:
                            if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ',' ) ) ) SeparatorInconsistencyFound = true; break;

                        case AirlinklogType.SlashCommaPoint:
                            if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ',' ) ) ) SeparatorInconsistencyFound = true; break;

                        default:
                            // Should never be here
                            SeparatorInconsistencyFound = true;
                            break;
                    }
                }

                if ( SeparatorInconsistencyFound )
                {
                    Sup.LogTraceErrorMessage( $"Airlinklog: Illegal part of the code, should never be here, FATAL ERROR!" );
                    Sup.LogTraceErrorMessage( $"Airlinklog: Separator Inconsistency in {filenameCopy.Remove( 0, 5 )} found, FATAL ERROR!" );
                    Sup.LogTraceErrorMessage( $"Airlinklog: Please check {filenameCopy.Remove( 0, 5 )} and the data directory." );
                    //Dispose();
                    //Environment.Exit( 0 );
                }
                else
                {
                    /*
                     * make a uniform line to read: convert all to SlashCommaPoint
                     */
                    if ( type == AirlinklogType.DashSemicolonComma )
                    {
                        tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == AirlinklogType.PointSemicolonComma )
                    {
                        tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == AirlinklogType.SlashSemicolonComma )
                    {
                        //tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == AirlinklogType.DashCommaPoint )
                    {
                        tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                    }
                }// NO Separator Inconsistency
            } // Not EOF

            return tmpLine.ToString();
        }

        private AirlinklogValue SetValues( string line, DateTime StartTime )
        {
            string tmpDatestring;
            string[] lineSplit = line.Split( ',' );

            AirlinklogValue ThisValue = new AirlinklogValue();

            int FieldInUse = (int) AirlinklogFieldName.thisDate;
            CultureInfo provider = CultureInfo.InvariantCulture;

            try
            {
                // DateTime
                tmpDatestring = lineSplit[ FieldInUse ];
                FieldInUse = (int) AirlinklogFieldName.thisTime;
                tmpDatestring += " " + lineSplit[ FieldInUse ];
                ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy HH:mm", provider );

                if ( ThisValue.ThisDate < StartTime )
                {
                    // Not within date range, try next line, possibly next  file
                    ThisValue.Valid = false;
                }
                else // Within date range
                {
                    ThisValue.Valid = true;

                    // Inside sensor
                    FieldInUse = (int) AirlinklogFieldName.In_temp;
                    ThisValue.In_temp = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_hum;
                    ThisValue.In_hum = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm1;
                    ThisValue.In_pm1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5;
                    ThisValue.In_pm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_1hr;
                    ThisValue.In_pm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_3hr;
                    ThisValue.In_pm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_24hr;
                    ThisValue.In_pm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_nowcast;
                    ThisValue.In_pm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10;
                    ThisValue.In_pm10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_1hr;
                    ThisValue.In_pm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_3hr;
                    ThisValue.In_pm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_24hr;
                    ThisValue.In_pm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_nowcast;
                    ThisValue.In_pm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_1hr;
                    ThisValue.In_pct_1hr = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_3hr;
                    ThisValue.In_pct_3hr = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_24hr;
                    ThisValue.In_pct_24hr = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_nowcast;
                    ThisValue.In_pct_nowcast = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5;
                    ThisValue.In_AQIpm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_1hr;
                    ThisValue.In_AQIpm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_3hr;
                    ThisValue.In_AQIpm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_24hr;
                    ThisValue.In_AQIpm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_nowcast;
                    ThisValue.In_AQIpm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10;
                    ThisValue.In_AQIPm10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_1hr;
                    ThisValue.In_AQIPm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_3hr;
                    ThisValue.In_AQIPm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_24hr;
                    ThisValue.In_AQIPm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_nowcast;
                    ThisValue.In_AQIPm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    // Now the Out values
                    FieldInUse = (int) AirlinklogFieldName.Out_temp;
                    ThisValue.Out_temp = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_hum;
                    ThisValue.Out_hum = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm1;
                    ThisValue.Out_pm1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5;
                    ThisValue.Out_pm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_1hr;
                    ThisValue.Out_pm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_3hr;
                    ThisValue.Out_pm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_24hr;
                    ThisValue.Out_pm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_nowcast;
                    ThisValue.Out_pm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10;
                    ThisValue.Out_pm10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_1hr;
                    ThisValue.Out_pm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_3hr;
                    ThisValue.Out_pm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_24hr;
                    ThisValue.Out_pm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_nowcast;
                    ThisValue.Out_pm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_1hr;
                    ThisValue.Out_pct_1hr = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_3hr;
                    ThisValue.Out_pct_3hr = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_24hr;
                    ThisValue.Out_pct_24hr = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_nowcast;
                    ThisValue.Out_pct_nowcast = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5;
                    ThisValue.Out_AQIpm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_1hr;
                    ThisValue.Out_AQIpm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_3hr;
                    ThisValue.Out_AQIpm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_24hr;
                    ThisValue.Out_AQIpm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_nowcast;
                    ThisValue.Out_AQIpm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10;
                    ThisValue.Out_AQIPm10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_1hr;
                    ThisValue.Out_AQIPm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_3hr;
                    ThisValue.Out_AQIPm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_24hr;
                    ThisValue.Out_AQIPm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_nowcast;
                    ThisValue.Out_AQIPm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    Sup.LogTraceVerboseMessage( "SetValues after adding the values:" );
                    Sup.LogTraceVerboseMessage( $"SetValues after adding the values: Original Line {line}" );
                }
            } // try
            catch ( Exception e ) when ( e is FormatException || e is OverflowException )
            {
                const string m = "AirlinkValue.SetValues";

                ErrorCount++;

                //handle exception
                if ( ErrorCount < MaxErrors )
                {
                    Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} ({enumFieldTypeNames[ FieldInUse ]})" );
                    Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );

                    Console.WriteLine( $"{m} fail: {e.Message}" );
                    Console.WriteLine( $"{m}: in field nr {FieldInUse} ({enumFieldTypeNames[ FieldInUse ]})" );

                    if ( string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) )
                    {
                        Sup.LogTraceErrorMessage( $"{m}: Field {enumFieldTypeNames[ FieldInUse ]} is Empty" );
                    }
                }

                if ( IgnoreDataErrors )
                    if ( ErrorCount < MaxErrors ) Sup.LogTraceErrorMessage( "AirlinklogValue.SetValues : Continuing to read data" );
                    else
                        // Environment.Exit(0);
                        throw;
            }
            catch ( IndexOutOfRangeException e )
            {
                const string m = "AirlinklogValue.SetValues";

                ErrorCount++;

                if ( ErrorCount < MaxErrors )
                {
                    Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} does  not exist in this file {filenameCopy}" );
                    Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );
                }

                if ( IgnoreDataErrors )
                    if ( ErrorCount < MaxErrors ) Sup.LogTraceErrorMessage( "AirlinklogValue.SetValues : Continuing to read data" );
                    else
                        throw;
            }

            return ThisValue;
        }

        ~Airlinklog()
        {
            Sup.LogTraceVerboseMessage( "Airlinklog destructor: Closing file and ending program" );
            Dispose( false );
        }

        public virtual void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposed )
            {
                if ( disposing )
                {
                    // release the large, managed resource here
                }

                // release unmagaed resources here
                disposed = true;
            }
        }
    }
}