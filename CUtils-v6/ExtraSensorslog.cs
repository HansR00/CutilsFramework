/*
 * ExtraSensorslog - Part of CumulusUtils
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
    internal enum ExtraSensorslogType
    {
        DashSemicolonComma,   // date separator, ; fieldseparator, , decimal fraction
        SlashSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        PointSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        DashCommaPoint,       // - date separator, , fieldseparator, . decimal fraction
        SlashCommaPoint       // date separator, , fieldseparator, . decimal fraction
    };

    internal enum ExtraSensorslogFieldName
    {
        // From the headerfile in the CMX distribution (like all logreaders)
        thisDate, thisTime, Temp1, Temp2, Temp3, Temp4, Temp5, Temp6, Temp7, Temp8, Temp9, Temp10,
        Humidity1, Humidity2, Humidity3, Humidity4, Humidity5, Humidity6, Humidity7, Humidity8, Humidity9, Humidity10,
        DewPoint1, DewPoint2, DewPoint3, DewPoint4, DewPoint5, DewPoint6, DewPoint7, DewPoint8, DewPoint9, DewPoint10,
        SoilTemp1, SoilTemp2, SoilTemp3, SoilTemp4,
        SoilMoisture1, SoilMoisture2, SoilMoisture3, SoilMoisture4,
        LeafTemp1, LeafTemp2, LeafWetness1, LeafWetness2,
        SoilTemp5, SoilTemp6, SoilTemp7, SoilTemp8, SoilTemp9, SoilTemp10, SoilTemp11, SoilTemp12, SoilTemp13, SoilTemp14, SoilTemp15, SoilTemp16,
        SoilMoisture5, SoilMoisture6, SoilMoisture7, SoilMoisture8, SoilMoisture9, SoilMoisture10, SoilMoisture11, SoilMoisture12, SoilMoisture13, SoilMoisture14, SoilMoisture15, SoilMoisture16,
        AirQuality1, AirQuality2, AirQuality3, AirQuality4, AirQualityAvg1, AirQualityAvg2, AirQualityAvg3, AirQualityAvg4,
        UserTemp1, UserTemp2, UserTemp3, UserTemp4, UserTemp5, UserTemp6, UserTemp7, UserTemp8,
        CO2, CO2Avg, CO2_pm2_5, CO2_pm2_5_avg, CO2_pm10, CO2_PM10_avg, CO2_Temp, CO2_Hum
    };

    internal struct ExtraSensorslogValue
    {
        public DateTime ThisDate { get; set; }

        public double Temp1 { get; set; }
        public double Temp2 { get; set; }
        public double Temp3 { get; set; }
        public double Temp4 { get; set; }
        public double Temp5 { get; set; }
        public double Temp6 { get; set; }
        public double Temp7 { get; set; }
        public double Temp8 { get; set; }
        public double Temp9 { get; set; }
        public double Temp10 { get; set; }

        public double Humidity1 { get; set; }
        public double Humidity2 { get; set; }
        public double Humidity3 { get; set; }
        public double Humidity4 { get; set; }
        public double Humidity5 { get; set; }
        public double Humidity6 { get; set; }
        public double Humidity7 { get; set; }
        public double Humidity8 { get; set; }
        public double Humidity9 { get; set; }
        public double Humidity10 { get; set; }

        public double Dewpoint1 { get; set; }
        public double Dewpoint2 { get; set; }
        public double Dewpoint3 { get; set; }
        public double Dewpoint4 { get; set; }
        public double Dewpoint5 { get; set; }
        public double Dewpoint6 { get; set; }
        public double Dewpoint7 { get; set; }
        public double Dewpoint8 { get; set; }
        public double Dewpoint9 { get; set; }
        public double Dewpoint10 { get; set; }

        public double SoilTemp1 { get; set; }
        public double SoilTemp2 { get; set; }
        public double SoilTemp3 { get; set; }
        public double SoilTemp4 { get; set; }
        public double SoilTemp5 { get; set; }
        public double SoilTemp6 { get; set; }
        public double SoilTemp7 { get; set; }
        public double SoilTemp8 { get; set; }
        public double SoilTemp9 { get; set; }
        public double SoilTemp10 { get; set; }
        public double SoilTemp11 { get; set; }
        public double SoilTemp12 { get; set; }
        public double SoilTemp13 { get; set; }
        public double SoilTemp14 { get; set; }
        public double SoilTemp15 { get; set; }
        public double SoilTemp16 { get; set; }


        public double SoilMoisture1 { get; set; }
        public double SoilMoisture2 { get; set; }
        public double SoilMoisture3 { get; set; }
        public double SoilMoisture4 { get; set; }
        public double SoilMoisture5 { get; set; }
        public double SoilMoisture6 { get; set; }
        public double SoilMoisture7 { get; set; }
        public double SoilMoisture8 { get; set; }
        public double SoilMoisture9 { get; set; }
        public double SoilMoisture10 { get; set; }
        public double SoilMoisture11 { get; set; }
        public double SoilMoisture12 { get; set; }
        public double SoilMoisture13 { get; set; }
        public double SoilMoisture14 { get; set; }
        public double SoilMoisture15 { get; set; }
        public double SoilMoisture16 { get; set; }

        public double LeafTemp1 { get; set; }
        public double LeafTemp2 { get; set; }

        public double LeafWetness1 { get; set; }
        public double LeafWetness2 { get; set; }

        public double AirQuality1 { get; set; }
        public double AirQuality2 { get; set; }
        public double AirQuality3 { get; set; }
        public double AirQuality4 { get; set; }

        public double AirQualityAvg1 { get; set; }
        public double AirQualityAvg2 { get; set; }
        public double AirQualityAvg3 { get; set; }
        public double AirQualityAvg4 { get; set; }

        public double UserTemp1 { get; set; }
        public double UserTemp2 { get; set; }
        public double UserTemp3 { get; set; }
        public double UserTemp4 { get; set; }
        public double UserTemp5 { get; set; }
        public double UserTemp6 { get; set; }
        public double UserTemp7 { get; set; }
        public double UserTemp8 { get; set; }

        public double CO2 { get; set; }
        public double CO2_24h { get; set; }
        public double CO2_pm2p5 { get; set; }
        public double CO2_pm2p5_24h { get; set; }
        public double CO2_pm10 { get; set; }
        public double CO2_pm10_24h { get; set; }
        public double CO2_temp { get; set; }
        public double CO2_hum { get; set; }

        public bool Valid { get; set; }
    }

    internal class ExtraSensorslog : IDisposable
    {
        private readonly ExtraSensorslogType type;
        private readonly CuSupport Sup;
        private readonly bool IgnoreDataErrors;
        private readonly string[] enumFieldTypeNames;
        private readonly string[] ExtraSensorslogList;

        private bool disposed;
        private string filenameCopy;

        const int MaxErrors = 10;
        int ErrorCount;

        internal ExtraSensorslog( CuSupport s )
        {
            string line;

            Sup = s;
            Sup.LogDebugMessage( $"ExtraSensorslog constructor: Using fixed path: | data/ |; file: | *log.txt" );

            // Get the list of monthly logfile in the datadirectory and check what type of delimeters we have
            ExtraSensorslogList = Directory.GetFiles( "data/", "ExtraLog*.txt" );

            if ( ExtraSensorslogList.Length >= 0 )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( ExtraSensorslogList[ 0 ] );
                Sup.LogDebugMessage( $"ExtraSensorslog constructor: Using {filenameCopy}" );
            }
            else
                return;

            if ( File.Exists( filenameCopy ) )
                File.Delete( filenameCopy );
            File.Copy( ExtraSensorslogList[ 0 ], filenameCopy );

            // Not sure about encoding of this file, let it be handled by the system. No presumtions.
            //
            using ( StreamReader mf = new StreamReader( filenameCopy ) )
            {
                Sup.LogTraceVerboseMessage( $"ExtraSensorslog constructor: Working on: {filenameCopy}" );

                line = mf.ReadLine();

                if ( line[ 2 ] == '-' && line[ 8 ] == ';' )
                    type = ExtraSensorslogType.DashSemicolonComma;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ';' )
                    type = ExtraSensorslogType.SlashSemicolonComma;
                else if ( line[ 2 ] == '.' && line[ 8 ] == ';' )
                    type = ExtraSensorslogType.PointSemicolonComma;
                else if ( line[ 2 ] == '-' && line[ 8 ] == ',' )
                    type = ExtraSensorslogType.DashCommaPoint;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ',' )
                    type = ExtraSensorslogType.SlashCommaPoint;
                else
                {
                    Sup.LogTraceErrorMessage( "ExtraSensorslog constructor: Internal Error - Unkown format of inputfile. Please notify programmer." );
                    Environment.Exit( 0 );
                }
            }

            File.Delete( filenameCopy );

            Sup.LogTraceInfoMessage( $"ExtraSensorslog constructor: ExtraSensorslogType is {type}" );
            enumFieldTypeNames = Enum.GetNames( typeof( ExtraSensorslogFieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true" );

            if ( ExtraSensorslogList.Length >= 0 && Sup.GetUtilsIniValue( "ExtraSensors", "CleanupExtraSensorslog", "false" ).Equals( "true" ) )
            {
                // We keep two month of data, the rest can be discarded
                Sup.LogTraceInfoMessage( $"ExtraSensors constructor: Cleaning up Extra Sensors Logfiles..." );

                foreach ( string thisFile in ExtraSensorslogList )
                {
                    if ( CMXutils.RunStarted.Month - File.GetLastWriteTime( thisFile ).Month > 2 )
                    {
                        try { File.Delete( thisFile ); }
                        catch { Sup.LogTraceInfoMessage( $"ExtraSensors constructor: Can't clean up / delete {thisFile}" ); }
                    }
                }
            }

            return;
        }

        internal List<ExtraSensorslogValue> MainExtraSensorsValuesList;

        internal List<ExtraSensorslogValue> ReadExtraSensorslog()
        {
            // Get the list of values starting datetime to Now - period by user definition GraphHours in section Graphs in Cumulus.ini
            //
            Sup.LogTraceInfoMessage( $"ExtraSensorslog: start." );

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

            Sup.LogTraceInfoMessage( $"ExtraSensorslog: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            ExtraSensorslogValue tmp;
            MainExtraSensorsValuesList = new List<ExtraSensorslogValue>();

            Filename = $"data/ExtraLog{timeStart:yyyy}{timeStart:MM}.txt";
            if ( !File.Exists( Filename ) )
            {
                Sup.LogTraceInfoMessage( $"ExtraSensorslog: Require {Filename} to start but it does not exist, aborting ExtraSensorsLog" );
                return MainExtraSensorsValuesList;
            }

            Sup.LogTraceInfoMessage( $"ExtraSensorslog: Require {Filename} to start" );

            while ( !PeriodComplete )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );
                if ( File.Exists( filenameCopy ) )
                    File.Delete( filenameCopy );
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
                            MainExtraSensorsValuesList.Add( tmp );
                        if ( tmp.ThisDate >= timeEnd )
                            break; // we have our set of data required
                        line = ReadLine( af, false );
                    } while ( !string.IsNullOrEmpty( line ) );
                } // End Using the ExtraSensorslog to Read

                if ( File.Exists( filenameCopy ) )
                    File.Delete( filenameCopy );

                if ( tmp.ThisDate.Month == timeEnd.Month )
                {
                    Sup.LogTraceInfoMessage( $"ExtraSensorslog: Finished reading the log at {tmp.ThisDate}" );
                    PeriodComplete = true;
                }
                else
                {
                    Filename = $"data/ExtraLog{timeEnd:yyyy}{timeEnd:MM}.txt";  // Take care of a period passing month boundary
                    Sup.LogTraceInfoMessage( $"ExtraSensorslog: Require the  next logfile: {Filename}" );
                }
            }

            Sup.LogTraceInfoMessage( $"ExtraSensorslog: MainExtraSensorsValuesList created: {MainExtraSensorsValuesList.Count} records." );
            watch.Stop();
            Sup.LogTraceInfoMessage( $"ExtraSensorslog: Timing of ExtraSensors logfile read = {watch.ElapsedMilliseconds} ms" );
            Sup.LogTraceInfoMessage( $"ExtraSensorslog: End" );

            return MainExtraSensorsValuesList;
        } // End ExtraSensorsLogs

        private string ReadLine( StreamReader af, bool SanityCheck )
        {
            StringBuilder tmpLine = new StringBuilder();

            if ( af.EndOfStream )
            {
                Sup.LogTraceInfoMessage( "ExtraSensorslog : EOF detected" ); // nothing to do;
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
                        case ExtraSensorslogType.DashSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ';' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case ExtraSensorslogType.PointSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '.' ) && ( tmpLine[ 8 ] == ';' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case ExtraSensorslogType.SlashSemicolonComma:
                            if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ';' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case ExtraSensorslogType.DashCommaPoint:
                            if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ',' ) ) )
                                SeparatorInconsistencyFound = true;
                            break;

                        case ExtraSensorslogType.SlashCommaPoint:
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
                    Sup.LogTraceErrorMessage( $"ExtraSensorslog: Illegal part of the code, should never be here, FATAL ERROR!" );
                    Sup.LogTraceErrorMessage( $"ExtraSensorslog: Separator Inconsistency in {filenameCopy.Remove( 0, 5 )} found, FATAL ERROR!" );
                    Sup.LogTraceErrorMessage( $"ExtraSensorslog: Please check {filenameCopy.Remove( 0, 5 )} and the data directory." );
                }
                else
                {
                    /*
                     * make a uniform line to read: convert all to SlashCommaPoint
                     */
                    if ( type == ExtraSensorslogType.DashSemicolonComma )
                    {
                        tmpLine[ 2 ] = '/';
                        tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == ExtraSensorslogType.PointSemicolonComma )
                    {
                        tmpLine[ 2 ] = '/';
                        tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == ExtraSensorslogType.SlashSemicolonComma )
                    {
                        //tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                        tmpLine.Replace( ',', '.' );
                        tmpLine.Replace( ';', ',' );
                    }
                    else if ( type == ExtraSensorslogType.DashCommaPoint )
                    {
                        tmpLine[ 2 ] = '/';
                        tmpLine[ 5 ] = '/';
                    }
                }// NO Separator Inconsistency
            } // Not EOF

            return tmpLine.ToString();
        }

        private ExtraSensorslogValue SetValues( string line, DateTime StartTime )
        {
            string tmpDatestring;
            string[] lineSplit = line.Split( ',' );

            ExtraSensorslogValue ThisValue = new ExtraSensorslogValue();

            int FieldInUse = (int) ExtraSensorslogFieldName.thisDate;
            CultureInfo provider = CultureInfo.InvariantCulture;

            try
            {
                // DateTime
                tmpDatestring = lineSplit[ FieldInUse ];
                FieldInUse = (int) ExtraSensorslogFieldName.thisTime;
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
                    FieldInUse = (int) ExtraSensorslogFieldName.Temp1;
                    ThisValue.Temp1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp2;
                    ThisValue.Temp2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp3;
                    ThisValue.Temp3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp4;
                    ThisValue.Temp4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp5;
                    ThisValue.Temp5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp6;
                    ThisValue.Temp6 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp7;
                    ThisValue.Temp7 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp8;
                    ThisValue.Temp8 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp9;
                    ThisValue.Temp9 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Temp10;
                    ThisValue.Temp10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity1;
                    ThisValue.Humidity1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity2;
                    ThisValue.Humidity2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity3;
                    ThisValue.Humidity3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity4;
                    ThisValue.Humidity4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity5;
                    ThisValue.Humidity5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity6;
                    ThisValue.Humidity6 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity7;
                    ThisValue.Humidity7 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity8;
                    ThisValue.Humidity8 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity9;
                    ThisValue.Humidity9 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.Humidity10;
                    ThisValue.Humidity10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint1;
                    ThisValue.Dewpoint1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint2;
                    ThisValue.Dewpoint2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint3;
                    ThisValue.Dewpoint3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint4;
                    ThisValue.Dewpoint4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint5;
                    ThisValue.Dewpoint5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint6;
                    ThisValue.Dewpoint6 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint7;
                    ThisValue.Dewpoint7 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint8;
                    ThisValue.Dewpoint8 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint9;
                    ThisValue.Dewpoint9 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.DewPoint10;
                    ThisValue.Dewpoint10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp1;
                    ThisValue.SoilTemp1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp2;
                    ThisValue.SoilTemp2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp3;
                    ThisValue.SoilTemp3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp4;
                    ThisValue.SoilTemp4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp5;
                    ThisValue.SoilTemp5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp6;
                    ThisValue.SoilTemp6 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp7;
                    ThisValue.SoilTemp7 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp8;
                    ThisValue.SoilTemp8 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp9;
                    ThisValue.SoilTemp9 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp10;
                    ThisValue.SoilTemp10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp11;
                    ThisValue.SoilTemp11 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp12;
                    ThisValue.SoilTemp12 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp13;
                    ThisValue.SoilTemp13 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp14;
                    ThisValue.SoilTemp14 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp15;
                    ThisValue.SoilTemp15 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilTemp16;
                    ThisValue.SoilTemp16 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture1;
                    ThisValue.SoilMoisture1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture2;
                    ThisValue.SoilMoisture2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture3;
                    ThisValue.SoilMoisture3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture4;
                    ThisValue.SoilMoisture4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture5;
                    ThisValue.SoilMoisture5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture6;
                    ThisValue.SoilMoisture6 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture7;
                    ThisValue.SoilMoisture7 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture8;
                    ThisValue.SoilMoisture8 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture9;
                    ThisValue.SoilMoisture9 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture10;
                    ThisValue.SoilMoisture10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture11;
                    ThisValue.SoilMoisture11 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture12;
                    ThisValue.SoilMoisture12 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture13;
                    ThisValue.SoilMoisture13 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture14;
                    ThisValue.SoilMoisture14 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture15;
                    ThisValue.SoilMoisture15 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.SoilMoisture16;
                    ThisValue.SoilMoisture16 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.LeafTemp1;
                    ThisValue.LeafTemp1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.LeafTemp2;
                    ThisValue.LeafTemp2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.LeafWetness1;
                    ThisValue.LeafWetness1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.LeafWetness2;
                    ThisValue.LeafWetness2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    //LeafTemp1,LeafTemp2,LeafWetness1,LeafWetness2,  dubious: 4 of each webtags exist. Are those not beimng logged or is the header wrong?

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQuality1;
                    ThisValue.AirQuality1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQuality2;
                    ThisValue.AirQuality2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQuality3;
                    ThisValue.AirQuality3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQuality4;
                    ThisValue.AirQuality4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQualityAvg1;
                    ThisValue.AirQualityAvg1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQualityAvg2;
                    ThisValue.AirQualityAvg2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQualityAvg3;
                    ThisValue.AirQualityAvg3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.AirQualityAvg4;
                    ThisValue.AirQualityAvg4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp1;
                    ThisValue.UserTemp1 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp2;
                    ThisValue.UserTemp2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp3;
                    ThisValue.UserTemp3 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp4;
                    ThisValue.UserTemp4 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp5;
                    ThisValue.UserTemp5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp6;
                    ThisValue.UserTemp6 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp7;
                    ThisValue.UserTemp7 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.UserTemp8;
                    ThisValue.UserTemp8 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2;
                    ThisValue.CO2 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2Avg;
                    ThisValue.CO2_24h = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2_pm2_5;
                    ThisValue.CO2_pm2p5 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2_pm2_5_avg;
                    ThisValue.CO2_pm2p5_24h = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2_pm10;
                    ThisValue.CO2_pm10 = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2_PM10_avg;
                    ThisValue.CO2_pm10_24h = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2_Temp;
                    ThisValue.CO2_temp = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    FieldInUse = (int) ExtraSensorslogFieldName.CO2_Hum;
                    ThisValue.CO2_hum = Convert.ToDouble( lineSplit[ FieldInUse ], provider );

                    Sup.LogTraceVerboseMessage( $"ExtraSensorslog: SetValues after adding the values: Original Line {line}" );
                }
            } // try
            catch ( Exception e ) when ( e is FormatException || e is OverflowException )
            {
                const string m = "ExtraSensorslog.SetValues";

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
                    if ( ErrorCount < MaxErrors )
                        Sup.LogTraceErrorMessage( "ExtraSensorslogValue.SetValues : Continuing to read data" );
                    else
                        // Environment.Exit(0);
                        throw;
            }
            catch ( IndexOutOfRangeException e )
            {
                const string m = "ExtraSensorslogValue.SetValues";

                ErrorCount++;

                if ( ErrorCount < MaxErrors )
                {
                    Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} does  not exist in this file {filenameCopy}" );
                    Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );
                }

                if ( IgnoreDataErrors )
                    if ( ErrorCount < MaxErrors )
                        Sup.LogTraceErrorMessage( "ExtraSensorslogValue.SetValues : Continuing to read data" );
                    else
                        throw;
            }

            return ThisValue;
        }

        ~ExtraSensorslog()
        {
            Sup.LogTraceVerboseMessage( "ExtraSensorslog destructor: Closing file and ending program" );
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