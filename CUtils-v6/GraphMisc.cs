/*
 * GraphMisc - Part of CumulusUtils
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
        #region Daily EVT

        private void GenDailyEVTGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int i = 0, j;
            float sum = 0, movingAverage = 0;
            int period = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "PeriodMovingAverage", "180" ), inv );

            Sup.LogDebugMessage( "GenDailyEVTGraphData : starting" );

            thisBuffer.AppendLine( "Highcharts.stockChart('chartcontainer', {" );
            thisBuffer.AppendLine( "  rangeSelector:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    selected: 4" );
            thisBuffer.AppendLine( "  }," );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "EVTTitle", "Daily EVT", true )}'" );
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
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "EVT-axisTitle", "EVT", true )} ({Sup.StationRain.Text()})' }}, opposite: false," );
            thisBuffer.AppendLine( "    }, {" );
            thisBuffer.AppendLine( "    min: 0," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"      text: '{Sup.GetCUstringValue( "Graphs", "EVT-axisOpposite", "Moving Average", true )} (p={period})'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    opposite: true," );
            thisBuffer.AppendLine( "  }]," );
            thisBuffer.AppendLine( "  tooltip:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    headerFormat: '<span style=\"font-size: 10px\">{point.key}</span><table>'," );
            thisBuffer.AppendLine( $"    pointFormat: '<tr><td style=\"color: {{series.color}}; padding: 0\">{{series.name}}: </td><td style=\"padding: 0\"><b>{{point.y:.2f}} {Sup.StationRain.Text()}</b></td></tr>'," );
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
            ds.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "EVTSeries1Name", "EVT", true )}',\n" );
            //      ds.Append("    color: 'blue',\n");
            ds.Append( "    data: [" );

            StringBuilder ma = new StringBuilder( "{\n" ); // ma: MovingAverage
            ma.Append( "    type: 'spline',\n" );
            ma.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "EVTSeries2Name", "Moving Average", true )}',  lineWidth: 1,\n" );
            //      ma.Append("    color: 'red',\n");
            ma.Append( "    yAxis: 1,\n" );
            ma.Append( "    data: [" );

            do
            {
                ds.Append( $"[{CuSupport.DateTimeToJS( ThisList[ i ].ThisDate )},{ThisList[ i ].EvapoTranspiration.ToString( "F1", NumberFormatInfo.InvariantInfo )}],\n" );

                if ( i == period )
                {
                    for ( j = i - period; j < i; j++ )
                    {
                        sum += ThisList[ j ].EvapoTranspiration;
                    }

                    movingAverage = sum / period;
                }
                else if ( i > period )
                {
                    movingAverage += ( ThisList[ i - 1 ].EvapoTranspiration - ThisList[ i - period - 1 ].EvapoTranspiration ) / period;
                }

                // If not yet reached the period length we have to do something
                if ( i >= period )
                    ma.Append( $"[{CuSupport.DateTimeToJS( ThisList[ i ].ThisDate )},{movingAverage.ToString( "F2", NumberFormatInfo.InvariantInfo )}],\n" );

                i++;
            }
            while ( i < ThisList.Count );

            // get rid of the last commas / newlines
            ds.Remove( ds.Length - 2, 2 );
            if ( i >= period )
                ma.Remove( ma.Length - 2, 2 );  // If no data for the moving average then don't do anything

            ds.Append( "]}," );
            ma.Append( "]}]\n" );

            thisBuffer.AppendLine( ds.ToString() );
            thisBuffer.AppendLine( ma.ToString() );
            thisBuffer.AppendLine( "});" );

            return;
        }

        #endregion

        #region Monthly EVT

        private void GenMonthlyEVTGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int counter;

            float[] MonthlyEVTValues = new float[ 12 ];

            List<float[]> YearValues = new List<float[]>();
            List<int> years = new List<int>();

            StringBuilder sb;

            Sup.LogDebugMessage( "GenMonthlyEVTGraphData : starting" );

            for ( int i = YearMin; i <= YearMax; i++ )
            {
                MonthlyEVTValues = new float[ Enum.GetNames( typeof( Months ) ).Length ];
                YearValues.Add( MonthlyEVTValues );
                years.Add( i );

                for ( int j = (int) Months.Jan; j <= (int) Months.Dec; j++ )
                {
                    //Now do the actual month work
                    if ( ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == j ).Any() )
                        MonthlyEVTValues[ j - 1 ] = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == j ).Select( x => x.EvapoTranspiration ).Sum();
                    else
                        MonthlyEVTValues[ j - 1 ] = -1;
                }
            }

            // Now generate the script
            thisBuffer.AppendLine( "  Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  chart:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    type: 'spline'," );
            thisBuffer.AppendLine( "    ignoreHiddenSeries: false" ); // scaling does not change when a series is disabled
            thisBuffer.AppendLine( "  }," );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "MEVTTitle", "Monthly EVT", true )} ({Sup.StationRain.Text()})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"   text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );

            // Generate the x-axis with the months accoding to the locale
            //      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']");

            sb = new StringBuilder();
            sb.Append( "    categories: [" );

            for ( int i = 0; i < 12; i++ )
                sb.Append( "'" + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i + 1 ) + "'," );

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( sb.ToString() );

            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  yAxis:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "  min: 0," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"      text: '{Sup.GetCUstringValue( "Graphs", "MEVTY-axis", "Monthly EVT", true )} ({Sup.StationRain.Text()})'" );
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

            thisBuffer.AppendLine( "  series: [{" );

            sb = new StringBuilder();

            counter = 0;
            foreach ( float[] ValuesList in YearValues )
            {
                if ( counter > 0 )
                    sb.Append( "},{\n" );

                sb.Append( $"      name: '{years[ counter ].ToString( inv )}', \n" );

                if ( counter >= YearValues.Count - maxNrOfSeriesVisibleInGraph ) { sb.Append( "      visible: true,\n" ); }
                else { sb.Append( "      visible: false,\n" ); }

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

            thisBuffer.AppendLine( $"{sb}\n    }}]" );
            thisBuffer.AppendLine( "  });" );

            return;
        }

        #endregion

        #region TempSum
        private void GenTempSum( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            float TempSum;
            float Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), inv );
            bool NorthernHemisphere = Latitude >= 0;
            DateTime StartDate, EndDate;

            Sup.LogDebugMessage( "GenTempSum : starting" );

            thisBuffer.AppendLine( "Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  rangeSelector:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    enabled: false" );
            thisBuffer.AppendLine( "  }," );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "TempSumTitle", "Temperature Sum", true )}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );
            //thisBuffer.AppendLine( "    type: 'datetime'," );
            thisBuffer.AppendLine( "    crosshair: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  yAxis: [{" );
            //thisBuffer.AppendLine( "    min: 0," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "TempSum-axisTitle", "Temperature Sum", true )} (Degree Days {Sup.StationTemp.Text()})' }}, opposite: false," );
            thisBuffer.AppendLine( "    }]," );
            thisBuffer.AppendLine( "  tooltip:" );
            thisBuffer.AppendLine( "  {" ); //xDateFormat: '%Y-%m-%d',
            thisBuffer.AppendLine( "    headerFormat: '<span style=\"font-size: 10px\">{point.key}</span><table>'," );
            thisBuffer.AppendLine( $"    pointFormat: '<tr><td style=\"color: {{series.color}}; padding: 0\">{{series.name}}: </td><td style=\"padding: 0\"><b>{{point.y:.1f}} {Sup.StationTemp.Text()}</b></td></tr>'," );
            thisBuffer.AppendLine( "    footerFormat: '</table>'," );
            thisBuffer.AppendLine( "    shared: true," );
            thisBuffer.AppendLine( "    useHTML: true" );
            thisBuffer.AppendLine( "  }," );

            StringBuilder ma = new StringBuilder( "" );
            thisBuffer.AppendLine( "series: [" );

            for ( int year = YearMin; year <= YearMax; year++ )
            {
                // Make sure we have data for the first day of the growing season :
                // 1 january on Northern Hemisphere, 1 july on southern hemisphere
                if ( NorthernHemisphere )
                {
                    StartDate = new DateTime( year, 1, 1 );
                    EndDate = new DateTime( year + 1, 1, 1 );
                }
                else
                {
                    // Must be Southern Hemisphere
                    StartDate = new DateTime( year, 7, 1 );
                    EndDate = new DateTime( year + 1, 7, 1 );
                }

                List<DayfileValue> yearList = new List<DayfileValue>();
                yearList = ThisList.Where( x => x.ThisDate >= StartDate && x.ThisDate < EndDate ).ToList();

                // Do data  exist on the startdate of the first year? If not than skip thios season
                if ( ThisList[ 0 ].ThisDate > StartDate || !yearList.Any() ) continue;

                int i = 0;
                TempSum = 0;

                ma.AppendLine( "{    type: 'spline'," );
                ma.AppendLine( $"    name: '{year}'," );

                if ( YearMax - year + 1 <= maxNrOfSeriesVisibleInGraph ) { ma.AppendLine( "    visible: true," ); }
                else { ma.AppendLine( "    visible: false," ); }

                ma.AppendLine( $"    zIndex:{year - YearMin + 1}," );
                ma.AppendLine( "    data: [" );

                if ( yearList.Any() )
                {
                    try
                    {
                        do
                        {
                            TempSum += yearList[ i ].AverageTempThisDay > 0.0 ? yearList[ i ].AverageTempThisDay : 0;
                            ma.Append( $"[{ i },{ TempSum.ToString( "F1", NumberFormatInfo.InvariantInfo ) }]," );
                        }
                        while ( ++i < yearList.Count );
                    }
                    catch ( Exception e )
                    {
                        Sup.LogDebugMessage( $"GenTempSum : Exception {e.Message}" );
                        Sup.LogDebugMessage( $"GenTempSum : Exception year: {year}, i: {i}, yearList.count: {yearList.Count}" );
                        Environment.Exit( 0 );
                    }

                    // get rid of the last commas / newlines
                    ma.Remove( ma.Length - 1, 1 );
                }

                ma.Append( "]}," );
            }

            if ( ma.Length > 15 )  // Anyway, larger than the "series: [{" init string
            {
                ma.Remove( ma.Length - 1, 1 );
            }

            ma.AppendLine( "]" );
            thisBuffer.AppendLine( ma.ToString() );
            thisBuffer.AppendLine( "});" );

            return;
        }

        #endregion

        #region GDD

        //
        // https://en.wikipedia.org/wiki/Growing_degree-day
        //
        private void GenGrowingDegreeDays( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            float TempSum;
            float TempReference = Convert.ToSingle( Sup.GetUtilsIniValue( "Graphs", "GrowingDegreeDaysReferenceTemp", "5" ) );
            float Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), inv );
            bool NorthernHemisphere = Latitude >= 0;
            DateTime StartDate, EndDate;

            Sup.LogDebugMessage( "GrowingDegreeDays : starting" );

            thisBuffer.AppendLine( "Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  rangeSelector:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    enabled: false" );
            thisBuffer.AppendLine( "  }," );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "GrowingDegreeDaysTitle", "Growing Degree Days", true )} - Reference value: {Sup.StationTemp.Format( TempReference )} °C'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );
            //thisBuffer.AppendLine( "    type: 'datetime'," );
            thisBuffer.AppendLine( "    crosshair: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  yAxis: [{" );
            //thisBuffer.AppendLine( "    min: 0," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "GrowingDegreeDays-axisTitle", "GrowingDegreeDays", true )} (Degree Days {Sup.StationTemp.Text()})' }}, opposite: false," );
            thisBuffer.AppendLine( "    }]," );
            thisBuffer.AppendLine( "  tooltip:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    headerFormat: '<span style=\"font-size: 10px\">{point.key}</span><table>'," );
            thisBuffer.AppendLine( $"    pointFormat: '<tr><td style=\"color: {{series.color}}; padding: 0\">{{series.name}}: </td><td style=\"padding: 0\"><b>{{point.y:.2f}} {Sup.StationTemp.Text()}</b></td></tr>'," );
            thisBuffer.AppendLine( "    footerFormat: '</table>'," );
            thisBuffer.AppendLine( "    shared: true," );
            thisBuffer.AppendLine( "    useHTML: true" );
            thisBuffer.AppendLine( "  }," );

            StringBuilder ma = new StringBuilder( "" );
            thisBuffer.AppendLine( "series: [" );

            for ( int year = YearMin; year <= YearMax; year++ )
            {
                // Make sure we have data for the first day of the growing season :
                // 1 january on Northern Hemisphere, 1 july on southern hemisphere
                if ( NorthernHemisphere )
                {
                    StartDate = new DateTime( year, 1, 1 );
                    EndDate = new DateTime( year + 1, 1, 1 );
                }
                else
                {
                    // Must be Southern Hemisphere
                    StartDate = new DateTime( year, 7, 1 );
                    EndDate = new DateTime( year + 1, 7, 1 );
                }

                List<DayfileValue> yearList = new List<DayfileValue>();
                yearList = ThisList.Where( x => x.ThisDate >= StartDate && x.ThisDate < EndDate ).ToList();

                // Do data  exist on the startdate of the first year? If not than skip thios season
                if ( ThisList[ 0 ].ThisDate > StartDate || !yearList.Any() ) continue;

                int i = 0;
                TempSum = 0;

                ma.AppendLine( "{    type: 'spline'," );
                ma.AppendLine( $"    name: '{year}'," );

                if ( YearMax - year + 1 <= maxNrOfSeriesVisibleInGraph ) { ma.AppendLine( "    visible: true," ); }
                else { ma.AppendLine( "    visible: false," ); }

                ma.AppendLine( $"    zIndex:{year - YearMin + 1}," );
                ma.AppendLine( "    data: [" );

                if ( yearList.Any() )
                {
                    try
                    {
                        do
                        {
                            TempSum += yearList[ i ].AverageTempThisDay - TempReference;
                            ma.Append( $"[{i},{   TempSum.ToString( "F1", NumberFormatInfo.InvariantInfo )}]," );
                        }
                        while ( ++i < yearList.Count );
                    }
                    catch ( Exception e )
                    {
                        Sup.LogDebugMessage( $"GenTempSum : Exception {e.Message}" );
                        Sup.LogDebugMessage( $"GenTempSum : Exception year: {year}, i: {i}, yearList.count: {yearList.Count}" );
                        Environment.Exit( 0 );
                    }

                    // get rid of the last commas / newlines
                    ma.Remove( ma.Length - 1, 1 );
                }

                ma.Append( "]}," );
            }

            if ( ma.Length > 15 )  // Anyway, larger than the "series: [{" init string
            {
                ma.Remove( ma.Length - 1, 1 );
            }

            ma.AppendLine( "]" );
            thisBuffer.AppendLine( ma.ToString() );
            thisBuffer.AppendLine( "});" );

            return;
        }

        #endregion

        #region ThermalSeasons

        struct YearSeasons
        {
            internal int year;
            internal int Winter1;
            internal int Spring;
            internal int Summer;
            internal int Autumn;
            internal int Winter2;
        }

        private void YearlySeasons( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            float Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), inv );
            bool NorthernHemisphere = Latitude >= 0;
            DateTime StartDate, EndDate;

            List<YearSeasons> SeasonList = new List<YearSeasons>();

            Sup.LogDebugMessage( "YearlySeasons : starting" );

            int WinterToSpringTemperatureLimit = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "WinterToSpringTemperatureLimit", "0" ) );
            int SpringToSummerTemperatureLimit = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "SpringToSummerTemperatureLimit", "10" ) );

            Sup.LogDebugMessage( $"YearlySeasons : WinterToSpringTemperatureLimit = {WinterToSpringTemperatureLimit}" );
            Sup.LogDebugMessage( $"YearlySeasons : SpringToSummerTemperatureLimit = {SpringToSummerTemperatureLimit}" );

            for ( int year = YearMin; year <= YearMax; year++ )
            {
                // Make sure we have data for the first day of the growing season :
                // 1 january on Northern Hemisphere, 1 july on southern hemisphere
                if ( NorthernHemisphere )
                {
                    StartDate = new DateTime( year, 1, 1 );
                    EndDate = new DateTime( year + 1, 1, 1 );
                }
                else
                {
                    // Must be Southern Hemisphere
                    StartDate = new DateTime( year, 7, 1 );
                    EndDate = new DateTime( year + 1, 7, 1 );
                }

                List<DayfileValue> yearList = new List<DayfileValue>();
                yearList = ThisList.Where( x => x.ThisDate >= StartDate && x.ThisDate < EndDate ).ToList();

                // Do data  exist on the startdate of the first year? If not than skip thios season
                if ( ThisList[ 0 ].ThisDate > StartDate || !yearList.Any() ) continue;

                YearSeasons thisYearSeasonList = new YearSeasons();

                int i = 0;
                bool WinterSOY = false, WinterEOY = false, Spring = false, Summer = false, Autumn = false;
                bool possibleChange = false;
                int changeCounter = 0;

                WinterSOY = true;

                do
                {
                    if ( WinterSOY )
                    {
                        if ( yearList[ i ].AverageTempThisDay >= WinterToSpringTemperatureLimit )
                            possibleChange = true;
                        else { possibleChange = false; changeCounter = 0; }

                        if ( possibleChange )
                        {
                            changeCounter++;
                            if ( changeCounter == 10 )
                            {
                                // We reached spring with 10 consecutive days above the Set Temperature limit.
                                WinterSOY = false;
                                Spring = true;
                                changeCounter = 0;
                                thisYearSeasonList.Winter1 = i - 10;
                                Sup.LogDebugMessage( $"YearlySeasons : {year} Spring starting on day {i - 10}" );
                            }
                        }
                    }
                    else if ( Spring )
                    {
                        if ( yearList[ i ].AverageTempThisDay >= SpringToSummerTemperatureLimit )
                            possibleChange = true;
                        else { possibleChange = false; changeCounter = 0; }

                        if ( possibleChange )
                        {
                            changeCounter++;
                            if ( changeCounter == 10 )
                            {
                                // We reached summer with 10 consecutive days above the Set Temperature limit.
                                Spring = false;
                                Summer = true;
                                changeCounter = 0;
                                thisYearSeasonList.Spring = i - 10 - thisYearSeasonList.Winter1;
                                Sup.LogDebugMessage( $"YearlySeasons : {year} Summer starting on day {i - 10}" );
                            }
                        }
                    }
                    else if ( Summer )
                    {
                        if ( yearList[ i ].AverageTempThisDay <= SpringToSummerTemperatureLimit )
                            possibleChange = true;
                        else { possibleChange = false; changeCounter = 0; }

                        if ( possibleChange )
                        {
                            changeCounter++;
                            if ( changeCounter == 10 )
                            {
                                // We reached summer with 10 consecutive days above the Set Temperature limit.
                                Summer = false;
                                Autumn = true;
                                changeCounter = 0;
                                thisYearSeasonList.Summer = i - 10 - thisYearSeasonList.Spring - thisYearSeasonList.Winter1;
                                Sup.LogDebugMessage( $"YearlySeasons : {year} Autumn starting on day {i - 10}" );
                            }
                        }
                    }
                    else if ( Autumn )
                    {
                        if ( yearList[ i ].AverageTempThisDay <= WinterToSpringTemperatureLimit )
                            possibleChange = true;
                        else { possibleChange = false; changeCounter = 0; }

                        if ( possibleChange )
                        {
                            changeCounter++;
                            if ( changeCounter == 10 )
                            {
                                // We reached spring with 10 consecutive days above the Set Temperature limit.
                                Autumn = false;
                                WinterEOY = true;
                                changeCounter = 0;
                                thisYearSeasonList.Autumn = i - 10 - thisYearSeasonList.Summer - thisYearSeasonList.Spring - thisYearSeasonList.Winter1;
                                Sup.LogDebugMessage( $"YearlySeasons : {year} Winter starting on day {i - 10}" );
                            }
                        }
                    }
                    else if ( WinterEOY )
                    {
                        thisYearSeasonList.Winter2 = yearList.Count - thisYearSeasonList.Autumn - thisYearSeasonList.Summer - thisYearSeasonList.Spring - thisYearSeasonList.Winter1;
                        ;
                        break;
                    }
                } while ( ++i < yearList.Count );

                // End of year reached. If not Winter 2 then ajust the season days backwards
                if ( WinterEOY ) { }
                if ( Autumn ) { thisYearSeasonList.Autumn = i - thisYearSeasonList.Summer - thisYearSeasonList.Spring - thisYearSeasonList.Winter1; }
                if ( Summer ) { thisYearSeasonList.Summer = i - thisYearSeasonList.Spring - thisYearSeasonList.Winter1; }
                if ( Spring ) { thisYearSeasonList.Spring = i - thisYearSeasonList.Winter1; }
                if ( WinterSOY ) { thisYearSeasonList.Winter1 = i; }

                thisYearSeasonList.year = year;
                SeasonList.Add( thisYearSeasonList );
            }

            SeasonList.Reverse();

            thisBuffer.AppendLine( "Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  rangeSelector:{enabled: false}," );
            thisBuffer.AppendLine( "  chart:{type: 'bar'}, " );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( $"  title:{{text: '{Sup.GetCUstringValue( "Graphs", "YearlySeasonsTitle", "Yearly Thermal Seasons", true )} " +
                $"- Limit values: {WinterToSpringTemperatureLimit} and {SpringToSummerTemperatureLimit} °C' }}," );
            thisBuffer.AppendLine( $"  subtitle:{{text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\" }}," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );

            StringBuilder ma = new StringBuilder( "" );
            ma.Append( "categories:[" );
            foreach ( YearSeasons seasonEntry in SeasonList )
                ma.Append( $"'{seasonEntry.year}'," );
            if ( SeasonList.Any() )
                ma.Remove( ma.Length - 1, 1 );
            ma.Append( "]," );

            thisBuffer.AppendLine( ma.ToString() );

            thisBuffer.AppendLine( "    crosshair: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  yAxis:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    title:{{text: '{Sup.GetCUstringValue( "Graphs", "YearlysSeasonsyAxisTitle", "Percentage of the year", true )}'}}" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  tooltip:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    headerFormat: '<span style=\"font-size: 10px\">{point.key}</span><table>'," );
            thisBuffer.AppendLine( $"    pointFormat: '<tr><td style=\"color: {{series.color}}; padding: 0\">{{series.name}}: </td><td style=\"padding: 0\"><b>{{point.y:.0f}}</b></td></tr>'," );
            thisBuffer.AppendLine( "    footerFormat: '</table>'," );
            thisBuffer.AppendLine( "    shared: true," );
            thisBuffer.AppendLine( "    useHTML: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  plotOptions: {bar: {stacking: 'percent'} }," );
            thisBuffer.AppendLine( "series: [" );

            StringBuilder wi1 = new StringBuilder();
            StringBuilder sp = new StringBuilder();
            StringBuilder su = new StringBuilder();
            StringBuilder au = new StringBuilder();
            StringBuilder wi2 = new StringBuilder();

            if ( SeasonList.Any() )
            {
                wi1.AppendLine( "{name:'Winter 1',data:[" );
                sp.AppendLine( "{name:'Spring',data:[" );
                su.AppendLine( "{name:'Summer',data:[" );
                au.AppendLine( "{name:'Autumn',data:[" );
                wi2.AppendLine( "{name:'Winter 2',data:[" );

                foreach ( YearSeasons seasonEntry in SeasonList )
                {
                    wi1.Append( $"{seasonEntry.Winter1}," );
                    sp.Append( $"{seasonEntry.Spring}," );
                    su.Append( $"{seasonEntry.Summer}," );
                    au.Append( $"{seasonEntry.Autumn}," );
                    wi2.Append( $"{seasonEntry.Winter2}," );
                }

                wi1.Remove( wi1.Length - 1, 1 );
                sp.Remove( sp.Length - 1, 1 );
                su.Remove( su.Length - 1, 1 );
                au.Remove( au.Length - 1, 1 );
                wi2.Remove( wi2.Length - 1, 1 );

                wi1.Append( "]}" );
                sp.Append( "]}," );
                su.Append( "]}," );
                au.Append( "]}," );
                wi2.Append( "]}," );
            }

            thisBuffer.AppendLine( wi2.ToString() );
            thisBuffer.AppendLine( au.ToString() );
            thisBuffer.AppendLine( su.ToString() );
            thisBuffer.AppendLine( sp.ToString() );
            thisBuffer.AppendLine( wi1.ToString() );
            thisBuffer.AppendLine( "]" );

            thisBuffer.AppendLine( "});" );

            return;
        }

        #endregion

        #region Clash of Averages

        private void GenerateClashOfAverages( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            float sumCumulusAverage = 0, sumMinMaxAverage = 0;

            int period = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "PeriodMovingAverage", "180" ), inv );

            Sup.LogDebugMessage( "GenerateClashOfAverages : starting" );

            // First generate the general HTML and Graph chartcontainer stuff,
            // Then generate the dataseries.

            thisBuffer.AppendLine( "Highcharts.stockChart('chartcontainer', {" );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "ClashOfAverages", "Clash of Averages", true )}'," );
            thisBuffer.AppendLine( "     type: 'spline'" );
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
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "ClashAvTempAverage", "Average Temperature", true )} ({Sup.StationTemp.Text()})', opposite: false," );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  },{" );
            thisBuffer.AppendLine( $"    min: -3," );
            thisBuffer.AppendLine( $"    max: 3," );
            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "ClashAvDiffAverage", "Average Difference", true )} ({Sup.StationTemp.Text()})', opposite: false," );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }]," );
            thisBuffer.AppendLine( "  tooltip:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    headerFormat: '<span style=\"font-size: 10px\">{point.key}</span><table>'," );
            thisBuffer.AppendLine( $"    pointFormat: '<tr><td style=\"color: {{series.color}}; padding: 0\">{{series.name}}: </td><td style=\"padding: 0\"><b>{{point.y:.2f}} {Sup.StationTemp.Text()}</b></td></tr>'," );
            thisBuffer.AppendLine( "    footerFormat: '</table>'," );
            thisBuffer.AppendLine( "    shared: true," );
            thisBuffer.AppendLine( "    useHTML: true" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );

            StringBuilder ds = new StringBuilder( "  series: [{\n" ); // ds: DataString
            ds.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "ClashSeries1Name", "Cumulus Daily Average", true )}',\n" );
            ds.Append( "    yAxis: 0,\n" );
            ds.Append( "    data: [" );

            StringBuilder ma = new StringBuilder( "{\n" ); // ma: (max+min)/2")
            ma.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "ClashSeries2Name", "(max+min)/2", true )}',\n" );
            ma.Append( "    yAxis: 0,\n" );
            ma.Append( "    data: [" );

            StringBuilder di = new StringBuilder( "{\n" ); // di: 
            di.Append( $"    name: '{Sup.GetCUstringValue( "Graphs", "ClashSeries3Name", "Difference", true )}',\n" );
            di.Append( "    yAxis: 1,\n" );
            di.Append( "    data: [" );

            //      sumCumulusAverage = 0;

            foreach ( DayfileValue Day in ThisList )
            {
                ds.Append( $"[{CuSupport.DateTimeToJS( Day.ThisDate )},{Day.AverageTempThisDay.ToString( "F1", NumberFormatInfo.InvariantInfo )}]," );
                ma.Append( $"[{CuSupport.DateTimeToJS( Day.ThisDate )},{( ( Day.MaxTemp + Day.MinTemp ) / 2 ).ToString( "F2", NumberFormatInfo.InvariantInfo )}]," );
                di.Append( $"[{CuSupport.DateTimeToJS( Day.ThisDate )},{( Day.AverageTempThisDay - ( ( Day.MaxTemp + Day.MinTemp ) / 2 ) ).ToString( "F2", NumberFormatInfo.InvariantInfo )}]," );

                sumCumulusAverage += Day.AverageTempThisDay;
                sumMinMaxAverage += ( Day.MaxTemp + Day.MinTemp ) / 2;
            }

            // get rid of the last commas / newlines
            ds.Remove( ds.Length - 1, 1 );
            ma.Remove( ma.Length - 1, 1 );
            di.Remove( di.Length - 1, 1 );

            ds.Append( "]},\n" );
            ma.Append( "]},\n" );
            di.Append( "]},\n" );

            thisBuffer.AppendLine( ds.ToString() );
            thisBuffer.AppendLine( ma.ToString() );
            thisBuffer.AppendLine( di.ToString() );
            thisBuffer.AppendLine( "]});" );

            Sup.LogTraceInfoMessage( $"GenerateClashOfAverages: Sum CumulusAverage {sumCumulusAverage / ThisList.Count}" );
            Sup.LogTraceInfoMessage( $"GenerateClashOfAverages: Sum MinMaxAverage {sumMinMaxAverage / ThisList.Count}" );

            sumCumulusAverage = ThisList.Select( x => x.AverageTempThisDay ).Average();
            sumMinMaxAverage = ( ( ThisList.Select( x => x.MaxTemp ).Sum() + ThisList.Select( x => x.MinTemp ).Sum() ) / 2 ) / ThisList.Count;

            Sup.LogTraceInfoMessage( $"GenerateClashOfAverages: Sum CumulusAverage ThisList.Select(x => x.AverageTempThisDay).Average(): {sumCumulusAverage}" );
            Sup.LogTraceInfoMessage( $"GenerateClashOfAverages: Sum MinMaxAverage ((ThisList.Select(x => x.MaxTemp).Sum() + ThisList.Select(x => x.MinTemp).Sum())/2) / ThisList.Count: {sumMinMaxAverage}" );

            return;
        }

        #endregion
    }
}
