using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CumulusUtils
{
    class CUlib
    {
        private readonly CuSupport Sup;

        private readonly bool DoStatistics;
        private readonly string StatisticsType;
        private readonly float Latitude;
        private readonly float Longitude;

        // Constructor
        public CUlib( CuSupport s )
        {
            Sup = s;

            StatisticsType = Sup.GetUtilsIniValue( "Website", "StatisticsType", "" );
            DoStatistics = !string.IsNullOrEmpty( StatisticsType );

            Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );
            Longitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Longitude", "" ), CUtils.Inv );
        }

        public void Generate()
        {
            StringBuilder CUlibFile = new StringBuilder();

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}cumulusutils.js", false, Encoding.UTF8 ) )
            {
                TimeZoneInfo TZ;
                int DST = 0;

                Sup.LogDebugMessage( $"Generating CUlib starting" );

                string tz;

                tz = Sup.GetCumulusIniValue( "Station", "TimeZone", "" );

                if ( TimeZoneInfo.TryFindSystemTimeZoneById( tz, out TZ ) )
                {
                    Sup.LogDebugMessage( $"TimeZone Id:           {TZ.Id} " );
                    Sup.LogDebugMessage( $"TimeZone StandardName: {TZ.StandardName} " );
                    Sup.LogDebugMessage( $"TimeZone DaylightName: {TZ.DaylightName} " );
                    Sup.LogDebugMessage( $"TimeZone DisplayName:  {TZ.DisplayName} " );
                    Sup.LogDebugMessage( $"TimeZone SupportsDaylightSavingTime: {TZ.SupportsDaylightSavingTime}" );
                    Sup.LogDebugMessage( $"TimeZone GetUtcOffset(now): {TZ.GetUtcOffset( DateTime.Now ).Hours}" );
                    Sup.LogDebugMessage( $"TimeZone BaseUtcoffset: {TZ.BaseUtcOffset.Hours}" );

                    if ( TZ.SupportsDaylightSavingTime )
                        DST = TZ.GetUtcOffset( DateTime.Now ).Hours - TZ.BaseUtcOffset.Hours;
                }
                else
                {
                    Sup.LogDebugMessage( $"Internal error - Timezone {tz} not found in IAIN database - Exiting" );
                    Environment.Exit( 0 );
                }

                string DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                string TimeSeparator = ":";
                string DateSeparator = CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator;

                bool ReplaceDecimalSeparator = !DecimalSeparator.Equals( "." );

                Sup.LogDebugMessage( $"Generating CUlib DecSep: |{DecimalSeparator}|, TimeSep: |{TimeSeparator}| and DateSep: |{DateSeparator}| for language {Sup.Language}" );

                // Moved DST to index.html.
                CUlibFile.Append(
                 $"var TZ = {TZ.BaseUtcOffset.Hours};" +
                  "var localTime = new Date();" +
                  "var TZdiffBrowser2UTC = -localTime.getTimezoneOffset()/60;" +
                  "var RT_timer;" +
                  $"var GRAPHS_timer = {Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "10" )};" +  // Not a real timer but a counter for use by the minute function

                  // The defaults for the Home button / charts page :
                  "var ClickEventChart = ['Temp','Pression','Rain','','WindSpeed','WindDir','WindDir','WindSpeed','','','Humidity','Solar','Temp','Temp','Pression','Humidity','','','','','Rain','Rain','Solar','Solar'];" +
                  "var CurrentChart0 = 'Temp'; " +  // Used for the automatic relaod of the charts, changed in case of Compiler use
                  "var CurrentChart = 'Temp'; " +  // Used for the automatic relaod of the charts, Just have it here in case of non-compiler use
                  "var ChartsType = 'default'; " +  // Used to distiguish between different chartsystems: default and compiler at the moment
                  "var ChartsLoaded = false;" + // Used for the automatic relaod of the charts
                  "" +
                  "const urlParams = new URLSearchParams(window.location.search);" +
                  "" +

                  "$(function () {" +
                  "  if ( urlParams.has('report') ) Report2Load = urlParams.get('report');" +
                  "  else Report2Load = 'cumuluscharts.txt';" +
                  "" +
                  "  Promise.allSettled([ $.getScript('lib/CUgauges.js'), " +
                  "                       LoadCUsermenu('CUsermenu.txt'), " +
                  "                       LoadCUserAbout('CUserAbout.txt') ])" +
                  "    .then(() => { " +
                  "      loadRealtimeTxt();" +
                 $"      RT_timer = setInterval(loadRealtimeTxt, {CUtils.UtilsRealTimeInterval} * 1000);" +
                  "      DoGaugeSettings();" +
                  "      worldClockZone(0);" +
                  "      CreateSun();" +
                  "      CreateMoon();" +
                  "      MinuteFunctions();" +
                  "      HourFunctions();" +
                  "" +
                  "      if (Report2Load != 'cumuluscharts.txt' ) LoadUtilsReport(Report2Load);" +
                  "      else LoadUtilsReport('cumuluscharts.txt', true); " +
                  "" +
                  "      console.log('Promise.AllSettled fullfilled...');" +
                  "      console.log('Document Ready function done');" + // Show we're alive
                  "    });" +
                  "});" +
                  // Seconds timer
                  "function worldClockZone(zone) {" +
                  "  $('#CUClocktimeutc').html('UTC:<h4>' + worldClock(0) + '</h4>');" +
                  $"  $('#CUClocktimetz').html('{Sup.GetCUstringValue( "Website", "Station", "Station", true )}:<h4>' + worldClock(TZ + DST) + '</h4>');" +
                  $"  $('#CUClocktimeBrowser').html('{Sup.GetCUstringValue( "Website", "Browser", "Browser", true )}:<h4>' + worldClock(TZdiffBrowser2UTC) + '</h4>');" +
                  "  setTimeout('worldClockZone(0)', 1000);" +
                  "}" +
                  // Minutes timer
                  "function MinuteFunctions() {" +
                  "  if (ChartsLoaded) {" +
                  "    GRAPHS_timer-- ;" +
                  "    if (GRAPHS_timer < 0) {" +
                  "      InitCumulusCharts();" +
#if TRUE
          "      console.log('GRAPHS_Timer < 0 -> Reinitialize the charts: InitCumulusCharts()');" +
#endif
          $"     GRAPHS_timer = {Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "10" )};" + // Reset the update interval
#if !RELEASE
          "      console.log('GRAPHS_timer = ' + GRAPHS_timer + ' Just after reinitialisation');" +
#endif
                  "    }" +
#if !RELEASE
          "    else {" +
                  "      console.log('GRAPHS_timer = ' + GRAPHS_timer + '  Nothing to do');" +
                  "    }" +
                  "  } " +
                  "  else {" +
                  "      console.log('No ChartsLoaded, ignore CHARTS_timer');" +
#endif
          "  }" +
                  "  MoveSunPosition();" +
                  "  setTimeout('MinuteFunctions()', 60000);" +
                  "}" +
                  // Hours timer
                  "function HourFunctions() {" +
                  $" MoveMoonPosition();" +
                  "  setTimeout('HourFunctions()', 60*60000);" +
                  "  return;" +
                  "}" +
                  // Worldclock from BCJkiwi all black MX website
                  "var pad = (x) => x < 10 ? '0' + x : x;" +
                  "function worldClock(zone) {" +
                  "  var time = new Date();" +
                  "  var gmtMS = time.getTime() - (TZdiffBrowser2UTC * 60 * 60000);" +
                  "  var gmtTime = new Date(gmtMS);" +
                  "  var hr = gmtTime.getHours() + zone;" +
                  "  var min = gmtTime.getMinutes();" +
                  "  var sec = gmtTime.getSeconds();" +
                  "  if (hr  < 0  ){hr  = hr + 24;}" +
                  "  if (hr  >= 24){hr  = hr - 24;}" +
                  $"  return pad(hr) + '{TimeSeparator}' + pad(min) + '{TimeSeparator}' + pad(sec);" +
                  "}" +
                  "function ToggleDashboard() {" +
                  "  if ($( '#ExtraAndCustom' ).is ( ':visible' ) ) $('#ExtraAndCustom').hide();" +
                  "  if ($('#Gauges').is (':visible')) {" +
                  "    $('#WindRoseContent').prependTo('#WindRoseGauge1');" +
                  "    $('#WindDirContent').prependTo('#WindDirGauge1');" +
                  "    $('#WindContent').prependTo('#WindGauge1');" +
                  "    $('#Gauges').hide();" +
                  "    $('#Dashboard').show();" +
                  "  }" +
                  "  else" +
                  "  {" +
                  "    $('#WindRoseContent').prependTo('#WindRoseGauge2');" +
                  "    $('#WindDirContent').prependTo('#WindDirGauge2');" +
                  "    $('#WindContent').prependTo('#WindGauge2');" +
                  "    $('#Dashboard').hide();" +
                  "    $('#Gauges').show();" +
                  "  } " +
                  "} " +


                  "var ajaxLoadReportObject = null;" +
                  "function LoadCUserAbout(filename) {" +
                  "  return $.ajax({" +
                  "    url: filename," +
                  "    cache:false," +
                  "    timeout: 2000," +
                  "    headers: { 'Access-Control-Allow-Origin': '*' }," +
                  "    crossDomain: true" +
                  "  })" +
                  "  .done(function (response, responseStatus) { " +
                  "    console.log('Userabout present...Loading');" +
                  "    $('#CUserAboutTxt').html(response);" +
                  "  })" +
                  "  .fail(function (xhr, textStatus, errorThrown) { " +
                  "    console.log('No Userabout present...' + textStatus + ' : ' + errorThrown);" +
                  "  } )" +
                  "}" +
                  "" +
                  "function LoadCUsermenu(filename) {" +
                  "  return $.ajax({" +
                  "    url: filename," +
                  "    cache:false," +
                  "    timeout: 2000," +
                  "    headers: { 'Access-Control-Allow-Origin': '*' }," +
                  "    crossDomain: true," +
                  "  })" +
                  "  .done(function (response, responseStatus) { " +
                  "    console.log('Usermenu present...Loading');" +
                  "    $('#CUsermenu').html(response) } )" +
                  "  .fail(function (xhr, textStatus, errorThrown) { " +
                  "    console.log('No Usermenu present...' + textStatus + ' : ' + errorThrown );" +
                  "  } )" +
                  "}" +
                  "" );

                CUlibFile.Append( "" +
                  "function LoadUtilsReport(ReportName, ChartLoading) {" +
                  "  if (ajaxLoadReportObject !== null) {" +
                  "    ajaxLoadReportObject.abort();" +
                  "    ajaxLoadReportObject = null;" +
                  "    console.log('LoadUtilsReport was busy, current is aborted for ' + ReportName);" +
                  "  }" +
                  "  console.log('LoadUtilsReport... ' + ReportName);" +
                 $"  if (ReportName == '{Sup.StationMapOutputFilename}') DoStationMap = true;" +
                  "  else DoStationMap = false;" +
                 $"  if (ReportName == '{Sup.MeteoCamOutputFilename}') DoWebCam = true;" +
                  "  else DoWebCam = false;" +
                 $"  if ( ReportName != '{Sup.ExtraSensorsOutputFilename}' && ReportName != '{Sup.ExtraSensorsCharts}' &&" +
                 $"       ReportName != '{Sup.CustomLogsOutputFilename}' && ReportName != '{Sup.CustomLogsCharts}' ) {{ " +
                  "    if ($('#ExtraAndCustom').is (':visible') ) {" +
                  "      $('#ExtraAndCustom').hide();" +
                  "      $('#Gauges').hide();" +
                  "      $('#Dashboard').show();" +
                  "    }" +
                  "  } else {" +
                  "    $('#Dashboard').hide();" +
                  "    $('#Gauges').hide();" +
                  "    $('#ExtraAndCustom').show();" +
                  "  }" +
                  "" +
                  "    if (ReportName != 'extrasensorscharts.txt' && ReportName != 'customlogscharts.txt') {" +
                  "      urlParams.delete('report');" +
                  "      urlParams.set('report', ReportName);" +
                  "      history.pushState( null, null, window.location.origin + window.location.pathname + '?' + urlParams);" +
                  "    }" +
                  "" +
                  "  ajaxLoadReportObject = $.ajax({" +
                  "    url: ReportName," +
                  "    cache:false," +
                  "    timeout: 2000," +
                  "    headers: { 'Access-Control-Allow-Origin': '*' }," +
                "  })" +
                "  .done(function (response, responseStatus) {" );

                CUlibFile.Append( DoStatistics ? GenerateStatisticsCode( StatisticsType, true ) : "" );

                CUlibFile.Append( "" +
                  "    $('#CUReportView').html(response);" +
                  "    if (ChartLoading) { " +
                  "      ChartsLoaded = true;" +
                 $"      GRAPHS_timer = {Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "10" )};" + // Set the update interval
                  "    }" +
                  "    else ChartsLoaded = false;" +
                  "" +
                  "    console.log(ReportName + ' loaded OK');" +
                  "    ajaxLoadReportObject = null;" +
                  "" +
                  "  })" +
                  "  .fail( function (xhr, textStatus, errorThrown) {" +
                  "    console.log('LoadReport error' + textStatus + ' : ' + errorThrown);" +
                  "    ajaxLoadReportObject = null;" + // End error callback
                  "  });" +
                  "" +
                  "  return ajaxLoadReportObject;" +  // End StatusCode & End .Ajax call
                  "}" +  // End function LoadUtilsReport

                "function loadRealtimeTxt() {" +
                "  $.ajax({" +
                $"    url: '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}realtime.txt'," +
                "    cache:false," +
                "    timeout: 2000," +
                "    headers: { 'Access-Control-Allow-Origin': '*' }," +
                "    crossDomain: true" +
                "  })" +
                "  .done( function (response, responseStatus) {" +
                "    DoRealtime(response);" +
                "  })" +
                "  .fail( function (xhr, textStatus, errorThrown) {" +
                "    console.log('realtime.txt ' + textStatus + ' : ' + errorThrown);" +
                "  })" +
                "}" +

                "var oldobs = [58];" +
                  "var BeaufortColour = ['#ffffff', '#ccffff', '#99ffcc', '#99ff99', '#99ff66', '#99ff00', '#99cc00', '#cccc00', '#ffcc00', '#ff9900', '#ff6600', '#ff3300', '#ff0000'];" +
                  "function ClearChangeIndicator() {" +
                  "  $('[id*=\"ajx\"]').css('color', '');" +
                  "}" +
                  "function DoRealtime(input) {" +
                  "let tmpInput = input;" );

                //if ( ReplaceDecimalSeparator ) CUlibFile.Append( $"  tmpInput = tmpInput.replace(/\\./g, '{DecimalSeparator}');" );
                // Do not replace the date separator if it is a point
                if ( ReplaceDecimalSeparator )
                    CUlibFile.Append( $"  tmpInput = tmpInput.substring(0,9)  + tmpInput.substring(9).replace(/\\./g, '{DecimalSeparator}');" );

                CUlibFile.Append( "" +
                          "  let realtime = tmpInput.split(' '); " +
                          $"  let UnitWind = '{Sup.StationWind.Text()}';" +
                          $"  let UnitDegree = realtime[{(int) RealtimeFields.tempunitnodeg}] == 'C' ? '°C' : '°F';" +
                          $"  let UnitPress = realtime[{(int) RealtimeFields.pressunit}] == 'in' ? 'inHg' : realtime[{(int) RealtimeFields.pressunit}];" +
                          $"  let UnitRain = realtime[{(int) RealtimeFields.rainunit}];" +

                          // This one is only for the StationMap, if that one is not active don't do this (needs to be implemented)
                          //                                    angle        Wind in Bf    Temp         Barometer     Humidity     Rain
                          $"  if (DoStationMap) UpdateWindArrow( realtime[{(int) RealtimeFields.bearing}], realtime[{(int) RealtimeFields.beaufortnumber}], " +
                          $"                                     realtime[{(int) RealtimeFields.temp}], realtime[{(int) RealtimeFields.press}], " +
                          $"                                     realtime[{(int) RealtimeFields.hum}], realtime[{(int) RealtimeFields.rfall}] );" +
                          "  if (DoWebCam) UpdateWebCam();" +

                          $"  if ( oldobs[{(int) RealtimeFields.temp}] != realtime[{(int) RealtimeFields.temp}] ) {{" +
                          $"    oldobs[{(int) RealtimeFields.temp}] = realtime[{(int) RealtimeFields.temp}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.temp}] + '&nbsp;' + UnitDegree;" +
                          $"    $('#ajxCurTemp').html(tmp);" +
                          $"    $('#ajxCurTemp').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.temptrend}] != realtime[{(int) RealtimeFields.temptrend}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.temptrend}] = realtime[{(int) RealtimeFields.temptrend}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.temptrend}] + '&nbsp;' + UnitDegree + '{Sup.PerHour}';" +
                          $"    $('#ajxTempChange').html(tmp);" +
                          $"    $('#ajxTempChange').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  tmp = realtime[{(int) RealtimeFields.temptrend}][0] == '+' ? '<b style=\"color:{Sup.GetUtilsIniValue( "Website", "ColorDashboardUpIndicator", "Chartreuse" )}\"> \\u25B2 </b>' : '<b style=\"color:{Sup.GetUtilsIniValue( "Website", "ColorDashboardDownIndicator", "Red" )}\"> \\u25BC </b>';" +
                          $"  $('#ajxTempChangeIndicator').html(tmp);" +
                          $"  if (oldobs[{(int) RealtimeFields.tempTH}] != realtime[{(int) RealtimeFields.tempTH}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.tempTH}] = realtime[{(int) RealtimeFields.tempTH}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "MaxToday", "Max today", true )}: ' + realtime[{(int) RealtimeFields.tempTH}] + '&nbsp;' + UnitDegree;" +
                          $"    $('#ajxTempMax').html(tmp);" +
                          $"    $('#ajxTempMax').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  tmp = '@ ' + realtime[{(int) RealtimeFields.TtempTH}]; $('#ajxTimeTempMax').html(tmp);" +
                          $"  if (oldobs[{(int) RealtimeFields.tempTL}] != realtime[{(int) RealtimeFields.tempTL}]) {{" +
                          $"    oldobs[28] = realtime[{(int) RealtimeFields.tempTL}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "MinToday", "Min today", true )}: ' + realtime[{(int) RealtimeFields.tempTL}] + '&nbsp;' + UnitDegree;" +
                          $"    $('#ajxTempMin').html(tmp);" +
                          $"    $('#ajxTempMin').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  tmp = '@ ' + realtime[{(int) RealtimeFields.TtempTL}]; $('#ajxTimeTempMin').html(tmp);" +
                          $"  if (oldobs[{(int) RealtimeFields.press}] != realtime[{(int) RealtimeFields.press}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.press}] = realtime[{(int) RealtimeFields.press}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.press}] + '&nbsp;' + UnitPress; " +
                          $"    $('#ajxCurPression').html(tmp);" +
                          $"    $('#ajxCurPression').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.presstrendval}] != realtime[{(int) RealtimeFields.presstrendval}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.presstrendval}] = realtime[{(int) RealtimeFields.presstrendval}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.presstrendval}] + '&nbsp;' + UnitPress + '{Sup.PerHour}';" +
                          $"    $('#ajxBarChange').html(tmp);" +
                          $"    $('#ajxBarChange').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  tmp = realtime[{(int) RealtimeFields.presstrendval}][0] == '+' ? '<b style=\"color:{Sup.GetUtilsIniValue( "Website", "ColorDashboardUpIndicator", "Chartreuse" )}\"> \\u25B2 </b>' : ' <b style=\"color:{Sup.GetUtilsIniValue( "Website", "ColorDashboardDownIndicator", "Red" )}\"> \\u25BC </b>';" +
                          "  $('#ajxBarChangeIndicator').html(tmp);" +
                          $"  if (oldobs[{(int) RealtimeFields.pressTH}] != realtime[{(int) RealtimeFields.pressTH}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.pressTH}] = realtime[{(int) RealtimeFields.pressTH}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "MaxToday", "Max today", true )}: ' + realtime[{(int) RealtimeFields.pressTH}] + '&nbsp;' + UnitPress;" +
                          $"    $('#ajxBarMax').html(tmp);" +
                          $"    $('#ajxBarMax').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  tmp = '@&nbsp;' + realtime[{(int) RealtimeFields.TpressTH}];" +
                          $"  $('#ajxTimeBarMax').html(tmp);" +
                          $"  if (oldobs[{(int) RealtimeFields.pressTL}] != realtime[{(int) RealtimeFields.pressTL}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.pressTL}] = realtime[{(int) RealtimeFields.pressTL}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "MinToday", "Min today", true )}: ' + realtime[{(int) RealtimeFields.pressTL}] + '&nbsp;' + UnitPress;" +
                          $"    $('#ajxBarMin').html(tmp);" +
                          $"    $('#ajxBarMin').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  tmp = '@&nbsp;' + realtime[{(int) RealtimeFields.TpressTL}];" +
                          "  $('#ajxTimeBarMin').html(tmp);" +
                          $"  if (oldobs[{(int) RealtimeFields.rfall}] != realtime[{(int) RealtimeFields.rfall}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.rfall}] = realtime[{(int) RealtimeFields.rfall}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.rfall}] + '&nbsp;' + UnitRain;" +
                          $"    $('#ajxRainToday').html(tmp);" +
                          $"    $('#ajxRainToday').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.rrate}] != realtime[{(int) RealtimeFields.rrate}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.rrate}] = realtime[{(int) RealtimeFields.rrate}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "Rainrate", "Rain Rate", true )}: ' + realtime[{(int) RealtimeFields.rrate}] + '&nbsp;' + UnitRain + '{Sup.PerHour}';" +
                          $"    $('#ajxRainRateNow').html(tmp);" +
                          $"    $('#ajxRainRateNow').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +

                          $"  tmp = '{Sup.GetCUstringValue( "Website", "Yesterday", "Yesterday", true )}: ' + realtime[{(int) RealtimeFields.rfallY}] + '&nbsp;' + UnitRain;" +
                          "  $('#ajxRainYesterday').html(tmp);" +
                          $"  tmp = '{Sup.GetCUstringValue( "General", "Week", "Week", true )}: ' + realtime[{(int) RealtimeFields.rweek}] + '&nbsp;' + UnitRain;" +
                          "  $('#ajxRainWeek').html(tmp);" +
                          $"  tmp = '{Sup.GetCUstringValue( "General", "Month", "Month", true )}: ' + realtime[{(int) RealtimeFields.rmonth}] + '&nbsp;' + UnitRain;" +
                          "  $('#ajxRainMonth').html(tmp);" +
                          $"  tmp = '{Sup.GetCUstringValue( "General", "Year", "Year", true )}: ' + realtime[{(int) RealtimeFields.ryear}] + '&nbsp;' + UnitRain;" +
                          "  $('#ajxRainYear').html(tmp);" +

                          $"  if (oldobs[{(int) RealtimeFields.wlatest}] != realtime[{(int) RealtimeFields.wlatest}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.wlatest}] = realtime[{(int) RealtimeFields.wlatest}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.wlatest}] + '&nbsp;' + UnitWind;" +
                          $"    $('#ajxCurWind').html(tmp);" +
                          $"    $('#ajxCurWind').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.wspeed}] != realtime[{(int) RealtimeFields.wspeed}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.wspeed}] = realtime[{(int) RealtimeFields.wspeed}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.wspeed}] + '&nbsp;' + UnitWind;" +
                          $"    $('#ajxAverageWind').html(tmp);" +
                          $"    $('#ajxAverageWind').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +

                          $"    tmp = '&nbsp;' + realtime[{(int) RealtimeFields.beaufortnumber}] + '&nbsp;Bf&nbsp;';" +
                          $"    $('#ajxCurWindBf').html(tmp);" +
                          $"    $('#ajxCurWindBf').css('background-color', BeaufortColour[parseInt(realtime[{(int) RealtimeFields.beaufortnumber}])] ); " +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.windTM}] != realtime[{(int) RealtimeFields.windTM}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.windTM}] = realtime[{(int) RealtimeFields.windTM}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "HighAverage", "High Average", true )}: ' + realtime[{(int) RealtimeFields.windTM}] + '&nbsp;' + UnitWind;" +
                          $"    $('#ajxHighAverage').html(tmp);" +
                          $"    $('#ajxHighAverage').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +

                          $"  tmp = '@&nbsp;' + realtime[{(int) RealtimeFields.TwindTM}];" +
                          $"  $('#ajxTimeHighAverage').html(tmp);" +

                          $"  if (oldobs[{(int) RealtimeFields.wgustTM}] != realtime[{(int) RealtimeFields.wgustTM}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.wgustTM}] = realtime[{(int) RealtimeFields.wgustTM}];" +
                          $"    tmp = '{Sup.GetCUstringValue( "Website", "HighGust", "High Gust", true )}: ' + realtime[{(int) RealtimeFields.wgustTM}] + '&nbsp;' + UnitWind;" +
                          $"    $('#ajxHighGust').html(tmp);" +
                          $"    $('#ajxHighGust').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +

                          $"  tmp = '@&nbsp;' + realtime[{(int) RealtimeFields.TwgustTM}];" +
                          $"  $('#ajxTimeHighGust').html(tmp);" +

                          $"  if (oldobs[{(int) RealtimeFields.hum}] != realtime[{(int) RealtimeFields.hum}]) {{" +
                          $"    oldobs[3] = realtime[{(int) RealtimeFields.hum}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.hum}] + '&nbsp;%';" +
                          $"    $('#ajxCurHumidity').html(tmp);" +
                          $"    $('#ajxCurHumidity').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.dew}] != realtime[{(int) RealtimeFields.dew}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.dew}] = realtime[{(int) RealtimeFields.dew}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.dew}] + '&nbsp;' + UnitDegree;" +
                          $"    $('#ajxDewpoint').html(tmp);" +
                          $"    $('#ajxDewpoint').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.SolarRad}] != realtime[{(int) RealtimeFields.SolarRad}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.SolarRad}] = realtime[{(int) RealtimeFields.SolarRad}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.SolarRad}] + '&nbsp;W/m²';" +
                          $"    $('#ajxCurSolar').html(tmp);" +
                          $"    $('#ajxCurSolar').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.CurrentSolarMax}] != realtime[{(int) RealtimeFields.CurrentSolarMax}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.CurrentSolarMax}] = realtime[{(int) RealtimeFields.CurrentSolarMax}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.CurrentSolarMax}] + '&nbsp;W/m²';" +
                          $"    $('#ajxCurSolarMax').html(tmp);" +
                          $"    $('#ajxCurSolarMax').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.SunshineHours}] != realtime[{(int) RealtimeFields.SunshineHours}]) {{" +
                          $"    oldobs[{(int) RealtimeFields.SunshineHours}] = realtime[{(int) RealtimeFields.SunshineHours}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.SunshineHours}];" +  // + '&nbsp;{Sup.GetCUstringValue( "General", "Hours", "hours", true )}
                          $"    $('#ajxSolarHours').html(tmp);" +
                          $"    $('#ajxSolarHours').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +
                          $"  if (oldobs[{(int) RealtimeFields.UV}] != realtime[{(int) RealtimeFields.UV}]) {{" +
                          $"    oldobs[43] = realtime[{(int) RealtimeFields.UV}];" +
                          $"    tmp = realtime[{(int) RealtimeFields.UV}];" +
                          $"    $('#ajxCurUVindex').html(tmp);" +
                          $"    $('#ajxCurUVindex').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" +
                          "  }" +

                          $"  tmp = '{Sup.GetCUstringValue( "Website", "LastUpdate", "Last Update", true )}: ' + realtime[{(int) RealtimeFields.timehhmmss}];" +
                          $"  $('#ajxTimeUpdate').html(tmp);" +

                          // Don't assume the webserver has the same date as the CMX machine so do the date as if you do not know anything
                          $"  SplitDateArray = realtime[{(int) RealtimeFields.date}].split(realtime[{(int) RealtimeFields.date}][2]);" +
                          $"  thisDay = SplitDateArray[0];" +
                          $"  thisMonth = SplitDateArray[1];" +
                          $"  thisYear = SplitDateArray[2];" +
                          $"  newDate = new Date('20' + thisYear, thisMonth - 1, thisDay);" +
                          $"  tmp = '{Sup.GetCUstringValue( "Website", "Date", "Date", true )}: ' + new Intl.DateTimeFormat('{Sup.Locale}').format(newDate);" +
                          //$"  tmp = '{Sup.GetCUstringValue( "Website", "Date", "Date", true )}: ' + realtime[{(int) RealtimeFields.date}];" +
                          $"  $('#ajxDateUpdate').html(tmp);" +
                          "  setTimeout('ClearChangeIndicator()', 3000);" +
                          "}" );

                // Do the SUN procedure

                CUlibFile.Append( "" +
                         $"var Latitude = {Latitude.ToString( "F4", CUtils.Inv )};" +
                         $"var Longitude = {Longitude.ToString( "F4", CUtils.Inv )};" +
                          "var Radius = 60;" +
                          "var ST, STT;" +
                          "var haveDay, haveCivil, haveNautical, haveAstronomical, haveNight, NorthernLight, SouthernLight;" +
                          "function CreateSun() {" +
                          "  var basedata = [];" +
                          "  var StartAngle;" +
                          "  var thisDate = new Date();" +
                          "  ST = SunCalc.getTimes(thisDate, Latitude, Longitude);" +
                          "  STT = SunCalc.getTimes(thisDate.setDate(thisDate.getDate() + 1), Latitude, Longitude);" +
                          "  for (let sunEvent in ST) {" +
                          "    ST[sunEvent].setHours(ST[sunEvent].getHours() + TZ + DST - TZdiffBrowser2UTC);" +
                          "    STT[sunEvent].setHours(STT[sunEvent].getHours() + TZ + DST - TZdiffBrowser2UTC);" +
                          "  }" +
                          "  haveDay = !isNaN(ST.sunrise) && !isNaN(ST.sunset);" +
                          "  haveCivil = !isNaN(ST.sunset) && !isNaN(ST.dusk);" +
                          "  haveNautical = !isNaN(ST.dusk) && !isNaN(ST.nauticalDusk);" +
                          "  haveAstronomical = !isNaN(ST.nauticalDusk) && !isNaN(ST.night);" +
                          "  haveNight = !isNaN(ST.night) && !isNaN(STT.nightEnd);" +
                          "  NorthernLight = false; SouthernLight = false;" +
                          "  if (!haveDay && !haveCivil && !haveNautical && !haveAstronomical) {" +
                          "    if (Latitude > 66) {" +
                          "      if (localTime > Date.parse('21 March ' + localTime.getFullYear()) && localTime < Date.parse('21 September ' + localTime.getFullYear())) {" +
                          "        NorthernLight = true; basedata[0] = 100;" +
                          "        SouthernLight = false;" +
                          $"        $('#CUsunrise').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsAboveHorizon", "24 hrs above hor.", true )}');" +
                          $"        $('#CUsunset').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsAboveHorizon", "24 hrs above hor.", true )}');" +
                          "      }" +
                          "      else {" +
                          "        NorthernLight = false; basedata[4] = 100;" +
                          "        SouthernLight = true;" +
                          $"        $('#CUsunrise').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsBelowHorizon", "24 hrs below hor.", true )}');" +
                          $"        $('#CUsunset').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsBelowHorizon", "24 hrs below hor.", true )}');" +
                          "      }" +
                          "    }" +
                          "    else if (Latitude < 66) {" +
                          "      if (localTime < Date.parse('21 March ' + localTime.getFullYear()) || localTime > Date.parse('21 September ' + localTime.getFullYear())) {" +
                          "        SouthernLight = true; basedata[0] = 100;" +
                          "        NorthernLight = false;" +
                          $"        $('#CUsunrise').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsAboveHorizon", "24 hrs above hor.", true )}');" +
                          $"        $('#CUsunset').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsAboveHorizon", "24 hrs above hor.", true )}');" +
                          "      }" +
                          "      else {" +
                          "        SouthernLight = false; basedata[4] = 100;" +
                          "        NorthernLight = true;" +
                          $"        $('#CUsunrise').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsBelowHorizon", "24 hrs below hor.", true )}');" +
                          $"        $('#CUsunset').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsBelowHorizon", "24 hrs below hor.", true )}');" +
                          "      }" +
                          "    }" +
                          "  } " +
                          "  else {" +
                          "    if (haveDay) {basedata[0] = DurationOfPartOfDay(ST.sunrise, ST.sunset);}" +
                          "    if (haveCivil) {" +
                          "      basedata[1] = DurationOfPartOfDay(ST.sunset, ST.dusk);" +
                          "      basedata[7] = DurationOfPartOfDay(ST.dawn, ST.sunrise);" +
                          "    }" +
                          "    else { " +
                          "      basedata[1] = (100 - basedata[0]) / 2;" +
                          "      basedata[7] = basedata[1];" +
                          "    }" +
                          "    if (haveNautical) {" +
                          "      basedata[2] = DurationOfPartOfDay(ST.dusk, ST.nauticalDusk);" +
                          "      basedata[6] = DurationOfPartOfDay(ST.nauticalDawn, ST.dawn);" +
                          "    }" +
                          "    else {" +
                          "      if (haveCivil) {" +
                          "        basedata[2] = (100 - basedata[0] - 2 * basedata[1]) / 2;" +
                          "        basedata[6] = basedata[2];" +
                          "      }" +
                          "    }" +
                          "    if (haveAstronomical) {" +
                          "      basedata[3] = DurationOfPartOfDay(ST.nauticalDusk, ST.night);" +
                          "      basedata[4] = DurationOfPartOfDay(ST.night, STT.nightEnd);" +
                          "      basedata[5] = DurationOfPartOfDay(ST.nightEnd, ST.nauticalDawn);" +
                          "    }" +
                          "    else {" +
                          "      if (haveNautical) {" +
                          "        basedata[3] = (100 - basedata[0] - 2 * basedata[1] - 2 * basedata[2]) / 2;" +
                          "        basedata[5] = basedata[3];" +
                          "      }" +
                          "    }" +
                          "    $('#CUsunrise').html(HHmm(ST.sunrise));" +
                          "    $('#CUsunset').html(HHmm(ST.sunset));" +

                          // OK: this might be done a bit more efficient but I wanted to use the functions HHmm and DurationOfPartOfDay for the checks 
                          //     and formatting within. Maybe later
                          //
                          "    let daylength = DurationOfPartOfDay(ST.sunrise, ST.sunset) * DayMilliSeconds / 100;" +
                          "    let hrs = Math.floor(daylength / 1000 / 60 / 60); let rest = daylength - hrs * 1000 * 60 * 60; let mins = Math.floor(rest / 1000 / 60);" +
                          "    $('#CUdaylength').html( HHmm( new Date(1970, 01, 01, hrs, mins) ) );" +
                          "  }" +
                          "  if (haveDay) StartAngle = Math.PI * (1 / 12 * (ST.sunrise.getHours() + ST.sunrise.getMinutes() / 60) - 1);" +
                          "  else StartAngle = 0;" +
                          "  const svgContainer = d3.select('#d3SunDisc');" +
                          "  svgContainer.append('svg')" +
                          "    .attr('viewBox', [-Radius, -Radius, 2*Radius, 2*Radius])" +
                          "    .attr('width', 2*Radius)" +
                          "    .attr('height', 2*Radius);" +
                          "  var myColor = ['#ffff40', '#9FF4FE', '#5876E2', '#4156A6', '#180746', '#4156A6', '#5876E2', '#9FF4FE'];" +   // yellow, Civil blue, nautical blue, Astronomical blue, night blue, etc...
                          "  const arcs = d3.pie()" +
                          "    .startAngle(StartAngle)" +
                          "    .sort(null)(basedata);" +
                          "  const arc = d3.arc()" +
                          "    .innerRadius(0)" +
                          "    .outerRadius(Radius - 1);" +
                          "  const pie = svgContainer.select('svg').append('g')" +
                          "    .selectAll('path')" +
                          "    .data(arcs)" +
                          "    .join('path')" +
                          "    .attr('d', arc)" +
                          "    .attr('fill', function (d, i) { return myColor[i]; });" +
                          "  const line = svgContainer.select('svg')" +
                          "    .append('line')" +
                          "    .attr('id', 'HandOfClock')" +
                          "    .attr('x1', 0).attr('y1', 0).attr('x2', 0).attr('y2', Radius)" +
                          "    .attr('stroke', 'red')" +
                          "    .attr('stroke-width', '2');" +
                          "}" );

                string backgroundImageDay = Sup.GetUtilsIniValue( "Website", "ColorTitleBackGroundImage", "" );

                string backgroundImageCivil = Sup.GetUtilsIniValue( "Website", "ColorTitleBackGroundImageCivil", "" );
                if ( string.IsNullOrEmpty( backgroundImageCivil ) ) backgroundImageCivil = backgroundImageDay;

                string backgroundImageNautical = Sup.GetUtilsIniValue( "Website", "ColorTitleBackGroundImageNautical", "" );
                if ( string.IsNullOrEmpty( backgroundImageNautical ) ) backgroundImageNautical = backgroundImageCivil;

                string backgroundImageAstronomical = Sup.GetUtilsIniValue( "Website", "ColorTitleBackGroundImageAstronomical", "" );
                if ( string.IsNullOrEmpty( backgroundImageAstronomical ) ) backgroundImageAstronomical = backgroundImageNautical;

                string backgroundImageNight = Sup.GetUtilsIniValue( "Website", "ColorTitleBackGroundImageNight", "" );
                if ( string.IsNullOrEmpty( backgroundImageNight ) ) backgroundImageNight = backgroundImageAstronomical;

                CUlibFile.Append( "const DayPhase = Object.freeze({" +
                    "DAY: Symbol( 'day' )," +
                    "CIVIL: Symbol( 'civil' )," +
                    "NAUTICAL: Symbol( 'nautical' )," +
                    "ASTRONOMICAL: Symbol( 'astronomical' )," +
                    "NIGHT: Symbol( 'night' )}); " );

                CUlibFile.Append( "function isValidDate( d ) {" +
                    "return d instanceof Date && !isNaN( d );" +
                "}" );

                //I will use isValidDate which I found  here: https://stackoverflow.com/questions/1353684/detecting-an-invalid-date-date-instance-in-javascript

                CUlibFile.Append( "var nowImage, prevImage;" );

                CUlibFile.Append( "function MoveSunPosition() {" +
                    "  date = new Date();" +

                    "  hours = date.getHours() + TZ + DST - TZdiffBrowser2UTC;" +
                    "  minutes = date.getMinutes();" +
                    "  angle = (hours + minutes / 60) / 24 * 360;" +
                    "  const line = d3.select('#HandOfClock')" +
                    "     .attr('transform', 'rotate(' + angle + ')');" +

                    "  if ( NorthernLight || SouthernLight ) {" +
                    $"    if ( NorthernLight && Latitude > 66 ) {{ nowImage = '{backgroundImageDay}'; }}" +
                    $"    else if ( SouthernLight && Latitude < 66 ) {{ nowImage = '{backgroundImageDay}'; }}" +
                    $"    else {{ nowImage = '{backgroundImageNight}'; }}" +
                    "  }" +
                    "  else {" +
                    $"    if ( prevImage == undefined ) nowImage = '{backgroundImageNight}';" +
                    $"    if ( date > ST.nightEnd ) {{ nowImage = '{backgroundImageAstronomical}'; }}" +
                    $"    if ( date > ST.nauticalDawn ) {{ nowImage = '{backgroundImageNautical}'; }}" +
                    $"    if ( date > ST.dawn ) {{ nowImage = '{backgroundImageCivil}'; }}" +
                    $"    if ( date > ST.sunrise ) {{ nowImage = '{backgroundImageDay}'; }}" +
                    $"    if ( date > ST.sunset ) {{ nowImage = '{backgroundImageCivil}'; }}" +
                    $"    if ( date > ST.dusk ) {{ nowImage = '{backgroundImageNautical}'; }}" +
                    $"    if ( date > ST.nauticalDusk ) {{ nowImage = '{backgroundImageAstronomical}'; }}" +
                    $"    if ( date > ST.night ) {{ nowImage = '{backgroundImageNight}'; }}" +
                    "  }" +
                    "  if (nowImage != prevImage){" +
                    "    console.log( 'Setting header image ' + nowImage + ' at ' + date );" +
                    "    $( '.CUTitle' ).css( 'background-image', 'url(' + nowImage + ')' );" +
                    "    prevImage = nowImage;" +
                    "  }" +
                    "}" );

                // Do the MOON procedure
                bool UseCMXMoonImage = Sup.GetUtilsIniValue( "Website", "UseCMXMoonImage", "false" ).Equals( "true", CUtils.Cmp );

                if ( UseCMXMoonImage )
                {
                    Sup.LogDebugMessage( $"Generating CUlib Using CMX Moon image" );

                    string MoonImageLocation = Sup.GetUtilsIniValue( "Website", "MoonImageLocation", "" );

                    CUlibFile.Append(
                        "function CreateMoon() {" +
                        //$"    tmpMoon = '<img src=\"{Sup.GetCumulusIniValue( "Graphs", "MoonImageFtpDest", "" )}\">';" +
                        $"    tmpMoon = '<img src=\"{MoonImageLocation}\">';" +
                        "    $('#d3MoonDisc').html(tmpMoon);" +
                        "}" +
                        "function MoveMoonPosition() {" +
                        "  var MoonTimes;" +
                        "  var thisDate = new Date();" +
                        "  MoonTimes = SunCalc.getMoonTimes(thisDate, Latitude, Longitude);" +
                        "  Illum = SunCalc.getMoonIllumination(thisDate);" +
                        "  if (MoonTimes.alwaysUp)" +
                        $"    $('#CUmoonrise').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsAboveHorizon", "24 hrs above hor.", true )}');" +
                        "  else if ((MoonTimes.alwaysDown))" +
                        $"    $('#CUmoonset').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsBelowHorizon", "24 hrs below hor.", true )}');" +
                        "  else {" +
                        "    for (let moonEvent in MoonTimes) {" +
                        "      MoonTimes[moonEvent].setHours(MoonTimes[moonEvent].getHours() + TZ + DST - TZdiffBrowser2UTC);" +
                        "    }" +
                        "    if (!isNaN(MoonTimes.rise)) { $('#CUmoonrise').html(HHmm(MoonTimes.rise)); } else { $('#CUmoonrise').html('--:--'); } " +
                        "    if (!isNaN(MoonTimes.set)) { $('#CUmoonset').html(HHmm(MoonTimes.set)); } else { $('#CUmoonset').html('--:--'); }" +
                        "  }" +
                        "}"
                        );
                }
                else
                {
                    Sup.LogDebugMessage( $"Generating CUlib Using CUtils Moonsimulation" );

                    CUlibFile.Append(
                              "var MoonRadius = 40;" +
                              "var MoonLight = '#ffff80';" +
                              "var MoonShadow = 'grey';" +
                              "function CreateMoon() {" +
                              "  const svgContainer = d3.select('#d3MoonDisc')" +
                              "    .append('svg')" +
                              "    .attr('id', 'moonDisc')" +
                              "    .attr('viewBox', [-Radius, -Radius, 2 * Radius, 2 * Radius])" +
                              "    .attr('width', 2 * Radius)" +
                              "    .attr('height', 2 * Radius)" +
                              "    .attr('style', 'mix-blend-mode: normal');" +
                              "  if (Latitude < 0) { svgContainer.attr('transform', 'rotate(180)'); }" +
                              "  const moon = d3.select('#moonDisc')" +
                              "    .append('circle')" +
                              "    .attr('id','baseMoon')" +
                              "    .attr('r', MoonRadius)" +
                              "    .attr('stroke', 'lightgrey')" +
                              "    .attr('stroke-width', 1)" +
                              "    .attr('fill', MoonShadow);" +
                              "  const semiMoon = d3.select('#moonDisc')" +
                              "    .append('path')" +
                              "    .attr('id', 'semiMoon');" +
                              "  const moonEllipse = d3.select('#moonDisc')" +
                              "    .append('ellipse')" +
                              "    .attr('id','ellipseMoon')" +
                              "    .attr('rx', MoonRadius)" +
                              "    .attr('ry', MoonRadius)" +
                              "    .attr('fill', MoonLight);" +
                              "}" +
                              "function MoveMoonPosition(){" +
                              "  var MoonTimes;" +
                              "  var BaseMoonColor = '';" +
                              "  var EllipseMoonColor = '';" +
                              "  var SemiMoonColor = '';" +
                              "  var CurrEndAngle = Math.PI;" +
                              "  var thisDate = new Date();" +
                              "  MoonTimes = SunCalc.getMoonTimes(thisDate, Latitude, Longitude);" +
                              "  Illum = SunCalc.getMoonIllumination(thisDate);" +
                              "  if (MoonTimes.alwaysUp)" +
                              $"    $('#CUmoonrise').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsAboveHorizon", "24 hrs above hor.", true )}');" +
                              "  else if ((MoonTimes.alwaysDown))" +
                              $"    $('#CUmoonset').html('<br/>{Sup.GetCUstringValue( "Website", "24HrsBelowHorizon", "24 hrs below hor.", true )}');" +
                              "  else {" +
                              "    for (let moonEvent in MoonTimes) {" +
                              "      MoonTimes[moonEvent].setHours(MoonTimes[moonEvent].getHours() + TZ + DST - TZdiffBrowser2UTC);" +
                              "    }" +
                              "    if (!isNaN(MoonTimes.rise)) { $('#CUmoonrise').html(HHmm(MoonTimes.rise)); } else { $('#CUmoonrise').html('--:--'); } " +
                              "    if (!isNaN(MoonTimes.set)) { $('#CUmoonset').html(HHmm(MoonTimes.set)); } else { $('#CUmoonset').html('--:--'); }" +
                              "  }" +
                              "  if (Illum.phase >= 0.5) {" +
                              "    BaseMoonColor = MoonLight;" +
                              "    SemiMoonColor = MoonShadow;" +
                              "    ellipseXradius = Math.abs(Illum.phase - 0.75) / 0.25 * MoonRadius;" +
                              "    if (Illum.phase > 0.75) {" +
                              "      EllipseMoonColor = MoonShadow;" +
                              "    }" +
                              "    else {" +
                              "      EllipseMoonColor = MoonLight;" +
                              "    }" +
                              "  }" +
                              "  else {" +
                              "    BaseMoonColor = MoonShadow;" +
                              "    SemiMoonColor = MoonLight;" +
                              "    ellipseXradius = Math.abs(Illum.phase - 0.25) / 0.25 * MoonRadius;" +
                              "    if (Illum.phase > 0.25) {" +
                              "      EllipseMoonColor = MoonLight;" +
                              "    }" +
                              "    else {" +
                              "      EllipseMoonColor = MoonShadow;" +
                              "    }" +
                              "  }" +
                              "  const arc = d3.arc()" +
                              "    .innerRadius(0)" +
                              "    .outerRadius(MoonRadius)" +
                              "    .startAngle(0)" +
                              "    .endAngle(CurrEndAngle);" +
                              "  const baseMoon = d3.select('#baseMoon')" +
                              "    .attr('fill', BaseMoonColor);" +
                              "  const semiMoon = d3.select('#semiMoon')" +
                              "    .attr('d', arc)" +
                              "    .attr('fill', SemiMoonColor);" +
                              "  const ellipseMoon = d3.select('#ellipseMoon')" +
                              "    .attr('rx', ellipseXradius)" +
                              "    .attr('fill', EllipseMoonColor);" +
                              "}" );

                }

                CUlibFile.Append(
                          "var DayMilliSeconds = 24*60*60*1000;" +
                          "function DurationOfPartOfDay(start, end) {" +
                          "  if ( isNaN(start) ) return 0;" +
                          "  if ( isNaN(end) ) return 0;" +
                          "  var diff = end - start;" +
                          "  return diff / DayMilliSeconds * 100;" +
                          "}" +
                          "function HHmm(date) {" +
                          "  var hours = date.getHours();" +
                          "  var minutes = date.getMinutes();" +
                          "  if (hours < 10) { hours = '0' + hours; }" +
                          "  if (minutes < 10) { minutes = '0' + minutes; }" +
                          $"  return hours + '{TimeSeparator}' + minutes;" +
                          "}" +
                          //"var ClickEventChart = ['','','','','','','','','','','','','','','','','','','','','','','',''];" +
                          "function ClickGauge(PaneNr) {" +
                          "  if ($.trim(ClickEventChart[PaneNr-1]).length !== 0) {" +
                          "    if (!urlParams.has('report', 'cumuluscharts.txt') ) {" +
                          "      LoadUtilsReport('cumuluscharts.txt', true);" +
                          "    }" +
                          "    if (!urlParams.has('dropdown', ClickEventChart[PaneNr-1])) {" +
                          "      urlParams.delete('dropdown');" +
                          "      urlParams.set('dropdown', ClickEventChart[PaneNr-1]);" +
                          "      history.pushState(null, null, window.location.origin + window.location.pathname + '?' + urlParams);" +
                          "    }" +
                          "    document.getElementById('graph0').value = ClickEventChart[PaneNr-1];" +
                          "    InitCumulusCharts();" +
                          "  }" +
                          "}" +
                          "" );

                CUlibFile.Append(
                          "function DayNumber2Date(dayNumber, year){" +
                          "  const date = new Date( year, 0, dayNumber);" +
                          $"  return date.toLocaleDateString('{Sup.Locale}');" +
                          "}" );

                // Now, do all checks on the individual steelseries parameters which are used 
                // This is a tiresome process, adding a parameter is awkward. Unfortunately, no check are made in the gauges or steelseries software
                //
                Sup.LogTraceInfoMessage( $"cumulusutils.js generation: Checking gauges parameters" );

                string ShowIndoorTempHum = Sup.GetUtilsIniValue( "Website", "ShowInsideMeasurements", "false" ).ToLowerInvariant();
                if ( !ShowIndoorTempHum.Equals( "true" ) && !ShowIndoorTempHum.Equals( "false" ) )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : ShowInsideMeasurements" );
                    ShowIndoorTempHum = "false";
                }

                string ShowUVStr = Sup.GetUtilsIniValue( "Website", "ShowUV", "true" ).ToLowerInvariant();
                if ( !ShowUVStr.Equals( "true" ) && !ShowUVStr.Equals( "false" ) )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : ShowUV" );
                    ShowUVStr = "true";
                }

                string ShowSolarStr = Sup.GetUtilsIniValue( "Website", "ShowSolar", "true" ).ToLowerInvariant();
                if ( !ShowSolarStr.Equals( "true" ) && !ShowSolarStr.Equals( "false" ) )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : ShowSolar" );
                    ShowSolarStr = "true";
                }

                string rainUseSectionColours = Sup.GetUtilsIniValue( "Website", "SteelseriesRainUseSectionColours", "false" ).ToLowerInvariant();
                if ( !rainUseSectionColours.Equals( "true" ) && !rainUseSectionColours.Equals( "false" ) )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesRainUseSectionColours" );
                    rainUseSectionColours = "false";
                }

                string rainUseGradientColours = Sup.GetUtilsIniValue( "Website", "SteelseriesRainUseGradientColours", "true" ).ToLowerInvariant();
                if ( !rainUseGradientColours.Equals( "true" ) && !rainUseGradientColours.Equals( "false" ) )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesRainUseGradientColours" );
                    rainUseGradientColours = "true";
                }

                string[] Colours = { "RED", "GREEN", "BLUE", "ORANGE", "YELLOW", "CYAN", "MAGENTA", "WHITE", "GRAY", "BLACK", "RAITH", "GREEN_LCD", "JUG_GREEN" };
                string[] LcdColours = { "BEIGE", "BLUE", "ORANGE", "RED", "YELLOW", "WHITE", "GRAY", "BLACK", "GREEN", "BLUE2", "BLUE_BLACK", "BLUE_DARKBLUE", "BLUE_GRAY", "STANDARD", "STANDARD_GREEN", "BLUE_BLUE", "RED_DARKRED", "DARKBLUE", "LILA", "BLACKRED", "DARKGREEN", "AMBER", "LIGHTBLUE", "SECTIONS" };

                string[] ForegroundTypes = { "TYPE1", "TYPE2", "TYPE3", "TYPE4", "TYPE5" };
                string[] Backgrounds = { "DARK_GRAY", "SATIN_GRAY", "LIGHT_GRAY", "WHITE", "BLACK", "BEIGE", "BROWN", "RED", "GREEN", "BLUE", "ANTHRACITE", "MUD", "PUNCHED_SHEET", "CARBON", "STAINLESS", "BRUSHED_METAL", "BRUSHED_STAINLESS", "TURNED" };
                string[] PointerTypes = { "TYPE1", "TYPE2", "TYPE3", "TYPE4", "TYPE5", "TYPE6", "TYPE7", "TYPE8", "TYPE9", "TYPE10", "TYPE11", "TYPE12", "TYPE13", "TYPE14", "TYPE15", "TYPE16" };
                string[] FrameDesigns = { "BLACK_METAL", "METAL", "SHINY_METAL", "BRASS", "STEEL", "CHROME", "GOLD", "ANTHRACITE", "TILTED_GRAY", "TILTED_BLACK", "GLOSSY_METAL" };
                string[] KnobStyles = { "BLACK", "BRASS", "SILVER" };
                string[] KnobsTypes = { "STANDARD_KNOB", "METAL_KNOB" };

                bool found = false;

                string SteelseriesDirAvgPointertype = Sup.GetUtilsIniValue( "Website", "SteelseriesDirAvgPointertype", "TYPE3" ).ToUpperInvariant();
                foreach ( string type in PointerTypes )
                {
                    if ( SteelseriesDirAvgPointertype.Equals( type ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesDirAvgPointertype" );
                    SteelseriesDirAvgPointertype = "TYPE3";
                }

                found = false;
                string steelseriesdirAvgPointerColour = Sup.GetUtilsIniValue( "Website", "SteelseriesDirAvgPointerColour", "BLUE" ).ToUpperInvariant();
                foreach ( string thisColour in Colours )
                {
                    if ( steelseriesdirAvgPointerColour.Equals( thisColour ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesDirAvgPointerColour" );
                    steelseriesdirAvgPointerColour = "BLUE";
                }

                found = false;
                string SteelseriesFramedesign = Sup.GetUtilsIniValue( "Website", "SteelseriesFramedesign", "SHINY_METAL" ).ToUpperInvariant();
                foreach ( string design in FrameDesigns )
                {
                    if ( SteelseriesFramedesign.Equals( design ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesFramedesign" );
                    SteelseriesFramedesign = "SHINY_METAL";
                }

                found = false;
                string SteelseriesBackgroundColor = Sup.GetUtilsIniValue( "Website", "SteelseriesBackgroundColor", "BROWN" ).ToUpperInvariant();
                foreach ( string Background in Backgrounds )
                {
                    if ( SteelseriesBackgroundColor.Equals( Background ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesBackgroundColor" );
                    SteelseriesBackgroundColor = "BROWN";
                }

                found = false;
                string SteelseriesPointerColour = Sup.GetUtilsIniValue( "Website", "SteelseriesPointerColour", "RED" ).ToUpperInvariant();
                foreach ( string thisColour in Colours )
                {
                    if ( SteelseriesPointerColour.Equals( thisColour ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesPointerColour" );
                    SteelseriesPointerColour = "RED";
                }

                found = false;
                string SteelseriesPointerType = Sup.GetUtilsIniValue( "Website", "SteelseriesPointerType", "TYPE3" ).ToUpperInvariant();
                foreach ( string type in PointerTypes )
                {
                    if ( SteelseriesPointerType.Equals( type ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesPointerType" );
                    SteelseriesPointerType = "TYPE3";
                }

                found = false;
                string SteelseriesLcdColour = Sup.GetUtilsIniValue( "Website", "SteelseriesLcdColour", "ORANGE" ).ToUpperInvariant();
                foreach ( string lcdColour in LcdColours )
                {
                    if ( SteelseriesLcdColour.Equals( lcdColour ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesLcdColour" );
                    SteelseriesLcdColour = "ORANGE";
                }

                found = false;
                string SteelseriesForegroundType = Sup.GetUtilsIniValue( "Website", "SteelseriesForegroundType", "TYPE1" ).ToUpperInvariant();
                foreach ( string foreground in ForegroundTypes )
                {
                    if ( SteelseriesForegroundType.Equals( foreground ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesForegroundType" );
                    SteelseriesForegroundType = "TYPE1";
                }

                found = false;
                string SteelseriesKnobType = Sup.GetUtilsIniValue( "Website", "SteelseriesKnobType", "STANDARD_KNOB" ).ToUpperInvariant();
                foreach ( string thisType in KnobsTypes )
                {
                    if ( SteelseriesKnobType.Equals( thisType ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesKnobType" );
                    SteelseriesKnobType = "STANDARD_KNOB";
                }

                found = false;
                string SteelseriesKnobStyle = Sup.GetUtilsIniValue( "Website", "SteelseriesKnobStyle", "SILVER" ).ToUpperInvariant();
                foreach ( string thisStyle in KnobStyles )
                {
                    if ( SteelseriesKnobStyle.Equals( thisStyle ) ) { found = true; break; }
                }
                if ( !found )
                {
                    Sup.LogTraceErrorMessage( $"cumulusutils.js generation: Parameter value did not pass : SteelseriesKnobStyle" );
                    SteelseriesKnobStyle = "SILVER";
                }

                CUlibFile.Append(
                  "function DoGaugeSettings() {" +
                  "console.log('DoGaugeSettings');" +
                 // See settings:
                 // http://www.boock.ch/meteo/gauges_SteelSeries/demoRadial.html
                 //
                 $"  gauges.config.realTimeURL = '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}realtimegauges.txt';" +
                 $"  gauges.config.realtimeInterval = {CUtils.UtilsRealTimeInterval};" +
                 $"  gauges.config.showIndoorTempHum = {ShowIndoorTempHum};" +
                 $"  gauges.config.showUvGauge = {ShowUVStr};" +
                 $"  gauges.config.showSolarGauge = {ShowSolarStr};" +
                 $"  gauges.config.ThresholdTempVisible = {Sup.GetUtilsIniValue( "Website", "ThresholdTempVisible", "false" ).ToLowerInvariant()};" +
                 $"  gauges.config.ThresholdTempValue = {Sup.GetUtilsIniValue( "Website", "ThresholdTempValue", "30" )};" +
                 $"  gauges.config.ThresholdHumidityVisible = {Sup.GetUtilsIniValue( "Website", "ThresholdHumidityVisible", "false" ).ToLowerInvariant()};" +
                 $"  gauges.config.ThresholdHumidityValue = {Sup.GetUtilsIniValue( "Website", "ThresholdHumidityValue", "80" )};" +
                 $"  gauges.config.ThresholdWindVisible = {Sup.GetUtilsIniValue( "Website", "ThresholdWindVisible", "false" ).ToLowerInvariant()};" +
                 $"  gauges.config.ThresholdWindValue = {Sup.GetUtilsIniValue( "Website", "ThresholdWindValue", "50" )};" +
                 $"  gauges.config.ThresholdRainVisible = {Sup.GetUtilsIniValue( "Website", "ThresholdRainVisible", "false" ).ToLowerInvariant()};" +
                 $"  gauges.config.ThresholdRainValue = {Sup.GetUtilsIniValue( "Website", "ThresholdRainValue", "5" )};" +
                 $"  gauges.config.ThresholdRRateVisible = {Sup.GetUtilsIniValue( "Website", "ThresholdRRateVisible", "false" ).ToLowerInvariant()};" +
                 $"  gauges.config.ThresholdRRateValue = {Sup.GetUtilsIniValue( "Website", "ThresholdRRateValue", "10" )};" +
                 $"  gauges.config.ThresholdUVVisible = {Sup.GetUtilsIniValue( "Website", "ThresholdUVVisible", "false" ).ToLowerInvariant()};" +
                 $"  gauges.config.ThresholdUVValue = {Sup.GetUtilsIniValue( "Website", "ThresholdUVValue", "10" )};" +
                 $"  gauges.setLang(LANG.{Sup.Language.ToUpperInvariant()});" +
                 $"  gauges.gaugeGlobals.dirAvgPointer = steelseries.PointerType.{SteelseriesDirAvgPointertype};" +
                 $"  gauges.gaugeGlobals.dirAvgPointerColour = steelseries.ColorDef.{steelseriesdirAvgPointerColour};" +

                 // Only one of these colour options should be true,  Set both to false to use the pointer colour
                 $"  gauges.gaugeGlobals.rainUseSectionColours =  {rainUseSectionColours};" +
                 $"  gauges.gaugeGlobals.rainUseGradientColours = {rainUseGradientColours};" );

                CUlibFile.Append( $"  gauges.SetFrameAppearance(steelseries.FrameDesign.{SteelseriesFramedesign});" );
                CUlibFile.Append( $"  gauges.SetBackground(steelseries.BackgroundColor.{SteelseriesBackgroundColor});" );
                CUlibFile.Append( $"  gauges.SetPointerColour(steelseries.ColorDef.{SteelseriesPointerColour});" );
                CUlibFile.Append( $"  gauges.SetPointerType(steelseries.PointerType.{SteelseriesPointerType});" );
                CUlibFile.Append( $"  gauges.SetLcdColour(steelseries.LcdColor.{SteelseriesLcdColour});" );
                CUlibFile.Append( $"  gauges.SetForegroundType(steelseries.ForegroundType.{SteelseriesForegroundType});" );
                CUlibFile.Append( $"  gauges.SetKnobType(steelseries.KnobType.{SteelseriesKnobType});" );
                CUlibFile.Append( $"  gauges.SetKnobStyle(steelseries.KnobStyle.{SteelseriesKnobStyle});" );
                CUlibFile.Append( $"  gauges.init(false);" ); // The false is to indicate the CumulusMX dashboard mode
                CUlibFile.Append( '}' );

                // See the Menu choice Print
                CUlibFile.Append( "function PrintScreen( DIVname ){" +
                    "  if (DIVname == 'Dashboard'){" +
                    "    if ($('#ExtraAndCustom').is(':visible')) DIVname = 'ExtraAndCustom';" +
                    "    else return;" +
                    "  }" +
                    "  var prtContent = document.getElementById( DIVname );" +
                    "  var WinPrint = window.open('', '', 'left=0,top=0,width=800,height=900,toolbar=0,scrollbars=0,status=0');" +
                    "" +
                    "  WinPrint.document.write('<link rel=\"stylesheet\" type=\"text/css\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css\">');" +
                    "  WinPrint.document.write(prtContent.innerHTML);" +
                    "  WinPrint.document.close();" +
                    "  WinPrint.setTimeout(function(){" +
                    "    WinPrint.focus();" +
                    "    WinPrint.print();" +
                    "    WinPrint.close(); " +
                    "  }, 1000);" +
                    "}" );

#if !RELEASE
                of.WriteLine( CUlibFile );
#else
                of.WriteLine( CuSupport.StringRemoveWhiteSpace( CUlibFile.ToString(), " " ) );
#endif
            }

            return;
        }

        #region GenerateStatisticsCode

        private string GenerateStatisticsCode( string StatisticsType, bool Event )
        {
            StringBuilder Buf = new StringBuilder();

            Sup.LogTraceInfoMessage( $"GenerateStatisticsCode: StatisticsType is '{StatisticsType}'; Event is '{Event}'" );

            if ( StatisticsType.Equals( "Google" ) )
            {
                if ( Event )
                {
                    // Now Event is the parameter, it might be usefull to have a string giving the variable ("ReportName") ???
                    Buf.Append( $" gtag('event', 'LoadReport', {{'event_category' : ReportName.replace(/\\.[^/.]+$/, '') }});" );
                }
            }
            else if ( StatisticsType.Equals( "Matomo" ) )
            {
                if ( Event )
                {
                    Sup.LogTraceWarningMessage( $"GenerateStatisticsCode: No Matomo events implemented yet" );
                }
            }
            else
            {
                Sup.LogTraceErrorMessage( $"GenerateStatisticsCode: StatisticsType '{StatisticsType}' is unknown, nothing generated" );
            }

            return Buf.ToString();
        }

        #endregion


    }
}
