/*
 * Records - Part of CumulusUtils
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
    public class Records( CuSupport s )
    {
        readonly string[] m = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };  //Culture independent, just strings to compare
        readonly CuSupport Sup = s;

        enum MeasurementRecords { Tmax, Tmin, Rrate, Rhour, Rday, Rmonth, Ryear, Wgust, Wrun, Waverage, Plow, Phigh };

        int NrOfYears;

        DayfileValue[] RecordsArray;
        DayfileValue[][] YearRecords;
        DayfileValue[][] MonthlyRecords;

        int[] MonthsNotPresentYearMin;
        int[] MonthsNotPresentYearMax;
        int[] MonthsNotPresentAllYears;
        readonly int[] tmpIntArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        readonly DateTime Yesterday = DateTime.Today.AddDays( -1 );

        public void GenerateRecords( List<DayfileValue> Thislist )
        {
            Sup.LogDebugMessage( "Generate Records Starting" );

            // Loop over years
            // First do the all time records, than the yearly records, than the daily records (for another pages)

            NrOfYears = CUtils.YearMax - CUtils.YearMin + 1;

            YearRecords = new DayfileValue[ NrOfYears + 1 ][]; // (first entry being the alltime records)
            MonthlyRecords = new DayfileValue[ ( NrOfYears + 1 ) * 12 ][]; //(first entry being the all time monthly records)

            // Produce the arrays for the months without data. Required for the selection  generation in javascript later on
            List<DayfileValue> yearlist = Thislist.Where( x => x.ThisDate.Year == CUtils.YearMin ).ToList();
            MonthsNotPresentYearMin = tmpIntArray.Except( yearlist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();
            yearlist = Thislist.Where( x => x.ThisDate.Year == CUtils.YearMax ).ToList();
            MonthsNotPresentYearMax = tmpIntArray.Except( yearlist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();
            MonthsNotPresentAllYears = tmpIntArray.Except( Thislist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();

            GenerateYearlyRecords( CUtils.YearMin, Thislist );
            if ( !CUtils.Thrifty || CUtils.ThriftyRecordsDirty )
                GenerateHTMLRecords( CUtils.YearMin, CUtils.YearMax );

            return;
        }

        private void GenerateYearlyRecords( int YearMin, List<DayfileValue> Thislist )
        {
            IEnumerable<DayfileValue> yearlist = null;
            IEnumerable<DayfileValue> monthlist = null;

            int thisYear = YearMin - 1;

            Sup.LogTraceInfoMessage( "Generate Yearly Records" );

            // Remember the indexcounters always need to be one higher becauze the alltime arrays are before it 
            for ( int count = 0; count <= NrOfYears; count++ )
            {
                float tmp;

                RecordsArray = new DayfileValue[ Enum.GetNames( typeof( MeasurementRecords ) ).Length ];
                YearRecords[ count ] = RecordsArray;

                if ( count == 0 ) yearlist = Thislist;
                else
                {
                    // NOTE: yearlist (and monthlist further down) change actively when the thisyear variable changes, no need to define
                    //       the query again. Same for i in the monthlist. For performance gain, just the if statement is made!
                    thisYear++;
                    if ( count == 1 ) yearlist = Thislist.Where( x => x.ThisDate.Year == thisYear );
                }

                if ( !yearlist.Any() ) continue;

                if ( count == 0 ) Sup.LogTraceInfoMessage( $"Generate Records for AllTime" );
                else Sup.LogTraceInfoMessage( $"Generate Records for {thisYear}" );

                tmp = yearlist.Max( y => y.MaxTemp );
                RecordsArray[ (int) MeasurementRecords.Tmax ] = yearlist.Where( x => x.MaxTemp == tmp ).First(); //Thislist.Max(x => x.MaxTemp);
                tmp = yearlist.Min( y => y.MinTemp );
                RecordsArray[ (int) MeasurementRecords.Tmin ] = yearlist.Where( x => x.MinTemp == tmp ).First();
                tmp = yearlist.Max( y => y.TotalRainThisDay );
                RecordsArray[ (int) MeasurementRecords.Rday ] = yearlist.Where( x => x.TotalRainThisDay == tmp ).First();
                tmp = yearlist.Max( y => y.MaxRainRate );
                RecordsArray[ (int) MeasurementRecords.Rrate ] = yearlist.Where( x => x.MaxRainRate == tmp ).First();
                tmp = yearlist.Max( y => y.HighHourlyRain );
                RecordsArray[ (int) MeasurementRecords.Rhour ] = yearlist.Where( x => x.HighHourlyRain == tmp ).First();
                tmp = yearlist.Max( y => y.MonthlyRain );
                RecordsArray[ (int) MeasurementRecords.Rmonth ] = yearlist.Where( x => x.MonthlyRain == tmp ).First();
                tmp = yearlist.Max( y => y.YearToDateRain );
                RecordsArray[ (int) MeasurementRecords.Ryear ] = yearlist.OrderByDescending( x => x.YearToDateRain ).Where( x => x.YearToDateRain == tmp ).First();
                tmp = yearlist.Max( y => y.HighAverageWindSpeed );
                RecordsArray[ (int) MeasurementRecords.Waverage ] = yearlist.Where( x => x.HighAverageWindSpeed == tmp ).First();
                tmp = yearlist.Max( y => y.HighWindGust );
                RecordsArray[ (int) MeasurementRecords.Wgust ] = yearlist.Where( x => x.HighWindGust == tmp ).First();
                tmp = yearlist.Max( y => y.TotalWindRun );
                RecordsArray[ (int) MeasurementRecords.Wrun ] = yearlist.Where( x => x.TotalWindRun == tmp ).First();
                tmp = yearlist.Max( y => y.MaxBarometer );
                RecordsArray[ (int) MeasurementRecords.Phigh ] = yearlist.Where( x => x.MaxBarometer == tmp ).First();
                tmp = yearlist.Min( y => y.MinBarometer );
                RecordsArray[ (int) MeasurementRecords.Plow ] = yearlist.Where( x => x.MinBarometer == tmp ).First();

                if ( CUtils.Thrifty && ( count == 0 || CUtils.RunStarted.Year == thisYear ) ) // Check for alltime record or the current record 
                {
                    for ( int i = 0; i < Enum.GetNames( typeof( MeasurementRecords ) ).Length; i++ )
                    {
                        if ( RecordsArray[ i ].ThisDate.Date == Yesterday.Date )
                        {
                            CUtils.ThriftyRecordsDirty = true;
                            Sup.LogTraceInfoMessage( $"Generate Records: CUtils.ThriftyRecordsDirty {CUtils.ThriftyRecordsDirty} detected on {RecordsArray[ i ].ThisDate}" );

                            break;
                        }
                    }
                }

                for ( int i = 0; i < 12; i++ )
                {
                    RecordsArray = new DayfileValue[ Enum.GetNames( typeof( MeasurementRecords ) ).Length ];
                    MonthlyRecords[ i + count * 12 ] = RecordsArray;

                    // See NOTE above for the yearlist!!
                    if ( i == 0 ) monthlist = yearlist.Where( x => x.ThisDate.Month == ( i + 1 ) );

                    if ( !monthlist.Any() ) continue;

                    if ( count == 0 ) Sup.LogTraceInfoMessage( $"Generate Records for AllTime/month: {i + 1}" );
                    else Sup.LogTraceInfoMessage( $"Generate Records for {thisYear}/month: {i + 1}" );

                    if ( monthlist.Any() )
                    {
                        tmp = monthlist.Max( y => y.MaxTemp );
                        RecordsArray[ (int) MeasurementRecords.Tmax ] = monthlist.Where( x => x.MaxTemp == tmp ).First(); //Thislist.Max(x => x.MaxTemp);
                        tmp = monthlist.Min( y => y.MinTemp );
                        RecordsArray[ (int) MeasurementRecords.Tmin ] = monthlist.Where( x => x.MinTemp == tmp ).First();
                        tmp = monthlist.Max( y => y.TotalRainThisDay );
                        RecordsArray[ (int) MeasurementRecords.Rday ] = monthlist.Where( x => x.TotalRainThisDay == tmp ).First();
                        tmp = monthlist.Max( y => y.MaxRainRate );
                        RecordsArray[ (int) MeasurementRecords.Rrate ] = monthlist.Where( x => x.MaxRainRate == tmp ).First();
                        tmp = monthlist.Max( y => y.HighHourlyRain );
                        RecordsArray[ (int) MeasurementRecords.Rhour ] = monthlist.Where( x => x.HighHourlyRain == tmp ).First();
                        tmp = monthlist.Max( y => y.MonthlyRain );
                        RecordsArray[ (int) MeasurementRecords.Rmonth ] = monthlist.Where( x => x.MonthlyRain == tmp ).First();
                        tmp = monthlist.Max( y => y.YearToDateRain );
                        RecordsArray[ (int) MeasurementRecords.Ryear ] = monthlist.OrderByDescending( x => x.YearToDateRain ).Where( x => x.YearToDateRain == tmp ).First();
                        tmp = monthlist.Max( y => y.HighAverageWindSpeed );
                        RecordsArray[ (int) MeasurementRecords.Waverage ] = monthlist.Where( x => x.HighAverageWindSpeed == tmp ).First();
                        tmp = monthlist.Max( y => y.HighWindGust );
                        RecordsArray[ (int) MeasurementRecords.Wgust ] = monthlist.Where( x => x.HighWindGust == tmp ).First();
                        tmp = monthlist.Max( y => y.TotalWindRun );
                        RecordsArray[ (int) MeasurementRecords.Wrun ] = monthlist.Where( x => x.TotalWindRun == tmp ).First();
                        tmp = monthlist.Max( y => y.MaxBarometer );
                        RecordsArray[ (int) MeasurementRecords.Phigh ] = monthlist.Where( x => x.MaxBarometer == tmp ).First();
                        tmp = monthlist.Min( y => y.MinBarometer );
                        RecordsArray[ (int) MeasurementRecords.Plow ] = monthlist.Where( x => x.MinBarometer == tmp ).First();

                        if ( CUtils.Thrifty && ( count == 0 || CUtils.RunStarted.Year == thisYear ) ) // Check for alltime record or a thisyear record 
                        {
                            for ( int j = 0; j < Enum.GetNames( typeof( MeasurementRecords ) ).Length; j++ )
                            {
                                if ( RecordsArray[ j ].ThisDate.Date == Yesterday.Date )
                                {
                                    CUtils.ThriftyRecordsDirty = true;
                                    Sup.LogTraceInfoMessage( $"Generate Records: CUtils.ThriftyRecordsDirty {CUtils.ThriftyRecordsDirty} detected on {RecordsArray[ j ].ThisDate}" );

                                    break; // We have a record on yesterday so we need to generate and upload!
                                }
                            }
                        }
                    }
                    else
                    {
                        MonthlyRecords[ i + count * 12 ] = null;
                    }
                }
            }

            return;
        }

        private void GenerateHTMLRecords( int YearMin, int YearMax )
        {
            string tmp;
            DateTime now = DateTime.Now;

            Sup.LogDebugMessage( "Generate HTML table for Records Starting" );

            // USe the top10 Header and Accent format
            //
            string BackgroundColorHeader = Sup.GetUtilsIniValue( "Top10", "BackgroundColorHeader", "#d0d0d0" );
            string BackgroundColorTable = Sup.GetUtilsIniValue( "Top10", "BackgroundColorTable", "#f0f0f0" );
            string RecordsTxtAccentColor = Sup.GetUtilsIniValue( "Top10", "TextColorAccentTable", "DarkOrange" );
            string RecordsTxtHeaderColor = Sup.GetUtilsIniValue( "Top10", "TextColorHeader", "Green" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.RecordsOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                of.WriteLine( $"{CuSupport.GenjQueryIncludestring()}" );

                of.WriteLine( "<script>" );
                of.WriteLine( "$(function() {" );
                of.WriteLine( "  $('.jqueryOptions').hide();" );
                of.WriteLine( $"  $('.AllYears{m[ now.Month - 1 ]}').show();" );
                of.WriteLine( "  SetMonthsDisabled();" );
                of.WriteLine( "  $('#year').change(function() {" );
                of.WriteLine( "    SetMonthsDisabled();" );
                of.WriteLine( "    $('#month').trigger('change');" );
                of.WriteLine( "  });" );
                of.WriteLine( "  $('#month').change(function() {" );
                of.WriteLine( "    $('.jqueryOptions').slideUp();" );
                of.WriteLine( "    $(\".\" + $('#year').val() + $(this).val()).slideDown();" );
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
                of.WriteLine( "    if (orgPos != $('#month option:selected').val() ) { orgPos = '';" );
                of.WriteLine( "    $('#month').trigger('change');} // Else selected not changed, no trigger" );
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
                of.WriteLine( "    if (orgPos != $('#month option:selected').val() ) { orgPos = '';" );
                of.WriteLine( "    $('#month').trigger('change');} // Else selected not changed, no trigger" );
                of.WriteLine( "  });" );

                of.WriteLine( "});" );

                of.WriteLine( "function SetMonthsDisabled()" );
                of.WriteLine( "{" );
                of.WriteLine( "  $('#01, #02, #03, #04, #05, #06, #07, #08, #09, #10, #11, #12').prop('disabled', false);" );

                // Write the disabled months for AllYears
                tmp = "";
                of.WriteLine( "  if ( $('#year').val() == 'AllYears') {" );
                for ( int i = 0; i < MonthsNotPresentAllYears.Length; i++ )
                    tmp += $"#{MonthsNotPresentAllYears[ i ]:D2}, ";
                if ( !string.IsNullOrEmpty( tmp ) )
                {
                    tmp = tmp.Remove( tmp.Length - 2 );
                    of.WriteLine( $"    $('{tmp}').prop('disabled', true);" );
                }

                // Write the disabled months for YearMin
                tmp = "";
                of.WriteLine( $"  }} if ( $('#year').val() == {YearMin}) {{" );
                for ( int i = 0; i < MonthsNotPresentYearMin.Length; i++ )
                    tmp += $"#{MonthsNotPresentYearMin[ i ]:D2}, ";
                if ( !string.IsNullOrEmpty( tmp ) )
                {
                    tmp = tmp.Remove( tmp.Length - 2 );
                    of.WriteLine( $"    $('{tmp}').prop('disabled', true);" );
                }


                // Write the disabled months for YearMax
                tmp = "";
                of.WriteLine( $"  }} if ( $('#year').val() == {YearMax}) {{" );
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
                of.WriteLine( $"   background-color: {BackgroundColorHeader};" );
                of.WriteLine( "   padding: 4px;" );
                of.WriteLine( "   width: 10%;" );
                of.WriteLine( "}" );
                of.WriteLine( "#report td{" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "  text-align: center;" );
                of.WriteLine( $"  background-color: {BackgroundColorTable};" );
                of.WriteLine( "  padding: 4px;" );
                of.WriteLine( "   width: 10%;" );
                of.WriteLine( "}" );

                of.WriteLine( ".buttonFat {border-radius: 4px; margin-right:10px; margin-left:10px; }" );
                of.WriteLine( ".buttonSlim {border-radius: 4px;}" );

                of.WriteLine( "</style>" );

                of.WriteLine( "<div id='report'>" );
                of.WriteLine( "<br/>" );

                // The user can choose: Alltime or per year, All time generates a list of all months
                // 

                of.WriteLine( "<p style='text-align:center;'>" );

                of.WriteLine( $"<input type='button' class=buttonSlim id='YRPrev' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );
                of.WriteLine( $"<input type='button' class=buttonFat id='MOPrev' value='{Sup.GetCUstringValue( "General", "PrevMonth", "Prev Month", false )}'>" );

                of.WriteLine( "<select id='year'>" );
                of.WriteLine( $"<option value='AllYears' selected>{Sup.GetCUstringValue( "General", "AllTime", "All Time", false )}</option>" );
                for ( int i = YearMin; i <= YearMax; i++ ) of.WriteLine( $"<option value='{i}'>{i}</option>" );
                of.WriteLine( "</select>" );

                of.WriteLine( "<select id='month'>" );
                of.WriteLine( $"<option value='AllMonths'>{Sup.GetCUstringValue( "General", "AllMonths", "All Months", false )}</option>" );
                for ( int i = 0; i < 12; i++ )
                {
                    tmp = now.Month == ( i + 1 ) ? "Selected" : "";
                    of.WriteLine( $"<option value='{m[ i ]}' id='{i + 1:D2}' {tmp}>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( i + 1 )}</option>" );
                }
                of.WriteLine( "</select>" );

                of.WriteLine( $"<input type='button' class=buttonFat id='MONext' value='{Sup.GetCUstringValue( "General", "NextMonth", "Next Month", false )}'>" );
                of.WriteLine( $"<input type='button' class=buttonSlim id='YRNext' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );

                of.WriteLine( "</p>" );

                for ( int j = 0; j <= NrOfYears; j++ )
                {
                    if ( j == 0 ) tmp = "jqueryOptions " + "AllYears" + "AllMonths";
                    else tmp = $"jqueryOptions {YearMin + j - 1}AllMonths";

                    of.WriteLine( $"<div class='{tmp}'>" );
                    of.WriteLine( $"<table class='CUtable'><thead>" );
                    of.WriteLine( $"<tr style='color:{RecordsTxtHeaderColor}'>" );

                    if ( j == 0 ) of.WriteLine( $"<th colspan='3' style='text-align:center;'>{Sup.GetCUstringValue( "General", "AllTime", "All Time", false )}</th>" );
                    else of.WriteLine( $"<th colspan='3' style='text-align:center'>{YearMin + j - 1}</th>" );

                    of.WriteLine( $"</tr>" );

                    of.WriteLine( $"<tr style='color:{RecordsTxtHeaderColor}'>" );
                    of.WriteLine( $"<th>{Sup.GetCUstringValue( "Records", "Item", "Item", false )}</th>" );
                    of.WriteLine( $"<th>{Sup.GetCUstringValue( "Records", "Value", "Value", false )}</th>" );
                    of.WriteLine( $"<th>{Sup.GetCUstringValue( "Records", "DateTime", "Date/Time", false )}</th>" );
                    of.WriteLine( $"</tr>" );
                    of.WriteLine( $"</thead>" );

                    of.WriteLine( $"<tbody>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Tmax ].TimeMaxTemp ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>T<sub>max</sub> ({Sup.StationTemp.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Temp.Format( YearRecords[ j ][ (int) MeasurementRecords.Tmax ].MaxTemp )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Tmax ].TimeMaxTemp.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Tmin ].TimeMinTemp ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>T<sub>min</sub> ({Sup.StationTemp.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Temp.Format( YearRecords[ j ][ (int) MeasurementRecords.Tmin ].MinTemp )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Tmin ].TimeMinTemp.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Rhour ].TimeHighHourlyRain ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>R<sub>hour</sub> ({Sup.StationRain.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( YearRecords[ j ][ (int) MeasurementRecords.Rhour ].HighHourlyRain )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Rhour ].TimeHighHourlyRain.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Rday ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>R<sub>day</sub> ({Sup.StationRain.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( YearRecords[ j ][ (int) MeasurementRecords.Rday ].TotalRainThisDay )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Rday ].ThisDate.ToString( "d", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Rmonth ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>R<sub>month</sub> ({Sup.StationRain.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( YearRecords[ j ][ (int) MeasurementRecords.Rmonth ].MonthlyRain )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Rmonth ].ThisDate.ToString( "MMM yyyy", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Ryear ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>R<sub>year (to date)</sub> ({Sup.StationRain.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( YearRecords[ j ][ (int) MeasurementRecords.Ryear ].YearToDateRain )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Ryear ].ThisDate.ToString( "MMM yyyy", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Rrate ].TimeMaxRainRate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>R<sub>rate</sub> ({Sup.StationRain.Text()}{Sup.PerHour})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( YearRecords[ j ][ (int) MeasurementRecords.Rrate ].MaxRainRate )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Rrate ].TimeMaxRainRate.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Waverage ].TimeHighAverageWindSpeed ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>W<sub>average</sub> ({Sup.StationWind.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Wind.Format( YearRecords[ j ][ (int) MeasurementRecords.Waverage ].HighAverageWindSpeed )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Waverage ].TimeHighAverageWindSpeed.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Wgust ].TimeHighWindGust ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>W<sub>gust</sub> ({Sup.StationWind.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Wind.Format( YearRecords[ j ][ (int) MeasurementRecords.Wgust ].HighWindGust )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Wgust ].TimeHighWindGust.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Wrun ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>W<sub>day run</sub> ({Sup.StationDistance.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Distance.Format( YearRecords[ j ][ (int) MeasurementRecords.Wrun ].TotalWindRun )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Wrun ].ThisDate.ToString( "d", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Phigh ].TimeMaxBarometer ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>P<sub>max</sub> ({Sup.StationPressure.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationPressure.Format( YearRecords[ j ][ (int) MeasurementRecords.Phigh ].MaxBarometer )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Phigh ].TimeMaxBarometer.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    tmp = ( now.Subtract( YearRecords[ j ][ (int) MeasurementRecords.Plow ].TimeMinBarometer ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                    of.WriteLine( $"<tr>" );
                    of.WriteLine( $"<td {tmp}>P<sub>min</sub> ({Sup.StationPressure.Text()})</td>" );
                    of.WriteLine( $"<td {tmp}>{Sup.StationPressure.Format( YearRecords[ j ][ (int) MeasurementRecords.Plow ].MinBarometer )}</td>" +
                        $"<td {tmp}>{YearRecords[ j ][ (int) MeasurementRecords.Plow ].TimeMinBarometer.ToString( "g", CUtils.ThisCulture )}</td>" );
                    of.WriteLine( "</tr>" );

                    of.WriteLine( $"</tbody></table>" );
                    of.WriteLine( $"</div>" );

                    // Now the monthly table for all time records
                    for ( int month = 0; month < 12; month++ )
                    {
                        string[] m = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };  //Culture independent, just strings to compare

                        int i = month + j * 12; // use the month index belonging to the year j (0 being the AllYears)

                        if ( MonthlyRecords[ i ] is not null )
                        {
                            if ( j == 0 ) tmp = $"jqueryOptions AllYears{m[ month ]}";
                            else tmp = $"jqueryOptions {YearMin + j - 1}{m[ month ]}";

                            of.WriteLine( $"<div class='{tmp}'>" );
                            of.WriteLine( $"<table class='CUtable'><thead>" );

                            of.WriteLine( $"<tr style='color:{RecordsTxtHeaderColor}'>" );

                            if ( j == 0 ) of.WriteLine( $"<th colspan='3' style='text-align:center'>{Sup.GetCUstringValue( "General", "AllTime", "All Time", false )} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( month + 1 )}</th>" );
                            else of.WriteLine( $"<th colspan='3' style='text-align:center'>{YearMin + j - 1} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( month + 1 )}</th>" );

                            of.WriteLine( $"</tr>" );

                            of.WriteLine( $"<tr style='color:{RecordsTxtHeaderColor}'>" );
                            of.WriteLine( $"<th>{Sup.GetCUstringValue( "Records", "Item", "Item", false )}</th>" );
                            of.WriteLine( $"<th>{Sup.GetCUstringValue( "Records", "Value", "Value", false )}</th>" );
                            of.WriteLine( $"<th>{Sup.GetCUstringValue( "Records", "DateTime", "Date/Time", false )}</th>" );
                            of.WriteLine( $"</tr>" );
                            of.WriteLine( $"</thead>" );

                            of.WriteLine( $"<tbody>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Tmax ].TimeMaxTemp ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>T<sub>max</sub> ({Sup.StationTemp.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Temp.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Tmax ].MaxTemp )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Tmax ].TimeMaxTemp.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Tmin ].TimeMinTemp ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>T<sub>min</sub> ({Sup.StationTemp.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Temp.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Tmin ].MinTemp )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Tmin ].TimeMinTemp.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Rhour ].TimeHighHourlyRain ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>R<sub>hour</sub> ({Sup.StationRain.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Rhour ].HighHourlyRain )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Rhour ].TimeHighHourlyRain.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Rday ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>R<sub>day</sub> ({Sup.StationRain.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Rday ].TotalRainThisDay )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Rday ].ThisDate.ToString( "d", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Rmonth ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>R<sub>month</sub> ({Sup.StationRain.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Rmonth ].MonthlyRain )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Rmonth ].ThisDate.ToString( "MMM yyyy", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Ryear ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>R<sub>year (to date)</sub> ({Sup.StationRain.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Ryear ].YearToDateRain )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Ryear ].ThisDate.ToString( "MMM yyyy", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Rrate ].TimeMaxRainRate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>R<sub>rate</sub> ({Sup.StationRain.Text()}{Sup.PerHour})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Rrate ].MaxRainRate )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Rrate ].TimeMaxRainRate.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Waverage ].TimeHighAverageWindSpeed ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>W<sub>average</sub> ({Sup.StationWind.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Wind.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Waverage ].HighAverageWindSpeed )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Waverage ].TimeHighAverageWindSpeed.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Wgust ].TimeHighWindGust ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>W<sub>gust</sub> ({Sup.StationWind.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationRain.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Wgust ].HighWindGust )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Wgust ].TimeHighWindGust.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Wrun ].ThisDate ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>W<sub>day run</sub> ({Sup.StationDistance.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Distance.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Wrun ].TotalWindRun )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Wrun ].ThisDate.ToString( "d", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Phigh ].TimeMaxBarometer ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>P<sub>max</sub> ({Sup.StationPressure.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationPressure.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Phigh ].MaxBarometer )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Phigh ].TimeMaxBarometer.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            tmp = ( now.Subtract( MonthlyRecords[ i ][ (int) MeasurementRecords.Plow ].TimeMinBarometer ).Days < 30 ) ? $"style='color:{RecordsTxtAccentColor}'" : "";
                            of.WriteLine( $"<tr>" );
                            of.WriteLine( $"<td {tmp}>P<sub>min</sub> ({Sup.StationPressure.Text()})</td>" );
                            of.WriteLine( $"<td {tmp}>{Sup.StationPressure.Format( MonthlyRecords[ i ][ (int) MeasurementRecords.Plow ].MinBarometer )}</td>" +
                                $"<td {tmp}>{MonthlyRecords[ i ][ (int) MeasurementRecords.Plow ].TimeMinBarometer.ToString( "g", CUtils.ThisCulture )}</td>" );
                            of.WriteLine( "</tr>" );

                            of.WriteLine( $"</tbody></table>" );
                            of.WriteLine( $"</div>" );
                        }
                    }
                }

                of.WriteLine( "<br/>" ); // #report
                of.WriteLine( $"<p>{Sup.GetCUstringValue( "Records", "RecordsSince", "Records registered since", false )} {CUtils.StartOfObservations.ToString( "D", CUtils.ThisCulture )} - " +
                             $"({( CUtils.RunStarted.Date - CUtils.StartOfObservations.Date ).TotalDays} {Sup.GetCUstringValue( "General", "Days", "Days", false )})</p>" );

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // #report
            }// End using(output file)
        }
    }
}
