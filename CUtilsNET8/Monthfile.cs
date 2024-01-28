/*
 * Monthfile - Part of CumulusUtils
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
 * 
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CumulusUtils
{
    public enum MonthfileFieldName
    {
        thisDate, thisTime, CurrTemp, CurrRH, CurrDewpoint, CMXAverageWind, CMXGustSpeed, AvWindBearing, CurrRainRate,
        TotalRainToday, CurrPressure, TotalRainfallCounter, InsideTemp, InsideRH, CMXLatestGust, WindChill, HeatIndex, UVindex,
        SolarRad, EVT, AnnualEVT, ApparentTemp, SolarTheoreticalMax, HrsOfSunshineSoFar, CurrWindBearing, RG11RainToday, TotalRainSinceMidnight
    };

    public struct MonthfileValue
    {
        // commented out values need to be read as Garbage
        public DateTime ThisDate { get; set; }
        /* CurrTemp               */
        /* CurrRH,                */
        /* CurrDewpoint,          */
        public float CMXAverageWind { get; set; }
        public float CMXGustSpeed { get; set; }
        public int AvWindBearing { get; set; }
        /*  CurrRainRate,           */
        /*  TotalRainToday          */
        public float CurrPressure { get; set; }
        /*  TotalRainfallCounter    */
        /*  InsideTemp              */
        /*  InsideRH                */
        public float CMXLatestGust { get; set; }
        /*  WindChill               */
        /*  HeatIndex               */
        /*  UVindex                 */
        public int SolarRad { get; set; }
        //-------------------------------------Up to here was 1.8.5
        /*  EVT                     */
        public float Evt { get; set; }
        /*  AnnualEVT               */
        /*  ApparentTemp            */
        public int SolarTheoreticalMax { get; set; }
        /*  HrsOfSunshineSoFar      */
        //-------------------------------------Up to here was 1.9.1
        public int CurrWindBearing { get; set; }
        /*  CurrWindBearing         */
        //-------------------------------------Up to here was 1.9.2
        /*  RG11RainToday           */
        //-------------------------------------Up to here was 1.9.3
        /*  TotalRainSinceMidnight  */
        //-------------------------------------Up to here was 1.9.4
        /*  Feels Like Temperature */
        //-------------------------------------Up to here was 3.6.0
        /*  Humidex Temperature    */
        //-------------------------------------Up to here was 3.6.12
        public bool Valid { get; set; }
        public bool UseAverageBearing { get; set; }
    }

    public class Monthfile : IDisposable
    {
        readonly CuSupport Sup;
        readonly bool IgnoreDataErrors;
        readonly string[] enumFieldTypeNames;
        readonly string[] MonthfileList;
        readonly string[] DaysOfMiracleAndWonder;

        //readonly DateTime AncientCumulus = new DateTime( 2004, 01, 27 );

        bool disposed;
        string filenameCopy;

        readonly int MaxErrors;
        int ErrorCount;

        bool MonthlyLogsRead;
        List<MonthfileValue> MainMonthList;

        public Monthfile( CuSupport s )
        {
            Sup = s;
            Sup.LogDebugMessage( $"Monthfile constructor: Using fixed path: | data/ |; file: | *log.txt" );

            string temp = Sup.GetUtilsIniValue( "General", "MonthsOfMiracleAndWonder", "" );

            if ( string.IsNullOrEmpty( temp ) )
            {
                // Fill the string in the inifile with the default i.e. the monthnames under the current locale used by CumulusUtils
                for ( int i = 0; i < 12; i++ )
                    //temp += CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i + 1 ).Substring( 0, 3 ) + ",";
                    temp += string.Concat( CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i + 1 ).AsSpan( 0, 3 ), "," );

                temp = temp.Substring( 0, temp.Length - 1 );
                Sup.SetUtilsIniValue( "General", "MonthsOfMiracleAndWonder", temp );
            }

            // The string has already been made and maybe been edited by the user so use these monthnames
            CuSupport.StringRemoveWhiteSpace( temp, " " );
            DaysOfMiracleAndWonder = temp.Split( ',' );

            // Get the list of monthly logfile in the datadirectory and check what type of delimeters we have
            // Unfortunately we cannot use the list when only doing one file read because we do not know the name of the month if 
            // the locale used for CumulusUtils is different from the Locale used for CMX. In that case we use the string[] DaysOfMiracleAndWonder
            MonthfileList = Directory.GetFiles( "data/", "*log.txt" );

            for ( int i = 0; i < MonthfileList.Length; i++ )
            {
                if ( MonthfileList[ i ].Contains( "alltimelog" ) || MonthfileList[ i ].Contains( "AirLink" ) )
                {
                    var foos = new List<string>( MonthfileList );
                    foos.RemoveAt( i-- );
                    MonthfileList = foos.ToArray();
                }
            }

            filenameCopy = "data/" + "copy_" + Path.GetFileName( MonthfileList[ 0 ] );

            if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
            File.Copy( MonthfileList[ 0 ], filenameCopy );

            string[] lines = File.ReadAllLines( filenameCopy );
            Sup.DetectSeparators( lines[ 0 ] );

            File.Delete( filenameCopy );

            enumFieldTypeNames = Enum.GetNames( typeof( MonthfileFieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true", CUtils.Cmp );
            MaxErrors = Convert.ToInt32( Sup.GetUtilsIniValue( "General", "MaxErrors", "10" ), CUtils.Inv );

            return;
        }

        public List<MonthfileValue> ReadMonthlyLogs()
        {
            Sup.LogDebugMessage( $"ReadMonthlyLogs: start." );

            if ( MonthlyLogsRead )
            {
                ;
            }
            else
            {
                List<MonthfileValue> thisList = new List<MonthfileValue>();
                MonthfileValue? tmp;

                foreach ( string file in MonthfileList )
                {
                    ErrorCount = 0; // make sure we only log MaxErrors per file

                    filenameCopy = "data/" + "copy_" + Path.GetFileName( file );
                    if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                    File.Copy( file, filenameCopy );

                    Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: reading {file}" );

                    string[] allLines = File.ReadAllLines( filenameCopy );

                    foreach ( string line in allLines )
                    {
                        tmp = SetValues( Sup.ChangeSeparators( line ) );

                        try
                        {
                            thisList.Add( (MonthfileValue) tmp );
                        }
                        catch
                        {
                            Sup.LogTraceErrorMessage( $"ReadMonthlyLogs: Error in {file} : {line}" );
                            break;
                        }
                    }

                    if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                } // Loop over all files in MonthfileList

                MainMonthList = thisList;
                MonthlyLogsRead = true;
            } // else [from: if (MonthlyLogsRead) ]

            // Adjust for RecordsBeganDate
            //
            int i = MainMonthList.RemoveAll( p => p.ThisDate < CUtils.StartOfObservations );
            Sup.LogTraceInfoMessage( $"Monthfile : RecordsBeganDate used: {CUtils.StartOfObservations}, Number of entries removed from list: {i}" );

            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: MainMonthList created/fetched: {MainMonthList.Count} records." );
            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: End" );

            return MainMonthList;
        } // End ReadMonthlyLogs

        public List<MonthfileValue> ReadPartialMonthlyLogs( DateTime Start, DateTime End )
        {
            Sup.LogDebugMessage( $"ReadPartialMonthlyLogs: start." );

            List<string> FilesToRead = new List<string>();
            List<MonthfileValue> thisList = new List<MonthfileValue>();
            MonthfileValue? tmp;

            // It will be either 1 at most 2 months
            for ( DateTime dt = new DateTime( Start.Year, Start.Month, 1 ); dt <= End; dt = dt.AddMonths( 1 ) )
            {
                string temp = DaysOfMiracleAndWonder[ dt.Month - 1 ];
                FilesToRead.Add( $"{temp}{dt:yy}log.txt" );
            }

            foreach ( string file in FilesToRead )
            {
                ErrorCount = 0; // make sure we only log MaxErrors per file

                filenameCopy = "data/" + "copy_" + Path.GetFileName( file );
                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                File.Copy( "data/" + file, filenameCopy );

                Sup.LogTraceInfoMessage( $"ReadPartialMonthlyLogs: reading {file}" );

                string[] allLines = File.ReadAllLines( filenameCopy );

                foreach ( string line in allLines )
                {
                    tmp = SetValues( Sup.ChangeSeparators( line ) );

                    try
                    {
                        thisList.Add( (MonthfileValue) tmp );
                    }
                    catch
                    {
                        Sup.LogTraceErrorMessage( $"ReadPartialMonthlyLogs: Error in {file} : {line}" );
                    }
                } // End Using the Monthly Log to Read

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
            } // Loop over all files in FilesToRead

            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: End" );


            // Adjust for RecordsBeganDate
            //
            if ( Start < CUtils.StartOfObservations )
            {
                int i = thisList.RemoveAll( p => p.ThisDate < CUtils.StartOfObservations );
                Sup.LogTraceInfoMessage( $"Monthfile : RecordsBeganDate used: {CUtils.StartOfObservations}, Number of entries removed from list: {i}" );
            }

            return thisList;
        } // End ReadMonthlyLogs

        private MonthfileValue? SetValues( string line )
        {
            int FieldInUse = 0;

            string tmpDatestring;
            string[] lineSplit = line.Split( ' ' );

            // Do check for the recordlength. If record length too short, then  return null
            // Upon return, the value null must trigger the skipping of the file The possibility that halfway the month the 
            // correct recordlength will be available is ignored. Month resolution is good enough

            MonthfileValue ThisValue = new MonthfileValue();

            // So the record must be OK, now fill the data

            try
            {
                if ( lineSplit.Length <= (int) MonthfileFieldName.CMXLatestGust )
                {
                    throw new IndexOutOfRangeException( $"Reading Monthfile SetValues : Line too short {lineSplit.Length} ({line})" );
                    /*ThisValue.ThisDate = AncientCumulus; return ThisValue;*/
                }

                FieldInUse = (int) MonthfileFieldName.thisDate;
                tmpDatestring = lineSplit[ FieldInUse ];
                FieldInUse = (int) MonthfileFieldName.thisTime;
                tmpDatestring += " " + lineSplit[ FieldInUse ];
                ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy HH:mm", CUtils.Inv );

                ThisValue.UseAverageBearing = lineSplit.Length <= (int) MonthfileFieldName.CurrWindBearing;

                if ( ThisValue.UseAverageBearing )
                {
                    FieldInUse = (int) MonthfileFieldName.AvWindBearing;
                    ThisValue.AvWindBearing = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );
                }
                else
                {
                    FieldInUse = (int) MonthfileFieldName.CurrWindBearing;
                    ThisValue.CurrWindBearing = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );
                }

                FieldInUse = (int) MonthfileFieldName.CurrPressure;
                ThisValue.CurrPressure = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) MonthfileFieldName.CMXLatestGust;
                ThisValue.CMXLatestGust = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) MonthfileFieldName.SolarRad;
                ThisValue.SolarRad = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) MonthfileFieldName.EVT;
                ThisValue.Evt = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) MonthfileFieldName.SolarTheoreticalMax;
                ThisValue.SolarTheoreticalMax = Convert.ToInt32( lineSplit[ FieldInUse ], CUtils.Inv );

                ThisValue.Valid = true;

                return ThisValue;
            }
            catch ( Exception e ) when ( e is FormatException || e is OverflowException )
            {
                const string m = "MonthfileValue.SetValues";

                ErrorCount++;

                //handle exception
                if ( ErrorCount <= MaxErrors )
                {
                    Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} ({enumFieldTypeNames[ FieldInUse ]})" );
                    Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );

                    Console.WriteLine( $"{m} fail: {e.Message}" );
                    Console.WriteLine( $"{m}: in field nr {FieldInUse} ({enumFieldTypeNames[ FieldInUse ]})" );

                    if ( String.IsNullOrEmpty( lineSplit[ FieldInUse ] ) )
                    {
                        Sup.LogTraceErrorMessage( $"{m}: Field {enumFieldTypeNames[ FieldInUse ]} is Empty" );
                    }
                }

                if ( IgnoreDataErrors )
                {
                    ThisValue.Valid = false;
                    if ( ErrorCount <= MaxErrors )
                        Sup.LogTraceInfoMessage( "Monthfile.SetValues : Continuing to read data" );
                }

                return null;
            }
            catch ( IndexOutOfRangeException e )
            {
                const string m = "MonthfileValue.SetValues";

                ErrorCount++;

                if ( ErrorCount <= MaxErrors )
                {
                    Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} does  not exist in this file" );
                    Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );
                }

                if ( IgnoreDataErrors )
                {
                    ThisValue.Valid = false;
                    if ( ErrorCount <= MaxErrors )
                        Sup.LogTraceInfoMessage( "Monthfile.SetValues : Continuing to read data" );
                }

                return null;
            }
        }

        ~Monthfile()
        {
            Sup.LogTraceInfoMessage( "Monthfile destructor: Closing file and ending program" );
            this.Dispose( false );
        }

        public virtual void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( !this.disposed )
            {
                if ( disposing )
                {
                    // release the large, managed resource here
                }

                // release unmagaed resources here
                this.disposed = true;
            }
        }
    } // End Class Monthfile
}