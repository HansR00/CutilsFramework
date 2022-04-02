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
using System.Linq;

namespace CumulusUtils
{
    internal struct ExternalExtraSensorslogValue
    {
        public DateTime ThisDate { get; set; }

        public double Value { get; set; }
    }

    internal class ExternalExtraSensorslog : IDisposable
    {
        private readonly CuSupport Sup;
        private readonly bool IgnoreDataErrors;
        private readonly string ThisSensorName;
        private readonly string[] ExternalExtraSensorslogList;

        private bool disposed;
        private string filenameCopy;

        const int MaxErrors = 10;
        int ErrorCount = 0;

        internal ExternalExtraSensorslog( CuSupport s, string SensorName )
        {
            Sup = s;
            Sup.LogDebugMessage( $"ExternalExtraSensorslog constructor: Using fixed path: | data/ |; file: | *log.txt" );

            ThisSensorName = SensorName;
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true" );

            // Get the list of monthly logfile in the datadirectory and check what type of delimeters we have
            ExternalExtraSensorslogList = Directory.GetFiles( "data/", $"{SensorName}*.txt" );

            if ( ExternalExtraSensorslogList.Length >= 0 && Sup.GetUtilsIniValue( "ExtraSensors", "CleanupExtraSensorslog", "false" ).Equals( "true" ) )
            {
                // We keep two month of data, the rest can be discarded
                Sup.LogTraceInfoMessage( $"ExternalExtraSensors constructor: Cleaning up Extra Sensors Logfiles..." );

                foreach ( string thisFile in ExternalExtraSensorslogList )
                {
                    if ( CMXutils.RunStarted.Month - File.GetLastWriteTime( thisFile ).Month > 2 )
                    {
                        try { File.Delete( thisFile ); }
                        catch { Sup.LogTraceInfoMessage( $"ExternalExtraSensors constructor: Can't clean up / delete {thisFile}" ); }
                    }
                }
            }

            return;
        }

        internal List<ExternalExtraSensorslogValue> ExternalExtraSensorsValuesList;

        internal List<ExternalExtraSensorslogValue> ReadExternalExtraSensorslog()
        {
            CultureInfo Inv = CultureInfo.InvariantCulture;
            bool PeriodComplete = false;

            // Get the list of values starting datetime to Now - period by user definition GraphHours in section Graphs in Cumulus.ini
            //
            Sup.LogDebugMessage( $"ExternalExtraSensorslog: start." );

            Stopwatch watch = Stopwatch.StartNew();

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

            Sup.LogDebugMessage( $"ExternalExtraSensorslog: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            ExternalExtraSensorslogValue tmp;
            ExternalExtraSensorsValuesList = new List<ExternalExtraSensorslogValue>();

            Filename = $"data/{ThisSensorName}{timeStart:yyyy}{timeStart:MM}.txt";
            if ( !File.Exists( Filename ) )
            {
                Sup.LogDebugMessage( $"ExternalExtraSensorslog: Require {Filename} to start but it does not exist, aborting ExternalExtraSensorsLog" );
                return ExternalExtraSensorsValuesList;
            }

            Sup.LogDebugMessage( $"ExternalExtraSensorslog: Require {Filename} to start" );

            while ( !PeriodComplete )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                File.Copy( Filename, filenameCopy );

                string[] allLines = File.ReadAllLines( filenameCopy ); //got to use this sometime

                foreach ( string line in allLines )
                {
                    tmp = new ExternalExtraSensorslogValue();

                    string[] splitLine = line.Split( ',' );

                    try
                    {
                        tmp.ThisDate = DateTime.ParseExact( splitLine[ 0 ], "dd/MM/yy HH:mm", Inv );
                        if ( tmp.ThisDate < timeStart ) continue;

                        tmp.Value = Convert.ToSingle( splitLine[ 1 ] );

                        ExternalExtraSensorsValuesList.Add( tmp );
                    }
                    catch ( Exception e ) when ( e is FormatException || e is OverflowException )
                    {
                        const string m = "ExternalExtraSensorslog.SetValues";

                        ErrorCount++;

                        //handle exception
                        if ( ErrorCount < MaxErrors )
                        {
                            Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                            Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );
                        }

                        if ( IgnoreDataErrors )
                            if ( ErrorCount < MaxErrors )
                                Sup.LogTraceErrorMessage( "ExtraSensorslogValue.SetValues : Continuing to read data" );
                            else
                                // Environment.Exit(0);
                                throw;
                    }
                }

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );


                if ( ExternalExtraSensorsValuesList.Last().ThisDate.Month == timeEnd.Month )
                {
                    Sup.LogDebugMessage( $"ExtraSensorslog: Finished reading the log at {ExternalExtraSensorsValuesList.Last().ThisDate}" );
                    PeriodComplete = true;
                }
                else
                {
                    Filename = $"data/{ThisSensorName}{timeEnd:yyyy}{timeEnd:MM}.txt";  // Take care of a period passing month boundary
                    Sup.LogDebugMessage( $"ExtraSensorslog: Require the  next logfile: {Filename}" );
                }
            }

            Sup.LogTraceInfoMessage( $"ExtraSensorslog: MainExtraSensorsValuesList created: {ExternalExtraSensorsValuesList.Count} records." );
            watch.Stop();

            Sup.LogTraceInfoMessage( $"ExtraSensorslog: Timing of ExtraSensors logfile read = {watch.ElapsedMilliseconds} ms" );
            Sup.LogTraceInfoMessage( $"ExtraSensorslog: End" );

            return ExternalExtraSensorsValuesList;
        } // End ExtraSensorsLogs


        ~ExternalExtraSensorslog()
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