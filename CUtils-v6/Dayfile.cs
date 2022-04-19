/*
 * Dayfile - Part of CumulusUtils
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
using MySqlConnector;


namespace CumulusUtils
{
    public enum FieldName
    {
        thisDate, highWindGust, bearingHighWindGust, timeHighWindGust, minTemp, timeMinTemp, maxTemp, timeMaxTemp,
        minBarometer, timeMinBarometer, maxBarometer, timeMaxBarometer, maxRainRate, timeMaxRainRate, totalRainThisDay,
        averageTempThisDay, totalWindRunThisDay, highAverageWindSpeed, timeHighAverageWindSpeed, lowHumidity,
        timeLowHumidity, highHumidity, timeHighHumidity, evapotranspiration, hrsofsunshine, highheatindex, timeofhighheatindex,
        highapparenttemp, timeofhighapptemp, lowapparenttemp, timeoflowapptemp, highHourlyRain, timeHighHourlyRain, lowwindchill,
        timeoflowwindchill, highdewpoint, timeofhighdewpoint, lowdewpoint, timeoflowdewpoint, dominantWindbearing,
        heatingdegreedays, coolingdegreedays, highsolarRadiation, timeofHighsolarRadiation, highUVindex, timeofHighUVIndex,
        highFeelsLike, timeofhighFeelsLike, lowFeelsLike, timeoflowFeelsLike, highHumidex, timeofhighHumidex
    };

    internal struct DayfileValue
    {
        // commented out values need to be read as Garbage
        public DateTime ThisDate { get; set; }
        public float HighWindGust { get; set; }
        public DateTime TimeHighWindGust { get; set; }
        public float MinTemp { get; set; }
        public DateTime TimeMinTemp { get; set; }
        public float MaxTemp { get; set; }
        public DateTime TimeMaxTemp { get; set; }
        public float MinBarometer { get; set; }
        public DateTime TimeMinBarometer { get; set; }
        public float MaxBarometer { get; set; }
        public DateTime TimeMaxBarometer { get; set; }
        public float MaxRainRate { get; set; }
        public DateTime TimeMaxRainRate { get; set; }
        public float TotalRainThisDay { get; set; }
        public float AverageTempThisDay { get; set; }
        public float TotalWindRun { get; set; }
        public float HighAverageWindSpeed { get; set; }
        public DateTime TimeHighAverageWindSpeed { get; set; }
        public float LowHumidity { get; set; }
        public DateTime TimeLowHumidity { get; set; }
        public float HighHumidity { get; set; }
        public DateTime TimeHighHumidity { get; set; }

        /* evapotranspiration       */
        public float EvapoTranspiration { get; set; }

        /* hrs of sunshine          */
        public float HrsOfSunshine { get; set; }
        /* high heatindex           */
        /* time of high heatindex   */
        /* high apparent temp.      */
        /* time of high app. temp.  */
        /* low apparent temp.       */
        /* time of low app. temp.   */

        public float HighHourlyRain { get; set; }
        public DateTime TimeHighHourlyRain { get; set; }

        /* low windchill            */
        /* time of low windchill    */
        /* high dew point           */
        /* time of high dew point   */
        /* low dew point            */
        /* time of low dew point    */
        /* Dominant Windbearing     */

        /* Heating degree days      */
        /* Cooling degree days      */
        public float HeatingDegreeDays { get; set; }
        public float CoolingDegreeDays { get; set; }
        /* High solar Radiation     */
        /* time of High solar Radiation */
        /* High UV index            */
        /* time of High UV Index    */

        /* High FeelsLike           */
        public float HighFeelsLike { get; set; }
        /* time of High FeelsLike   */
        public DateTime TimeHighFeelsLike { get; set; }
        /* Low FeelsLike            */
        public float LowFeelsLike { get; set; }
        /* time of Low FeelsLike    */
        public DateTime TimeLowFeelsLike { get; set; }
        /* High Humidex             */
        public float HighHumidex { get; set; }
        /* time of High Humidex     */
        public DateTime TimeHighHumidex { get; set; }

        public int WetPeriod { get; set; }
        public int DryPeriod { get; set; }
        public float MonthlyRain { get; set; }
        public float YearToDateRain { get; set; }
        public bool Valid { get; set; }
    }


    internal enum DayfileType
    {
        DashSemicolonComma,   // - date separator, ; fieldseparator, , decimal fraction
        SlashSemicolonComma,  // / date separator, ; fieldseparator, , decimal fraction
        PointSemicolonComma,  // . date separator, ; fieldseparator, , decimal fraction
        DashCommaPoint,       // - date separator, , fieldseparator, . decimal fraction
        SlashCommaPoint       // / date separator, , fieldseparator, . decimal fraction
    };

    internal class Dayfile : IDisposable
    {
        private readonly string filenameCopy;
        private readonly StreamReader df;
        private readonly DayfileType type;
        private readonly CuSupport Sup;
        private readonly bool IgnoreDataErrors;
        private readonly string[] enumFieldTypeNames;
        private readonly bool UseSQL;

        private bool disposed;

        public Dayfile( CuSupport s )
        {
            string line;

            Sup = s;
            string path = "data/";
            string filename = "dayfile.txt";

            try
            {
                Sup.LogDebugMessage( $"Dayfile constructor: Using path: | data/ |; file: | dayfile.txt" );

                filenameCopy = path + "copy_" + filename;
                if ( File.Exists( filenameCopy ) )
                    File.Delete( filenameCopy );
                File.Copy( path + filename, filenameCopy );
                df = new StreamReader( filenameCopy ); //File.OpenRead(filenameCopy);

                Sup.LogTraceInfoMessage( $"Dayfile constructor: Working on: {filenameCopy}" );
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Dayfile constructor exiting: {e.Message}" );
                Environment.Exit( 0 );
                throw; // satisfy the compiler
            }

            line = df.ReadLine();
            df.Dispose();
            df = new StreamReader( filenameCopy ); //reset to start

            if ( line[ 2 ] == '-' && line[ 8 ] == ';' )
                type = DayfileType.DashSemicolonComma;
            else if ( line[ 2 ] == '/' && line[ 8 ] == ';' )
                type = DayfileType.SlashSemicolonComma;
            else if ( line[ 2 ] == '.' && line[ 8 ] == ';' )
                type = DayfileType.PointSemicolonComma;
            else if ( line[ 2 ] == '-' && line[ 8 ] == ',' )
                type = DayfileType.DashCommaPoint;
            else if ( line[ 2 ] == '/' && line[ 8 ] == ',' )
                type = DayfileType.SlashCommaPoint;
            else
            {
                Sup.LogTraceErrorMessage( "Dayfile constructor: Internal Error - Unkown format of inputfile. Please notify programmer." );
                Environment.Exit( 0 );
            }

            Sup.LogTraceVerboseMessage( $"Dayfile constructor: DayfileType is {type}" );

            enumFieldTypeNames = Enum.GetNames( typeof( FieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true" );
            UseSQL = Sup.GetUtilsIniValue( "General", "UseSQL", "false" ).Equals( "true" );

            return;
        }

        internal List<DayfileValue> DayfileRead()
        {
            string line;
            DayfileValue tmp;
            List<DayfileValue> tmpMainlist = new List<DayfileValue>();

            // Start the p rocessing of dayfile.txt
            Sup.LogTraceInfoMessage( "CumulusUtils : Creating class dayfile -> Opening dayfile.txt" );

            Stopwatch watch = Stopwatch.StartNew();

            if ( UseSQL )
            {
                tmpMainlist = ReadDayfileFromSQL();
            }
            else
            {
                // Use classical Dayfile read
                line = ReadLine();

                while ( !String.IsNullOrEmpty( line ) )
                {
                    tmp = SetValues( line, new DayfileValue() );

                    // valid is a consequence of errors in the datafile while the user expressed the wish to continue
                    // through the ini parameter 'IgnoreDataErrors=true'
                    if ( tmp.Valid )
                    {
                        tmpMainlist.Add( tmp );
                        SetExtraValues( tmpMainlist );
                    }

                    line = ReadLine();
                }

                watch.Stop();
                Sup.LogTraceInfoMessage( $"Timing of Dayfile load = {watch.ElapsedMilliseconds} ms" );
            }

            Sup.LogTraceInfoMessage( $"CumulusUtils : Read dayfile.txt succesfully - {tmpMainlist.Count} records" );

            return tmpMainlist;
        }

        private string ReadLine()
        {
            //int i, separatorCount = 0;
            StringBuilder tmpLine = new StringBuilder();

            if ( df.EndOfStream )
                Sup.LogTraceVerboseMessage( "Dayfile : EOF detected" ); // nothing to do;
            else
            {
                tmpLine.Append( df.ReadLine() );

                /*
                 * make a uniform line to read: convert all to SlashCommaPoint
                 */
                if ( type == DayfileType.DashSemicolonComma )
                {
                    tmpLine[ 2 ] = '/';
                    tmpLine[ 5 ] = '/';
                    tmpLine.Replace( ',', '.' );
                    tmpLine.Replace( ';', ',' );
                }
                else if ( type == DayfileType.PointSemicolonComma )
                {
                    tmpLine[ 2 ] = '/';
                    tmpLine[ 5 ] = '/';
                    tmpLine.Replace( ',', '.' );
                    tmpLine.Replace( ';', ',' );
                }
                else if ( type == DayfileType.SlashSemicolonComma )
                {
                    //tmpLine[ 2 ] = '/'; tmpLine[ 5 ] = '/';
                    tmpLine.Replace( ',', '.' );
                    tmpLine.Replace( ';', ',' );
                }
                else if ( type == DayfileType.DashCommaPoint )
                {
                    tmpLine[ 2 ] = '/';
                    tmpLine[ 5 ] = '/';
                }
            }

            return tmpLine.ToString();
        }

        private DayfileValue SetValues( string line, DayfileValue ThisValue )
        {
            string tmpDatestring;
            string[] lineSplit = line.Split( ',' );
            int FieldInUse = (int) FieldName.thisDate;

            CultureInfo provider = CultureInfo.InvariantCulture;

            try
            {
                tmpDatestring = lineSplit[ FieldInUse ];
                ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy", provider );

                FieldInUse = (int) FieldName.highWindGust;
                ThisValue.HighWindGust = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeHighWindGust;
                ThisValue.TimeHighWindGust = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.minTemp;
                ThisValue.MinTemp = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeMinTemp;
                ThisValue.TimeMinTemp = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.maxTemp;
                ThisValue.MaxTemp = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeMaxTemp;
                ThisValue.TimeMaxTemp = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.minBarometer;
                ThisValue.MinBarometer = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeMinBarometer;
                ThisValue.TimeMinBarometer = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.maxBarometer;
                ThisValue.MaxBarometer = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeMaxBarometer;
                ThisValue.TimeMaxBarometer = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.maxRainRate;
                ThisValue.MaxRainRate = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeMaxRainRate;
                ThisValue.TimeMaxRainRate = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.totalRainThisDay;
                ThisValue.TotalRainThisDay = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                // Special handling of the rain periods, finished after the caller gets control back;
                if ( ThisValue.TotalRainThisDay >= (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, 0.2 ) ) { ThisValue.DryPeriod = 0; ThisValue.WetPeriod = 1; }
                else { ThisValue.DryPeriod = 1; ThisValue.WetPeriod = 0; }

                ThisValue.MonthlyRain = ThisValue.TotalRainThisDay;     // do the actual work in SetExtraValues
                ThisValue.YearToDateRain = ThisValue.TotalRainThisDay;  // do the actual work in SetExtraValues

                // Up to here goes the The first version of cumulus.  These fields MUST be present!!

                //  v 1.8.9 build 907
                FieldInUse = (int) FieldName.averageTempThisDay;
                ThisValue.AverageTempThisDay = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.totalWindRunThisDay;
                ThisValue.TotalWindRun = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                //  v 1.9.0
                FieldInUse = (int) FieldName.highAverageWindSpeed;
                ThisValue.HighAverageWindSpeed = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeHighAverageWindSpeed;
                ThisValue.TimeHighAverageWindSpeed = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                //  v 1.9.1
                FieldInUse = (int) FieldName.lowHumidity;
                ThisValue.LowHumidity = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeLowHumidity;
                ThisValue.TimeLowHumidity = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.highHumidity;
                ThisValue.HighHumidity = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeHighHumidity;
                ThisValue.TimeHighHumidity = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                // 3 floats, 1 time, 1 float, 1 time, 1 float, 1 time
                FieldInUse = (int) FieldName.evapotranspiration;
                ThisValue.EvapoTranspiration = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.hrsofsunshine;
                ThisValue.HrsOfSunshine = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.highHourlyRain;
                ThisValue.HighHourlyRain = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.timeHighHourlyRain;
                ThisValue.TimeHighHourlyRain = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", provider );

                FieldInUse = (int) FieldName.heatingdegreedays;
                ThisValue.HeatingDegreeDays = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                FieldInUse = (int) FieldName.coolingdegreedays;
                ThisValue.CoolingDegreeDays = Convert.ToSingle( lineSplit[ FieldInUse ], provider );

                // The fields below are actually already in the JSON delivered by CMX so don't bother for the time being.
                //
                //FieldInUse = (int)FieldName.highFeelsLike;
                //ThisValue.HighFeelsLike = Convert.ToSingle(lineSplit[FieldInUse], provider);

                //FieldInUse = (int)FieldName.timeofhighFeelsLike;
                //ThisValue.TimeHighFeelsLike = DateTime.ParseExact(tmpDatestring + " " + lineSplit[FieldInUse], "dd/MM/yy HH:mm", provider);

                //FieldInUse = (int)FieldName.lowFeelsLike;
                //ThisValue.LowFeelsLike = Convert.ToSingle(lineSplit[FieldInUse], provider);

                //FieldInUse = (int)FieldName.timeoflowFeelsLike;
                //ThisValue.TimeLowFeelsLike = DateTime.ParseExact(tmpDatestring + " " + lineSplit[FieldInUse], "dd/MM/yy HH:mm", provider);

                //FieldInUse = (int)FieldName.highHumidex;
                //ThisValue.HighHumidex = Convert.ToSingle(lineSplit[FieldInUse], provider);

                //FieldInUse = (int)FieldName.timeofhighHumidex;
                //ThisValue.TimeHighHumidex = DateTime.ParseExact(tmpDatestring + " " + lineSplit[FieldInUse], "dd/MM/yy HH:mm", provider);

                Sup.LogTraceVerboseMessage( "SetValues after adding the values:" );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: Original Line " + line );

                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _thisDate: " + ThisValue.ThisDate.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values:_highWindGust: " + ThisValue.HighWindGust.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values:_timeHighWindGust: " + ThisValue.TimeHighWindGust.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values:_minTemp :" + ThisValue.MinTemp.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMinTemp: " + ThisValue.TimeMinTemp.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _maxTemp: " + ThisValue.MaxTemp.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMaxTemp: " + ThisValue.TimeMaxTemp.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _minBarometer: " + ThisValue.MinBarometer.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMinBarometer: " + ThisValue.TimeMinBarometer.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _maxBarometer: " + ThisValue.MaxBarometer.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMaxBarometer: " + ThisValue.TimeMaxBarometer.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _maxRainRate: " + ThisValue.MaxRainRate.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMaxRainRate: " + ThisValue.TimeMaxRainRate.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _totalRainThisDay: " + ThisValue.TotalRainThisDay.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values:  _dryPeriod: " + ThisValue.DryPeriod.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _wetPeriod: " + ThisValue.WetPeriod.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _monthlyRain: " + ThisValue.MonthlyRain.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _averageTempThisDay: " + ThisValue.AverageTempThisDay.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _totalWindRun: " + ThisValue.TotalWindRun.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _highAverageWindSpeed: " + ThisValue.HighAverageWindSpeed.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeHighAverageWindSpeed: " + ThisValue.TimeHighAverageWindSpeed.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _lowHumidity: " + ThisValue.LowHumidity.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeLowHumidity: " + ThisValue.TimeLowHumidity.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _highHumidity: " + ThisValue.HighHumidity.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMinTemp: " + ThisValue.TimeHighHumidity.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _evapotranspiration: " + ThisValue.EvapoTranspiration.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _highHourlyRain: " + ThisValue.HighHourlyRain.ToString( CultureInfo.CurrentCulture ) );
                Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeHighHourlyRain : " + ThisValue.TimeHighHourlyRain.ToString( CultureInfo.CurrentCulture ) );

                ThisValue.Valid = true;
            }
            catch ( Exception e ) when ( e is FormatException || e is OverflowException )
            {
                const string m = "DayfileValue.SetValues";

                //handle exception
                Sup.LogTraceErrorMessage( $"{m} fail: " + e.Message );
                Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} ({enumFieldTypeNames[ FieldInUse ]})" );
                Sup.LogTraceErrorMessage( $"{m}: line is: {line}" );

                Console.WriteLine( $"{m} fail: " + e.Message );
                Console.WriteLine( $"{m}: in field nr {FieldInUse} ({enumFieldTypeNames[ FieldInUse ]})" );

                if ( String.IsNullOrEmpty( lineSplit[ FieldInUse ] ) )
                {
                    Sup.LogTraceErrorMessage( $"{m}: Field {enumFieldTypeNames[ FieldInUse ]} is Empty" );
                }

                if ( IgnoreDataErrors )
                {
                    ThisValue.Valid = false;
                    Sup.LogTraceErrorMessage( "DayfileValue.SetValues : Continuing to read data" );
                }
                else
                {
                    Sup.LogTraceErrorMessage( "DayfileValue.SetValues : data error - not continuing to read data." );
                    throw;
                }
            }
            catch ( IndexOutOfRangeException e )
            {
                const string m = "DayfileValue.SetValues";

                Sup.LogTraceErrorMessage( $"{m} fail: " + e.Message );
                Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} does  not exist in this file {filenameCopy}" );
                Sup.LogTraceErrorMessage( $"{m}: line is: '{line}')" );

                if ( IgnoreDataErrors )
                {
                    ThisValue.Valid = false;
                    Sup.LogTraceErrorMessage( "DayfileValue.SetValues : Continuing to read data" );
                }
                else
                {
                    Sup.LogTraceErrorMessage( "DayfileValue.SetValues : data error - not continuing to read data." );
                    throw;
                }
            }

            return ThisValue;
        }

        internal void SetExtraValues( List<DayfileValue> values )
        {
            // This function does the additional calculations which require the history (and thus the MainList)
            int lastEntry;
            DayfileValue tmpValue;

            lastEntry = values.Count - 1;
            if ( lastEntry == 0 )
                return;

            tmpValue = values[ lastEntry ];

            if ( values[ lastEntry ].ThisDate.Day != 1 )  //First day of the month so Monthly rain must be reset
            {
                // The total rain is carried over the month in this value
                tmpValue.MonthlyRain += values[ lastEntry - 1 ].MonthlyRain;
            }

            if ( !( tmpValue.ThisDate.Month == 1 && tmpValue.ThisDate.Day == 1 ) ) // First of January, reset YearToDateRain the counter
            {
                tmpValue.YearToDateRain += values[ lastEntry - 1 ].YearToDateRain;
            }

            if ( values[ lastEntry ].TotalRainThisDay >= (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, 0.2 ) )  // Dew is not counted in the .1 mm registering equipment (e.g. Ecowitt)
                tmpValue.WetPeriod += values[ lastEntry - 1 ].WetPeriod;
            else
                tmpValue.DryPeriod += values[ lastEntry - 1 ].DryPeriod;

            values.RemoveAt( lastEntry );
            values.Add( tmpValue );

            return;
        }

        ~Dayfile()
        {
            Sup.LogDebugMessage( "Dayfile destructor: Closing file and ending program" );
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
                    df.Dispose();
                    File.Delete( filenameCopy );
                }

                // release unmagaed resources here
                disposed = true;
            }
        }

        List<DayfileValue> ReadDayfileFromSQL()
        {
            List<DayfileValue> tmpList = new List<DayfileValue>();

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

                Sup.LogDebugMessage( $"ReadDayfileFromSQL: Reading Dayfile records from {builder.Database}@{builder.Server}" );

                Stopwatch Watch = Stopwatch.StartNew();

                using ( MySqlConnection connection = new MySqlConnection( builder.ConnectionString ) )
                {
                    CultureInfo ci = CultureInfo.InvariantCulture;
                    String sql = "SELECT * FROM Dayfile;";

                    using ( MySqlCommand command = new MySqlCommand( sql, connection ) )
                    {
                        connection.Open();

                        using ( MySqlDataReader reader = command.ExecuteReader() )
                        {
                            while ( reader.Read() )
                            {
                                string DateAsString = reader.GetDateTime( (int) FieldName.thisDate ).ToString( "dd-MM-yyyy " );

                                DayfileValue tmp = new DayfileValue
                                {
                                    ThisDate = reader.GetDateTime( (int) FieldName.thisDate ),
                                    HighWindGust = reader.GetFloat( (int) FieldName.highWindGust ),
                                    TimeHighWindGust = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeHighWindGust ), "dd-MM-yyyy HH:mm", ci ),
                                    MinTemp = reader.GetFloat( (int) FieldName.minTemp ),
                                    TimeMinTemp = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeMinTemp ), "dd-MM-yyyy HH:mm", ci ),
                                    MaxTemp = reader.GetFloat( (int) FieldName.maxTemp ),
                                    TimeMaxTemp = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeMaxTemp ), "dd-MM-yyyy HH:mm", ci ),
                                    MinBarometer = reader.GetFloat( (int) FieldName.minBarometer ),
                                    TimeMinBarometer = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeMinBarometer ), "dd-MM-yyyy HH:mm", ci ),
                                    MaxBarometer = reader.GetFloat( (int) FieldName.maxBarometer ),
                                    TimeMaxBarometer = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeMaxBarometer ), "dd-MM-yyyy HH:mm", ci ),
                                    MaxRainRate = reader.GetFloat( (int) FieldName.maxRainRate ),
                                    TimeMaxRainRate = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeMaxRainRate ), "dd-MM-yyyy HH:mm", ci ),
                                    TotalRainThisDay = reader.GetFloat( (int) FieldName.totalRainThisDay ),
                                    AverageTempThisDay = reader.GetFloat( (int) FieldName.averageTempThisDay ),
                                    TotalWindRun = reader.GetFloat( (int) FieldName.totalWindRunThisDay ),
                                    HighAverageWindSpeed = reader.GetFloat( (int) FieldName.highAverageWindSpeed ),
                                    TimeHighAverageWindSpeed = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeHighAverageWindSpeed ), "dd-MM-yyyy HH:mm", ci ),
                                    LowHumidity = reader.GetFloat( (int) FieldName.lowHumidity ),
                                    TimeLowHumidity = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeLowHumidity ), "dd-MM-yyyy HH:mm", ci ),
                                    HighHumidity = reader.GetFloat( (int) FieldName.highHumidity ),
                                    TimeHighHumidity = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeHighHumidity ), "dd-MM-yyyy HH:mm", ci ),
                                    EvapoTranspiration = reader.GetFloat( (int) FieldName.evapotranspiration ),
                                    HrsOfSunshine = reader.GetFloat( (int) FieldName.hrsofsunshine ),

                                    // here some additional data are skipped
                                    HighHourlyRain = reader.GetFloat( (int) FieldName.highHourlyRain ),
                                    TimeHighHourlyRain = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeHighHourlyRain ), "dd-MM-yyyy HH:mm", ci ),

                                    // here some additional data are skipped
                                    HeatingDegreeDays = reader.GetFloat( (int) FieldName.heatingdegreedays ),
                                    CoolingDegreeDays = reader.GetFloat( (int) FieldName.coolingdegreedays ),

                                    // here some additional data are skipped
                                    //HighFeelsLike = reader.GetFloat( (int) FieldName.highFeelsLike ),
                                    //TimeHighFeelsLike = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeofhighFeelsLike ), "dd-MM-yyyy HH:mm", ci ),
                                    //LowFeelsLike = reader.GetFloat( (int) FieldName.highFeelsLike ),
                                    //TimeLowFeelsLike = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeoflowFeelsLike ), "dd-MM-yyyy HH:mm", ci ),
                                    //HighHumidex = reader.GetFloat( (int) FieldName.highHumidex ),
                                    //TimeHighHumidex = DateTime.ParseExact( DateAsString + reader.GetString( (int) FieldName.timeofhighHumidex ), "dd-MM-yyyy HH:mm", ci ),

                                    Valid = true
                                };

                                // Special handling of the rain periods, finished after the caller gets control back;
                                if ( tmp.TotalRainThisDay > 0 ) { tmp.DryPeriod = 0; tmp.WetPeriod = 1; }
                                else { tmp.DryPeriod = 1; tmp.WetPeriod = 0; }

                                tmp.MonthlyRain = tmp.TotalRainThisDay;     // do the actual work in SetExtraValues
                                tmp.YearToDateRain = tmp.TotalRainThisDay;  // do the actual work in SetExtraValues

                                tmpList.Add( tmp );
                                SetExtraValues( tmpList );
                            } // Loop over the records
                        } // using: Execute the reader
                    } // using: Execute the command

                } // using: Connection

                Watch.Stop();

                Sup.LogDebugMessage( $"Timing of SQL command generation = {Watch.ElapsedMilliseconds} ms" );
                Sup.LogDebugMessage( $"Reading Dayfile MySQL Done" );
            }
            catch ( MySqlException e )
            {
                Console.WriteLine( $"ReadDayfileFromSQL: Exception - {e.ErrorCode} - {e.Message}" );
            }

            return tmpList;
        }
    }
}
