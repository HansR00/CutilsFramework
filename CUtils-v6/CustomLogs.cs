/*
 * CustomLogs - Part of CumulusUtils
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
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web.ModelBinding;
using static CumulusUtils.ExtraSensors;

namespace CumulusUtils
{
    internal class CustomLogs
    {
        internal int[] Frequencies = { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60 };
        internal struct CustomLog
        {
            internal string Name;
            internal string Content;
            internal int Frequency;  // in minutes
            internal List<string> TagsRaw;
            internal List<string> TagNames;
        }

        internal WebtagInfo WebTags { get; set; }
        internal List<CustomLog> CustomLogsList = new();

        readonly CuSupport Sup;

        #region Constructor
        internal CustomLogs( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "CustomLogs constructor: start" );

            WebTags = new WebtagInfo();

            // Fetch the Interval CustomLogs Content
            for ( int i = 0; ; i++ )
            {
                CustomLog tmp;
                string IntervalEnabled;
                
                IntervalEnabled = Sup.GetCumulusIniValue( "CustomLogs", $"IntervalEnabled{i}", "" );

                if ( IntervalEnabled.Equals( "1" ) )
                {
                    tmp = new()
                    {
                        // Create entry in the list
                        Name = Sup.GetCumulusIniValue( "CustomLogs", $"IntervalFilename{i}", "" ),
                        Content = Sup.GetCumulusIniValue( "CustomLogs", $"IntervalContent{i}", "" ),
                        Frequency = Frequencies[ Convert.ToInt32( Sup.GetCumulusIniValue( "CustomLogs", $"IntervalIdx{i}", "" ) ) ],
                        TagsRaw = new List<string>(),
                        TagNames = new List<string>()
                    };

                    CustomLogsList.Add( tmp );
                }
                else if ( IntervalEnabled.Equals( "0" ) ) continue;     // take next entry (if it exists)
                else break;                                             // No more Interval Custom Logs, the Enabled value must  be empty
            }

            // Fetch the Daily CustomLogs Content
            for ( int i = 0; ; i++ )
            {
                CustomLog tmp;
                string DailyEnabled = Sup.GetCumulusIniValue( "CustomLogs", $"DailyEnabled{i}", "" );

                if ( DailyEnabled.Equals( "1" ) )
                {
                    tmp = new()
                    {
                        // Create entry in the list
                        Name = Sup.GetCumulusIniValue( "CustomLogs", $"DailyFilename{i}", "" ),
                        Content = Sup.GetCumulusIniValue( "CustomLogs", $"DailyContent{i}", "" ),
                        Frequency = -1,      // Indicates a Daily log
                        TagsRaw = new List<string>(),
                        TagNames = new List<string>()
                    };

                    CustomLogsList.Add( tmp );
                }
                else if ( DailyEnabled.Equals( "0" ) ) continue;    // take next entry (if it exists)
                else break;                                         // No more Daily Custom Logs, the Enabled value must  be empty
            }

            // Split the content lines into a list of full Webtags specifications (inclusing their modifiers)
            foreach ( CustomLog thisLog in CustomLogsList )
            {
                int CurrentIndex = 0;

                do
                {
                    string tmp = WebTags.FetchWebtagRaw( thisLog.Content, ref CurrentIndex );

                    if ( !string.IsNullOrEmpty( tmp ) )
                    {
                        Sup.LogTraceInfoMessage( $"Constructor CustomLogs: handling Custom log {thisLog.Name} - Webtag {tmp}" );

                        thisLog.TagsRaw.Add( tmp );

                        string tmp2 = WebTags.FetchWebtagName( tmp );

                        if ( !string.IsNullOrEmpty( tmp2 ) )
                        {
                            if ( !WebTags.IsValidWebtag( tmp2 ) )
                            {
                                Sup.LogTraceWarningMessage( $"Constructor CustomLogs: Not a valid Webtag {tmp2} used in Custom log {thisLog.Name}" );
                                thisLog.TagsRaw.Remove( tmp );
                            }
                            else
                                thisLog.TagNames.Add( tmp2 );
                        }
                        else
                        {
                            Sup.LogTraceErrorMessage( $"Constructor CustomLogs: Serious error while getting the WebTagNames" );
                            break;
                        }
                    }
                    else break; // no (more) webtags found
                } while ( CurrentIndex < thisLog.Content.Length );
            }

            Sup.LogTraceInfoMessage( "Extra Sensors constructor: stop" );

            return;
        }

        #endregion

#region DoCustomLogs
        internal void DoCustomLogs()
        {
            Sup.LogDebugMessage( "DoCustomLogs - Start" );

            // I: create the extrasensorsrealtime.txt which has to be processed by CMX and transferred to the webroot.
            // 
            GenerateCustomLogsRealtime();
            GenerateCustomLogsCharts();
            GenerateCustomLogsModule();

            Sup.LogDebugMessage( "DoCustomLogs - Stop" );

            return;
        }
#endregion

#region GenerateCustomLogsModule

        internal void GenerateCustomLogsModule()
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsOutputFilename}", false, Encoding.UTF8 ) )
            {
                #region Javascript

                StringBuilder sb = new StringBuilder();

                Sup.LogDebugMessage( $"GenerateCustomLogsModule: Generating the Module Javascript code..." );

                // Generate the table on the basis of what is set in the inifile and setup in the constructor. 
                // We don't support live switching as we don't support that for anything.
                // Generate the start of the realtime script
                //

                sb.AppendLine( $"{Sup.GenjQueryIncludestring()}" );

                if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                {
                    sb.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                }

                sb.AppendLine( "<script>" );
                sb.AppendLine( "console.log('Module CustomLogsTimer ...');" );
                sb.AppendLine( "var CustomLogsTimer;" );
                sb.AppendLine( "$(function () {" );  // Get the whole thing going
                sb.AppendLine( $"  SetupCustomLogsTable();" );
                sb.AppendLine( $"  $('#Dashboard').hide();" );
                sb.AppendLine( $"  $('#Gauges').hide();" );
                sb.AppendLine( $"  $('#ExtraAndCustom').show();" );  //misuse the ExtraSensors place for this.
                sb.AppendLine( "  loadCustomLogsRealtime();" );
                sb.AppendLine( "  if (CustomLogsTimer == null) CustomLogsTimer = setInterval(loadCustomLogsRealtime, 60 * 1000);" );
                sb.AppendLine( $"  LoadUtilsReport( '{Sup.CustomLogsCharts}', false );" );
                sb.AppendLine( "});" );
                sb.AppendLine( "" );
                sb.AppendLine( "function loadCustomLogsRealtime() {" );
                sb.AppendLine( "  $.ajax({" );
                sb.AppendLine( $"    url: '{Sup.CustomLogsRealtimeFilename}'," );
                sb.AppendLine( "    timeout: 1000," );
                sb.AppendLine( "    cache:false," );
                sb.AppendLine( "    headers: { 'Access-Control-Allow-Origin': '*' }," );
                sb.AppendLine( "    crossDomain: true," );
                sb.AppendLine( "  })" +
                    ".done(function (response, responseStatus) {DoCustomLogsRT(response)})" +
                    ".fail(function(jqXHR, responseStatus){ console.log('ajax call: ' + responseStatus) });" );
                sb.AppendLine( "}" );

                sb.AppendLine( $"var oldobsExtra = [ {CustomLogsList.Count} ]; " );
                sb.AppendLine( "function DoCustomLogsRT(input) {" );
                sb.AppendLine( "  var CustomLogsRT = input.split(' ');" );

                int i = 0;

                foreach ( CustomLog tmp in CustomLogsList )
                {
                    foreach( string thisTag in tmp.TagNames)
                    {
                        sb.AppendLine( $"  if ( oldobsExtra[{i}] != ExtraSensorRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsExtra[{i}] = ExtraSensorRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCustomLogs{thisTag}').html(ExtraSensorRT[ {i} ] + ' {WebTags.GetTagUnit(thisTag)}');" );
                        sb.AppendLine( $"    $('#ajxCustomLogs{thisTag}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );
                    }

                    i++;
                }

                sb.AppendLine( "  setTimeout( 'CustomLogsClearChangeIndicator()', 3000 );" );
                sb.AppendLine( "}" ); // End DoCustomLogsRT

                sb.AppendLine( "function CustomLogsClearChangeIndicator(){" );
                sb.AppendLine( "  $( '[id*=\"ajx\"]' ).css( 'color', '' );" );
                sb.AppendLine( "}" );
                sb.AppendLine( "" );

                // Setup the HTML ExtraSensors table for the Dashboard area

                sb.AppendLine( "function SetupCustomLogsTable() {" );

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

                buf.Append( $"<style>.centerItem {{width: 80%; max-height: 80vh; margin: 6vh auto;overflow-y: auto; }}</style>" );
                buf.Append( $"<div class='centerItem' style='text-align:left;'><table style='width:100%'>" );
                buf.Append( $"<tr " +
                    $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                    $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
                buf.Append( $"<th {thisPadding()}>Sensor name</th><th>Value</th></tr>" );

                foreach ( CustomLog tmp in CustomLogsList )
                {
                    buf.Append( $"<tr {RowColour()}><td {thisPadding()}>{tmp.Name}:</td><td></td></tr>" );
                    foreach ( string thisTag in tmp.TagNames )
                    {
                        buf.Append( $"<tr {RowColour()} onclick='Do{thisTag}();'><td {thisPadding()}>{thisTag}</td><td id='ajxCustomLogs{thisTag}'></td></tr>" );
                    }
                }


                buf.Append( "</table></div>" );
                sb.AppendLine( $"  $('#ExtraAndCustom').html(\"{buf}\");" );
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

        #region GenerateCustomLogsRealtime

        internal void GenerateCustomLogsRealtime()
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsRealtimeFilename}", false, Encoding.UTF8 ) )
            {
                StringBuilder sb = new StringBuilder();

                Sup.LogDebugMessage( $"GenerateExtraSensorsRealtime: Writing the CustomLogs realtime file for the actual valid tags found" );

                foreach ( CustomLog tmp in CustomLogsList )
                {
                    foreach ( string Tag in tmp.TagsRaw )
                    {
                        sb.Append( $"{Tag} " );
                    }
                }

                Sup.LogDebugMessage( $"GenerateCustomLogsRealtime: {sb}" );

                of.Write( sb );
            }
        }

        #endregion

        #region GenerateCustomLogsCharts

        internal void GenerateCustomLogsCharts()
        {
            const string DemarcationLineCustomLogs = "; CustomLogsCharts";
            const string DemarcationLineExtraSensors = "; ExtraSensorCharts";

            bool OutputWritten = false;
            bool DemarcationLineFound = false;
            int i, j;

            string[] CutilsChartsIn, CutilsChartsOut;
            List<string> CutilsChartsMods;

            Sup.LogDebugMessage( $"GenerateCustomLogCharts: Generating the CustomLogs Charts CDL code into {Sup.PathUtils}{Sup.CutilsChartsDef}..." );

            if ( !File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                Sup.LogTraceErrorMessage( $"GenerateCustomLogsCharts: No {Sup.PathUtils}{Sup.CutilsChartsDef} present, can't modify" );
                Sup.LogTraceErrorMessage( $"GenerateCustomLogsharts: Please move {Sup.CutilsChartsDef} from distribution to ${Sup.PathUtils}" );
                return;
            }

            Sup.LogDebugMessage( $"GenerateCustomLogsCharts: Testing UserModificationCustomLogsCharts: {Sup.GetUtilsIniValue( "CustomLogs", "UserModificationCustomLogsCharts", "false" )}" );

            if ( Sup.GetUtilsIniValue( "CustomLogs", "UserModificationCustomLogsCharts", "false" ).Equals( "true", CUtils.cmp ) ) return;

            CutilsChartsIn = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" );

            for ( i = 0; i < CutilsChartsIn.Length; i++ )
            {
                if ( CutilsChartsIn[ i ].Contains( DemarcationLineCustomLogs ) )
                {
                    if ( i < CutilsChartsIn.Length - 1 )
                    {
                        for ( j = CutilsChartsIn.Length - 1; j > i; j-- )
                        {
                            CutilsChartsIn = CutilsChartsIn.RemoveAt( j );
                        }
                    }
                    DemarcationLineFound = true;
                }
            }

            CutilsChartsMods = CutilsChartsIn.ToList();
            if ( !DemarcationLineFound )
                CutilsChartsMods.Add( DemarcationLineCustomLogs );

            // Now the road is clear to add the charts from the list of plotparameters per class (Temp, Humidity etc....
            ExtraSensorType currentType;
            CutilsChartsMods.Add( "" );

            for ( i = 0; i < CustomLogsList.Count; )
            {
                //string thisKeyword;

                //CutilsChartsMods.Add( $"Chart Extra{CustomLogsList[ i ].Type} Title " +
                //    $"{Sup.GetCUstringValue( "ExtraSensors", "Trend chart of Extra", "Trend chart of Extra", true )} " +
                //    $"{CustomLogsList[ i ].Type} " +
                //    $"{Sup.GetCUstringValue( "ExtraSensors", "Sensors", "Sensors", true )}" );

                //while ( i < CustomLogsList.Count && ( CustomLogsList[ i ].Type == currentType || thisKeyword.Substring( 0, 3 ).Equals( "CO2", CUtils.cmp ) ) )
                //{
                //    if ( ExtraSensorList[ i ].Type == ExtraSensorType.External ) thisKeyword = ExtraSensorList[ i ].Name;
                //    else thisKeyword = ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ];

                //    Sup.LogTraceInfoMessage( $"GenerateExtraSensorsCharts: Adding Sensor: {thisKeyword}" );

                //    CutilsChartsMods.Add( $"  Plot Extra {thisKeyword}" );
                //    _ = Sup.GetCUstringValue( "Compiler", thisKeyword, ExtraSensorList[ i ].Name, false );

                //    i++;

                //    if ( currentType == ExtraSensorType.AirQuality ) // Then the next item must be AirQualityAvg (that's how it's constructed
                //    {
                //        CutilsChartsMods.Add( $"  Plot Extra {ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ]}" );
                //        Sup.GetCUstringValue( "Compiler", ChartsCompiler.PlotvarKeywordEXTRA[ ExtraSensorList[ i ].PlotvarIndex ], ExtraSensorList[ i ].Name, false );

                //        i++;
                //    }
                //}

                if ( !OutputWritten )
                {
                    CutilsChartsMods.Add( $"EndChart Output {Sup.ExtraSensorsCharts}" );
                    OutputWritten = true;
                }
                else
                    CutilsChartsMods.Add( $"EndChart" );

                CutilsChartsMods.Add( "" );
            }

            //Sup.LogDebugMessage( $"GenerateExtraSensorsCharts: Writing the CutilsCharts.def" );
            //File.WriteAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", CutilsChartsMods, Encoding.UTF8 );

            return;
        }

        #endregion

    } // Class CustomLogs

    #region WebtagInfo
    internal class WebtagInfo
    {
        internal WebtagInfo()
        {
            // Constructor
        }

        internal string GetTagUnit( string name )
        {
            int i = Array.FindIndex( Tagname, word => word.Equals( name, CUtils.cmp ) );
            return TagUnit[ i ];
        }

        internal bool IsValidWebtag( string name )
        {
            return Array.FindIndex( Tagname, word => word.Equals( name, CUtils.cmp ) ) != -1;
        }

        internal string FetchWebtagRaw( string s, ref int Start )
        {
            // returns the raw Webtag, or if no webtag found, returns null
            string tmp;
            Start = s.IndexOf( "<#", Start );

            if ( Start >= 0 )
            {
                int b = s.IndexOf( ">", Start );
                if ( b >= 0 ) { tmp = s.Substring( Start, b - Start + 1 ); Start = b + 1; }// startindex and length
                else tmp = null;
            }
            else tmp = null;

            return tmp;
        }

        // Fetch the name from a full webtag spec e.g. <#temp> => temp
        internal string FetchWebtagName( string s )
        {
            int a = s.IndexOf( ' ' );
            if ( a == -1 ) return s.Substring( 2, s.Length - 3 );
            else return s.Substring( 2, a - 2 );
        }
        static readonly string[] TagUnit = new string[] {
            
        };

        static readonly bool[] changeIndicator = new bool[] {
        };

        static readonly string[] Tagname = new string[] {
            "temp",
            "apptemp",
            "feelslike",
            "temprange",
            "heatindex",
            "avgtemp",
            "hum",
            "humidex",
            "press",
            "altimeterpressure",
            "cloudbase",
            "dew",
            "wetbulb",
            "presstrendval",
            "PressChangeLast3Hours",
            "windrun",
            "domwindbearing",
            "heatdegdays",
            "cooldegdays",
            "wlatest",
            "wspeed",
            "currentwdir",
            "wdir",
            "wgust",
            "windAvg",
            "wchill",
            "rrate",
            "bearing",
            "avgbearing",
            "BearingRangeFrom",
            "BearingRangeTo",
            "BearingRangeFrom10",
            "BearingRangeTo10",
            "beaufortnumber",
            "rfall",
            "ConsecutiveRainDays",
            "ConsecutiveDryDays",
            "rmidnight",
            "rmonth",
            "rhour",
            "r24hour",
            "ryear",
            "inhum",
            "intemp",
            "tempTH",
            "tempTL",
            "tempMidnightTH",
            "tempMidnightTL",
            "tempMidnightRangeT",
            "wchillTL",
            "apptempTH",
            "apptempTL",
            "feelslikeTH",
            "feelslikeTL",
            "humidexTH",
            "dewpointTH",
            "dewpointTL",
            "heatindexTH",
            "pressTH",
            "pressTL",
            "humTH",
            "humTL",
            "windTM",
            "wgustTM",
            "bearingTM",
            "rrateTM",
            "hourlyrainTH",
            "rain24hourTH",
            "solarTH",
            "UVTH",
            "tempH",
            "tempL",
            "apptempH",
            "apptempL",
            "feelslikeH",
            "feelslikeL",
            "humidexH",
            "dewpointH",
            "dewpointL",
            "heatindexH",
            "gustM",
            "wspeedH",
            "windrunH",
            "wchillH",
            "rrateM",
            "rfallH",
            "r24hourH",
            "rfallhH",
            "rfallmH",
            "pressH",
            "pressL",
            "humH",
            "humL",
            "mintempH",
            "maxtempL",
            "LongestDryPeriod",
            "LongestWetPeriod",
            "LowDailyTempRange",
            "HighDailyTempRange",
            "daylength",
            "daylightlength",
            "chillhours",
            "chillhoursToday",
            "MinutesSinceLastRainTip",
            "ET",
            "AnnualET",
            "UV",
            "SolarRad",
            "Light",
            "CurrentSolarMax",
            "SunshineHours",
            "SunshineHoursMonth",
            "SunshineHoursYear",
            "ExtraTemp1",
            "ExtraTemp2",
            "ExtraTemp3",
            "ExtraTemp4",
            "ExtraTemp5",
            "ExtraTemp6",
            "ExtraTemp7",
            "ExtraTemp8",
            "ExtraTemp9",
            "ExtraTemp10",
            "ExtraDP1",
            "ExtraDP2",
            "ExtraDP3",
            "ExtraDP4",
            "ExtraDP5",
            "ExtraDP6",
            "ExtraDP7",
            "ExtraDP8",
            "ExtraDP9",
            "ExtraDP10",
            "ExtraHum1",
            "ExtraHum2",
            "ExtraHum3",
            "ExtraHum4",
            "ExtraHum5",
            "ExtraHum6",
            "ExtraHum7",
            "ExtraHum8",
            "ExtraHum9",
            "ExtraHum10",
            "SoilTemp1",
            "SoilTemp2",
            "SoilTemp3",
            "SoilTemp4",
            "SoilTemp5",
            "SoilTemp6",
            "SoilTemp7",
            "SoilTemp8",
            "SoilTemp9",
            "SoilTemp10",
            "SoilTemp11",
            "SoilTemp12",
            "SoilTemp13",
            "SoilTemp14",
            "SoilTemp15",
            "SoilTemp16",
            "SoilMoisture1",
            "SoilMoisture2",
            "SoilMoisture3",
            "SoilMoisture4",
            "SoilMoisture5",
            "SoilMoisture6",
            "SoilMoisture7",
            "SoilMoisture8",
            "SoilMoisture9",
            "SoilMoisture10",
            "SoilMoisture11",
            "SoilMoisture12",
            "SoilMoisture13",
            "SoilMoisture14",
            "SoilMoisture15",
            "SoilMoisture16",
            "UserTemp1",
            "UserTemp2",
            "UserTemp3",
            "UserTemp4",
            "UserTemp5",
            "UserTemp6",
            "UserTemp7",
            "UserTemp8",
            "AirQuality1",
            "AirQuality2",
            "AirQuality3",
            "AirQuality4",
            "AirQualityAvg1",
            "AirQualityAvg2",
            "AirQualityAvg3",
            "AirQualityAvg4",
            "CO2",
            "CO2-24h",
            "CO2-pm2p5",
            "CO2-pm2p5-24h",
            "CO2-pm10",
            "CO2-pm10-24h",
            "CO2-temp",
            "CO2-hum",
            "LightningDistance",
            "LightningStrikesToday",
            "LeafTemp1",
            "LeafTemp2",
            "LeafTemp3",
            "LeafTemp4",
            "LeafWetness1",
            "LeafWetness2",
            "LeafWetness3",
            "LeafWetness4",
            "LeafWetness5",
            "LeafWetness6",
            "LeafWetness7",
            "LeafWetness8",
            "AirLinkTempIn",
            "AirLinkHumIn",
            "AirLinkPm1In",
            "AirLinkPm2p5In",
            "AirLinkPm2p5_1hrIn",
            "AirLinkPm2p5_3hrIn",
            "AirLinkPm2p5_24hrIn",
            "AirLinkPm2p5_NowcastIn",
            "AirLinkPm10In",
            "AirLinkPm10_1hrIn",
            "AirLinkPm10_3hrIn",
            "AirLinkPm10_24hrIn",
            "AirLinkPm10_NowcastIn",
            "AirLinkTempOut",
            "AirLinkHumOut",
            "AirLinkPm1Out",
            "AirLinkPm2p5Out",
            "AirLinkPm2p5_1hrOut",
            "AirLinkPm2p5_3hrOut",
            "AirLinkPm2p5_24hrOut",
            "AirLinkPm2p5_NowcastOut",
            "AirLinkPm10Out",
            "AirLinkPm10_1hrOut",
            "AirLinkPm10_3hrOut",
            "AirLinkPm10_24hrOut",
            "AirLinkPm10_NowcastOut",
            "AirLinkAqiPm2p5In",
            "AirLinkAqiPm2p5_1hrIn",
            "AirLinkAqiPm2p5_3hrIn",
            "AirLinkAqiPm2p5_24hrIn",
            "AirLinkAqiPm2p5_NowcastIn",
            "AirLinkAqiPm10In",
            "AirLinkAqiPm10_1hrIn",
            "AirLinkAqiPm10_3hrIn",
            "AirLinkAqiPm10_24hrIn",
            "AirLinkAqiPm10_NowcastIn",
            "AirLinkAqiPm2p5Out",
            "AirLinkAqiPm2p5_1hrOut",
            "AirLinkAqiPm2p5_3hrOut",
            "AirLinkAqiPm2p5_24hrOut",
            "AirLinkAqiPm2p5_NowcastOut",
            "AirLinkAqiPm10Out",
            "AirLinkAqiPm10_1hrOut",
            "AirLinkAqiPm10_3hrOut",
            "AirLinkAqiPm10_24hrOut",
            "AirLinkAqiPm10_NowcastOut",
            "AirLinkPct_1hrIn",
            "AirLinkPct_3hrIn",
            "AirLinkPct_24hrIn",
            "AirLinkPct_NowcastIn",
            "AirLinkPct_1hrOut",
            "AirLinkPct_3hrOut",
            "AirLinkPct_24hrOut",
            "AirLinkPct_NowcastOut",
            "MonthTempH",
            "MonthTempL",
            "MonthHeatIndexH",
            "MonthWChillL",
            "MonthAppTempH",
            "MonthAppTempL",
            "MonthFeelsLikeH",
            "MonthFeelsLikeL",
            "MonthHumidexH",
            "MonthMinTempH",
            "MonthMaxTempL",
            "MonthPressH",
            "MonthPressL",
            "MonthHumH",
            "MonthHumL",
            "MonthGustH",
            "MonthWindH",
            "MonthRainRateH",
            "MonthHourlyRainH",
            "MonthRain24HourH",
            "MonthDailyRainH",
            "MonthDewPointH",
            "MonthDewPointL",
            "MonthWindRunH",
            "MonthLongestDryPeriod",
            "MonthLongestWetPeriod",
            "MonthHighDailyTempRange",
            "MonthLowDailyTempRange",
            "YearTempH",
            "YearTempL",
            "YearHeatIndexH",
            "YearWChillL",
            "YearAppTempH",
            "YearAppTempL",
            "YearFeelsLikeH",
            "YearFeelsLikeL",
            "YearHumidexH",
            "YearMinTempH",
            "YearMaxTempL",
            "YearPressH",
            "YearPressL",
            "YearHumH",
            "YearHumL",
            "YearGustH",
            "YearWindH",
            "YearRainRateH",
            "YearHourlyRainH",
            "YearRain24HourH",
            "YearDailyRainH",
            "YearMonthlyRainH",
            "YearDewPointH",
            "YearDewPointL",
            "YearWindRunH",
            "YearLongestDryPeriod",
            "YearLongestWetPeriod",
            "YearHighDailyTempRange",
            "YearLowDailyTempRange",
            "MoonPercent",
            "MoonPercentAbs",
            "MoonAge",
            "DavisTotalPacketsReceived",
            "DavisTotalPacketsMissed",
            "DavisNumberOfResynchs",
            "DavisMaxInARow",
            "DavisNumCRCerrors",
            "DavisReceptionPercent",
            "ByMonthTempH",
            "ByMonthTempL",
            "ByMonthAppTempH",
            "ByMonthAppTempL",
            "ByMonthFeelsLikeH",
            "ByMonthFeelsLikeL",
            "ByMonthHumidexH",
            "ByMonthDewPointH",
            "ByMonthDewPointL",
            "ByMonthHeatIndexH",
            "ByMonthGustH",
            "ByMonthWindH",
            "ByMonthWindRunH",
            "ByMonthWChillL",
            "ByMonthRainRateH",
            "ByMonthDailyRainH",
            "ByMonthHourlyRainH",
            "ByMonthRain24HourH",
            "ByMonthMonthlyRainH",
            "ByMonthPressH",
            "ByMonthPressL",
            "ByMonthHumH",
            "ByMonthHumL",
            "ByMonthMinTempH",
            "ByMonthMaxTempL",
            "ByMonthLongestDryPeriod",
            "ByMonthLongestWetPeriod",
            "ByMonthLowDailyTempRange",
            "ByMonthHighDailyTempRange"
        };

    }

    #endregion

} // Namespace CumulusUtils
