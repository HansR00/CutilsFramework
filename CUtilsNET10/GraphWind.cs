/*
 * GraphWind - Part of CumulusUtils
 *
 */

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    partial class Graphx
    {
        // For WindRose
        private readonly int NrOfCompassSectors;
        private readonly float CompassSector;
        private readonly float HalfCompassSector;

        private readonly float NrOfWindforceClasses;
        private readonly float SpeedForRoseInterval;
        private int ZeroWindCount;

        // For WindRun
        readonly int WindrunClassWidth;
        readonly int MaxWindrun;
        readonly int NrofWindrunClasses;
        readonly int[] WindrunClasses;      // e.g.  { 100, 200, 300, 400, 500, 600, 700}; // in km, other unist need conversion

        #region WindRose
        private void GenerateWindRose( List<MonthfileValue> thisList, StringBuilder thisBuffer )
        {
            string ArrayCode;
            StringBuilder DataBuilder;
            List<MonthfileValue> PeriodList;

            Sup.LogDebugMessage( "GenerateWindRose: Starting" );

            // Hier komen alle gegenereerde arrays met de data. Voor lange perioden kan dat bnehoorlijk wat zijn!
            // Naamgeving: {value van #year}{value van #month}{nr vh Array van 1 t/m 7} (b.v. AllTimeAllMonths2)
            // Berekening: het aantal observaties telt. Of dat nu per minuut of per 15 minuten is, de waarde van de gemiddelde windsnelheid wordt genomen 
            // 0 km/hr wordt meegerekend en in klasse o geplaatst. Het (aantal waarnemingen per klasse)/ (totaal aantal waarnemingen) is de frequentie
            // 

            thisBuffer.AppendLine( "var chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  chart: {" );
            thisBuffer.AppendLine( "    polar: true," );
            thisBuffer.AppendLine( "    type: 'column'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( $"  title:{{ text: '{Sup.GetCUstringValue( "Graphs", "WindRose", "Wind Rose", true )}' }}," );

            string tmp = Sup.GetUtilsIniValue( "Graphs", "WindRoseColors", "['lightgrey', 'green', 'blue', 'yellow', 'orange', 'red', 'deeppink', 'purple', 'black']" );
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( $"  subtitle: {{ text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\" }}," );
            thisBuffer.AppendLine( "  pane: { size: '85%' }," );
            thisBuffer.AppendLine( "  legend: {" );
            thisBuffer.AppendLine( $"    reversed: {Sup.GetUtilsIniValue( "Graphs", "WindRoseInversed", "false" ).ToLowerInvariant()}," );
            thisBuffer.AppendLine( "    align: 'right', verticalAlign: 'top', y: 100, layout: 'vertical' }," );
            thisBuffer.AppendLine( $"  xAxis: {{ categories: {Sup.GetCUstringValue( "Graphs", "CompassSectors", "[\"N\", \"NNE\", \"NE\", \"ENE\", \"E\", \"ESE\", \"SE\", \"SSE\", \"S\", \"SSW\", \"SW\", \"WSW\", \"W\", \"WNW\", \"NW\", \"NNW\"]", true )} }}," );
            thisBuffer.AppendLine( "  yAxis: { min: 0, endOnTick: false, showLastLabel: true," );
            thisBuffer.AppendLine( $"    title: {{ text: '{Sup.GetCUstringValue( "Graphs", "Frequency", "Frequency", true )} (%)' }}," );
            thisBuffer.AppendLine( "    labels:{ formatter: function() { return this.value + '%'; } }," );
            thisBuffer.AppendLine( $"     reversedStacks: {Sup.GetUtilsIniValue( "Graphs", "WindRoseInversed", "false" ).ToLowerInvariant()}" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  tooltip: { valueSuffix: '%', followPointer: true }," );
            thisBuffer.AppendLine( "  plotOptions: { series: { stacking: 'normal', shadow: false, groupPadding: 0, pointPlacement: 'on' } }," );

            thisBuffer.AppendLine( "  series: [ " );

            // Set ZeroWindCount here, will be used in the array generation function locally as well
            //
            ZeroWindCount = thisList.Where( x => x.CMXLatestGust == 0 ).Count();

            for ( int i = 1; i <= NrOfWindforceClasses; i++ )
            {
                if ( i == 1 )
                {
                    thisBuffer.AppendLine( $"      {{ 'name': '{Sup.GetCUstringValue( "Graphs", "ZeroWind", "Zero Wind", true )} ' + ZeroWind + ' %'}}," );
                    thisBuffer.AppendLine( $"      {{ 'name': '&lt; {Wind.Format( i * SpeedForRoseInterval )} {Sup.StationWind.Text()}', 'data': data{i} }}," );
                }
                else if ( i == NrOfWindforceClasses )
                {
                    thisBuffer.AppendLine( $"      {{ 'name': '&gt; {Wind.Format( ( NrOfWindforceClasses - 1 ) * SpeedForRoseInterval )} {Sup.StationWind.Text()}', 'data': data{i} }}," );
                }
                else
                {
                    thisBuffer.AppendLine( $"      {{ 'name': '{Wind.Format( ( i - 1 ) * SpeedForRoseInterval )} - {Wind.Format( i * SpeedForRoseInterval )} {Sup.StationWind.Text()}', 'data': data{i} }}," );
                }
            }
            thisBuffer.AppendLine( "          ]" );
            thisBuffer.AppendLine( "       }, function (chart) {" );
            thisBuffer.AppendLine( "         chart.customText = chart.renderer.text(textLine, 40, 80)" +
              ".css({color: chart.title.styles.color}).add();" );
            thisBuffer.AppendLine( "       });" );
            thisBuffer.AppendLine( "}" );

            Sup.LogTraceInfoMessage( "WindRose : Starting writing the javascript data arrays." );

            // The lookup hashing makes it 35% faster!!
            // ILookup<int, MonthfileValue> YearLookup = thisList.ToLookup( x => x.ThisDate.Year, x => x );
            var YearLookup = thisList.ToLookup( x => x.ThisDate.Year );


            for ( int year = CUtils.YearMin; year <= CUtils.YearMax; year++ )
            {
                Sup.LogTraceInfoMessage( $"WindRose : Starting writing year: {year}" );

                List<MonthfileValue> YearList = YearLookup[ year ].ToList();

                var MonthLookup = YearList.ToLookup( x => x.ThisDate.Month );

                for ( int month = 1; month <= 12; month++ )
                {
                    ArrayCode = $"{year}{m[ month - 1 ]}";

                    PeriodList = MonthLookup[ month ].ToList();

                    if ( PeriodList.Any() )
                    {
                        DataBuilder = GenerateDataArray( ArrayCode, PeriodList );
                        thisBuffer.AppendLine( DataBuilder.ToString() );
                    } // If we have a valid period with data
                } // Loop over all months

                // Now do the full year
                ArrayCode = $"{year}AllMonths";
                PeriodList = YearLookup[ year ].ToList();

                if ( PeriodList.Any() )
                {
                    DataBuilder = GenerateDataArray( ArrayCode, PeriodList );
                    thisBuffer.AppendLine( DataBuilder.ToString() );
                } // If we have a valid period with data
            } // Loop over all years

            for ( int month = 1; month <= 12; month++ )
            {
                var MonthLookup = thisList.ToLookup( x => x.ThisDate.Month );

                // Do the months for AllYears
                ArrayCode = $"AllYears{m[ month - 1 ]}";
                PeriodList = MonthLookup[ month ].ToList();
                DataBuilder = GenerateDataArray( ArrayCode, PeriodList );
                thisBuffer.AppendLine( DataBuilder.ToString() );
            }

            ArrayCode = "AllYearsAllMonths";
            Sup.LogTraceInfoMessage( $"WindRose : Starting writing : {ArrayCode}" );

            DataBuilder = GenerateDataArray( ArrayCode, thisList );
            thisBuffer.AppendLine( DataBuilder.ToString() );

            return;
        }

        private StringBuilder GenerateDataArray( string ArrayCode, List<MonthfileValue> thisList )
        {
            float FreqTmp;
            List<MonthfileValue> tmpWindforceList;
            StringBuilder DataBuilder = new StringBuilder();

            if ( thisList.Any() )
            {
                ZeroWindCount = thisList.Where( x => x.CMXLatestGust == 0 ).Count();
                DataBuilder.Append( $"ZeroWind{ArrayCode} = {( (float) ZeroWindCount / thisList.Count * 100 ).ToString( "F1", CUtils.Inv )};\n" );

                for ( int i = 1; i <= NrOfWindforceClasses; i++ )
                {
                    DataBuilder.Append( $"Rose{ArrayCode}{i}=[" );

                    tmpWindforceList = thisList.Where( x => x.CMXLatestGust <= i * SpeedForRoseInterval && x.CMXLatestGust > ( i - 1 ) * SpeedForRoseInterval ).ToList();

                    for ( int j = 0; j < NrOfCompassSectors; j++ )  //Loop over de compassectors : 360 grd/ 16 sectors = 22.5
                    {
                        if ( tmpWindforceList.Any() )
                        {
                            float m = ( j * CompassSector - HalfCompassSector ) < 0 ? 360 - HalfCompassSector : j * CompassSector - HalfCompassSector;
                            float n = ( j * CompassSector + HalfCompassSector ) > 360 ? j * CompassSector + HalfCompassSector - 360 : j * CompassSector + HalfCompassSector;
                            if ( j == 0 )
                            {
                                if ( tmpWindforceList.First().UseAverageBearing )
                                    FreqTmp = (float) ( tmpWindforceList.Where( x => x.AvWindBearing > m ).Count() + tmpWindforceList.Where( x => x.AvWindBearing < n ).Count() ) / ( thisList.Count - ZeroWindCount );
                                else
                                    FreqTmp = (float) ( tmpWindforceList.Where( x => x.CurrWindBearing > m ).Count() + tmpWindforceList.Where( x => x.CurrWindBearing < n ).Count() ) / ( thisList.Count - ZeroWindCount );
                            }
                            else
                            {
                                if ( tmpWindforceList.First().UseAverageBearing )
                                    FreqTmp = (float) tmpWindforceList.Where( x => x.AvWindBearing > m && x.AvWindBearing < n ).Count() / ( thisList.Count - ZeroWindCount );
                                else
                                    FreqTmp = (float) tmpWindforceList.Where( x => x.CurrWindBearing > m && x.CurrWindBearing < n ).Count() / ( thisList.Count - ZeroWindCount );
                            }
                        }
                        else
                        {
                            FreqTmp = 0.0F;
                        }

                        DataBuilder.Append( $"{( 100 * FreqTmp ).ToString( "F4", CUtils.Inv )}," );
                    }

                    DataBuilder.Remove( DataBuilder.Length - 1, 1 );
                    DataBuilder.Append( "];\n" );
                }
            }

            return DataBuilder;

        }
        #endregion

        #region WindRun
        private void GenerateWindrunStatistics( List<DayfileValue> thisList, StringBuilder thisBuffer, int year )
        {
            Sup.LogTraceInfoMessage( $"GenerateWindrunStatistics: Starting {year}" );

            StringBuilder sb = new StringBuilder();
            List<int> WindrunMonthData;
            List<List<int>> WindrunYearData = new List<List<int>>();

            int tmp;

            // Use the year nr, assume they are added in the increasing order
            // Do the all years as first entry in the list
            for ( int month = 1; month <= 12; month++ )
            {
                List<DayfileValue> MonthList;

                tmp = 0;

                if ( year == 0 )
                    MonthList = thisList.Where( x => x.ThisDate.Month == month ).ToList();
                else
                    MonthList = thisList.Where( x => x.ThisDate.Year == year ).Where( x => x.ThisDate.Month == month ).ToList();

                WindrunMonthData = new List<int>();

                for ( int WindrunClass = 0; WindrunClass < NrofWindrunClasses; WindrunClass++ )
                {
                    tmp = WindrunClass == 0 ? MonthList.Where( x => x.TotalWindRun < WindrunClasses[ WindrunClass ] ).Count() : MonthList.Where( x => x.TotalWindRun < WindrunClasses[ WindrunClass ] && x.TotalWindRun >= WindrunClasses[ WindrunClass - 1 ] ).Count();
                    WindrunMonthData.Add( tmp ); // low windrun class is first in the list, last is the highest windrun
                }

                WindrunYearData.Add( WindrunMonthData );
            }

            thisBuffer.AppendLine( "chart = Highcharts.chart('chartcontainer', {" );
            thisBuffer.AppendLine( "  chart:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( "    type: 'column'" );
            thisBuffer.AppendLine( "  }," );

            string tmp2 = Sup.GetUtilsIniValue( "Graphs", "GraphColors", graphColors );
            if ( !string.IsNullOrEmpty( tmp2 ) )
            {
                thisBuffer.AppendLine( $"    colors: {tmp2}," );  // Else fall back to HighchartsDefaults
            }

            thisBuffer.AppendLine( "  title:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: '{Sup.GetCUstringValue( "Graphs", "WindRunTitle", "Wind Run", true )} - " +
              $"{( year == 0 ? Sup.GetCUstringValue( "General", "AllTime", "All Time", false ) : year.ToString( "F0", CUtils.Inv ) )} ({Sup.StationDistance.Text()})'" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  subtitle:" );
            thisBuffer.AppendLine( "  {" );
            thisBuffer.AppendLine( $"    text: \"{Sup.GetCumulusIniValue( "Station", "LocDesc", "Unknown Station" )}\"" );
            thisBuffer.AppendLine( "  }," );
            thisBuffer.AppendLine( "  xAxis:" );
            thisBuffer.AppendLine( "  {" );

            sb.Clear();
            for ( int i = 1; i <= 12; i++ )
                sb.Append( $"'{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName( i )}'," );

            sb.Remove( sb.Length - 1, 1 );

            thisBuffer.AppendLine( $"    categories: [{sb}]" );
            thisBuffer.AppendLine( "   }," );
            thisBuffer.AppendLine( "   yAxis:" );
            thisBuffer.AppendLine( "   {" );
            thisBuffer.AppendLine( "     min: 0," );
            thisBuffer.AppendLine( "     title:" );
            thisBuffer.AppendLine( "     {" );
            thisBuffer.AppendLine( $"       text: '{Sup.GetCUstringValue( "Graphs", "WindRun-YaxisTitle", "Percentage of WindRun", true )} (%)'" );
            thisBuffer.AppendLine( "     }," );
            thisBuffer.AppendLine( "     stackLabels:" );
            thisBuffer.AppendLine( "     {" );
            thisBuffer.AppendLine( "       enabled: false," );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    tooltip:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      headerFormat: '<b>{point.x}</b><br/>'," );
            thisBuffer.AppendLine( $"      pointFormat: '" +
                $"{{series.name}}: {{point.y}} {Sup.GetCUstringValue( "General", "Days", "Days", true )}<br/>" +
                $"Total: {{point.stackTotal}} {Sup.GetCUstringValue( "General", "Days", "Days", true )}" +
                $"'" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    plotOptions:" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      column:" );
            thisBuffer.AppendLine( "      {" );
            thisBuffer.AppendLine( "        stacking: 'percent'," );
            thisBuffer.AppendLine( "      }" );
            thisBuffer.AppendLine( "    }," );
            thisBuffer.AppendLine( "    series: [" );

            for ( int WindrunClass = NrofWindrunClasses - 1; WindrunClass >= 0; WindrunClass-- )
            {
                sb.Clear();
                foreach ( List<int> MonthList in WindrunYearData )
                {
                    sb.Append( $"{MonthList[ WindrunClass ]}," );
                }
                sb.Remove( sb.Length - 1, 1 );
                thisBuffer.AppendLine( $"      {{name: '{WindrunClasses[ WindrunClass ]} {Sup.StationDistance.Text()}', data:[{sb}]}}," );
            }

            thisBuffer.AppendLine( "     ]" );
            thisBuffer.AppendLine( "});" );

            return;
        }
        #endregion


        #region PrevNextMenu WindRose
        private void GenerateWindRosePrevNextYearMonthMenu( List<DayfileValue> thisList, StringBuilder thisBuffer )
        {
            string tmp;

            // required for the menu jQuery procedure
            int[] MonthsNotPresentYearMin;
            int[] MonthsNotPresentYearMax;
            int[] MonthsNotPresentAllYears;
            int[] tmpIntArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            int NrOfYears;

            // Loop over years
            // First do the all time records, than the yearly records, than the daily records (for another pages)
            int YearMax = thisList.Select( x => x.ThisDate.Year ).Max();
            int YearMin = thisList.Select( x => x.ThisDate.Year ).Min();
            NrOfYears = YearMax - YearMin + 1;

            // Produce the arrays for the months without data. Required for the selection  generation in javascript later on
            List<DayfileValue> yearlist = thisList.Where( x => x.ThisDate.Year == YearMin ).ToList();
            MonthsNotPresentYearMin = tmpIntArray.Except( yearlist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();
            yearlist = thisList.Where( x => x.ThisDate.Year == YearMax ).ToList();
            MonthsNotPresentYearMax = tmpIntArray.Except( yearlist.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();
            MonthsNotPresentAllYears = tmpIntArray.Except( thisList.Select( x => x.ThisDate.Month ).Distinct() ).ToArray();

            Sup.LogTraceInfoMessage( "Generate WindRose : Starting writing the javascript menu." );

            thisBuffer.AppendLine( "<script>" );
            thisBuffer.AppendLine( "console.log('graphsWindRose Menu...');" );
            thisBuffer.AppendLine( "$(function() {" );
            thisBuffer.AppendLine( "  SetMonthsDisabled();" );
            thisBuffer.AppendLine( "  SetData('AllYears','AllMonths');" );
            thisBuffer.AppendLine( "  graph1();" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('OnLoad WindRose: ');" );
#endif

            thisBuffer.AppendLine( "  $('#yearRose').change(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose yearRose Change: ');" );
#endif

            thisBuffer.AppendLine( "    SetMonthsDisabled();" );
            thisBuffer.AppendLine( "    $('#monthRose').trigger('change');" );
            thisBuffer.AppendLine( "  });" );
            thisBuffer.AppendLine( "  $('#monthRose').change(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose monthRose Change: ');" );
#endif

            thisBuffer.AppendLine( "    SetData( $('#yearRose').val() , $(this).val() );" );
            thisBuffer.AppendLine( "    graph1();" );
            thisBuffer.AppendLine( "  });" );

            thisBuffer.AppendLine( "  $('#YRNextRose').click(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose YRNextRose Click: ');" );
#endif

            thisBuffer.AppendLine( "    if ( $('#yearRose option:selected').val() != $('#yearRose option').last().val() ) {" );
            thisBuffer.AppendLine( "      $('#yearRose option:selected').next('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "      SetMonthsDisabled();" );
            thisBuffer.AppendLine( "      $('#monthRose').trigger('change');" );
            thisBuffer.AppendLine( "    };" ); // else do nothing
            thisBuffer.AppendLine( "  });" );

            thisBuffer.AppendLine( "  $('#YRPrevRose').click(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose YRPrevRose Click: ');" );
#endif

            thisBuffer.AppendLine( "    if ( $('#yearRose option:selected').val() != $('#yearRose option').first().val() ) {" );
            thisBuffer.AppendLine( "      $('#yearRose option:selected').prev('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "      SetMonthsDisabled();" );
            thisBuffer.AppendLine( "      $('#monthRose').trigger('change');" );
            thisBuffer.AppendLine( "    };" ); // else do nothing
            thisBuffer.AppendLine( "  });" );

            thisBuffer.AppendLine( "  $('#MONextRose').click(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose MONextRose Click: ');" );
#endif

            thisBuffer.AppendLine( "    orgPos = $('#monthRose option:selected').val(); " );
            thisBuffer.AppendLine( "    if ( $('#monthRose option:selected').val() != $('#monthRose option').last().val() ) MOnext: {" );
            thisBuffer.AppendLine( "      while ( $('#monthRose option:selected').next().prop('disabled') == true) {" );
            thisBuffer.AppendLine( "        $('#monthRose option:selected').next('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "        if ($('#monthRose option:selected').val() == $('#monthRose option').last().val() ) { $('#monthRose').val(orgPos).prop('selected', 'selected'); break MOnext;}" );
            thisBuffer.AppendLine( "      };" );  // End While
            thisBuffer.AppendLine( "      $('#monthRose option:selected').next('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "    }; " );
            thisBuffer.AppendLine( "    if (orgPos != $('#monthRose option:selected').val() ) { orgPos = '';" );
            thisBuffer.AppendLine( "    $('#monthRose').trigger('change');} " );
            thisBuffer.AppendLine( "  }); " );
            thisBuffer.AppendLine( "     " );

            thisBuffer.AppendLine( "  $('#MOPrevRose').click(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose MOPrevRose Click: ');" );
#endif

            thisBuffer.AppendLine( "    orgPos = $('#monthRose option:selected').val();" );
            thisBuffer.AppendLine( "    if ( $('#monthRose option:selected').val() != $('#monthRose option').first().val() ) MOprev: {" );
            thisBuffer.AppendLine( "      while ( $('#monthRose option:selected').prev().prop('disabled') == true) {" );
            thisBuffer.AppendLine( "        $('#monthRose option:selected').prev('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "        if ($('#monthRose option:selected').val() == $('#monthRose option').first().val() ) { $('#monthRose').val(orgPos).prop('selected', 'selected') ; break MOprev;}" );
            thisBuffer.AppendLine( "      };" );
            thisBuffer.AppendLine( "      $('#monthRose option:selected').prev('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "    };" );
            thisBuffer.AppendLine( "    if (orgPos != $('#monthRose option:selected').val() ) { orgPos = '';" );
            thisBuffer.AppendLine( "    $('#monthRose').trigger('change');} " );
            thisBuffer.AppendLine( "  });" );

            thisBuffer.AppendLine( "});" );

            thisBuffer.AppendLine( "function SetMonthsDisabled()" );
            thisBuffer.AppendLine( "{" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose SetMonthsDisabled: ');" );
#endif

            thisBuffer.AppendLine( "  $('#01, #02, #03, #04, #05, #06, #07, #08, #09, #10, #11, #12').prop('disabled', false);" );

            // Write the disabled months for AllYears
            tmp = "";
            thisBuffer.AppendLine( "  if ( $('#yearRose').val() == 'AllYears') {" );
            for ( int i = 0; i < MonthsNotPresentAllYears.Length; i++ )
                tmp += $"#{MonthsNotPresentAllYears[ i ]:D2}, ";
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                tmp = tmp.Remove( tmp.Length - 2 );
                thisBuffer.AppendLine( $"    $('{tmp}').prop('disabled', true);" );
            }

            // Write the disabled months for YearMin
            tmp = "";
            thisBuffer.AppendLine( $"  }} if ( $('#yearRose').val() == {YearMin}) {{" );
            for ( int i = 0; i < MonthsNotPresentYearMin.Length; i++ )
                tmp += $"#{MonthsNotPresentYearMin[ i ]:D2}, ";
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                tmp = tmp.Remove( tmp.Length - 2 );
                thisBuffer.AppendLine( $"    $('{tmp}').prop('disabled', true);" );
            }

            // Write the disabled months for YearMax
            tmp = "";
            thisBuffer.AppendLine( $"  }} if ( $('#yearRose').val() == {YearMax}) {{" );
            for ( int i = 0; i < MonthsNotPresentYearMax.Length; i++ )
                tmp += $"#{MonthsNotPresentYearMax[ i ]:D2}, ";
            if ( !string.IsNullOrEmpty( tmp ) )
            {
                tmp = tmp.Remove( tmp.Length - 2 );
                thisBuffer.AppendLine( $"    $('{tmp}').prop('disabled', true);" );
            }

            thisBuffer.AppendLine( "  }" );
            thisBuffer.AppendLine( "}" );

            thisBuffer.AppendLine( "var textLine;" );
            thisBuffer.AppendLine( "function SetData(thisYear, thisMonth) {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRose SetData: ' + thisYear + ' - ' + thisMonth);" );
#endif

            thisBuffer.AppendLine( "code = thisYear + thisMonth;" );
            thisBuffer.AppendLine( $"textLine = '<b>{Sup.GetCUstringValue( "Graphs", "PeriodInWindRose", "Period in WindRose", true )}:</b><br>" +
              $"<b>{Sup.GetCUstringValue( "General", "Year", "Year", true )}: </b>'+ $('#yearRose option:selected').text() +'<br>" +
              $"<b>{Sup.GetCUstringValue( "General", "Month", "Month", true )}:</b> '+ $('#monthRose option:selected').text();" );

            for ( int i = 1; i <= NrOfWindforceClasses; i++ )
                thisBuffer.AppendLine( $"  data{i} = eval('Rose' + code + {i});" );

            thisBuffer.AppendLine( $"  ZeroWind = eval('ZeroWind' + code);" );
            thisBuffer.AppendLine( "}" );

            thisBuffer.AppendLine( "</script>" );

            // The user can choose: Alltime or per year, All time generates a list of all months
            // 

            thisBuffer.AppendLine( "<p style='text-align:center;'>" );
            thisBuffer.AppendLine( $"<input type='button' class=buttonSlim id='YRPrevRose' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );
            thisBuffer.AppendLine( $"<input type='button' class=buttonFat id='MOPrevRose' value='{Sup.GetCUstringValue( "General", "PrevMonth", "Prev Month", false )}'>" );
            thisBuffer.AppendLine( "<select id='yearRose'>" );
            thisBuffer.AppendLine( $"<option value='AllYears' selected>{Sup.GetCUstringValue( "General", "AllTime", "All Time", false )}</option>" );
            for ( int i = YearMin; i <= YearMax; i++ )
                thisBuffer.AppendLine( $"<option value='{i}'>{i}</option>" );
            thisBuffer.AppendLine( "</select>" );
            thisBuffer.AppendLine( "<select id='monthRose'>" );
            thisBuffer.AppendLine( $"<option value='AllMonths' selected>{Sup.GetCUstringValue( "General", "AllMonths", "All Months", false )}</option>" );
            thisBuffer.AppendLine( $"<option value='Jan' id='01'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 1 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Feb' id='02'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 2 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Mar' id='03'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 3 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Apr' id='04'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 4 )}</option>" );
            thisBuffer.AppendLine( $"<option value='May' id='05'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 5 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Jun' id='06'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 6 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Jul' id='07'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 7 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Aug' id='08'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 8 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Sep' id='09'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 9 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Oct' id='10'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 10 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Nov' id='11'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 11 )}</option>" );
            thisBuffer.AppendLine( $"<option value='Dec' id='12'>{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( 12 )}</option>" );
            thisBuffer.AppendLine( "</select>" );
            thisBuffer.AppendLine( $"<input type='button' class=buttonFat id='MONextRose' value='{Sup.GetCUstringValue( "General", "NextMonth", "Next Month", false )}'>" );
            thisBuffer.AppendLine( $"<input type='button' class=buttonSlim id='YRNextRose' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );
            thisBuffer.AppendLine( "</p>" );
            // thisBuffer.AppendLine("  <div id='chartcontainer'></div>");
        }
        #endregion

        #region PrevNextMenu WindRun
        private void GenerateWindRunPrevNextYearMenu( StringBuilder thisBuffer )
        {
            Sup.LogTraceInfoMessage( "Generate Windrun : Starting writing the javascript menu." );

            thisBuffer.AppendLine( "<script>" );
            thisBuffer.AppendLine( "console.log('graphsWindRun Menu...');" );
            thisBuffer.AppendLine( "$(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('OnLoad WindRun: ');" );
#endif

            // Watch out if we get more WindGraphs, this then should be the first graph you want to see and it gets a multiple choice
            if ( !GraphWindRose )
                thisBuffer.AppendLine( "    eval('graph' + $('#yearRun option:selected').val() + '();');" );
            thisBuffer.AppendLine( "  $('#yearRun').change(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRun Change: ' + $('#yearRun option:selected').val() );" );
#endif

            thisBuffer.AppendLine( "    eval('graph' + $('#yearRun option:selected').val() + '();');" );
            thisBuffer.AppendLine( "  });" );
            thisBuffer.AppendLine( "  $('#YRNextRun').click(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRun YRNextRun click: ');" );
#endif

            thisBuffer.AppendLine( "    if ( $('#yearRun option:selected').val() != $('#yearRun option').last().val() ) {" );
            thisBuffer.AppendLine( "      $('#yearRun option:selected').next('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "      $('#yearRun').trigger('change');" );
            thisBuffer.AppendLine( "    };" ); // else do nothing
            thisBuffer.AppendLine( "  });" );
            thisBuffer.AppendLine( "  $('#YRPrevRun').click(function() {" );

#if !RELEASE
            thisBuffer.AppendLine( "  console.log('WindRun YRPrevRun click: ');" );
#endif

            thisBuffer.AppendLine( "    if ( $('#yearRun option:selected').val() != $('#yearRun option').first().val() ) {" );
            thisBuffer.AppendLine( "      $('#yearRun option:selected').prev('option').prop('selected', 'selected');" );
            thisBuffer.AppendLine( "      $('#yearRun').trigger('change');" );
            thisBuffer.AppendLine( "    };" ); // else do nothing
            thisBuffer.AppendLine( "  });" );
            thisBuffer.AppendLine( "});" );
            thisBuffer.AppendLine( "</script>" );

            // The user can choose: Alltime or per year, All time generates a list of all months
            // 

            thisBuffer.AppendLine( "<p style='text-align:center;'>" );
            thisBuffer.AppendLine( $"<input type='button' class=buttonFat id='YRPrevRun' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );
            thisBuffer.AppendLine( "<select id='yearRun'>" );
            thisBuffer.AppendLine( $"<option value='AllYears' selected>{Sup.GetCUstringValue( "General", "AllTime", "All Time", false )}</option>" );
            for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
                thisBuffer.AppendLine( $"<option value='{i}'>{i}</option>" );
            thisBuffer.AppendLine( "</select>" );
            thisBuffer.AppendLine( $"<input type='button' class=buttonFat id='YRNextRun' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );
            thisBuffer.AppendLine( "</p>" );
        }

        #endregion

    } // Class
} // Namespace
