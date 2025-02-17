/*
 * DayRecords - Part of CumulusUtils
 *
 * © Copyright 2019-2024 Hans Rottier <hans.rottier@gmail.com>
 *
 * The code of CumulusUtils is public domain and distributed under the  
 * Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License
 * (Note: this is different license than for CumulusMX itself, it is basically is usage license)
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 2 september 2019 with Top10 and pwsFWI .NET Framework 4.8
 *              Initial release: pwsFWI                 (version 1.0)
 *                               Website Generator      (version 3.0)
 *                               ChartsCompiler         (version 5.0)
 *                               Maintenance releases   (version 6.x) including CustomLogs
 *              Startdate : 16 november 2021 start of conversion to .NET 5, 6 and 7
 *              Startdate : 15 january 2024 start of conversion to .NET 8
 *              
 * Environment: Raspberry Pi 4B and up
 *              Raspberry Pi OS
 *              C# / Visual Studio / Windows for development
 *              
 * Design / Structure:
 *              1) create the  list of actual sensors being logged based on 
 *                 Cumulus.ini - [section Station], param: LogExtraSensors=1
 *                 strings.ini - [section ExtraTempCaptions] - all entry value != "Sensor i", i is 10  max
 *                             - [section ExtraHumCaptions]  - all entry value != "Sensor i", i is 10  max
 *                             - [section ExtraDPCaptions] - all entry value != "Sensor i", i is 10  max
 *                             - [section SoilTempCaptions] - all entry value != "Sensor i", i is 16  max
 *                             - [section SoilMoistureCaptions] - all entry value != "Sensor i", i is 16  max
 *                             - [section LeafTempCaptions] - all entry value != "Sensor i", i is 4  max
 *                             - [section LeafWetnessCaptions] - all entry value != "Sensor i", i is 8  max
 *                             - [section AirQualityCaptions] - all entry value != "Sensor i", i is 4 max (must have their equal for the avg entries)
 *                             - [section UserTempCaptions] - all entry value != "Sensor i", i is 8  max
 *                             - [section CO2Captions] - If set in Parameters AND (has a value in the  logfile on the current CO2 value) THEN is sensor present
 *                             - Lightning sensor - Is present If set true in ExtraSensors parameter section
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CumulusUtils
{
    public class ExtraSensors
    {
        public enum ExtraSensorType
        {
            Temperature, Humidity, DewPoint, SoilTemp, SoilMoisture, AirQuality, AirQualityAvg, UserTemp, LeafTemp, LeafWetness, LaserDist, LaserDepth,
            CO2, CO2avg, CO2pm2p5, CO2pm2p5avg, CO2pm10, CO2pm10avg, CO2temp, CO2hum, External, Lightning
        }

        public struct ExtraSensor
        {
            public string Name;
            public string PlotvarType;
            public int PlotvarIndex;
            public ExtraSensorType Type;
            public int SensorIndex;
            public int RTposition;
        }

        readonly CuSupport Sup;
        ExtraSensor tmp;
        List<ExtraSensor> ExtraSensorList;

        #region Constructor
        public ExtraSensors( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "Extra Sensors constructor: starting" );

            // After the next call ExtraSensorList contains all active Extra Sensors
            InitialiseExtraSensorList();

            Sup.LogTraceInfoMessage( "Extra Sensors constructor: stop" );

            return;
        }

        #endregion

        #region DoExtraSensors
        public void DoExtraSensors()
        {
            Sup.LogDebugMessage( "DoExtraSensors - Starting" );

            // I: create the extrasensorsrealtime.txt which has to be processed by CMX and transferred to the webroot.
            // 
            GenerateExtraSensorsRealtime();
            GenerateExtraSensorsCharts();
            GenerateExtraSensorsModule();

            Sup.LogTraceInfoMessage( "DoExtraSensors - Stop" );

            return;
        }

        #endregion DoExtraSensors

        #region GenerateExtraSensorsModule

        public void GenerateExtraSensorsModule()
        {
            #region Javascript

            StringBuilder sb = new StringBuilder();

            Sup.LogDebugMessage( $"GenerateExtraSensorsModule: Generating the Module Javascript code..." );

            // Generate the table on the basis of what is set in the inifile and setup in the constructor. 
            // We don't support live switching as we don't support that for anything.
            // Generate the start of the realtime script
            //

            sb.AppendLine( $"{CuSupport.GenjQueryIncludestring()}" );

            if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
            {
                sb.AppendLine( Sup.GenHighchartsIncludes().ToString() );
            }

            sb.AppendLine( "<script>" );
            sb.AppendLine( "console.log('Module ExtraSensors ...');" );
            sb.AppendLine( "var ExtraSensorTimer;" );
            sb.AppendLine( "$(function () {" );  // Get the whole thing going
            sb.AppendLine( $"  SetupExtraSensorsTable();" );
            sb.AppendLine( "  loadExtraSensorsRealtime();" );
            sb.AppendLine( "  if (ExtraSensorTimer == null) ExtraSensorTimer = setInterval(loadExtraSensorsRealtime, 60 * 1000);" );
            sb.AppendLine( $"  LoadUtilsReport( '{Sup.ExtraSensorsCharts}', true );" );
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

            sb.AppendLine( $"var oldobsExtra = [ {ExtraSensorList.Count} ]; " );
            sb.AppendLine( "function DoExtraSensorRT(input) {" );
            sb.AppendLine( "  var ExtraSensorRT = input.split(' ');" );

            int i = 0;

            foreach ( ExtraSensor tmp in ExtraSensorList )
            {
                switch ( (int) tmp.Type )
                {
                    case (int) ExtraSensorType.Temperature:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxExtraTemp{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {Sup.StationTemp.Text()}');" );
                        sb.AppendLine( $"    $('#ajxExtraTemp{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.DewPoint:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxExtraDP{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {Sup.StationTemp.Text()}');" );
                        sb.AppendLine( $"    $('#ajxExtraDP{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.Humidity:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxExtraHum{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' %');" );
                        sb.AppendLine( $"    $('#ajxExtraHum{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.SoilTemp:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxSoilTemp{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {Sup.StationTemp.Text()}');" );
                        sb.AppendLine( $"    $('#ajxSoilTemp{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.SoilMoisture:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxSoilMoisture{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ]);" );
                        sb.AppendLine( $"    $('#ajxSoilMoisture{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.AirQuality:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxAirQuality{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {PMconc.Text()}');" );
                        sb.AppendLine( $"    $('#ajxAirQuality{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.AirQualityAvg:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxAirQualityAvg{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {PMconc.Text()}');" );
                        sb.AppendLine( $"    $('#ajxAirQualityAvg{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.LaserDist:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxLaserDist{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' cm');" );
                        sb.AppendLine( $"    $('#ajxLaserDist{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.LaserDepth:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxLaserDepth{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' cm');" );
                        sb.AppendLine( $"    $('#ajxLaserDepth{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {CO2conc.Text()}');" );
                        sb.AppendLine( $"    $('#ajxCO2').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2avg:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2-24h').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {CO2conc.Text()}');" );
                        sb.AppendLine( $"    $('#ajxCO2-24h').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2pm2p5:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2pm2p5').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {PMconc.Text()}');" );
                        sb.AppendLine( $"    $('#ajxCO2pm2p5').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2pm2p5avg:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2pm2p5-24h').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {PMconc.Text()}');" );
                        sb.AppendLine( $"    $('#ajxCO2pm2p5-24h').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2pm10:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2pm10').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {PMconc.Text()} ');" );
                        sb.AppendLine( $"    $('#ajxCO2pm10').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2pm10avg:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2pm10-24h').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {PMconc.Text()} ');" );
                        sb.AppendLine( $"    $('#ajxCO2pm10-24h').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2temp:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2temp').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {Sup.StationTemp.Text()}');" );
                        sb.AppendLine( $"    $('#ajxCO2temp').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.CO2hum:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCO2hum').html(ExtraSensorRT[ {tmp.RTposition} ] + ' %');" );
                        sb.AppendLine( $"    $('#ajxCO2hum').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.UserTemp:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxUserTemp{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ] + ' {Sup.StationTemp.Text()}');" );
                        sb.AppendLine( $"    $('#ajxUserTemp{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.LeafWetness:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxLeafWetness{tmp.SensorIndex}').html(ExtraSensorRT[ {tmp.RTposition} ]);" );
                        sb.AppendLine( $"    $('#ajxLeafWetness{tmp.SensorIndex}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                        break;
                    case (int) ExtraSensorType.External:
                        //sb.AppendLine( $" $('#{tmp.Name}').html(ExtraSensorRT[ {i++} ]);" );
                        break;
                    case (int) ExtraSensorType.Lightning:
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    tmp = ExtraSensorRT[ {i} ] == 0 ? '' : ' \u26a1';" );
                        sb.AppendLine( $"    $('#ajxLightningStrikesToday').html(ExtraSensorRT[ {tmp.RTposition} ] + tmp);" );
                        sb.AppendLine( $"    $('#ajxLightningStrikesToday').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );

                        sb.AppendLine( $"    $('#ajxLightningTime').html(ExtraSensorRT[ {tmp.RTposition + 1} ] + ' ' + ExtraSensorRT[ {tmp.RTposition + 2} ]);" );
                        sb.AppendLine( $"    $('#ajxLightningTime').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );

                        sb.AppendLine( $"    $('#ajxLightningDistance').html(Math.round(ExtraSensorRT[ {tmp.RTposition + 3} ]) + ' {Sup.StationDistance.Text()}');" );
                        sb.AppendLine( $"    $('#ajxLightningDistance').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );

                        break;
                    default:
                        Sup.LogTraceErrorMessage( $"GenerateExtraSensorsModule: At impossible Switch default assigning realtime values" );
                        break;
                }

                i++;
            }

            sb.AppendLine( "  setTimeout( 'ExtraClearChangeIndicator()', 3000 );" );
            sb.AppendLine( "}" ); // End DoExtraSensorRT

            sb.AppendLine( "function ExtraClearChangeIndicator(){" );
            sb.AppendLine( "  $( '[id*=\"ajx\"]' ).css( 'color', '' );" );
            sb.AppendLine( "}" );
            sb.AppendLine( "" );

            // Setup the HTML ExtraSensors table for the Dashboard area

            sb.AppendLine( "function SetupExtraSensorsTable() {" );

            StringBuilder buf = new StringBuilder();

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

            buf.Append( $"<style>.centerItem {{width: 80%; max-height: 70vh; margin: 6vh auto;overflow-y: auto; }}</style>" );
            buf.Append( $"<div class='centerItem' style='text-align:left;'><table style='width:100%'>" );
            buf.Append( $"<tr " +
                $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
            buf.Append( $"<th {thisPadding()}>{Sup.GetCUstringValue( "ExtraSensors", "SensorName", "Sensor Name", false )}</th>" +
                $"<th>{Sup.GetCUstringValue( "ExtraSensors", "Value", "Value", false )}</th></tr>" );

            foreach ( ExtraSensor tmp in ExtraSensorList )
            {
                switch ( (int) tmp.Type )
                {
                    case (int) ExtraSensorType.Temperature:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxExtraTemp{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.DewPoint:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxExtraDP{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.Humidity:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxExtraHum{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.SoilTemp:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxSoilTemp{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.SoilMoisture:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxSoilMoisture{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.AirQuality:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxAirQuality{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.AirQualityAvg:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxAirQualityAvg{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.LaserDist:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxLaserDist{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.LaserDepth:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxLaserDepth{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2avg:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2-24h'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2pm2p5:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()} > {Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2pm2p5'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2pm2p5avg:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2pm2p5-24h'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2pm10:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2pm10'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2pm10avg:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2pm10-24h'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2temp:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2temp'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.CO2hum:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxCO2hum'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.UserTemp:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxUserTemp{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.LeafTemp:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxLeafTemp{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.LeafWetness:
                        buf.Append(
                            $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td id='ajxLeafWetness{tmp.SensorIndex}'></td></tr>" );
                        break;
                    case (int) ExtraSensorType.External:
                        Sup.LogTraceWarningMessage( $"GenerateExtraSensorsModule: External realtime not implemented." );
                        break;
                    case (int) ExtraSensorType.Lightning:
                        buf.Append( $"<tr {RowColour()}><td {thisPadding()}>{Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, "", false )}</td><td></td></tr>" );
                        buf.Append( $"<tr {RowColour()}><td {thisPadding()}>&nbsp;&nbsp;{Sup.GetCUstringValue( "Compiler", "StrikesToday", "Strikes today", false )}</td><td id='ajxLightningStrikesToday'></td></tr>" );
                        buf.Append( $"<tr {RowColour()}><td {thisPadding()}>&nbsp;&nbsp;{Sup.GetCUstringValue( "Compiler", "TimeLastStrike", "Time last strike", false )}</td><td id='ajxLightningTime'></td></tr>" );
                        buf.Append( $"<tr {RowColour()}><td {thisPadding()}>&nbsp;&nbsp;{Sup.GetCUstringValue( "Compiler", "DistanceLastStrike", "Distance last strike", false )}</td><td id='ajxLightningDistance'></td></tr>" );
                        break;
                    default:
                        Sup.LogTraceErrorMessage( $"GenerateExtraSensorsModule: At impossible Switch default generating the table" );
                        break;
                }
            }

            buf.Append( "</table></div>" );
            sb.AppendLine( $"  $('#ExtraAndCustom').html(\"{buf}\");" );
            sb.AppendLine( "}" );
            sb.AppendLine( "</script>" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.ExtraSensorsOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

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
            bool OutputWritten = false;
            int i;
            string[] CutilsChartsIn;

            List<string> CutilsChartsMods;

            Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Generating the ExtraSensor Charts CDL code into {Sup.PathUtils}{Sup.CutilsChartsDef}..." );

            if ( !File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                Sup.LogTraceErrorMessage( $"GenerateExtraSenorsCharts: No {Sup.PathUtils}{Sup.CutilsChartsDef} present, can't modify" );
                Sup.LogTraceErrorMessage( $"GenerateExtraSenorsCharts: Please move {Sup.CutilsChartsDef} from distribution to ${Sup.PathUtils}" );
                return;
            }

            Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Testing UserModificationExtraSensorCharts: {Sup.GetUtilsIniValue( "ExtraSensors", "UserModificationExtraSensorCharts", "false" )}" );
            if ( Sup.GetUtilsIniValue( "ExtraSensors", "UserModificationExtraSensorCharts", "false" ).Equals( "true", CUtils.Cmp ) ) return;

            CutilsChartsIn = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" );

            for ( i = 0; i < CutilsChartsIn.Length; i++ )
            {
                if ( CutilsChartsIn[ i ].Contains( Sup.DemarcationLineExtraSensors ) && i < CutilsChartsIn.Length )
                {
                    for ( ; i < CutilsChartsIn.Length && !CutilsChartsIn[ i ].Contains( Sup.DemarcationLineCustomLogs ); )
                        CutilsChartsIn = CutilsChartsIn.RemoveAt( i );

                    if ( i >= CutilsChartsIn.Length || CutilsChartsIn[ i ].Contains( Sup.DemarcationLineCustomLogs ) ) break;
                }
            }

            CutilsChartsMods = CutilsChartsIn.ToList();

            CutilsChartsMods.Add( "" );
            CutilsChartsMods.Add( Sup.DemarcationLineExtraSensors );
            CutilsChartsMods.Add( "" );

            // Now the road is clear to add the charts from the list of plotparameters per class (Temp, Humidity etc....

            for ( i = 0; i < ExtraSensorList.Count; )
            {
                if ( ExtraSensorList[ i ].Type == ExtraSensorType.Lightning ) { i++; continue; }; // atm no lightning data in the JSON, later...Maybe...

                CutilsChartsMods.Add( $"Chart Extra{ExtraSensorList[ i ].Type} Title " +
                    $"{Sup.GetCUstringValue( "ExtraSensors", "Trend chart of Extra", "Trend chart of Extra", true )} " +
                    $"{ExtraSensorList[ i ].Type} " +
                    $"{Sup.GetCUstringValue( "ExtraSensors", "Sensors", "Sensors", true )}" );

                if ( ExtraSensorList[ i ].Type == ExtraSensorType.CO2 )
                {
                    Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Adding Sensor: {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );

                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 1 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 1 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 2 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 2 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 3 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 3 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 4 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 4 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 5 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 5 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 6 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 6 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                    CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 7 ].PlotvarIndex ]}" );
                    _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 7 ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );

                    i += 7;
                    //if ( ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i + 1 ].PlotvarIndex ].Substring( 0, 3 ).Equals( "CO2", CUtils.Cmp ) ) i++;
                    //else break;
                }
                else if ( ExtraSensorList[ i ].Type == ExtraSensorType.External )
                {
                    Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Adding Sensor: {ExtraSensorList[ i ].Name}" );

                    CutilsChartsMods.Add( $"  Plot Extra {ExtraSensorList[ i ].Name}" );
                    _ = Sup.GetCUstringValue( "Compiler", ExtraSensorList[ i ].Name, ExtraSensorList[ i ].Name, false );
                }
                else
                {
                    ExtraSensorType currentType = ExtraSensorList[ i ].Type;

                    do
                    {
                        Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Adding Sensor: {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );

                        CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );
                        _ = Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );

                        if ( ExtraSensorList[ i ].Type == ExtraSensorType.AirQuality ) // Then the next item must be AirQualityAvg (anomaly but that's how it's constructed)
                        {
                            // Required here because  of AirQualityAvg: 
                            i++;

                            CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );
                            Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );
                        }

                        if ( i + 1 < ExtraSensorList.Count && currentType == ExtraSensorList[ i + 1 ].Type ) i++;
                        else break;
                    } while ( true );
                }

                i++; // So now we continue with the next ExtraSensorType

                if ( !OutputWritten )
                {
                    CutilsChartsMods.Add( $"EndChart Output {Sup.ExtraSensorsCharts}" );
                    OutputWritten = true;
                }
                else
                    CutilsChartsMods.Add( $"EndChart" );

                CutilsChartsMods.Add( "" );
            }

            Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Writing the CutilsCharts.def" );
            File.WriteAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", CutilsChartsMods, Encoding.UTF8 );

            return;
        }

        #endregion

        #region GenerateExtraSensorDataJson

        public void GenerateExtraSensorDataJson()
        {
            // Purpose is to create the JSON for the ExtraSensors data and offering the possibility to do only that to accomodate the fact that
            // CMX does not (and probably will never) generate that JSON like it generates the temperature JSON for graphing.
            // CumulusUtils will only generate the ExtraSensors JSON by issueing the command: "./utils/bin/cumulusutils.exe UserAskedData"

            // When we are generating the module, the JSON is automatically generated and uploaded (in the main loop) as well
            // So here we go: it has already been determined that ExtraSensors are present and that we need the data

            StringBuilder sb = new StringBuilder();

            sb.Append( '{' );

            List<ExtraSensorslogValue> thisList;
            string VariableName;

            ExtraSensorslog Esl = new ExtraSensorslog( Sup );
            thisList = Esl.ReadExtraSensorslog();

            foreach ( ExtraSensor thisSensor in ExtraSensorList )  // Loop over the sensors in use
            {
                if ( thisSensor.Type == ExtraSensorType.Lightning ) continue; // atm no lightning data in the JSON, later...

                if ( thisSensor.Type == ExtraSensorType.External )
                {
                    List<ExternalExtraSensorslogValue> thisExternalList;

                    ExternalExtraSensorslog EEsl = new ExternalExtraSensorslog( Sup, thisSensor.Name );
                    thisExternalList = EEsl.ReadExternalExtraSensorslog();

                    sb.Append( $"\"{thisSensor.Name}\":[" );

                    foreach ( ExternalExtraSensorslogValue entry in thisExternalList )
                        sb.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.Value.ToString( "F2", CUtils.Inv )}]," );

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
                            double? d = (double?) Field.GetValue( value );

                            if ( d is not null ) sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},{d?.ToString( "F2", CUtils.Inv )}]," );
                            else sb.Append( $"[{CuSupport.DateTimeToJS( value.ThisDate )},null]," );
                        }

                        sb.Remove( sb.Length - 1, 1 );
                        sb.Append( $"]," );
                    }
                } // External or noraml ExtraSensor
            } // Loop over ExtraSensors

            sb.Remove( sb.Length - 1, 1 );
            sb.Append( '}' );

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
                StringBuilder sb = new StringBuilder();

                Sup.LogTraceInfoMessage( $"GenerateExtraSensorsRealtime: Writing the ExtraSensors realtime file for the actual sensors found" );

                foreach ( ExtraSensor tmp in ExtraSensorList )
                {
                    switch ( (int) tmp.Type )
                    {
                        case (int) ExtraSensorType.Temperature:
                            sb.Append( $"<#ExtraTemp{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.DewPoint:
                            sb.Append( $"<#ExtraDP{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.Humidity:
                            sb.Append( $"<#ExtraHum{+tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.SoilTemp:
                            sb.Append( $"<#SoilTemp{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.SoilMoisture:
                            sb.Append( $"<#SoilMoisture{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.AirQuality:
                            sb.Append( $"<#AirQuality{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.AirQualityAvg:
                            sb.Append( $"<#AirQualityAvg{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.LaserDist:
                            sb.Append( $"<#LaserDist{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.LaserDepth:
                            sb.Append( $"<#LaserDepth{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2:
                            sb.Append( $"<#CO2 rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2avg:
                            sb.Append( $"<#CO2_24h rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5:
                            sb.Append( $"<#CO2_pm2p5 rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm2p5avg:
                            sb.Append( $"<#CO2_pm2p5-24h rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm10:
                            sb.Append( $"<#CO2_pm10 rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2pm10avg:
                            sb.Append( $"<#CO2_pm10-24h rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2temp:
                            sb.Append( $"<#CO2_temp rc=y> " );
                            break;
                        case (int) ExtraSensorType.CO2hum:
                            sb.Append( $"<#CO2_hum rc=y> " );
                            break;
                        case (int) ExtraSensorType.UserTemp:
                            sb.Append( $"<#UserTemp{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.LeafWetness:
                            sb.Append( $"<#LeafWetness{tmp.SensorIndex} rc=y> " );
                            break;
                        case (int) ExtraSensorType.External:
                            Sup.LogTraceWarningMessage( $"DoExtraSensorsWork: No ExtraSensorsRealTime for {tmp.Name} ({tmp.Type}) - has no realtime value." );
                            break;
                        case (int) ExtraSensorType.Lightning:
                            sb.Append( $"<#LightningStrikesToday> <#LightningTime format=\"g\"> <#LightningDistance rc=y> " );
                            break;
                        default:
                            Sup.LogTraceErrorMessage( $"DoExtraSensorsWork: Illegal ExtraSensor type {tmp.Type} - no realtime value." );
                            break;
                    }
                }

                Sup.LogTraceInfoMessage( $"GenerateExtraSensorsRealtime: {sb}" );

                of.Write( sb );
            }
        }

        #endregion

        #region SupportingFunctions

        private void InitialiseExtraSensorList()
        {
            string thisSensor;
            int PlotvarStartindex = -1, RTindex = 0;
            int[] ActiveSensors;

            ExtraSensorList = new List<ExtraSensor>();

            // Search which ExtraSensors are present using strings.ini (the use MUST rename from the defaults)
            // or check values for the lightning sensor and the CO2 sensor because these have no or no likely translation entry in strings.ini

            // Extra Temperature sensors
            ActiveSensors = GetActiveSensors( "ExtraTemp" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "ExtraTempCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.Temperature );
            }

            // Extra Humidity sensors
            PlotvarStartindex += 10;
            ActiveSensors = GetActiveSensors( "ExtraHum" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "ExtraHumCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.Humidity );
            }

            // Extra Dewpoint sensors
            PlotvarStartindex += 10;
            ActiveSensors = GetActiveSensors( "ExtraDP" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "ExtraDPCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.DewPoint );
            }

            // Extra SoilTemperature sensors
            PlotvarStartindex += 10;
            ActiveSensors = GetActiveSensors( "SoilTemp" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "SoilTempCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.SoilTemp );
            }

            // Extra Soil Moisture sensors
            PlotvarStartindex += 16;
            ActiveSensors = GetActiveSensors( "SoilMoisture" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "SoilMoistureCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.SoilMoisture );
            }

            // Extra AirQuality sensors
            PlotvarStartindex += 16;
            ActiveSensors = GetActiveSensors( "AirQuality" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "AirQualityCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.AirQuality );
            }

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "AirQualityCaptions", $"SensorAvg{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.AirQualityAvg );
            }

            // Extra UserTemp sensors
            PlotvarStartindex += 8;
            ActiveSensors = GetActiveSensors( "UserTemp" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "UserTempCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.UserTemp );
            }

            // Extra Leaf Wetness sensors
            PlotvarStartindex += 8;
            ActiveSensors = GetActiveSensors( "LeafWetness" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "LeafWetnessCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.LeafWetness );
            }

            // Extra LaserDist sensors
            PlotvarStartindex += 8;
            ActiveSensors = GetActiveSensors( "LaserDist" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "LaserDistCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.LaserDist );
            }

            // Extra LaserDepth sensors
            PlotvarStartindex += 4;
            ActiveSensors = GetActiveSensors( "LaserDepth" );

            foreach ( int i in ActiveSensors )
            {
                thisSensor = Sup.GetStringsIniValue( "LaserDepthCaptions", $"Sensor{i}", "" );
                registerSensor( thisSensor, i, ExtraSensorType.LaserDepth );
            }

            // Check for the CO2 sensor (WH45). If there is a non-zero value in the CO2 field then the sensor is present.
            PlotvarStartindex += 4;
            ActiveSensors = GetActiveSensors( "CO2" );

            if ( ActiveSensors.Length > 1 )  // 
            {
                Sup.LogTraceErrorMessage( $"GetActiveSensors: CO2 sensors - There can be only one! Please check configuration." );
            }
            else
            {
                foreach ( int i in ActiveSensors )
                {
                    if ( i == 0 ) continue;

                    string[] theseLines;            // I need the last line to be sure, so read all and take last
                    string[] thislineFields;

                    DateTime timeStart = CUtils.RunStarted;
                    string Filename = $"data/ExtraLog{timeStart:yyyy}{timeStart:MM}.txt";

                    if ( !File.Exists( Filename ) )
                    {
                        Sup.LogTraceErrorMessage( $"CO2 sensors - No logfile...  quitting the activation of CO2 sensor." );
                        return; // Nothing to do, may not happen
                    }

                    string filenameCopy = "data/" + "copy_" + Path.GetFileName( Filename );
                    if ( File.Exists( filenameCopy ) ) File.Delete( filenameCopy );
                    File.Copy( Filename, filenameCopy );

                    theseLines = File.ReadAllLines( filenameCopy );
                    thislineFields = theseLines[ ^1 ].Split( theseLines[ 0 ][ 8 ] );

                    int FieldInUse = (int) ExtraSensorslogFieldName.CO2;

                    // @formatter:off
                    if ( thislineFields[ FieldInUse ] != "0" )
                    {
                        thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Current", "" );
                        registerSensor( thisSensor, 1, ExtraSensorType.CO2 );

                        thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-24hr", "" );
                        registerSensor( thisSensor, 2, ExtraSensorType.CO2avg );

                        thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm2p5", "" );
                        registerSensor( thisSensor, 3, ExtraSensorType.CO2pm2p5 );

                        thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm2p5-24hr", "" );
                        registerSensor( thisSensor, 4, ExtraSensorType.CO2pm2p5avg );

                        thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm10", "" );
                        registerSensor( thisSensor, 5, ExtraSensorType.CO2pm10 );

                        thisSensor = Sup.GetStringsIniValue( "CO2Captions", "CO2-Pm10-24hr", "" );
                        registerSensor( thisSensor, 6, ExtraSensorType.CO2pm10avg );

                        thisSensor = "WH45 Temperature";
                        registerSensor( thisSensor, 7, ExtraSensorType.CO2temp );

                        thisSensor = "WH45 Humidity";
                        registerSensor( thisSensor, 8, ExtraSensorType.CO2hum );
                    }
                    else
                    {
                        Sup.LogTraceErrorMessage( $"GetActiveSensors: CO2 sensor is active but there is no data. Sensor is ignored" );
                    }
                    // @formatter:on
                }
            }

            PlotvarStartindex += 8;

            //PlotvarStartindex += 8;
            // Check for the Lightning sensor (WH51). User has to indicate this in cumulusutils.ini as this sensor has no logging (not a PlotvarType!!).
            {
                bool DoLightningSensor = Sup.GetUtilsIniValue( "ExtraSensors", "LightningSensor", "false" ).Equals( "true", CUtils.Cmp );

                if ( DoLightningSensor )
                {
                    thisSensor = "Lightning";
                    registerSensor( thisSensor, 1, ExtraSensorType.Lightning );
                }
            }

            // Do the External Extra Sensors
            {
                string[] ExternalExtraSensors = Sup.GetUtilsIniValue( "ExtraSensors", "ExternalExtraSensors", "" ).Split( GlobConst.CommaSeparator );

                if ( !string.IsNullOrEmpty( ExternalExtraSensors[ 0 ] ) )
                    foreach ( string thisExternal in ExternalExtraSensors )
                        registerSensor( thisExternal, 1, ExtraSensorType.External );
            }

            Sup.LogTraceInfoMessage( $"InitialiseExtraSensorList: Creating ExtraSensors list Done. CUtils Continues, if any configuration errors, please correct." );
            Sup.LogTraceInfoMessage( $"InitialiseExtraSensorList: Found the following Extra Sensors:" );
            foreach ( ExtraSensor tmp in ExtraSensorList )
                Sup.LogTraceInfoMessage( $"  {tmp.Name} of type: {tmp.Type}" );

            return;

            // Private functions to help in setting up the ExtraSensor:List
            // 1) GetActiveSensors : Get the string with configured ExtraSensors from cumulusutils.ini and convert to array of int
            //
            int[] GetActiveSensors( string Type )
            {
                Sup.LogTraceInfoMessage( $"GetActiveSensors: Getting Extra Sensors for {Type}" );

                //char[] charSeparators = new char[] { ',' };

                string a = Sup.GetUtilsIniValue( "ExtraSensors", Type, "" );
                string[] sensorsAsStrings = a.Split( GlobConst.CommaSeparator, StringSplitOptions.RemoveEmptyEntries );
                int[] theseSensors;

                try
                {
                    theseSensors = new int[ sensorsAsStrings.Length ];

                    for ( int i = 0; i < sensorsAsStrings.Length; i++ )
                        theseSensors[ i ] = Convert.ToInt32( sensorsAsStrings[ i ] );
                }
                catch ( Exception e )
                {
                    // reinitialise to make sure we have a correct array with all nuls
                    theseSensors = new int[ sensorsAsStrings.Length ];
                    Sup.LogTraceErrorMessage( $"GetActiveSensors: Exception {e.Message} \n- can't configure for type = {Type}. Please check configuration." );
                }

                return theseSensors;
            }

            // 2) registerSensor : Create an entry in the ExtraSensorList for a configured sensor
            //
            void registerSensor( string thisSensor, int sensorIndex, ExtraSensorType sensorType )
            {
                // Elementary errorcheck, full range check vcan be implemented, I will when it becomes a nuisance
                //
                if ( sensorIndex == 0 ) return;

                tmp = new ExtraSensor
                {
                    Name = thisSensor,
                    Type = sensorType,
                    PlotvarIndex = PlotvarStartindex + sensorIndex,
                    PlotvarType = sensorType == ExtraSensorType.External ? thisSensor : ChartsCompiler.PlotvarTypesEXTRA[ PlotvarStartindex + sensorIndex ],
                    SensorIndex = sensorIndex,
                    RTposition = RTindex++,
                };

                // Do some corrections for the deviative sensors
                if ( sensorType == ExtraSensorType.AirQualityAvg ) { tmp.PlotvarIndex += 4; tmp.PlotvarType = ChartsCompiler.PlotvarTypesEXTRA[ tmp.PlotvarIndex ]; }
                if ( sensorType == ExtraSensorType.Lightning ) { RTindex += 3; } // Compensate for 

                // And immediately set the name as default in the Language file so the chart legend becomes comprehensible
                // and does not require additional user action
                _ = Sup.GetCUstringValue( "Compiler", tmp.PlotvarType, thisSensor, false );

                ExtraSensorList.Add( tmp );
            }
        } // End InitialiseExtraSensorList

        #endregion

        #region SensorCommunity Iframe

        public void CreateSensorCommunityMapIframeFile()
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
