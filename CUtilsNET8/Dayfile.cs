/*
 * Dayfile - Part of CumulusUtils
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

    public struct DayfileValue
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

    public class Dayfile : IDisposable
    {
        private readonly CuSupport Sup;
        private readonly string filename;
        private readonly string[] lines;

        private readonly bool IgnoreDataErrors;
        private readonly string[] enumFieldTypeNames;

        private bool disposed;

        public Dayfile( CuSupport s )
        {
            Sup = s;

            filename = "data/dayfile.txt";

            Sup.LogDebugMessage( "Dayfile: Reading the dayfile.txt..." );
            lines = File.ReadAllLines( filename );

            Sup.LogDebugMessage( "Dayfile: Detecting Separators..." );
            Sup.DetectSeparators( lines[ 0 ] );

            enumFieldTypeNames = Enum.GetNames( typeof( FieldName ) );
            IgnoreDataErrors = Sup.GetUtilsIniValue( "General", "IgnoreDataErrors", "true" ).Equals( "true", CUtils.Cmp );

            return;
        }

        public List<DayfileValue> DayfileRead()
        {
            List<DayfileValue> tmpMainlist = new List<DayfileValue>();
            DayfileValue tmp;

            foreach ( string line in lines )
            {
                tmp = SetValues( Sup.ChangeSeparators( line ), new DayfileValue() );

                // valid is a consequence of errors in the datafile while the user expressed the wish to continue
                // through the ini parameter 'IgnoreDataErrors=true'
                if ( tmp.Valid )
                {
                    tmpMainlist.Add( tmp );
                    SetExtraValues( tmpMainlist );
                }
            }

            Sup.LogTraceInfoMessage( $"CumulusUtils : Read dayfile.txt succesfully - {tmpMainlist.Count} records" );

            return tmpMainlist;
        }

        private DayfileValue SetValues( string line, DayfileValue ThisValue )
        {
            string tmpDatestring;
            string[] lineSplit = line.Split( ' ' );  // Always fields separated by ' '
            int FieldInUse = (int) FieldName.thisDate;

            try
            {
                tmpDatestring = lineSplit[ FieldInUse ];
                ThisValue.ThisDate = DateTime.ParseExact( tmpDatestring, "dd/MM/yy", CUtils.Inv );

                FieldInUse = (int) FieldName.highWindGust;
                ThisValue.HighWindGust = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeHighWindGust;
                ThisValue.TimeHighWindGust = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.minTemp;
                ThisValue.MinTemp = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeMinTemp;
                ThisValue.TimeMinTemp = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.maxTemp;
                ThisValue.MaxTemp = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeMaxTemp;
                ThisValue.TimeMaxTemp = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.minBarometer;
                ThisValue.MinBarometer = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeMinBarometer;
                ThisValue.TimeMinBarometer = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.maxBarometer;
                ThisValue.MaxBarometer = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeMaxBarometer;
                ThisValue.TimeMaxBarometer = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.maxRainRate;
                ThisValue.MaxRainRate = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeMaxRainRate;
                ThisValue.TimeMaxRainRate = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.totalRainThisDay;
                ThisValue.TotalRainThisDay = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                // Special handling of the rain periods, finished after the caller gets control back;
                if ( ThisValue.TotalRainThisDay >= (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, GlobConst.RainLimit ) ) { ThisValue.DryPeriod = 0; ThisValue.WetPeriod = 1; }
                else { ThisValue.DryPeriod = 1; ThisValue.WetPeriod = 0; }

                ThisValue.MonthlyRain = ThisValue.TotalRainThisDay;     // do the actual work in SetExtraValues
                ThisValue.YearToDateRain = ThisValue.TotalRainThisDay;  // do the actual work in SetExtraValues

                // Up to here goes the The first version of cumulus.  These fields MUST be present!!

                //  v 1.8.9 build 907
                FieldInUse = (int) FieldName.averageTempThisDay;
                ThisValue.AverageTempThisDay = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.totalWindRunThisDay;
                ThisValue.TotalWindRun = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                //  v 1.9.0
                FieldInUse = (int) FieldName.highAverageWindSpeed;
                ThisValue.HighAverageWindSpeed = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeHighAverageWindSpeed;
                ThisValue.TimeHighAverageWindSpeed = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                //  v 1.9.1
                FieldInUse = (int) FieldName.lowHumidity;
                ThisValue.LowHumidity = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeLowHumidity;
                ThisValue.TimeLowHumidity = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.highHumidity;
                ThisValue.HighHumidity = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeHighHumidity;
                ThisValue.TimeHighHumidity = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                // 3 floats, 1 time, 1 float, 1 time, 1 float, 1 time
                FieldInUse = (int) FieldName.evapotranspiration;
                ThisValue.EvapoTranspiration = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.hrsofsunshine;
                ThisValue.HrsOfSunshine = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.highHourlyRain;
                ThisValue.HighHourlyRain = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.timeHighHourlyRain;
                ThisValue.TimeHighHourlyRain = DateTime.ParseExact( tmpDatestring + " " + lineSplit[ FieldInUse ], "dd/MM/yy HH:mm", CUtils.Inv );

                FieldInUse = (int) FieldName.heatingdegreedays;
                ThisValue.HeatingDegreeDays = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

                FieldInUse = (int) FieldName.coolingdegreedays;
                ThisValue.CoolingDegreeDays = Convert.ToSingle( lineSplit[ FieldInUse ], CUtils.Inv );

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

                //Sup.LogTraceVerboseMessage( "SetValues after adding the values:" );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: Original Line " + line );

                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _thisDate: " + ThisValue.ThisDate.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values:_highWindGust: " + ThisValue.HighWindGust.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values:_timeHighWindGust: " + ThisValue.TimeHighWindGust.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values:_minTemp :" + ThisValue.MinTemp.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMinTemp: " + ThisValue.TimeMinTemp.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _maxTemp: " + ThisValue.MaxTemp.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMaxTemp: " + ThisValue.TimeMaxTemp.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _minBarometer: " + ThisValue.MinBarometer.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMinBarometer: " + ThisValue.TimeMinBarometer.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _maxBarometer: " + ThisValue.MaxBarometer.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMaxBarometer: " + ThisValue.TimeMaxBarometer.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _maxRainRate: " + ThisValue.MaxRainRate.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMaxRainRate: " + ThisValue.TimeMaxRainRate.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _totalRainThisDay: " + ThisValue.TotalRainThisDay.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values:  _dryPeriod: " + ThisValue.DryPeriod.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _wetPeriod: " + ThisValue.WetPeriod.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _monthlyRain: " + ThisValue.MonthlyRain.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _averageTempThisDay: " + ThisValue.AverageTempThisDay.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _totalWindRun: " + ThisValue.TotalWindRun.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _highAverageWindSpeed: " + ThisValue.HighAverageWindSpeed.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeHighAverageWindSpeed: " + ThisValue.TimeHighAverageWindSpeed.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _lowHumidity: " + ThisValue.LowHumidity.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeLowHumidity: " + ThisValue.TimeLowHumidity.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _highHumidity: " + ThisValue.HighHumidity.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeMinTemp: " + ThisValue.TimeHighHumidity.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _evapotranspiration: " + ThisValue.EvapoTranspiration.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _highHourlyRain: " + ThisValue.HighHourlyRain.ToString( CultureInfo.CurrentCulture ) );
                //Sup.LogTraceVerboseMessage( "SetValues after adding the values: _timeHighHourlyRain : " + ThisValue.TimeHighHourlyRain.ToString( CultureInfo.CurrentCulture ) );

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
                Sup.LogTraceErrorMessage( $"{m}: in field nr {FieldInUse} does  not exist in this file {filename}" );
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

        public void SetExtraValues( List<DayfileValue> values )
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

            if ( values[ lastEntry ].TotalRainThisDay >= (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, GlobConst.RainLimit ) )  // Dew is not counted in the .1 mm registering equipment (e.g. Ecowitt)
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
                }

                // release unmagaed resources here
                disposed = true;
            }
        }
    }
}
