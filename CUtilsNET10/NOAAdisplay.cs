/*
 * NOAAdisplay - Part of CumulusUtils
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
    // As the NOAA reports are written by Cumulus, the only thing to be done is display
    // and as such be able to get rid the PHP procedures
    class NOAAdisplay( CuSupport s )
    {
        readonly CuSupport Sup = s;

        private int[] MonthsNotPresentYearMin;
        private int[] MonthsNotPresentYearMax;
        private int[] MonthsNotPresentAllYears;
        private readonly int[] tmpIntArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public void GenerateNOAATxtfile( List<DayfileValue> Thislist )
        {
            // Produce the arrays for the months without data. Required for the selection  generation in javascript later on
            List<DayfileValue> yearlist = Thislist.Where( x => x.ThisDate.Year == CUtils.YearMin ).ToList();
            MonthsNotPresentYearMin = tmpIntArray.Except( yearlist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();
            yearlist = Thislist.Where( x => x.ThisDate.Year == CUtils.YearMax ).ToList();
            MonthsNotPresentYearMax = tmpIntArray.Except( yearlist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();
            MonthsNotPresentAllYears = tmpIntArray.Except( Thislist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();

            Sup.LogDebugMessage( $"NOAA write starting" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.NOAAOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                of.WriteLine( $"{CuSupport.GenjQueryIncludestring()}" );

                of.WriteLine( "<script>" );
                of.WriteLine( "console.log('Module NOAA...');" );
                of.WriteLine( "$(function() {" );
                of.WriteLine( "  $('#year').change(function() {" );
                of.WriteLine( "    SetMonthsDisabled();" );
                of.WriteLine( "    $('#month').trigger('change');" );
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#month').change(function() {" );
                of.WriteLine( "    if ( $(this).val() != '' ) { " );
                of.WriteLine( $"      filename = '{Sup.GetUtilsIniValue( "NOAA", "FTPDirectory", "./Reports/" )}' + 'NOAAMO' + $(this).val() + $('#year').val().slice(2,4) + '.txt';" );
                of.WriteLine( "    } else {" );
                of.WriteLine( $"       filename = '{Sup.GetUtilsIniValue( "NOAA", "FTPDirectory", "./Reports/" )}' + 'NOAAYR' + $('#year').val() + '.txt';" );
                of.WriteLine( "    }" );
#if !RELEASE
                of.WriteLine( "    console.log('Filename: ' + filename);" );
#endif
                of.WriteLine( "    loadNOAAreport(filename);" );
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRNext').click(function() {" );
                of.WriteLine( "    if ( $('#year option:selected').val() != $('#year option').last().val() ) {" );
                of.WriteLine( "      $('#year option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "      SetMonthsDisabled();" );
                of.WriteLine( "      $('#month').trigger('change');" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRPrev').click(function() {" );
                of.WriteLine( "    if ( $('#year option:selected').val() != $('#year option').first().val() ) {" );
                of.WriteLine( "      $('#year option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "      SetMonthsDisabled();" );
                of.WriteLine( "      $('#month').trigger('change');" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#MONext').click(function() {" );
                of.WriteLine( "    orgPos = $('#month option:selected').val(); " );
                of.WriteLine( "    if ( $('#month option:selected').val() != $('#month option').last().val() ) MOnext: {" );
                of.WriteLine( "      while ( $('#month option:selected').next().prop('disabled') == true) {" );
                of.WriteLine( "        $('#month option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "        if ($('#month option:selected').val() == $('#month option').last().val() ) { $('#month').val(orgPos).prop('selected', 'selected'); break MOnext;}" );
                of.WriteLine( "      }; // End While" );
                of.WriteLine( "      $('#month option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "    }; // End if" );
                of.WriteLine( "    orgPos = '';" );
                of.WriteLine( "    $('#month').trigger('change');" );
                of.WriteLine( "  }); // End function;" );
                of.WriteLine( "     " );

                of.WriteLine( "  $('#MOPrev').click(function() {" );
                of.WriteLine( "    orgPos = $('#month option:selected').val();" );
                of.WriteLine( "    if ( $('#month option:selected').val() != $('#month option').first().val() ) MOprev: {" );
                of.WriteLine( "      while ( $('#month option:selected').prev().prop('disabled') == true) {" );
                of.WriteLine( "        $('#month option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "        if ($('#month option:selected').val() == $('#month option').first().val() ) { $('#month').val(orgPos).prop('selected', 'selected') ; break MOprev;}" );
                of.WriteLine( "      };" );
                of.WriteLine( "      $('#month option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "    };" );
                of.WriteLine( "    orgPos = '';" );
                of.WriteLine( "    $('#month').trigger('change');" );
                of.WriteLine( "  });" );

                // That was the setting of everything in the Document Load function
                // Now start the whole thing

                if ( Sup.GetUtilsIniValue( "NOAA", "StartInCurrentMonth", "true" ).Equals( "false", CUtils.Cmp ) )
                    of.WriteLine( "    $('#month').val('');" );  // Set the whole thing to only the year (the original)

                of.WriteLine( "    $('#year').trigger('change');" );
                of.WriteLine( "});" );

                of.WriteLine( "function loadNOAAreport( filename )" );
                of.WriteLine( "{" );
                of.WriteLine( "  $.ajax({" );
                of.WriteLine( "    url: filename," );
                of.WriteLine( "    timeout: 2000," );
                of.WriteLine( "    cache: false," );
                of.WriteLine( "    headers:{'Access-Control-Allow-Origin': '*'}," );
                of.WriteLine( "    crossDomain: true" );
                of.WriteLine( "  }).fail( function (jqXHR, textStatus, errorThrown) {" );
                of.WriteLine( "      console.log( 'loadNOAAreport: ' + textStatus + ' : ' + errorThrown );" );
                of.WriteLine( "  }).done( function (response, responseStatus) {" );
                of.WriteLine( "      $( '#NOAAplaceholder' ).html( response );" );
                of.WriteLine( "  });" );
                of.WriteLine( "};" );

                of.WriteLine( "function SetMonthsDisabled()" );
                of.WriteLine( "{" );
                of.WriteLine( "  var tmpValue = $('#year').val();" );
                of.WriteLine( "  $('#01, #02, #03, #04, #05, #06, #07, #08, #09, #10, #11, #12').prop('disabled', false);" );

                // Write the disabled months for AllYears
                string tmp = "";
                of.WriteLine( "  if ( tmpValue == 'AllYears') {" );
                for ( int i = 0; i < MonthsNotPresentAllYears.Length; i++ )
                    tmp += $"#{MonthsNotPresentAllYears[ i ]:D2}, ";
                if ( !string.IsNullOrEmpty( tmp ) )
                {
                    tmp = tmp.Remove( tmp.Length - 2 );
                    of.WriteLine( $"    $('{tmp}').prop('disabled', true);" );
                }

                // Write the disabled months for YearMin
                tmp = "";
                of.WriteLine( $"  }} else if ( tmpValue == {CUtils.YearMin}) {{" );
                for ( int i = 0; i < MonthsNotPresentYearMin.Length; i++ )
                    tmp += $"#{MonthsNotPresentYearMin[ i ]:D2}, ";
                if ( !string.IsNullOrEmpty( tmp ) )
                {
                    tmp = tmp.Remove( tmp.Length - 2 );
                    of.WriteLine( $"    $('{tmp}').prop('disabled', true);" );
                }

                // Write the disablked months for YearMax
                tmp = "";
                of.WriteLine( $"  }} else if ( tmpValue == {CUtils.YearMax}) {{" );
                for ( int i = 0; i < MonthsNotPresentYearMax.Length; i++ )
                    tmp += $"#{MonthsNotPresentYearMax[ i ]:D2}, ";
                if ( !string.IsNullOrEmpty( tmp ) )
                {
                    tmp = tmp.Remove( tmp.Length - 2 );
                    of.WriteLine( $"    $('{tmp}').prop('disabled', true);" );
                }

                of.WriteLine( "  }" );
                of.WriteLine( "}" );

                of.WriteLine( "</script>" );

                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );
                of.WriteLine( ".noaa_rep_container {" );
                of.WriteLine( "  font-family: courier new,courier,monospace;" );
                of.WriteLine( "  width: 700px;" );
                of.WriteLine( "  margin: 0 auto;" );
                of.WriteLine( "}" );
                of.WriteLine( ".noaa_rep_container pre {" );
                of.WriteLine( $"  color: {Sup.GetUtilsIniValue( "NOAA", "ColorNOAAText", "Black" )};" );
                of.WriteLine( $"  background-color: {Sup.GetUtilsIniValue( "NOAA", "ColorNOAABackground", "#f9f8EB" )};" );
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

                of.WriteLine( "<div id=report><br/>" );
                of.WriteLine( "<p style='text-align:center;'>" );
                of.WriteLine( $"<input type='button' class=buttonSlim id='YRPrev' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );
                of.WriteLine( $"<input type='button' class=buttonFat id='MOPrev' value='{Sup.GetCUstringValue( "General", "PrevMonth", "Prev Month", false )}'>" );
                of.WriteLine( "<select id='year'>" );

                for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
                {
                    if ( i < CUtils.YearMax )
                        of.WriteLine( $"<option value='{i}'>{i}</option>" );
                    else
                        of.WriteLine( $"<option value='{i}' selected>{i}</option>" );
                }

                of.WriteLine( "</select>" );

                of.WriteLine( "<select id='month'>" );
                of.WriteLine( $"<option value=''>---</option>" );

                DateTime noaaDate = CUtils.RunStarted;
                if ( noaaDate.Day == 1 )
                    noaaDate = noaaDate.AddDays( -1 );

                for ( int i = 1; i <= 12; i++ )
                    of.WriteLine( $"<option value='{i:00}' id='{i:00}' {( i == noaaDate.Month ? "selected" : "" )}>" +
                        $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i )}</option>" );

                of.WriteLine( "</select>" );

                of.WriteLine( $"<input type='button' class=buttonFat id='MONext' value='{Sup.GetCUstringValue( "General", "NextMonth", "Next Month", false )}'>" );
                of.WriteLine( $"<input type='button' class=buttonSlim id='YRNext' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );

                of.WriteLine( "</p>" );

                of.WriteLine( "<div class='noaa_rep_container'>" ); // Format for table

                // IS it HTML or pre-formatted - CMX version 4.4.0 and up 
                if ( Sup.GetCumulusIniValue( "NOAA", "NOAAUOutputText", "1" ).Equals( "1" ) ) // pre text formatted
                {
                    of.WriteLine( "<pre id='NOAAplaceholder'></pre>" );
                }
                else  // HTML formatted
                {
                    of.WriteLine( "<span id='NOAAplaceholder'></span>" );
                }

                of.WriteLine( "</div>" ); // Format for table

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // from div report
            }// end using streamwriter
        }
    }
}
