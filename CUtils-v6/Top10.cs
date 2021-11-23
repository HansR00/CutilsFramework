/*
 * Top10 - Part of CumulusUtils
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    public struct Top10Format
    {
        public int NrOfColumns { get; set; }
        public string BgcolorHeader { get; set; }
        public string BgcolorTable { get; set; }
        public string TxtcolorHeader { get; set; }
        public string TxtAccentTable { get; set; }
    }

    internal class Top10 : IDisposable
    {
        private enum Top10Types
        {
            maxTemp, minTemp, minHumidity, highPressure, lowPressure, highWind, highGust, totalWindrun, highRainRate, highHourlyRain, highDailyRain,
            highestMonthlyRain, longestDryPeriod, longestWetPeriod
        };

        private readonly string[] enumNames;
        private readonly List<string> TypesUnits;
        private readonly List<string> TypesHeaders;
        private readonly List<DayfileValue>[] Top10List;
        private Top10Format Top10TableFormat;

        private int MonthlyRainNrOfMonths; // to memorise the nr of months actually in top10 ist. May be <10 for a young system

        private readonly CuSupport Sup;


        // Constructor
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>" )]
        public Top10( CuSupport s )
        {
            int i;

            Sup = s;
            Sup.LogDebugMessage( "Top10funcs: Initialising Top10 Array" );

            Top10List = new List<DayfileValue>[ Enum.GetNames( typeof( Top10Types ) ).Length ];
            Top10TableFormat = new Top10Format
            {
                NrOfColumns = Convert.ToInt32( Sup.GetUtilsIniValue( "Top10", "NumberOfColumns", "3" ), CultureInfo.InvariantCulture ),
                BgcolorHeader = Sup.GetUtilsIniValue( "Top10", "BackgroundColorHeader", "#d0d0d0" ),
                BgcolorTable = Sup.GetUtilsIniValue( "Top10", "BackgroundColorTable", "#f0f0f0" ),
                TxtcolorHeader = Sup.GetUtilsIniValue( "Top10", "TextColorHeader", "Green" ),
                TxtAccentTable = Sup.GetUtilsIniValue( "Top10", "TextColorAccentTable", "DarkOrange" )
            };

            // init array of enum names (for debugging output purposes and eventually strings in the inifile
            enumNames = Enum.GetNames( typeof( Top10Types ) );
            TypesUnits = new List<string>();
            TypesHeaders = new List<string>();

            // create the Unitstring:
            foreach ( string str in enumNames ) { TypesUnits.Add( "" ); TypesHeaders.Add( "" ); }

            TypesUnits[ (int) Top10Types.maxTemp ] = Sup.StationTemp.Text();
            TypesUnits[ (int) Top10Types.minTemp ] = Sup.StationTemp.Text();
            TypesUnits[ (int) Top10Types.minHumidity ] = "%";
            TypesUnits[ (int) Top10Types.highPressure ] = Sup.StationPressure.Text();
            TypesUnits[ (int) Top10Types.lowPressure ] = Sup.StationPressure.Text();
            TypesUnits[ (int) Top10Types.highWind ] = Sup.StationWind.Text();
            TypesUnits[ (int) Top10Types.highGust ] = Sup.StationWind.Text();
            TypesUnits[ (int) Top10Types.totalWindrun ] = Sup.StationDistance.Text();
            TypesUnits[ (int) Top10Types.highRainRate ] = Sup.StationRain.Text() + "/hr";
            TypesUnits[ (int) Top10Types.highHourlyRain ] = Sup.StationRain.Text();
            TypesUnits[ (int) Top10Types.highDailyRain ] = Sup.StationRain.Text();
            TypesUnits[ (int) Top10Types.highestMonthlyRain ] = Sup.StationRain.Text();
            TypesUnits[ (int) Top10Types.longestDryPeriod ] = Sup.GetCUstringValue( "General", "Days", "Days", false );
            TypesUnits[ (int) Top10Types.longestWetPeriod ] = Sup.GetCUstringValue( "General", "Days", "Days", false );

            TypesHeaders[ (int) Top10Types.maxTemp ] = Sup.GetCUstringValue( "Top10", "MaxTemp", "maxTemp", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.maxTemp ]})";
            TypesHeaders[ (int) Top10Types.minTemp ] = Sup.GetCUstringValue( "Top10", "MinTemp", "minTemp", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.minTemp ]})";
            TypesHeaders[ (int) Top10Types.minHumidity ] = Sup.GetCUstringValue( "Top10", "MinHumidity", "minHumidity", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.minHumidity ]})";
            TypesHeaders[ (int) Top10Types.highPressure ] = Sup.GetCUstringValue( "Top10", "HighPressure", "highPressure", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highPressure ]})";
            TypesHeaders[ (int) Top10Types.lowPressure ] = Sup.GetCUstringValue( "Top10", "LowPressure", "lowPressure", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.lowPressure ]})";
            TypesHeaders[ (int) Top10Types.highWind ] = Sup.GetCUstringValue( "Top10", "HighWind", "highWind", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highWind ]})";
            TypesHeaders[ (int) Top10Types.highGust ] = Sup.GetCUstringValue( "Top10", "HighGust", "highGust", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highGust ]})";
            TypesHeaders[ (int) Top10Types.totalWindrun ] = Sup.GetCUstringValue( "Top10", "TotalRun", "totalRun", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.totalWindrun ]})";
            TypesHeaders[ (int) Top10Types.highRainRate ] = Sup.GetCUstringValue( "Top10", "HighRainRate", "highRainRate", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highRainRate ]})";
            TypesHeaders[ (int) Top10Types.highHourlyRain ] = Sup.GetCUstringValue( "Top10", "HighHourlyRain", "highHourlyRain", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highHourlyRain ]})";
            TypesHeaders[ (int) Top10Types.highDailyRain ] = Sup.GetCUstringValue( "Top10", "HighDailyRain", "highDailyRain", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highDailyRain ]})";
            TypesHeaders[ (int) Top10Types.highestMonthlyRain ] = Sup.GetCUstringValue( "Top10", "HighestMonthlyRain", "highestMonthlyRain", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.highestMonthlyRain ]})";
            TypesHeaders[ (int) Top10Types.longestDryPeriod ] = Sup.GetCUstringValue( "Top10", "LongestDryPeriod", "longestDryPeriod", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.longestDryPeriod ]})";
            TypesHeaders[ (int) Top10Types.longestWetPeriod ] = Sup.GetCUstringValue( "Top10", "LongestWetPeriod", "longestWetPeriod", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.longestWetPeriod ]})";

            for ( i = 0; i < Enum.GetNames( typeof( Top10Types ) ).Length; i++ )
                Top10List[ i ] = new List<DayfileValue>();
        }

        // Below are the comparers

        private class compareHighestMonthlyRain : Comparer<DayfileValue>
        {
            public override int Compare( DayfileValue x, DayfileValue y )
            {
                if ( x.MonthlyRain > y.MonthlyRain )
                    return ( -1 );
                else if ( x.MonthlyRain < y.MonthlyRain )
                    return ( 1 );
                else
                    return ( 0 );
            }
        }

        private class compareLongestDryPeriod : Comparer<DayfileValue>
        {
            public override int Compare( DayfileValue x, DayfileValue y )
            {
                if ( x.DryPeriod > y.DryPeriod )
                    return ( -1 );
                else if ( x.DryPeriod < y.DryPeriod )
                    return ( 1 );
                else
                    return ( 0 );
            }
        }

        private class compareLongestWetPeriod : Comparer<DayfileValue>
        {
            public override int Compare( DayfileValue x, DayfileValue y )
            {
                if ( x.WetPeriod > y.WetPeriod )
                    return ( -1 );
                else if ( x.WetPeriod < y.WetPeriod )
                    return ( 1 );
                else
                    return ( 0 );
            }
        }

        public void GenerateTop10List( List<DayfileValue> ThisList )
        {
            //MaxTemp
            Top10List[ (int) Top10Types.maxTemp ] = ThisList.OrderByDescending( x => x.MaxTemp ).Take( 10 ).ToList();

            //MinTemp
            Top10List[ (int) Top10Types.minTemp ] = ThisList.OrderByDescending( x => x.MinTemp ).ToList();
            Top10List[ (int) Top10Types.minTemp ].Reverse();
            Top10List[ (int) Top10Types.minTemp ] = Top10List[ (int) Top10Types.minTemp ].Take( 10 ).ToList();

            //MinHumidity
            Top10List[ (int) Top10Types.minHumidity ] = ThisList.OrderByDescending( x => x.LowHumidity ).ToList();
            Top10List[ (int) Top10Types.minHumidity ].Reverse();
            Top10List[ (int) Top10Types.minHumidity ] = Top10List[ (int) Top10Types.minHumidity ].Take( 10 ).ToList();

            //HighPressure
            Top10List[ (int) Top10Types.highPressure ] = ThisList.OrderByDescending( x => x.MaxBarometer ).Take( 10 ).ToList();

            //LowPressure
            Top10List[ (int) Top10Types.lowPressure ] = ThisList.OrderByDescending( x => x.MinBarometer ).ToList();
            Top10List[ (int) Top10Types.lowPressure ].Reverse();
            Top10List[ (int) Top10Types.lowPressure ] = Top10List[ (int) Top10Types.lowPressure ].Take( 10 ).ToList();

            //HighWind
            Top10List[ (int) Top10Types.highWind ] = ThisList.OrderByDescending( x => x.HighAverageWindSpeed ).Take( 10 ).ToList();

            //HighGust
            Top10List[ (int) Top10Types.highGust ] = ThisList.OrderByDescending( x => x.HighWindGust ).Take( 10 ).ToList();

            //TotalWindRun
            Top10List[ (int) Top10Types.totalWindrun ] = ThisList.OrderByDescending( x => x.TotalWindRun ).Take( 10 ).ToList();

            //HighRainRate
            Top10List[ (int) Top10Types.highRainRate ] = ThisList.OrderByDescending( x => x.MaxRainRate ).Take( 10 ).ToList();

            //HighHourlyRain
            Top10List[ (int) Top10Types.highHourlyRain ] = ThisList.OrderByDescending( x => x.HighHourlyRain ).Take( 10 ).ToList();

            //HighDailyRain
            Top10List[ (int) Top10Types.highDailyRain ] = ThisList.OrderByDescending( x => x.TotalRainThisDay ).Take( 10 ).ToList();

            int i, j;

            // Still looking for a nice LINQ query to solve the last three top10 lists
            //
            ThisList.Sort( new compareHighestMonthlyRain() );

            Top10List[ (int) Top10Types.highestMonthlyRain ].Add( ThisList[ 0 ] );

            foreach ( DayfileValue element in ThisList )
            {
                bool FoundInTop10 = false;

                for ( j = 0; j < Top10List[ (int) Top10Types.highestMonthlyRain ].Count; j++ )
                {
                    if ( ( element.ThisDate.Year == Top10List[ (int) Top10Types.highestMonthlyRain ][ j ].ThisDate.Year ) &&
                        ( element.ThisDate.Month == Top10List[ (int) Top10Types.highestMonthlyRain ][ j ].ThisDate.Month ) )
                    {
                        FoundInTop10 = true;
                    }
                }

                if ( FoundInTop10 )
                    continue;
                else
                {
                    Top10List[ (int) Top10Types.highestMonthlyRain ].Add( element );
                    if ( Top10List[ (int) Top10Types.highestMonthlyRain ].Count == 10 )
                        break;
                }
            }

            MonthlyRainNrOfMonths = Top10List[ (int) Top10Types.highestMonthlyRain ].Count;

            if ( Top10List[ (int) Top10Types.highestMonthlyRain ].Count < 10 )
            {
                for ( j = Top10List[ (int) Top10Types.highestMonthlyRain ].Count; j < 10; j++ )
                {
                    Top10List[ (int) Top10Types.highestMonthlyRain ].Add( ThisList[ Top10List[ (int) Top10Types.highestMonthlyRain ].Count - 1 ] );
                }
            }

            ThisList.Sort( new compareLongestDryPeriod() );

            Top10List[ (int) Top10Types.longestDryPeriod ].Add( ThisList[ 0 ] );

            foreach ( DayfileValue element in ThisList )
            {
                bool FoundInTop10 = false;

                for ( j = 0; j < Top10List[ (int) Top10Types.longestDryPeriod ].Count; j++ )
                {
                    DateTime tmpDate = Top10List[ (int) Top10Types.longestDryPeriod ][ j ].ThisDate;

                    if ( Math.Abs( element.ThisDate.Subtract( tmpDate ).TotalDays ) < Top10List[ (int) Top10Types.longestDryPeriod ][ j ].DryPeriod )
                    {
                        FoundInTop10 = true;
                    }
                }

                if ( FoundInTop10 )
                    continue;
                else
                {
                    Top10List[ (int) Top10Types.longestDryPeriod ].Add( element );
                    if ( Top10List[ (int) Top10Types.longestDryPeriod ].Count == 10 )
                        break;
                }
            }

            ThisList.Sort( new compareLongestWetPeriod() );
            Top10List[ (int) Top10Types.longestWetPeriod ].Add( ThisList[ 0 ] );

            foreach ( DayfileValue element in ThisList )
            {
                bool FoundInTop10 = false;

                for ( j = 0; j < Top10List[ (int) Top10Types.longestWetPeriod ].Count; j++ )
                {
                    DateTime tmpDate = Top10List[ (int) Top10Types.longestWetPeriod ][ j ].ThisDate;

                    if ( Math.Abs( element.ThisDate.Subtract( tmpDate ).TotalDays ) < Top10List[ (int) Top10Types.longestWetPeriod ][ j ].WetPeriod )
                    {
                        FoundInTop10 = true;
                    }
                }

                if ( FoundInTop10 )
                    continue;
                else
                {
                    Top10List[ (int) Top10Types.longestWetPeriod ].Add( element );
                    if ( Top10List[ (int) Top10Types.longestWetPeriod ].Count == 10 )
                        break;
                }
            }

            // Now, do some DEBUG printing
            DateTime Yesterday = DateTime.Today.AddDays( -1 );

            i = 0;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeMaxTemp:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].MaxTemp:F2}" );

                if ( Top10List[ i ][ j ].TimeMaxTemp.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeMinTemp:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].MinTemp:F2}" );
                if ( Top10List[ i ][ j ].TimeMinTemp.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeLowHumidity:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].LowHumidity:F2}" );
                if ( Top10List[ i ][ j ].TimeLowHumidity.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeMaxBarometer:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].MaxBarometer:F2}" );

                if ( Top10List[ i ][ j ].TimeMaxBarometer.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeMinBarometer:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].MinBarometer:F2}" );

                if ( Top10List[ i ][ j ].TimeMinBarometer.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeHighAverageWindSpeed:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].HighAverageWindSpeed:F2}" );

                if ( Top10List[ i ][ j ].TimeHighAverageWindSpeed.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeHighWindGust:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].HighWindGust:F2}" );

                if ( Top10List[ i ][ j ].TimeHighWindGust.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].ThisDate:dd/MM/yyyy} {Top10List[ i ][ j ].TotalWindRun:F2}" );

                if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < 10; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeMaxRainRate:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].MaxRainRate:F2}" );

                if ( Top10List[ i ][ j ].TimeMaxRainRate.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < Top10List[ i ].Count; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].TimeHighHourlyRain:dd/MM/yyyy HH:mm} {Top10List[ i ][ j ].HighHourlyRain:F2}" );
                if ( Top10List[ i ][ j ].TimeHighHourlyRain.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < Top10List[ i ].Count; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].ThisDate:dd/MM/yyyy} {Top10List[ i ][ j ].TotalRainThisDay:F2}" );

                if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < Top10List[ i ].Count; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].ThisDate:MM/yyyy} {Top10List[ i ][ j ].MonthlyRain:F2}" );

                if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < Top10List[ i ].Count; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].ThisDate:dd/MM/yyyy} {Top10List[ i ][ j ].DryPeriod:F2}" );

                if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            i++;
            Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
            for ( j = 0; j < Top10List[ i ].Count; j++ )
            {
                Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                  $"{Top10List[ i ][ j ].ThisDate:dd/MM/yyyy} {Top10List[ i ][ j ].WetPeriod:F2}" );

                if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                {
                    CMXutils.ThriftyTop10RecordsDirty = true;
                    Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CMXutils.ThriftyTop10RecordsDirty {CMXutils.ThriftyTop10RecordsDirty} detected." );
                }
            }

            // If the cycle is true then set records dirty so it is always uploaded; required to release accented records when 30 day period has  passed
            if ( CMXutils.RunStarted.DayOfYear % CMXutils.ThriftyTop10RecordsPeriod == 0 )
                CMXutils.ThriftyTop10RecordsDirty = true;

            if ( !CMXutils.Thrifty || CMXutils.ThriftyTop10RecordsDirty )
                HTMLexportTop10();
            Sup.LogTraceVerboseMessage( $"Thrifty: !Thrifty || ThriftyTop10RecordsDirty - {!CMXutils.Thrifty || CMXutils.ThriftyTop10RecordsDirty} => Top10 , NO HTML generated!" );

            return; //all done
        }

        private void HTMLexportTop10()
        {
            int i, j, k;
            int NrOfRecordTypes;
            string buf, timebuf;

            const int AttentionPeriod = 30;

            Sup.LogDebugMessage( "HTMLexportTop10 : starting Style" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.Top10OutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );
                of.WriteLine( "#report .CUtable{" );
                of.WriteLine( "   border-collapse: collapse;" );
                of.WriteLine( "   border-spacing: 0;" );
                of.WriteLine( "   text-align: center;" );
                of.WriteLine( "   width:100%;" );
                of.WriteLine( "   max-width:1000px;" );
                of.WriteLine( "   margin: auto;" );
                of.WriteLine( "}" );
                of.WriteLine( "#report th{" );
                of.WriteLine( "   border: 1px solid #b0b0b0;" );
                of.WriteLine( "   text-align: center;" );
                of.WriteLine( $"   background-color: {Top10TableFormat.BgcolorHeader};" );
                of.WriteLine( "   padding: 4px;" );
                of.WriteLine( "}" );
                of.WriteLine( "#report td{" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "  text-align: center;" );
                of.WriteLine( $"  background-color: {Top10TableFormat.BgcolorTable};" );
                of.WriteLine( "  padding: 4px;" );
                of.WriteLine( "}" );
                of.WriteLine( "</style>" );
                of.WriteLine( "<div id=\"report\">" );
                of.WriteLine( "<br/>" );

                // Now do the table

                NrOfRecordTypes = Enum.GetNames( typeof( Top10Types ) ).Length;
                for ( i = 0; i < NrOfRecordTypes; i += Top10TableFormat.NrOfColumns )
                {
                    of.WriteLine( "<table class=\"CUtable\">\n<thead>" );
                    of.WriteLine( "<tr>" );

                    for ( k = 0; k < Top10TableFormat.NrOfColumns; k++ )
                    {
                        if ( i + k >= NrOfRecordTypes )
                            break;
                        of.WriteLine( "<th style=\"color:{0};\">{1}</th>", Top10TableFormat.TxtcolorHeader, TypesHeaders[ i + k ] ); //enumNames[i + k]
                    }

                    of.WriteLine( "</tr>\n</thead>" );
                    of.WriteLine( "<tbody>" );

                    for ( j = 0; j < 10; j++ )
                    {
                        of.WriteLine( "<tr>" );

                        for ( k = 0; k < Top10TableFormat.NrOfColumns; k++ )
                        {
                            if ( i + k >= NrOfRecordTypes )
                                break;
                            if ( j >= Top10List[ i + k ].Count )
                                break;
                            if ( Math.Abs( Top10List[ i + k ][ j ].ThisDate.Subtract( DateTime.Now ).Days ) < AttentionPeriod )
                            {
                                /*
                                 * Then the record is less than 30 days old,
                                 * so give it a color: \"style="color:DarkOrange;\"
                                */
                                Sup.LogTraceVerboseMessage( $"ExportHTMLfragment -> highlight values - {enumNames[ i + k ]}" );
                                Sup.LogTraceVerboseMessage( $"ExportHTMLfragment -> Mainlist: " +
                                  $"{Top10List[ i + k ][ j ].ThisDate:dd/MM/yyyy HH:mm} nu:{DateTime.Now:dd/MM/yyyy HH:mm}" );

                                buf = string.Format( $"style=\"color:{Top10TableFormat.TxtAccentTable};\"" );
                            }
                            else
                                buf = "";

                            switch ( i + k )
                            {
                                case (int) Top10Types.maxTemp:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeMaxTemp );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].MaxTemp );
                                    break;

                                case (int) Top10Types.minTemp:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeMinTemp );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].MinTemp );
                                    break;

                                case (int) Top10Types.minHumidity:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeLowHumidity );
                                    of.WriteLine( "<td {0}>{1} : <b>{2,3:D}</b></td>", buf, timebuf, (int) Top10List[ i + k ][ j ].LowHumidity );
                                    break;

                                case (int) Top10Types.highPressure:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeMaxBarometer );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].MaxBarometer );
                                    break;

                                case (int) Top10Types.lowPressure:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeMinBarometer );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].MinBarometer );
                                    break;

                                case (int) Top10Types.highWind:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeHighAverageWindSpeed );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].HighAverageWindSpeed );
                                    break;

                                case (int) Top10Types.highGust:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeHighWindGust );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].HighWindGust );
                                    break;

                                case (int) Top10Types.totalWindrun:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy}", Top10List[ i + k ][ j ].ThisDate );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].TotalWindRun );
                                    break;

                                case (int) Top10Types.highRainRate:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeMaxRainRate );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].MaxRainRate );
                                    break;

                                case (int) Top10Types.highHourlyRain:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy HH:mm}", Top10List[ i + k ][ j ].TimeHighHourlyRain );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].HighHourlyRain );
                                    break;

                                case (int) Top10Types.highDailyRain:
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy}", Top10List[ i + k ][ j ].ThisDate );
                                    of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].TotalRainThisDay );
                                    break;

                                case (int) Top10Types.highestMonthlyRain:
                                    if ( j < MonthlyRainNrOfMonths )
                                    {
                                        timebuf = String.Format( CultureInfo.InvariantCulture, "{0:MMM yyyy}", Top10List[ i + k ][ j ].ThisDate );
                                        of.WriteLine( "<td {0}>{1} : <b>{2:F1}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].MonthlyRain );
                                    }
                                    else
                                    {
                                        // do nothing, no line to output (Only for systems younger than 10 months)
                                        of.WriteLine( "<td></td>" );
                                    }
                                    break;

                                case (int) Top10Types.longestDryPeriod:
                                    //if ( Top10List[ i + k ] == null ) break;
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy}", Top10List[ i + k ][ j ].ThisDate );
                                    of.WriteLine( "<td {0}>{1} : <b>{2,3:D}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].DryPeriod );
                                    break;

                                case (int) Top10Types.longestWetPeriod:
                                    //if ( Top10List[ i + k ] == null ) break;
                                    timebuf = String.Format( CultureInfo.InvariantCulture, "{0:dd/MM/yyyy}", Top10List[ i + k ][ j ].ThisDate );
                                    of.WriteLine( "<td {0}>{1} : <b>{2,3:D}</b></td>", buf, timebuf, Top10List[ i + k ][ j ].WetPeriod );
                                    break;

                                default:
                                    Sup.LogTraceErrorMessage( "ExportHTMLfragment -> At Default of Switch " );
                                    Sup.LogTraceErrorMessage( $"ExportHTMLfragment -> At Default of Switch. i+k={i + k}" );
                                    Environment.Exit( 0 );
                                    break;
                            }
                        }
                        of.WriteLine( "</tr>" );
                    }
                    of.WriteLine( "</tbody></table><br/>" );
                }

                of.WriteLine( $"<p>{Sup.GetCUstringValue( "Records", "RecordsSince", "Records registered since", false )} {CMXutils.StartOfObservations.Date:dd MMMM yyyy} - " +
                             $"({( CMXutils.RunStarted.Date - CMXutils.StartOfObservations.Date ).TotalDays} {Sup.GetCUstringValue( "General", "Days", "Days", false )})</p>" );


                if ( !CMXutils.DoWebsite )
                {
                    of.WriteLine( $"<p style ='text-align: center; font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // (id=report)
            } // End using of (output file)
        }

        #region IDisposable

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Top10()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( false );
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( true );
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable
    }
}