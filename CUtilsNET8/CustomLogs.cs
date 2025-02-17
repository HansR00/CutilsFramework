/*
 * CustomLogs - Part of CumulusUtils
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

        private readonly int TotalNrOfWebtags = 0;

        readonly CuSupport Sup;

        #region Constructor
        public CustomLogs( CuSupport s )
        {
            string Excluded;
            string[] ExcludedCustomLogs = Array.Empty<string>();

            Sup = s;

            Sup.LogTraceInfoMessage( "CustomLogs constructor: starting" );

            Excluded = Sup.GetUtilsIniValue( "CustomLogs", "ExcludedCustomLogs", "" );
            if ( Excluded.Length > 0 ) ExcludedCustomLogs = Excluded.Split( GlobConst.CommaSeparator );

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
                        TagNames = new List<string>(),
                    };

                    if ( Array.Exists( ExcludedCustomLogs, word => word.Equals( tmp.Name, CUtils.Cmp ) ) ) continue;

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
                        TagNames = new List<string>(),
                    };

                    if ( Array.Exists( ExcludedCustomLogs, word => word.Equals( tmp.Name, CUtils.Cmp ) ) ) continue;

                    CustomLogsList.Add( tmp );
                }
                else if ( DailyEnabled.Equals( "0" ) ) continue;    // take next entry (if it exists)
                else break;                                         // No more Daily Custom Logs, the Enabled value must  be empty
            }

            // Split the content lines into a list of full Webtags specifications (including their modifiers)
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
            Sup.LogDebugMessage( "DoCustomLogs - Starting" );

            GenerateCustomLogsRealtime();
            GenerateCustomLogsCharts();
            GenerateCustomLogsModule();

            GenerateCustomLogsDataJson( NonIncremental: true );

            Sup.LogTraceInfoMessage( "DoCustomLogs - Stop" );

            return;
        }

        #endregion

        #region GenerateCustomLogsModule

        public void GenerateCustomLogsModule()
        {
            StringBuilder sb = new StringBuilder();

            Sup.LogDebugMessage( $"GenerateCustomLogsModule: Generating the Module Javascript code..." );

            sb.AppendLine( $"{CuSupport.GenjQueryIncludestring()}" );

            if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes ) sb.AppendLine( Sup.GenHighchartsIncludes().ToString() );

            sb.AppendLine( "<script>" );
            sb.AppendLine( "console.log('Module CustomLogsTimer ...');" );
            sb.AppendLine( "var CustomLogsTimer;" );
            sb.AppendLine( "$(function () {" );  // Get the whole thing going
            sb.AppendLine( $"  SetupCustomLogsTable();" );
            sb.AppendLine( "  loadCustomLogsRealtime();" );
            sb.AppendLine( "  if (CustomLogsTimer == null) CustomLogsTimer = setInterval(loadCustomLogsRealtime, 60 * 1000);" );
            sb.AppendLine( $"  LoadUtilsReport( '{Sup.CustomLogsCharts}', true );" );
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
                foreach ( string thisName in tmp.TagNames )
                {
                    sb.AppendLine( $"  if ( oldobsCustomLogs[{i}] != CustomLogsRT[{i}]) {{" );
                    sb.AppendLine( $"    oldobsCustomLogs[{i}] = CustomLogsRT[{i}];" );
                    sb.AppendLine( $"    $('#ajxCustomLogs{tmp.Name}{thisName}').html(CustomLogsRT[ {i} ] + ' {WebTags.GetTagUnit( thisName )}');" );
                    sb.AppendLine( $"    $('#ajxCustomLogs{tmp.Name}{thisName}').css('color', '{Sup.GetUtilsIniValue( "Website", "ColorDashboardTextAccent", "Chartreuse" )}');" );
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

            buf.Append( $"<style>.centerItem {{width: 90%; max-height: 67vh; margin: 6vh auto;overflow-y: auto; }}</style>" );

            buf.Append( $"<div id='RecentCustomLogs' class='slideOptions centerItem' style='text-align:left;'>" );
            buf.Append( $"<table style='width:100%'><tr " +
                $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
            buf.Append( $"<th {thisPadding()}>{Sup.GetCUstringValue( "CustomLogs", "WebtagName", "Webtag Name", false )}&nbsp&nbsp" +
                $"<a href='https://www.cumuluswiki.org/a/Full_list_of_Webtags' target='_blank'>(Cumulus Webtags -  Full List)</a></th>" +
                $"<th>{Sup.GetCUstringValue( "CustomLogs", "RecentValue", "RECENT Value", false )}</th></tr>" );

            bool RecentDone = false;

            foreach ( CustomLog tmp in CustomLogsList )
            {
                if ( tmp.Frequency == -1 && !RecentDone )
                {
                    // End the table and start a new one
                    buf.Append( "</table></div>" );

                    buf.Append( $"<div id='DailyCustomLogs' class='slideOptions centerItem' style='text-align:left;'><table style='width:100%'>" );
                    buf.Append( $"<tr " +
                        $"style='background-color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarBackground", "#C5C55B" )}; " +
                        $"color: {Sup.GetUtilsIniValue( "Website", "ColorDashboardCellTitleBarText", "White" )}; width:100%'>" );
                    buf.Append( $"<th {thisPadding()}>{Sup.GetCUstringValue( "CustomLogs", "WebtagName", "Webtag Name", false )}&nbsp&nbsp" +
                        $"<a href='https://www.cumuluswiki.org/a/Full_list_of_Webtags' target='_blank'>(Cumulus Webtags -  Full List)</a></th>" +
                        $"<th>{Sup.GetCUstringValue( "CustomLogs", "DailyValue", "DAILY Value", false )}</th></tr>" );

                    RecentDone = true;
                }

                buf.Append( $"<tr {RowColour()}><td {thisPadding()}><strong>{tmp.Name}:</strong></td><td></td></tr>" );

                for ( int c = 0; c < tmp.TagNames.Count; c++ )
                {
                    //buf.Append( $"<tr {RowColour()}><td {thisPadding()}>&nbsp;&nbsp;{tmp.TagsRaw[ c ]}</td>" +
                    buf.Append( $"<tr {RowColour()}><td {thisPadding()}>&nbsp;&nbsp;{tmp.TagNames[ c ]}</td>" +
                        $"<td id='ajxCustomLogs{tmp.Name}{tmp.TagNames[ c ]}'></td></tr>" );
                }
            }

            buf.Append( "</table></div>" );
            sb.AppendLine( $"  $('#ExtraAndCustom').html(\"{buf}\");" );
            sb.AppendLine( "}" );
            sb.AppendLine( "</script>" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

#if !RELEASE
                of.WriteLine( sb );
#else
                of.WriteLine( CuSupport.StringRemoveWhiteSpace( sb.ToString(), " " ) );
#endif

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

                Sup.LogTraceInfoMessage( $"GenerateCustomLogsRealtime: {sb}" );

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

            Sup.LogTraceInfoMessage( $"GenerateCustomLogsCharts: Testing UserModificationCustomLogsCharts: {Sup.GetUtilsIniValue( "CustomLogs", "UserModificationCustomLogsCharts", "false" )}" );

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
                    // Note: the webtag names will be added to the compiler in the declarations contructor
                    //       the names are formed {logname}{tagname} so the webtag can be used in more than one chart. 
                    //       For the same webtag with different modifiers currently two charts are required. Maybe in future the same webtag 
                    //       can be used more often with different modifiers in the same chart... wishlist.
                    if ( thisLog.Frequency == -1 ) // Meaning a DAILY log
                        CutilsChartsMods.Add( $"  PLOT ALL {thisLog.Name}{thisTagName}" );
                    else
                        CutilsChartsMods.Add( $"  PLOT EXTRA {thisLog.Name}{thisTagName}" );
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

            Sup.LogTraceInfoMessage( "GenerateCustomLogsCharts: Writing the CutilsCharts.def" );
            File.WriteAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", CutilsChartsMods, Encoding.UTF8 );

            return;
        }

        #endregion

        #region GenerateCustomLogsDataJson

        private struct CustomLogValue
        {
            public DateTime Date { get; set; }
            public List<double?> Value { get; set; }
        }

        public void GenerateCustomLogsDataJson( bool NonIncremental )
        {
            bool DoDailyAsWell;
            DateTime DoneToday;

            Sup.SetStartAndEndForData( out DateTime timeStart, out DateTime timeEnd );

            if ( NonIncremental )
            {
                DoneToday = DateTime.Now.AddDays( -1 );
                timeStart = timeEnd.AddHours( -CUtils.HoursInGraph );
            }
            else
                _ = DateTime.TryParse( Sup.GetUtilsIniValue( "CustomLogs", "DoneToday", $"{DateTime.Now.AddDays( -1 ):d}" ), out DoneToday );

            Sup.LogTraceInfoMessage( $"CustomLogs GenerateCustomLogsDataJson: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            // Required for separate DAILY JSON files which need only be sent once per day
            DoDailyAsWell = DoneToday < DateTime.Today;
            Sup.LogTraceInfoMessage( $"CustomLogs GenerateCustomLogsDataJson: DoneToday = {DoneToday}... DoDailyAsWell = {DoDailyAsWell}" );

            // Purpose is to create the JSON for the CustomLogs data and offering the possibility to do only that to accomodate the fact that
            // CMX does not (and probably will never) generate that JSON like it generates the temperature JSON for graphing.
            // CumulusUtils will only generate the CustomLogs JSON by issueing the command: "./utils/bin/cumulusutils.exe UserAskedData"

            StringBuilder sbRecent = new StringBuilder();
            StringBuilder sbDaily = new StringBuilder();
            StringBuilder sb;

            sbRecent.Append( "{ " );
            sbDaily.Append( "{ " );

            // Fill the json with the variables needed:
            // 1) Read the logfile belonging to one Custom Log and write the values in the list.
            // 2) Don't use more than the nr of hrs as defined in parameter [Graphs] / GraphHours
            // 3) prefix the webtag name with the table name: that is how they are known to the compiler
            // 4) the date/time format is dd-mm-yy;hh:mm; (the semicolons are CMX generated as are the formats).

            List<CustomLogValue> thisList;

            foreach ( CustomLog thisLog in CustomLogsList )
            {
                if ( thisLog.Frequency == -1 )
                {
                    if ( DoDailyAsWell )
                    {
                        try
                        {
                            thisList = ReadDailyCustomLog( thisLog ); // we use another JSON for the DAILY CustomLogs
                            sb = sbDaily;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    else continue;
                }
                else
                {
                    try
                    {
                        thisList = ReadRecentCustomLog( thisLog, timeStart, timeEnd );
                        sb = sbRecent;
                    }
                    catch
                    {
                        continue;
                    }
                }

                if ( thisList is not null )
                {
                    for ( int i = 0; i < thisLog.TagNames.Count; i++ )
                    {
                        sb.Append( $"\"{thisLog.Name}{thisLog.TagNames[ i ]}\":[" );

                        foreach ( CustomLogValue entry in thisList )
                        {
                            try
                            {
                                if ( entry.Value[ i ] is not null ) sb.Append( $"[{CuSupport.DateTimeToJS( entry.Date )},{entry.Value[ i ]?.ToString( "F2", CUtils.Inv )}]," );
                                else sb.Append( $"[{CuSupport.DateTimeToJS( entry.Date )}, null]," );
                            }
                            catch { continue; }
                        }

                        sb.Remove( sb.Length - 1, 1 );
                        sb.Append( $"]," );
                    }
                }
                else return; // In case no CustomLog is found but the module is activated anyway

            }

            sbRecent.Remove( sbRecent.Length - 1, 1 );
            sbRecent.Append( '}' );

            using ( StreamWriter thisJSON = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsRecentJSON}", false, Encoding.UTF8 ) )
            {
                thisJSON.WriteLine( sbRecent.ToString() );
            }

            if ( DoDailyAsWell )
            {
                sbDaily.Remove( sbDaily.Length - 1, 1 );
                sbDaily.Append( '}' );

                using ( StreamWriter thisJSON = new StreamWriter( $"{Sup.PathUtils}{Sup.CustomLogsDailyJSON}", false, Encoding.UTF8 ) )
                {
                    thisJSON.WriteLine( sbDaily.ToString() );
                }
            }

            Sup.SetUtilsIniValue( "CustomLogs", "DoneToday", $"{DateTime.Now:s}" );

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
                string fullFilename;
                string copyFilename;

                fullFilename = "data/" + thisLog.Name + FilenamePostFix;
                copyFilename = "data/copy_" + thisLog.Name + FilenamePostFix;

                Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: {fullFilename} - Start: {Start} ; End: {End} ;" );

                if ( File.Exists( copyFilename ) ) File.Delete( copyFilename );
                File.Copy( fullFilename, copyFilename );

                string[] allLines = File.ReadAllLines( copyFilename );
                File.Delete( copyFilename );

                foreach ( string line in allLines )
                {
                    DateTimeText = line.Substring( 0, 14 );
                    tmp.Date = DateTime.ParseExact( DateTimeText, "dd/MM/yy,HH:mm", CUtils.Inv );

                    if ( tmp.Date < Start ) continue;

                    ValuesAsText = line.Substring( 15 );
                    ValuesAsTextArray = ValuesAsText.Split( GlobConst.CommaSeparator );
                    tmp.Value = new List<double?>();

                    if ( thisLog.TagNames.Count != ValuesAsTextArray.Length )
                    {
                        if ( !WarningWritten )
                        {
                            Sup.LogTraceWarningMessage( $"CustomLogs : There are more/less webtags than values in the log {fullFilename}" );
                            Sup.LogTraceWarningMessage( $"CustomLogs : The chart may not be what you want, please correct the content of the datafile. Continuing..." );
                            WarningWritten = true;
                        }
                    }

                    foreach ( string thisValue in ValuesAsTextArray ) 
                    {
                        if ( !string.IsNullOrEmpty( thisValue ) )
                        {
                            try { tmp.Value.Add( Convert.ToDouble( thisValue, CUtils.Inv ) ); }
                            catch
                            {
                                Sup.LogTraceWarningMessage( $"CustomLogs ReadRecentCustomLog for {thisLog.Name}: Field Invalid value: {thisValue}, continuing" );
                                tmp.Value.Add( null );
                            }
                        }
                        else
                            tmp.Value.Add( null );
                    }

                    thisList.Add( tmp );
                }

                Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: Deciding: tmp.Date: {tmp.Date} ; End: {End} ; thisList.Last: {thisList.Last().Date}" );

                // handle a possible file boundary crossing
                if ( tmp.Date >= End || NextFileTried )
                {
                    Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: Finished reading the log at {tmp.Date}" );
                    PeriodComplete = true;
                }
                else
                {
                    NextFileTried = true;

                    FilenamePostFix = Start.Date.AddMonths( 1 ).ToString( "-yyyyMM" ) + ".txt";
                    fullFilename = $"data/{thisLog.Name}{FilenamePostFix}";
                    Sup.LogTraceInfoMessage( $"CustomLogs ReadRecentCustomLog: Require the  next logfile: {fullFilename}" );

                    if ( !File.Exists( fullFilename ) )
                    {
                        if ( CUtils.FTPIntervalInMinutes % Frequencies[ thisLog.Frequency ] != 0 )
                        {
                            Sup.LogTraceWarningMessage( $"CustomLogs ReadRecentCustomLog {thisLog.Name}: Log Frequency {thisLog.Frequency} Min. is larger or not In Sync with Internet Interval of {CUtils.FTPIntervalInMinutes} Min." );
                            Sup.LogTraceWarningMessage( $"CustomLogs ReadRecentCustomLog: {fullFilename} does not exist and most likely is not required." );
                        }
                        else
                            Sup.LogTraceErrorMessage( $"CustomLogs ReadRecentCustomLog: Require {fullFilename} to continue but it does not exist, continuing with next CustomsLog" );

                        PeriodComplete = true;
                    }
                }
            }

            return thisList;
        }

        private List<CustomLogValue> ReadDailyCustomLog( CustomLog thisLog )
        {
            bool LogWarningWritten = false;

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
            File.Delete( copyFilename );

            foreach ( string line in allLines )
            {
                DateTimeText = line.Substring( 0, 8 );
                tmp.Date = DateTime.ParseExact( DateTimeText, "dd/MM/yy", CUtils.Inv );

                ValuesAsText = line.Substring( 9 );
                ValuesAsTextArray = ValuesAsText.Split( GlobConst.CommaSeparator );
                tmp.Value = new List<double?>();

                if ( thisLog.TagNames.Count != ValuesAsTextArray.Length )
                {
                    if ( !LogWarningWritten )
                    {
                        Sup.LogTraceWarningMessage( $"CustomLogs : There are more/less webtags than values in the log {thisLog.Name} " +
                            $"=> defined: {thisLog.TagNames.Count} and found {ValuesAsTextArray.Length}" );
                        Sup.LogTraceWarningMessage( $"CustomLogs : The chart may not be what you want, please correct the content of the datafile. Continuing..." );
                        LogWarningWritten = true;
                    }
                }

                foreach ( string thisValue in ValuesAsTextArray )
                {
                    if ( !string.IsNullOrEmpty( thisValue ) )
                    {
                        try { tmp.Value.Add( Convert.ToDouble( thisValue, CUtils.Inv ) ); }
                        catch 
                        {
                            Sup.LogTraceWarningMessage( $"CustomLogs ReadDailyCustomLog for {thisLog.Name}: Field Invalid value: {thisValue} , continuing" );
                            tmp.Value.Add( null ); 
                        }
                    }
                    else
                        tmp.Value.Add( null );
                }

                thisList.Add( tmp );
            }

            return thisList;
        }

        #endregion

    } // Class CustomLogs

    #region WebtagInfo
    public class WebtagInfo
    {
        readonly CuSupport Sup;

        public WebtagInfo( CuSupport s )          // Constructor
        {
            Sup = s;

            TagUnit = new string[]
            {
                Sup.StationTemp.Text(),             // 0
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "%",
                "",
                Sup.StationPressure.Text(),
                Sup.StationPressure.Text(),

                Sup.StationHeight.Text(),         // 10
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationPressure.Text(),
                Sup.StationPressure.Text(),
                Sup.StationDistance.Text(),
                "",
                "",
                "",
                Sup.StationWind.Text(),

                Sup.StationWind.Text(),              // 20
                "",
                "",
                Sup.StationWind.Text(),
                Sup.StationWind.Text(),
                Sup.StationTemp.Text(),
                Sup.StationRain.Text() + Sup.PerHour,
                "",
                "",
                "",

                "",                                 // 30
                "",
                "",
                "",
                Sup.StationRain.Text(),
                "",
                "",
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),

                Sup.StationRain.Text(),             // 40
                Sup.StationRain.Text(),
                "%",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 50
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationPressure.Text(),
                Sup.StationPressure.Text(),

                "%",                                // 60
                "%",
                Sup.StationWind.Text(),
                Sup.StationWind.Text(),
                "",
                Sup.StationRain.Text() + Sup.PerHour,
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                "",
                "",

                Sup.StationTemp.Text(),             // 70
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationWind.Text(),             // 80
                Sup.StationWind.Text(),
                Sup.StationDistance.Text(),
                Sup.StationTemp.Text(),
                Sup.StationRain.Text() + Sup.PerHour,
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationPressure.Text(),

                Sup.StationPressure.Text(),         // 90
                "%",
                "%",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "hr",

                "hr",                               // 100
                "hr",
                "hr",
                "min",
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                "",
                "W/m²",
                "",
                "W/m²",

                "hr",                               // 110
                "hr",
                "hr",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 120
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),             // An extra one as there are 11 webtags in this blok
                                                    // My bad :(

                Sup.StationTemp.Text(),             // 130
                Sup.StationTemp.Text(),
                "%",
                "%",
                "%",
                "%",
                "%",
                "%",
                "%",
                "%",

                "%",                                // 140
                "%",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 150
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                "",

                "",                                 // 160
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",

                "",                                 // 170
                "",
                "",
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 180
                Sup.StationTemp.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),

                CO2conc.Text(),                     // 190
                CO2conc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                Sup.StationTemp.Text(),
                "%",
                Sup.StationDistance.Text(),
                "",

                Sup.StationTemp.Text(),             // 200
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                "",
                "",
                "",
                "",
                "",

                "",                                 // 210
                "",
                Sup.StationTemp.Text(),
                "%",
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),

                PMconc.Text(),                      // 220
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                Sup.StationTemp.Text(),
                "%",
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),

                PMconc.Text(),                      // 230
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                "",
                "",

                "",                                 // 240
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",

                "",                                 // 250
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "%",
                "%",

                "%",                                // 260
                "%",
                "%",
                "%",
                "%",
                "%",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 270
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationPressure.Text(),
                Sup.StationPressure.Text(),
                "%",

                "%",                                // 280
                Sup.StationWind.Text(),
                Sup.StationWind.Text(),
                Sup.StationRain.Text() + Sup.PerHour,
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationDistance.Text(),

                "",                                 // 290
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 300
                Sup.StationTemp.Text(),
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationPressure.Text(),
                Sup.StationPressure.Text(),
                "%",
                "%",
                Sup.StationWind.Text(),

                Sup.StationWind.Text(),             // 310
                Sup.StationRain.Text() + Sup.PerHour,
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationDistance.Text(),
                "",

                "",                                 // 320
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "%",
                "%",
                "",
                "",
                "",
                "",
                "",

                "",                                 // 330
                "%",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 340
                Sup.StationTemp.Text(),
                Sup.StationWind.Text(),
                Sup.StationWind.Text(),
                Sup.StationDistance.Text(),
                Sup.StationTemp.Text(),
                Sup.StationRain.Text() + Sup.PerHour,
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),
                Sup.StationRain.Text(),

                Sup.StationRain.Text(),             // 350
                Sup.StationPressure.Text(),
                Sup.StationPressure.Text(),
                "%",
                "%",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "",
                "",
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),             // 360
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationDistance.Text(),
                Sup.StationRain.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),

                "%",                                // 370
                Sup.StationWind.Text(),
                Sup.StationWind.Text(),
                Sup.StationWind.Text(),
                "",
                "",
                Sup.StationPressure.Text(),
                Sup.StationRain.Text(),
                "W/m²",
                "",

                Sup.StationTemp.Text(),             // 380
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                "",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                "",
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                "%",

                Sup.StationPressure.Text(),         // 390
                Sup.StationTemp.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                PMconc.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationRain.Text(),

                Sup.StationRain.Text(),              // 400
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",     // Laser sensor will mostly be used for snow
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",     // if not then use the same units (e.g. for groundwater level)
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.StationTemp.Text(),

                Sup.StationTemp.Text(),              //410
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationTemp.Text(),
                Sup.StationRain.Text(),
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",     // Laser sensor will mostly be used for snow
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",     // if not then use the same units (e.g. for groundwater level)
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",

                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",     // 420
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",     
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in",
                Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "0" ) == "0" ? "cm" : "in"

            };

            TagAxis = new AxisType[]
            {
                AxisType.Temp,              // 0
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Humidity,
                AxisType.Temp,
                AxisType.Pressure,
                AxisType.Pressure,

                AxisType.Height,          // 10
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Pressure,
                AxisType.Pressure,
                AxisType.Distance,
                AxisType.Direction,
                AxisType.DegreeDays,
                AxisType.DegreeDays,
                AxisType.Wind,

                AxisType.Wind,              // 20
                AxisType.Direction,
                AxisType.Direction,
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Temp,
                AxisType.Rrate,
                AxisType.Direction,
                AxisType.Direction,
                AxisType.Direction,

                AxisType.Direction,         // 30
                AxisType.Direction,
                AxisType.Direction,
                AxisType.Free,
                AxisType.Rain,
                AxisType.Free,
                AxisType.Free,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,

                AxisType.Rain,              // 40
                AxisType.Rain,
                AxisType.Humidity,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 50
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Pressure,
                AxisType.Pressure,

                AxisType.Humidity,          // 60
                AxisType.Humidity,
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Direction,
                AxisType.Rrate,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Solar,
                AxisType.UV,

                AxisType.Temp,              // 70
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Wind,              // 80
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Temp,
                AxisType.Rrate,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Pressure,

                AxisType.Pressure,          // 90
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Free,
                AxisType.Free,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Free,

                AxisType.Hours,             // 100
                AxisType.Hours,
                AxisType.Hours,
                AxisType.Free,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.UV,
                AxisType.Solar,
                AxisType.Free,
                AxisType.Solar,

                AxisType.Hours,             // 110
                AxisType.Hours,
                AxisType.Hours,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 120
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,              // An extra one as there are 11 webtags in this blok
                                            // My bad :(

                AxisType.Temp,              // 130
                AxisType.Temp,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Humidity,

                AxisType.Humidity,          // 140
                AxisType.Humidity,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 150
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 160
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 170
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 180
                AxisType.Temp,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,

                AxisType.ppm,               // 190
                AxisType.ppm,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.Temp,
                AxisType.Humidity,
                AxisType.Distance,
                AxisType.Free,

                AxisType.Temp,              // 200
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 210
                AxisType.Free,
                AxisType.Temp,
                AxisType.Humidity,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,

                AxisType.AQ,                // 220
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.Temp,
                AxisType.Humidity,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,

                AxisType.AQ,                // 230
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 240
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 250
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 260
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 270
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Pressure,
                AxisType.Pressure,
                AxisType.Humidity,

                AxisType.Humidity,          // 280
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Rrate,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Distance,

                AxisType.Free,              // 290
                AxisType.Free,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 300
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Pressure,
                AxisType.Pressure,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Wind,

                AxisType.Wind,              // 310
                AxisType.Rrate,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Distance,
                AxisType.Free,

                AxisType.Free,              // 320
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,
                AxisType.Free,

                AxisType.Free,              // 330
                AxisType.Free,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Temp,              // 340
                AxisType.Temp,
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Distance,
                AxisType.Temp,
                AxisType.Rrate,
                AxisType.Rain,
                AxisType.Rain,
                AxisType.Rain,

                AxisType.Rain,              // 350
                AxisType.Pressure,
                AxisType.Pressure,
                AxisType.Humidity,
                AxisType.Humidity,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Free,
                AxisType.Free,
                AxisType.Temp,

                AxisType.Temp,              // 360
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Distance,
                AxisType.Rain,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,

                AxisType.Humidity,          // 370
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Wind,
                AxisType.Direction,
                AxisType.Direction,
                AxisType.Pressure,
                AxisType.Rain,
                AxisType.Solar,
                AxisType.UV,

                AxisType.Temp,              // 380
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Distance,
                AxisType.Free,
                AxisType.Distance,
                AxisType.Free,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Humidity,

                AxisType.Pressure,          // 390
                AxisType.Temp,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.AQ,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Rain,

                AxisType.Rain,               // 400
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Temp,

                AxisType.Temp,               //410
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Temp,
                AxisType.Rain,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,
                AxisType.Height,


                AxisType.Height,             //420
                AxisType.Height,
                AxisType.Height,
                AxisType.Height

            };

            if ( Tagname.Length != TagUnit.Length )
            {
                Sup.LogTraceErrorMessage( $"CustomLogs WebtagInfo constructor: number of defined Webtag Units ({TagUnit.Length}) != number of Webtags ({Tagname.Length}). Exiting" );
                Environment.Exit( 0 );
            }

            if ( TagUnit.Length != TagAxis.Length )
            {
                Sup.LogTraceErrorMessage( $"CustomLogs WebtagInfo constructor: number of defined Webtag Axis ({TagAxis.Length}) != number of Webtags ({Tagname.Length}). Exiting" );
                Environment.Exit( 0 );
            }

            Sup.LogTraceInfoMessage( $"CustomLogs WebtagInfo constructor: number of defined Webtag Units: {TagUnit.Length}, everything OK. " );

            if ( !CUtils.DoWebsite && Sup.LoggingOn && Sup.CUTraceSwitch.Level == System.Diagnostics.TraceLevel.Verbose )
            {
                Sup.LogTraceVerboseMessage( $"CustomLogs WebtagInfo Verbose info:" );

                for ( int i = 0; i < Tagname.Length; i++ )
                {
                    Sup.LogTraceVerboseMessage( $"    {Tagname[ i ]} => unit {TagUnit[ i ]} => axis {TagAxis[ i ]}" );
                }

                Environment.Exit( 0 );
            }

            return;
        }

        public string GetTagUnit( string name ) => TagUnit[ Array.FindIndex( Tagname, word => word.Equals( name, CUtils.Cmp ) ) ];

        public AxisType GetTagAxis( string name ) => TagAxis[ Array.FindIndex( Tagname, word => word.Equals( name, CUtils.Cmp ) ) ];

        public bool IsValidWebtag( string name ) => Array.FindIndex( Tagname, word => word.Equals( name, CUtils.Cmp ) ) != -1;

        public string FetchWebtagRaw( string s, ref int Start )
        {
            // returns the raw Webtag, or if no webtag found, returns null
            string tmp;
            Start = s.IndexOf( "<#", Start );

            if ( Start >= 0 )
            {
                int b = s.IndexOf( '>', Start );
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

        //static readonly bool[] changeIndicator = new bool[] { };

        private readonly string[] TagUnit;

        private readonly AxisType[] TagAxis;

        static readonly string[] Tagname = new string[] {
            "temp",                     // 0
            "apptemp",
            "feelslike",
            "temprange",
            "heatindex",
            "avgtemp",
            "hum",
            "humidex",
            "press",
            "altimeterpressure",

            "cloudbasevalue",                // 10
            "dew",
            "wetbulb",
            "presstrendval",
            "PressChangeLast3Hours",
            "windrun",
            "domwindbearing",
            "heatdegdays",
            "cooldegdays",
            "wlatest",

            "wspeed",                   // 20
            "currentwdir",
            "wdir",
            "wgust",
            "windAvg",
            "wchill",
            "rrate",
            "bearing",
            "avgbearing",
            "BearingRangeFrom",

            "BearingRangeTo",           // 30
            "BearingRangeFrom10",
            "BearingRangeTo10",
            "beaufortnumber",
            "rfall",
            "ConsecutiveRainDays",
            "ConsecutiveDryDays",
            "rmidnight",
            "rmonth",
            "rhour",

            "r24hour",                  // 40
            "ryear",
            "inhum",
            "intemp",
            "tempTH",
            "tempTL",
            "tempMidnightTH",
            "tempMidnightTL",
            "tempMidnightRangeT",
            "wchillTL",

            "apptempTH",                // 50
            "apptempTL",
            "feelslikeTH",
            "feelslikeTL",
            "humidexTH",
            "dewpointTH",
            "dewpointTL",
            "heatindexTH",
            "pressTH",
            "pressTL",

            "humTH",                    // 60
            "humTL",
            "windTM",
            "wgustTM",
            "bearingTM",
            "rrateTM",
            "hourlyrainTH",
            "rain24hourTH",
            "solarTH",
            "UVTH",

            "tempH",                    // 70
            "tempL",
            "apptempH",
            "apptempL",
            "feelslikeH",
            "feelslikeL",
            "humidexH",
            "dewpointH",
            "dewpointL",
            "heatindexH",

            "gustM",                    // 80
            "wspeedH",
            "windrunH",
            "wchillL",
            "rrateM",
            "rfallH",
            "r24hourH",
            "rfallhH",
            "rfallmH",
            "pressH",

            "pressL",                   // 90
            "humH",
            "humL",
            "mintempH",
            "maxtempL",
            "LongestDryPeriod",
            "LongestWetPeriod",
            "LowDailyTempRange",
            "HighDailyTempRange",
            "daylength",

            "daylightlength",           // 100
            "chillhours",
            "chillhoursToday",
            "MinutesSinceLastRainTip",
            "ET",
            "AnnualET",
            "UV",
            "SolarRad",
            "Light",
            "CurrentSolarMax",

            "SunshineHours",            // 110
            "SunshineHoursMonth",
            "SunshineHoursYear",
            "ExtraTemp1",
            "ExtraTemp2",
            "ExtraTemp3",
            "ExtraTemp4",
            "ExtraTemp5",
            "ExtraTemp6",
            "ExtraTemp7",

            "ExtraTemp8",               // 120
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

            "ExtraDP9",                 // 130
            "ExtraDP10",
            "ExtraHum1",
            "ExtraHum2",
            "ExtraHum3",
            "ExtraHum4",
            "ExtraHum5",
            "ExtraHum6",
            "ExtraHum7",
            "ExtraHum8",

            "ExtraHum9",                // 140
            "ExtraHum10",
            "SoilTemp1",
            "SoilTemp2",
            "SoilTemp3",
            "SoilTemp4",
            "SoilTemp5",
            "SoilTemp6",
            "SoilTemp7",
            "SoilTemp8",

            "SoilTemp9",                // 150
            "SoilTemp10",
            "SoilTemp11",
            "SoilTemp12",
            "SoilTemp13",
            "SoilTemp14",
            "SoilTemp15",
            "SoilTemp16",
            "SoilMoisture1",
            "SoilMoisture2",

            "SoilMoisture3",            // 160
            "SoilMoisture4",
            "SoilMoisture5",
            "SoilMoisture6",
            "SoilMoisture7",
            "SoilMoisture8",
            "SoilMoisture9",
            "SoilMoisture10",
            "SoilMoisture11",
            "SoilMoisture12",

            "SoilMoisture13",           // 170
            "SoilMoisture14",
            "SoilMoisture15",
            "SoilMoisture16",
            "UserTemp1",
            "UserTemp2",
            "UserTemp3",
            "UserTemp4",
            "UserTemp5",
            "UserTemp6",

            "UserTemp7",                // 180
            "UserTemp8",
            "AirQuality1",
            "AirQuality2",
            "AirQuality3",
            "AirQuality4",
            "AirQualityAvg1",
            "AirQualityAvg2",
            "AirQualityAvg3",
            "AirQualityAvg4",

            "CO2",                      // 190
            "CO2_24h",
            "CO2_pm2p5",
            "CO2_pm2p5_24h",
            "CO2_pm10",
            "CO2_pm10_24h",
            "CO2_temp",
            "CO2_hum",
            "LightningDistance",
            "LightningStrikesToday",

            "none1",                // 200  none was occupied by LeafTemp
            "none1",
            "none1",
            "none1",
            "LeafWetness1",
            "LeafWetness2",
            "LeafWetness3",
            "LeafWetness4",
            "LeafWetness5",
            "LeafWetness6",

            "LeafWetness7",             // 210
            "LeafWetness8",
            "AirLinkTempIn",
            "AirLinkHumIn",
            "AirLinkPm1In",
            "AirLinkPm2p5In",
            "AirLinkPm2p5_1hrIn",
            "AirLinkPm2p5_3hrIn",
            "AirLinkPm2p5_24hrIn",
            "AirLinkPm2p5_NowcastIn",

            "AirLinkPm10In",            // 220
            "AirLinkPm10_1hrIn",
            "AirLinkPm10_3hrIn",
            "AirLinkPm10_24hrIn",
            "AirLinkPm10_NowcastIn",
            "AirLinkTempOut",
            "AirLinkHumOut",
            "AirLinkPm1Out",
            "AirLinkPm2p5Out",
            "AirLinkPm2p5_1hrOut",

            "AirLinkPm2p5_3hrOut",      // 230
            "AirLinkPm2p5_24hrOut",
            "AirLinkPm2p5_NowcastOut",
            "AirLinkPm10Out",
            "AirLinkPm10_1hrOut",
            "AirLinkPm10_3hrOut",
            "AirLinkPm10_24hrOut",
            "AirLinkPm10_NowcastOut",
            "AirLinkAqiPm2p5In",
            "AirLinkAqiPm2p5_1hrIn",

            "AirLinkAqiPm2p5_3hrIn",    // 240
            "AirLinkAqiPm2p5_24hrIn",
            "AirLinkAqiPm2p5_NowcastIn",
            "AirLinkAqiPm10In",
            "AirLinkAqiPm10_1hrIn",
            "AirLinkAqiPm10_3hrIn",
            "AirLinkAqiPm10_24hrIn",
            "AirLinkAqiPm10_NowcastIn",
            "AirLinkAqiPm2p5Out",
            "AirLinkAqiPm2p5_1hrOut",

            "AirLinkAqiPm2p5_3hrOut",   // 250
            "AirLinkAqiPm2p5_24hrOut",
            "AirLinkAqiPm2p5_NowcastOut",
            "AirLinkAqiPm10Out",
            "AirLinkAqiPm10_1hrOut",
            "AirLinkAqiPm10_3hrOut",
            "AirLinkAqiPm10_24hrOut",
            "AirLinkAqiPm10_NowcastOut",
            "AirLinkPct_1hrIn",
            "AirLinkPct_3hrIn",

            "AirLinkPct_24hrIn",        // 260
            "AirLinkPct_NowcastIn",
            "AirLinkPct_1hrOut",
            "AirLinkPct_3hrOut",
            "AirLinkPct_24hrOut",
            "AirLinkPct_NowcastOut",
            "MonthTempH",
            "MonthTempL",
            "MonthHeatIndexH",
            "MonthWChillL",

            "MonthAppTempH",            // 270
            "MonthAppTempL",
            "MonthFeelsLikeH",
            "MonthFeelsLikeL",
            "MonthHumidexH",
            "MonthMinTempH",
            "MonthMaxTempL",
            "MonthPressH",
            "MonthPressL",
            "MonthHumH",

            "MonthHumL",                // 280
            "MonthGustH",
            "MonthWindH",
            "MonthRainRateH",
            "MonthHourlyRainH",
            "MonthRain24HourH",
            "MonthDailyRainH",
            "MonthDewPointH",
            "MonthDewPointL",
            "MonthWindRunH",

            "MonthLongestDryPeriod",    // 290
            "MonthLongestWetPeriod",
            "MonthHighDailyTempRange",
            "MonthLowDailyTempRange",
            "YearTempH",
            "YearTempL",
            "YearHeatIndexH",
            "YearWChillL",
            "YearAppTempH",
            "YearAppTempL",

            "YearFeelsLikeH",           // 300
            "YearFeelsLikeL",
            "YearHumidexH",
            "YearMinTempH",
            "YearMaxTempL",
            "YearPressH",
            "YearPressL",
            "YearHumH",
            "YearHumL",
            "YearGustH",

            "YearWindH",                // 310
            "YearRainRateH",
            "YearHourlyRainH",
            "YearRain24HourH",
            "YearDailyRainH",
            "YearMonthlyRainH",
            "YearDewPointH",
            "YearDewPointL",
            "YearWindRunH",
            "YearLongestDryPeriod",

            "YearLongestWetPeriod",     // 320
            "YearHighDailyTempRange",
            "YearLowDailyTempRange",
            "MoonPercent",
            "MoonPercentAbs",
            "MoonAge",
            "DavisTotalPacketsReceived",
            "DavisTotalPacketsMissed",
            "DavisNumberOfResynchs",
            "DavisMaxInARow",

            "DavisNumCRCerrors",        // 330
            "DavisReceptionPercent",
            "ByMonthTempH",
            "ByMonthTempL",
            "ByMonthAppTempH",
            "ByMonthAppTempL",
            "ByMonthFeelsLikeH",
            "ByMonthFeelsLikeL",
            "ByMonthHumidexH",
            "ByMonthDewPointH",

            "ByMonthDewPointL",         // 340
            "ByMonthHeatIndexH",
            "ByMonthGustH",
            "ByMonthWindH",
            "ByMonthWindRunH",
            "ByMonthWChillL",
            "ByMonthRainRateH",
            "ByMonthDailyRainH",
            "ByMonthHourlyRainH",
            "ByMonthRain24HourH",

            "ByMonthMonthlyRainH",          // 350
            "ByMonthPressH",
            "ByMonthPressL",
            "ByMonthHumH",
            "ByMonthHumL",
            "ByMonthMinTempH",
            "ByMonthMaxTempL",
            "ByMonthLongestDryPeriod",
            "ByMonthLongestWetPeriod",
            "ByMonthLowDailyTempRange",

            "ByMonthHighDailyTempRange",    // 360
            "CPUTemp",
            "THWindex",
            "THSWindex",
            "windrunmonth",
            "StormRain",
            "RecentOutsideTemp",
            "RecentWindChill",
            "RecentDewPoint",
            "RecentHeatIndex",

            "RecentHumidity",               // 370
            "RecentWindSpeed",
            "RecentWindGust",
            "RecentWindLatest",
            "RecentWindDir",
            "RecentWindAvgDir",
            "RecentPressure",
            "RecentRainToday",
            "RecentSolarRad",
            "RecentUV",

            "RecentWindChill",              // 380
            "RecentFeelsLike",
            "RecentHumidex",
            "snowdepth",
            "mone",                         // deprecated snowlying
            "snow24hr",                     // deprecated snowfalling => snow24h
            "Tbeaufortnumber",
            "RecentApparent",
            "RecentIndoorTemp",
            "RecentIndoorHumidity",

            "stationpressure",              //390
            "TempAvg24Hrs",
            "CO2_pm1",
            "CO2_pm1_24h",
            "CO2_pm4",
            "CO2_pm4_24h",
            "ByMonthTempAvg",
            "MonthTempAvg",
            "YearTempAvg",
            "MonthRainfall",

            "AnnualRainfall",               //400
            "LaserDist1",
            "LaserDist2",
            "LaserDist3",
            "LaserDist4",
            "LaserDepth1",
            "LaserDepth2",
            "LaserDepth3",
            "LaserDepth4",
            "temp9amTH",

            "temp9amTL",                    //410
            "temp9amRangeT",
            "temp9amYH",
            "temp9amYL",
            "temp9amRangeY",
            "rweek",
            "SnowAccumSeason1",
            "SnowAccumSeason2",
            "SnowAccumSeason3",
            "SnowAccumSeason4",

            "SnowAccum24h1",                //420
            "SnowAccum24h2",
            "SnowAccum24h3",
            "SnowAccum24h4",   
        };

    }

    #endregion

} // Namespace CumulusUtils
