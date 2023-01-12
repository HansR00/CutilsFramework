/*
 * GraphRain - Part of CumulusUtils
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
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    partial class Graphx
    {
        private void GenDailyRainGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int i = 0, j;
            float sum = 0, movingAverage = 0, MaxYearlyRainAlltime = 0;
            int period = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "PeriodMovingAverage", "180" ), inv );

            Sup.LogDebugMessage( "GenDailyRainGraphData : starting" );

            // First generate the general HTML and Graph chartcontainer stuff,
            // Then generate the dataseries.

            thisBuffer.AppendLine( "chart = Highcharts.stockChart('chartcontainer', {" );
            thisBuffer.AppendLine( "  rangeSelector:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    selected: 4" );
            thisBuffer.AppendLine( "  }," );

            string tmp2 = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp2 ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp2}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "DRTitle", "Daily Rain", true )}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    type: 'datetime'," );
            thisBuffer.AppendLine( "    crosshair: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  yAxis: [{" );
            thisBuffer.AppendLine( "    min: 0," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "DRY-axisTitle", "Rain", true )} ({Sup.StationRain.Text()})' }}, opposite: false," );
            thisBuffer.AppendLine( "    }, {" );
            thisBuffer.AppendLine( "    min: 0," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"      text: '{Sup.GetCUstringValue( "Graphs", "DRY-axisOpposite", "Moving Average", true )} (p={period})'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    opposite: true," );
            thisBuffer.AppendLine( "  }, {" );

            // Now we have to determine the reference values for the total rainfall. A lot of work for two value :)
            //

            string NormalUsage;
            float NOAARainNormYearAv = 0;
            float StationRainYearAv = 0;

            List<float> YearlyValues = new List<float>();

            NormalUsage = Sup.GetUtilsIniValue( "Graphs", "UseNormalRainReference", "Normal" );

            if ( NormalUsage.Equals( "Normal", CUtils.cmp ) || NormalUsage.Equals( "Both", CUtils.cmp ) )
            {
                StationNormal = true;

                for ( i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                {
                    string iniKeyName = "NOAARainNorm" + Enum.GetNames( typeof( Months ) )[ i - 1 ];
                    string iniResult = Sup.GetCumulusIniValue( "NOAA", iniKeyName, "0.0" );
                    if ( iniResult.IndexOf( ',' ) > 0 )
                        iniResult = iniResult.Replace( ',', '.' );
                    NOAARainNormYearAv += (float) Convert.ToDouble( iniResult, inv );
                }

                YearlyValues.Add( NOAARainNormYearAv );

                Sup.LogTraceInfoMessage( $" GenDailyRainGraphData normal values: {NOAARainNormYearAv:F1}" );
            }

            // Use station Average
            if ( NormalUsage.Equals( "StationAverage", CUtils.cmp ) || NormalUsage.Equals( "Both", CUtils.cmp ) )
            {
                List<float> tmp = new List<float>();

                StationAverage = true;

                for ( j = YearMin; j <= YearMax; j++ )
                {
                    if ( ThisList.Where( x => x.ThisDate.Year == j ).Count() < 365 )
                        continue; // Incomplete year
                    tmp.Add( ThisList.Where( x => x.ThisDate.Year == j ).Select( x => x.TotalRainThisDay ).Sum() );
                }

                // Second pass to determine the average and StdDev
                if ( tmp.Any() )
                {
                    StationRainYearAv = tmp.Average();
                }

                Sup.LogTraceInfoMessage( $" GenDailyRainGraphData : StationRainYearAv {StationRainYearAv}" );
            }

            //  Now get the highest year rainfall ever
            for ( j = YearMin; j <= YearMax; j++ )
            {
                MaxYearlyRainAlltime = Math.Max( ThisList.Where( x => x.ThisDate.Year == j ).Select( x => x.TotalRainThisDay ).Sum(), MaxYearlyRainAlltime );
            }

            thisBuffer.AppendLine( "    min: 0," );
            thisBuffer.AppendLine( $"    max: {MaxYearlyRainAlltime.ToString( "F0", inv )}," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"      text: '{Sup.GetCUstringValue( "Graphs", "DRY-axisOpposite2", "Accumulated Yearly Rain to date", true )} ({Sup.StationRain.Text()})'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    opposite: true," );

            thisBuffer.AppendLine( "      plotLines: [{" );

            if ( StationNormal )
            {
                thisBuffer.AppendLine( $"        value: {NOAARainNormYearAv.ToString( "F1", inv )}," );
                thisBuffer.AppendLine( "        zindex: 2," );
                thisBuffer.AppendLine( "        color: 'red'," );
                thisBuffer.AppendLine( "        dashStyle: 'shortdash'," );
                thisBuffer.AppendLine( "        width: 2," );
                thisBuffer.AppendLine( $"        label: {{ text: 'Normal Yearly Rainfall ({NOAARainNormYearAv.ToString( "F0", inv )})', align: 'left'  }}" );
            }

            if ( StationAverage && StationNormal )
                thisBuffer.AppendLine( "      },{" );

            if ( StationAverage ) // Must be StationAverage
            {
                thisBuffer.AppendLine( $"      value: {StationRainYearAv.ToString( "F1", inv )}," );
                thisBuffer.AppendLine( "      zindex: 2," );
                thisBuffer.AppendLine( "      color: 'green'," );
                thisBuffer.AppendLine( "      dashStyle: 'shortdash'," );
                thisBuffer.AppendLine( "      width: 2," );
                thisBuffer.AppendLine( $"      label: {{ text: 'Station Yearly Rainfall ({StationRainYearAv.ToString( "F0", inv )})', align: 'right', textAlign: 'right' }}" );
            }

            thisBuffer.AppendLine( "    }] " ); // closing the plotLines
            thisBuffer.AppendLine( "  }]," ); // closing the Y-axis
            thisBuffer.AppendLine( "  tooltip:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    headerFormat: '<span style=\"font-size: 10px\">{point.key}</span><table>'," );
            thisBuffer.AppendLine( $"    pointFormat: '<tr><td style=\"color:{{series.color}};padding: 0\">{{series.name}}: </td><td style=\"padding: 0\"><b>{{point.y:.2f}} {Sup.StationRain.Text()}</b></td></tr>'," );
            thisBuffer.AppendLine( "    footerFormat: '</table>'," );
            thisBuffer.AppendLine( "    shared: true," );
            thisBuffer.AppendLine( "    useHTML: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  plotOptions:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    column:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      pointPadding: 0.2," );
            thisBuffer.AppendLine( "      borderWidth: 0" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    spline:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      lineWidth: 1" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }," );

            StringBuilder ds = new StringBuilder( "  series: [{\n" ); // ds: DataString
            ds.Append( "    type: 'column',\n" );
            ds.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "DRSeries1Name", "Rainfall", true )}',\n" );
            //      ds.Append("    color: 'blue',\n");
            ds.Append( "    data: [ \n" );

            StringBuilder ma = new StringBuilder( "{\n" ); // ma: MovingAverage
            ma.Append( "    type: 'spline',\n" );
            ma.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "DRSeries2Name", "Moving Average", true )}',  lineWidth: 1,\n" );
            //      ma.Append("    color: 'red',\n");
            ma.Append( "    yAxis: 1,\n" );
            ma.Append( "    data: [ \n" );

            StringBuilder cr = new StringBuilder( "{\n" ); // cr: Cumulative rain
            cr.Append( "    type: 'spline',\n" );
            cr.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "DRSeries3Name", "Yearly Rain to Date", true )}',  lineWidth: 1,\n" );
            //      cr.Append("    color: 'red',\n");
            cr.Append( "    yAxis: 2,\n" );
            cr.Append( "    data: [ \n" );

            do
            {
                ds.Append( $"[{CuSupport.DateTimeToJS( ThisList[ i ].ThisDate )},{ThisList[ i ].TotalRainThisDay.ToString( "F1", NumberFormatInfo.InvariantInfo )}],\n" );

                if ( i == period )
                {
                    for ( j = i - period; j < i; j++ )
                    {
                        sum += ThisList[ j ].TotalRainThisDay;
                    }

                    movingAverage = sum / period;
                }
                else if ( i > period )
                {
                    movingAverage += ( ThisList[ i - 1 ].TotalRainThisDay - ThisList[ i - period - 1 ].TotalRainThisDay ) / period;
                }

                if ( i >= period )
                    ma.Append( $"[{CuSupport.DateTimeToJS( ThisList[ i ].ThisDate )},{movingAverage.ToString( "F2", NumberFormatInfo.InvariantInfo )}],\n" );
                cr.Append( $"[{CuSupport.DateTimeToJS( ThisList[ i ].ThisDate )},{ThisList[ i ].YearToDateRain.ToString( "F1", NumberFormatInfo.InvariantInfo )}],\n" );

                i++;
            }
            while ( i < ThisList.Count );

            // get rid of the last commas / newlines
            ds.Remove( ds.Length - 2, 2 );
            ma.Remove( ma.Length - 2, 2 );
            cr.Remove( cr.Length - 2, 2 );

            ds.Append( "]}," );
            ma.Append( "]}," );
            cr.Append( "]}]\n" );

            thisBuffer.AppendLine( ds.ToString() );
            thisBuffer.AppendLine( ma.ToString() );
            thisBuffer.AppendLine( cr.ToString() );
            thisBuffer.AppendLine( "});" );

            return;
        }

        private void GenMonthlyRainvsNOAAGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int counter;
            string NormalUsage;

            float[] MonthlyRainValues = new float[ 12 ];
            float[] NOAARainNorm = new float[ 12 ];
            float[] NOAARainStationAv = new float[ 12 ];
            float[] NOAARainStdDev = new float[ 12 ];

            List<float[]> YearValues = new List<float[]>();
            List<int> years = new List<int>();

            StringBuilder sb;

            Sup.LogDebugMessage( "GenMonthlyRainvsNOAAGraphData : starting" );

            NormalUsage = Sup.GetUtilsIniValue( "Graphs", "UseNormalRainReference", "Normal" );

            // Fill the Normal array - from tradition this is named after the NOAA but it can be from any Meteo organisation

            if ( NormalUsage.Equals( "Normal", CUtils.cmp ) || NormalUsage.Equals( "Both", CUtils.cmp ) )
            {
                StationNormal = true;

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                {
                    string iniKeyName = "NOAARainNorm" + Enum.GetNames( typeof( Months ) )[ i - 1 ];
                    string iniResult = Sup.GetCumulusIniValue( "NOAA", iniKeyName, "0.0" );
                    if ( iniResult.IndexOf( ',' ) > 0 )
                        iniResult = iniResult.Replace( ',', '.' );
                    NOAARainNorm[ i - 1 ] = (float) Convert.ToDouble( iniResult, inv );

                    Sup.LogTraceInfoMessage( $" Normal values: {iniKeyName} -> {NOAARainNorm[ i - 1 ].ToString( "F1", inv )}" );
                }
            }

            // Use station Average
            if ( NormalUsage.Equals( "StationAverage", CUtils.cmp ) || NormalUsage.Equals( "Both", CUtils.cmp ) )
            {
                StationAverage = true;

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                {
                    List<float> tmp = new List<float>();

                    NOAARainStationAv[ i - 1 ] = 0;

                    // First pass to detect the rain per month per year. Needed for estimating the StdDev
                    for ( int j = YearMin; j <= YearMax; j++ )
                    {
                        if ( ThisList.Where( x => x.ThisDate.Month == i ).Where( x => x.ThisDate.Year == j ).Any() )
                            tmp.Add( ThisList.Where( x => x.ThisDate.Month == i ).Where( x => x.ThisDate.Year == j ).Select( x => x.MonthlyRain ).Max() );
                    }

                    // Second pass to determine the average and StdDev
                    if ( tmp.Any() )
                    {
                        NOAARainStationAv[ i - 1 ] = tmp.Average();  //= counter;
                        NOAARainStdDev[ i - 1 ] = tmp.StdDev();
                    }
                    else
                        NOAARainStationAv[ i - 1 ] = -1;

                    Sup.LogTraceInfoMessage( $" Station Average values: {Enum.GetNames( typeof( Months ) )[ i - 1 ]} -> {NOAARainNorm[ i - 1 ].ToString( "F1", inv )}" );
                }
            }

            Sup.LogTraceInfoMessage( "GenMonthlyRainvsNOAAGraphData : start loop over DayfileValues" );

            for ( int i = YearMin; i <= YearMax; i++ )
            {
                MonthlyRainValues = new float[ Enum.GetNames( typeof( Months ) ).Length ];
                YearValues.Add( MonthlyRainValues );
                years.Add( i );

                for ( int j = (int) Months.Jan; j <= (int) Months.Dec; j++ )
                {
                    //Now do the actual month work
                    if ( ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == j ).Any() )
                        MonthlyRainValues[ j - 1 ] = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == j ).Select( x => x.MonthlyRain ).Max();
                    else
                        MonthlyRainValues[ j - 1 ] = -1;
                }
            }

            Sup.LogTraceInfoMessage( "GenMonthlyRainvsNOAAGraphData : start Generation" );

            // Now generate the script
            thisBuffer.AppendLine( "  chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  chart:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    type: 'spline'," );
            thisBuffer.AppendLine( "    ignoreHiddenSeries: false" ); // scaling does not changes when a series is disabled
            thisBuffer.AppendLine( "  }," );

            string tmp2 = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp2 ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp2}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "MRTitle", "Monthly Rain", true )} {Sup.StationRain.Text()}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"   text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );

            sb = new StringBuilder();
            sb.Append( "    categories: [" );

            for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                sb.Append( $"'{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i )}'," );

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( sb.ToString() );

            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  yAxis:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"      text: '{Sup.GetCUstringValue( "Graphs", "MRY-axis", "Monthly Rain", true )} {Sup.StationRain.Text()}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }," );

            thisBuffer.AppendLine( "  plotOptions:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    line:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      dataLabels:" );
            thisBuffer.AppendLine( "      {" );
            thisBuffer.AppendLine( "        enabled: true" );
            thisBuffer.AppendLine( "      }," );
            thisBuffer.AppendLine( "      enableMouseTracking: false" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }," );


            thisBuffer.AppendLine( "    series: [{" );

            sb = new StringBuilder();

            if ( StationNormal )
            {
                //Do the NORMAL series
                sb.Append( $"       name : '{Sup.GetCUstringValue( "Graphs", "MTNormal", "Normal", true )}', color: 'black', visible:true, zIndex:-1,\n" +
                          "       data : [" );
                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                    sb.Append( $"{NOAARainNorm[ i - 1 ].ToString( "F1", NumberFormatInfo.InvariantInfo )}," );

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( ']' );
            }

            if ( StationAverage )
            {
                if ( StationNormal )
                    sb.Append( "},{\n" );

                //Do the Station Average serie
                sb.Append( $"       name : '{Sup.GetCUstringValue( "Graphs", "MTStationAverage", "Station average", true )}', color: 'grey', visible:true, zIndex:-2,\n" +
                  "       data : [" );
                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                    sb.Append( $"{NOAARainStationAv[ i - 1 ].ToString( "F1", NumberFormatInfo.InvariantInfo )}," );

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( ']' );
                sb.Append( "},{\n" );

                // Build the StdDev around Station Average series
                sb.Append( $"       name: '{Sup.GetCUstringValue( "Graphs", "StdDev", "StdDev", true )}', color: 'Gainsboro', visible:true, zIndex:-3, \n" );
                sb.Append( "       marker: { enabled: false}," );
                sb.Append( "       type: 'areasplinerange',\n" );
                sb.Append( "       data: [" );

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                    sb.Append( $"[{Math.Max( 0.0, NOAARainStationAv[ i - 1 ] - NOAARainStdDev[ i - 1 ] ).ToString( "F1", NumberFormatInfo.InvariantInfo )}," +
                      $"{( NOAARainStationAv[ i - 1 ] + NOAARainStdDev[ i - 1 ] ).ToString( "F1", NumberFormatInfo.InvariantInfo )}]," );

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( "],\n" );
                sb.Append( "       showInLegend: true" );
            }

            bool PrintFirstAccolades = false;
            if ( StationNormal || StationAverage )
                PrintFirstAccolades = true;

            counter = 0;
            foreach ( float[] ValuesList in YearValues )
            {
                if ( PrintFirstAccolades )
                    sb.Append( "},{\n" );
                else
                    PrintFirstAccolades = true;

                sb.Append( $"      name: '{years[ counter ].ToString( inv )}', \n" );

                if ( counter >= YearValues.Count - maxNrOfSeriesVisibleInGraph ) { sb.Append( "      visible: true,\n" ); }
                else { sb.Append( "      visible: false,\n" ); }

                sb.Append( $"      zIndex:{counter},\n" );
                sb.Append( $"      data: [" );

                for ( int i = 0; i < 12; i++ )
                {
                    if ( ValuesList[ i ] == -1 )
                        sb.Append( "null," );
                    else
                        sb.Append( $"{ValuesList[ i ].ToString( "F1", NumberFormatInfo.InvariantInfo )}," );
                }

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( $"]" );
                counter++;
            }

            thisBuffer.AppendLine( $"{sb}\n }}]" );
            thisBuffer.AppendLine( "  });" );

            return;
        }

        private void GenerateYearRainStatistics( List<DayfileValue> Thislist, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();
            ;

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> minrain = new List<float>();
            List<float> maxrain = new List<float>();

            Sup.LogDebugMessage( "Generate GenerateYearRainStatistics Start" );

            for ( int i = YearMin; i <= YearMax; i++ )
            {
                List<DayfileValue> yearlist = Thislist.Where( x => x.ThisDate.Year == i ).ToList();

                if ( yearlist.Count == 0 ) continue;

                Sup.LogTraceInfoMessage( $"Generating Year Rain Statistics, doing year {i}" );

                years.Add( i );
                average.Add( yearlist.Select( x => x.TotalRainThisDay ).Average() );
                stddev.Add( yearlist.Select( x => x.TotalRainThisDay ).StdDev() );
                minrain.Add( yearlist.Select( x => x.TotalRainThisDay ).Min() );
                maxrain.Add( yearlist.Select( x => x.TotalRainThisDay ).Max() );
            }

            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YRSTitle", "Year Rain Statistics", true )} ({Sup.GetCUstringValue( "Graphs", "LogarithmicScale", "Logarithmic scale!", true )})' " );
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
            thisBuffer.AppendLine( "  type: 'logarithmic'," );
            thisBuffer.AppendLine( "  custom: {allowNegativeLog: true}," );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YRSRainRange", "Rain range", true )} ({Sup.StationRain.Text()})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: 0.0," );
            thisBuffer.AppendLine( $"  max: {maxRain.ToString( "F2", inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: '{Sup.StationRain.Text()}'" );
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
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.StationRain.Text()}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YRSRainRange", "Rain range", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(19, 114, 248, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba(0, 128, 0, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                // minrain[i] is replaced by 0, can't have subzero rain
                sb.Append( $"[{0.ToString( "F2", inv )},{maxrain[ i ].ToString( "F2", inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YRSAverageRain", "Average rain", true )}'," );
            thisBuffer.AppendLine( "zIndex: 2," );
            thisBuffer.AppendLine( "  type: 'spline'," );
            thisBuffer.AppendLine( "  lineWidth: 1," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{average[ i ].ToString( "F2", inv )}]," );
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

                sb.Append( $"[{( AvMinusStddev < 0.0 ? 0.ToString( "F2", inv ) : AvMinusStddev.ToString( "F2", inv ) )}," +
                    $"{( average[ i ] + stddev[ i ] ).ToString( "F2", inv )}]," );
            }

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );
        }

        private void GenerateYearMonthRainStatistics( List<DayfileValue> Thislist, Months thisMonth, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();
            ;

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> minrain = new List<float>();
            List<float> maxrain = new List<float>();

            for ( int i = YearMin; i <= YearMax; i++ )
            {
                List<DayfileValue> yearmonthlist = Thislist.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == (int) thisMonth ).ToList();

                Sup.LogTraceVerboseMessage( $"Generating Year Month Rain Statistics, doing year {i} and month {thisMonth}" );

                if ( yearmonthlist.Any() )
                {
                    years.Add( i );
                    average.Add( yearmonthlist.Select( x => x.TotalRainThisDay ).Average() );
                    stddev.Add( yearmonthlist.Select( x => x.TotalRainThisDay ).StdDev() );
                    minrain.Add( yearmonthlist.Select( x => x.TotalRainThisDay ).Min() );
                    maxrain.Add( yearmonthlist.Select( x => x.TotalRainThisDay ).Max() );
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
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YMRSTitle", "Year Rain Statistics for", true )} " +
              $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( (int) thisMonth )} ({Sup.GetCUstringValue( "Graphs", "LogarithmicScale", "Logarithmic scale!", true )})' " );
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
            thisBuffer.AppendLine( "  type: 'logarithmic'," );
            thisBuffer.AppendLine( "  custom: {allowNegativeLog: true}," );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YRSRainRange", "Rain range", true )} ({Sup.StationRain.Text()})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: 0.0," );
            thisBuffer.AppendLine( $"  max: {maxRain.ToString( "F2", inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: '{Sup.StationRain.Text()}'" );
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
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.StationRain.Text()}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YRSRainRange", "Rain range", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(19, 114, 248, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba(0, 128, 0, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
                sb.Append( $"[0.0,{maxrain[ i ].ToString( "F2", inv )}]," );

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YRSAverageRain", "Average rain", true )}'," );
            thisBuffer.AppendLine( "zIndex: 2," );
            thisBuffer.AppendLine( "  type: 'spline'," );
            thisBuffer.AppendLine( "  lineWidth: 1," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{average[ i ].ToString( "F2", inv )}]," );
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

                sb.Append( $"[{( AvMinusStddev < 0.0 ? 0.ToString( "F2", inv ) : AvMinusStddev.ToString( "F2", inv ) )}," +
                    $"{( average[ i ] + stddev[ i ] ).ToString( "F2", inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );
        }
    }
}
