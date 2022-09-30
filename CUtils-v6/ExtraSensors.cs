/*
 * DayRecords - Part of CumulusUtils
 *
 * © Copyright 2019 - 2021 Hans Rottier <hans.rottier@gmail.com>
 *
 * When the code is made public domain the licence will be changed to the GNU 
 * General Public License as published by the Free Software Foundation;
 * Until then, the code of CumulusUtils is not public domain and only the executable is 
 * distributed under the  Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * As a consequence, this code should not be in your posession unless with explicit permission by Hans Rottier
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI
 *              Initial release: pwsFWI             (version 1.0)
 *                               Website Generator  (version 3.0)
 *                               ChartsCompiler     (version 5.0)
 *              
 * Environment: Raspberry 3B+
 *              Raspbian / Linux 
 *              C# / Visual Studio
 *              
 * Design / Structure:
 *              1) create the  list of actual sensors being logged based on 
 *                 Cumulus.ini - [section Station], param: LogExtraSensors=1
 *                 strings.ini - [section ExtraTempCaptions] - all entry value != "Sensor i", i is 10  max
 *                             - [section ExtraHumCaptions]  - all entry value != "Sensor i", i is 10  max
 *                             - [section ExtraDPCaptions] - all entry value != "Sensor i", i is 10  max
 *                             - [section SoilTempCaptions] - all entry value != "Sensor i", i is 16  max
 *                             - [section SoilMoistureCaptions] - all entry value != "Sensor i", i is 16  max
 *                             - [section LeafTempCaptions] - all entry value != "Sensor i", i is 8  max
 *                             - [section LeafWetnessCaptions] - all entry value != "Sensor i", i is 8  max
 *                             - [section AirQualityCaptions] - all entry value != "Sensor i", i is 4 max (must have their equal for the avg entries)
 *                             - [section UserTempCaptions] - all entry value != "Sensor i", i is 8  max
 *                             - [section CO2Captions] - If (has a value in the  logfile on the current CO2 value) THEN is sensor present
 *                             - Lightning sensor - Is present IF (distance to last strike && Time of last strike differ from default values)
 *                             
 *              2) Each sensorsection gets its own RECENT graph, written in CDL. So two extra temp sensors are displayed in one chart. 
 *                 The cutilscharts.def file is written with the extrasenso charts at the end. Optimisation: detect changes, if none then skip this step.
 *                 The variables in the charts have the names of 1) the extra sensor names as in the strings.ini and are of type RECENT
 *                                                               2) (POSSIBLY!! : any other name as long as it is from the same type so:
 *                                                                    an ExtraTemp chart only can have (any) RECENT --- not sure about this added variable )
 *                                                               3) the  user may use these variables in his own charts as well
 *                 
 *              3) The runtime system is generated: extrasensors.txt. The dashboard part is switched on/off through the loading of reportview code in the cumulusutils.js
 *                 Task of the runtime system: 1) The charts display
 *                                             2) The event handling for the clicks on the sensor display in the dashboard
 *                                             3) The actual reading of the realtime values of the sensors in the dashboard.
 *                                             
 *              4) The Dashboard displays: 1) The extra sensors in a tabular form (first column) and the realtime value (second column)
 *                                         2) The realtime change indicator (colour) is the same as in the normal dashboard
 *                                         3) An up/down indicator might be implemented ???
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CumulusUtils
{
    internal class ExtraSensors
    {
        internal enum ExtraSensorType
        {
            Temperature, Humidity, DewPoint, SoilTemp, SoilMoisture, AirQuality, AirQualityAvg, UserTemp, LeafTemp, LeafWetness,
            CO2, CO2avg, CO2pm2p5, CO2pm2p5avg, CO2pm10, CO2pm10avg, CO2temp, CO2hum, External
        }

        internal struct ExtraSensor
        {
            internal string Name;
            internal string PlotvarType;
            internal int PlotvarIndex;
            internal ExtraSensorType Type;
        }

        readonly CultureInfo inv = CultureInfo.InvariantCulture;
        readonly CuSupport Sup;
        ExtraSensor tmp;
        List<ExtraSensor> ExtraSensorList;

        #region Constructor
        public ExtraSensors( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "Extra Sensors constructor: start" );

            // After the next call ExtraSensorList contains all active Extra Sensors
            InitialiseExtraSensorList();

            Sup.LogTraceInfoMessage( "Extra Sensors constructor: stop" );

            return;
        }

        #endregion

        #region DoExtraSensors
        internal void DoExtraSensors()
        {
            Sup.LogDebugMessage( "DoExtraSensors - Start" );

            // I: create the extrasensorsrealtime.txt which has to be processes by CMX and transferred to the webroot.
            // 
            GenerateExtraSensorsRealtime();
            GenerateExtraSensorsCharts();
            GenerateExtraSensorsModule();

            Sup.LogDebugMessage( "DoExtraSensors - Stop" );

            return;
        }

        #endregion DoExtraSensors

        #region GenerateExtraSensorsModule

        internal void GenerateExtraSensorsModule()
        {
            // Some variables required for generaton
            int ExtraTemp, ExtraDP, ExtraHum, SoilTemp, SoilMoisture, AirQuality, AirQualityAvg, UserTemp, LeafTemp, LeafWetness;
            int i;

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ExtraSensorsOutputFilename}", false, Encoding.UTF8 ) )
            {
                #region Javascript

                StringBuilder sb = new StringBuilder();

                Sup.LogDebugMessage( $"GenerateExtraSensorsModule: Generating the Module Javascript code..." );

                // Generate the table on the basis of what is set in the inifile and setup in the constructor. 
                // We don't support live switching as we don't support that for anything.
                // Generate the start of the realtime script
                //

                sb.AppendLine( $"{Sup.GenjQueryIncludestring()}" );

                if ( !CMXutils.DoWebsite && CMXutils.DoLibraryIncludes )
                {
                    sb.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                }

                sb.AppendLine( "<script>" );
                sb.AppendLine( "console.log('Module ExtraSensors ...');" );
                sb.AppendLine( "var ExtraSensorTimer;" );
                sb.AppendLine( "$(function () {" );  // Get the whole thing going
                sb.AppendLine( $"  SetupExtraSensorsTable();" );
                sb.AppendLine( $"  $('#Dashboard').hide();" );
                sb.AppendLine( $"  $('#Gauges').hide();" );
                sb.AppendLine( $"  $('#ExtraSensors').show();" );
                sb.AppendLine( "  loadExtraSensorsRealtime();" );
                sb.AppendLine( "  if (ExtraSensorTimer == null) ExtraSensorTimer = setInterval(loadExtraSensorsRealtime, 60 * 1000);" );
                sb.AppendLine( $"  LoadUtilsReport( '{Sup.ExtraSensorsCharts}', false );" );
                sb.AppendLine( "});" );
                sb.AppendLine( "" );
                sb.AppendLine( "function loadExtraSensorsRealtime() {" );
                sb.AppendLine( "  $.ajax({" );
                sb.AppendLine( $"    url: '{Sup.ExtraSensorsRealtimeFilename}'," );
                sb.AppendLine( "    timeout: 1000," );
                sb.AppendLine( "    cache:false," );
                sb.AppendLine( "    headers: { 'Access-Control-Allow-Origin': '*' }," );
                sb.AppendLine( "    crossDomain: true," );
                sb.AppendLine( "  })" +
                    ".done(function (response, responseStatus) {DoExtraSensorRT(response)})" +
                    ".fail(function(jqXHR, responseStatus){ console.log('ajax call: ' + responseStatus) });" );
                sb.AppendLine( "}" );

                sb.AppendLine( "function DoExtraSensorRT(input) {" );
                sb.AppendLine( "  var ExtraSensorRT = input.split(' ');" );

                // The order is the same as in writing the realtime file so we follow that with i to give the index
                //
                ExtraTemp = 0;
                ExtraDP = 0;
                ExtraHum = 0;
                SoilTemp = 0;
                SoilMoisture = 0;
                AirQuality = 0;
                AirQualityAvg = 0;
                UserTemp = 0;
                LeafTemp = 0;
                LeafWetness = 0;
                i = 0;

                foreach ( ExtraSensor tmp in ExtraSensorList )
                {
                    switch ( (int) tmp.Type )
                    {
                        case (int) ExtraSensorType.Temperature:
                            sb.AppendLine( $" $('#ExtraTemp{++ExtraTemp}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.DewPoint:
                            sb.AppendLine( $" $('#ExtraDP{++ExtraDP}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.Humidity:
                            sb.AppendLine( $" $('#ExtraHum{++ExtraHum}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.SoilTemp:
                            sb.AppendLine( $" $('#SoilTemp{++SoilTemp}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.SoilMoisture:
                            sb.AppendLine( $" $('#SoilMoisture{++SoilMoisture}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.AirQuality:
                            sb.AppendLine( $" $('#Airquality{++AirQuality}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.AirQualityAvg:
                            sb.AppendLine( $" $('#AirQualityAvg{++AirQualityAvg}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2:
                            sb.AppendLine( $" $('#CO2').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2avg:
                            sb.AppendLine( $" $('#CO2-24h').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5:
                            sb.AppendLine( $" $('#CO2-pm2p5').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5avg:
                            sb.AppendLine( $" $('#CO2-pm2p5-24h').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2pm10:
                            sb.AppendLine( $" $('#CO2-pm10').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2pm10avg:
                            sb.AppendLine( $" $('#CO2-pm10-24h').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2temp:
                            sb.AppendLine( $" $('#CO2-temp').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.CO2hum:
                            sb.AppendLine( $" $('#CO2-hum').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.UserTemp:
                            sb.AppendLine( $" $('#UserTemp{++UserTemp}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.LeafTemp:
                            sb.AppendLine( $" $('#LeafTemp{++LeafTemp}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.LeafWetness:
                            sb.AppendLine( $" $('#LeafWetness{++LeafWetness}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        case (int) ExtraSensorType.External:
                            //sb.AppendLine( $" $('#{tmp.Name}').html(ExtraSensorRT[ {i++} ]);" );
                            break;
                        default:
                            Sup.LogTraceErrorMessage( $"GenerateExtraSensorsModule: At impossible Switch default assigning realtime values" );
                            break;
                    }
                }

                sb.AppendLine( "}" ); // End DoExtraSensorRT
                sb.AppendLine( "" );

                // Setup the HTML ExtraSensors table for the Dashboard area

                sb.AppendLine( "function SetupExtraSensorsTable() {" );

                StringBuilder buf = new StringBuilder();

                ExtraTemp = 0;
                ExtraDP = 0;
                ExtraHum = 0;
                SoilTemp = 0;
                SoilMoisture = 0;
                AirQuality = 0;
                AirQualityAvg = 0;
                UserTemp = 0;
                LeafTemp = 0;
                LeafWetness = 0;
                i = 0;
                bool SwitchRowBackground = true;

                string RowColour()
                {
                    SwitchRowBackground = !SwitchRowBackground;
                    return SwitchRowBackground ?
                        $" style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorMenuBackground", "Lightgrey" )};" +
                        $" color:{Sup.GetUtilsIniValue( "Website", "ColorMenuText", "Black" )}'" : "";
                }

                string thisPadding()
                {
                    return "style='padding: 5px 5px 5px 5px;'";
                }

                buf.Append( $"<style>.centerItem {{width: 80%; max-height: 80vh; margin: 6vh auto;overflow-y: auto; }}</style>" );
                buf.Append( $"<div class='centerItem' style='text-align:left;'><table style='width:100%'>" );
                buf.Append( $"<tr " +
                    $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                    $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
                buf.Append( $"<th {thisPadding()}>Sensor name</th><th>Value</th></tr>" );

                foreach ( ExtraSensor tmp in ExtraSensorList )
                {

                    switch ( (int) tmp.Type )
                    {
                        case (int) ExtraSensorType.Temperature:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='ExtraTemp{++ExtraTemp}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.DewPoint:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='ExtraDP{++ExtraDP}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.Humidity:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='ExtraHum{++ExtraHum}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.SoilTemp:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='SoilTemp{++SoilTemp}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.SoilMoisture:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='SoilMoisture{++SoilMoisture}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.AirQuality:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='Airquality{++AirQuality}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.AirQualityAvg:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='AirQualityAvg{++AirQualityAvg}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2avg:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-24h'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-pm2p5'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5avg:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-pm2p5-24h'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2pm10:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-pm10'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2pm10avg:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-pm10-24h'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2temp:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-temp'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.CO2hum:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='CO2-hum'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.UserTemp:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='UserTemp{++UserTemp}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.LeafTemp:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='LeafTemp{++LeafTemp}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.LeafWetness:
                            buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Type}();'><td {thisPadding()}>{tmp.Name}</td><td id='LeafWetness{++LeafWetness}'></td></tr>" );
                            break;
                        case (int) ExtraSensorType.External:
                            //buf.Append( $"<tr {RowColour()} onclick='Do{tmp.Name}();'><td {thisPadding()}>{tmp.Name}</td><td id='{tmp.Name}'></td></tr>" );
                            Sup.LogTraceWarningMessage( $"GenerateExtraSensorsModule: External realtime not implemented." );
                            break;
                        default:
                            Sup.LogTraceErrorMessage( $"GenerateExtraSensorsModule: At impossible Switch default generating the table" );
                            break;
                    }
                }

                buf.Append( "</table></div>" );
                sb.AppendLine( $"  $('#ExtraSensors').html(\"{buf}\");" );
                sb.AppendLine( "}" );
                sb.AppendLine( "</script>" );

#if !RELEASE
                of.WriteLine( sb );
#else
                of.WriteLine( CuSupport.StringRemoveWhiteSpace( sb.ToString(), " " ) );
#endif

                #endregion
            }
        }

        #endregion

        #region GenerateExtraSensorsCharts

        private void GenerateExtraSensorsCharts()
        {
            const string DemarcationLine = "; ExtraSensorCharts";

            bool OutputWritten = false;
            bool DemarcationLineFound = false;
            int i, j;
            string[] CutilsCharts;
            List<string> CutilsChartsMods;

            Sup.LogDebugMessage( $"GenerateExtraSensorsCharts: Generating the ExtraSensor Charts CDL code into {Sup.PathUtils}{Sup.CutilsChartsDef}..." );

            if ( !File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                Sup.LogTraceErrorMessage( $"GenerateExtraSenorsCharts: No {Sup.PathUtils}{Sup.CutilsChartsDef} present, can't modify" );
                Sup.LogTraceErrorMessage( $"GenerateExtraSenorsCharts: Please move {Sup.CutilsChartsDef} from distribution to ${Sup.PathUtils}" );
                return;
            }

            Sup.LogDebugMessage( $"GenerateExtraSensorsCharts: Testing UserModificationExtraSensorCharts: {Sup.GetUtilsIniValue( "ExtraSensors", "UserModificationExtraSensorCharts", "false" )}" );

            if ( Sup.GetUtilsIniValue( "ExtraSensors", "UserModificationExtraSensorCharts", "false" ).Equals( "true" ) )
                return;

            CutilsCharts = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" );

            for ( i = 0; i < CutilsCharts.Length; i++ )
                if ( CutilsCharts[ i ].Contains( DemarcationLine ) )
                {
                    if ( i < CutilsCharts.Length - 1 )
                        for ( j = CutilsCharts.Length - 1; j > i; j-- )
                            CutilsCharts = CutilsCharts.RemoveAt( j );
                    DemarcationLineFound = true;
                }

            CutilsChartsMods = CutilsCharts.ToList();
            if ( !DemarcationLineFound )
                CutilsChartsMods.Add( DemarcationLine );

            // Now the road is clear to add the charts from the list of plotparameters per class (Temp, Humidity etc....
            ExtraSensorType currentType;
            CutilsChartsMods.Add( "" );

            for ( i = 0; i < ExtraSensorList.Count; )
            {
                string thisKeyword;

                if ( ExtraSensorList[ i ].Type == ExtraSensorType.External )
                {
                    thisKeyword = ExtraSensorList[ i ].Name;
                    currentType = ExtraSensorType.External;
                }
                else
                {
                    thisKeyword = ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ];
                    currentType = ExtraSensorList[ i ].Type;
                }

                CutilsChartsMods.Add( $"Chart Extra{ExtraSensorList[ i ].Type} Title " +
                    $"{Sup.GetCUstringValue( "ExtraSensors", "Trend chart of Extra", "Trend chart of Extra", true )} " +
                    $"{ExtraSensorList[ i ].Type} " +
                    $"{Sup.GetCUstringValue( "ExtraSensors", "Sensors", "Sensors", true )}" );

                while ( i < ExtraSensorList.Count && ( ExtraSensorList[ i ].Type == currentType || thisKeyword.Substring( 0, 3 ).Equals( "CO2" ) ) )
                {
                    if ( ExtraSensorList[ i ].Type == ExtraSensorType.External )
                        thisKeyword = ExtraSensorList[ i ].Name;
                    else
                        thisKeyword = ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ];

                    Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Adding Sensor: {thisKeyword}" );

                    CutilsChartsMods.Add( $"  Plot Extra {thisKeyword}" );
                    _ = Sup.GetCUstringValue( "Compiler", thisKeyword, ExtraSensorList[ i ].Name, false );

                    i++;

                    if ( currentType == ExtraSensorType.AirQuality ) // Then the next item must be AirQualityAvg (that's how it's constructed
                    {
                        CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );
                        Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );

                        i++;
                    }
                }

                if ( !OutputWritten )
                {
                    CutilsChartsMods.Add( $"EndChart Output {Sup.ExtraSensorsCharts}" );
                    OutputWritten = true;
                }
                else
                    CutilsChartsMods.Add( $"EndChart" );

                CutilsChartsMods.Add( "" );
            }

            Sup.LogDebugMessage( $"GenerateExtraSensorsCharts: Writing the CutilsCharts.def" );
            File.WriteAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", CutilsChartsMods, Encoding.UTF8 );

            return;
        }

        #endregion

        #region GenerateExtraSensorDataJson

        internal void GenerateExtraSensorDataJson()
        {
            // Purpose is to create the JSON for the ExtraSensor data and offering the poissibility to do only that to accomodate the fact that
            // CMX does not (and probably will never) generate that JSON like it generates the temperature JSON for graphing.
            // CumulusUtils will only generate the ExtraSensor JSON by issueing the command: "./utils/bin/cumulusutils.exe UserAskedData"

            //InitialiseExtraSensorList();

            // Purpose is to create the JSON for the Airlink data and offering the poissibility to do only that to accomodate the fact that
            // CMX does not (and probably will never) generate that JSON like it generates the temperature JSON for graphing.
            // CumulusUtils will only generate the AirLink JSON by issueing the command: "./utils/bin/cumulusutils.exe UserAskedData"
            // When we are generating the module, the JSON is automatically generated and uploaded (in the main loop) as well
            // So here we go: it has already been determined that an Airlink is present and that we need the data

            StringBuilder sb = new StringBuilder( 300000 );

            sb.AppendLine( "{" );

            List<ExtraSensorslogValue> thisList;
            string VariableName;

            ExtraSensorslog Esl = new ExtraSensorslog( Sup );
            thisList = Esl.ReadExtraSensorslog();

            foreach ( ExtraSensor thisSensor in ExtraSensorList )  // Loop over the sensors in use
            {
                if ( thisSensor.Type == ExtraSensorType.External )
                {
                    List<ExternalExtraSensorslogValue> thisExternalList;

                    ExternalExtraSensorslog EEsl = new ExternalExtraSensorslog( Sup, thisSensor.Name );
                    thisExternalList = EEsl.ReadExternalExtraSensorslog();

                    sb.Append( $"\"{thisSensor.Name}\":[" );

                    foreach ( ExternalExtraSensorslogValue entry in thisExternalList )
                        sb.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.Value.ToString( "F2", inv )}]," );

                    sb.Remove( sb.Length - 1, 1 );
                    sb.Append( $"]," );

                    EEsl.Dispose();

                }
                else
                {
                    PropertyInfo Field;

                    VariableName = ChartsCompiler.PlotvarTypesEXTRA[ thisSensor.PlotvarIndex ];

                    if ( thisList.Count != 0 )
                    {
                        sb.Append( $"\"{VariableName}\":[" );

                        Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                        foreach ( ExtraSensorslogValue value in thisList )
                        {
                            double d = (double) Field.GetValue( value );
                            sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", inv )}]," );
                        }

                        sb.Remove( sb.Length - 1, 1 );
                        sb.Append( $"]," );
                    }

                    if ( thisSensor.Type == ExtraSensorType.CO2 )
                    {
                        for ( int i = 1; i < 8; i++ )
                        {
                            VariableName = ChartsCompiler.PlotvarTypesEXTRA[ thisSensor.PlotvarIndex + i ];

                            if ( thisList.Count != 0 )
                            {
                                sb.AppendLine( $"\"{VariableName}\":[" );

                                Field = thisList[ 0 ].GetType().GetProperty( VariableName );
                                foreach ( ExtraSensorslogValue value in thisList )
                                {
                                    double d = (double) Field.GetValue( value );
                                    sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d.ToString( "F2", inv )}]," );
                                }

                                sb.Remove( sb.Length - 1, 1 );
                                sb.Append( $"]," );
                            }
                        }
                    }
                }
            }

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( "}" );

            Esl.Dispose();

            using ( StreamWriter thisJSON = new StreamWriter( $"{Sup.PathUtils}{Sup.ExtraSensorsJSON}", false, Encoding.UTF8 ) )
            {
                thisJSON.WriteLine( sb.ToString() );
            }
        }

        #endregion

        #region GenerateExtraSensorsRealtime

        private void GenerateExtraSensorsRealtime()
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ExtraSensorsRealtimeFilename}", false, Encoding.UTF8 ) )
            {
                int ExtraTemp = 0, ExtraDP = 0, ExtraHum = 0, SoilTemp = 0, SoilMoisture = 0, AirQuality = 0, AirQualityAvg = 0, UserTemp = 0, LeafTemp = 0, LeafWetness = 0;
                StringBuilder sb = new StringBuilder();

                Sup.LogDebugMessage( $"GenerateExtraSensorsRealtime: Writing the ExtraSensors realtime file for the actual sensors found" );

                foreach ( ExtraSensor tmp in ExtraSensorList )
                {
                    switch ( (int) tmp.Type )
                    {
                        case (int) ExtraSensorType.Temperature:
                            sb.Append( $"<#ExtraTemp{++ExtraTemp} rc=y> " );
                            break;
                        case (int) ExtraSensorType.DewPoint:
                            sb.Append( $"<#ExtraDP{++ExtraDP} rc=y> " );
                            break;
                        case (int) ExtraSensorType.Humidity:
                            sb.Append( $"<#ExtraHum{++ExtraHum} rc=y> " );
                            break;
                        case (int) ExtraSensorType.SoilTemp:
                            sb.Append( $"<#SoilTemp{++SoilTemp} rc=y> " );
                            break;
                        case (int) ExtraSensorType.SoilMoisture:
                            sb.Append( $"<#SoilMoisture{++SoilMoisture} rc=y> " );
                            break;
                        case (int) ExtraSensorType.AirQuality:
                            sb.Append( $"<#AirQuality{++AirQuality} rc=y> " );
                            break;
                        case (int) ExtraSensorType.AirQualityAvg:
                            sb.Append( $"<#AirQualityAvg{++AirQualityAvg} rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2:
                            sb.Append( $"<#CO2 rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2avg:
                            sb.Append( $"<#CO2-24h rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5:
                            sb.Append( $"<#CO2-pm2p5 rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5avg:
                            sb.Append( $"<#CO2-pm2p5-24h rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm10:
                            sb.Append( $"<#CO2-pm10 rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm10avg:
                            sb.Append( $"<#CO2-pm10-24h rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2temp:
                            sb.Append( $"<#CO2-temp rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2hum:
                            sb.Append( $"<#CO2-hum rc=y> " );
                            break;
                        case (int) ExtraSensorType.UserTemp:
                            sb.Append( $"<#UserTemp{++UserTemp} rc=y> " );
                            break;
                        case (int) ExtraSensorType.LeafTemp:
                            sb.Append( $"<#LeafTemp{++LeafTemp} rc=y> " );
                            break;
                        case (int) ExtraSensorType.LeafWetness:
                            sb.Append( $"<#LeafWetness{++LeafWetness} rc=y> " );
                            break;
                        case (int) ExtraSensorType.External:
                            Sup.LogTraceWarningMessage( $"DoExtraSensorsWork: No ExtraSensorsRealTime for {tmp.Name} ({tmp.Type}) - has no realtime value." );
                            //sb.Append( $"<#{tmp.Name} rc=y> " );  // Maybe later
                            break;
                        default:
                            Sup.LogTraceErrorMessage( $"DoExtraSensorsWork: Illegal ExtraSensor type {tmp.Type} - no realtime value." );
                            break;
                    }
                }

                Sup.LogDebugMessage( $"GenerateExtraSensorsRealtime: {sb}" );

                of.Write( sb );
            }
        }

        #endregion

        #region SupportingFunctions

        private void InitialiseExtraSensorList()
        {
            string thisSensor;
            int PlotvarStartindex;

            // Research which ExtraSensors are present using strings.ini (the use MUST rename from the defaults
            // or check values for the lighning sensor and the CO2 sensor because these have no or no likely translation entry in strings.ini

            ExtraSensorList = new List<ExtraSensor>();

            PlotvarStartindex = -1;

            // Extra Temperature sensors
            for ( int i = 1; i <= 10; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "ExtraTempCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.Temperature,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );

                    // And immediately set the name as default in the Language file so the chart legend becomes comprehensible
                    // and does not require additional user action
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra Humidity sensors
            PlotvarStartindex += 10;
            for ( int i = 1; i <= 10; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "ExtraHumCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.Humidity,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra Dewpoint sensors
            PlotvarStartindex += 10;
            for ( int i = 1; i <= 10; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "ExtraDPCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.DewPoint,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra SoilTemperature sensors
            PlotvarStartindex += 10;
            for ( int i = 1; i <= 16; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "SoilTempCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.SoilTemp,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra Soil Moisture sensors
            PlotvarStartindex += 16;
            for ( int i = 1; i <= 16; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "SoilMoistureCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.SoilMoisture,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra AirQuality sensors
            PlotvarStartindex += 16;
            for ( int i = 1; i <= 4; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "AirQualityCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.AirQuality,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }

                thisSensor = Sup.GetStringsIniValue( "AirQualityCaptions", $"SensorAvg{i}", "" );
                if ( !thisSensor.Equals( $"Sensor Avg {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.AirQualityAvg,
                        PlotvarIndex = PlotvarStartindex + i + 4,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i + 4 ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            PlotvarStartindex += 8;

            for ( int i = 1; i <= 8; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "UserTempCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.UserTemp,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra Leaf Temperature sensors
            PlotvarStartindex += 8;
            for ( int i = 1; i <= 2; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "LeafTempCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.LeafTemp,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]

                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            // Extra Leaf Wetness sensors
            PlotvarStartindex += 2;
            for ( int i = 1; i <= 2; i++ )
            {
                thisSensor = Sup.GetStringsIniValue( "LeafWetnessCaptions", $"Sensor{i}", "" );
                if ( !thisSensor.Equals( $"Sensor {i}" ) && !string.IsNullOrEmpty( thisSensor ) )
                {
                    tmp = new ExtraSensor
                    {
                        Name = thisSensor,
                        Type = ExtraSensorType.LeafWetness,
                        PlotvarIndex = PlotvarStartindex + i,
                        PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + i ]
                    };

                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
            }

            PlotvarStartindex += 2;
            // Check for the CO2 sensor (WH45). If there is a non-zero value in the CO2 field then the sensor is present.
            {
                string[] theseLines;            // I need the last line to be sure, so read all and pick last
                string[] thislineFields;

                DateTime timeStart = CMXutils.RunStarted;
                string Filename = $"data/ExtraLog{timeStart:yyyy}{timeStart:MM}.txt";

                if ( !File.Exists( Filename ) )
                    return; // Nothing to do, may not happen

                string filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );
                if ( File.Exists( filenameCopy ) )
                    File.Delete( filenameCopy );
                File.Copy( Filename, filenameCopy );

                theseLines = File.ReadAllLines( filenameCopy );
                thislineFields = theseLines[ theseLines.Length - 1 ].Split( theseLines[ 0 ][ 8 ] );

                int FieldInUse = (int) ExtraSensorslogFieldName.CO2;

                // @formatter:off
                if ( thislineFields[ FieldInUse ] != "0" )
                {
                    thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Current", "" );
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2, PlotvarIndex = PlotvarStartindex + 1, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 1 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-24hr", "" );
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2avg, PlotvarIndex = PlotvarStartindex + 2, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 2 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm2p5", "" );
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2pm2p5, PlotvarIndex = PlotvarStartindex + 3, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 3 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm2p5-24hr", "" );
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2pm2p5avg, PlotvarIndex = PlotvarStartindex + 4, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 4 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm10", "" );
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2pm10, PlotvarIndex = PlotvarStartindex + 5, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 5 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm10-24hr", "" );
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2pm10avg, PlotvarIndex = PlotvarStartindex + 6, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 6 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = "WH45 Temperature";
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2temp, PlotvarIndex = PlotvarStartindex + 7, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 7 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );

                    thisSensor = "WH45 Humidity";
                    tmp = new ExtraSensor { Name = thisSensor, Type = ExtraSensorType.CO2hum, PlotvarIndex = PlotvarStartindex + 8, PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + 8 ] };
                    ExtraSensorList.Add( tmp );
                    Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor );
                }
                // @formatter:on

                // Do the External Extra Sensors
                {
                    string[] ExternalExtraSensors = Sup.GetUtilsIniValue( "ExtraSensors", "ExternalExtraSensors", "" ).Split( ',' );

                    if ( !string.IsNullOrEmpty( ExternalExtraSensors[ 0 ] ) )
                    {
                        foreach ( string thisExternal in ExternalExtraSensors )
                        {
                            tmp = new ExtraSensor
                            {
                                Name = thisExternal,
                                Type = ExtraSensorType.External,
                                PlotvarIndex = 0,  // Nul for every External because this will not be an index in an array, every external is on its own
                                PlotvarType = thisExternal
                            };

                            ExtraSensorList.Add( tmp );

                            // And immediately set the name as default in the Language file so the chart legend becomes comprehensible
                            // and does not require additional user action
                            Sup.SetCUstringValue( "Compiler", tmp.PlotvarType, thisExternal );
                        }
                    }
                }

                Sup.LogTraceInfoMessage( $"InitialiseExtraSensorList: Found the following Extra Sensors:" );
                foreach ( ExtraSensor tmp in ExtraSensorList )
                    Sup.LogTraceInfoMessage( $"  {tmp.Name} of type: {tmp.Type}" );
            }

            return;
        }

        #endregion

        #region SensorCommunity Iframe

        internal void CreateSensorCommunityMapIframeFile()
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.SensorCommunityOutputFilename}", false, Encoding.UTF8 ) )
            {
                string Latitude = Sup.GetCumulusIniValue( "Station", "Latitude", "" );
                string Longitude = Sup.GetCumulusIniValue( "Station", "Longitude", "" );

                of.WriteLine( $"<iframe src='https://maps.sensor.community/?nowind#11/{Latitude}/{Longitude}' width='100%' frameborder='0' style='border:0;height:75vh;'></a>" );
            }
        }

        #endregion

    } // Class ExtraSensors
} // Namespace CumulusUtils
