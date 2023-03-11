/*
 * YADR - Part of CumulusUtils
 *
 * © Copyright 2019-2023 Hans Rottier <hans.rottier@gmail.com>
 *
 * The code of CumulusUtils is public domain and distributed under the  
 * Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI .NET Framework 4.8
 *              Initial release: pwsFWI                 (version 1.0)
 *                               Website Generator      (version 3.0)
 *                               ChartsCompiler         (version 5.0)
 *                               Maintenance releases   (version 6.x)
 *              Startdate : 16 november 2021 start of conversion to .NET 5, 6 and 7
 *              
 * Environment: Raspberry Pi 3B+ and up
 *              Raspberry Pi OS  for testruns
 *              C# / Visual Studio / Windows for development
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
    // Yet Another Dafile Reader (Utils - Variation and inspired by Beteljuice and Murry Conarroe of Wildwood Weather)
    // Needed to get rid of PHP code

    public class Yadr : IDisposable
    {
        readonly string[] Months;

        readonly float[] TempRanges = new float[ 13 ];
        readonly string[] TempColorFormat = new string[] { "#0000cc", "#0066cc", "#0099cc", "#00ccff", "#00ffcc", "#00ff99", "#ccff00", "#ffff66",
                                                        "#ffcc00","#ff9933","#ff6600","#ff3300","#cc3300" };

        readonly float[] RainRanges = new float[ 13 ];
        readonly string[] RainColorFormat = new string[] { "#CCFFFF", "#99FFCC", "#99FF99", "#99FF66", "#99FF00", "#99CC00", "#CCCC00",
                                                        "#FFCC00","#FF9900","#FF6600","#FF3300","#FF0000", "#cc3300"};

        readonly float[] WindRanges = new float[ 13 ] { 1, 6, 12, 20, 29, 39, 50, 62, 75, 89, 103, 118, 118 };
        readonly string[] WindColorFormat = new string[] { "#ffffff", "#ccffff", "#99ffcc", "#99ff99", "#99ff66", "#99ff00", "#99cc00", "#cccc00",
                                                       "#ffcc00","#ff9900","#ff6600","#ff3300","#ff0000" };

        readonly string[] WindDescr = new string[ 13 ];

        readonly float[] WindrunRanges = new float[ 13 ];
        readonly string[] WindrunColorFormat = new string[] { "#ffffff", "#ccffff", "#99ffcc", "#99ff99", "#99ff66", "#99ff00", "#99cc00", "#cccc00",
                                                       "#ffcc00","#ff9900","#ff6600","#ff3300","#ff0000" };
        //    readonly string[] WindrunColorFormat = new string[] { "#0000cc", "#0066cc", "#0099cc", "#00ccff", "#00ffcc", "#00ff99",
        //                                                        "#ccff00", "#ffff66", "#ffcc00","#ff9933","#ff6600","#ff3300","#cc3300" };

        readonly float[] PressRanges = new float[ 13 ];
        readonly string[] PressColorFormat = new string[] { "#0000cc", "#0066cc", "#0099cc", "#00ccff", "#00ffcc", "#00ff99",
                                                        "#ccff00", "#ffff66", "#ffcc00","#ff9933","#ff6600","#ff3300","#cc3300" };

        readonly float[] HumRanges = new float[ 13 ];
        readonly string[] HumColorFormat = new string[] {"#cc3300", "#ff3300" , "#ff6600", "#ff9933" , "#ffcc00", "#ffff66",
                                                     "#ccff00", "#00ff99", "#00ffcc","#00ccff","#0099cc","#0066cc","#0000cc" };

        readonly float Tempbase = -15, Tempstep = 5;                    // in Celsius
        readonly float Rainbase = 2, Rainstep = 2;                      // in mm
        readonly float Pressbase = 950, Pressstep = (float) 100 / 13;    // in hPa
        readonly float Windrunbase, Windrunstep;                        // in km
        readonly float Humbase = 0, Humstep = (float) 100 / 13;            // in %

        readonly float InvalidValue = -1000;

        readonly CuSupport Sup;

        #region Constructor
        public Yadr( CuSupport s )
        {
            // Constructor: Initialise everything
            int i;

            Sup = s;
            Sup.LogDebugMessage( $"Main CmulusUtils: Yadr Constructor Start" );

            //Windrunbase = Windrunstep = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "WindrunClassWidth", "75" ), CultureInfo.InvariantCulture );
            Windrunbase = Windrunstep = 75;

            // we have 13 classes in the table. If it becomes more or less we have to adjust tables
            for ( i = 0; i < 13; i++ )
                TempRanges[ i ] = (float) Sup.StationTemp.Convert( TempDim.celsius, Sup.StationTemp.Dim, Tempbase + i * Tempstep );
            for ( i = 0; i < 13; i++ )
                RainRanges[ i ] = (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, Rainbase + i * Rainstep );
            for ( i = 0; i < 13; i++ )
                WindRanges[ i ] = (float) Sup.StationWind.Convert( WindDim.kmh, Sup.StationWind.Dim, WindRanges[ i ] );
            for ( i = 0; i < 13; i++ )
                WindrunRanges[ i ] = (float) Sup.StationDistance.Convert( DistanceDim.kilometer, Sup.StationDistance.Dim, Windrunbase + i * Windrunstep );
            for ( i = 0; i < 13; i++ )
                PressRanges[ i ] = (float) Sup.StationPressure.Convert( PressureDim.hectopascal, Sup.StationPressure.Dim, Pressbase + i * Pressstep );

            for ( i = 0; i < 13; i++ )
                HumRanges[ i ] = Humbase + i * Humstep;

            WindDescr[ 0 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc0", "Calm", false );
            WindDescr[ 1 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc1", "Light air", false );
            WindDescr[ 2 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc2", "Light breeze", false );
            WindDescr[ 3 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc3", "Gentle breeze", false );
            WindDescr[ 4 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc4", "Moderate breeze", false );
            WindDescr[ 5 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc5", "Fresh breeze", false );
            WindDescr[ 6 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc6", "Strong breeze", false );
            WindDescr[ 7 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc7", "High wind, Moderate gale, Near gale", false );
            WindDescr[ 8 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc8", "Gale, Fresh gale", false );
            WindDescr[ 9 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc9", "Strong gale", false );
            WindDescr[ 10 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc10", "Storm, Whole gale", false );
            WindDescr[ 11 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc11", "Violent storm", false );
            WindDescr[ 12 ] = Sup.GetCUstringValue( "Yadr", "BeaufortDesc12", "Hurricane force", false );

            // Create the monthnames for current culture
            Months = new string[ 12 ];

            for ( i = 0; i < 12; i++ )
                Months[ i ] = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i + 1 );

        }
        #endregion

        #region Driver procedure GenerateYadr
        public void GenerateYadr( List<DayfileValue> Thislist )
        {
            Sup.LogDebugMessage( $" Generate Yadr Start" );

            // Loop over years
            // First create the datafiles per year per parameter. So e.g. for every year available create a yyyyTemperature.txt, then yyyyHumidity.txt
            // etc... in first instance we just create all data, later we may shuffle around to get the display right

            int YearMax = Thislist.Select( x => x.ThisDate.Year ).Max();
            int YearMin = Thislist.Select( x => x.ThisDate.Year ).Min();

            if ( CUtils.Thrifty )
            {
                if ( CUtils.RunStarted.DayOfYear == 2 )
                    GenerateYadrTxtfile( YearMin, YearMax );

                // So now, do only the current year
                YearMin = YearMax;
            }
            else
                GenerateYadrTxtfile( YearMin, YearMax );


            GenerateYadrTempData( YearMin, YearMax, Thislist );
            GenerateYadrRainData( YearMin, YearMax, Thislist );
            GenerateYadrWindData( YearMin, YearMax, Thislist );
            GenerateYadrWindrunData( YearMin, YearMax, Thislist );
            GenerateYadrPressionData( YearMin, YearMax, Thislist );
            GenerateYadrHumidityData( YearMin, YearMax, Thislist );
        }// End Generate Yadr
        #endregion

        #region GenerateYadrTxtfile (Menu)
        void GenerateYadrTxtfile( int yearMin, int yearMax )
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}Yadr.txt", false, Encoding.UTF8 ) )
            {
                of.WriteLine( $"{Sup.GenjQueryIncludestring()}" );
                of.WriteLine( "<script>" );
                of.WriteLine( "console.log( 'Module YADR...' );" );

                of.WriteLine( "$(function() {" );
                of.WriteLine( "  $('#year').change(function() {" );
                of.WriteLine( "    handleChange();" );
                of.WriteLine( "  });" );
                of.WriteLine( "  $('#func').change(function() {" );
                of.WriteLine( "    handleChange();" );
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRNext').click(function() {" );
                of.WriteLine( "    $('#year option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "    handleChange();" );
                of.WriteLine( "  });" );
                of.WriteLine( "  $('#YRPrev').click(function() {" );
                of.WriteLine( "    $('#year option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "    handleChange();" );
                of.WriteLine( "  });" );
                of.WriteLine( "  handleChange();" );
                of.WriteLine( "});" );

                of.WriteLine( "function handleChange()" );
                of.WriteLine( "{" );
                of.WriteLine( "  let w1 = $('#year').val();" );
                of.WriteLine( "  let w2 = $('#func').val();" );

                of.WriteLine( "  loadYadrTable(w2+w1);" );
                of.WriteLine( "};" );

                of.WriteLine( "function loadYadrTable( FuncYear)" );
                of.WriteLine( "{" );
                of.WriteLine( "  $.ajax({" );
                of.WriteLine( "    url: 'Yadr' + FuncYear + '.txt'," );
                of.WriteLine( "    timeout: 2000," );
                of.WriteLine( "    cache: false," );
                of.WriteLine( "    headers:{'Access-Control-Allow-Origin': '*'}," );
                of.WriteLine( "    crossDomain: true" );
                of.WriteLine( "  }).fail( function (jqXHR, textStatus, errorThrown) {" );
                of.WriteLine( "      console.log( 'loadYadrTable: ' + textStatus + ' : ' + errorThrown );" );
                of.WriteLine( "  }).done( function (response, responseStatus) {" );
                of.WriteLine( "      $( '#Table' ).html( response );" );
                of.WriteLine( "  });" );
                of.WriteLine( "};" );

                of.WriteLine( "</script>" );

                // Do the CSS
                of.WriteLine( "<style>" );

                const int FontPercentage = 80;

                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .CUtable{" );
                of.WriteLine( "  border-collapse: collapse;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "   text-align: center;" );
                of.WriteLine( "   width:100%;" );
                of.WriteLine( "   max-width:1000px;" );
                of.WriteLine( "   margin: auto;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .labels {" );
                of.WriteLine( "  background-color: #777777;" );
                of.WriteLine( "  color: #cccccc;" );
                of.WriteLine( "  border: 1px solid #888888;" );
                of.WriteLine( "}" );


                of.WriteLine( "#report .reportday {" );
                of.WriteLine( "    background-color: #999999;" );
                of.WriteLine( "    border: 1px solid #888888;" );
                of.WriteLine( $"    font-size: {FontPercentage}%;" );
                of.WriteLine( "}" );


                of.WriteLine( "#report .reportttl {" );
                of.WriteLine( "  background-color: #777777;" );
                of.WriteLine( "  color: #cccccc;" );
                of.WriteLine( "  border: 1px solid #888888;" );
                of.WriteLine( $"    font-size: {FontPercentage}%;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .separator {" );
                of.WriteLine( "  background-color: #ffffff; " );
                of.WriteLine( "}" );

                of.WriteLine( "#report .levelT {" );
                of.WriteLine( "    border: 1px solid #888888;" );
                of.WriteLine( $"    font-size: {FontPercentage}%;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .beaufort {" );
                of.WriteLine( "    border: 1px solid #888888;" );
                of.WriteLine( $"    font-size: {FontPercentage}%;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .levelR {" );
                of.WriteLine( "    border: 1px solid #888888;" );
                of.WriteLine( $"    font-size: {FontPercentage}%;" );
                of.WriteLine( "}" );

                of.WriteLine( ".buttonFat {border-radius: 4px; margin-right:10px; margin-left:10px; }" );
                of.WriteLine( ".buttonSlim {border-radius: 4px;}" );

                of.WriteLine( "</style>" );

                of.WriteLine( "<div id=report><br/>" );
                of.WriteLine( "<p style='text-align:center;'>" );

                of.WriteLine( $"<input type='button' class=buttonFat id='YRPrev' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );

                of.WriteLine( "<select id='year'>" );

                // loop over years for the options
                for ( int i = yearMin; i <= yearMax; i++ )
                {
                    of.WriteLine( $"<option value='{i}' {( i == yearMax ? "selected" : "" )}>{i}</option>" );
                }

                of.WriteLine( "</select>" );

                of.WriteLine( "<select id='func'>" );
                of.WriteLine( $"<option value='Temp' selected>{Sup.GetCUstringValue( "Yadr", "OptionTemp", "Temperature", false )}</option>" );
                of.WriteLine( $"<option value='Rain'>{Sup.GetCUstringValue( "Yadr", "OptionRain", "Rain", false )}</option>" );
                of.WriteLine( $"<option value='Wind'>{Sup.GetCUstringValue( "Yadr", "OptionWind", "Wind Speed", false )}</option>" );
                of.WriteLine( $"<option value='Windrun'>{Sup.GetCUstringValue( "Yadr", "OptionWindRun", "Wind Run", false )}</option>" );
                of.WriteLine( $"<option value='Press'>{Sup.GetCUstringValue( "Yadr", "OptionPression", "Pressure", false )}</option>" );
                of.WriteLine( $"<option value='Hum'>{Sup.GetCUstringValue( "Yadr", "OptionHumidity", "Humidity", false )}</option>" );
                of.WriteLine( "</select>" );

                of.WriteLine( $"<input type='button' class=buttonFat id='YRNext' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );

                of.WriteLine( "</p>" );
                of.WriteLine( "<div id='Table'></div>" ); // placeholder for table

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // from div report
            }// end using streamwriter
        }
        #endregion

        #region YADR Temperature
        void GenerateYadrTempData( int YearMin, int YearMax, List<DayfileValue> Thislist )
        {
            for ( int thisYear = YearMin; thisYear <= YearMax; thisYear++ )
            {
                StringBuilder sb = new StringBuilder();
                // Part I:

                Sup.LogTraceInfoMessage( $"GenerateYadrTempData: Looping over years, doing year {thisYear}" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}YadrTemp{thisYear}.txt", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<h3 style='text-align:center;'>{Sup.GetCUstringValue( "Yadr", "TempTitle", "Daily Values Temperature", false )} " +
                      $"({Sup.StationTemp.Text()})</h3><br/>" );
                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( $"  <th rowspan='2' class='labels'  style='width:7%;'>{Sup.GetCUstringValue( "Yadr", "DayText", "Day", false )}</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th colspan='2' class='labels' style='width:6%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    of.WriteLine( "<tr>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:2%;'>" +
   $"{Sup.GetCUstringValue( "Yadr", "HighShortText", "Hi", false )}</th><th class='labels' style='width:2%;'>" +
   $"{Sup.GetCUstringValue( "Yadr", "LowShortText", "Lo", false )}</th>" );
                    of.WriteLine( "</tr>\n" );

                    for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                    {
                        List<DayfileValue> DayList = Thislist.Where( x => x.ThisDate.Day == thisDay ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        sb.Clear();
                        sb.Append( $"<tr>" );
                        sb.Append( $"<td class=\"reportttl\">{thisDay}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 )
                            {
                                sb.Append( "<td class=\"reportday\">---</td><td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td {GetTempColourFormat( currentItem.MaxTemp )}>{Temp.Format( currentItem.MaxTemp )}</td>" );
                                sb.Append( $"<td {GetTempColourFormat( currentItem.MinTemp )}>{Temp.Format( currentItem.MinTemp )}</td>" );
                            }
                        }// for loop over months

                        sb.Append( thisDay.ToString( $"</tr>\n" ) );
                        of.WriteLine( $"{sb}" );
                    }// for loop over Days  

                    of.WriteLine( $"</tbody></table>" );

                    //
                    // That was the first part of the table, now the statistics
                    //
                    // Part II:


                    of.WriteLine( "" );
                    of.WriteLine( "<table class='CUtable'><tbody>" );
                    of.WriteLine( "<tr><td class='separator' colspan='25'>&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><th class='labels' style='width:7%;'>&nbsp;</th>" );

                    // Write the tableheaders for the months  
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th colspan ='2'  class='labels' style='width:6%;'>{Months[ i ]}</th>" );

                    float[,] values = new float[ 5, 12 ];

                    // Now do the loop again for the max, max avg, avg, min avg, min statistics
                    for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                    {
                        List<DayfileValue> StatisticsList = Thislist.Where( x => x.ThisDate.Month == thisMonth + 1 ).Where( x => x.ThisDate.Year == thisYear ).ToList(); //.Max().MaxTemp;

                        if ( StatisticsList.Count > 0 )
                        {
                            values[ 0, thisMonth ] = StatisticsList.Select( x => x.MaxTemp ).ToList().Max();
                            values[ 1, thisMonth ] = StatisticsList.Select( x => x.MaxTemp ).ToList().Average();
                            values[ 2, thisMonth ] = StatisticsList.Select( x => x.AverageTempThisDay ).ToList().Average();
                            values[ 3, thisMonth ] = StatisticsList.Select( x => x.MinTemp ).ToList().Average();
                            values[ 4, thisMonth ] = StatisticsList.Select( x => x.MinTemp ).ToList().Min();
                        }
                        else
                        {
                            values[ 0, thisMonth ] = InvalidValue;
                            values[ 1, thisMonth ] = InvalidValue;
                            values[ 2, thisMonth ] = InvalidValue;
                            values[ 3, thisMonth ] = InvalidValue;
                            values[ 4, thisMonth ] = InvalidValue;
                        }
                    }// for loop over months


                    for ( int statistic = 0; statistic < 5; statistic++ )
                    {
                        sb.Clear();
                        sb.Append( "<tr>" );

                        switch ( statistic )
                        {
                            case 0:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "HighLongText", "High", false )}</td>" );
                                break;
                            case 1:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "AvgHighText", "Avg High", false )}</td>" );
                                break;
                            case 2:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "MeanText", "Mean", false )}</td>" );
                                break;
                            case 3:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "AvgLowText", "Avg Low", false )}</td>" );
                                break;
                            case 4:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "LowLongText", "Low", false )}</td>" );
                                break;
                        }


                        for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                        {
                            if ( values[ statistic, thisMonth ] == InvalidValue )
                            { //class=\"reportttl\" 
                                sb.Append( "<td colspan=\"2\" class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td colspan=\"2\" {GetTempColourFormat( values[ statistic, thisMonth ] )}>{Temp.Format( values[ statistic, thisMonth ] )}</td>" );
                            }
                        }

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }

                    of.WriteLine( "</tbody></table>" );
                    //
                    // That was the second part of the table, now the color key
                    //
                    // Part III:

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan=\"13\">&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><td class=\"reportttl\" colspan=\"13\">Color Key</td></tr>" );

                    sb.Clear();
                    sb.Append( $"<tr>" );

                    for ( int i = 0; i < 13; i++ )
                    {
                        if ( i == 0 )
                            sb.Append( $"<td {GetTempColourFormat( TempRanges[ i ] )}>&lt;&nbsp;{TempRanges[ i ]:F0}</td>" );
                        else if ( i == 12 )
                            sb.Append( $"<td {GetTempColourFormat( TempRanges[ i ] )}>{TempRanges[ i - 1 ]:F0}&gt;</td>" );
                        else
                            sb.Append( $"<td {GetTempColourFormat( TempRanges[ i ] )}>{TempRanges[ i - 1 ]:F0} - {TempRanges[ i ]:F0}</td>" );
                    }

                    sb.Append( $"</tr>" );
                    of.WriteLine( sb.ToString() );

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( "<br/>" ); // report
                                             // 
                }// End Using StreamWriter
            }// End loop over years
        }
        #endregion

        #region YADR Rain
        void GenerateYadrRainData( int YearMin, int YearMax, List<DayfileValue> Thislist )
        {
            for ( int thisYear = YearMin; thisYear <= YearMax; thisYear++ )
            {
                StringBuilder sb = new StringBuilder();
                // Part I:

                Sup.LogTraceInfoMessage( $"GenerateYadrRainData: Looping over years, doing year {thisYear}" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}YadrRain{thisYear}.txt", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<h3 style='text-align:center;'>{Sup.GetCUstringValue( "Yadr", "RainTitle", "Daily Values Rain", false )} ({Sup.StationRain.Text()})</h3><br/>" );

                    of.WriteLine( "<table class='CUtable'><tbody>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( $"  <th class='labels'>{Sup.GetCUstringValue( "Yadr", "DayText", "Day", false )}</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                    {
                        List<DayfileValue> DayList = Thislist.Where( x => x.ThisDate.Day == thisDay ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        sb.Clear();
                        sb.Append( $"<tr>" );
                        sb.Append( $"<td class='reportttl'>{thisDay}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 ) // || dat > nr of days per month) // default currentValue
                            {
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td {GetRainColourFormat( currentItem.TotalRainThisDay )}>{Sup.StationRain.Format( currentItem.TotalRainThisDay )}</td>" );
                            }
                        }// for loop over months

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }// for loop over Days  

                    of.WriteLine( $"</tbody></table>" );

                    //
                    // That was the first part of the table, now the statistics
                    //
                    // Part II:


                    of.WriteLine( "" );
                    of.WriteLine( "<table class='CUtable'><tbody>" );
                    of.WriteLine( "<tr><td class='separator' colspan='13'>&nbsp;</td></tr>" );
                    of.WriteLine( "<tr>" );
                    of.WriteLine( "<th class='labels' style='width:7%;'>&nbsp;</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:6%;'>{Months[ i ]}</th>" ); //  ??
                    of.WriteLine( "</tr>" );

                    float[,] values = new float[ 3, 12 ];

                    // Now do the loop again for the max, max avg, avg, min avg, min statistics
                    for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                    {
                        List<DayfileValue> StatisticsList = Thislist.Where( x => x.ThisDate.Month == thisMonth + 1 ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        if ( StatisticsList.Count > 0 )
                        {
                            values[ 0, thisMonth ] = StatisticsList.Where( x => x.TotalRainThisDay > 0 ).Count();
                            values[ 1, thisMonth ] = StatisticsList.Select( x => x.TotalRainThisDay ).Sum();
                            values[ 2, thisMonth ] = thisMonth > 0 ? values[ 2, thisMonth ] = ( values[ 2, thisMonth - 1 ] == InvalidValue ? 0 : values[ 2, thisMonth - 1 ] ) + values[ 1, thisMonth ]
                                                                 : values[ 2, thisMonth ] = values[ 1, thisMonth ];
                        }
                        else
                        {
                            values[ 0, thisMonth ] = InvalidValue;
                            values[ 1, thisMonth ] = InvalidValue;
                            values[ 2, thisMonth ] = InvalidValue;
                        }
                    }// for loop over months


                    for ( int statistic = 0; statistic < 3; statistic++ )
                    {
                        sb.Clear();
                        sb.Append( "<tr>" );

                        switch ( statistic )
                        {
                            case 0:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "RainDaysText", "Rain Days", false )}</td>" );
                                break;
                            case 1:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "MonthTotal", "Month Total", false )}</td>" );
                                break;
                            case 2:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "YtdTotalText", "Ytd Total", false )}</td>" );
                                break;
                        }


                        for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                        {
                            if ( values[ statistic, thisMonth ] == InvalidValue )
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            else
                                sb.Append( $"<td class=\"reportttl\">{Sup.StationRain.Format( values[ statistic, thisMonth ] )}</td>" );
                        }

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }

                    of.WriteLine( "</tbody></table>" );

                    //
                    // That was the second part of the table, now the color key
                    //
                    // Part III:

                    of.WriteLine( "<table class='CUtable'><tbody>" );
                    of.WriteLine( "<tr><td class='separator' colspan='13'>&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><td class='reportttl' colspan='13'>Color Key</td></tr>" );

                    sb.Clear();
                    sb.Append( $"<tr>" );

                    for ( int i = 0; i < 13; i++ )
                    {
                        if ( Sup.StationRain.Dim == RainDim.millimeter )
                            if ( i == 0 )
                                sb.Append( $"<td {GetRainColourFormat( RainRanges[ i ] / 2 )}>&lt;&nbsp;{RainRanges[ i ]:F0}</td>" );
                            else if ( i == 12 )
                                sb.Append( $"<td {GetRainColourFormat( RainRanges[ i ] )}>{RainRanges[ i - 1 ]:F0}&gt;</td>" );
                            else
                                sb.Append( $"<td {GetRainColourFormat( RainRanges[ i ] )}>{RainRanges[ i - 1 ]:F0} - {RainRanges[ i ]:F0}</td>" );
                        else
                            if ( i == 0 )
                            sb.Append( $"<td {GetRainColourFormat( RainRanges[ i ] / 2 )}>&lt;&nbsp;{RainRanges[ i ]:F2}</td>" );
                        else if ( i == 12 )
                            sb.Append( $"<td {GetRainColourFormat( RainRanges[ i ] )}>{RainRanges[ i - 1 ]:F2}&gt;</td>" );
                        else
                            sb.Append( $"<td {GetRainColourFormat( RainRanges[ i ] )}>{RainRanges[ i - 1 ]:F2} - {RainRanges[ i ]:F2}</td>" );
                    }

                    sb.Append( $"</tr>" );
                    of.WriteLine( sb.ToString() );

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( "<br/>" ); // report
                }// End Using StreamWriter

            }// End loop over years
        }
        #endregion

        #region YADR Wind
        void GenerateYadrWindData( int YearMin, int YearMax, List<DayfileValue> Thislist )
        {
            int i;

            for ( int thisYear = YearMin; thisYear <= YearMax; thisYear++ )
            {
                StringBuilder sb = new StringBuilder();
                // Part I:

                Sup.LogTraceInfoMessage( $"GenerateYadrWindData: Looping over years, doing year {thisYear}" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}YadrWind{thisYear}.txt", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<h3 style='text-align:center;'>{Sup.GetCUstringValue( "Yadr", "WindTitle", "Daily Values Wind Speed", false )} ({Sup.StationWind.Text()})</h3><br/>" );

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( $"  <th rowspan='2' class='labels' style='width:7%;'>{Sup.GetCUstringValue( "Yadr", "DayText", "Day", false )}</th>" );
                    for ( i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th colspan='2' class='labels' style='width:6%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    of.WriteLine( "<tr>" );
                    for ( i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:3%;'>{Sup.GetCUstringValue( "Yadr", "AvgText", "Avg", false )}</th>" +
 $"<th class='labels' style='width:3%;'>{Sup.GetCUstringValue( "Yadr", "HiText", "Hi", false )}</th>" );
                    of.WriteLine( "</tr>\n" );

                    for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                    {
                        List<DayfileValue> DayList = Thislist.Where( x => x.ThisDate.Day == thisDay ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        sb.Clear();
                        sb.Append( $"<tr>" );
                        sb.Append( $"<td class=\"reportttl\">{thisDay}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 )
                                sb.Append( "<td class=\"reportday\">---</td><td class=\"reportday\">---</td>" );
                            else
                            {
                                sb.Append( $"<td {GetWindColourFormat( currentItem.HighAverageWindSpeed )}>{Wind.Format( currentItem.HighAverageWindSpeed )}</td>" );
                                sb.Append( $"<td {GetWindColourFormat( currentItem.HighWindGust )}>{Wind.Format( currentItem.HighWindGust )}</td>" );
                            }
                        }// for loop over months

                        sb.Append( "</tr>" );
                        of.WriteLine( $"{sb}" );
                    }// for loop over Days  

                    of.WriteLine( "</tbody></table>" );

                    //
                    // That was the first part of the table, now the statistics
                    //
                    // Part II:

                    of.WriteLine( "" );
                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan=\"25\">&nbsp;</td></tr>" );
                    of.WriteLine( "<tr>" );
                    of.WriteLine( "<th class='labels' style='width:8%;'>&nbsp;</th>" );
                    for ( i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th colspan='2' class='labels' style='width:6%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>\n" );

                    float[,] values = new float[ 4, 12 ];

                    // Now do the loop again for the max, max avg, avg, min avg, min statistics
                    for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                    {
                        List<DayfileValue> StatisticsList = Thislist.Where( x => x.ThisDate.Month == thisMonth + 1 ).Where( x => x.ThisDate.Year == thisYear ).ToList(); //.Max().MaxTemp;

                        if ( StatisticsList.Count > 0 )
                        {
                            values[ 0, thisMonth ] = StatisticsList.Select( x => x.HighAverageWindSpeed ).ToList().Average();
                            values[ 1, thisMonth ] = StatisticsList.Select( x => x.HighAverageWindSpeed ).ToList().Max();
                            values[ 2, thisMonth ] = StatisticsList.Select( x => x.HighWindGust ).ToList().Average();
                            values[ 3, thisMonth ] = StatisticsList.Select( x => x.HighWindGust ).ToList().Max();
                        }
                        else
                        {
                            values[ 0, thisMonth ] = InvalidValue;
                            values[ 1, thisMonth ] = InvalidValue;
                            values[ 2, thisMonth ] = InvalidValue;
                            values[ 3, thisMonth ] = InvalidValue;
                        }
                    }// for loop over months


                    for ( int statistic = 0; statistic < 2; statistic++ )
                    {
                        sb.Clear();
                        sb.Append( "<tr>" );

                        switch ( statistic )
                        {
                            case 0:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "MonthAvgText", "Month Avg", false )}</td>" );
                                break;
                            case 1:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "MonthHighText", "Month High", false )}</td>" );
                                break;
                        }


                        for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                        {
                            if ( values[ statistic, thisMonth ] == InvalidValue )
                            { //class=\"reportttl\" 
                                sb.Append( "<td class=\"reportday\">---</td><td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td {GetWindColourFormat( values[ statistic, thisMonth ] )}>{Wind.Format( values[ statistic, thisMonth ] )}</td>" +
                                  $"<td {GetWindColourFormat( values[ statistic + 2, thisMonth ] )}>{Wind.Format( values[ statistic + 2, thisMonth ] )}</td>" );
                            }
                        }

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }

                    of.WriteLine( "</tbody></table>" );
                    //
                    // That was the second part of the table, now the color key
                    //
                    // Part III:

                    of.WriteLine( "<table class='CUtable'><tbody>" );
                    of.WriteLine( "<tr><td class='separator' colspan='13'>&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><td class='reportttl' colspan='13'>Color Key</td></tr>" );
                    of.WriteLine( $"<tr class='reportttl' style='text-align:left'>" +
                      $"<th colspan='2'>Beaufort</th>" +
                      $"<th colspan='5'>{Sup.GetCUstringValue( "Yadr", "DescriptionText", "Description", false )}</th>" +
                      $"<th  colspan='6'>{Sup.GetCUstringValue( "Yadr", "WindSpeedText", "Wind Speed", false )} {Sup.StationWind.Text()}</th></tr>" );

                    for ( i = 0; i < 13; i++ )
                    {
                        sb.Clear();
                        sb.Append( $"<tr class='beaufort' style='text-align:left;{GetWindColourFormat( i )}'><td colspan='2'>{i}</td><td colspan='5'>{WindDescr[ i ]}</td>" );

                        if ( i == 0 )
                            sb.Append( $"<td colspan='6'>&lt;&nbsp;{Wind.Format( WindRanges[ i ] )}</td></tr>" );
                        else if ( i == 12 )
                            sb.Append( $"<td colspan='6'>{Wind.Format( WindRanges[ i ] )}&nbsp;&gt;</td></tr>" );
                        else
                            sb.Append( $"<td colspan='6'>{Wind.Format( WindRanges[ i - 1 ] )} - {Wind.Format( WindRanges[ i ] )}</td></tr>" );

                        of.WriteLine( sb.ToString() );
                    }

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( "<br/>" );
                }// End Using StreamWriter
            }// End loop over years
        }
        #endregion

        #region YADR WindRun

        void GenerateYadrWindrunData( int YearMin, int YearMax, List<DayfileValue> Thislist )
        {
            for ( int thisYear = YearMin; thisYear <= YearMax; thisYear++ )
            {
                StringBuilder sb = new StringBuilder();
                // Part I:

                Sup.LogTraceInfoMessage( $"GenerateYadrWindrunData: Looping over years, doing year {thisYear}" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}YadrWindrun{thisYear}.txt", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<h3 style='text-align:center;'>{Sup.GetCUstringValue( "Yadr", "WindrunTitle", "Daily Windrun values", false )} ({Sup.StationDistance.Text()})</h3><br/>" );

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( $"  <th class=\"labels\">{Sup.GetCUstringValue( "Yadr", "DayText", "Day", false )}</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class=\"labels\">{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                    {
                        List<DayfileValue> DayList = Thislist.Where( x => x.ThisDate.Day == thisDay ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        sb.Clear();
                        sb.Append( $"<tr>" );
                        sb.Append( $"<td class='reportttl'>{thisDay}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 )
                            { // no data in this month
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td  {GetWindrunColourFormat( currentItem.TotalWindRun )}>{Distance.Format( currentItem.TotalWindRun )}</td>" );
                            }
                        }// for loop over months

                        sb.Append( $"</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }// for loop over Days  

                    of.WriteLine( $"</tbody></table>" );

                    //
                    // That was the first part of the table, now the statistics
                    //
                    // Part II:


                    of.WriteLine( "" );
                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan=\"13\">&nbsp;</td></tr>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( "<th class='labels' style='width:7%;'>&nbsp;</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:6%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    float[,] values = new float[ 3, 12 ];

                    // Now do the loop again for the max, max avg, avg, min avg, min statistics
                    for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                    {
                        List<DayfileValue> StatisticsList = Thislist.Where( x => x.ThisDate.Month == thisMonth + 1 ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        if ( StatisticsList.Count > 0 )
                        {
                            values[ 0, thisMonth ] = StatisticsList.Select( x => x.TotalWindRun ).ToList().Max();
                            values[ 1, thisMonth ] = StatisticsList.Select( x => x.TotalWindRun ).Average();
                            values[ 2, thisMonth ] = StatisticsList.Select( x => x.TotalWindRun ).ToList().Min();
                        }
                        else
                        {
                            values[ 0, thisMonth ] = InvalidValue;
                            values[ 1, thisMonth ] = InvalidValue;
                            values[ 2, thisMonth ] = InvalidValue;
                        }
                    }// for loop over months


                    for ( int statistic = 0; statistic < 3; statistic++ )
                    {
                        sb.Clear();
                        sb.Append( "<tr>" );

                        switch ( statistic )
                        {
                            case 0:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "HighLongText", "High", false )}</td>" );
                                break;
                            case 1:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "MeanText", "Mean", false )}</td>" );
                                break;
                            case 2:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "LowLongText", "Low", false )}</td>" );
                                break;
                        }


                        for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                        {
                            if ( values[ statistic, thisMonth ] == InvalidValue )
                            { //class=\"reportttl\" 
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td {GetWindrunColourFormat( values[ statistic, thisMonth ] )}>{Distance.Format( values[ statistic, thisMonth ] )}</td>" );
                            }
                        }

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }

                    of.WriteLine( "</tbody></table>" );
                    //
                    // That was the second part of the table, now the color key
                    //
                    // Part III:

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan=\"13\">&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><td class=\"reportttl\" colspan=\"13\">Color Key</td></tr>" );

                    sb.Clear();
                    sb.Append( $"<tr>" );

                    for ( int i = 0; i < 13; i++ )
                    {
                        if ( i == 12 )
                            sb.Append( $"<td {GetWindrunColourFormat( WindrunRanges[ i ] )}>{Distance.Format( WindrunRanges[ i ] )}&gt;</td>" );
                        else
                            sb.Append( $"<td {GetWindrunColourFormat( WindrunRanges[ i ] )}>&lt;{Distance.Format( WindrunRanges[ i ] )}</td>" );

                    }

                    sb.Append( $"</tr>" );
                    of.WriteLine( sb.ToString() );

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( "<br/>" ); // report
                                             // 
                }// End Using StreamWriter
            }// End loop over years
        }


        #endregion

        #region YADR Pression
        void GenerateYadrPressionData( int YearMin, int YearMax, List<DayfileValue> Thislist )
        {
            for ( int thisYear = YearMin; thisYear <= YearMax; thisYear++ )
            {
                StringBuilder sb = new StringBuilder();
                // Part I:

                Sup.LogTraceInfoMessage( $"GenerateYadrPressionData: Looping over years, doing year {thisYear}" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}YadrPress{thisYear}.txt", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<h3 style='text-align:center;'>{Sup.GetCUstringValue( "Yadr", "PressionTitle", "Daily Values Pressure", false )} ({Sup.StationPressure.Text()})</h3><br/>" );

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( $"  <th class='labels' style='width:6%;'>{Sup.GetCUstringValue( "Yadr", "DayText", "Day", false )}</th>" );
                    of.WriteLine( $"  <th class='labels' style='width:9%;'>{Sup.GetCUstringValue( "Yadr", "High/Low", "High/Low", false )}</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:7%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                    {
                        List<DayfileValue> DayList = Thislist.Where( x => x.ThisDate.Day == thisDay ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        sb.Clear();
                        sb.Append( $"<tr>" );
                        sb.Append( $"<td rowspan='2' class='reportttl'>{thisDay}</td>" );

                        sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "HighLongText", "High", false )}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 )
                            { // no data in this month
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                // float tmp = (currentItem.MaxBarometer + currentItem.MinBarometer) / 2;
                                sb.Append( $"<td  {GetPressColourFormat( currentItem.MaxBarometer )}>{Sup.StationPressure.Format( currentItem.MaxBarometer )}</td>" );
                            }
                        }// for loop over months

                        // Do the same for the low value
                        sb.Append( $"</tr><tr>\n" );
                        sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "LowLongText", "Low", false )}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 )
                            { // no data in this month
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                // float tmp = (currentItem.MaxBarometer + currentItem.MinBarometer) / 2;
                                sb.Append( $"<td  {GetPressColourFormat( currentItem.MinBarometer )}>{Sup.StationPressure.Format( currentItem.MinBarometer )}</td>" );
                            }
                        }// for loop over months

                        sb.Append( $"</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }// for loop over Days  

                    of.WriteLine( $"</tbody></table>" );

                    //
                    // That was the first part of the table, now the statistics
                    //
                    // Part II:


                    of.WriteLine( "" );
                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan='13'>&nbsp;</td></tr>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( "<th class='labels' style='width:7%;'>&nbsp;</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:6%;'>{Months[ i ]}</th>" ); //  ??
                    of.WriteLine( "</tr>" );

                    float[,] values = new float[ 3, 12 ];

                    // Now do the loop again for the max, max avg, avg, min avg, min statistics
                    for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                    {
                        List<DayfileValue> StatisticsList = Thislist.Where( x => x.ThisDate.Month == thisMonth + 1 ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        if ( StatisticsList.Count > 0 )
                        {
                            values[ 0, thisMonth ] = StatisticsList.Select( x => x.MaxBarometer ).ToList().Max();
                            values[ 1, thisMonth ] = StatisticsList.Select( x => ( x.MaxBarometer + x.MinBarometer ) / 2 ).ToList().Average();
                            values[ 2, thisMonth ] = StatisticsList.Select( x => x.MinBarometer ).ToList().Min();
                        }
                        else
                        {
                            values[ 0, thisMonth ] = InvalidValue;
                            values[ 1, thisMonth ] = InvalidValue;
                            values[ 2, thisMonth ] = InvalidValue;
                        }
                    }// for loop over months


                    for ( int statistic = 0; statistic < 3; statistic++ )
                    {
                        sb.Clear();
                        sb.Append( "<tr>" );

                        switch ( statistic )
                        {
                            case 0:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "HighLongText", "High", false )}</td>" );
                                break;
                            case 1:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "MeanText", "Mean", false )}</td>" );
                                break;
                            case 2:
                                sb.Append( $"<td class=\"reportttl\">{Sup.GetCUstringValue( "Yadr", "LowLongText", "Low", false )}</td>" );
                                break;
                        }


                        for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                        {
                            if ( values[ statistic, thisMonth ] == InvalidValue )
                            { //class=\"reportttl\" 
                                sb.Append( "<td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td {GetPressColourFormat( values[ statistic, thisMonth ] )}>{Sup.StationPressure.Format( values[ statistic, thisMonth ] )}</td>" );
                            }
                        }

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }

                    of.WriteLine( "</tbody></table>" );
                    //
                    // That was the second part of the table, now the color key
                    //
                    // Part III:

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan=\"13\">&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><td class=\"reportttl\" colspan=\"13\">Color Key</td></tr>" );

                    sb.Clear();
                    sb.Append( $"<tr>" );

                    for ( int i = 0; i < 13; i++ )
                    {
                        if ( i == 0 )
                            sb.Append( $"<td {GetPressColourFormat( PressRanges[ i ] )}>&lt;{Sup.StationPressure.Format( PressRanges[ i ] )}</td>" );
                        else if ( i == 12 )
                            sb.Append( $"<td {GetPressColourFormat( PressRanges[ i ] )}>{Sup.StationPressure.Format( PressRanges[ i ] )}&gt;</td>" );
                        else
                            sb.Append( $"<td {GetPressColourFormat( PressRanges[ i ] )}>&gt;{Sup.StationPressure.Format( PressRanges[ i ] )}</td>" );

                    }

                    sb.Append( $"</tr>" );
                    of.WriteLine( sb.ToString() );

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( "<br/>" ); // report
                                             // 
                }// End Using StreamWriter
            }// End loop over years
        }
        #endregion

        #region YADR Humidity
        void GenerateYadrHumidityData( int YearMin, int YearMax, List<DayfileValue> Thislist )
        {
            for ( int thisYear = YearMin; thisYear <= YearMax; thisYear++ )
            {
                StringBuilder sb = new StringBuilder();
                // Part I:

                Sup.LogTraceInfoMessage( $"GenerateYadrHumidityData: Looping over years, doing year {thisYear}" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}YadrHum{thisYear}.txt", false, Encoding.UTF8 ) )
                {
                    of.WriteLine( $"<h3 style='text-align:center;'>{Sup.GetCUstringValue( "Yadr", "HumTitle", "Daily Values Relative Humidity", false )} (%)</h3><br/>" );

                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );

                    of.WriteLine( "<tr>" );
                    of.WriteLine( $"  <th rowspan='2' class='labels'  style='width:7%;'>{Sup.GetCUstringValue( "Yadr", "DayText", "Day", false )}</th>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th colspan='2' class='labels' style='width:6%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    of.WriteLine( "<tr>" );
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"  <th class='labels' style='width:2%;'>" +
$"{Sup.GetCUstringValue( "Yadr", "HighShortText", "Hi", false )}</th><th class='labels' style='width:2%;'>" +
$"{Sup.GetCUstringValue( "Yadr", "LowShortText", "Lo", false )}</th>" );
                    of.WriteLine( "</tr>\n" );

                    for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                    {
                        List<DayfileValue> DayList = Thislist.Where( x => x.ThisDate.Day == thisDay ).Where( x => x.ThisDate.Year == thisYear ).ToList();

                        sb.Clear();
                        sb.Append( $"<tr>" );
                        sb.Append( $"<td class=\"reportttl\">{thisDay}</td>" );

                        for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        {
                            DayfileValue currentItem = DayList.Where( x => x.ThisDate.Month == thisMonth ).FirstOrDefault();

                            if ( currentItem.ThisDate.Year == 1 ) // || dat > nr of days per month) // default currentValue
                            {
                                sb.Append( "<td class=\"reportday\">---</td><td class=\"reportday\">---</td>" );
                            }
                            else
                            {
                                sb.Append( $"<td {GetHumColourFormat( currentItem.HighHumidity )}>{currentItem.HighHumidity:F0}</td>" );
                                sb.Append( $"<td {GetHumColourFormat( currentItem.LowHumidity )}>{currentItem.LowHumidity:F0}</td>" );
                            }
                        }// for loop over months

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }// for loop over Days  

                    of.WriteLine( $"</tbody></table>" );

                    //
                    // That was the first part of the table, now the statistics
                    //
                    // Part II:


                    of.WriteLine( "" );
                    of.WriteLine( "<table class=\"CUtable\"><tbody>" );
                    of.WriteLine( "<tr><td class=\"separator\" colspan=\"13\">&nbsp;</td></tr>" );

                    of.WriteLine( "<tr><th class='labels' style='width:7%;'>&nbsp;</th>" );

                    // Write the tableheaders for the months  
                    for ( int i = 0; i < 12; i++ )
                        of.WriteLine( $"<th class='labels' style='width:6%;'>{Months[ i ]}</th>" );
                    of.WriteLine( "</tr>" );

                    float[,] values = new float[ 5, 12 ];

                    // Now do the loop again for the max, max avg, avg, min avg, min statistics
                    for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                    {
                        List<DayfileValue> StatisticsList = Thislist.Where( x => x.ThisDate.Month == thisMonth + 1 ).Where( x => x.ThisDate.Year == thisYear ).ToList(); //.Max().MaxTemp;

                        if ( StatisticsList.Count > 0 )
                        {
                            values[ 0, thisMonth ] = StatisticsList.Select( x => x.HighHumidity ).ToList().Max();
                            values[ 1, thisMonth ] = StatisticsList.Select( x => x.HighHumidity ).ToList().Average();
                            values[ 2, thisMonth ] = StatisticsList.Select( x => ( x.HighHumidity + x.LowHumidity ) / 2 ).ToList().Average();
                            values[ 3, thisMonth ] = StatisticsList.Select( x => x.LowHumidity ).ToList().Average();
                            values[ 4, thisMonth ] = StatisticsList.Select( x => x.LowHumidity ).ToList().Min();
                        }
                        else
                        {
                            values[ 0, thisMonth ] = InvalidValue;
                            values[ 1, thisMonth ] = InvalidValue;
                            values[ 2, thisMonth ] = InvalidValue;
                            values[ 3, thisMonth ] = InvalidValue;
                            values[ 4, thisMonth ] = InvalidValue;
                        }
                    }// for loop over months


                    for ( int statistic = 0; statistic < 5; statistic++ )
                    {
                        sb.Clear();
                        sb.Append( "<tr>" );

                        switch ( statistic )
                        {
                            case 0:
                                sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "HighLongText", "High", false )}</td>" );
                                break;
                            case 1:
                                sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "AvgHighText", "Avg High", false )}</td>" );
                                break;
                            case 2:
                                sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "MeanText", "Mean", false )}</td>" );
                                break;
                            case 3:
                                sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "AvgLowText", "Avg Low", false )}</td>" );
                                break;
                            case 4:
                                sb.Append( $"<td class='reportttl'>{Sup.GetCUstringValue( "Yadr", "LowLongText", "Low", false )}</td>" );
                                break;
                        }

                        for ( int thisMonth = 0; thisMonth < 12; thisMonth++ )
                        {
                            if ( values[ statistic, thisMonth ] == InvalidValue )
                                sb.Append( "<td class='reportday'>---</td>" );
                            else
                                sb.Append( $"<td {GetHumColourFormat( values[ statistic, thisMonth ] )}>{values[ statistic, thisMonth ]:F1}</td>" );
                        }

                        sb.Append( "</tr>\n" );
                        of.WriteLine( $"{sb}" );
                    }

                    of.WriteLine( "</tbody></table>" );
                    //
                    // That was the second part of the table, now the color key
                    //
                    // Part III:

                    of.WriteLine( "<table class='CUtable'><tbody>" );
                    of.WriteLine( "<tr><td class='separator' colspan='13'>&nbsp;</td></tr>" );
                    of.WriteLine( "<tr><td class='reportttl' colspan='13'>Color Key</td></tr>" );

                    sb.Clear();
                    sb.Append( $"<tr>" );

                    for ( int i = 0; i < 13; i++ )
                    {
                        if ( i == 12 )
                            sb.Append( $"<td {GetHumColourFormat( HumRanges[ i ] )}>{HumRanges[ i ]:F0} - {100:F0}</td>" );
                        else
                            sb.Append( $"<td {GetHumColourFormat( HumRanges[ i ] )}>{HumRanges[ i ]:F0} - {HumRanges[ i + 1 ]:F0}</td>" );
                    }

                    sb.Append( $"</tr>" );
                    of.WriteLine( sb.ToString() );

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( "<br/>" ); // report
                                             // 
                }// End Using StreamWriter
            }// End loop over years
        }
        #endregion

        #region YADR Supporting ColourFormat - 
        string GetTempColourFormat( float thisValue )
        {
            try
            {
                for ( int i = 0; i < 13; i++ )
                {
                    string c;

                    if ( i <= 2 || i >= 10 )
                        c = "color:white;";
                    else
                        c = "";

                    if ( thisValue <= TempRanges[ i ] )
                        return $"class=\"levelT\" style=\"{c}background-color:{TempColorFormat[ i ]}\"";
                }
            }
            catch ( IndexOutOfRangeException e )
            {
                // Note the extreme value but return the highest colour accent
                Sup.LogTraceErrorMessage( $"IndexOutOfRange {e.Message}: illegal or abnormal value ({thisValue}) in Dayfile.txt" );
            }

            return $"class=\"levelT\" style=\"color:white;background-color:{TempColorFormat[ 12 ]}\"";
        }

        string GetRainColourFormat( float thisValue )
        {
            try
            {
                if ( thisValue < (float) Sup.StationRain.Convert( RainDim.millimeter, Sup.StationRain.Dim, 0.2 ) )
                    return "class=\"reportday\"";
                else
                    for ( int i = 0; i < 13; i++ )
                    {
                        string c = "";

                        if ( i >= 10 )
                            c = "color:white;";

                        if ( thisValue <= RainRanges[ i ] )
                        {
                            return $"class=\"levelT\" style=\"{c}background-color:{RainColorFormat[ i ]}\"";
                        }
                    }
            }
            catch ( IndexOutOfRangeException e )
            {
                // Note the error but return the highest colour accent
                Sup.LogTraceErrorMessage( $"IndexOutOfRange {e.Message}: illegal or abnormal value ({thisValue}) in Dayfile.txt" );
            }

            // i must be 13 so we return the level_13 format
            return $"class=\"levelR\" style=\"color:white;background-color: {RainColorFormat[ 12 ]}\"";
        }

        string GetWindColourFormat( float thisValue )
        {
            try
            {
                for ( int i = 0; i < 13; i++ )
                    if ( thisValue < WindRanges[ i ] )
                        return $"class=\"beaufort\" style=\"background-color: {WindColorFormat[ i ]}\"";
            }
            catch ( IndexOutOfRangeException e )
            {
                Sup.LogTraceErrorMessage( $"IndexOutOfRange {e.Message}: illegal or abnormal value ({thisValue}) in Dayfile.txt" );
            }

            // i must be 13 so we return the Max beaufort format
            return $"class=\"beaufort\" style=\"background-color: {WindColorFormat[ 12 ]}\"";
        }

        string GetWindColourFormat( int beaufort )
        {
            return $"background-color: {WindColorFormat[ beaufort ]}";
        }

        string GetWindrunColourFormat( float thisValue )
        {
            try
            {
                for ( int i = 0; i < 13; i++ )
                {
                    string c = "";

                    //if (i <= 2 || i >= 10) c = "color:white;";
                    //else c = "";

                    if ( thisValue <= WindrunRanges[ i ] )
                        return $"class=\"levelT\" style=\"{c}background-color:{WindrunColorFormat[ i ]}\"";
                }
            }
            catch ( IndexOutOfRangeException e )
            {
                Sup.LogTraceErrorMessage( $"IndexOutOfRange for value: illegal or abnormal value ({thisValue}) in Dayfile.txt - {e.Message}" );
            }

            // i must be 13 so we return the level_13 format
            return $"class=\"levelT\" style=\"background-color:{WindrunColorFormat[ 12 ]}\"";
        }

        string GetPressColourFormat( float thisValue )
        {
            try
            {
                for ( int i = 0; i < 13; i++ )
                {
                    string c;

                    if ( i <= 2 || i >= 10 )
                        c = "color:white;";
                    else
                        c = "";

                    if ( thisValue <= PressRanges[ i ] )
                        return $"class=\"levelT\" style=\"{c}background-color:{PressColorFormat[ i ]}\"";
                }
            }
            catch ( IndexOutOfRangeException e )
            {
                Sup.LogTraceErrorMessage( $"IndexOutOfRange for value: illegal or abnormal value ({thisValue}) in Dayfile.txt - {e.Message}" );
            }

            // i must be 13 so we return the level_13 format
            return $"class=\"levelT\" style=\"color:white;background-color:{PressColorFormat[ 12 ]}\"";
        }

        string GetHumColourFormat( float thisValue )
        {
            try
            {
                for ( int i = 0; i < 13; i++ )
                {
                    string c;

                    if ( i <= 2 || i >= 10 )
                        c = "color:white;";
                    else
                        c = "";

                    if ( thisValue < HumRanges[ i ] )
                        return $"class=\"levelT\" style=\"{c}background-color:{HumColorFormat[ i - 1 ]}\"";
                }
            }
            catch ( IndexOutOfRangeException e )
            {
                Sup.LogTraceErrorMessage( $"IndexOutOfRange for value: illegal or abnormal value ({thisValue}) in Dayfile.txt - {e.Message}" );
            }

            return $"class=\"levelT\" style=\"color:white; background-color:{HumColorFormat[ 12 ]}\"";
        }
        #endregion

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
        ~Yadr()
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
        #endregion

    }
}
