/*
 * Diary - Part of CumulusUtils
 *
 */

/*
 * Because of:
 *   - https://cumulus.hosiene.co.uk/viewtopic.php?t=20796&hilit=snow+log
 *   - https://cumulus.hosiene.co.uk/viewtopic.php?p=176371&hilit=diary#p176371
 *   - see also: https://cumulus.hosiene.co.uk/search.php?keywords=diary
 *   - https://www.komokaweather.com/weather/snowfall-log.pdf
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace CumulusUtils
{
    struct DiaryValue
    {
        public DateTime ThisDate;
        public float? snow24h;
        public float? snowDepth;
    }

    public class Diary
    {
        const int FieldWidth = 20;

        readonly CuSupport Sup;
        readonly List<DiaryValue> DiaryValues;

        public Diary( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "Diary constructor: starting" );

            if ( Sup.GetUtilsIniValue( "Diary", "Diary", "true" ).Equals( "true", CUtils.Cmp ) )
            {
                DiaryValues = LoadDiaryDatabase();

                if ( DiaryValues is null )
                {
                    Sup.LogTraceInfoMessage( "Diary database: No Data" );
                    CUtils.HasDiaryMenu = false;
                }
                else
                {
                    Sup.LogTraceInfoMessage( $"Diary database: {DiaryValues.Count} records" );
                    CUtils.HasDiaryMenu = true;
                }
            }
            else
            {
                // User does not want the Diary which overrules everything
                CUtils.HasDiaryMenu = false;
            }

            Sup.LogTraceInfoMessage( "Diary constructor: stop" );

            return;
        }

        // This function needs to run when not thrifty
        public void GenerateDiaryDisplay()
        {
            Sup.LogDebugMessage( "Generating Diary module - Starting" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.DiaryOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                of.WriteLine( $"{CuSupport.GenjQueryIncludestring()}" );

                of.WriteLine( "<script>" );
                of.WriteLine( "console.log('Module DIARY...');" );
                of.WriteLine( "$(function() {" );
                of.WriteLine( "  $('#year').change(function() {" );
                of.WriteLine( "    ReloadOnYearChange();" );
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRNext').click(function() {" );
                of.WriteLine( "    if ( $('#year option:selected').val() != $('#year option').last().val() ) {" );
                of.WriteLine( "      $('#year option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "      $('#year').trigger('change');" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRPrev').click(function() {" );
                of.WriteLine( "    if ( $('#year option:selected').val() != $('#year option').first().val() ) {" );
                of.WriteLine( "      $('#year option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "      $('#year').trigger('change');" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );

                of.WriteLine( "$('#year').trigger('change');" );
                of.WriteLine( "});" );

                of.WriteLine( "  function ReloadOnYearChange() {" );
                of.WriteLine( "    if ( urlParams.has( 'dropdown' ) ) {" );
                of.WriteLine( "      Diary2Load = urlParams.get( 'dropdown' );" );
                of.WriteLine( "      console.log('Module DIARY Reloading ... ' + Diary2Load);" );
                of.WriteLine( "      switch ( Diary2Load ) {" );
                of.WriteLine( "        case 'DiaryReport':" );
                of.WriteLine( "          SetTableView(); " );
                of.WriteLine( "          break;" );
                of.WriteLine( "        case 'DiaryChart':" );
                of.WriteLine( "          SetChartView();" );
                of.WriteLine( "          break;" );
                of.WriteLine( "        default:" );
                of.WriteLine( "          SetTableView();" );
                of.WriteLine( "          break;" );
                of.WriteLine( "      } " );
                of.WriteLine( "    }else SetTableView();" );
                of.WriteLine( "  }; " );

                of.WriteLine( "function SetTableView( )" );
                of.WriteLine( "{" );
                of.WriteLine( "  $('[id*=\"Diary\"]').hide();" );
                of.WriteLine( "  urlParams.delete('dropdown');" );
                of.WriteLine( "  urlParams.set('dropdown', 'DiaryReport');" );
                of.WriteLine( "  history.pushState(null, null, window.location.origin + window.location.pathname + '?' + urlParams);" );
                of.WriteLine( "" );
                of.WriteLine( "  $.ajax({" );

                if ( CUtils.DoModular )
                    of.WriteLine( $"    url: '{CUtils.ModulePath}' + 'Diary'+$(\'#year\').val() + '.txt'," );
                else
                    of.WriteLine( "    url: 'Diary'+$(\'#year\').val() + '.txt'," );

                of.WriteLine( "    timeout: 2000," );
                of.WriteLine( "    cache: false," );
                of.WriteLine( "    headers:{'Access-Control-Allow-Origin': '*'}," );
                of.WriteLine( "    crossDomain: true" );
                of.WriteLine( "  }).fail( function (jqXHR, textStatus, errorThrown) {" );
                of.WriteLine( "      console.log( 'loadDIARYreport: ' + textStatus + ' : ' + errorThrown );" );
                of.WriteLine( "  }).done( function (response, responseStatus) {" );
                of.WriteLine( "      $( '#DIARYplaceholder' ).html( response );" );
                of.WriteLine( "      $('#DiaryTable').show();" );
                of.WriteLine( "  });" );
                of.WriteLine( "};" );

                of.WriteLine( "function SetChartView( ) {" );
                of.WriteLine( "  $( '[id*=\"Diary\"]' ).hide();" );
                of.WriteLine( "  urlParams.delete( 'dropdown' );" );
                of.WriteLine( "  urlParams.set( 'dropdown', 'DiaryChart' );" );
                of.WriteLine( "  history.pushState( null, null, window.location.origin + window.location.pathname + '?' + urlParams );" );
                of.WriteLine( "" );
                of.WriteLine( "    DoSnowChart();" );
                of.WriteLine( "    $( '#DiaryChart' ).show();" );
                of.WriteLine( "};" );

                string snowUnit = Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in";

                of.WriteLine( "function DoSnowChart() {" );
                of.WriteLine( "  var options = {" );
                of.WriteLine( "    chart: {renderTo: 'chartcontainer'}," );
                of.WriteLine( $"   title: {{text: '{Sup.GetCUstringValue( "Diary", "Snowfall", "Snowfall", true )}' }}," );
                of.WriteLine( "    credits:{enabled: true}," );
                of.WriteLine( "    xAxis:{type: 'datetime',ordinal: false,dateTimeLabelFormats:{day: '%e %b %y',week: '%e %b %y',month: '%b %y',year: '%Y'}}," );
                of.WriteLine( $"   yAxis:[{{title:{{text: '{Sup.GetCUstringValue( "Diary", "Snow Depth", "Snow Depth", true )} ({snowUnit})' }}," );
                of.WriteLine( "    opposite: false,min: 0,labels:{align: 'right',x: -5} }]," );
                of.WriteLine( "    legend:{enabled: true}," );
                of.WriteLine( "    plotOptions:{series:{grouping: false,pointPadding: 0} }," );
                of.WriteLine( "    tooltip:{shared: true,split: false,valueDecimals: 1,xDateFormat: '%e %b %y'}," );
                of.WriteLine( "    series: [],rangeSelector:{inputEnabled: true,selected: 4}" );
                of.WriteLine( "};" );

                of.WriteLine( "chart = new Highcharts.StockChart( options );chart.showLoading();" );

                of.WriteLine( "$.ajax({" );
                of.WriteLine( $"  url: '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}alldailysnowdata.json'," );
                of.WriteLine( "  dataType: 'json'})" );
                of.WriteLine( ".done( function( resp ) {" );
                of.WriteLine( "  if ( 'SnowDepth' in resp && resp.SnowDepth.length > 0) {" );
                of.WriteLine( "    chart.addSeries({" );
                of.WriteLine( $"   name: '{Sup.GetCUstringValue( "Diary", "Snow Depth", "Snow Depth", true )}'," );
                of.WriteLine( "    type: 'column'," );
                of.WriteLine( $"   color: '{Sup.GetUtilsIniValue( "Diary", "ColorDiaryChartSnowDepth", "yellow" )}'," );
                of.WriteLine( $"   tooltip:{{valueSuffix: ' {snowUnit}'}}," );
                of.WriteLine( "    data: resp.SnowDepth," );
                of.WriteLine( "    showInNavigator: true});" );
                of.WriteLine( "  }" );

                of.WriteLine( "  if ( 'Snow24h' in resp && resp.Snow24h.length > 0) {" );
                of.WriteLine( "    chart.addSeries({" );
                of.WriteLine( $"   name: '{Sup.GetCUstringValue( "Diary", "Snow 24h", "Snow 24h", true )}'," );
                of.WriteLine( "    type: 'column'," );
                of.WriteLine( $"   color: '{Sup.GetUtilsIniValue( "Diary", "ColorDiaryChartSnow24h", "blue" )}'," );
                of.WriteLine( $"   tooltip:{{valueSuffix: ' {snowUnit}'}}," );
                of.WriteLine( "    data: resp.Snow24h," );
                of.WriteLine( "    showInNavigator: true});" );
                of.WriteLine( "  }" );
                of.WriteLine( "})" );
                of.WriteLine( ".always( function() {chart.hideLoading();}) };" );

                of.WriteLine( " </script>" );

                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );
                of.WriteLine( ".diary_rep_container {" );
                of.WriteLine( "  font-family: courier new,courier,monospace;" );
                of.WriteLine( "  width: 1050px;" );
                of.WriteLine( "  margin: 0 auto;" );
                of.WriteLine( "}" );
                of.WriteLine( ".diary_rep_container pre {" );
                of.WriteLine( $"  color: {Sup.GetUtilsIniValue( "Diary", "ColorDiaryText", "Black" )};" );
                of.WriteLine( $"  background-color: {Sup.GetUtilsIniValue( "Diary", "ColorDiaryBackground", "#f9f8EB" )};" );
                of.WriteLine( "  font-family: monospace;" );
                of.WriteLine( "  font-size: 9pt;" );
                of.WriteLine( "  font-weight: normal;" );
                of.WriteLine( "  text-align: left;" );
                of.WriteLine( "  border: 1px solid #000000;" );
                of.WriteLine( "  border-radius: 10px 10px 10px 10px;" );
                of.WriteLine( "  padding: 20px 0px 25px 20px;" );
                of.WriteLine( "}" );
                of.WriteLine( ".buttonFat {border-radius: 4px; margin-right:10px; margin-left:10px; }" );
                of.WriteLine( ".buttonSlim {border-radius: 4px;}" );
                of.WriteLine( "</style>" );

                of.WriteLine( "<div style = 'float:right;'>" );
                of.WriteLine( $"<input type = 'button' class=buttonSlim value = '{Sup.GetCUstringValue( "Diary", "TableView", "TableView", false )}' onclick='SetTableView()'>" );
                of.WriteLine( $"<input type = 'button' class=buttonSlim value = '{Sup.GetCUstringValue( "Diary", "ChartView", "ChartView", false )}' onclick='SetChartView()'>" );
                of.WriteLine( "</div>" );

                of.WriteLine( "<p style='text-align:center;'>" );
                of.WriteLine( $"  <input type='button' class=buttonFat id='YRPrev' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );
                of.WriteLine( "  <select id='year'>" );

                int StartYear = DateTime.Now.Month > 6 && DateTime.Now.Month <= 12 ? DateTime.Now.Year : DateTime.Now.Year - 1;

                for ( int i = CUtils.YearMin; i <= StartYear; i++ )
                {
                    if ( i < StartYear )
                        of.WriteLine( $"<option value='{i}'>{i}</option>" );
                    else
                        of.WriteLine( $"<option value='{i}' selected>{i}</option>" );
                }

                of.WriteLine( "</select>" );

                of.WriteLine( $"<input type='button' class=buttonFat id='YRNext' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );

                of.WriteLine( "</p>" );

                of.WriteLine( "<div id='DiaryTable' class='diary_rep_container'>" ); // Format for table
                of.WriteLine( "  <pre id='DIARYplaceholder'></pre>" );
                of.WriteLine( "</div>" ); // Format for table

                of.WriteLine( "<div id='DiaryChart'>" ); // Format for chart
                of.WriteLine( "  <div id='chartcontainer' style='min-height: 650px; margin-top: 10px; margin-bottom: 5px;'>" );
                of.WriteLine( "</div>" ); // Format for chart

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }
            } // End of the  module

            Sup.LogTraceInfoMessage( "End Generating Diary" );

            return;
        }

        // =================================================================================================================================
        // Below is the generation 
        // This function needs to run always when calling 

        public void GenerateDiaryReport()   // Generate the file to actually load into display
        {
            float Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );
            bool NorthernHemisphere = Latitude >= 0;

            Sup.LogTraceInfoMessage( $"Start Generating Diary data" );

            if ( NorthernHemisphere )
            {
                for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
                {
                    //if ( CUtils.Thrifty && i < CUtils.YearMax ) continue; // Under thrifty we only generate the current winterseason

                    Sup.LogTraceInfoMessage( $"Start Generating Diary data for {i}" );

                    GenerateDiaryForThisYear( i );

                } // Loop over all years
            }
            else
            {
                Sup.LogTraceInfoMessage( $"GenerateDiaryReport: No snow report  for the southern hemisphere has been implemented yet." );
                Sup.LogTraceInfoMessage( $"GenerateDiaryReport: Please request when required." );
            }

            Sup.LogTraceInfoMessage( $"Start Generating Diary data" );
            return;
        }

        private void GenerateDiaryForThisYear( int thisYear )
        {
            string thisLine2 = "";

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}Diary{thisYear}.txt", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CString( $"{thisYear}/{thisYear + 1} Seasonal snowfall in {Sup.GetCumulusIniValue( "Station", "LocName", "" )}", 15 * FieldWidth / 2 ) );
                of.WriteLine( CString( $"Measured each day at about " +
                    $"{Sup.GetCumulusIniValue( "Station", "SnowDepthHour", "" )} hr for the previous 24 hrs " +
                    $"(in {( Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "" ) == "0" ? "cm" : "in" )})", 15 * FieldWidth / 2 ) );
                of.WriteLine();

                of.WriteLine( CString( "Day", FieldWidth / 2 ) +
                    CString( Months.Oct.ToString(), FieldWidth ) + CString( Months.Nov.ToString(), FieldWidth ) +
                    CString( Months.Dec.ToString(), FieldWidth ) + CString( Months.Jan.ToString(), FieldWidth ) +
                    CString( Months.Feb.ToString(), FieldWidth ) + CString( Months.Mar.ToString(), FieldWidth ) + CString( Months.Apr.ToString(), FieldWidth ) );

                of.WriteLine( $"{"",FieldWidth / 2}" +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) );

                of.WriteLine( CString( "----------------------------------------------------------------------------------------------------------------------------------------------------", 150 ) );

                for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                {
                    thisLine2 = "";

                    thisLine2 += CreateLineForDay( thisYear, thisDay );
                    of.WriteLine( $"{thisLine2}" );

                } // Loop over all days
            } // User the current year as the start of the season

        }

        private string CreateLineForDay( int thisYear, int thisDay )
        {
            string thisLine = "";

            thisLine += CString( thisDay.ToString(), FieldWidth / 2 );

            for ( int thisMonth = 10; thisMonth <= 12; thisMonth++ ) // Loop over months
            {
                if ( thisDay > DateTime.DaysInMonth( thisYear, thisMonth ) )
                {
                    thisLine += CString( "   ", FieldWidth / 2 ) + CString( "   ", FieldWidth / 2 );
                    continue;
                }
                else
                    thisLine += CreateMonthPartofLineForDay( thisYear, thisMonth, thisDay );
            }  // Loop over months

            for ( int thisMonth = 1; thisMonth <= 4; thisMonth++ ) // Loop over months
            {
                if ( thisDay > DateTime.DaysInMonth( thisYear, thisMonth ) )
                {
                    thisLine += CString( "   ", FieldWidth / 2 ) + CString( "   ", FieldWidth / 2 );
                    continue;
                }
                else
                    thisLine += CreateMonthPartofLineForDay( thisYear + 1, thisMonth, thisDay );
            }  // Loop over months
            return thisLine;
        }

        private string CreateMonthPartofLineForDay( int thisYear, int thisMonth, int thisDay )
        {
            DiaryValue thisValue;
            float? SnowDepth, Snow24h;
            string StrSnowDepth = "", StrSnow24h = "", thisLine = "";

            try
            {
                thisValue = DiaryValues.Where( x => x.ThisDate.Year == thisYear )
                                    .Where( x => x.ThisDate.Month == thisMonth )
                                    .Where( x => x.ThisDate.Day == thisDay ).First();

                SnowDepth = thisValue.snowDepth;
                Snow24h = thisValue.snow24h;  // See https://cumulus.hosiene.co.uk/viewtopic.php?p=188423#p188423

                if ( Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ) // cm
                {
                    StrSnowDepth = $"{( ( SnowDepth is null ) ? "---" : SnowDepth ):F1}";
                    StrSnow24h = $"{( Snow24h is null ? "---" : Snow24h ):F1}";
                }
                else
                {
                    StrSnowDepth = $"{( ( SnowDepth is null ) ? "---" : SnowDepth ):F2}";
                    StrSnow24h = $"{( Snow24h is null ? "---" : Snow24h ):F2}";
                }

                thisLine += CString( StrSnow24h, FieldWidth / 2 ) + CString( StrSnowDepth, FieldWidth / 2 );
            }
            catch ( Exception )
            {
                thisLine += CString( "---", FieldWidth );
            }

            return thisLine;
        }

        private List<DiaryValue> LoadDiaryDatabase()
        {
            List<DiaryValue> tmpList = new();

            try
            {
                using ( SqliteConnection thisConnection = new( $"DataSource=data/diary.db; Mode=ReadOnly;" ) )
                {
                    thisConnection.Open();

                    SqliteCommand command = thisConnection.CreateCommand();
                    command.CommandText = @"SELECT * FROM DiaryData";

                    using ( SqliteDataReader reader = command.ExecuteReader() )
                    {
                        if ( reader.HasRows )
                        {
                            int OrdinalTimestamp = reader.GetOrdinal( "Date" );
                            int OrdinalEntry = reader.GetOrdinal( "Entry" );
                            int OrdinalSnow24h = reader.GetOrdinal( "Snow24h" );
                            int OrdinalSnowDepth = reader.GetOrdinal( "SnowDepth" );

                            while ( reader.Read() )
                            {
                                DiaryValue tmp = new()
                                {
                                    ThisDate = reader.GetDateTime( OrdinalTimestamp ),
                                    snow24h = reader.IsDBNull( OrdinalSnow24h ) ? null : reader.GetFloat( OrdinalSnow24h ),
                                    snowDepth = reader.IsDBNull( OrdinalSnowDepth ) ? null : reader.GetFloat( OrdinalSnowDepth )
                                };

                                if ( tmp.snow24h is not null || tmp.snowDepth is not null ) tmpList.Add( tmp );

                                Sup.LogTraceVerboseMessage( $"Value - Date: {tmp.ThisDate} Snow24h: {tmp.snow24h} SnowDepth: {tmp.snowDepth}" );
                            } // Loop over the records
                        }
                    } // using: Execute the command

                    thisConnection.Close();
                } // using: Connection

                if ( tmpList.Count > 0 ) return tmpList;
                else
                {
                    Sup.LogDebugMessage( "Generating Diary - No Data" );
                    return null;
                }
            }
            catch ( SqliteException e )
            {
                Console.WriteLine( $"ReadDiaryFromSQL: Exception - {e.ErrorCode} - {e.Message}" );
                Console.WriteLine( $"ReadDiaryFromSQL: No Diary data" );
                return null;
            }
        } // End GetDiaryDatabase()

        // Center the string on the width of the container
        private static string CString( string s, int width )
        {
            if ( s.Length >= width )
                return s;

            int leftPadding = ( width - s.Length ) / 2;
            int rightPadding = width - s.Length - leftPadding;

            return new string( ' ', leftPadding ) + s + new string( ' ', rightPadding );
        }
    } // End class Diary
} // End Namespace 
