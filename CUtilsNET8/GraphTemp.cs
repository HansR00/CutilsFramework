/*
 * GraphTemp - Part of CumulusUtils
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
using System.Globalization;
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    partial class Graphx
    {
        private void GenMonthlyTempvsNOAAGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int counter;
            string NormalUsage;

            float[] MonthlyTempValues = new float[ 12 ];
            float[] NOAATempNorm = new float[ 12 ];
            float[] NOAATempStationAv = new float[ 12 ];
            float[] NOAATempStdDev = new float[ 12 ];

            List<float[]> YearValues = new List<float[]>();
            List<int> years = new List<int>();

            StringBuilder sb;

            Sup.LogDebugMessage( "GenMonthlyTempvsNOAAGraphData : starting" );

            NormalUsage = Sup.GetUtilsIniValue( "Graphs", "UseNormalTempReference", "Normal" );

            // Fill the Normal array - from tradition this is named after the NOAA but it can be from any Meteo organisation

            if ( NormalUsage.Equals( "Normal", CUtils.Cmp ) || NormalUsage.Equals( "Both", CUtils.Cmp ) )
            {
                StationNormal = true;

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                {
                    string iniKeyName = "NOAATempNorm" + Enum.GetNames( typeof( Months ) )[ i - 1 ];
                    string iniResult = Sup.GetCumulusIniValue( "NOAA", iniKeyName, "0.0" );
                    if ( iniResult.IndexOf( ',' ) > 0 )
                        iniResult = iniResult.Replace( ',', '.' );
                    NOAATempNorm[ i - 1 ] = (float) Convert.ToDouble( iniResult, CUtils.Inv );

                    Sup.LogTraceInfoMessage( $" Normal values: {iniKeyName} -> {NOAATempNorm[ i - 1 ].ToString( "F1", CUtils.Inv )}" );
                }
            }
            else
                StationNormal = false;

            // Use station Average
            if ( NormalUsage.Equals( "StationAverage", CUtils.Cmp ) || NormalUsage.Equals( "Both", CUtils.Cmp ) )
            {
                StationAverage = true;

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                {
                    if ( ThisList.Where( x => x.ThisDate.Month == i ).Any() )
                    {
                        NOAATempStationAv[ i - 1 ] = ThisList.Where( x => x.ThisDate.Month == i ).Select( x => x.AverageTempThisDay ).Average();
                        NOAATempStdDev[ i - 1 ] = ThisList.Where( x => x.ThisDate.Month == i ).Select( x => x.AverageTempThisDay ).StdDev();
                    }
                    else
                        NOAATempStationAv[ i - 1 ] = -1;

                    Sup.LogTraceInfoMessage( $" Station Average values: {Enum.GetNames( typeof( Months ) )[ i - 1 ]} -> {NOAATempStationAv[ i - 1 ].ToString( "F1", CUtils.Inv )}" );
                }
            }
            else
                StationAverage = false;

            Sup.LogTraceInfoMessage( "GenMonthlyTempvsNOAAGraphData : starting loop over DayfileValues" );

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                MonthlyTempValues = new float[ Enum.GetNames( typeof( Months ) ).Length ];
                YearValues.Add( MonthlyTempValues );
                years.Add( i );

                for ( int j = (int) Months.Jan; j <= (int) Months.Dec; j++ )
                {
                    //Now do the actual month work
                    if ( ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == j ).Any() )
                        MonthlyTempValues[ j - 1 ] = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == j ).Select( x => x.AverageTempThisDay ).Average();
                    else
                        MonthlyTempValues[ j - 1 ] = -1;
                }
            }

            Sup.LogTraceInfoMessage( "GenMonthlyTempvsNOAAGraphData : starting Generation" );

            // Now generate the script
            thisBuffer.AppendLine( "console.log('Monthly Temp vs NOAA Chart starting.');" );
            thisBuffer.AppendLine( "  chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "    chart:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      type: 'spline'," );
            thisBuffer.AppendLine( "      ignoreHiddenSeries: false" ); // scaling does not changes when a series is disabled
            thisBuffer.AppendLine( "    }," );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "    title:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"      text: '{Sup.GetCUstringValue( "Graphs", "MTTitle", "Monthly Average Temperature", true )} {Sup.StationTemp.Text()}'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    subtitle:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( $"     text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    xAxis:" );
            thisBuffer.AppendLine( "    {" );

            sb = new StringBuilder();
            sb.Append( "    categories: [" );

            for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                sb.Append( $"'{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i )}'," );

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( ']' );

            thisBuffer.AppendLine( sb.ToString() );

            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    yAxis:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      title:" );
            thisBuffer.AppendLine( "      {" );
            thisBuffer.AppendLine( $"        text: '{Sup.GetCUstringValue( "Graphs", "MTY-axis", "Average Temperature", true )} {Sup.StationTemp.Text()}'" );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    plotOptions:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      line:" );
            thisBuffer.AppendLine( "      {" );
            thisBuffer.AppendLine( "        dataLabels:" );
            thisBuffer.AppendLine( "        {" );
            thisBuffer.AppendLine( "          enabled: true" );
            thisBuffer.AppendLine( "        }," );
            thisBuffer.AppendLine( "        enableMouseTracking: false" );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );

            thisBuffer.AppendLine( "    series: [{" );

            sb = new StringBuilder();

            if ( StationNormal )
            {
                //Do the NORMAL series
                sb.Append( $"       name : '{Sup.GetCUstringValue( "Graphs", "MTNormal", "Normal", true )}', color: 'black', visible:true, zIndex:-1,\n" +
                          "       data : [" );
                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                    sb.Append( $"{NOAATempNorm[ i - 1 ].ToString( "F1", NumberFormatInfo.InvariantInfo )}," );

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
                    sb.Append( $"{NOAATempStationAv[ i - 1 ].ToString( "F1", NumberFormatInfo.InvariantInfo )}," );

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( ']' );
                sb.Append( "},{\n" );

                // Build the StdDev around Station Average series
                sb.Append( $"name: '{Sup.GetCUstringValue( "Graphs", "StdDev", "Standard Deviation", true )}', color: 'Gainsboro', visible:true, zIndex:-3, \n" );
                sb.Append( "marker: { enabled: false}," );
                sb.Append( "type: 'areasplinerange',\n" );
                sb.Append( "data: [" );

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                    sb.Append( $"[{( NOAATempStationAv[ i - 1 ] - NOAATempStdDev[ i - 1 ] ).ToString( "F1", NumberFormatInfo.InvariantInfo )}," +
                      $"{( NOAATempStationAv[ i - 1 ] + NOAATempStdDev[ i - 1 ] ).ToString( "F1", NumberFormatInfo.InvariantInfo )}]," );

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( "],\n" );
                sb.Append( "  showInLegend: true" );
            }

            bool PrintFirstAccolades = false;
            if ( StationNormal || StationAverage )
                PrintFirstAccolades = true;

            // Now build the data series
            counter = 0;
            foreach ( float[] ValuesList in YearValues )
            {
                if ( PrintFirstAccolades )
                    sb.Append( "},{\n" );
                else
                    PrintFirstAccolades = true;

                sb.Append( $"      name: '{years[ counter ].ToString( CultureInfo.CurrentCulture )}',\n" );

                if ( counter >= YearValues.Count - maxNrOfSeriesVisibleInGraph ) { sb.Append( $"      visible: true, \n" ); }
                else { sb.Append( "      visible: false,\n" ); }

                sb.Append( $"zIndex:{counter},\n" );
                sb.Append( $"      data: [" );

                for ( int i = 0; i < 12; i++ )
                {
                    if ( ValuesList[ i ] == -1 )
                        sb.Append( "null," );
                    else
                        sb.Append( $"{ValuesList[ i ].ToString( "F1", NumberFormatInfo.InvariantInfo )}," );
                }

                sb.Remove( sb.Length - 1, 1 ); //remove last comma
                sb.Append( ']' );
                counter++;
            }

            thisBuffer.AppendLine( $"{sb}\n    }}]" );
            thisBuffer.AppendLine( "  });" );

            return;
        }

        private void GenStackedWarmDaysGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int max25Count = 0, max30Count = 0, max35Count = 0, max40Count = 0;
            int Limit25C = 25, Limit30C = 30, Limit35C = 35, Limit40C = 40;

            StringBuilder sb = new StringBuilder();
            List<int> YearValue;
            List<List<int>> AllYears;

            Sup.LogDebugMessage( "GenStackedWarmDaysGraphData : starting" );

            if ( Sup.StationTemp.Dim == TempDim.fahrenheit ) // Fahrenheit
            {
                Limit25C = (int) Sup.StationTemp.Convert( TempDim.celsius, TempDim.fahrenheit, 25.0 );   //(25 * 1.8 + 32);
                Limit30C = (int) Sup.StationTemp.Convert( TempDim.celsius, TempDim.fahrenheit, 30.0 );   //(30 * 1.8 + 32);
                Limit35C = (int) Sup.StationTemp.Convert( TempDim.celsius, TempDim.fahrenheit, 35.0 );   //(35 * 1.8 + 32);
                Limit40C = (int) Sup.StationTemp.Convert( TempDim.celsius, TempDim.fahrenheit, 40.0 );   //(40 * 1.8 + 32);
            }

            AllYears = new List<List<int>>();

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                if ( ThisList.Where( x => x.ThisDate.Year == i ).Any() )
                {
                    max40Count = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.MaxTemp >= Limit40C ).Count();
                    max35Count = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.MaxTemp >= Limit35C && x.MaxTemp < Limit40C ).Count();
                    max30Count = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.MaxTemp >= Limit30C && x.MaxTemp < Limit35C ).Count();
                    max25Count = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.MaxTemp >= Limit25C && x.MaxTemp < Limit30C ).Count();
                }

                // and write the values to the list
                YearValue = new List<int>
                {
                  i,
                  max25Count,
                  max30Count,
                  max35Count,
                  max40Count
                };

                AllYears.Add( YearValue );
            }

            thisBuffer.AppendLine( "console.log('Warmer Days Chart starting.');" );
            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  chart:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    type: 'column'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "WDTitle", "Warmer Days", true )} {Sup.StationTemp.Text()}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"'{Year[ (int) WarmDayType.Zero ]}'," );
            }

            sb.Remove( sb.Length - 1, 1 );

            thisBuffer.AppendLine( $"    categories: [{sb}]" );
            thisBuffer.AppendLine( "   }," );
            thisBuffer.AppendLine( "   yAxis:" );
            thisBuffer.AppendLine( "   {" );
            thisBuffer.AppendLine( "     min: 0," );
            thisBuffer.AppendLine( "     title:" );
            thisBuffer.AppendLine( "     {" );
            thisBuffer.AppendLine( $"       text: '{Sup.GetCUstringValue( "Graphs", "WDY-axisTitle", "Number of Days", true )}'" );
            thisBuffer.AppendLine( "     }," );
            thisBuffer.AppendLine( "     stackLabels:" );
            thisBuffer.AppendLine( "     {" );
            thisBuffer.AppendLine( "       enabled: true," );
            thisBuffer.AppendLine( "       style:" );
            thisBuffer.AppendLine( "       {" );
            thisBuffer.AppendLine( "         fontWeight: 'bold'," );
            thisBuffer.AppendLine( "         color: (" );
            thisBuffer.AppendLine( "           Highcharts.defaultOptions.title.style &&" );
            thisBuffer.AppendLine( "           Highcharts.defaultOptions.title.style.color" );
            thisBuffer.AppendLine( "           ) || 'gray'" );
            thisBuffer.AppendLine( "        }" );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    tooltip:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      headerFormat: '<b>{point.x}</b><br/>'," );
            thisBuffer.AppendLine( "      pointFormat: '{series.name}: {point.y}<br/>Total: {point.stackTotal}'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    plotOptions:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      column:" );
            thisBuffer.AppendLine( "      {" );
            thisBuffer.AppendLine( "        stacking: 'normal'," );
            thisBuffer.AppendLine( "        dataLabels:" );
            thisBuffer.AppendLine( "        {" );
            thisBuffer.AppendLine( "          enabled: true" );
            thisBuffer.AppendLine( "        }" );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    series: [" );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"{Year[ (int) WarmDayType.Plus40Day ]}," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"      {{name: '>{Limit40C}', color: 'black', data:[{sb}]}}," );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"{Year[ (int) WarmDayType.Plus35Day ]}," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"      {{name: '>{Limit35C}', color: 'magenta', data:[{sb}]}}," );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"{Year[ (int) WarmDayType.Plus30Day ]}," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"      {{name: '>{Limit30C}', color: 'red', data:[{sb}]}}," );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"{Year[ (int) WarmDayType.Plus25Day ]}," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"      {{name: '>{Limit25C}', color: 'orange', data:[{sb}]}}" );

            thisBuffer.AppendLine( "     ]" );
            thisBuffer.AppendLine( "});" );
        }

        private void GenStackedFrostDaysGraphData( List<DayfileValue> ThisList, StringBuilder thisBuffer )
        {
            int FrostDays = 0, IceDays = 0;
            int ZeroValue = 0; // for Celsius, gets value for Faherenheit llater on

            StringBuilder sb = new StringBuilder();
            List<int> YearValue;
            List<List<int>> AllYears;

            Sup.LogDebugMessage( "GenStackedFrostDaysGraphData : starting" );

            // Fahrenheit
            if ( Sup.StationTemp.Dim == TempDim.fahrenheit ) ZeroValue = 32;

            AllYears = new List<List<int>>();

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                if ( ThisList.Where( x => x.ThisDate.Year == i ).Any() )
                {
                    FrostDays = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.MaxTemp >= ZeroValue && x.MinTemp < ZeroValue ).Count();
                    IceDays = ThisList.Where( x => x.ThisDate.Year == i ).Where( x => x.MaxTemp <= ZeroValue ).Count();
                }

                // and write the values to the list
                YearValue = new List<int>
                {
                  i,
                  FrostDays,
                  IceDays
                };

                AllYears.Add( YearValue );
            }

            thisBuffer.AppendLine( "console.log('Frost Days Chart starting.');" );
            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  chart:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    type: 'column'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "FrostDaysTitle", "Frost and Ice Days", true )} {Sup.StationTemp.Text()}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station")}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"'{Year[ (int) ColdDayType.Zero ]}'," );
            }

            sb.Remove( sb.Length - 1, 1 );

            thisBuffer.AppendLine( $"    categories: [{sb}]" );
            thisBuffer.AppendLine( "   }," );
            thisBuffer.AppendLine( "   yAxis:" );
            thisBuffer.AppendLine( "   {" );
            thisBuffer.AppendLine( "     min: 0," );
            thisBuffer.AppendLine( "     title:" );
            thisBuffer.AppendLine( "     {" );
            thisBuffer.AppendLine( $"       text: '{Sup.GetCUstringValue( "Graphs", "FrostDaysAxisTitle", "Number of Days", true )}'" );
            thisBuffer.AppendLine( "     }," );
            thisBuffer.AppendLine( "     stackLabels:" );
            thisBuffer.AppendLine( "     {" );
            thisBuffer.AppendLine( "       enabled: true," );
            thisBuffer.AppendLine( "       style:" );
            thisBuffer.AppendLine( "       {" );
            thisBuffer.AppendLine( "         fontWeight: 'bold'," );
            thisBuffer.AppendLine( "         color: (" );
            thisBuffer.AppendLine( "           Highcharts.defaultOptions.title.style &&" );
            thisBuffer.AppendLine( "           Highcharts.defaultOptions.title.style.color" );
            thisBuffer.AppendLine( "           ) || 'gray'" );
            thisBuffer.AppendLine( "        }" );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    tooltip:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      headerFormat: '<b>{point.x}</b><br/>'," );
            thisBuffer.AppendLine( "      pointFormat: '{series.name}: {point.y}<br/>Total: {point.stackTotal}'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    plotOptions:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      column:" );
            thisBuffer.AppendLine( "      {" );
            thisBuffer.AppendLine( "        stacking: 'normal'," );
            thisBuffer.AppendLine( "        dataLabels:" );
            thisBuffer.AppendLine( "        {" );
            thisBuffer.AppendLine( "          enabled: true" );
            thisBuffer.AppendLine( "        }" );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    series: [" );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"{Year[ (int) ColdDayType.FrostDay ]}," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"      {{name: '{Sup.GetCUstringValue( "Graphs", "Frost Days", "Frost Days", true )}', color: 'LightBlue', data:[{sb}]}}," );

            sb.Clear();
            foreach ( List<int> Year in AllYears )
            {
                sb.Append( $"{Year[ (int) ColdDayType.IceDay ]}," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"      {{name: '{Sup.GetCUstringValue( "Graphs", "Ice Days", "Ice Days", true )}', color: 'Blue', data:[{sb}]}}" );

            thisBuffer.AppendLine( "     ]" );
            thisBuffer.AppendLine( "});" );
        }

        private void GenerateHeatMap( List<DayfileValue> Thislist, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();

            Sup.LogDebugMessage( "Generate Heat Map Starting" );

            thisBuffer.AppendLine( "console.log('Heatmap Chart starting.');" );
            thisBuffer.AppendLine( "chart = thisHeatmap = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'heatmap'" );
            thisBuffer.AppendLine( "}," );

            if ( UseHighchartsBoostModule )
            {
                thisBuffer.AppendLine( "boost:" );
                thisBuffer.AppendLine( "{" );
                thisBuffer.AppendLine( "  useGPUTranslations: true" );
                thisBuffer.AppendLine( "}," );
            }

            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "HMTitle", "Heat Map", true )}'," );
            thisBuffer.AppendLine( "  align: 'left'," );
            thisBuffer.AppendLine( "  x: 40" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "xAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'lineair'," );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "HMXaxisTitle", "Daynumber", true )}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  min: 1," );
            thisBuffer.AppendLine( "  max: 366," );
            thisBuffer.AppendLine( "  labels:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    align: 'left'," );
            thisBuffer.AppendLine( "    x: 5," );
            thisBuffer.AppendLine( "    y: 14," );
            thisBuffer.AppendLine( "    format: '{value}'" ); // Day number of year
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  tickLength: 16" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "yAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'lineair'," );
            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "HMYaxisTitle", "Years", true )}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  labels:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    format: '{value}'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  tickWidth: 1," );
            thisBuffer.AppendLine( "  tickInterval: 1," );
            thisBuffer.AppendLine( "  startOnTick: false," );
            thisBuffer.AppendLine( "  endOnTick: false," );
            thisBuffer.AppendLine( $"  min: {( SplitHeatmapPages ? CUtils.YearMax - HeatmapNrOfYearsPerPage : CUtils.YearMin )}," );
            thisBuffer.AppendLine( $"  max: {CUtils.YearMax}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "colorAxis:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  stops: " );
            thisBuffer.AppendLine( "  [" );
            thisBuffer.AppendLine( "    [0, '#0000ff']," );
            thisBuffer.AppendLine( "    [0.2, '#4d4dff']," );
            thisBuffer.AppendLine( "    [0.5, '#fffbbc']," );
            thisBuffer.AppendLine( "    [0.8, '#ff4d4d']," );
            thisBuffer.AppendLine( "    [1, '#ff0000']" );
            thisBuffer.AppendLine( "  ]," );

            if ( Sup.StationTemp.Dim == TempDim.celsius )
            {
                thisBuffer.AppendLine( $"  min: -20," );
                thisBuffer.AppendLine( $"  max: 50," );
            }
            else // Fahrenheit
            {
                thisBuffer.AppendLine( $"  min: 0," );
                thisBuffer.AppendLine( $"  max: 100," );
            }

            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( "  boostThreshold: 100," );
            thisBuffer.AppendLine( "  lineWidth: 0," );

            // Note that the thousandsSep is set to empty string in the HighchartsLanguage.js because we want '2023' and not '2 023'
            //
            thisBuffer.AppendLine( "  tooltip: {" );
            thisBuffer.AppendLine( $"    headerFormat: '<b>{Sup.GetCUstringValue( "Graphs", "HMTTtemperatureTXT", "Temperature", true )}:</b><br/>'," );
            thisBuffer.AppendLine( $"    pointFormatter() {{ return DayNumber2Date(this.x, this.y) + ': ' + this.value + ' {Sup.StationTemp.Text()}' }}" );
            thisBuffer.AppendLine( "  }," );

            thisBuffer.AppendLine( "data: [" );

            sb.Clear();
            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                Sup.LogTraceInfoMessage( $"Generating Heat Map data, doing year {i}" );

                List<DayfileValue> yearlist = Thislist.Where( x => x.ThisDate.Year == i ).ToList();

                foreach ( DayfileValue day in yearlist )
                {
                    sb.Append( $"[{day.ThisDate.DayOfYear},{day.ThisDate.Year},{day.MaxTemp.ToString( "F2", CUtils.Inv )}]," );
                }
            }
            sb.Remove( sb.Length - 1, 1 );

            thisBuffer.AppendLine( $"{sb}" );
            thisBuffer.AppendLine( "]" );
            thisBuffer.AppendLine( "}]" ); // End data);
            thisBuffer.AppendLine( "});" ); // End chart);
        }

        private void GenerateYearTempStatistics( List<DayfileValue> Thislist, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();
            ;

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> mintemp = new List<float>();
            List<float> maxtemp = new List<float>();

            Sup.LogDebugMessage( "Generate GenerateYearTempStatistics Starting" );

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                List<DayfileValue> yearlist = Thislist.Where( x => x.ThisDate.Year == i ).ToList();

                if ( yearlist.Count == 0 )
                    continue;

                Sup.LogTraceInfoMessage( $"Generating Year Temp Statistics, doing year {i}" );

                years.Add( i );
                average.Add( yearlist.Select( x => x.AverageTempThisDay ).Average() );
                stddev.Add( yearlist.Select( x => x.AverageTempThisDay ).StdDev() );
                mintemp.Add( yearlist.Select( x => x.MinTemp ).Min() );
                maxtemp.Add( yearlist.Select( x => x.MaxTemp ).Max() );
            }

            thisBuffer.AppendLine( "console.log('Year Temp Stats Chart starting.');" );
            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YTSTitle", "Year Temperature Statistics", true )}' " );
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
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YTSTempRange", "Temperature range", true )} ({Sup.StationTemp.Text()})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: {minTemp.ToString( "F2", CUtils.Inv )}," );
            thisBuffer.AppendLine( $"  max: {maxTemp.ToString( "F2", CUtils.Inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: '{Sup.StationTemp.Text()}'" );
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
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.StationTemp.Text()}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YTSTempRange", "Temperature range", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(19, 114, 248, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba(242, 70, 36, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();
            for ( int i = 0; i < years.Count; i++ )
            {
                sb.Append( $"[{mintemp[ i ].ToString( "F2", CUtils.Inv )},{maxtemp[ i ].ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YTSAverageTemp", "Average Temperature", true )}'," );
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
                sb.Append( $"[{( average[ i ] - stddev[ i ] ).ToString( "F2", CUtils.Inv )},{( average[ i ] + stddev[ i ] ).ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );
        }

        private void GenerateYearMonthTempStatistics( List<DayfileValue> Thislist, Months thisMonth, StringBuilder thisBuffer )
        {
            StringBuilder sb = new StringBuilder();

            List<int> years = new List<int>();
            List<float> average = new List<float>();
            List<float> stddev = new List<float>();
            List<float> mintemp = new List<float>();
            List<float> maxtemp = new List<float>();

            //Sup.LogDebugMessage( "Generate GenerateYearMonthTempStatistics Start" );

            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
            {
                List<DayfileValue> yearmonthlist = Thislist.Where( x => x.ThisDate.Year == i ).Where( x => x.ThisDate.Month == (int) thisMonth ).ToList();

                Sup.LogTraceVerboseMessage( $"Generating Year Month Temp Statistics, doing year {i} and month {thisMonth}" );

                if ( yearmonthlist.Any() )
                {
                    years.Add( i );
                    average.Add( yearmonthlist.Select( x => x.AverageTempThisDay ).Average() );
                    stddev.Add( yearmonthlist.Select( x => x.AverageTempThisDay ).StdDev() );
                    mintemp.Add( yearmonthlist.Select( x => x.MinTemp ).Min() );
                    maxtemp.Add( yearmonthlist.Select( x => x.MaxTemp ).Max() );
                }
            }

            if ( years.Count == 0 )
            {
                thisBuffer.AppendLine( "console.log('Not enough data - choose another month');" );
                thisBuffer.AppendLine( "window.alert('Not enough data - choose another month');" );
                return; // We're done, nothing here
            }

            thisBuffer.AppendLine( "console.log('Monthly Temp Stats Chart starting.');" );
            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "chart:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  type: 'columnrange'" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "title:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  text: '{Sup.GetCUstringValue( "Graphs", "YMTSTitle", "Year Temperature Statistics for", true )} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( (int) thisMonth )}' " );
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
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "YTSTempRange", "Temperature range", true )} ({Sup.StationTemp.Text()})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  min: {minTemp.ToString( "F2", CUtils.Inv )}," );
            thisBuffer.AppendLine( $"  max: {maxTemp.ToString( "F2", CUtils.Inv )}" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "tooltip:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( $"  valueSuffix: '{Sup.StationTemp.Text()}'" );
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
            thisBuffer.AppendLine( $"      format: '{{y}} {Sup.StationTemp.Text()}'" );
            thisBuffer.AppendLine( "    }" );
            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "legend:" );
            thisBuffer.AppendLine( "{" );
            thisBuffer.AppendLine( "  enabled: true" );
            thisBuffer.AppendLine( "}," );
            thisBuffer.AppendLine( "series: [{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YTSTempRange", "Temperature range", true )}'," );
            thisBuffer.AppendLine( "zIndex: 3," );
            thisBuffer.AppendLine( "color:" );
            thisBuffer.AppendLine( "{" );

            thisBuffer.AppendLine( "  linearGradient: { x1: 0, x2: 0, y1: 1, y2: 0 }," );
            thisBuffer.AppendLine( "  stops: [" );
            thisBuffer.AppendLine( "    [0, 'rgba(19, 114, 248, .9)']," );
            thisBuffer.AppendLine( "    [1, 'rgba(242, 70, 36, .9)']" );
            thisBuffer.AppendLine( "  ]" );
            thisBuffer.AppendLine( "}," );

            thisBuffer.AppendLine( "  pointWidth: 6," );
            thisBuffer.AppendLine( "  data: [" );

            sb.Clear();

            for ( int i = 0; i < years.Count; i++ )
                sb.Append( $"[{mintemp[ i ].ToString( "F2", CUtils.Inv )},{maxtemp[ i ].ToString( "F2", CUtils.Inv )}]," );

            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]},{" );
            thisBuffer.AppendLine( $"  name: '{Sup.GetCUstringValue( "Graphs", "YTSAverageTemp", "Average Temperature", true )}'," );
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
                sb.Append( $"[{( average[ i ] - stddev[ i ] ).ToString( "F2", CUtils.Inv )},{( average[ i ] + stddev[ i ] ).ToString( "F2", CUtils.Inv )}]," );
            }
            sb.Remove( sb.Length - 1, 1 );
            thisBuffer.AppendLine( $"{sb}" );

            thisBuffer.AppendLine( "  ]}]" );
            thisBuffer.AppendLine( "});" );
        }

        //  // May want see:
        //  // https://stackoverflow.com/questions/17821828/calculating-heat-map-colours
        //  // https://javascript.plainenglish.io/creating-color-gradients-for-heat-maps-with-vanilla-javascript-c8d62bdd648e
        //  // Changed to adjust for a max/min value
    }
}
