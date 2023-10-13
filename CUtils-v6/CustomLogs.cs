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
using System.IO;
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    public class CustomLogs
    {
        public int[] Frequencies = { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60 };
        public struct CustomLog
        {
            public string Name;
            public string Content;
            public int Frequency;  // in minutes
            public List<string> TagsRaw;
            public List<string> TagNames;
        }

        public WebtagInfo WebTags { get; set; }
        public List<CustomLog> CustomLogsList = new();

        public int TotalNrOfCustomLogs = 0;
        public int TotalNrOfWebtags = 0;

        readonly CuSupport Sup;

        #region Constructor
        public CustomLogs( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "CustomLogs constructor: start" );

            WebTags = new WebtagInfo( Sup );

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
                    TotalNrOfCustomLogs += 1;
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
                    TotalNrOfCustomLogs += 1;
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

                TotalNrOfWebtags += thisLog.TagsRaw.Count;
            }

            Sup.LogTraceInfoMessage( "Custom Logs constructor: stop" );

            return;
        }

        #endregion

        #region DoCustomLogs
        public void DoCustomLogs()
        {
            Sup.LogDebugMessage( "DoCustomLogs - Start" );

            // I: create the customlogsrealtime.txt which has to be processed by CMX and transferred to the webroot.
            // II: Create the charts to be compiled by the chartscompiler
            // III: generate the  module which rules it all
            //
            GenerateCustomLogsRealtime();
            GenerateCustomLogsCharts();
            GenerateCustomLogsModule();

            Sup.LogDebugMessage( "DoCustomLogs - Stop" );

            return;
        }
        #endregion

        #region GenerateCustomLogsModule

        public void GenerateCustomLogsModule()
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsOutputFilename}", false, Encoding.UTF8 ) )
            {
                #region Javascript

                StringBuilder sb = new StringBuilder();
                string prefix;

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

                sb.AppendLine( $"var oldobsCustomLogs = [ {TotalNrOfWebtags} ]; " );
                sb.AppendLine( "function DoCustomLogsRT(input) {" );
                sb.AppendLine( "  var CustomLogsRT = input.split(' ');" );

                int i = 0;

                foreach ( CustomLog tmp in CustomLogsList )
                {
                    prefix = tmp.Frequency == -1 ? "Daily" : "";

                    foreach ( string thisTag in tmp.TagNames )
                    {
                        sb.AppendLine( $"  if ( oldobsCustomLogs[{i}] != CustomLogsRT[{i}]) {{" );
                        sb.AppendLine( $"    oldobsCustomLogs[{i}] = CustomLogsRT[{i}];" );
                        sb.AppendLine( $"    $('#ajxCustomLogs{prefix}{thisTag}').html(CustomLogsRT[ {i} ] + ' {WebTags.GetTagUnit( thisTag )}');" );
                        sb.AppendLine( $"    $('#ajxCustomLogs{prefix}{thisTag}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
                        sb.AppendLine( "  }" );

                        i++;
                    }
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
                buf.Append( $"<a class='centerItem' href='https://www.cumuluswiki.org/a/Full_list_of_Webtags' target='_blank'>Cumulus Webtags -  Full List</a><br/>" );
                buf.Append( $"<tr " +
                    $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                    $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
                buf.Append( $"<th {thisPadding()}>{Sup.GetCUstringValue( "CustomLogs", "WebtagName", "Webtag Name", false )}</th>" +
                    $"<th>{Sup.GetCUstringValue( "CustomLogs", "RecentValue", "RECENT Value", false )}</th></tr>" );

                bool RecentDone = false;
                prefix = "";

                foreach ( CustomLog tmp in CustomLogsList )
                {
                    if ( tmp.Frequency == -1 && !RecentDone )
                    {
                        // End the table and start a new one
                        buf.Append( "</table></div>" );

                        buf.Append( $"<div class='centerItem' style='text-align:left;'><table style='width:100%'>" );
                        buf.Append( $"<tr " +
                            $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                            $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
                        buf.Append( $"<th {thisPadding()}>{Sup.GetCUstringValue( "CustomLogs", "WebtagName", "Webtag Name", false )}</th>" +
                            $"<th>{Sup.GetCUstringValue( "CustomLogs", "DailyValue", "DAILY Value", false )}</th></tr>" );

                        RecentDone = true;
                        prefix = "Daily";
                    }

                    buf.Append( $"<tr {RowColour()}><td {thisPadding()}>{tmp.Name}:</td><td></td></tr>" );
                    foreach ( string thisTag in tmp.TagNames )
                    {
                        buf.Append( $"<tr {RowColour()} onclick='Do{thisTag}();'><td {thisPadding()}>&nbsp;&nbsp;{thisTag}</td><td id='ajxCustomLogs{prefix}{thisTag}'></td></tr>" );
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

        public void GenerateCustomLogsRealtime()
        {
            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsRealtimeFilename}", false, Encoding.UTF8 ) )
            {
                StringBuilder sb = new StringBuilder();

                Sup.LogDebugMessage( $"GenerateCustomLogsRealtime: Writing the CustomLogs realtime file for the actual valid tags found" );

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

            return;
        }

        #endregion

        #region GenerateCustomLogsCharts

        public void GenerateCustomLogsCharts()
        {
            bool OutputWritten = false;
            int i;

            string[] CutilsChartsIn;
            List<string> CutilsChartsMods;

            Sup.LogDebugMessage( $"GenerateCustomLogCharts: Generating the CustomLogs Charts CDL code into {Sup.PathUtils}{Sup.CutilsChartsDef}..." );

            if ( !File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                Sup.LogTraceErrorMessage( $"GenerateCustomLogsCharts: No {Sup.PathUtils}{Sup.CutilsChartsDef} present, can't modify" );
                Sup.LogTraceErrorMessage( $"GenerateCustomLogsharts: Please move {Sup.CutilsChartsDef} from distribution to ${Sup.PathUtils}" );
                return;
            }

            Sup.LogDebugMessage( $"GenerateCustomLogsCharts: Testing UserModificationCustomLogsCharts: {Sup.GetUtilsIniValue( "CustomLogs", "UserModificationCustomLogsCharts", "false" )}" );

            if ( Sup.GetUtilsIniValue( "CustomLogs", "UserModificationCustomLogsCharts", "false" ).Equals( "true", CUtils.Cmp ) ) return;

            CutilsChartsIn = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" );

            for ( i = 0; i < CutilsChartsIn.Length; i++ )
            {
                if ( CutilsChartsIn[ i ].Contains( Sup.DemarcationLineCustomLogs ) && i < CutilsChartsIn.Length )
                {
                    for ( ; i < CutilsChartsIn.Length && !CutilsChartsIn[ i ].Contains( Sup.DemarcationLineExtraSensors ); )
                        CutilsChartsIn = CutilsChartsIn.RemoveAt( i );

                    if ( i >= CutilsChartsIn.Length || CutilsChartsIn[ i ].Contains( Sup.DemarcationLineExtraSensors ) ) break;
                }
            }

            CutilsChartsMods = CutilsChartsIn.ToList();

            CutilsChartsMods.Add( Sup.DemarcationLineCustomLogs );
            CutilsChartsMods.Add( "" );

            foreach ( CustomLog thisLog in CustomLogsList )
            {
                CutilsChartsMods.Add( $"Chart {thisLog.Name} Title " +
                    $"{Sup.GetCUstringValue( "CustomLogs", "Trend chart of CustomLog", "Trend chart of CustomLog", true )} " +
                    $"{thisLog.Name} " );

                foreach ( string thisTagName in thisLog.TagNames )
                {
                    // Note: the webtag names have been added to the compiler in the declarations contructor
                    //       the names are formed {logname}{tagname} so the webtag can be used in more than one chart. 
                    //       For the same webtag with different modifiers currently two charts are required. Maybe in future the same webtag 
                    //       can be used more often with different modifiers in the same chart... wishlist.
                    if ( thisLog.Frequency == -1 ) // Meaning a DAILY log
                        CutilsChartsMods.Add( $"  PLOT DAILY {thisLog.Name}{thisTagName}" );
                    else
                        CutilsChartsMods.Add( $"  PLOT EXTRA {thisLog.Name}{thisTagName}" );

                    // Set the string for meaning and translation. The user will have to modify this after initial creation to make sense of the chart.
                    // 
                    string tmp = thisLog.Name + thisTagName;
                    //Sup.GetCUstringValue( "CustomLogs", tmp, tmp, false );
                }

                if ( !OutputWritten )
                {
                    CutilsChartsMods.Add( $"EndChart Output {Sup.CustomLogsCharts}" );
                    OutputWritten = true;
                }
                else
                    CutilsChartsMods.Add( $"EndChart" );

                CutilsChartsMods.Add( "" );
            }

            Sup.LogDebugMessage( $"GenerateCustomLogsCharts: Writing the CutilsCharts.def" );
            File.WriteAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", CutilsChartsMods, Encoding.UTF8 );

            return;
        }

        #endregion

        #region GenerateCustomLogsDataJson

        private struct CustomLogValue
        {
            public DateTime Date { get; set; }
            public List<double> Value { get; set; }
        }

        public void GenerateCustomLogsDataJson()
        {
            DateTime Now = DateTime.Now;
            Now = new DateTime( Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, 0 );
            DateTime timeEnd = Now.AddMinutes( -Now.Minute % Math.Max( CUtils.FTPIntervalInMinutes, CUtils.LogIntervalInMinutes ) );
            DateTime timeStart;

            if ( CUtils.Isup.IsIncrementalAllowed() )
            {
                try
                {
                    timeStart = DateTime.ParseExact( Sup.GetUtilsIniValue( "General", "LastUploadTime", "" ), "dd/MM/yy HH:mm", CUtils.Inv ).AddMinutes( 1 );
                }
                catch
                {
                    timeStart = timeEnd.AddHours( -CUtils.HoursInGraph );
                }

            }
            else
            {
                timeStart = timeEnd.AddHours( -CUtils.HoursInGraph );
            }

            Sup.LogTraceInfoMessage( $"CustomLogs GenerateCustomLogsDataJson: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            // Purpose is to create the JSON for the CustomLogs data and offering the possibility to do only that to accomodate the fact that
            // CMX does not (and probably will never) generate that JSON like it generates the temperature JSON for graphing.
            // CumulusUtils will only generate the CustomLogs JSON by issueing the command: "./utils/bin/cumulusutils.exe UserAskedData"

            StringBuilder sbRecent = new StringBuilder();
            StringBuilder sbDaily = new StringBuilder();
            StringBuilder sb = sbRecent;

            sbRecent.Append( "{" );
            sbDaily.Append( "{" );

            // Fill the json with the variables needed:
            // 1) Read the logfile belonging to one Custom Log and write the values in the list.
            // 2) Don't use more than the nr of hrs as defined in parameter [Graphs] / GraphHours
            // 3) prefix the webtag name with the table name: that is how they are known to the compiler
            // 4) the date/time format is dd-mm-yy;hh:mm; (the semicolons are CMX generated as are the formats).

            // Required for separate DAILY JSON files which need only be sent once per day
            _ = DateTime.TryParse( Sup.GetUtilsIniValue( "CustomLogs", "DoneToday", $"{Now.AddDays( -1 ):d}" ), out DateTime DoneToday );
            bool DoDailyAsWell =  DoneToday < DateTime.Today;
            Sup.LogTraceInfoMessage( $"CustomLogs GenerateCustomLogsDataJson: DoneToday = {DoneToday}... DoDailyAsWell = {DoDailyAsWell}" );

            List<CustomLogValue> thisList;

            foreach ( CustomLog thisLog in CustomLogsList )
            {
                if ( thisLog.Frequency == -1 )
                {
                    if ( DoDailyAsWell )
                    {
                        thisList = ReadDailyCustomLog( thisLog ); // Activate this if we use another JSON for the DAILY CustomLogs
                        sb = sbDaily;
                    }
                    else continue;
                }
                else
                {
                    thisList = ReadRecentCustomLog( thisLog, timeStart, timeEnd );
                    sb = sbRecent;
                }

                Sup.LogTraceInfoMessage( $"CustomLogs GenerateCustomLogsDataJson: Doing {thisLog.Name}" );

                for ( int i = 0; i < thisLog.TagNames.Count; i++ )
                {
                    sb.Append( $"\"{thisLog.Name}{thisLog.TagNames[ i ]}\":[" );

                    foreach ( CustomLogValue entry in thisList )
                        sb.Append( $"[{CuSupport.DateTimeToJS( entry.Date )},{entry.Value[ i ].ToString( "F1", CUtils.Inv )}]," );

                    sb.Remove( sb.Length - 1, 1 );
                    sb.Append( $"]," );
                }
            }

            sbRecent.Remove( sbRecent.Length - 1, 1 );
            sbRecent.Append( "}" );

            using ( StreamWriter thisJSON = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsRecentJSON}", false, Encoding.UTF8 ) )
            {
                thisJSON.WriteLine( sbRecent.ToString() );
            }

            if ( DoDailyAsWell )
            {
                sbDaily.Remove( sbDaily.Length - 1, 1 );
                sbDaily.Append( "}" );

                using ( StreamWriter thisJSON = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsDailyJSON}", false, Encoding.UTF8 ) )
                {
                    thisJSON.WriteLine( sbDaily.ToString() );
                }
            }

            Sup.SetUtilsIniValue( "CustomLogs", "DoneToday", $"{Now:s}" );

            return;
        }

        private List<CustomLogValue> ReadRecentCustomLog( CustomLog thisLog, DateTime Start, DateTime End )
        {
            Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: {thisLog.Name}" );

            bool PeriodComplete = false, NextFileTried = false;
            bool WarningWritten = false;

            string FilenamePostFix = Start.Date.ToString( "-yyyyMM" ) + ".txt";
            string DateTimeText, ValuesAsText;
            string[] ValuesAsTextArray;

            CustomLogValue tmp = new CustomLogValue();
            List<CustomLogValue> thisList = new List<CustomLogValue>();

            while ( !PeriodComplete )
            {
                string fullFilename = "data/" + thisLog.Name + FilenamePostFix;
                string copyFilename = "data/copy_" + thisLog.Name + FilenamePostFix;
                Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: {fullFilename} - Start: {Start} ; End: {End} ;" );

                if ( File.Exists( copyFilename ) ) File.Delete( copyFilename );
                File.Copy( fullFilename, copyFilename );

                string[] allLines = File.ReadAllLines( copyFilename );
                DetectSeparators( allLines[ 0 ] );

                for ( int i = 0; i < allLines.Length; i++ )
                {
                    // Set the separators correct and do the reading: / in the date and the . as decimal separator.
                    //
                    string thisLine = ChangeSeparators( allLines[ i ] );

                    //DateTimeText = allLines[ i ].Substring( 0, 14 ).Replace('-','/').Replace('.','/');

                    DateTimeText = thisLine.Substring( 0, 14 );
                    tmp.Date = DateTime.ParseExact( DateTimeText, "dd/MM/yy HH:mm", CUtils.Inv );
                    if ( tmp.Date < Start ) continue;

                    ValuesAsText = thisLine.Substring( 15 );
                    ValuesAsTextArray = ValuesAsText.Split( new char[] { ' ' } );
                    tmp.Value = new List<double>();

                    if ( thisLog.TagNames.Count() != ValuesAsTextArray.Count() )
                    {
                        if ( !WarningWritten )
                        {
                            Sup.LogTraceWarningMessage( $"CustomLogs : There are more/less webtags than values in the log on line {i}: {allLines[ i ]}" );
                            Sup.LogTraceWarningMessage( $"CustomLogs : The chart may not be what you want, please correct the content of the datafile. Continuing..." );
                            WarningWritten = true;
                        }

                        continue;
                    }

                    for ( int j = 0; j < ValuesAsTextArray.Length; j++ )
                    {
                        try
                        {
                            tmp.Value.Add( Convert.ToDouble( ValuesAsTextArray[ j ], CUtils.Inv ) );
                        }
                        catch ( Exception e )
                        {
                            Sup.LogTraceErrorMessage( $"CustomLogs ReadRecentCustomLog: Cannot parse value in CustomLog {fullFilename}" );
                            Sup.LogTraceErrorMessage( $"CustomLogs ReadRecentCustomLog: {e.Message}" );
                            break;
                        }
                    }

                    thisList.Add( tmp );
                }

                // handle a possible file boundary
                if ( File.Exists( copyFilename ) ) File.Delete( copyFilename );

                Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: Deciding: tmp.Date: {tmp.Date} ; End: {End} ; thisList.Last: {thisList.Last().Date}" );

                if ( tmp.Date >= End || NextFileTried )
                {
                    Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: Finished reading the log at {tmp.Date}" );
                    PeriodComplete = true;
                }
                else
                {
                    NextFileTried = true;

                    FilenamePostFix = Start.Date.AddMonths( 1 ).ToString( "-YYYYMM" ) + ".txt";
                    Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: Require the  next logfile: {thisLog.Name}" );

                    if ( !File.Exists( "data/" + thisLog.Name + FilenamePostFix ) )
                    {
                        Sup.LogTraceErrorMessage( $"CustomLogs ReadRecentCustomLog: Require {thisLog.Name} to continue but it does not exist, continuing with next CustomsLog" );
                        PeriodComplete = true;
                    }
                }
            }

            return thisList;
        }

        private List<CustomLogValue> ReadDailyCustomLog( CustomLog thisLog )
        {
            bool WarningWritten = false;

            string FilenamePostFix = ".txt";
            string DateTimeText, ValuesAsText;
            string[] ValuesAsTextArray;

            CustomLogValue tmp = new CustomLogValue();

            List<CustomLogValue> thisList = new List<CustomLogValue>();

            string fullFilename = "data/" + thisLog.Name + FilenamePostFix;
            string copyFilename = "data/copy_" + thisLog.Name + FilenamePostFix;
            Sup.LogTraceInfoMessage( $"CustomLogs ReadDailyLog: {fullFilename}" );

            if ( File.Exists( copyFilename ) ) File.Delete( copyFilename );
            File.Copy( fullFilename, copyFilename );

            string[] allLines = File.ReadAllLines( copyFilename );
            DetectSeparators( allLines[ 0 ] );

            for ( int i = 0; i < allLines.Length; i++ )
            {
                string thisLine = ChangeSeparators( allLines[ i ] );

                DateTimeText = thisLine.Substring( 0, 8 );
                tmp.Date = DateTime.ParseExact( DateTimeText, "dd/MM/yy", CUtils.Inv );

                ValuesAsText = thisLine.Substring( 9 );
                ValuesAsTextArray = ValuesAsText.Split( new char[] { ' ' } );
                tmp.Value = new List<double>();

                if ( thisLog.TagNames.Count() != ValuesAsTextArray.Count() )
                {
                    if ( !WarningWritten )
                    {
                        Sup.LogTraceWarningMessage( $"CustomLogs : There are more/less webtags than values in the log on line {i}: {allLines[ i ]}" );
                        Sup.LogTraceWarningMessage( $"CustomLogs : The chart may not be what you want, please correct the content of the datafile. Continuing..." );
                        WarningWritten = true;
                    }

                    continue;
                }

                for ( int j = 0; j < ValuesAsTextArray.Length; j++ )
                {
                    try
                    {
                        tmp.Value.Add( Convert.ToDouble( ValuesAsTextArray[ j ], CUtils.Inv ) ); // Use the local separator assuming it is the same as for CMX in writing
                    }
                    catch ( Exception e )
                    {
                        Sup.LogTraceErrorMessage( $"CustomLogs GenerateCustomLogsDataJson: Cannot parse value in CustomLog {fullFilename}" );
                        Sup.LogTraceErrorMessage( $"CustomLogs GenerateCustomLogsDataJson: {e.Message}" );
                        break;
                    }
                }

                thisList.Add( tmp );
            }

            return thisList;
        }
        #endregion

        #region Separator handling

        char DateSeparator;
        char FieldSeparator;
        char DecimalSeparator;

        private void DetectSeparators(string line)
        {
            if ( line[ 2 ] == '-' && line[ 8 ] == ';' )
            {
                DateSeparator = '-';
                FieldSeparator = ';';
                DecimalSeparator = ',';
            }
            else if ( line[ 2 ] == '/' && line[ 8 ] == ';' )
            {
                DateSeparator = '/';
                FieldSeparator = ';';
                DecimalSeparator = ',';
            }
            else if ( line[ 2 ] == '.' && line[ 8 ] == ';' )
            {
                DateSeparator = '.';
                FieldSeparator = ';';
                DecimalSeparator = ',';
            }
            else if ( line[ 2 ] == '-' && line[ 8 ] == ',' )
            {
                DateSeparator = '-';
                FieldSeparator = ',';
                DecimalSeparator = '.';
            }
            else if ( line[ 2 ] == '/' && line[ 8 ] == ',' )
            {
                DateSeparator = '/';
                FieldSeparator = ',';
                DecimalSeparator = '.';
            }
            else
            {
                Sup.LogTraceErrorMessage( "CustomLogs Logreaders: Internal Error - Unkown format of inputfile. Please get help." );
                Environment.Exit( 0 );
            }

            return;
        }

        private string ChangeSeparators( string line ) 
        {
            string thisLine;

            thisLine = line.Substring(0,8).Replace( DateSeparator, '/') ;
            thisLine = thisLine + line.Substring(8).Replace( FieldSeparator, ' ' ).Replace( DecimalSeparator, '.' );
            thisLine = CuSupport.StringRemoveWhiteSpace( thisLine, " " );

            return thisLine;
        }

        #endregion

    } // Class CustomLogs

    #region WebtagInfo
    public class WebtagInfo
    {
        CuSupport Sup;

        public WebtagInfo(CuSupport s)
        {
            // Constructor
            Sup = s;

            TagUnit = new string[] 
            {
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),    // Index 5 (no 6)

            };

            TagAxis = new AxisType []
            {
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,             // Index 5 (no 6)

            };

        }


        public string GetTagUnit( string name )
        {
            //int i = Array.FindIndex( Tagname, word => word.Equals( name, CUtils.Cmp ) );
            //return TagUnit[ i ];

            return "";
        }

        public string GetTagAxis( string name )
        {
            //return TagAxis[ i ];

            return "";
        }

        public bool IsValidWebtag( string name )
        {
            return Array.FindIndex( Tagname, word => word.Equals( name, CUtils.Cmp ) ) != -1;
        }

        public string FetchWebtagRaw( string s, ref int Start )
        {
            // returns the raw Webtag, or if no webtag found, returns null
            string tmp;
            Start = s.IndexOf( "<#", Start );

            if ( Start >= 0 )
            {
                int b = s.IndexOf( ">", Start );
                if ( b > Start ) { tmp = s.Substring( Start, b - Start + 1 ); Start = b + 1; }// startindex and length
                else tmp = null;
            }
            else tmp = null;

            return tmp;
        }

        // Fetch the name from a full webtag spec e.g. <#temp> => temp
        public string FetchWebtagName( string s )
        {
            int a = s.IndexOf( ' ' );
            if ( a == -1 ) return s.Substring( 2, s.Length - 3 );
            else return s.Substring( 2, a - 2 );
        }

        public readonly string[] TagUnit;

        public readonly AxisType[] TagAxis;

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
