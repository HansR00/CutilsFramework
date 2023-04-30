/*
 * ExtraSensorslog - Part of CumulusUtils
 *
 * © Copyright 2019-2023 Hans Rottier <hans.rottier@gmail.com>
 *
 * The code of CumulusUtils is public domain and distributed under the  
 * Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI .NET Framework 4.8
 *              Initial release: pwsFWI                 (version 1.0)
 *                               Website Generator      (version 3.0)
 *                               ChartsCompiler         (version 5.0)
 *                               Maintenance releases   (version 6.x)
 *              Startdate : 16 november 2021 start of conversion to .NET 5, 6 and 7
 *              
 * Environment: Raspberry Pi 3B+ and up
 *              Raspberry Pi OS  for testruns
 *              C# / Visual Studio / Windows for development
 *              
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CumulusUtils
{
    public struct ExternalExtraSensorslogValue
    {
        public DateTime ThisDate { get; set; }

        public double Value { get; set; }
    }

    public class ExternalExtraSensorslog : IDisposable
    {
        private readonly CuSupport Sup;
        private readonly bool IgnoreDataErrors;
        private readonly string ThisSensorName;
        private readonly string[] ExternalExtraSensorslogList;

        private bool disposed;
        private string filenameCopy;

        const int MaxErrors = 10;
        int ErrorCount = 0;

        public ExternalExtraSensorslog( CuSupport s, string SensorName )
        {
            Sup = s;
            Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog constructor: Using fixed path: | data/ |; file: | *log.txt" );

            ThisSensorName = SensorName;
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true", CUtils.Cmp );

            // Get the list of monthly logfile in the datadirectory and check what type of delimeters we have
            ExternalExtraSensorslogList = Directory.GetFiles( "data/", $"{SensorName}*.txt" );

            if ( ExternalExtraSensorslogList.Length >= 0 && Sup.GetUtilsIniValue( "ExtraSensors", "CleanupExtraSensorslog", "false" ).Equals( "true", CUtils.Cmp ) )
            {
                // We keep two month of data, the rest can be discarded
                Sup.LogTraceInfoMessage( $"ExternalExtraSensors constructor: Cleaning up Extra Sensors Logfiles..." );

                foreach ( string thisFile in ExternalExtraSensorslogList )
                {
                    if ( CUtils.RunStarted.Month - File.GetLastWriteTime( thisFile ).Month > 2 )
                    {
                        try { File.Delete( thisFile ); }
                        catch { Sup.LogTraceInfoMessage( $"ExternalExtraSensors constructor: Can't clean up / delete {thisFile}" ); }
                    }
                }
            }

            return;
        }

        public List<ExternalExtraSensorslogValue> ExternalExtraSensorsValuesList;

        public List<ExternalExtraSensorslogValue> ReadExternalExtraSensorslog()
        {
            bool NextFileTried = false;
            bool PeriodComplete = false;

            // Get the list of values starting datetime to Now - period by user definition GraphHours in section Graphs in Cumulus.ini
            //
            Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: start." );

            string Filename;

            DateTime Now = DateTime.Now;
            Now = new DateTime( Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, 0 );
            DateTime timeEnd = Now.AddMinutes( -Now.Minute % Math.Max( CUtils.FTPIntervalInMinutes, CUtils.LogIntervalInMinutes ) );
            DateTime timeStart;

            if ( CUtils.Isup.IsIncrementalAllowed() )
            {
                try
                {
                    timeStart = DateTime.ParseExact( Sup.GetUtilsIniValue( "General", "LastUploadTime", "" ), "dd/MM/yy HH:mm", CUtils.Inv ).AddMinutes( 1 );
                }
                catch
                {
                    timeStart = timeEnd.AddHours( -CUtils.HoursInGraph );
                }

            }
            else
            {
                timeStart = timeEnd.AddHours( -CUtils.HoursInGraph );
            }

            Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            ExternalExtraSensorslogValue tmp;
            ExternalExtraSensorsValuesList = new List<ExternalExtraSensorslogValue>();

            Filename = $"data/{ThisSensorName}{timeStart:yyyy}{timeStart:MM}.txt";
            if ( !File.Exists( Filename ) )
            {
                Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Require {Filename} to start but it does not exist, aborting ExternalExtraSensorsLog" );
                return ExternalExtraSensorsValuesList;
            }

            Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Require {Filename} to start" );

            while ( !PeriodComplete )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                File.Copy( Filename, filenameCopy );

                string[] allLines = File.ReadAllLines( filenameCopy ); //got to use this sometime

                tmp = new ExternalExtraSensorslogValue();

                foreach ( string line in allLines )
                {
                    string[] splitLine = line.Split( ',' );

                    try
                    {
                        //Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Try block Parsing: {splitLine[0]} {splitLine[1]}" );
                        tmp.ThisDate = DateTime.ParseExact( splitLine[ 0 ], "dd/MM/yy HH:mm", CUtils.Inv );

                        //Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Try block Parsing after parse {tmp.ThisDate} {timeStart} {timeEnd}" );
                        if ( tmp.ThisDate < timeStart ) continue;
                        if ( tmp.ThisDate >= timeEnd ) break; // we have our set of data required

                        //Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Try block Converting" );
                        tmp.Value = Convert.ToSingle( splitLine[ 1 ] );

                        //Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Try block Add: {tmp.ThisDate}" );
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
                                Sup.LogTraceErrorMessage( $"{m} : Continuing to read data" );
                            else
                                // Environment.Exit(0);
                                throw;
                    }
                }

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                if ( ExternalExtraSensorsValuesList.Last().ThisDate >= timeEnd || NextFileTried )
                {
                    Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Finished reading the log at {ExternalExtraSensorsValuesList.Last().ThisDate}" );
                    PeriodComplete = true;
                }
                else
                {
                    NextFileTried = true;

                    Filename = $"data/{ThisSensorName}{timeEnd:yyyy}{timeEnd:MM}.txt";  // Take care of a period passing month boundary
                    Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: Require the  next logfile: {Filename}" );

                    if ( !File.Exists( Filename ) )
                    {
                        Sup.LogTraceErrorMessage( $"ExternalExtraSensorslog: Require {Filename} to continue but it does not exist, aborting ExternalExtraSensorsLog" );
                        PeriodComplete = true;
                    }
                }
            }

            Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: MainExtraSensorsValuesList created: {ExternalExtraSensorsValuesList.Count} records." );
            Sup.LogTraceInfoMessage( $"ExternalExtraSensorslog: End" );

            return ExternalExtraSensorsValuesList;
        } // End ExtraSensorsLogs


        ~ExternalExtraSensorslog()
        {
            Sup.LogTraceVerboseMessage( "ExternalExtraSensorslog destructor: Closing file and ending program" );
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