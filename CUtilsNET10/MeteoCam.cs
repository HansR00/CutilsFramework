/*
 * Meteocam - Part sb CumulusUtils
 *
 */

using System.IO;
using System.Text;

namespace CumulusUtils
{
    class MeteoCam( CuSupport s )
    {
        readonly CuSupport Sup = s;
        readonly string WantToSeeLines = s.GetUtilsIniValue( "MeteoCam", "WantToSeeLines", "WTPHR" ).ToUpper();

        #region Constructor
        public void GenerateMeteoCam()
        {
            if ( !CUtils.HasMeteoCamMenu )
            {
                return; // Don't generate, ignore everything, just get back.
            }

            Sup.LogDebugMessage( "MeteoCam: Starting" );

            string MeteoCamType = Sup.GetUtilsIniValue( "MeteoCam", "CamType", "Manual" ).ToLower();

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.MeteoCamOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                switch ( MeteoCamType )
                {
                    case "manual":
                        of.WriteLine( MeteoCamManual() );
                        break;

                    case "ecowitthp10":
                        of.WriteLine( MeteoCamEcowittHP10() );
                        break;

                    default:
                        Sup.LogTraceInfoMessage( $"MeteoCam: CamType unknown: {MeteoCamType}" );
                        Sup.LogTraceInfoMessage( $"MeteoCam: Nothing to do." );
                        break;
                }
            }

            Sup.LogTraceInfoMessage( "MeteoCam: End" );

            return;
        }
        #endregion

