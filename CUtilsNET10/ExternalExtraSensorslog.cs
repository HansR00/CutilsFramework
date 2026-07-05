/*
 * ExtraSensorslog - Part of CumulusUtils
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Sup.LogMessage( $"ExternalExtraSensorslog constructor: Using fixed path: | data/ |; file: | *log.txt", TraceLevel.Info );

            ThisSensorName = SensorName;
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true", CUtils.Cmp );

            // Get the list of monthly logfile in the datadirectory and check what type of delimeters we have
            ExternalExtraSensorslogList = Directory.GetFiles( "data/", $"{SensorName}*.txt" );

            if ( ExternalExtraSensorslogList.Length >= 0 && Sup.GetUtilsIniValue( "ExtraSensors", "CleanupExtraSensorslog", "false" ).Equals( "true", CUtils.Cmp ) )
            {
                // We keep two month of data, the rest can be discarded
                Sup.LogMessage( $"ExternalExtraSensors constructor: Cleaning up Extra Sensors Logfiles...", TraceLevel.Info );

                foreach ( string thisFile in ExternalExtraSensorslogList )
                {
                    if ( CUtils.RunStarted.Month - File.GetLastWriteTime( thisFile ).Month > 2 )
                    {
                        try { File.Delete( thisFile ); }
                        catch { Sup.LogMessage( $"ExternalExtraSensors constructor: Can't clean up / delete {thisFile}", TraceLevel.Info ); }
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
            Sup.LogMessage( $"ExternalExtraSensorslog: starting", TraceLevel.Info );

            string Filename;

            Sup.SetStartAndEndForData( out DateTime timeStart, out DateTime timeEnd );

            Sup.LogMessage( $"ExternalExtraSensorslog: timeStart = {timeStart}; timeEnd = {timeEnd}", TraceLevel.Info );

            ExternalExtraSensorslogValue tmp;
            ExternalExtraSensorsValuesList = new List<ExternalExtraSensorslogValue>();

            Filename = $"data/{ThisSensorName}{timeStart:yyyy}{timeStart:MM}.txt";
            if ( !File.Exists( Filename ) )
            {
                Sup.LogMessage( $"ExternalExtraSensorslog: Require {Filename} to start but it does not exist, aborting ExternalExtraSensorsLog", TraceLevel.Info );
                return ExternalExtraSensorsValuesList;
            }

            Sup.LogMessage( $"ExternalExtraSensorslog: Require {Filename} to start", TraceLevel.Info );

            while ( !PeriodComplete )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                File.Copy( Filename, filenameCopy );

                string[] allLines = File.ReadAllLines( filenameCopy );

                tmp = new ExternalExtraSensorslogValue();

                foreach ( string line in allLines )
                {
                    string[] splitLine = line.Split( GlobConst.CommaSeparator );

                    try
                    {
                        string tmpDatestring;

                        tmpDatestring = splitLine[ 0 ]; // Date
                        tmpDatestring += " " + splitLine[ 1 ]; // Time

                        tmp.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy HH:mm", CUtils.Inv );

                        if ( tmp.ThisDate < timeStart ) continue;
                        if ( tmp.ThisDate > timeEnd ) break; // we have our set of data required

                        // NOTE: formally this can be a series of values for all extra sensors in this definition
                        //       requires additional coding ToDo when required
                        tmp.Value = Convert.ToSingle( splitLine[ 2 ], CUtils.Inv );
                        ExternalExtraSensorsValuesList.Add( tmp );
                    }
                    catch ( Exception e ) when ( e is FormatException || e is OverflowException )
                    {
                        const string m = "ExternalExtraSensorslog.SetValues";

                        ErrorCount++;

                        //handle exception
                        if ( ErrorCount < MaxErrors )
                        {
                            Sup.LogMessage( $"{m} fail: {e.Message}", TraceLevel.Error );
                            Sup.LogMessage( $"{m}: line is: {line}", TraceLevel.Error );
                        }

                        if ( IgnoreDataErrors )
                        {
                            if ( ErrorCount < MaxErrors )
                                Sup.LogMessage( $"{m} : Continuing to read data", TraceLevel.Info );
                        }
                        else throw;
                    }
                }

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                if ( ExternalExtraSensorsValuesList.Last().ThisDate >= timeEnd || NextFileTried )
                {
                    Sup.LogMessage( $"ExternalExtraSensorslog: Finished reading the log at {ExternalExtraSensorsValuesList.Last().ThisDate}", TraceLevel.Info );
                    PeriodComplete = true;
                }
                else
                {
                    NextFileTried = true;

                    Filename = $"data/{ThisSensorName}{timeEnd:yyyy}{timeEnd:MM}.txt";  // Take care of a period passing month boundary
                    Sup.LogMessage( $"ExternalExtraSensorslog: Require the  next logfile: {Filename}", TraceLevel.Info );

                    if ( !File.Exists( Filename ) )
                    {
                        Sup.LogMessage( $"ExternalExtraSensorslog: Require {Filename} to continue but it does not exist, aborting ExternalExtraSensorsLog", TraceLevel.Error );
                        PeriodComplete = true;
                    }
                }
            }

            Sup.LogMessage( $"ExternalExtraSensorslog: MainExtraSensorsValuesList created: {ExternalExtraSensorsValuesList.Count} records.", TraceLevel.Info );
            Sup.LogMessage( $"ExternalExtraSensorslog: End", TraceLevel.Info );

            return ExternalExtraSensorsValuesList;
        } // End ExtraSensorsLogs


        ~ExternalExtraSensorslog()
        {
            Sup.LogMessage( "ExternalExtraSensorslog destructor: Closing file and ending program", TraceLevel.Info );
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