/*
 * Airlinklog - Part of CumulusUtils
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using FluentFTP.Helpers;

namespace CumulusUtils
{
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
        public double? In_temp { get; set; }
        public int? In_hum { get; set; }

        // In
        public double? In_pm1 { get; set; }
        public double? In_pm2p5 { get; set; }
        public double? In_pm2p5_1hr { get; set; }
        public double? In_pm2p5_3hr { get; set; }
        public double? In_pm2p5_24hr { get; set; }
        public double? In_pm2p5_nowcast { get; set; }

        public double? In_pm10 { get; set; }
        public double? In_pm10_1hr { get; set; }
        public double? In_pm10_3hr { get; set; }
        public double? In_pm10_24hr { get; set; }
        public double? In_pm10_nowcast { get; set; }

        public int? In_pct_1hr { get; set; }
        public int? In_pct_3hr { get; set; }
        public int? In_pct_24hr { get; set; }
        public int? In_pct_nowcast { get; set; }

        public double? In_AQIpm2p5 { get; set; }
        public double? In_AQIpm2p5_1hr { get; set; }
        public double? In_AQIpm2p5_3hr { get; set; }
        public double? In_AQIpm2p5_24hr { get; set; }
        public double? In_AQIpm2p5_nowcast { get; set; }
        public double? In_AQIPm10 { get; set; }
        public double? In_AQIPm10_1hr { get; set; }
        public double? In_AQIPm10_3hr { get; set; }
        public double? In_AQIPm10_24hr { get; set; }
        public double? In_AQIPm10_nowcast { get; set; }

        // Out
        public double? Out_temp { get; set; }
        public int? Out_hum { get; set; }
        public double? Out_pm1 { get; set; }
        public double? Out_pm2p5 { get; set; }
        public double? Out_pm2p5_1hr { get; set; }
        public double? Out_pm2p5_3hr { get; set; }
        public double? Out_pm2p5_24hr { get; set; }
        public double? Out_pm2p5_nowcast { get; set; }
        public double? Out_pm10 { get; set; }
        public double? Out_pm10_1hr { get; set; }
        public double? Out_pm10_3hr { get; set; }
        public double? Out_pm10_24hr { get; set; }
        public double? Out_pm10_nowcast { get; set; }

        public int? Out_pct_1hr { get; set; }
        public int? Out_pct_3hr { get; set; }
        public int? Out_pct_24hr { get; set; }
        public int? Out_pct_nowcast { get; set; }

        public double? Out_AQIpm2p5 { get; set; }
        public double? Out_AQIpm2p5_1hr { get; set; }
        public double? Out_AQIpm2p5_3hr { get; set; }
        public double? Out_AQIpm2p5_24hr { get; set; }
        public double? Out_AQIpm2p5_nowcast { get; set; }
        public double? Out_AQIPm10 { get; set; }
        public double? Out_AQIPm10_1hr { get; set; }
        public double? Out_AQIPm10_3hr { get; set; }
        public double? Out_AQIPm10_24hr { get; set; }
        public double? Out_AQIPm10_nowcast { get; set; }

        public bool Valid { get; set; }
    }

    public class Airlinklog : IDisposable
    {
        //private readonly AirlinklogType type;
        private readonly CuSupport Sup;
        private readonly bool IgnoreDataErrors;
        private readonly string[] enumFieldTypeNames;
        private readonly string[] AirlinklogList;

        string[] lines;

        private bool disposed;
        private string filenameCopy;

        const int MaxErrors = 10;
        int ErrorCount;

        public Airlinklog( CuSupport s )
        {
            Sup = s;
            Sup.LogTraceInfoMessage( $"Airlinklog constructor: Using fixed path: | data/ |; file: | *log.txt" );

            // Get the list of Airlink logfile in the datadirectory and check what type of delimeters we have
            AirlinklogList = Directory.GetFiles( "data/", "AirLink*.txt" );

            if ( AirlinklogList.Length >= 0 )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( AirlinklogList[ 0 ] );
                Sup.LogTraceInfoMessage( $"Airlinklog constructor: Using {filenameCopy}" );
            }
            else
                return;


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
            Sup.LogDebugMessage( $"ReadAirlinklog: starting." );

            bool NextFileTried = false;
            bool PeriodComplete = false;

            string Filename;

            Sup.SetStartAndEndForData( out DateTime timeStart, out DateTime timeEnd );
            Sup.LogTraceInfoMessage( $"AirLinklog: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            AirlinklogValue tmp = new AirlinklogValue();
            MainAirLinkList = new List<AirlinklogValue>();

            Filename = $"data/AirLink{timeStart:yyyy}{timeStart:MM}log.txt";
            if ( !File.Exists( Filename ) )
            {
                Sup.LogTraceInfoMessage( $"AirLinklog: Require {Filename} to start but it does not exist, aborting AirLinkLog" );
                return MainAirLinkList;
            }

            Sup.LogTraceInfoMessage( $"AirLinklog: Require {Filename} to start" );

            while ( !PeriodComplete )
            {
                filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );
                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                File.Copy( Filename, filenameCopy );

                lines = File.ReadAllLines( filenameCopy );
                File.Delete( filenameCopy );

                foreach ( string line in lines )
                {
                    tmp = SetValues( line, timeStart );

                    // valid is a consequence of errors in the datafile while the user expressed the wish to continue
                    // through the ini parameter 'IgnoreDataErrors=true'
                    if ( tmp.Valid ) MainAirLinkList.Add( tmp );
                }

                if ( tmp.ThisDate >= timeEnd || NextFileTried )
                {
                    Sup.LogDebugMessage( $"AirLinklog: Finished reading the log at {tmp.ThisDate}" );
                    PeriodComplete = true;
                }
                else
                {
                    NextFileTried = true;
                    Filename = $"data/AirLink{timeEnd:yyyy}{timeEnd:MM}log.txt";  // Take care of a period passing month boundary
                    Sup.LogTraceInfoMessage( $"AirLinklog: Require the  next logfile: {Filename}" );

                    if ( !File.Exists( Filename ) )
                    {
                        Sup.LogTraceInfoMessage( $"AirLinklog: {Filename} Does not exist so we need to stop reading" );
                        PeriodComplete = true;
                    }
                }
            } // Loop over all files in AirlinkfileList

            Sup.LogTraceInfoMessage( $"ReadAirlinklog: MainMonthList created: {MainAirLinkList.Count} records." );
            Sup.LogTraceInfoMessage( $"ReadAirlinklog: End" );

            return MainAirLinkList;
        } // End ReadAirlinklog

        private AirlinklogValue SetValues( string line, DateTime StartTime )
        {
            string tmpDatestring, tmpTimestring;
            int FieldInUse = 0;

            string[] lineSplit = line.Split( GlobConst.CommaSeparator );

            AirlinklogValue ThisValue = new AirlinklogValue();


            try
            {
                // DateTime
                FieldInUse = (int) AirlinklogFieldName.thisDate;
                tmpDatestring = lineSplit[ FieldInUse ];

                FieldInUse = (int) AirlinklogFieldName.thisTime;
                tmpTimestring = lineSplit[ FieldInUse ];

                //ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy HH:mm", CUtils.Inv );
                ThisValue.ThisDate = CuSupport.UnixTimestampToDateTime( tmpTimestring );

                if ( ThisValue.ThisDate < StartTime )
                {
                    // Not within date range, try next line, possibly next  file
                    ThisValue.Valid = false;
                }
                else // Within date range
                {
                    // Inside sensor
                    FieldInUse = (int) AirlinklogFieldName.In_temp;
                    ThisValue.In_temp = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_hum;
                    ThisValue.In_hum = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm1;
                    ThisValue.In_pm1 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5;
                    ThisValue.In_pm2p5 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_1hr;
                    ThisValue.In_pm2p5_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_3hr;
                    ThisValue.In_pm2p5_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_24hr;
                    ThisValue.In_pm2p5_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm2p5_nowcast;
                    ThisValue.In_pm2p5_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm10;
                    ThisValue.In_pm10 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_1hr;
                    ThisValue.In_pm10_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_3hr;
                    ThisValue.In_pm10_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_24hr;
                    ThisValue.In_pm10_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_nowcast;
                    ThisValue.In_pm10_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pct_1hr;
                    ThisValue.In_pct_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pct_3hr;
                    ThisValue.In_pct_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pct_24hr;
                    ThisValue.In_pct_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pct_nowcast;
                    ThisValue.In_pct_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5;
                    ThisValue.In_AQIpm2p5 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_1hr;
                    ThisValue.In_AQIpm2p5_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_3hr;
                    ThisValue.In_AQIpm2p5_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_pm10_24hr;
                    ThisValue.In_AQIpm2p5_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIpm2p5_nowcast;
                    ThisValue.In_AQIpm2p5_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10;
                    ThisValue.In_AQIPm10 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_1hr;
                    ThisValue.In_AQIPm10_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_3hr;
                    ThisValue.In_AQIPm10_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_24hr;
                    ThisValue.In_AQIPm10_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.In_AQIPm10_nowcast;
                    ThisValue.In_AQIPm10_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    // Now the Out values
                    FieldInUse = (int) AirlinklogFieldName.Out_temp;
                    ThisValue.Out_temp = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_hum;
                    ThisValue.Out_hum = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm1;
                    ThisValue.Out_pm1 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5;
                    ThisValue.Out_pm2p5 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_1hr;
                    ThisValue.Out_pm2p5_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_3hr;
                    ThisValue.Out_pm2p5_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_24hr;
                    ThisValue.Out_pm2p5_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm2p5_nowcast;
                    ThisValue.Out_pm2p5_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10;
                    ThisValue.Out_pm10 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_1hr;
                    ThisValue.Out_pm10_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_3hr;
                    ThisValue.Out_pm10_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_24hr;
                    ThisValue.Out_pm10_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_nowcast;
                    ThisValue.Out_pm10_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_1hr;
                    ThisValue.Out_pct_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_3hr;
                    ThisValue.Out_pct_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_24hr;
                    ThisValue.Out_pct_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pct_nowcast;
                    ThisValue.Out_pct_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5;
                    ThisValue.Out_AQIpm2p5 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_1hr;
                    ThisValue.Out_AQIpm2p5_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_3hr;
                    ThisValue.Out_AQIpm2p5_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_pm10_24hr;
                    ThisValue.Out_AQIpm2p5_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIpm2p5_nowcast;
                    ThisValue.Out_AQIpm2p5_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10;
                    ThisValue.Out_AQIPm10 = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_1hr;
                    ThisValue.Out_AQIPm10_1hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_3hr;
                    ThisValue.Out_AQIPm10_3hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_24hr;
                    ThisValue.Out_AQIPm10_24hr = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    FieldInUse = (int) AirlinklogFieldName.Out_AQIPm10_nowcast;
                    ThisValue.Out_AQIPm10_nowcast = !string.IsNullOrEmpty( lineSplit[ FieldInUse ] ) ? Convert.ToDouble( lineSplit[ FieldInUse ], CUtils.Inv ) : null;

                    ThisValue.Valid = true;

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
                {
                    if ( ErrorCount < MaxErrors )
                        Sup.LogTraceErrorMessage( "AirlinklogValue.SetValues : Continuing to read data" );
                }
                else
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
                {
                    if ( ErrorCount < MaxErrors )
                        Sup.LogTraceErrorMessage( "AirlinklogValue.SetValues : Continuing to read data" );
                }
                else
                    throw;
            }

            return ThisValue;
        }

        ~Airlinklog()
        {
            Sup.LogTraceInfoMessage( "Airlinklog destructor: Closing file and ending program" );
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