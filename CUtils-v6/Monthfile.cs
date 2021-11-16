/*
 * Monthfile - Part of CumulusUtils
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

using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace CumulusUtils
{
    public enum MonthfileFieldName
    {
        thisDate, thisTime, CurrTemp, CurrRH, CurrDewpoint, CMXAverageWind, CMXGustSpeed, AvWindBearing, CurrRainRate,
        TotalRainToday, CurrPressure, TotalRainfallCounter, InsideTemp, InsideRH, CMXLatestGust, WindChill, HeatIndex, UVindex,
        SolarRad, EVT, AnnualEVT, ApparentTemp, SolarTheoreticalMax, HrsOfSunshineSoFar, CurrWindBearing, RG11RainToday, TotalRainSinceMidnight
    };

    internal struct MonthfileValue
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


    internal enum MonthfileType
    {
        DashSemicolonComma,   // date separator, ; fieldseparator, , decimal fraction
        SlashSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        PointSemicolonComma,  // date separator, ; fieldseparator, , decimal fraction
        DashCommaPoint,       // - date separator, , fieldseparator, . decimal fraction
        SlashCommaPoint       // date separator, , fieldseparator, . decimal fraction
    };

    internal class Monthfile : IDisposable
    {
        readonly MonthfileType type;
        readonly CuSupport Sup;
        readonly bool IgnoreDataErrors;
        readonly string[] enumFieldTypeNames;
        readonly string[] MonthfileList;
        readonly string[] DaysOfMiracleAndWonder;
        readonly bool UseSQL;
        readonly DateTime AncientCumulus = new DateTime( 2004, 01, 27 );
        readonly CultureInfo provider = CultureInfo.InvariantCulture;

        bool disposed;
        string filenameCopy;

        const int MaxErrors = 10;
        int ErrorCount;

        bool MonthlyLogsRead;
        List<MonthfileValue> MainMonthList;

        public Monthfile( CuSupport s )
        {
            string line;

            Sup = s;
            Sup.LogDebugMessage( $"Monthfile constructor: Using fixed path: | data/ |; file: | *log.txt" );

            string temp = Sup.GetUtilsIniValue( "General", "MonthsOfMiracleAndWonder", "" );

            if ( string.IsNullOrEmpty( temp ) )
            {
                // Fill the string in the inifile with the default i.e. the monthnames under the current locale used by CumulusUtils
                for ( int i = 0; i < 12; i++ )
                    temp += CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i + 1 ).Substring( 0, 3 ) + ",";

                temp = temp.Substring( 0, temp.Length - 1 );
                Sup.SetUtilsIniValue( "General", "MonthsOfMiracleAndWonder", temp );
            }

            // The string has already been made and maybe been edited by the user so use these monthnames
            Sup.StringRemoveWhiteSpace( temp );
            DaysOfMiracleAndWonder = temp.Split( ',' );

            // Get the list of monthly logfile in the datadirectory and check what type of delimeters we have
            // Unfortunately we cannot use the list when only doing one file read because we do not know the name of the month if 
            // the locale used for CumulusUtils is different from the Locale used for CMX. In that case we use the string[] DaysOfMiracleAndWonder
            MonthfileList = Directory.GetFiles( "data/", "*log.txt" );

            for ( int i = 0; i < MonthfileList.Length; i++ )
            {
                if ( MonthfileList[ i ].Contains( "alltimelog" ) || MonthfileList[ i ].Contains( "AirLink" ) )
                {
                    Sup.LogTraceInfoMessage( $"MonthfileList removing from list of monthfiles to read: {MonthfileList[ i ]}" );
                    var foos = new List<string>( MonthfileList );
                    foos.RemoveAt( i-- );
                    MonthfileList = foos.ToArray();
                }
            }

            filenameCopy = "data/" + "copy_" + Path.GetFileName( MonthfileList[ 0 ] );

            if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
            File.Copy( MonthfileList[ 0 ], filenameCopy );

            // Not sure about encoding of this file, let it be handled by the system. No presumtions.
            //
            using ( StreamReader mf = new StreamReader( filenameCopy ) )
            {
                Sup.LogTraceInfoMessage( $"Monthfile constructor: Working on: {filenameCopy}" );

                line = mf.ReadLine();

                if ( line[ 2 ] == '-' && line[ 8 ] == ';' ) type = MonthfileType.DashSemicolonComma;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ';' ) type = MonthfileType.SlashSemicolonComma;
                else if ( line[ 2 ] == '.' && line[ 8 ] == ';' ) type = MonthfileType.PointSemicolonComma;
                else if ( line[ 2 ] == '-' && line[ 8 ] == ',' ) type = MonthfileType.DashCommaPoint;
                else if ( line[ 2 ] == '/' && line[ 8 ] == ',' ) type = MonthfileType.SlashCommaPoint;
                else
                {
                    Sup.LogTraceErrorMessage( "Monthfile constructor: Internal Error - Unkown format of inputfile. Please notify programmer." );
                    Environment.Exit( 0 );
                }
            }

            File.Delete( filenameCopy );

            Sup.LogTraceInfoMessage( $"Monthfile constructor: MonthfileType is {type}" );
            enumFieldTypeNames = Enum.GetNames( typeof( MonthfileFieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true" );
            UseSQL = Sup.GetUtilsIniValue( "General", "UseSQL", "false" ).Equals( "true" );

            return;
        }

        public List<MonthfileValue> ReadMonthlyLogs()
        {
            Sup.LogDebugMessage( $"ReadMonthlyLogs: start." );

            Stopwatch watch = Stopwatch.StartNew();

            if ( MonthlyLogsRead )
            {
                ;
                // We  have the list already so immediately return that list
                // Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: MainMonthList was already created, returning existing list with {MainMonthList.Count} records." );
            }
            else if ( UseSQL )
            {
                MainMonthList = ReadMonthfileFromSQL();
                MonthlyLogsRead = true;
            }
            else
            {
                List<MonthfileValue> thisList = new List<MonthfileValue>();
                MonthfileValue tmp;

                foreach ( string file in MonthfileList )
                {
                    ErrorCount = 0; // make sure we only log MaxErrors per file
                    filenameCopy = "data/" + "copy_" + Path.GetFileName( file );

                    if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                    File.Copy( file, filenameCopy );

                    Sup.LogDebugMessage( $"ReadMonthlyLogs: reading {file}" );

                    //watch = Stopwatch.StartNew();
                    string[] allLines = File.ReadAllLines( filenameCopy ); //got to use this sometime
                    //watch.Stop();
                    //Sup.LogTraceInfoMessage( $"Timing of ReadAllLines for {filenameCopy} = {watch.ElapsedMilliseconds} ms" );

                    bool SanityCheck = true;

                    foreach ( string line in allLines )
                    {
                        string thisLine = ReadLine( line, SanityCheck );
                        SanityCheck = false;

                        tmp = SetValues( thisLine );

                        if ( tmp.ThisDate > AncientCumulus )
                        {
                            thisList.Add( tmp );
                        }
                        else
                        {
                            Sup.LogTraceErrorMessage( $"ReadMonthlyLogs: File {file} skipped, it is from before 27  jan 2004!" );
                            break; // skip the file on the basis of record  length (See SetValues)
                        }
                    }

                    if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                } // Loop over all files in MonthfileList

                MainMonthList = thisList;
                MonthlyLogsRead = true;
            } // else [from: if (MonthlyLogsRead) ]

            watch.Stop();
            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: Timing of Monthly logfile read = {watch.ElapsedMilliseconds} ms" );
            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: MainMonthList created/fetched: {MainMonthList.Count} records." );
            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: End" );

            return MainMonthList;
        } // End ReadMonthlyLogs

        internal List<MonthfileValue> ReadPartialMonthlyLogs( DateTime Start, DateTime End )
        {
            Sup.LogDebugMessage( $"ReadPartialMonthlyLogs: start." );

            Stopwatch watch = Stopwatch.StartNew();

            List<string> FilesToRead = new List<string>();
            List<MonthfileValue> thisList = new List<MonthfileValue>();
            MonthfileValue tmp;

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

                //watch = Stopwatch.StartNew();
                string[] allLines = File.ReadAllLines( filenameCopy ); //got to use this sometime
                //watch.Stop();
                //Sup.LogTraceInfoMessage( $"Timing of ReadAllLines for {filenameCopy} = {watch.ElapsedMilliseconds} ms" );

                bool SanityCheck = true;

                foreach ( string line in allLines )
                {
                    string thisLine = ReadLine( line, SanityCheck );
                    SanityCheck = false;

                    tmp = SetValues( thisLine );
                    thisList.Add( tmp );
                } // End Using the Monthly Log to Read

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
            } // Loop over all files in FilesToRead

            watch.Stop();
            Sup.LogTraceInfoMessage( $"ReadPartialMonthlyLogs: Timing = {watch.ElapsedMilliseconds} ms" );
            Sup.LogTraceInfoMessage( $"ReadMonthlyLogs: End" );

            return thisList;
        } // End ReadMonthlyLogs

        private string ReadLine( string line, bool SanityCheck )
        {
            bool SeparatorInconsistencyFound = false;
            StringBuilder tmpLine = new StringBuilder();

            tmpLine.Append( line );

            if ( SanityCheck )
            {
                // Do a sanity check on the presence of the correct separators which we determined when starting the reading process
                //
                switch ( type )
                {
                    case MonthfileType.DashSemicolonComma:
                        if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ';' ) ) ) SeparatorInconsistencyFound = true; break;

                    case MonthfileType.PointSemicolonComma:
                        if ( !( ( tmpLine[ 2 ] == '.' ) && ( tmpLine[ 8 ] == ';' ) ) ) SeparatorInconsistencyFound = true; break;

                    case MonthfileType.SlashSemicolonComma:
                        if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ';' ) ) ) SeparatorInconsistencyFound = true; break;

                    case MonthfileType.DashCommaPoint:
                        if ( !( ( tmpLine[ 2 ] == '-' ) && ( tmpLine[ 8 ] == ',' ) ) ) SeparatorInconsistencyFound = true; break;

                    case MonthfileType.SlashCommaPoint:
                        if ( !( ( tmpLine[ 2 ] == '/' ) && ( tmpLine[ 8 ] == ',' ) ) ) SeparatorInconsistencyFound = true; break;

                    default:
                        // Should never be here
                        Sup.LogTraceErrorMessage( $"ReadMonthlyLogs: Illegal part of the code, should never be here, FATAL ERROR!" );
                        SeparatorInconsistencyFound = true;
                        break;
                }
            }// Sanity Check

            if ( SeparatorInconsistencyFound )
            {
                // Serious issue: file with different separators detected, clean up datadirectory
                // User has to decide what to do with it
                Sup.LogTraceErrorMessage( $"ReadMonthlyLogs: Separator Inconsistency in {filenameCopy} found, FATAL ERROR! Exiting." );
                Sup.LogTraceErrorMessage( $"ReadMonthlyLogs: Please check {filenameCopy.Remove( 0, 5 )}." );

                if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );

                Dispose();
                Environment.Exit( 0 );
            }
            else
            {
                /*
                 * make a uniform line to read: convert all to SlashCommaPoint
                 */
                if ( type == MonthfileType.DashSemicolonComma )
                {
                    tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                    tmpLine.Replace( ',', '.' );
                    tmpLine.Replace( ';', ',' );
                }
                else if ( type == MonthfileType.PointSemicolonComma )
                {
                    tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                    tmpLine.Replace( ',', '.' );
                    tmpLine.Replace( ';', ',' );
                }
                else if ( type == MonthfileType.SlashSemicolonComma )
                {
                    //tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                    tmpLine.Replace( ',', '.' );
                    tmpLine.Replace( ';', ',' );
                }
                else if ( type == MonthfileType.DashCommaPoint )
                {
                    tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                }
            } // No Separator inconsistency found

            return tmpLine.ToString();
        }

        private MonthfileValue SetValues( string line )
        {
            int FieldInUse = 0;
            string tmpDatestring;
            string[] lineSplit = line.Split( ',' );

            // Do check for the recordlength. If record length too short, then  return null
            // Upon return, the value null must trigger the skipping of the file The possibility that halfway the month the 
            // correct recordlength will be available is ignored. Month resolution is good enough

            MonthfileValue ThisValue = new MonthfileValue();

            if ( lineSplit.Length <= (int) MonthfileFieldName.CMXLatestGust ) { ThisValue.ThisDate = AncientCumulus; return ThisValue; }

            // So the record must be OK, now fill the data

            try
            {
                FieldInUse = (int) MonthfileFieldName.thisDate;
                tmpDatestring = lineSplit[ FieldInUse ];
                FieldInUse = (int) MonthfileFieldName.thisTime;
                tmpDatestring += " " + lineSplit[ FieldInUse ];
                ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy HH:mm", provider );

                ThisValue.UseAverageBearing = lineSplit.Length <= (int) MonthfileFieldName.CurrWindBearing;

                if ( ThisValue.UseAverageBearing )
                {
                    FieldInUse = (int) MonthfileFieldName.AvWindBearing;
                    ThisValue.AvWindBearing = Convert.ToInt32( lineSplit[ FieldInUse ], provider );
                }
                else
                {
                    FieldInUse = (int) MonthfileFieldName.CurrWindBearing;
                    ThisValue.CurrWindBearing = Convert.ToInt32( lineSplit[ FieldInUse ], provider );
                }

                FieldInUse = (int) MonthfileFieldName.CurrPressure;
                ThisValue.CurrPressure = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) MonthfileFieldName.CMXLatestGust;
                ThisValue.CMXLatestGust = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) MonthfileFieldName.EVT;
                ThisValue.Evt = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) MonthfileFieldName.SolarRad;
                ThisValue.SolarRad = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) MonthfileFieldName.SolarTheoreticalMax;
                ThisValue.SolarTheoreticalMax = Convert.ToInt32( lineSplit[ FieldInUse ], provider );

                ThisValue.Valid = true;
            }
            catch ( Exception e ) when ( e is FormatException || e is OverflowException )
            {
                const string m = "MonthfileValue.SetValues";

                ErrorCount++;

                //handle exception
                if ( ErrorCount < MaxErrors )
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
                    if ( ErrorCount < MaxErrors ) Sup.LogTraceInfoMessage( "Monthfile.SetValues : Continuing to read data" );
                }
                else
                {
                    // Environment.Exit(0);
                    throw;
                }
            }
            catch ( IndexOutOfRangeException e )
            {
                const string m = "MonthfileValue.SetValues";

                ErrorCount++;

                if ( ErrorCount < MaxErrors )
                {
                    Sup.LogTraceErrorMessage( $"{m} fail: {e.Message}" );
                    Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} does  not exist in this file" );
                    Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );
                }

                if ( IgnoreDataErrors )
                {
                    ThisValue.Valid = false;
                    if ( ErrorCount < MaxErrors ) Sup.LogTraceInfoMessage( "Monthfile.SetValues : Continuing to read data" );
                }
                else
                {
                    throw;
                }
            }

            return ThisValue;
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

        List<MonthfileValue> ReadMonthfileFromSQL()
        {
            const int BatchSize = 2500;
            int NrOfRecords;

            List<MonthfileValue> tmpList = new List<MonthfileValue>();

            try
            {
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
                {
                    Server = Sup.GetCumulusIniValue( "MySQL", "Host", "" ),
                    Port = Convert.ToUInt32( Sup.GetCumulusIniValue( "MySQL", "Port", "" ) ),
                    UserID = Sup.GetCumulusIniValue( "MySQL", "User", "" ),
                    Password = Sup.GetCumulusIniValue( "MySQL", "Pass", "" ),
                    Database = Sup.GetCumulusIniValue( "MySQL", "Database", "" )
                };

                Sup.LogDebugMessage( $"ReadMonthfileFromSQL: Reading Monthfile records from {builder.Database}@{builder.Server}" );

                using ( MySqlConnection connection = new MySqlConnection( builder.ConnectionString ) )
                {
                    CultureInfo ci = CultureInfo.InvariantCulture;

                    using ( MySqlCommand command = new MySqlCommand( "SELECT COUNT(*) FROM Monthly;", connection ) )
                    {

                        // command.CommandTimeout = 120;
                        connection.Open();
                        using ( MySqlDataReader reader = command.ExecuteReader() )
                        {
                            reader.Read();
                            NrOfRecords = reader.GetInt32( 0 );
                        }


                        for ( int i = 0; i < NrOfRecords; i += BatchSize )
                        {
                            command.CommandText = $"SELECT * FROM Monthly LIMIT {BatchSize} OFFSET { i }; ";

                            Console.Write( $"{i}\r" );

                            using ( MySqlDataReader reader = command.ExecuteReader() )
                            {
                                while ( reader.Read() )
                                {
                                    MonthfileValue tmp = new MonthfileValue
                                    {
                                        ThisDate = reader.GetDateTime( (int) MonthfileFieldName.thisDate ),
                                        CMXAverageWind = reader.GetFloat( (int) MonthfileFieldName.CMXAverageWind - 1 ),
                                        CMXGustSpeed = reader.GetFloat( (int) MonthfileFieldName.CMXGustSpeed - 1 ),
                                        AvWindBearing = Convert.ToInt32( reader.GetString( (int) MonthfileFieldName.AvWindBearing - 1 ) ),
                                        CurrPressure = reader.GetFloat( (int) MonthfileFieldName.CurrPressure - 1 ),
                                        CMXLatestGust = reader.GetFloat( (int) MonthfileFieldName.CMXLatestGust - 1 ),
                                        SolarRad = reader.GetInt32( (int) MonthfileFieldName.SolarRad - 1 ),
                                        Evt = reader.GetFloat( (int) MonthfileFieldName.EVT - 1 ),
                                        SolarTheoreticalMax = reader.GetInt32( (int) MonthfileFieldName.SolarTheoreticalMax - 1 ),
                                        CurrWindBearing = Convert.ToInt32( reader.GetString( (int) MonthfileFieldName.CurrWindBearing - 1 ) ),

                                        Valid = true
                                    };

                                    tmpList.Add( tmp );
                                } // Loop over the records
                            } // using: Execute the reader
                        }
                    } // using: Execute the command

                } // using: Connection

                Sup.LogDebugMessage( $"ReadMonthfileFromSQL: Reading Monthly MySQL Done" );
            }
            catch ( MySqlException e )
            {
                Console.WriteLine( $"ReadMonthfileFromSQL: Exception - {e.ErrorCode} - {e.Message}" );
            }

            return tmpList;
        } // End ReadMonthfileFromSQL 
    } // End Class Monthfile
}

#region GrauweKiekendief

#if GrauweKiekendief
      // Maatwerk voor het Grauwe Kiekendief onderzoek, bewaard ivm de sort van de monthlist
      // First part is from the reading of the monthly files, second  part is the sorting function
      using (StreamWriter of = new StreamWriter("GrauweKiekendief.txt"))
      {
        foreach(MonthfileValue entry in MainMonthList)
        {
          of.WriteLine($"{entry.ThisDate:dd/MM/yy HH:mm},{entry.CurrPressure:F1}");
        }
      }

      // We want a sorted list. Not really necessary but for debugging purposes nice.
      MainMonthList.Sort(new compareDates());

    private class compareDates : Comparer<MonthfileValue>
    {
      public override int Compare(MonthfileValue x, MonthfileValue y)
      {
        if (x.ThisDate > y.ThisDate) return (1);
        else if (x.ThisDate < y.ThisDate) return (-1);
        else return (0);
      }
    }
#endif

#endregion