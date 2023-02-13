/*
 * Webcam - Part of CumulusUtils
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
using System.IO;
using System.Text;

namespace CumulusUtils
{
    class MeteoCam
    {
        readonly CuSupport Sup;

        public MeteoCam( CuSupport s )
        {
            Sup = s;
        }

        public void GenerateMeteoCam()
        {
            Sup.LogDebugMessage( "MeteoCam: start" );

            if ( !CUtils.HasMeteoCamMenu )
                return; // Don't generate, ignore everything, just get back.

            //string MeteoCamName = Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamName", "" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.MeteoCamOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( "<script>" );
                of.WriteLine( "  console.log('Meteocam starting...')" );
                of.WriteLine( "  $( function() {" );
                of.WriteLine( $"    $.get( '{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamDir", "." )}/', function( data ) {{" );
                of.WriteLine( "      thing = data;" );
                of.WriteLine( $"      searchFor = /.{Sup.GetUtilsIniValue( "MeteoCam", "TimelapseExtension", "mp4" )}</g;" );
                of.WriteLine( "      a = 0; b = 0;" );
                of.WriteLine( "      var str = '';" );
                of.WriteLine( "      while ( ( doextensions = searchFor.exec( thing ) ) != null ) {" );
                of.WriteLine( "      str = '';" );
                of.WriteLine( "      a = doextensions.index;" );
                of.WriteLine( "      while(thing[a]!='>'){a--} a++; while(thing[a]!='<' ) {str=str+thing[a];a++;}" );
                of.WriteLine( "      $('#timelapses').append('<option value=\"' + str + '\" select>' + str + '</option>' );" );
                of.WriteLine( "    }" );
                of.WriteLine( "  });" );
                of.WriteLine( "  $('#timelapses').change(function() {" );
                of.WriteLine( $"    $('#videoSource').attr('src','{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamDir", "." )}/' + $( '#timelapses' ).val() );" );
                of.WriteLine( "    video = $('#videoPlayer')[0];" );
                of.WriteLine( "    video.load();" );
                of.WriteLine( "    video.play();" );
                of.WriteLine( "  });" );
                of.WriteLine( "  RadioViewerChange();" );
                of.WriteLine( "  UpdateWebCam();" );
                of.WriteLine( "});" ); // Document load function

                of.WriteLine( "function RadioViewerChange() {" );
                of.WriteLine( "  if ($('input[name=\"viewer\"]:checked').val() == 'Image') {" );
                of.WriteLine( "    $('#videoPlayer').hide();" );
                of.WriteLine( "    $('#imageViewer').show();" );

                of.WriteLine( "    $('#videoPlayer')[0].pause();" );
                of.WriteLine( "    DoWebCam = true;" );
                of.WriteLine( "  } else {" );
                of.WriteLine( "    $('#videoPlayer').show();" );
                of.WriteLine( "    $('#imageViewer').hide();" );
                of.WriteLine( "    $('#timelapses').change();" );
                of.WriteLine( "    DoWebCam = false;" );
                of.WriteLine( "  }" );
                of.WriteLine( "}" );

                of.WriteLine( "function UpdateWebCam() {" );
                of.WriteLine( $"  $('#imageViewer').attr('src', " +
                    $"'{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamDir", "." )}/{Sup.GetUtilsIniValue( "MeteoCam", "MeteoCamName", "meteocam.jpg" )}' + '?v=' + Math.random() );" );
                of.WriteLine( "}" );
                of.WriteLine( "</script>" );

                of.WriteLine( "<style>" );
                of.WriteLine( ".CURadioButton {width: 20px !important;height: 20px !important;position: relative !important;vertical-align: -2px !important;margin-right: 3px !important;}" );
                of.WriteLine( ".CURadioLabel {height: 15px !important;text-align: left !important;font-size: 15px !important;vertical-align: 0px !important;}" );

                of.WriteLine( "#report {" );
                of.WriteLine( "  text-align: center;" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );
                of.WriteLine( "</style>" );

                of.WriteLine( "<div id='report'>" );
                of.WriteLine( "<br />" );
                of.WriteLine( "  <input type='radio' class='CURadioButton' id='nowViewer' name='viewer' value='Image' onchange='RadioViewerChange();' checked>" +
                    $"<label for='nowViewer' class='CURadioLabel'>{Sup.GetCUstringValue( "Website", "MeteoLabel", "Meteocam", false )}</label>" );
                of.WriteLine( "  <input type='radio' class='CURadioButton' id='timelapseViewer' name='viewer' value='Timelapse' onchange='RadioViewerChange();'>" +
                    $"<label for='timelapseViewer' class='CURadioLabel'>{Sup.GetCUstringValue( "Website", "TimeLapseLabel", "TimeLapse", false )}</label>&nbsp;&nbsp;" );

                of.WriteLine( "  <select id='timelapses'></select><br />" );
                of.WriteLine( "  <br />" );

                of.WriteLine( "  <image id='imageViewer' src='' width='100%' height='100%' frameborder='0' style='border: 0;'>" );

                of.WriteLine( "  <video id='videoPlayer' width='100%' height='100%' autoplay muted controls>" );
                of.WriteLine( $"    <source id='videoSource' src='' type='video/{Sup.GetUtilsIniValue( "MeteoCam", "TimelapseExtension", "mp4" )}'>" );
                of.WriteLine( "    Your browser does not support the video tag." );
                of.WriteLine( "  </video>" );
                of.WriteLine( "  <br /><br />" );
                of.WriteLine( "</div>" );
            }

            Sup.LogDebugMessage( "MeteoCam: End" );

            return;
        }
    }
}
