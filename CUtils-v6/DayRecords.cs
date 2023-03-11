/*
 * DayRecords - Part of CumulusUtils
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

namespace CumulusUtils
{
    class DayRecords
    {
        readonly string[] m = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };  //Culture independent, just strings to compare
        readonly CuSupport Sup;

        public DayRecords( CuSupport s )
        {
            Sup = s;
        }

        public void GenerateDayRecords( List<DayfileValue> Thislist )
        {
            DateTime now = DateTime.Now; //Used to handle javascript current month and later to calculate timediff with record date

            Sup.LogDebugMessage( "Generate DayRecords Start" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.DayRecordsOutputFilename}", false, System.Text.Encoding.UTF8 ) )
            {
                int[] MonthsNotPresentAllYears;
                int[] tmpIntArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
                string tmpString;

                // Produce the arrays for the months without data. Required for the selection  generation in javascript later on
                MonthsNotPresentAllYears = tmpIntArray.Except( Thislist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();

                of.WriteLine( $"{Sup.GenjQueryIncludestring()}" );
                of.WriteLine( "<script>" );
                of.WriteLine( "$(function() {" );
                of.WriteLine( "  $('.jqueryOptions').hide();" );
                of.WriteLine( $"  $('.{m[ now.Month - 1 ]}').show();" );
                of.WriteLine( "  SetMonthsDisabled();" );
                of.WriteLine( "  $('#month').change(function() {" );
                of.WriteLine( "    $('.jqueryOptions').slideUp();" );
                of.WriteLine( "    $(\".\" + $(this).val()).slideDown();" );
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#MONext').click(function() {" );
                of.WriteLine( "    if ( $('#month option:selected').val() != $('#month option').last().val() ) MOnext: {" );
                of.WriteLine( "      if ( $('#month option:selected').next('option').prop('disabled') == true) break MOnext;" );
                of.WriteLine( "      $('#month option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "      $('#month').trigger('change')" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );
                of.WriteLine( "  $('#MOPrev').click(function() {" );
                of.WriteLine( "    if ( $('#month option:selected').val() != $('#month option').first().val() ) MOprev: {" );
                of.WriteLine( "      if ( $('#month option:selected').prev('option').prop('disabled') == true) break MOprev;" );
                of.WriteLine( "      $('#month option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "      $('#month').trigger('change')" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );

                of.WriteLine( "});" );

                of.WriteLine( "function SetMonthsDisabled()" );
                of.WriteLine( "{" );
                of.WriteLine( "  $(\"#01, #02, #03, #04, #05, #06, #07, #08, #09, #10, #11, #12\").prop(\"disabled\", false);" );

                // Write the diabled months for AllYears
                tmpString = "";
                for ( int i = 0; i < MonthsNotPresentAllYears.Length; i++ )
                    tmpString += $"#{MonthsNotPresentAllYears[ i ]:D2}, ";
                if ( !string.IsNullOrEmpty( tmpString ) )
                {
                    tmpString = tmpString.Remove( tmpString.Length - 2 );
                    of.WriteLine( $"    $(\"{tmpString}\").prop(\"disabled\", true);" );
                }

                of.WriteLine( "}" );
                of.WriteLine( "</script>" );

                // Do the CSS
                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .CUtable{" );
                of.WriteLine( "  border-collapse: collapse;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "   text-align: center;" );
                of.WriteLine( "   width:100%;" );
                of.WriteLine( "   max-width:1000px;" );
                of.WriteLine( "   margin: auto;" );
                of.WriteLine( "}" );

                if ( Sup.GetUtilsIniValue( "General", "UseScrollableTables", "true" ).Equals( "true", CUtils.Cmp ) )
                {
                    of.WriteLine( ".CUtable{" );
                    of.WriteLine( "  scrollbar-width: thin;" );
                    of.WriteLine( "  display: block;" );
                    of.WriteLine( "  table-layout: fixed;" );
                    of.WriteLine( "  max-height: 65vh;" );
                    of.WriteLine( "  overflow : auto;" );
                    of.WriteLine( "}" );

                    of.WriteLine( ".CUtable thead { position: sticky; top:0; z-index: 1; }" );
                    of.WriteLine( ".CUtable td { width: 4%; }" );
                }

                of.WriteLine( "#report .labels {" );
                of.WriteLine( "  background-color: #d0d0d0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "  color: #222222;" );
                of.WriteLine( "  font-size: 90%;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .labels1 {" );
                of.WriteLine( "  background-color: #d0d0d0;" );
                of.WriteLine( "  color: white;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "  padding: 4px;" );
                of.WriteLine( "  font-size: 170%;" );
                of.WriteLine( "  font-weight: bold;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .footnote {" );
                of.WriteLine( "    background-color: #f0f0f0;" );
                of.WriteLine( "    font-weight: bold;" );
                of.WriteLine( "    font-size: 85%;" );
                of.WriteLine( "   text-align: left;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .reportday {" );
                of.WriteLine( "    background-color: #f0f0f0;" );
                of.WriteLine( "    font-weight: bold;" );
                of.WriteLine( "    font-size: 85%;" );
                of.WriteLine( "}" );

                of.WriteLine( "#report .reportttl {" );
                of.WriteLine( "  background-color: #f0f0f0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "  color: #222222;" );
                of.WriteLine( "  font-size: 85%;" );
                of.WriteLine( "  font-weight: bold;" );
                of.WriteLine( "}" );

                of.WriteLine( ".buttonFat {border-radius: 4px; margin-right:10px; margin-left:10px; }" );

                of.WriteLine( "</style>" );

                of.WriteLine( "<div id=\"report\">" );
                of.WriteLine( "<br/>" );

                // The user can choose a month so create the select list:
                of.WriteLine( "<p style='text-align:center;'>" );

                of.WriteLine( $"<input type=\"button\" class=buttonFat id=\"MOPrev\" value=\"{Sup.GetCUstringValue( "General", "PrevMonth", "Prev Month", false )}\">" );

                of.WriteLine( "<select id=\"month\">" );
                for ( int i = 0; i < 12; i++ )
                {
                    tmpString = now.Month == ( i + 1 ) ? "Selected" : "";
                    of.WriteLine( $"<option value=\"{m[ i ]}\" id=\"{i + 1:D2}\" {tmpString}>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( i + 1 )}</option>" );
                }
                of.WriteLine( "</select>" );

                of.WriteLine( $"<input type=\"button\" class=buttonFat id=\"MONext\" value=\"{Sup.GetCUstringValue( "General", "NextMonth", "Next Month", false )}\">" );

                of.WriteLine( "</p>" );

                // Required for determining the Dirty-bit
                DateTime Yesterday = DateTime.Today.AddDays( -1 );

                // Loop over all months
                for ( int i = 1; i <= 12; i++ )
                {
                    List<DayfileValue> MonthList = Thislist.Where( x => x.ThisDate.Month == i ).ToList();

                    if ( MonthList.Any() )
                    {
                        tmpString = "jqueryOptions " + m[ i - 1 ]; //This is the jQuery class on which the dropdownlist month change works. 
                                                                   //The month strings must be identical in class identification, in html select statement and in section (div) definition

                        of.WriteLine( $"<div class=\"{tmpString}\">" );
                        of.WriteLine( "<table class=\"CUtable\"><thead>" );

                        of.WriteLine( "<tr>" );
                        of.WriteLine( $"<th class=\"labels1\" colspan=\"8\">{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( i )}</th>" );
                        of.WriteLine( "</tr>" );
                        of.WriteLine( "<tr>" );
                        of.WriteLine( "<th class=\"labels\" style='width:4%;'>Day</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "HighTemperature", "High Temperature", false )}<sup>1</sup><br/>({Sup.StationTemp.Text()})</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "LowTemperature", "Low Temperature", false )}<sup>2</sup><br/>({Sup.StationTemp.Text()})</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "DailyRain", "Daily Rain", false )}<br/>({Sup.StationRain.Text()})</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "MonthToDateRain", "Month To Date Rain", false )}<br/>({Sup.StationRain.Text()})</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "YearToDateRain", "Year To Date Rain", false )}<br/>({Sup.StationRain.Text()})</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "DailyAvWind", "Daily Average Wind", false )}<br/>({Sup.StationWind.Text()})</th>" );
                        of.WriteLine( $"<th class=\"labels\" style='width:8%;'>{Sup.GetCUstringValue( "DayRecords", "HighWindGust", "High Wind Gust", false )}<br/>({Sup.StationWind.Text()})</th>" );
                        of.WriteLine( "</tr>" );
                        of.WriteLine( "</thead><tbody>" );

                        // Now loop over all days in the month; Make these loop parameters variable because start/end month may not be complete
                        // Even so sometimes another month may  be incomplete beccause of equipment failure. That is not handled now. It may even work ;)
                        int NrOfDaysInMonth = MonthList.Select( x => x.ThisDate.Day ).Distinct().Max();
                        int FirstDayInMonth = MonthList.Select( x => x.ThisDate.Day ).Distinct().Min();

                        for ( int j = FirstDayInMonth; j <= NrOfDaysInMonth; j++ )
                        {
                            // NOTE: In this loop the ThrifyDayRecordDirty bit is only set, the file is always completely generated because that is how
                            // the system  is set up. 
                            float tmp;  // Used for the queries
                            DayfileValue thisEntry; //Used for the queries

                            string HighlightFormat = ""; //Used for the formatting the result of the queries

                            List<DayfileValue> DayList = MonthList.Where( x => x.ThisDate.Day == j ).ToList();

                            if ( DayList.Any() )
                            {
                                //Write out day number
                                of.WriteLine( $"<tr><td class=\"labels\">{j}</td>" );

                                // Do HighHighTemp
                                tmp = DayList.Select( x => x.MaxTemp ).Max();
                                thisEntry = DayList.Where( x => x.MaxTemp == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: Red\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\"><table class=\"CUtable\">" );
                                of.WriteLine( $"<tr><td {HighlightFormat}>{Temp.Format( thisEntry.MaxTemp )} ({thisEntry.ThisDate.Year})</td></tr>" );

                                // Do LowHighTemp
                                tmp = DayList.Select( x => x.MaxTemp ).Min();
                                thisEntry = DayList.Where( x => x.MaxTemp == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: blue\""
                                  : "";
                                of.WriteLine( $"<tr><td {HighlightFormat}>{Temp.Format( thisEntry.MaxTemp )} ({thisEntry.ThisDate.Year})</td></tr>" );
                                of.WriteLine( "</table></td>" );

                                // Do HighLowTemp
                                tmp = DayList.Select( x => x.MinTemp ).Max();
                                thisEntry = DayList.Where( x => x.MinTemp == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: Red\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\"><table class=\"CUtable\">" );
                                of.WriteLine( $"<tr><td {HighlightFormat}>{Temp.Format( thisEntry.MinTemp )} ({thisEntry.ThisDate.Year})</td></tr>" );

                                // Do LowLowTemp
                                tmp = DayList.Select( x => x.MinTemp ).Min();
                                thisEntry = DayList.Where( x => x.MinTemp == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: blue\""
                                  : "";
                                of.WriteLine( $"<tr><td {HighlightFormat}>{Temp.Format( thisEntry.MinTemp )} ({thisEntry.ThisDate.Year})</td></tr>" );
                                of.WriteLine( "</table></td>" );

                                tmp = DayList.Select( x => x.TotalRainThisDay ).Max();
                                thisEntry = DayList.Where( x => x.TotalRainThisDay == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: DeepSkyBlue\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\" {HighlightFormat}>{Sup.StationRain.Format( thisEntry.TotalRainThisDay )} ({thisEntry.ThisDate.Year})</td>" );

                                tmp = DayList.Select( x => x.MonthlyRain ).Max();
                                thisEntry = DayList.Where( x => x.MonthlyRain == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: DeepSkyBlue\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\" {HighlightFormat}>{Sup.StationRain.Format( thisEntry.MonthlyRain )} ({thisEntry.ThisDate.Year})</td>" );

                                tmp = DayList.Select( x => x.YearToDateRain ).Max();
                                thisEntry = DayList.Where( x => x.YearToDateRain == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: DeepSkyBlue\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\" {HighlightFormat}>{Sup.StationRain.Format( thisEntry.YearToDateRain )} ({thisEntry.ThisDate.Year})</td>" );

                                tmp = DayList.Select( x => x.HighAverageWindSpeed ).Max();
                                thisEntry = DayList.Where( x => x.HighAverageWindSpeed == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: MediumSeaGreen\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\" {HighlightFormat}>{Wind.Format( thisEntry.HighAverageWindSpeed )} ({thisEntry.ThisDate.Year})</td>" );

                                tmp = DayList.Select( x => x.HighWindGust ).Max();
                                thisEntry = DayList.Where( x => x.HighWindGust == tmp ).First();

                                if ( thisEntry.ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyDayRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate DayRecords: CUtils.ThriftyDayRecordsDirty {CUtils.ThriftyDayRecordsDirty} detected on {thisEntry.ThisDate.Date}." );
                                }

                                HighlightFormat = CUtils.StartOfObservations < now.Date.AddYears( -1 ) && thisEntry.ThisDate > now.Date.AddYears( -1 ) && thisEntry.ThisDate > CUtils.StartOfObservations.AddYears( 1 )
                                  ? "style=\"color: green\""
                                  : "";
                                of.WriteLine( $"<td class=\"reportttl\" {HighlightFormat}>{Wind.Format( thisEntry.HighWindGust )} ({thisEntry.ThisDate.Year})</td>" );

                                of.WriteLine( $"</tr>" );
                            } // else no day (e.g. a gap in the first month) so skip nonexisting days
                        } // loop over days

                        of.WriteLine( "<tr><td colspan=8 class=footnote>" );
                        of.WriteLine( $"<p>1) {Sup.GetCUstringValue( "DayRecords", "Footnote_1", "Highest Temperature /  Lowest High Temperature.", false )}<br/>" );
                        of.WriteLine( $"2) {Sup.GetCUstringValue( "DayRecords", "Footnote_2", "Lowest Temperature /  Highest Low Temperature.", false )}<br/>" );
                        of.WriteLine( $"3) {Sup.GetCUstringValue( "DayRecords", "Footnote_3", "Highlighted values are set within the last year (if more than a years data exist).", false )}<br/>" );
                        of.WriteLine( $"4) {Sup.GetCUstringValue( "Records", "RecordsSince", "Records registered since", false )} {CUtils.StartOfObservations.Date:dd MMMM yyyy} - " +
                                       $"({( CUtils.RunStarted.Date - CUtils.StartOfObservations.Date ).TotalDays} {Sup.GetCUstringValue( "General", "Days", "Days", false )})</p>" );
                        of.WriteLine( "</td></tr>" );
                        of.WriteLine( "</tbody></table>" );
                        of.WriteLine( $"</div>" );
                    } // if MonnthList.Any()??
                } // loop over months

                of.WriteLine( $"<br/>" );

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style='text-align:center;font-size:11px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // #report
            } // using streamwriter

            Sup.LogTraceInfoMessage( "Generate DayRecords End" );
        } // method GenerateDayRecords
    } // class DayRecords
} // Namespace