        #region Manual
        private string MeteoCamManual()
        {
            StringBuilder sb = new StringBuilder();
            // 

            Sup.LogTraceInfoMessage( $"MeteoCam CamType : Manual" );

            sb.AppendLine( "<script>" );
            sb.AppendLine( "  console.log('Meteocam starting...');" );
            sb.AppendLine( "  $( function() {" );
            sb.AppendLine( $"    $.get( '{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamDir", "." )}/', function( data ) {{" );
            sb.AppendLine( "      thing = data;" );
            sb.AppendLine( $"      searchFor = /.{Sup.GetUtilsIniValue( "MeteoCam", "TimelapseExtension", "mp4" )}</g;" );
            sb.AppendLine( "      a = 0; b = 0;" );
            sb.AppendLine( "      var str = '';" );
            sb.AppendLine( "      while ( ( doextensions = searchFor.exec( thing ) ) != null ) {" );
            sb.AppendLine( "      str = '';" );
            sb.AppendLine( "      a = doextensions.index;" );
            sb.AppendLine( "      while(thing[a]!='>'){a--} a++; while(thing[a]!='<' ) {str=str+thing[a];a++;}" );
            sb.AppendLine( "      $('#timelapses').append('<option value=\"' + str + '\" select>' + str + '</option>' );" );
            sb.AppendLine( "    }" );
            sb.AppendLine( "  });" );
            sb.AppendLine( "  $('#timelapses').change(function() {" );
            sb.AppendLine( $"    $('#videoSource').attr('src','{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamDir", "." )}/' + $( '#timelapses' ).val() );" );
            sb.AppendLine( "    video = $('#videoPlayer')[0];" );
            sb.AppendLine( "    video.load();" );
            sb.AppendLine( "    video.play();" );
            sb.AppendLine( "  });" );
            sb.AppendLine( "  RadioViewerChange();" );
            sb.AppendLine( "  UpdateWebCam();" );
            sb.AppendLine( "});" ); // Document load function

            sb.AppendLine( "function RadioViewerChange() {" );
            sb.AppendLine( "  if ($('input[name=\"viewer\"]:checked').val() == 'Image') {" );
            sb.AppendLine( "    $('#videoPlayer').hide();" );
            sb.AppendLine( "    $('.text-block').show();" );
            sb.AppendLine( "    $('#imageViewer').show();" );

            sb.AppendLine( "    $('#videoPlayer')[0].pause();" );
            sb.AppendLine( "    DoWebCam = true; " );
            sb.AppendLine( "  } else {" );
            sb.AppendLine( "    $('#videoPlayer').show();" );
            sb.AppendLine( "    $('.text-block').hide();" );
            sb.AppendLine( "    $('#imageViewer').hide();" );
            sb.AppendLine( "    $('#timelapses').change();" );
            sb.AppendLine( "    DoWebCam = false;" );
            sb.AppendLine( "  }" );
            sb.AppendLine( "}" );

            sb.AppendLine( "function UpdateWebCam(newAngle, beaufort, temp, baro, humidity, rain) {" );
            sb.AppendLine( $"  $('#imageViewer').attr('src', " +
                $"'{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamDir", "." )}/{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamName", "meteocam.jpg" )}' + '?v=' + Math.random() );" );
            if ( WantToSeeLines.Contains( 'W' ) ) sb.AppendLine( "  $('#TT1').html(beaufort);" );
            if ( WantToSeeLines.Contains( 'T' ) ) sb.AppendLine( "  $('#TT2').html(temp);" );
            if ( WantToSeeLines.Contains( 'P' ) ) sb.AppendLine( "  $('#TT3').html(baro);" );
            if ( WantToSeeLines.Contains( 'H' ) ) sb.AppendLine( "  $('#TT4').html(humidity);" );
            if ( WantToSeeLines.Contains( 'R' ) ) sb.AppendLine( "  $('#TT5').html(rain);" );
            sb.AppendLine( "}" );
            sb.AppendLine( "</script>" );

            sb.AppendLine( "<style>" );
            sb.AppendLine( ".CURadioButton {width: 20px !important;height: 20px !important;position: relative !important;vertical-align: -2px !important;margin-right: 3px !important;}" );
            sb.AppendLine( ".CURadioLabel {height: 15px !important;text-align: left !important;font-size: 15px !important;vertical-align: 0px !important;}" );

            sb.AppendLine( "#report {" );
            sb.AppendLine( "  text-align: center;" );
            sb.AppendLine( "  font-family: arial;" );
            sb.AppendLine( "  border-radius: 15px;" );
            sb.AppendLine( "  border-spacing: 0;" );
            sb.AppendLine( "  border: 1px solid #b0b0b0;" );
            sb.AppendLine( "}" );
            sb.AppendLine( ".container {" );
            sb.AppendLine( "  position: relative;" );
            sb.AppendLine( "  text-align: left;" );
            sb.AppendLine( "  color: white;" );
            sb.AppendLine( "}" );
            sb.AppendLine( ".text-block {" );
            sb.AppendLine( "  background: transparent;" );
            sb.AppendLine( "  position: absolute;" );
            sb.AppendLine( $"  font-size: {Sup.GetUtilsIniValue( "MeteoCam", "FontSize", "24" )}px;" );
            sb.AppendLine( $"  font-weight: {Sup.GetUtilsIniValue( "MeteoCam", "FontWeight", "bold" )};" );
            sb.AppendLine( $"  bottom: {Sup.GetUtilsIniValue( "MeteoCam", "BottomOffset", "150" )}px;" );
            sb.AppendLine( $"  {Sup.GetUtilsIniValue( "MeteoCam", "BlockLeftOrRight", "left" )}: {Sup.GetUtilsIniValue( "MeteoCam", "BorderOffset", "50" )}px;" );
            sb.AppendLine( $"  text-align:{Sup.GetUtilsIniValue( "MeteoCam", "TextAlign", "left" )};" );
            sb.AppendLine( $"  color: {Sup.GetUtilsIniValue( "MeteoCam", "TextColor", "White" )};" );
            sb.AppendLine( "}" );

            sb.AppendLine( "</style>" );

            sb.AppendLine( "<div id='report'>" );
            sb.AppendLine( "<br />" );
            sb.AppendLine( "  <input type='radio' class='CURadioButton' id='nowViewer' name='viewer' value='Image' onchange='RadioViewerChange();' checked>" +
                $"<label for='nowViewer' class='CURadioLabel'>{Sup.GetCUstringValue( "Website", "MeteoLabel", "Meteocam", false )}</label>" );
            sb.AppendLine( "  <input type='radio' class='CURadioButton' id='timelapseViewer' name='viewer' value='Timelapse' onchange='RadioViewerChange();'>" +
                $"<label for='timelapseViewer' class='CURadioLabel'>{Sup.GetCUstringValue( "Website", "TimeLapseLabel", "TimeLapse", false )}</label>&nbsp;&nbsp;" );

            sb.AppendLine( "  <select id='timelapses'></select><br />" );
            sb.AppendLine( "  <br />" );

            sb.AppendLine( "  <div class='container'>" );
            sb.AppendLine( "  <image id='imageViewer' style='width:100%; height:75vh;'>" );

            sb.AppendLine( "  <div class='text-block'>" );
            if ( WantToSeeLines.Contains( 'W' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "WindInBf", "Wind", true )}: <span id='TT1'></span> Bf<br/>" );
            if ( WantToSeeLines.Contains( 'T' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Temperature", "Temperature", true )}: <span id='TT2'></span> {Sup.StationTemp.Text()}<br/>" );
            if ( WantToSeeLines.Contains( 'P' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Pressure", "Pressure", true )}: <span id='TT3'></span> {Sup.StationPressure.Text()}<br/>" );
            if ( WantToSeeLines.Contains( 'H' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Humidity", "Humidity", true )}: <span id='TT4'></span> %<br/>" );
            if ( WantToSeeLines.Contains( 'R' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Rain", "Rain", true )}: <span id ='TT5'></span> {Sup.StationRain.Text()}" );
            sb.AppendLine( "  </div>" );

            sb.AppendLine( "  <video id='videoPlayer' width='100%' height='100%' autoplay muted controls>" );
            sb.AppendLine( $"    <source id='videoSource' src='' type='video/{Sup.GetUtilsIniValue( "MeteoCam", "TimelapseExtension", "mp4" )}'>" );
            sb.AppendLine( "    Your browser does not support the video tag." );
            sb.AppendLine( "  </video>" );
            sb.AppendLine( "  <br /><br />" );
            sb.AppendLine( "  </div>" ); // container
            sb.AppendLine( "</div>" ); // report

#if !RELEASE
            return sb.ToString();
#else
            return CuSupport.StringRemoveWhiteSpace( sb.ToString(), " " );
#endif

        }
        #endregion

        #region HP10
        private string MeteoCamEcowittHP10()
        {
            StringBuilder sb = new StringBuilder();

            Sup.LogTraceInfoMessage( $"MeteoCam CamType : EcowittHP10" );

            // I: First generate the RealTime meteocam file
            //    Although the file is named Realtime, it is best to have this processed at the Interval frequency because the HP10 has an
            //    interval of 5, 10, 15, 20 or 25 minutes and it is the user who sets that interval.
            File.WriteAllText( $"{Sup.PathUtils}{Sup.MeteocamRealtimeFilename}", "<#EcowittCameraUrl>" );

            // II: write the module
            sb.AppendLine( "<script>" );
            sb.AppendLine( "console.log('Meteocam starting...');" );
            sb.AppendLine( "var prevMeteocamURL;" );
            sb.AppendLine( "$(function () {" );
            sb.AppendLine( "  UpdateWebCam();" );
            sb.AppendLine( "});" );
            sb.AppendLine( "function loadWebcamURL() {" );
            sb.AppendLine( "  $.ajax({" );
            sb.AppendLine( "    url: 'meteocamrealtime.txt'," );
            sb.AppendLine( "    cache:false," );
            sb.AppendLine( "    timeout: 2000," );
            sb.AppendLine( "    headers: { 'Access-Control-Allow-Origin': '*' }," );
            sb.AppendLine( "    crossDomain: true" );
            sb.AppendLine( "  })" );
            sb.AppendLine( "  .done( function (response, responseStatus) {" +
                "if (response !== prevMeteocamURL){" +
#if !RELEASE
                "  console.log('Setting new image in the viewer...');" +
#endif
                "  $('#imageViewer').attr('src', response);" +
                "  prevMeteocamURL = response;" +
                "} } )" );
            sb.AppendLine( "  .fail( function (xhr, textStatus, errorThrown) {console.log('webcamrealtime.txt ' + textStatus + ' : ' + errorThrown) })" );
            sb.AppendLine( "}" );
            sb.AppendLine( "function UpdateWebCam(newAngle, beaufort, temp, baro, humidity, rain) {" );
            sb.AppendLine( $"  loadWebcamURL();" );
            if ( WantToSeeLines.Contains( 'W' ) ) sb.AppendLine( "  $('#TT1').html(beaufort);" );
            if ( WantToSeeLines.Contains( 'T' ) ) sb.AppendLine( "  $('#TT2').html(temp);" );
            if ( WantToSeeLines.Contains( 'P' ) ) sb.AppendLine( "  $('#TT3').html(baro);" );
            if ( WantToSeeLines.Contains( 'H' ) ) sb.AppendLine( "  $('#TT4').html(humidity);" );
            if ( WantToSeeLines.Contains( 'R' ) ) sb.AppendLine( "  $('#TT5').html(rain);" );
            sb.AppendLine( "}" );
            sb.AppendLine( "</script>" );
            sb.AppendLine( "" );
            sb.AppendLine( "<style>" );
            sb.AppendLine( "#report {" );
            sb.AppendLine( "  text-align: center;" );
            sb.AppendLine( "  font-family: arial;" );
            sb.AppendLine( "  border-radius: 15px;" );
            sb.AppendLine( "  border-spacing: 0;" );
            sb.AppendLine( "  border: 1px solid #b0b0b0;" );
            sb.AppendLine( "}" );
            sb.AppendLine( ".container {" );
            sb.AppendLine( "  position: relative;" );
            sb.AppendLine( "  text-align: left;" );
            sb.AppendLine( "  color: white;" );
            sb.AppendLine( "}" );
            sb.AppendLine( ".text-block {" );
            sb.AppendLine( "  background: transparent;" );
            sb.AppendLine( "  position: absolute;" );
            sb.AppendLine( $"  font-size: {Sup.GetUtilsIniValue( "MeteoCam", "FontSize", "24" )}px;" );
            sb.AppendLine( $"  font-weigt: {Sup.GetUtilsIniValue( "MeteoCam", "FontWeight", "bold" )};" );
            sb.AppendLine( $"  bottom: {Sup.GetUtilsIniValue( "MeteoCam", "BottomOffset", "150" )}px;" );
            sb.AppendLine( $"  {Sup.GetUtilsIniValue( "MeteoCam", "BlockLeftOrRight", "left" )}: {Sup.GetUtilsIniValue( "MeteoCam", "BorderOffset", "50" )}px;" );
            sb.AppendLine( $"  text-align:{Sup.GetUtilsIniValue( "MeteoCam", "TextAlign", "left" )};" );
            sb.AppendLine( $"  color: {Sup.GetUtilsIniValue( "MeteoCam", "TextColor", "White" )};" );
            sb.AppendLine( "}" );
            sb.AppendLine( "</style>" );
            sb.AppendLine( "<div id='report'>" );
            sb.AppendLine( "<div class='container'>" );
            sb.AppendLine( "  <image id='imageViewer' style='width:100%; height:75vh;'>" );
            sb.AppendLine( "  <div class='text-block'>" );
            if ( WantToSeeLines.Contains( 'W' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "WindInBf", "Wind", true )}: <span id='TT1'></span> Bf<br/>" );
            if ( WantToSeeLines.Contains( 'T' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Temperature", "Temperature", true )}: <span id='TT2'></span> {Sup.StationTemp.Text()}<br/>" );
            if ( WantToSeeLines.Contains( 'P' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Pressure", "Pressure", true )}: <span id='TT3'></span> {Sup.StationPressure.Text()}<br/>" );
            if ( WantToSeeLines.Contains( 'H' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Humidity", "Humidity", true )}: <span id='TT4'></span> %<br/>" );
            if ( WantToSeeLines.Contains( 'R' ) ) sb.AppendLine( $"{Sup.GetCUstringValue( "MeteoCam", "Rain", "Rain", true )}: <span id ='TT5'></span> {Sup.StationRain.Text()}" );
            sb.AppendLine( "  </div>" );

            sb.AppendLine( "  </div>" ); // container
            sb.AppendLine( "</div>" ); // report

#if !RELEASE
            return sb.ToString();
#else
            return CuSupport.StringRemoveWhiteSpace( sb.ToString(), " " );
#endif

        }
        #endregion

    }
}
