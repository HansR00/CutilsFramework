﻿/*
 * Top10 - Part of CumulusUtils
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

    public class Top10 : IDisposable
    {
        private enum Top10Types
        {
            maxTemp, minTemp, minHumidity, highPressure, lowPressure, highWind, highGust, totalWindrun, highRainRate, highHourlyRain, highDailyRain,
            highestMonthlyRain, lowestMonthlyRain, longestDryPeriod, longestWetPeriod
        };

        private readonly string[] enumNames;
        private readonly List<string> TypesUnits;
        private readonly List<string> TypesHeaders;
        private readonly List<DayfileValue>[] Top10List;

        private Top10Format Top10TableFormat;
        private int MonthlyRainNrOfMonths; // to memorise the nr of months actually in top10 ist. May be <10 for a young system

        private readonly CuSupport Sup;


        // Constructor
        //[System.Diagnostics.CodeAnalysis.SuppressMessage( "Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>" )]
        public Top10( CuSupport s )
        {
            int i;

            Sup = s;
            Sup.LogDebugMessage( "Top10funcs: Initialising Top10 Array" );

            Top10List = new List<DayfileValue>[ Enum.GetNames( typeof( Top10Types ) ).Length ];
            Top10TableFormat = new Top10Format
            {
                NrOfColumns = Convert.ToInt32( Sup.GetUtilsIniValue( "Top10", "NumberOfColumns", "3" ), CUtils.Inv ),
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
            TypesUnits[ (int) Top10Types.highRainRate ] = Sup.StationRain.Text() + Sup.PerHour;
            TypesUnits[ (int) Top10Types.highHourlyRain ] = Sup.StationRain.Text();
            TypesUnits[ (int) Top10Types.highDailyRain ] = Sup.StationRain.Text();
            TypesUnits[ (int) Top10Types.highestMonthlyRain ] = Sup.StationRain.Text();
            TypesUnits[ (int) Top10Types.lowestMonthlyRain ] = Sup.StationRain.Text();
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
            TypesHeaders[ (int) Top10Types.lowestMonthlyRain ] = Sup.GetCUstringValue( "Top10", "LowestMonthlyRain", "lowestMonthlyRain", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.lowestMonthlyRain ]})";
            TypesHeaders[ (int) Top10Types.longestDryPeriod ] = Sup.GetCUstringValue( "Top10", "LongestDryPeriod", "longestDryPeriod", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.longestDryPeriod ]})";
            TypesHeaders[ (int) Top10Types.longestWetPeriod ] = Sup.GetCUstringValue( "Top10", "LongestWetPeriod", "longestWetPeriod", false ) +
                                                      $"<br/>({TypesUnits[ (int) Top10Types.longestWetPeriod ]})";

            for ( i = 0; i < Enum.GetNames( typeof( Top10Types ) ).Length; i++ )
                Top10List[ i ] = new List<DayfileValue>();
        }

        public void GenerateTop10List( List<DayfileValue> ThisList )
        {
            Sup.LogDebugMessage( "Top10funcs: Generating Top10 lists" );

            //Do the Dry/WetPeriods before re-sorting the list for other purposes
            List<DayfileValue> DryPeriodList = new List<DayfileValue>();
            List<DayfileValue> WetPeriodList = new List<DayfileValue>();
            DayfileValue previous = ThisList[ 0 ];

            bool DryPeriodActive = false, WetPeriodActive = false;

            foreach ( DayfileValue element in ThisList )
            {
                int year = element.ThisDate.Year;

                if ( element.DryPeriod != 0 )
                {
                    DryPeriodActive = true;

                    if ( WetPeriodActive ) WetPeriodList.Add( previous );
                    WetPeriodActive = false;
                }

                if ( element.WetPeriod != 0 )
                {
                    WetPeriodActive = true;

                    if ( DryPeriodActive ) DryPeriodList.Add( previous );
                    DryPeriodActive = false;
                }

                previous = element;
            }

            // Make sure the current period is added to the list
            if ( DryPeriodActive ) { DryPeriodList.Add( previous ); }
            else { WetPeriodList.Add( previous ); }

            Top10List[ (int) Top10Types.longestDryPeriod ] = DryPeriodList.OrderByDescending(x => x.ThisDate).OrderByDescending( x => x.DryPeriod ).Take( 10 ).ToList();

            Top10List[ (int) Top10Types.longestWetPeriod ] = WetPeriodList.OrderByDescending( x => x.ThisDate ).OrderByDescending( x => x.WetPeriod ).Take( 10 ).ToList();

            //MaxTemp
            Top10List[ (int) Top10Types.maxTemp ] = ThisList.OrderByDescending( x => x.MaxTemp ).Take( 10 ).ToList();

            //MinTemp
            Top10List[ (int) Top10Types.minTemp ] = ThisList.OrderBy( x => x.MinTemp ).Take( 10 ).ToList();

            //LowHumidity
            Top10List[ (int) Top10Types.minHumidity ] = ThisList.OrderBy( x => x.LowHumidity ).Take( 10 ).ToList();

            //HighPressure
            Top10List[ (int) Top10Types.highPressure ] = ThisList.OrderByDescending( x => x.MaxBarometer ).Take( 10 ).ToList();

            //LowPressure
            Top10List[ (int) Top10Types.lowPressure ] = ThisList.OrderBy( x => x.MinBarometer ).Take( 10 ).ToList();

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

            //HighMonthlyRain
            List<DayfileValue> tmpList = new();

            for ( int thisYear = ThisList.Select( x => x.ThisDate.Year ).Min(); thisYear <= ThisList.Select( x => x.ThisDate.Year ).Max(); thisYear++ )
                for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                    try
                    {
                        tmpList.Add( ThisList.Where( x => x.ThisDate.Year == thisYear ).Where( x => x.ThisDate.Month == thisMonth ).OrderByDescending( x => x.MonthlyRain ).First() );
                    }
                    catch ( Exception ) { continue; }

            Top10List[ (int) Top10Types.highestMonthlyRain ] = tmpList.OrderByDescending( x => x.MonthlyRain ).Take( 10 ).ToList();

            Top10List[ (int) Top10Types.lowestMonthlyRain ] = tmpList.OrderBy( x => x.MonthlyRain ).Take( 10 ).ToList();

            MonthlyRainNrOfMonths = Top10List[ (int) Top10Types.highestMonthlyRain ].Count;

            // Now, do some DEBUG printing
            DateTime Yesterday = DateTime.Today.AddDays( -1 );

            int i = 0, j;

            try
            {
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeMaxTemp:g} {Top10List[ i ][ j ].MaxTemp:F2}" );

                    if ( Top10List[ i ][ j ].TimeMaxTemp.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeMinTemp:g} {Top10List[ i ][ j ].MinTemp:F2}" );

                    if ( Top10List[ i ][ j ].TimeMinTemp.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeLowHumidity:g} {Top10List[ i ][ j ].LowHumidity:F2}" );

                    if ( Top10List[ i ][ j ].TimeLowHumidity.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeMaxBarometer:g} {Top10List[ i ][ j ].MaxBarometer:F2}" );

                    if ( Top10List[ i ][ j ].TimeMaxBarometer.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeMinBarometer:g} {Top10List[ i ][ j ].MinBarometer:F2}" );

                    if ( Top10List[ i ][ j ].TimeMinBarometer.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeHighAverageWindSpeed:g} {Top10List[ i ][ j ].HighAverageWindSpeed:F2}" );

                    if ( Top10List[ i ][ j ].TimeHighAverageWindSpeed.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeHighWindGust:g} {Top10List[ i ][ j ].HighWindGust:F2}" );

                    if ( Top10List[ i ][ j ].TimeHighWindGust.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].ThisDate:d} {Top10List[ i ][ j ].TotalWindRun:F2}" );

                    if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < 10; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeMaxRainRate:g} {Top10List[ i ][ j ].MaxRainRate:F2}" );

                    if ( Top10List[ i ][ j ].TimeMaxRainRate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < Top10List[ i ].Count; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].TimeHighHourlyRain:g} {Top10List[ i ][ j ].HighHourlyRain:F2}" );

                    if ( Top10List[ i ][ j ].TimeHighHourlyRain.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < Top10List[ i ].Count; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].ThisDate:d} {Top10List[ i ][ j ].TotalRainThisDay:F2}" );

                    if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < Top10List[ i ].Count; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].ThisDate:MMM yyyy} {Top10List[ i ][ j ].MonthlyRain:F2}" );

                    if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < Top10List[ i ].Count; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].ThisDate:MMM yyyy} {Top10List[ i ][ j ].MonthlyRain:F2}" );  // Lowest monthly rain, inverted from previous list

                    if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < Top10List[ i ].Count; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].ThisDate:d} {Top10List[ i ][ j ].DryPeriod:F2}" );

                    if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                i++;
                Sup.LogTraceVerboseMessage( "GenerateTop10List:" + enumNames[ i ] );
                for ( j = 0; j < Top10List[ i ].Count; j++ )
                {
                    Sup.LogTraceVerboseMessage( $"GenerateTop10List:\t\t " +
                        $"{Top10List[ i ][ j ].ThisDate:d} {Top10List[ i ][ j ].WetPeriod:F2}" );

                    if ( Top10List[ i ][ j ].ThisDate.Date == Yesterday.Date )
                    {
                        CUtils.ThriftyTop10RecordsDirty = true;
                        Sup.LogTraceVerboseMessage( $"Generate Top10 Records: CUtils.ThriftyTop10RecordsDirty {CUtils.ThriftyTop10RecordsDirty} detected." );
                    }
                }

                // If the cycle is true then set records dirty so it is always uploaded; required to release accented records when 30 day period has  passed
                //if ( CUtils.RunStarted.DayOfYear % CUtils.ThriftyTop10RecordsPeriod == 0 ) CUtils.ThriftyTop10RecordsDirty = true;
                CUtils.ThriftyTop10RecordsDirty = true;

                Sup.LogTraceInfoMessage( "Top10funcs: Starting the HTML Generation" );
                if ( !CUtils.Thrifty || CUtils.ThriftyTop10RecordsDirty ) HTMLexportTop10();

                //Sup.LogTraceInfoMessage( $"Thrifty: !Thrifty || ThriftyTop10RecordsDirty - {!CUtils.Thrifty || CUtils.ThriftyTop10RecordsDirty} => Top10 , NO HTML generated!" );

                Sup.LogTraceInfoMessage( "Top10funcs: Completed, returning from generating the Top10 list" );
            }
            catch ( Exception e ) // Not yet 32 days
            {
                using ( StreamWriter of = new( $"{Sup.PathUtils}{Sup.Top10OutputFilename}", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( "GenerateTop10List: Not yet enough days, skipping module." );
                }

                Sup.LogDebugMessage( "GenerateTop10List: Not yet enough days, skipping module." );
                Sup.LogTraceInfoMessage( $"GenerateTop10List: {e.Message}." );
                return;
            }

            return; //all done
        }

        private void HTMLexportTop10()
        {
            int i, j, k;
            int NrOfRecordTypes;
            string buf, timebuf;

            const int AttentionPeriod = 30;

            Sup.LogDebugMessage( "HTMLexportTop10" );

            using ( StreamWriter of = new( $"{Sup.PathUtils}{Sup.Top10OutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );

                if ( Sup.GetUtilsIniValue( "General", "UseScrollableTables", "true" ).Equals( "true", CUtils.Cmp ) )
                {
                    of.WriteLine( "  height: 80vh; overflow: auto;" );
                }

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
                of.WriteLine( "<div id=\"reportBox\">" );
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
                        if ( i + k >= NrOfRecordTypes ) break;
                        of.WriteLine( $"<th style=\"color:{Top10TableFormat.TxtcolorHeader};\">{TypesHeaders[ i + k ]}</th>" ); //enumNames[i + k]
                    }

                    of.WriteLine( "</tr>\n</thead>" );
                    of.WriteLine( "<tbody>" );

                    for ( j = 0; j < 10; j++ )
                    {
                        of.WriteLine( "<tr>" );

                        for ( k = 0; k < Top10TableFormat.NrOfColumns; k++ )
                        {
                            if ( i + k >= NrOfRecordTypes ) break;
                            if ( j >= Top10List[ i + k ].Count ) break;
                            if ( Math.Abs( Top10List[ i + k ][ j ].ThisDate.Subtract( DateTime.Now ).Days ) < AttentionPeriod )
                            {
                                /*
                                 * Then the record is less than 30 days old,
                                 * so give it a color: \"style="color:DarkOrange;\"
                                */
                                Sup.LogTraceVerboseMessage( $"ExportHTMLfragment -> highlight values - {enumNames[ i + k ]}" );
                                Sup.LogTraceVerboseMessage( $"ExportHTMLfragment -> Mainlist: " +
                                  $"{Top10List[ i + k ][ j ].ThisDate:g} nu:{DateTime.Now:g}" );

                                buf = string.Format( $"style=\"color:{Top10TableFormat.TxtAccentTable};\"" );
                            }
                            else buf = "";

                            switch ( i + k )
                            {
                                case (int) Top10Types.maxTemp:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeMaxTemp.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Temp.Format( Top10List[ i + k ][ j ].MaxTemp )}</b></td>" );
                                    break;

                                case (int) Top10Types.minTemp:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeMinTemp.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Temp.Format( Top10List[ i + k ][ j ].MinTemp )}</b></td>" );
                                    break;

                                case (int) Top10Types.minHumidity:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeLowHumidity.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{(int) Top10List[ i + k ][ j ].LowHumidity:D}</b></td>" );
                                    break;

                                case (int) Top10Types.highPressure:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeMaxBarometer.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Sup.StationPressure.Format( Top10List[ i + k ][ j ].MaxBarometer )}</b></td>" );
                                    break;

                                case (int) Top10Types.lowPressure:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeMinBarometer.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Sup.StationPressure.Format( Top10List[ i + k ][ j ].MinBarometer )}</b></td>" );
                                    break;

                                case (int) Top10Types.highWind:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeHighAverageWindSpeed.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Wind.Format( Top10List[ i + k ][ j ].HighAverageWindSpeed )}</b></td>" );
                                    break;

                                case (int) Top10Types.highGust:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeHighWindGust.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Wind.Format( Top10List[ i + k ][ j ].HighWindGust )}</b></td>" );
                                    break;

                                case (int) Top10Types.totalWindrun:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].ThisDate.ToString( "d", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Distance.Format( Top10List[ i + k ][ j ].TotalWindRun )}</b></td>" );
                                    break;

                                case (int) Top10Types.highRainRate:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeMaxRainRate.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Sup.StationRain.Format( Top10List[ i + k ][ j ].MaxRainRate )}</b></td>" );
                                    break;

                                case (int) Top10Types.highHourlyRain:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].TimeHighHourlyRain.ToString( "g", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Sup.StationRain.Format( Top10List[ i + k ][ j ].HighHourlyRain )}</b></td>" );
                                    break;

                                case (int) Top10Types.highDailyRain:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].ThisDate.ToString( "d", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Sup.StationRain.Format( Top10List[ i + k ][ j ].TotalRainThisDay )}</b></td>" );
                                    break;

                                case (int) Top10Types.highestMonthlyRain:
                                case (int) Top10Types.lowestMonthlyRain:
                                    if ( j < MonthlyRainNrOfMonths )
                                    {
                                        timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].ThisDate.ToString( "MMM yyyy", CUtils.ThisCulture )}" );
                                        of.WriteLine( $"<td {buf}>{timebuf} : <b>{Sup.StationRain.Format( Top10List[ i + k ][ j ].MonthlyRain )}</b></td>" );
                                    }
                                    else
                                    {
                                        // do nothing, no line to output (Only for systems younger than 10 months)
                                        of.WriteLine( "<td></td>" );
                                    }
                                    break;

                                case (int) Top10Types.longestDryPeriod:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].ThisDate.ToString( "d", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Top10List[ i + k ][ j ].DryPeriod:D}</b></td>" );
                                    break;

                                case (int) Top10Types.longestWetPeriod:
                                    timebuf = string.Format( CUtils.Inv, $"{Top10List[ i + k ][ j ].ThisDate.ToString( "d", CUtils.ThisCulture )}" );
                                    of.WriteLine( $"<td {buf}>{timebuf} : <b>{Top10List[ i + k ][ j ].WetPeriod:D}</b></td>" );
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

                of.WriteLine( $"<p>{Sup.GetCUstringValue( "Records", "RecordsSince", "Records registered since", false )} {CUtils.StartOfObservations.ToString( "D", CUtils.ThisCulture )} - " +
                             $"({( CUtils.RunStarted.Date - CUtils.StartOfObservations.Date ).TotalDays} {Sup.GetCUstringValue( "General", "Days", "Days", false )})</p>" );

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style ='text-align: center; font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // (id=report)
                of.WriteLine( "</div>" ); // (id=reportBox)

                Sup.LogTraceInfoMessage( $"HTMLexportTop10 : Ready generating HTML" );

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
