/*
 * Airlinklog - Part of CumulusUtils
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
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentFTP.Helpers;

namespace CumulusUtils
{
    public enum AirlinklogType
    {
        DashSemicolonComma,   // date separator, ; fieldseparator, , decimal fraction
        SlashSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        PointSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        DashCommaPoint,       // - date separator, , fieldseparator, . decimal fraction
        SlashCommaPoint       // date separator, , fieldseparator, . decimal fraction
    };

    public enum AirlinklogFieldName
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

    public struct AirlinklogValue
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

    public class Airlinklog : IDisposable
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

        public Airlinklog( CuSupport s )
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
            else
                return;

            if ( File.Exists( filenameCopy ) )
                File.Delete( filenameCopy );
            File.Copy( AirlinklogList[ 0 ], filenameCopy );

            // Not sure about encoding of this file, let it be handled by the system. No presumtions.
            //
            using ( StreamReader mf = new StreamReader( filenameCopy ) )
            {
                Sup.LogTraceVerboseMessage( $"Airlinklog constructor: Working on: {filenameCopy}" );

                line = mf.ReadLine();

                if ( line[ 2 ] == '-' && line[ 8 ] == ';' )
                    type = AirlinklogType.DashSemicolonComma;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ';' )
                    type = AirlinklogType.SlashSemicolonComma;
                else if ( line[ 2 ] == '.' && line[ 8 ] == ';' )
                    type = AirlinklogType.PointSemicolonComma;
                else if ( line[ 2 ] == '-' && line[ 8 ] == ',' )
                    type = AirlinklogType.DashCommaPoint;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ',' )
                    type = AirlinklogType.SlashCommaPoint;
                else
                {
                    Sup.LogTraceErrorMessage( "Airlinklog constructor: Internal Error - Unkown format of inputfile. Please notify programmer." );
                    Environment.Exit( 0 );
                }
            }

            File.Delete( filenameCopy );

            Sup.LogTraceInfoMessage( $"Airlinklog constructor: AirlinklogType is {type}" );
            enumFieldTypeNames = Enum.GetNames( typeof( AirlinklogFieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true", CUtils.Cmp );

            if ( AirlinklogList.Length >= 0 && Sup.GetUtilsIniValue( "AirLink", "CleanupAirlinkLogs", "false" ).Equals( "true", CUtils.Cmp ) )
            {
                // We keep two month of data, the rest can be discarded
                foreach ( string thisFile in AirlinklogList )
                {
                    if ( CUtils.RunStarted.Month - File.GetLastWriteTime( thisFile ).Month > 2 )
                    {
                        try { File.Delete( thisFile ); }
                        catch { Sup.LogTraceInfoMessage( $"Airlinklog constructor: Can't clean up / delete {thisFile}" ); }
                    }
                }
            }

            return;
        }

        public List<AirlinklogValue> MainAirLinkList;

        public List<AirlinklogValue> ReadAirlinklog()
        {
            // Get the list of values starting datetim NOW to Now - period by user definition GraphHours in section Graphs in Cumulus.ini
            //
            Sup.LogDebugMessage( $"ReadAirlinklog: start." );

            string line;
            bool NextFileTried = false;
            bool PeriodComplete = false;

            string Filename;

            Sup.SetStartAndEndForData( out DateTime timeStart, out DateTime timeEnd );
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
                    line = ReadLine( af, SanityCheck: true );

                    // Loop over all lines in file
                    do
                    {
                        // OK, continue here : check date and create only the list  we need (actually only read the files we need
                        tmp = SetValues( line, timeStart );
                        if ( tmp.Valid )
                            MainAirLinkList.Add( tmp );
                        if ( tmp.ThisDate >= timeEnd )
                            break; // we have our set of data required
                        line = ReadLine( af, false );
                    } while ( !string.IsNullOrEmpty( line ) );
                } // End Using the AirLink Log to Read

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                if ( tmp.ThisDate >= timeEnd || NextFileTried )
                {
                    Sup.LogDebugMessage( $"AirLinklog: Finished reading the log at {tmp.ThisDate}" );
                    PeriodComplete = true;
                }
                else
                {
                    NextFileTried = true;
                    Filename = $"data/AirLink{timeEnd:yyyy}{timeEnd:MM}log.txt";  // Take care of a period passing month boundary
                    Sup.LogDebugMessage( $"AirLinklog: Require the  next logfile: {Filename}" );

                    if ( !File.Exists( Filename ) )
                    {
                        Sup.LogDebugMessage( $"AirLinklog: {Filename} Does not exist so we need to stop reading" );
                        PeriodComplete = true;
                    }
                }
            } // Loop over all files in AirlinkfileList

            Sup.LogTraceInfoMessage( $"ReadAirlinklog: MainMonthList created: {MainAirLinkList.Count} records." );
            Sup.LogTraceInfoMessage( $"ReadAirlinklog: End" );

            return MainAirLinkList;
        } // End ReadAirlinklog

        private string ReadLine( StreamReader af, bool SanityCheck )
        {
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
                            if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ';' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case AirlinklogType.PointSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '.' ) && ( tmpLine[ 8 ] == ';' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case AirlinklogType.SlashSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ';' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case AirlinklogType.DashCommaPoint:
                            if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ',' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case AirlinklogType.SlashCommaPoint:
                            if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ',' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

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
                }
                else
                {
                    /*
                     * make a uniform line to read: convert all to SlashCommaPoint
                     */
                    if ( type == AirlinklogType.DashSemicolonComma )
                    {
                        tmpLine[ 2 ] = '/';
                        tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == AirlinklogType.PointSemicolonComma )
                    {
                        tmpLine[ 2 ] = '/';
                        tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == AirlinklogType.SlashSemicolonComma )
                    {
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == AirlinklogType.DashCommaPoint )
                    {
                        tmpLine[ 2 ] = '/';
                        tmpLine[ 5 ] = '/';
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

            try
            {
                // DateTime
                tmpDatestring = lineSplit[ FieldInUse ];
                FieldInUse = (int) AirlinklogFieldName.thisTime;
                tmpDatestring += " " + lineSplit[ FieldInUse ];
                ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy HH:mm", CUtils.Inv );

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
                    ThisValue.In_temp = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_hum;
                    ThisValue.In_hum = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm1;
                    ThisValue.In_pm1 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5;
                    ThisValue.In_pm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_1hr;
                    ThisValue.In_pm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_3hr;
                    ThisValue.In_pm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_24hr;
                    ThisValue.In_pm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_nowcast;
                    ThisValue.In_pm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10;
                    ThisValue.In_pm10 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_1hr;
                    ThisValue.In_pm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_3hr;
                    ThisValue.In_pm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_24hr;
                    ThisValue.In_pm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_nowcast;
                    ThisValue.In_pm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_1hr;
                    ThisValue.In_pct_1hr = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_3hr;
                    ThisValue.In_pct_3hr = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_24hr;
                    ThisValue.In_pct_24hr = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pct_nowcast;
                    ThisValue.In_pct_nowcast = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5;
                    ThisValue.In_AQIpm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_1hr;
                    ThisValue.In_AQIpm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_3hr;
                    ThisValue.In_AQIpm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_24hr;
                    ThisValue.In_AQIpm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_nowcast;
                    ThisValue.In_AQIpm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10;
                    ThisValue.In_AQIPm10 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_1hr;
                    ThisValue.In_AQIPm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_3hr;
                    ThisValue.In_AQIPm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_24hr;
                    ThisValue.In_AQIPm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_nowcast;
                    ThisValue.In_AQIPm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    // Now the Out values
                    FieldInUse = (int) AirlinklogFieldName.Out_temp;
                    ThisValue.Out_temp = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_hum;
                    ThisValue.Out_hum = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm1;
                    ThisValue.Out_pm1 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5;
                    ThisValue.Out_pm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_1hr;
                    ThisValue.Out_pm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_3hr;
                    ThisValue.Out_pm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_24hr;
                    ThisValue.Out_pm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_nowcast;
                    ThisValue.Out_pm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10;
                    ThisValue.Out_pm10 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_1hr;
                    ThisValue.Out_pm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_3hr;
                    ThisValue.Out_pm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_24hr;
                    ThisValue.Out_pm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_nowcast;
                    ThisValue.Out_pm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_1hr;
                    ThisValue.Out_pct_1hr = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_3hr;
                    ThisValue.Out_pct_3hr = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_24hr;
                    ThisValue.Out_pct_24hr = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_nowcast;
                    ThisValue.Out_pct_nowcast = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5;
                    ThisValue.Out_AQIpm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_1hr;
                    ThisValue.Out_AQIpm2p5_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_3hr;
                    ThisValue.Out_AQIpm2p5_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_24hr;
                    ThisValue.Out_AQIpm2p5_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_nowcast;
                    ThisValue.Out_AQIpm2p5_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10;
                    ThisValue.Out_AQIPm10 = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_1hr;
                    ThisValue.Out_AQIPm10_1hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_3hr;
                    ThisValue.Out_AQIPm10_3hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_24hr;
                    ThisValue.Out_AQIPm10_24hr = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_nowcast;
                    ThisValue.Out_AQIPm10_nowcast = Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv );

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

                    if ( lineSplit[ FieldInUse ].IsBlank() )
                        Sup.LogTraceErrorMessage( $"{m}: Field {enumFieldTypeNames[ FieldInUse ]} is Empty" );
                }

                if ( IgnoreDataErrors )
                    if ( ErrorCount < MaxErrors )
                        Sup.LogTraceErrorMessage( "AirlinklogValue.SetValues : Continuing to read data" );
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
                    if ( ErrorCount < MaxErrors )
                        Sup.LogTraceErrorMessage( "AirlinklogValue.SetValues : Continuing to read data" );
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