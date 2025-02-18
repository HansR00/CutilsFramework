﻿/*
 * GraphSolar - Part of CumulusUtils
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
 *  https://en.wikipedia.org/wiki/Solar_irradiance
 *  https://en.wikipedia.org/wiki/Watt
 *  https://www.3tier.com/en/support/solar-online-tools/what-are-units-solar-irradiance/  On the units Vaisala site
 * 
 * Vaisala provides various solar products for numerous purposes. The different physical quantities represented and the units used in these products can be somewhat confusing.
 * 
 * You can change the units displayed in the Solar Prospecting Tools from W/m² to kWh/m²/day under the "Account" menu, located at the upper right of the screen. In that menu 
 * under "Preferences" you can change the unit type displayed.
 * 
 * Irradiance is a measurement of solar power and is defined as the rate at which solar energy falls onto a surface. The unit of power is the Watt (abbreviated W). 
 * In the case of solar irradiance, we usually measure the power per unit area, so irradiance is typically quoted as W/m², that is, Watts per square meter. The irradiance falling 
 * on a surface can and does vary from moment to moment, which is why it is important to remember that irradiance is a measure of power - the rate that energy is received, 
 * not the total amount of energy.
 * 
 * The total amount of solar energy that falls over a given time is called the insolation. Insolation is a measure of energy. It is the power from the sun added up over some time period.
 * 
 * Now here comes the confusing part. If the sun shines at a constant 1000 W/m² for one hour, we say it has delivered 1 kWh/m² of energy. The amount of power is the product of 
 * the power (1000 W/m²) times the length of time (1 hour), so that the unit of energy is the kWh. Insolation (measured in kWh) is not the same as power (measured in kW) in the same way 
 * that miles per hour is not the same as miles.
 * 
 * Another commonly used term is “peak sun hours,” which reflects the energy received during total daylight hours as defined by the equivalent number of hours it would take to reach 
 * that total energy value had solar irradiance averaged 1000 W/m². Although "peak sun hours" has the unit of hours, because of the assumptions behind its definition, the value is 
 * interchangeable with kWh/m²/day.
 *              
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#if TIMING
using System.Diagnostics;
#endif

namespace CumulusUtils
{
    partial class Graphx
    {
        #region declaration

        // PossibleIntervals are the loggingintervals in minutes with the possibilities as given in the Station Settings of CMX
        //readonly int[] PossibleIntervals = { 1, 5, 10, 15, 20, 30 };

        public struct DaySolarValues
        {
            public int SunUpTimeInMinutes { get; set; }
            public float SolarHours { get; set; }
            public float SolarEnergy { get; set; }
            public DateTime ThisDate { get; set; }
        }

        readonly List<DaySolarValues> DailySolarValuesList = new List<DaySolarValues>();
        private float minminSH = 1000, maxmaxSH;
        private float minminSE = 1000, maxmaxSE;

        #endregion

        #region SolarHours
        void GenerateYearSolarHoursStatistics( StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> minSolarHours = new List<float>();
            List<float> maxSolarHours = new List<float>();

            Sup.LogDebugMessage( "GenerateYearSolarHoursStatistics: Starting" );

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                List<DaySolarValues> yearlist = DailySolarValuesList.Where( x => x.ThisDate.Year == i ).ToList();

                Sup.LogTraceInfoMessage( $"Generating Year Solar Hours Statistics, doing year {i}" );

                if ( yearlist.Any() )
                {
                    years.Add( i );
                    average.Add( yearlist.Select( x => x.SolarHours ).Average() );
                    stddev.Add( yearlist.Select( x => x.SolarHours ).StdDev() );
                    minSolarHours.Add( yearlist.Select( x => x.SolarHours ).Min() );
                    maxSolarHours.Add( yearlist.Select( x => x.SolarHours ).Max() );

                    minminSH = Math.Min( minSolarHours.Last(), minminSH );
                    maxmaxSH = Math.Max( maxSolarHours.Last(), maxmaxSH );
                }
                else
                {
                    years.Add( i );
                    average.Add( 0 );
                    stddev.Add( 0 );
                    minSolarHours.Add( 0 );
                    maxSolarHours.Add( 0 );

                    minminSH = Math.Min( 0, minminSH );
                    maxmaxSH = Math.Max( 0, maxmaxSH );
                }
            }

            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YSHSTitle", "Solar Hours Statistics per year", true )}' " );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "subtitle:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "xAxis:" );
            thisBuffer.AppendLine( "{" );

            sb.Clear();
            sb.Append( "categories: [" );
            foreach ( int year in years )
            {
                sb.Append( $"'{year:####}'," );
            }
            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( $"  {sb}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "yAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YSHSYaxis", "Solar Hours per day", true )} ({Sup.GetCUstringValue( "General", "Hours", "Hours", true )})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: {minminSH.ToString( "F2", CUtils.Inv )}," );
            thisBuffer.AppendLine( $"  max: {maxmaxSH.ToString( "F2", CUtils.Inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: ' {Sup.GetCUstringValue( "General", "Hours", "Hours", true )}'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "plotOptions:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  dataLabels: {enabled: false}," );
            thisBuffer.AppendLine( "  columnrange:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    grouping: false," );   // Exactly overlap
            thisBuffer.AppendLine( "    dataLabels:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      enabled: false," );
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.GetCUstringValue( "General", "Hours", "Hours", true )}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YSHSSolarHours", "Solar Hours", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(204, 209, 209, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba( 255, 235, 59, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{minSolarHours[ i ].ToString( "F2", CUtils.Inv )},{maxSolarHours[ i ].ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YSHSAverageSolarHours", "Average Solar Hours", true )}'," );
            thisBuffer.AppendLine( "zIndex: 2," );
            thisBuffer.AppendLine( "  type: 'spline'," );
            thisBuffer.AppendLine( "  lineWidth: 1," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{average[ i ].ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "StdDev", "Standard Deviation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 1," );
            thisBuffer.AppendLine( "color: 'lightgrey'," );
            thisBuffer.AppendLine( "  pointWidth: 18," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
            {
                float AvMinusStddev = average[ i ] - stddev[ i ];

                sb.Append( $"[{( AvMinusStddev < 0.0 ? 0.ToString( "F2", CUtils.Inv ) : AvMinusStddev.ToString( "F2", CUtils.Inv ) )}," +
                    $"{( average[ i ] + stddev[ i ] ).ToString( "F2", CUtils.Inv )}]," );
            }

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );

            return;
        }

        void GenerateYearMonthSolarHoursStatistics( Months thisMonth, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();
            ;

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> minSolarHours = new List<float>();
            List<float> maxSolarHours = new List<float>();

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                List<DaySolarValues> yearMonthlist = DailySolarValuesList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == (int) thisMonth ).ToList();

                //Sup.LogTraceInfoMessage( $"Generating Year Month Solar Hours Statistics, doing year {i} and month {thisMonth}" );

                if ( yearMonthlist.Any() )
                {
                    years.Add( i );
                    average.Add( yearMonthlist.Select( x => x.SolarHours ).Average() );
                    stddev.Add( yearMonthlist.Select( x => x.SolarHours ).StdDev() );
                    minSolarHours.Add( yearMonthlist.Select( x => x.SolarHours ).Min() );
                    maxSolarHours.Add( yearMonthlist.Select( x => x.SolarHours ).Max() );

                    minminSH = Math.Min( minSolarHours.Last(), minminSH );
                    maxmaxSH = Math.Max( maxSolarHours.Last(), maxmaxSH );
                }
            }

            if ( years.Count == 0 )
            {
                thisBuffer.AppendLine( "console.log('Not enough data - choose another month');" );
                thisBuffer.AppendLine( "window.alert('Not enough data - choose another month');" );
                return; // We're done, nothing here
            }

            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YMSHSTitle", "Yearly Solar Hours Statistics per month for", true )} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( (int) thisMonth )}' " );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "subtitle:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "xAxis:" );
            thisBuffer.AppendLine( "{" );

            sb.Clear();
            sb.Append( "categories: [" );

            foreach ( int year in years )
                sb.Append( $"'{year:####}'," );

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( $"  {sb}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "yAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YSHSYaxis", "Solar Hours per day", true )} ({Sup.GetCUstringValue( "General", "Hours", "Hours", true )})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: {minminSH.ToString( "F2", CUtils.Inv )}," );
            thisBuffer.AppendLine( $"  max: {maxmaxSH.ToString( "F2", CUtils.Inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: ' {Sup.GetCUstringValue( "General", "Hours", "Hours", true )}'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "plotOptions:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  dataLabels: {enabled: false}," );
            thisBuffer.AppendLine( "  columnrange:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    grouping: false," );   // Exactly overlap
            thisBuffer.AppendLine( "    dataLabels:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      enabled: false," );
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.GetCUstringValue( "General", "Hours", "Hours", true )}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YMSHSSolarHoursRange", "Solar Hours Range", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(204, 209, 209, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba( 255, 235, 59, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
                sb.Append( $"[{minSolarHours[ i ].ToString( "F2", CUtils.Inv )},{maxSolarHours[ i ].ToString( "F2", CUtils.Inv )}]," );

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YMSHSAverageSolarHours", "Average Solar Hours", true )}'," );
            thisBuffer.AppendLine( "zIndex: 2," );
            thisBuffer.AppendLine( "  type: 'spline'," );
            thisBuffer.AppendLine( "  lineWidth: 1," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
                sb.Append( $"[{average[ i ].ToString( "F2", CUtils.Inv )}]," );

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "StdDev", "Standard Deviation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 1," );
            thisBuffer.AppendLine( "color: 'lightgrey'," );
            thisBuffer.AppendLine( "  pointWidth: 18," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
            {
                float AvMinusStddev = average[ i ] - stddev[ i ];

                sb.Append( $"[{( AvMinusStddev < 0.0 ? 0.ToString( "F2", CUtils.Inv ) : AvMinusStddev.ToString( "F2", CUtils.Inv ) )}," +
                    $"{( average[ i ] + stddev[ i ] ).ToString( "F2", CUtils.Inv )}]," );
            }

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );

            return;
        }

        #endregion

        #region Solar Energy
        void GenerateYearSolarEnergyStatistics( StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> minSolarEnergy = new List<float>();
            List<float> maxSolarEnergy = new List<float>();

            Sup.LogDebugMessage( "Generate GenerateYearSolarEnergyStatistics: Starting" );

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                List<DaySolarValues> yearlist = DailySolarValuesList.Where( x => x.ThisDate.Year == i ).ToList();

                Sup.LogTraceInfoMessage( $"Generating Year Solar Energy Statistics, doing year {i}" );

                if ( yearlist.Any() )
                {
                    years.Add( i );
                    average.Add( yearlist.Select( x => x.SolarEnergy ).Average() );
                    stddev.Add( yearlist.Select( x => x.SolarEnergy ).StdDev() );
                    minSolarEnergy.Add( yearlist.Select( x => x.SolarEnergy ).Min() );
                    maxSolarEnergy.Add( yearlist.Select( x => x.SolarEnergy ).Max() );

                    minminSE = Math.Min( minSolarEnergy.Last(), minminSE );
                    maxmaxSE = Math.Max( maxSolarEnergy.Last(), maxmaxSE );
                }
                else
                {
                    years.Add( i );
                    average.Add( 0 );
                    stddev.Add( 0 );
                    minSolarEnergy.Add( 0 );
                    maxSolarEnergy.Add( 0 );

                    minminSE = Math.Min( 0, minminSE );
                    maxmaxSE = Math.Max( 0, maxmaxSE );
                }
            }

            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YSESTitle", "Insolation Statistics per year", true )}' " );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "subtitle:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "xAxis:" );
            thisBuffer.AppendLine( "{" );

            sb.Clear();
            sb.Append( "categories: [" );
            foreach ( int year in years )
            {
                sb.Append( $"'{year:####}'," );
            }
            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( $"  {sb}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "yAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    useHTML: true," );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YSESYaxis", "Daily kilo Watt hour per m²", true )} ({Sup.GetCUstringValue( "General", "kWh", "kWh", true )})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: {minminSE.ToString( "F2", CUtils.Inv )}," );
            thisBuffer.AppendLine( $"  max: {maxmaxSE.ToString( "F2", CUtils.Inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: ' {Sup.GetCUstringValue( "General", "kWh", "kWh", true )}'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "plotOptions:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  dataLabels: {enabled: false}," );
            thisBuffer.AppendLine( "  columnrange:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    grouping: false," );   // Exactly overlap
            thisBuffer.AppendLine( "    dataLabels:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      enabled: false," );
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.GetCUstringValue( "General", "kWh", "kWh", true )}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YSESSolarEnergy", "Insolation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(204, 209, 209, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba(255, 59, 59, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{minSolarEnergy[ i ].ToString( "F2", CUtils.Inv )},{maxSolarEnergy[ i ].ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YSESAverageSolarEnergy", "Average Insolation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 2," );
            thisBuffer.AppendLine( "  type: 'spline'," );
            thisBuffer.AppendLine( "  lineWidth: 1," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{average[ i ].ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "StdDev", "Standard Deviation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 1," );
            thisBuffer.AppendLine( "color: 'lightgrey'," );
            thisBuffer.AppendLine( "  pointWidth: 18," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
            {
                float AvMinusStddev = average[ i ] - stddev[ i ];

                sb.Append( $"[{( AvMinusStddev < 0.0 ? 0.ToString( "F2", CUtils.Inv ) : AvMinusStddev.ToString( "F2", CUtils.Inv ) )}," +
                    $"{( average[ i ] + stddev[ i ] ).ToString( "F2", CUtils.Inv )}]," );
            }

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );

            return;
        }

        void GenerateYearMonthSolarEnergyStatistics( Months thisMonth, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> minSolarEnergy = new List<float>();
            List<float> maxSolarEnergy = new List<float>();

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                List<DaySolarValues> yearMonthlist = DailySolarValuesList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == (int) thisMonth ).ToList();

                //Sup.LogTraceInfoMessage( $"Generating Year Month Solar Energy Statistics, doing year {i} and month {thisMonth}" );

                if ( yearMonthlist.Any() )
                {
                    years.Add( i );
                    average.Add( yearMonthlist.Select( x => x.SolarEnergy ).Average() );
                    stddev.Add( yearMonthlist.Select( x => x.SolarEnergy ).StdDev() );
                    minSolarEnergy.Add( yearMonthlist.Select( x => x.SolarEnergy ).Min() );
                    maxSolarEnergy.Add( yearMonthlist.Select( x => x.SolarEnergy ).Max() );

                    minminSH = Math.Min( minSolarEnergy.Last(), minminSH );
                    maxmaxSH = Math.Max( maxSolarEnergy.Last(), maxmaxSH );
                }
            }

            if ( years.Count == 0 )
            {
                thisBuffer.AppendLine( "console.log('Not enough data - choose another month');" );
                thisBuffer.AppendLine( "window.alert('Not enough data - choose another month');" );
                return; // We're done, nothing here
            }

            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YMSESTitle", "Monthly Insolation Statistics per year for", true )} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( (int) thisMonth )}' " );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "subtitle:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "xAxis:" );
            thisBuffer.AppendLine( "{" );

            sb.Clear();
            sb.Append( "categories: [" );

            foreach ( int year in years )
                sb.Append( $"'{year:####}'," );

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( $"  {sb}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "yAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    useHTML: true," );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YSESYaxis", "Daily kilo Watt hour per m²", true )} ({Sup.GetCUstringValue( "General", "kWh", "kWh", true )})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: {minminSE.ToString( "F2", CUtils.Inv )}," );
            thisBuffer.AppendLine( $"  max: {maxmaxSE.ToString( "F2", CUtils.Inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: ' {Sup.GetCUstringValue( "General", "kWh", "kWh", true )}'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "plotOptions:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  dataLabels: {enabled: false}," );
            thisBuffer.AppendLine( "  columnrange:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    grouping: false," );   // Exactly overlap
            thisBuffer.AppendLine( "    dataLabels:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      enabled: false," );
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.GetCUstringValue( "General", "kWh", "kWh", true )}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YSESSolarEnergy", "Insolation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(204, 209, 209, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba(255, 59, 59, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
                sb.Append( $"[{minSolarEnergy[ i ].ToString( "F2", CUtils.Inv )},{maxSolarEnergy[ i ].ToString( "F2", CUtils.Inv )}]," );

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YMSESAverageSolarEnergy", "Average Insolation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 2," );
            thisBuffer.AppendLine( "  type: 'spline'," );
            thisBuffer.AppendLine( "  lineWidth: 1," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
                sb.Append( $"[{average[ i ].ToString( "F2", CUtils.Inv )}]," );

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "StdDev", "Standard Deviation", true )}'," );
            thisBuffer.AppendLine( "zIndex: 1," );
            thisBuffer.AppendLine( "color: 'lightgrey'," );
            thisBuffer.AppendLine( "  pointWidth: 18," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
            {
                float AvMinusStddev = average[ i ] - stddev[ i ];

                sb.Append( $"[{( AvMinusStddev < 0.0 ? 0.ToString( "F2", CUtils.Inv ) : AvMinusStddev.ToString( "F2", CUtils.Inv ) )}," +
                    $"{( average[ i ] + stddev[ i ] ).ToString( "F2", CUtils.Inv )}]," );
            }

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );

            return;
        }

        #endregion

        #region CreateListsFromMonthlyLogs ONCE
        void CreateListsFromMonthlyLogs( List<MonthfileValue> MonthfileList )
        {
            Sup.LogTraceInfoMessage( $"Solar Graphs - CreateListsFromMonthlyLogs start" );

#if TIMING
            Stopwatch watch = Stopwatch.StartNew();
#endif

            List<MonthfileValue> thisList = MonthfileList.Where( x => x.SolarTheoreticalMax > 0 ).ToList();

            // My question at stackoverflow:
            // https://stackoverflow.com/questions/64004453/increase-performance-of-timeinterval-calculation/64005138#64005138
            //
            var dayLookup = thisList.ToLookup( x => x.ThisDate.Date );

            DateTime startdate = MonthfileList.Select( x => x.ThisDate ).Min().Date;
            DateTime enddate = MonthfileList.Select( x => x.ThisDate ).Max().Date;

            int OldIntervalInMinutes;
            int IntervalInMinutes = CUtils.PossibleIntervals[ Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "DataLogInterval", "" ), CUtils.Inv ) ];
            float SunThreshold = Convert.ToSingle( Sup.GetCumulusIniValue( "Solar", "SunThreshold", "" ), CUtils.Inv ) / 100;

            Sup.LogTraceInfoMessage( $"Solar Graphs - CreateListsFromMonthlyLogs DataLogInterval = {IntervalInMinutes}" );
            Sup.LogTraceInfoMessage( $"Solar Graphs - CreateListsFromMonthlyLogs SunThreshold = {SunThreshold}" );

            for ( DateTime currentDate = startdate; currentDate <= enddate; currentDate = currentDate.AddDays( 1 ) )
            {
                int nextIndex = 0;

                DaySolarValues tmp = new DaySolarValues();
                List<MonthfileValue> DayList = dayLookup[ currentDate ].ToList();

                foreach ( MonthfileValue entry in DayList )
                {
                    if ( entry.SolarRad is null ) continue;

                    if ( ++nextIndex < DayList.Count - 1 )
                    {
                        OldIntervalInMinutes = IntervalInMinutes;
                        IntervalInMinutes = ( DayList[ nextIndex ].ThisDate - entry.ThisDate ).Minutes;         // Adjust the timediff

                        if ( IntervalInMinutes > 30 )  // PossibleIntervals[PossibleIntervals.Length - 1]
                        {
                            Sup.LogTraceVerboseMessage( $"Solar Graphs - DataLogInterval change or data gap too large = {IntervalInMinutes}" );
                            Sup.LogTraceVerboseMessage( $"Solar Graphs - DataLogInterval change Skip record" );
                            IntervalInMinutes = OldIntervalInMinutes; //reset the value and try again
                            continue;  // If more than 30 minutes, then skip this data
                        }
                        else if ( IntervalInMinutes != OldIntervalInMinutes )
                        {
                            Sup.LogTraceVerboseMessage( $"Interval change at entry: {entry.ThisDate} (next entry({nextIndex}): {DayList[ nextIndex ].ThisDate}) - Old: {OldIntervalInMinutes} New: {IntervalInMinutes}" );
                        }
                    }

                    // So this day has values, rework them to day averages with statistics in the graphing function
                    // W(interval)/M2, 60 is nr of seconds in the minute, so Energy in Ws
                    // SolarHours is determined in minutes (the total sum of interval minutes, at the end reworked to hours
                    tmp.SolarHours += ( ( (float) entry.SolarRad / entry.SolarTheoreticalMax ) >= SunThreshold ) ? IntervalInMinutes : 0;
                    tmp.SolarEnergy += (float) entry.SolarRad * IntervalInMinutes * 60;
                    tmp.SunUpTimeInMinutes += IntervalInMinutes;

                }

                tmp.SolarHours /= 60;             // The total time where the energy is larger than the threshold from minutes to hrs
                tmp.SolarEnergy /= 3600 * 1000;   // from Ws -> kWh NOTE: the construction *= for some reason does  not work
                tmp.ThisDate = currentDate;

                DailySolarValuesList.Add( tmp );
            }

#if TIMING
            watch.Stop();
            Sup.LogTraceInfoMessage( $"Solar Graphs: Timing of CreateListsFromMonthlyLogs = {watch.ElapsedMilliseconds} ms" );
#endif

            // Do the daily output in a CSV when asked for
            //
            if ( Sup.GetUtilsIniValue( "General", "NeedSolarEnergyDailyValuesInCSV", "false" ).Equals( "true" ) )
            {
                int curMonth = -1;
                string csvFilename = "DailySolarEnergy.csv";

                using ( StreamWriter dse = new StreamWriter( $"{Sup.PathUtils}{csvFilename}", false, Encoding.UTF8 ) )
                {
                    dse.WriteLine( "Date,Solar Energy,Solar Hours" );

                    // Calculate for each day the actual pwsFWI and create the csv file for the whole dayfile.txt
                    Sup.LogDebugMessage( "Writing CSV DailySolarEnergy : starting" );

                    foreach ( DaySolarValues tmp in DailySolarValuesList )
                    {
                        if ( curMonth.Equals( tmp.ThisDate.Month ) )
                        {
                            // Sam emonth, just print the values for the date
                        }
                        else
                        {
                            // New month, print new header
                            curMonth = tmp.ThisDate.Month;

                            dse.WriteLine( "" );
                            dse.WriteLine( $"New month --- {tmp.ThisDate.ToString( "Y", CUtils.ThisCulture )} :" );
                        }

                        dse.WriteLine( $"{tmp.ThisDate.ToString( "d", CUtils.ThisCulture )}{CUtils.ThisCulture.TextInfo.ListSeparator}" +
                            $"{tmp.SolarEnergy.ToString( "F2", CUtils.ThisCulture )}{CUtils.ThisCulture.TextInfo.ListSeparator}" +
                            $"{tmp.SolarHours.ToString( "F1", CUtils.ThisCulture )}" );
                    }

                    Sup.LogTraceInfoMessage( "Writing CSV DailySolarEnergy : Done" );
                }
            }

            return;
        }

        #endregion
    } // Class
} // Namespace
