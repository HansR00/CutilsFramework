﻿/*
 * ChartsCompiler CodeGen - Part of CumulusUtils
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
 * Literature:  https://github.com/jstat/jstat
 *              https://jstat.github.io/all.html
 *              https://www.highcharts.com/docs/chart-and-series-types/chart-types
 *              https://stackoverflow.com/questions/1232040/how-do-i-empty-an-array-in-javascript
 *              
 * 
 * Highcharts date/time format specifiers (https://api.highcharts.com/class-reference/Highcharts.Time) :
 * 
 *        * Supported format keys:
 *
 *        %a: Short weekday, like 'Mon'
 *        %A: Long weekday, like 'Monday'
 *        %d: Two digit day of the month, 01 to 31
 *        %e: Day of the month, 1 through 31
 *        %w: Day of the week, 0 through 6
 *        %b: Short month, like 'Jan'
 *        %B: Long month, like 'January'
 *        %m: Two digit month number, 01 through 12
 *        %y: Two digits year, like 09 for 2009
 *        %Y: Four digits year, like 2009
 *        %H: Two digits hours in 24h format, 00 through 23
 *        %k: Hours in 24h format, 0 through 23
 *        %I: Two digits hours in 12h format, 00 through 11
 *        %l: Hours in 12h format, 1 through 12
 *        %M: Two digits minutes, 00 through 59
 *        %p: Upper case AM or PM
 *        %P: Lower case AM or PM
 *        %S: Two digits seconds, 00 through 59
 *        %L: Milliseconds (naming from Ruby)
 *
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    partial class ChartsCompiler
    {

        #region CodeGenerator

        public void GenerateUserDefinedCharts( List<ChartDef> theseCharts, string filename, int UniqueOutputId )
        {
            if ( theseCharts.Count == 0 ) return;

            StringBuilder Html = new StringBuilder();
            StringBuilder MenuJavascript = new StringBuilder();
            StringBuilder AjaxJavascript = new StringBuilder();
            StringBuilder AddSeriesJavascript = new StringBuilder();
            StringBuilder GenericJavascript = new StringBuilder();
            StringBuilder TheCharts = new StringBuilder();

            Sup.LogDebugMessage( $"Compiler - CodeGen: {filename}" );

            List<AllVarInfo> AllVars = CheckAllVariablesInThisSetOfCharts( theseCharts );
            GenerateSeriesVariables( GenericJavascript, AllVars );

            List<string> theseDatafiles = AllVars.Where( p => p.Datafile != "" ).Select( p => p.Datafile ).Distinct().ToList();
            bool TheseChartsUseWindBarbs = theseCharts.Where( p => p.HasWindBarbs ).Any();
            bool TheseChartsUseInfo = theseCharts.Where( p => p.HasInfo ).Any();

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{filename}", false, Encoding.UTF8 ) )
            {
                bool UseHighchartsBoostModule = Sup.GetUtilsIniValue( "Graphs", "UseHighchartsBoostModule", "true" ).Equals( "true", CUtils.Cmp );

                Html.AppendLine( "<!--" );
                Html.AppendLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                Html.AppendLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                Html.AppendLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                Html.AppendLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                Html.AppendLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                Html.AppendLine( "-->" );

                Html.AppendLine( Sup.GenjQueryIncludestring() );

                if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes && TheseChartsUseInfo )
                    Html.AppendLine( "<script src=\"https://cdnjs.cloudflare.com/ajax/libs/jquery-modal/0.9.2/jquery.modal.min.js\"  crossorigin=\"anonymous\" referrerpolicy=\"no-referrer\"></script>" +
                       "<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/jquery-modal/0.9.2/jquery.modal.css\" crossorigin=\"anonymous\" referrerpolicy=\"no-referrer\" />" );

                if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes ) Html.AppendLine( Sup.GenHighchartsIncludes().ToString() );

                Html.AppendLine( "<style>" );
                Html.AppendLine( "#report{" );
                Html.AppendLine( "  font-family: arial;" );
                Html.AppendLine( "  border-radius: 15px;" );
                Html.AppendLine( "  border-spacing: 0;" );
                Html.AppendLine( "  border: 1px solid #b0b0b0;" );
                Html.AppendLine( "}" );
                Html.AppendLine( Sup.HighchartsAllowBackgroundImage() );
                Html.AppendLine( "</style>" );

                Html.AppendLine( "<div><p style='text-align:center;'>" );
                Html.AppendLine( $"<select id='graph{UniqueOutputId}'>" );

                MenuJavascript.AppendLine( $"$('#graph{UniqueOutputId}').change(function(){{CurrentChart{UniqueOutputId}=document.getElementById('graph{UniqueOutputId}').value; handleChange{UniqueOutputId}();}});" );
                MenuJavascript.AppendLine( "var prevChartRange;" );
                MenuJavascript.AppendLine( $"function handleChange{UniqueOutputId}() {{" );
                MenuJavascript.AppendLine( $"  var w1 = document.getElementById('graph{UniqueOutputId}').value = CurrentChart{UniqueOutputId};" );

                GenericJavascript.AppendLine( "var chart, config, freezing;" );

                // Change defaults of the Charts Page / Home button in the Document Init function but only if NOT website
                GenericJavascript.Append( "$( function(){  " );

                // This complex conditional must make sure the initialisation of the chart is done and is done only once.
                //   1) !website means we are doing compileonly (modular) but that maybe for use in the CUtils website or really for another website
                //   2) CUtils.DojQueryInclude || CUtils.DoLibraryIncludes is used to determine compileonly is used for another website
                //   3) If UniqueOutputId > 0 the Init is not called by the runtime system and needs to be done here
                //   4) If the condition is true, the chart needs the initialisation on itself
                //      --- this must be a rewrite: initialisation should always be done from the runtime such that we know what the last chart is and that the timer can 
                //      refresh whatever chart is loaded
                if ( ( !CUtils.DoWebsite && ( CUtils.DojQueryInclude || CUtils.DoLibraryIncludes ) ) || UniqueOutputId > 0 )
                    GenericJavascript.Append( "InitCumulusCharts();" );

                GenericJavascript.AppendLine( " } );" );

                // Generate InitCumulusCharts
                GenericJavascript.Append( "function InitCumulusCharts() {" );
                GenericJavascript.Append( "  ChartsType = 'compiler';" );

                if ( CUtils.DoWebsite && UniqueOutputId == 0 )
                    GenericJavascript.AppendLine( $"  if (CurrentChart{UniqueOutputId} === 'Temp') {{" );   // Temp is the default when no compiler is used

                GenericJavascript.Append( "    ClickEventChart = [" );
                for ( int i = 0; i < 24; i++ )
                    GenericJavascript.Append( $"'{ClickEvents[ i ]}'," );
                GenericJavascript.Remove( GenericJavascript.Length - 1, 1 );
                GenericJavascript.AppendLine( "];" );
                GenericJavascript.AppendLine( "     console.log('Cumuluscharts Compiler version has been initialised');" );
                GenericJavascript.AppendLine( $"    CurrentChart{UniqueOutputId} = '{theseCharts[ 0 ].Id}';" );

                if ( CUtils.DoWebsite && UniqueOutputId == 0 )
                    GenericJavascript.AppendLine( "  }" );

                GenericJavascript.AppendLine( $"  $.when( Promise.all([GraphconfigAjax()" );

                foreach ( string df in theseDatafiles )
                {
                    if ( !string.IsNullOrEmpty( df ) )
                        GenericJavascript.AppendLine( $", {df.Substring( 0, df.IndexOf( '.' ) )}Ajax()" );
                }

                // Add the WindBarbs line
                if ( TheseChartsUseWindBarbs ) GenericJavascript.AppendLine( $", WindBarbsAjax()" );

                GenericJavascript.AppendLine( $"])).then( () => $( '#graph{UniqueOutputId}' ).trigger( 'change' ) ) " );
                GenericJavascript.AppendLine( "}" );

                if ( TheseChartsUseWindBarbs )
                    GenericJavascript.Append( "function convertToMs(data) {" +
                        "  data.map( " +
                       $"  s => {{s[ 1 ] = s[ 1 ] * {Sup.StationWind.Convert( Sup.StationWind.Dim, WindDim.ms, 1 ).ToString( "F5", CUtils.Inv )} }} );" +
                        "  return" +
                        "}\n" );

                GenericJavascript.AppendLine( "var compassP = function (deg) {" );
                GenericJavascript.AppendLine( "  var a = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'];" );
                GenericJavascript.AppendLine( "  return a[Math.floor((deg + 22.5) / 45) % 8];" );
                GenericJavascript.AppendLine( "};" );

                GenericJavascript.AppendLine( "function GraphconfigAjax(){" );
                GenericJavascript.AppendLine( "  console.log( 'Highcharts version : ' + Highcharts.version );" );
                GenericJavascript.AppendLine( "  return $.ajax({" );
                GenericJavascript.AppendLine( $"    url: '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}graphconfig.json', cache: true, datatype: 'json'}})" );
                GenericJavascript.AppendLine( "    .done( function(resp) {" +
                    "      config = resp;" +
                    "      freezing = config.temp.units === 'C' ? 0 : 32;" +
                    "      console.log('Succes in Ajax Graphconfig');" +
                   $"      document.getElementById( 'graph{UniqueOutputId}' ).value = CurrentChart{UniqueOutputId};" +
                    "})" ); // End .done()
                GenericJavascript.AppendLine( "    .fail(function ( xhr, textStatus, errorThrown) {" +
                    "      console.log('graphconfig.json ' + textStatus + ' : ' + errorThrown);" +
                    "});" ); // End $.ajax
                GenericJavascript.AppendLine( "}" ); // End GraphconfigAjax

                bool first = true;

                // Loop over the datafiles to create the Ajax calls
                // Already determined in the AllVars loop, now independent of all charts
                foreach ( string df in theseDatafiles )
                {
                    if ( !string.IsNullOrEmpty( df ) )
                    {
                        AjaxJavascript.AppendLine( $"function {df.Substring( 0, df.IndexOf( '.' ) )}Ajax(){{" );

                        foreach ( AllVarInfo avi in AllVars )
                            if ( df == avi.Datafile )
                                AjaxJavascript.AppendLine( $"  {avi.KeywordName}.length = 0;" );

                        AjaxJavascript.AppendLine( "  return $.ajax({" );

                        // Make a distinction between CumulusUtils JSONfiles and regular CMX JSONfiles
                        // CumulusUtils JSONs are always in the CumulusUtils directory. So: the current webroot
                        if ( df.StartsWith( "CUserdata" ) || df.StartsWith( "extrasensors" ) || df.StartsWith( "customlogs" ) )
                            if ( CUtils.DoModular )
                                AjaxJavascript.AppendLine( $"    url: '{CUtils.ModulePath}{df}'," );
                            else
                                AjaxJavascript.AppendLine( $"    url: '{df}'," );
                        else
                            AjaxJavascript.AppendLine( $"    url: '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}{df}'," );

                        AjaxJavascript.AppendLine( $"    cache: false, datatype: 'json'" );
                        AjaxJavascript.AppendLine( "    })" );
                        AjaxJavascript.AppendLine( $"    .fail( function (xhr, textStatus, errorThrown) {{ console.log( '{df} ' + textStatus + ' : ' + errorThrown ); }})" );
                        AjaxJavascript.AppendLine( "    .done( function(resp) {" );      // Add the series

                        foreach ( AllVarInfo avi in AllVars )
                            if ( df == avi.Datafile )
                            {
                                AjaxJavascript.AppendLine( $"      for (var i = 0; i < resp.{avi.TypeName}.length; i++)" );
                                AjaxJavascript.AppendLine( $"        {avi.KeywordName}.push([resp.{avi.TypeName}[i][0], resp.{avi.TypeName}[i][1] ]);" );
                            }
                        AjaxJavascript.AppendLine( "    })" ); // End .done / .ajax
                        AjaxJavascript.AppendLine( "  }" ); // End datafilenameAjax() function
                    }
                }

                // Add the WindBarbsAjax() function when needed
                if ( TheseChartsUseWindBarbs )
                {
                    AjaxJavascript.AppendLine( "function WindBarbsAjax() {" );
                    AjaxJavascript.AppendLine( "  WindBarbData.length = 0;" );
                    AjaxJavascript.AppendLine( "  return $.when( " );
                    AjaxJavascript.AppendLine( "  $.ajax({" );
                    AjaxJavascript.AppendLine( $"    url: '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}winddata.json'," );
                    AjaxJavascript.AppendLine( "    cache: false," );
                    AjaxJavascript.AppendLine( "    datatype: 'json' }), " );
                    AjaxJavascript.AppendLine( "  $.ajax({" );
                    AjaxJavascript.AppendLine( $"    url: '{Sup.GetUtilsIniValue( "Website", "CumulusRealTimeLocation", "" )}wdirdata.json'," );
                    AjaxJavascript.AppendLine( "    cache: false," );
                    AjaxJavascript.AppendLine( "    datatype: 'json' })" );
                    AjaxJavascript.AppendLine( "  ).then( " );
                    AjaxJavascript.AppendLine( "    function( resp1, resp2 ) { " +
                        "  for ( var i = 0; i < resp1[0].wspeed.length; i++ ) " +
                        "    WindBarbData.push([ resp1[0].wspeed[ i ][ 0 ], resp1[0].wspeed[ i ][ 1 ], resp2[0].avgbearing[ i ][ 1 ] ]); " +
                        "  convertToMs( WindBarbData );" +
                        "}," );
                    AjaxJavascript.AppendLine( "    function(){ console.log( 'FAIL reading WindBarb Data...' )}" );
                    AjaxJavascript.AppendLine( "  );" );
                    AjaxJavascript.AppendLine( "}" );
                }

                Sup.LogTraceInfoMessage( $"Compiler - CodeGen: {filename} Written the Ajax calls" );

                foreach ( ChartDef thisChart in theseCharts )
                {
                    AxisType AxisSet = AxisType.None;

                    Html.AppendLine( $"  <option value='{thisChart.Id}'{( first ? " selected" : "" )}>{thisChart.Id}</option>" );
                    MenuJavascript.AppendLine( $"  if (w1=='{thisChart.Id}') {{ " );

                    // Meaning we are dealing with CustomLogs in the website (with realtime values table);
                    // A bit awkward method, may change that sometime haha...
                    if ( filename.Equals( Sup.CustomLogsCharts ) )
                    {
                        // Add some code to subdivide the realtime tables into RECENT and DAILY and show only one of them. 
                        // They are already defined of limited length and have an overflow.
                        MenuJavascript.AppendLine( $"if (prevChartRange != {(int) thisChart.Range} ) {{" );
                        MenuJavascript.AppendLine( "  $( '.slideOptions' ).slideUp('slow');" );
                        MenuJavascript.Append( thisChart.Range == PlotvarRangeType.Extra ? "  $( '#RecentCustomLogs' )" : "  $( '#DailyCustomLogs' )" );
                        MenuJavascript.AppendLine( ".slideDown('slow'); " );
                        MenuJavascript.AppendLine( $"prevChartRange = {(int) thisChart.Range};" );
                        MenuJavascript.AppendLine( "}" );
                    }
                    MenuJavascript.AppendLine( $"    do{thisChart.Id}()}}" );

                    // AlignTicks must be true!! (is the default of HighCharts) this also makes softMax superfluous
                    TheCharts.AppendLine( $"function do{thisChart.Id}() {{" );
                    TheCharts.AppendLine( $"  console.log('Creating chart: {thisChart.Title}');" );

                    TheCharts.AppendLine( "  chart = Highcharts.stockChart('chartcontainer', { title: {" );
                    TheCharts.Append( $" text: '{thisChart.Title}'" );
                    if ( thisChart.HasWindBarbs && !thisChart.WindBarbsBelow ) TheCharts.Append( ", margin: 35" );
                    TheCharts.AppendLine( "}," );

                    TheCharts.Append( "      xAxis:" );
                    if ( thisChart.HasWindBarbs ) TheCharts.Append( "[" );

                    TheCharts.AppendLine( "      {type: 'datetime', crosshair: true, ordinal: false,dateTimeLabelFormats:{day: '%e %b',week: '%e %b %y',month: '%b %y',year: '%Y'}}," );
                    if ( thisChart.HasWindBarbs )
                    {
                        if ( thisChart.WindBarbsBelow )
                            TheCharts.AppendLine( "{linkedTo:0, labels: {enabled: false}, offset: 0}" );
                        else
                            TheCharts.AppendLine( "{linkedTo:0, opposite: true, labels: {enabled: false} }" );

                        TheCharts.AppendLine( "]," );
                    }
                    TheCharts.AppendLine( "      yAxis:{" );
                    CreateAxis( thisChart, TheCharts, ref AxisSet );
                    TheCharts.AppendLine( "      }," );

                    TheCharts.AppendLine( "      legend:{enabled: true}," );

                    if ( thisChart.HasScatter )
                    {
                        TheCharts.AppendLine( "      plotOptions: { scatter: {cursor: 'pointer'," +
                            $"{( Graphx.UseHighchartsBoostModule ? "boostThreshold: 200," : "" )} lineWidth:0," +
                            $"marker: {{radius: {thisChart.PlotVars.First().LineWidth} }}, " +
                            "}}," );
                        TheCharts.AppendLine( "      tooltip: { xDateFormat: '%A, %b %e %H:%M ', " +
                            "pointFormatter() {return this.series.name + ': ' + this.y}," +
                            "headerFormat: '{point.key}' }," );
                    }
                    else
                    {
                        TheCharts.AppendLine( "      plotOptions: { series: {turboThreshold: 0, states: { hover: { halo: { size: 5,opacity: 0.25} } }," +
                            "marker: { enabled: false, states: { hover: { enabled: true, radius: 0.1} } } }, }," );
                        TheCharts.AppendLine( "      tooltip: {split: true, valueDecimals: 1, xDateFormat: '%A, %b %e, %H:%M'}," );
                    }

                    TheCharts.AppendLine( "      series:[]," );

                    if ( thisChart.Range == PlotvarRangeType.Recent || thisChart.Range == PlotvarRangeType.Extra )
                    {
                        TheCharts.AppendLine( "      rangeSelector:{" );

                        if ( thisChart.HasWindBarbs && !thisChart.WindBarbsBelow ) TheCharts.AppendLine( "    floating: true, y: -50," );

                        TheCharts.AppendLine( "      buttons:[{" );
                        TheCharts.AppendLine( $"       count: {CUtils.HoursInGraph / 4},type: 'hour',text: '{CUtils.HoursInGraph / 4}h'}}, {{" );
                        TheCharts.AppendLine( $"       count: {CUtils.HoursInGraph / 2},type: 'hour',text: '{CUtils.HoursInGraph / 2}h'}}, {{" );
                        TheCharts.AppendLine( "        type: 'all',text: 'All'}]," );
                        TheCharts.AppendLine( "      inputEnabled: false," );

                        if ( thisChart.Zoom == -1 )
                            TheCharts.AppendLine( "     selected: 2 }" );
                        else
                            TheCharts.AppendLine( $"     selected: {thisChart.Zoom} - 1 }}" );
                    }
                    else
                    {
                        if ( thisChart.Range == PlotvarRangeType.Daily )
                        {
                            if ( thisChart.Zoom == -1 )
                                TheCharts.AppendLine( "      rangeSelector:{allButtonsEnabled: true, selected: 0 }" );
                            else
                                TheCharts.AppendLine( $"      rangeSelector:{{allButtonsEnabled: true, selected: {thisChart.Zoom} - 1 }}" );
                        }
                        else
                        {
                            if ( thisChart.Zoom == -1 )
                                TheCharts.AppendLine( "      rangeSelector:{allButtonsEnabled: true, selected: 4 }" );
                            else
                                TheCharts.AppendLine( $"      rangeSelector:{{allButtonsEnabled: true, selected: {thisChart.Zoom} - 1 }}" );
                        }
                    }

                    TheCharts.AppendLine( "  });" );

                    if ( thisChart.HasInfo )
                    {
                        string Info = $"{Sup.GetCUstringValue( "General", "Info", "Info", true )}";

                        TheCharts.AppendLine( "chart.update({" );
                        TheCharts.AppendLine( "  chart:{events:{render() {const {x,y,width} = this.exportingGroup.getBBox();" );

                        TheCharts.AppendLine( "  if ( !this.customText ){" ); // Create a customText if it doesn't exist
                        TheCharts.AppendLine( $"    this.customText = this.renderer.text( '{Info}', x - width - 15, y + 15 )" );
                        TheCharts.AppendLine( "      .add()" +
                            ".css({color: this.title.styles.color})" +
                            ".css({cursor: 'pointer'})" +
                            $".on('click', () => $('#{thisChart.Id}').modal( 'show') );" );
                        TheCharts.AppendLine( "  } else {" ); // Update the label position on render event (i.e on window resize)
                        TheCharts.AppendLine( "    this.customText.attr({x: x - width - 15, y: y + 15}); } } } } });" );
                    }

                    TheCharts.AppendLine( "  chart.showLoading();" );

                    CreateAxis( thisChart, TheCharts, ref AxisSet );

                    TheCharts.AppendLine( $"  Promise.all([" );
                    TheCharts.AppendLine( "]).then(() => {" );
                    TheCharts.AppendLine( $"  {thisChart.Id}AddSeries(chart);" );

                    TheCharts.AppendLine( "  chart.hideLoading();" );
                    TheCharts.AppendLine( "  chart.redraw();});" );
                    TheCharts.AppendLine( "}" );

                    Sup.LogTraceInfoMessage( $"Compiler - CodeGen: {filename} Written the Chart {thisChart.Id}" );

                    // Rewrite below for the create series
                    AddSeriesJavascript.AppendLine( $"function {thisChart.Id}AddSeries(thisChart){{" );

                    foreach ( Plotvar thisPlotvar in thisChart.PlotVars )
                    {
                        string pvSuffix = thisPlotvar.PlotVar.Length > 2 ? thisPlotvar.PlotVar.Substring( 3 ) : "";

                        if ( thisPlotvar.GraphType == "columnrange" )
                        {
                            // By beteljuice (in principle that is)
                            AddSeriesJavascript.AppendLine( $"var {pvSuffix}RangeMinMax = [];" );
                            AddSeriesJavascript.AppendLine( $"for(var i=0; i<{thisPlotvar.Keyword}.length; i++) {{" +
                                $"{pvSuffix}RangeMinMax.push([{thisPlotvar.Keyword}[i][0],min{pvSuffix}[i][1], max{pvSuffix}[i][1]]) }}" );
                        }
                        else if ( thisPlotvar.Equation != null ) // | Must be done for all variables
                        {
                            string sumExpr = "";

                            string tmpEquation = thisPlotvar.Equation;

                            if ( thisPlotvar.Equation.Contains( "sum(" ) )
                            {
                                int startSum, endSum;
                                string tmp;

                                startSum = tmpEquation.IndexOf( "sum(" );
                                endSum = tmpEquation.IndexOf( ")", startSum );
                                sumExpr = tmpEquation.Substring( startSum + 4, endSum - startSum - 4 );
                                tmp = tmpEquation.Remove( startSum, endSum + 1 - startSum );
                                tmpEquation = tmp.Insert( startSum, "sumResult[i][1]" );

                                foreach ( AllVarInfo avi in thisPlotvar.EqAllVarList )
                                    if ( sumExpr.Contains( avi.KeywordName, CUtils.Cmp ) )
                                        sumExpr = sumExpr.Replace( avi.KeywordName, $"{avi.KeywordName}[i][1]" );

                                GenerateSumFunction( GenericJavascript );
                                AddSeriesJavascript.AppendLine( $"sumResult.length = 0;" );
                                AddSeriesJavascript.AppendLine( $"for(var i=0; i<{thisPlotvar.EqAllVarList[ 0 ].KeywordName}.length; i++) {{" );
                                AddSeriesJavascript.AppendLine( $"  sum( {sumExpr}, sumResult, i, {thisPlotvar.EqAllVarList[ 0 ].KeywordName}[i][0]);" );
                                AddSeriesJavascript.AppendLine( "}" );
                            }

                            if ( thisPlotvar.Equation.Contains( "ln(" ) )
                                tmpEquation = tmpEquation.Replace( "ln(", "Math.log(" );
                            if ( thisPlotvar.Equation.Contains( "sqrt(" ) )
                                tmpEquation = tmpEquation.Replace( "sqrt(", "Math.sqrt(" );
                            if ( thisPlotvar.Equation.Contains( "exp(" ) )
                                tmpEquation = tmpEquation.Replace( "exp(", "Math.exp(" );
                            if ( thisPlotvar.Equation.Contains( "pow(" ) )
                                tmpEquation = tmpEquation.Replace( "pow(", "Math.pow(" );

                            if ( thisPlotvar.EqAllVarList.Count > 0 )
                            {
                                foreach ( AllVarInfo avi in thisPlotvar.EqAllVarList )
                                    if ( tmpEquation.Contains( avi.KeywordName, CUtils.Cmp ) )
                                        tmpEquation = tmpEquation.Replace( avi.KeywordName, $"{avi.KeywordName}[i][1]" );

                                // Now write out the values in the array at runtime.
                                AddSeriesJavascript.AppendLine( $"{thisPlotvar.Keyword}.length=0;" );
                                AddSeriesJavascript.AppendLine( $"for(var i=0; i<{thisPlotvar.EqAllVarList[ 0 ].KeywordName}.length; i++) {{" );
                                AddSeriesJavascript.AppendLine( $"  {thisPlotvar.Keyword}.push([ {thisPlotvar.EqAllVarList[ 0 ].KeywordName}[i][0], {tmpEquation} ]);" );
                                AddSeriesJavascript.AppendLine( "}" );
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"Compiler - CodeGen: Using Function without Plotvariable - NOT Supported in {thisChart.Id}/{thisPlotvar.Keyword}" );
                            }
                        }

                        AddSeriesJavascript.AppendLine( "   thisChart.addSeries({ " );

                        if ( thisPlotvar.GraphType == "columnrange" )
                        {
                            AddSeriesJavascript.AppendLine( $"    name:'{Sup.GetCUstringValue( "Compiler", pvSuffix + "range", pvSuffix + "range", true )}'," );
                            AddSeriesJavascript.AppendLine( $"    id:'{pvSuffix}range'," );
                            AddSeriesJavascript.AppendLine( $"    data: {pvSuffix}RangeMinMax," );
                        }
                        else if ( thisPlotvar.Equation != null )
                        {
                            AddSeriesJavascript.AppendLine( $"    name:'{Sup.GetCUstringValue( "Compiler", thisPlotvar.Keyword, thisPlotvar.Keyword, true )}'," );
                            AddSeriesJavascript.AppendLine( $"    id:'{thisPlotvar.Keyword}'," );
                            AddSeriesJavascript.AppendLine( $"    data: {thisPlotvar.Keyword}," );
                        }
                        else if ( thisPlotvar.IsStats && thisPlotvar.GraphType == "sma" )
                        {
                            AddSeriesJavascript.AppendLine( $"    name:'{thisPlotvar.GraphType}{Sup.GetCUstringValue( "Compiler", thisPlotvar.Keyword, thisPlotvar.Keyword, true )}'," );
                            AddSeriesJavascript.AppendLine( $"    id:'{thisPlotvar.GraphType}{thisPlotvar.Keyword}'," );
                            AddSeriesJavascript.AppendLine( $"    linkedTo:'{thisPlotvar.Keyword}'," );
                            AddSeriesJavascript.AppendLine( $"    showInLegend:true," );
                            AddSeriesJavascript.AppendLine( $"    params: {{period: {Sup.GetUtilsIniValue( "Graphs", "PeriodMovingAverage", "180" )} }}," );
                        }
                        else
                        {
                            AddSeriesJavascript.AppendLine( $"    name:'{Sup.GetCUstringValue( "Compiler", thisPlotvar.Keyword, thisPlotvar.Keyword, true )}'," );
                            AddSeriesJavascript.AppendLine( $"    id:'{thisPlotvar.Keyword}'," );
                            AddSeriesJavascript.AppendLine( $"    data: {thisPlotvar.Keyword}," );
                        }

                        if ( thisPlotvar.GraphType == "area" )
                            AddSeriesJavascript.AppendLine( $"    fillOpacity: {thisPlotvar.Opacity.ToString( "F1", CUtils.Inv )}," );


                        AddSeriesJavascript.AppendLine( $"    color: '{thisPlotvar.Color}'," );
                        AddSeriesJavascript.AppendLine( $"    yAxis: '{thisPlotvar.AxisId}'," );
                        AddSeriesJavascript.AppendLine( $"    type: '{thisPlotvar.GraphType}'," );

                        if ( !thisChart.HasScatter ) AddSeriesJavascript.AppendLine( $"    lineWidth: {thisPlotvar.LineWidth}," );
                        if ( !thisPlotvar.Visible ) AddSeriesJavascript.AppendLine( $"    visible: false," );

                        AddSeriesJavascript.AppendLine( $"    zIndex: {thisPlotvar.zIndex}," );
                        AddSeriesJavascript.AppendLine( $"    tooltip:{{valueSuffix: ' {thisPlotvar.Unit}'}}" );
                        AddSeriesJavascript.AppendLine( "   }, false);" );

                        Sup.LogTraceInfoMessage( $"Compiler - CodeGen: {filename} Written the Series {thisPlotvar.Keyword}" );

                    } // Loop over all plotvars within the chart

                    if ( thisChart.HasWindBarbs )
                    {
                        // Since the data is in m/s in the WindBarbData array it has to be converted back for the tooltip
                        // 

                        AddSeriesJavascript.AppendLine( "  thisChart.addSeries({ " );
                        AddSeriesJavascript.AppendLine( $"    name: '{Sup.GetCUstringValue( "Compiler", "WindBarbs", "WindBarbs", true )}'," );
                        AddSeriesJavascript.AppendLine( "    xAxis: 1," );
                        AddSeriesJavascript.AppendLine( $"    color: '{thisChart.WindBarbColor}'," );
                        AddSeriesJavascript.AppendLine( "    type: 'windbarb'," );
                        AddSeriesJavascript.AppendLine( "    visible: true," );
                        AddSeriesJavascript.AppendLine( $"    dataGrouping: {{enabled: true,units: [ ['hour', [{Sup.HighChartsWindBarbSpacing()}] ] ]}}, " );
                        AddSeriesJavascript.AppendLine( $"    tooltip: {{pointFormatter() {{return this.series.name + ': ' + " +
                            $"(this.value/{Sup.StationWind.Convert( Sup.StationWind.Dim, WindDim.ms, 1 ).ToString( "F5", CUtils.Inv )}).toFixed(1) + " +
                            $"' {Sup.StationWind.Text()}'}} }}," );
                        AddSeriesJavascript.AppendLine( "    data: WindBarbData" );
                        AddSeriesJavascript.AppendLine( "  }, false);" );
                    }

                    AddSeriesJavascript.AppendLine( "  }" );

                    first = false;
                } // Loop over all charts

                Sup.LogTraceInfoMessage( $"Compiler - CodeGen: {filename} Written the AddSeries Calls" );

                MenuJavascript.AppendLine( "}\n" );   // Close the script

                Html.AppendLine( "</select>" );
                Html.AppendLine( "</p>" );
                Html.AppendLine( "</div>" );

                Html.AppendLine( "<div id=report><br/>" );

                Html.AppendLine( $"<div id='chartcontainer' style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'> </div>" );
                Html.AppendLine( $" <p style='text-align:center;font-size:11px;'>Generated with the ChartsCompiler {CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                Html.AppendLine( "</div>" ); // #Report
                Html.AppendLine( "<script>" );

                Html.AppendLine( MenuJavascript.ToString() );
                Html.AppendLine( GenericJavascript.ToString() );
                Html.AppendLine( AjaxJavascript.ToString() );
                Html.AppendLine( AddSeriesJavascript.ToString() );
                Html.AppendLine( TheCharts.ToString() );

                Html.AppendLine( "</script>" );

                // Now write out the modal popup texts for the chart info's

                foreach ( ChartDef thisChart in theseCharts )
                {
                    if ( thisChart.HasInfo )
                    {
                        if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                        {
                            // Use the jQuery modal, by setting the DoLibraryIncludes to false the user has control whether or not to use the 
                            // supplied includes or do it all by her/himself
                            Html.AppendLine(
                                $"<div class='modal' id='{thisChart.Id}' style='font-family: Verdana, Geneva, Tahoma, sans-serif;font-size: 120%;'>" +
                                "      <div>" +
                                $"        <h5 class='modal-title'>{thisChart.Title}</h5>" +
                                "      </div>" +
                                "      <div style='text-align: left;'>" +
                                $"       {thisChart.InfoText}" +
                                "      </div>" +
                                "</div>" );
                        }
                        else
                        {
                            // Use the bootstrap modal --- tabindex='-1' 
                            Html.AppendLine( $"<div class='modal fade' id='{thisChart.Id}' role='dialog' aria-hidden='true'>" +
                            "  <div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" +
                            "    <div class='modal-content'>" +
                            "      <div class='modal-header'>" +
                            $"        <h5 class='modal-title'>{thisChart.Title}</h5>" +
                            "        <button type='button' class='close' data-bs-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" +
                            "      </div>" +
                            "      <div class='modal-body text-start'>" +
                            $"       {thisChart.InfoText}" +
                            "      </div>" +
                            "      <div class='modal-footer'>" +
                            $"       <button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{Sup.GetCUstringValue( "Website", "Close", "Close", false )}</button>" +
                            "      </div>" +
                            "    </div>" +
                            "  </div>" +
                            "</div>" );
                        }
                    }
                }

#if !RELEASE
                of.WriteLine( Html );
#else
                of.WriteLine( CuSupport.StringRemoveWhiteSpace( Html.ToString(), " " ) );
#endif

                Sup.LogDebugMessage( $"Compiler - CodeGen: {filename} Finished" );
            } // using output file
        } // End Function GenerateUserDefinedCharts

        #endregion

        #region CreateAxis

        void CreateAxis( ChartDef thisChart, StringBuilder buf, ref AxisType AxisSet )
        {
            // Each graph uses it's own set of axis, so for each Chart, do generate
            // Note the first axis (index 0) is always there and has to set in a different way (can't be done with addAxis)
            bool opposite = false;
            bool UseAddAxisCall = false;
            bool NoClosingAddAxis = false;
            int i = 0;

            //Plotvar thisPlotvar;

            Sup.LogDebugMessage( $"Compiler - Creating Axis for {thisChart.Id} " );

            if ( AxisSet.Equals( AxisType.None ) ) NoClosingAddAxis = true;

            foreach ( Plotvar thisPlotvar in thisChart.PlotVars )
            {
                if ( AxisSet.HasFlag( thisPlotvar.Axis ) ) { i++; continue; }

                Sup.LogTraceInfoMessage( $"Compiler - Creating Axis {thisPlotvar.Axis} on {thisPlotvar.PlotVar} on {thisChart.Id} " );

                if ( i++ > 0 )
                {
                    // No axis set so it is the first
                    opposite = !opposite;
                    UseAddAxisCall = true;
                    buf.Append( "  chart.addAxis({" );
                }

                // Generic attributes:
                buf.Append( $"id: '{thisPlotvar.AxisId}'," );

                if ( thisPlotvar.Axis.HasFlag( AxisType.Temp ) && !AxisSet.HasFlag( AxisType.Temp ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Temperature", "Temperature", true )} ({Sup.StationTemp.Text()})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( "softMin: freezing," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2" : "labels:{align: 'right',x: -5, y: -2" )}" +
                      $",formatter: function() {{return '<span style=\"fill: ' + (this.value <= freezing ? 'blue' : 'red') + '; \">' + this.value + '</span>';}} }}," );
                    buf.Append( "plotLines:[{value: freezing,color: 'rgb(0, 0, 180)',width: 1,zIndex: 2}]," );
                    AxisSet |= AxisType.Temp;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Pressure ) && !AxisSet.HasFlag( AxisType.Pressure ) )
                {
                    int NrOfDecimals = Sup.StationPressure.Dim == PressureDim.inchHg ? 2 : 0;

                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    _ = Sup.StationPressure.Dim == PressureDim.inchHg ? buf.Append( "allowDecimals: true," ) : buf.Append( "allowDecimals: false," );
                    buf.Append( $"softMin: {Sup.StationPressure.Format( MinPressure ).Replace( ',', '.' )}, softMax: {Sup.StationPressure.Format( MaxPressure ).Replace( ',', '.' )}," );
                    buf.Append( $"labels: {{ formatter: function () {{return Highcharts.numberFormat(this.value, {NrOfDecimals}, '.', '');}}, " +
                        $"{( opposite ? "align: 'left',x: 5,y: -2}," : "align: 'right',x: -5, y: -2}," )}" );
                    AxisSet |= AxisType.Pressure;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Rain ) && !AxisSet.HasFlag( AxisType.Rain ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Rain", "Rain", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"endOnTick: false, showLastLabel: true, softMax: 1,min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Rain;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Rrate ) && !AxisSet.HasFlag( AxisType.Rrate ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Rainrate", "Rain Rate", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"endOnTick: false, showLastLabel: true, softMax: 1,min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Rrate;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Wind ) && !AxisSet.HasFlag( AxisType.Wind ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Wind", "Wind", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Wind;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Direction ) && !AxisSet.HasFlag( AxisType.Direction ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Direction", "Direction", true )} (Compass / degrees)'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "min: 0, max: 360," );
                    buf.Append( "tickInterval: 45," ); // align: 'right',x: -5,
                    buf.Append( $"labels: {{ {( opposite ? "align: 'left',x: 5,y: -2" : "align: 'right',x: -5, y: -2" )}, formatter: function() {{return compassP(this.value);}} }}," );
                    buf.Append( "allowDecimals: false," );
                    AxisSet |= AxisType.Direction;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.UV ) && !AxisSet.HasFlag( AxisType.UV ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "UVindex", "UV index", true )}'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.UV;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Solar ) && !AxisSet.HasFlag( AxisType.Solar ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "SolarRadiation", "Solar Radiation", true )} (W/m²)'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"softMax: {ApproximateSolarMax()},min: 0," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Solar;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Humidity ) && !AxisSet.HasFlag( AxisType.Humidity ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", true )} (%)'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "min: 0, max: 100," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Humidity;
                } // End of block generatiing the Exis info
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Hours ) && !AxisSet.HasFlag( AxisType.Hours ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "General", "Hours", "Hours", true )}'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Hours;
                } // End of block generatiing the Exis info
                else if ( thisPlotvar.Axis.HasFlag( AxisType.EVT ) && !AxisSet.HasFlag( AxisType.EVT ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Evapotranspiration", "Evapotranspiration", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 1,min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.EVT;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Distance ) && !AxisSet.HasFlag( AxisType.Distance ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Distance", "Distance", true )} " +
                        $"({( string.IsNullOrEmpty( thisPlotvar.Unit ) ? new Distance( DistanceDim.kilometer ).Text() : thisPlotvar.Unit )})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 10,softMin: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Distance;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Height ) && !AxisSet.HasFlag( AxisType.Height ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", "Height", "Height", true )} " +
                        $"({( string.IsNullOrEmpty( thisPlotvar.Unit ) ? Sup.StationHeight.Text() : thisPlotvar.Unit )})'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 100,softMin: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.Height;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.DegreeDays ) && !AxisSet.HasFlag( AxisType.DegreeDays ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "DegreeDays", "DegreeDays", true )}'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 10,softMin: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}," );
                    AxisSet |= AxisType.DegreeDays;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Free ) && !AxisSet.HasFlag( AxisType.Free ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", $"{thisChart.Id}Dimensionless", "Dimensionless", true )}'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 10,softMin: 0," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )} ," );
                    AxisSet |= AxisType.Free;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.AQ ) && !AxisSet.HasFlag( AxisType.AQ ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", "ParticulateMatter", "Particulate Matter", true )} (μg/m3)'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 30,softMin: 0," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )} ," );
                    AxisSet |= AxisType.AQ;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.ppm ) && !AxisSet.HasFlag( AxisType.ppm ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", "PartsPerMillion", "Parts Per Million", true )} (ppm)'}}," );
                    buf.Append( $"opposite: {opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 500,softMin: 0," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )} ," );
                    AxisSet |= AxisType.ppm;
                }

                if ( UseAddAxisCall )
                {
                    buf.AppendLine( "alignTicks: false, gridLineWidth: 0, minorGridLineWidth:0 }, false, false );" );
                }
                else
                {
                    //Use this to avoid scaling errors
                    buf.Append( "alignTicks: false," );
                    break; // Skip the other plotvars, first var now has its axis
                }
            } // For loop over all plotvars

            if ( thisChart.Axis.CountFlags() == 1 && !NoClosingAddAxis )
            {
                buf.Append( "  chart.addAxis({linkedTo: 0, gridLineWidth: 0, minorGridLineWidth:0," );

                if ( thisChart.Axis.HasFlag( AxisType.Temp ) )
                {
                    buf.Append( "labels:{align: 'left',y: -2, x: 5, formatter: function() {return '<span style=\"fill: ' + (this.value <= freezing ? 'blue' : 'red') + '; \">' + this.value + '</span>';}}," );
                    buf.Append( "plotLines:[{value: freezing,color: 'rgb(0, 0, 180)',width: 1,zIndex: 2}]," );
                }
                else
                    buf.Append( "labels:{align: 'left',y: -2, x: 5}," );

                buf.AppendLine( "opposite: true, title: {text: null} }, false, false );" );
            }
        } // CreateAxis

        #endregion

        #region GenerateJavascript Runtime Functions

        //bool RuntimeGenerated = false;
        bool SumFunctionGenerated = false;

        void GenerateSeriesVariables( StringBuilder buf, List<AllVarInfo> AllVars )
        {
            Sup.LogTraceVerboseMessage( $"Compiler - Creating Runtime Series Variables" );

            // Even if we don't need these we just generate this to make life easier: we do not have to search for it
            buf.AppendLine( $"var WindBarbData = [];" );
            buf.AppendLine( $"var sumResult = [];" );

            foreach ( AllVarInfo avi in AllVars )
                buf.AppendLine( $"var {avi.KeywordName} = [];" );

            buf.AppendLine( "" );
        }

        void GenerateSumFunction( StringBuilder buf )
        {
            Sup.LogTraceVerboseMessage( $"Compiler - Creating Runtime Sum function" );

            if ( SumFunctionGenerated )
                return;
            else
                SumFunctionGenerated = true;

            //buf.AppendLine( "// This is the Compiler Runtime for the functions" );
            buf.AppendLine( "function sum( curVal, valArray, curIndex, thisEpochDate)" );
            buf.AppendLine( "{" );
            //buf.AppendLine( "  // Fill the value in the valuearray to be plotted and restart the value on first of januari - implicit year cycle for the sum function" );
            buf.AppendLine( "  thisDate = new Date( thisEpochDate );" );
            buf.AppendLine( "  if (( thisDate.getMonth() == 0 && thisDate.getDate() == 1) || curIndex == 0 ) {" );
            //buf.AppendLine( "    console.log('!!' + thisDate + ' / ' + curVal + ' / ' + curIndex);" );
            buf.AppendLine( "    valArray.push( [ thisEpochDate, curVal ] );" );
            buf.AppendLine( "  }" );
            buf.AppendLine( "  else {" );
            //buf.AppendLine( "    console.log(thisDate + ' / ' + curVal + ' / ' + curIndex);" );
            buf.AppendLine( "    tmp = valArray[ curIndex - 1 ][ 1 ] + curVal;" );
            buf.AppendLine( "    valArray.push( [ thisEpochDate, tmp ] );" );
            buf.AppendLine( "  }" );
            buf.AppendLine( "}" );
        }

        #endregion

        #region Datagenerator
        public DateTime GenerateUserAskedData( List<ChartDef> thisList )
        {
            Sup.LogDebugMessage( $"Generating Compiler UserAskedData: Start" );

            if ( thisList?.Any() != true )
            {
                Sup.LogTraceInfoMessage( $"Generating UserAskedData: Nothing to do" );
                return DateTime.Now;
            }

            // Take the FTP frequency or the LogInterval (whichever is the largest) and use the minute value being a multiple of that one cycle below the now time as the end time
            // Then go the hours in Graphs back to complete the full cycle. 
            // So with a 10 min FTP cycle and Now = 08h09 the endtime must be 08h00 -> the minute value MOD FTP frequency
            // This should give it the same starttime as the CMX JSONS, this is relevant for the wind addition later on.
            // This is also shared with the UserAskedData JSON creation -> it has become a shared function for start and endtime related to the intervals.
            //
            Sup.SetStartAndEndForData( out DateTime timeStart, out DateTime timeEnd );
            Sup.LogTraceInfoMessage( $"GenerateUserAskedData: timeStart = {timeStart}; timeEnd = {timeEnd}" );

            StringBuilder Recent = new StringBuilder( "{" );
            StringBuilder Daily = new StringBuilder( "{" );
            StringBuilder All = new StringBuilder( "{" );

            // Make the partial MonthFilelist for the RECENT variable
            Monthfile thisMonthlist = new Monthfile( Sup );
            List<MonthfileValue> RoughList = thisMonthlist.ReadPartialMonthlyLogs( timeStart, timeEnd );
            List<MonthfileValue> MonthlyListToWriteOut = RoughList.Where( b => b.ThisDate <= timeEnd ).Where( a => a.ThisDate >= timeStart ).ToList();
            thisMonthlist.Dispose();

            // Do the ALL/DAILY only once a day

            _ = DateTime.TryParse( Sup.GetUtilsIniValue( "Compiler", "DoneToday", $"{DateTime.Now.AddDays( -1 ):s}" ), out DateTime DoneToday );

            Sup.LogTraceInfoMessage( $"Generate UserAskedData: DoneToday = {DoneToday}." );

            bool DoDailyAndAll = !Sup.DateIsToday( DoneToday );

            if ( DoDailyAndAll )
            {
                Sup.LogTraceInfoMessage( $"Generate UserAskedData: Must generate the ALL Range." );
                Sup.SetUtilsIniValue( "Compiler", "DoneToday", $"{DateTime.Now:s}" );
            }
            else
                Sup.LogTraceInfoMessage( $"Generate UserAskedData: Must NOT generate the ALL Range." );

            foreach ( ChartDef thisChart in thisList )
            {
                Sup.LogTraceInfoMessage( $"Generate UserAskedData - Loop over Chart: {thisChart.Id})" );
                foreach ( Plotvar thisVar in thisChart.PlotVars )
                {
                    Sup.LogTraceInfoMessage( $"Generate UserAskedData - Testing {thisVar.PlotVar} into {thisVar.Datafile} (Range is {thisVar.PlotvarRange})" );
                    if ( thisVar.Datafile.StartsWith( "CUserdata" ) )
                    {
                        // This is one to generate. Write out this variable. 
                        // NOTE: there can be more variables in this file so the writing is always append
                        Sup.LogTraceInfoMessage( $"Generate UserAskedData - generating {thisVar.PlotVar} into {thisVar.Datafile} (Range is {thisVar.PlotvarRange})" );
                        switch ( thisVar.PlotvarRange )
                        {
                            case PlotvarRangeType.Extra:
                            case PlotvarRangeType.Recent:
                                Recent.Append( $"\"{thisVar.PlotVar}\":[" );
                                foreach ( MonthfileValue entry in MonthlyListToWriteOut )
                                    Recent.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.Evt.ToString( "F1", CUtils.Inv )}]," );
                                Recent.Remove( Recent.Length - 1, 1 );
                                Recent.Append( $"]," );

                                break;

                            case PlotvarRangeType.Daily:
                            case PlotvarRangeType.All:
                                if ( !DoDailyAndAll )
                                    break;

                                All.Append( $"\"{thisVar.PlotVar}\":[" );

                                if ( thisVar.PlotVar.Equals( "heatingdegreedays" ) )
                                    foreach ( DayfileValue entry in CUtils.MainList )
                                        All.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.HeatingDegreeDays.ToString( "F1", CUtils.Inv )}]," );

                                else if ( thisVar.PlotVar.Equals( "coolingdegreedays" ) )
                                    foreach ( DayfileValue entry in CUtils.MainList )
                                        All.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.CoolingDegreeDays.ToString( "F1", CUtils.Inv )}]," );

                                else if ( thisVar.PlotVar.Equals( "evapotranspiration" ) )
                                    foreach ( DayfileValue entry in CUtils.MainList )
                                        All.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.EvapoTranspiration.ToString( "F1", CUtils.Inv )}]," );

                                All.Remove( All.Length - 1, 1 );
                                All.Append( $"]," );

                                break;

                            default:
                                Sup.LogTraceErrorMessage( "Generate UserAskedData - Switch default is an internal error! Must be set while parsing the charts)" );
                                break;
                        }
                    } // else: data must come from CMX
                } // foreach Plotvar
            } // foreach Chart

            // Done so cleanup and finish the Stringbuilders and write out to files
            if ( Recent.Length > 1 )
            {
                Recent.Remove( Recent.Length - 1, 1 );
                Recent.Append( "}" );
                using ( StreamWriter sw = new StreamWriter( $"{Sup.PathUtils}{Sup.CUserdataRECENT}", false, Encoding.UTF8 ) ) { sw.Write( Recent.ToString() ); }
            }

            if ( Daily.Length > 1 )
            {
                Daily.Remove( Daily.Length - 1, 1 );
                Daily.Append( "}" );
                using ( StreamWriter sw = new StreamWriter( $"{Sup.PathUtils}{Sup.CUserdataDAILY}", false, Encoding.UTF8 ) ) { sw.Write( Daily.ToString() ); }
            }

            if ( All.Length > 1 )
            {
                All.Remove( All.Length - 1, 1 );
                All.Append( "}" );
                using ( StreamWriter sw = new StreamWriter( $"{Sup.PathUtils}{Sup.CUserdataALL}", false, Encoding.UTF8 ) ) { sw.Write( All.ToString() ); }
            }

            return timeEnd;
        } // End Generate JSON

        #endregion

        #region Additional generating functions
        List<AllVarInfo> CheckAllVariablesInThisSetOfCharts( List<ChartDef> theseCharts )
        {
            List<AllVarInfo> AllVars = new List<AllVarInfo>();
            AllVarInfo tmpVarInfo = new AllVarInfo();

            bool found = false;

            // Make a list from all variables in 
            foreach ( ChartDef c in theseCharts )
                foreach ( Plotvar p in c.PlotVars )
                {
                    if ( AllVars.Count != 0 )
                    {
                        foreach ( AllVarInfo avi in AllVars )
                        {
                            if ( p.Keyword.Equals( avi.KeywordName, CUtils.Cmp ) )
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if ( !found )
                    {
                        tmpVarInfo.KeywordName = p.Keyword;
                        tmpVarInfo.TypeName = p.PlotVar;
                        tmpVarInfo.Datafile = p.Datafile;
                        AllVars.Add( tmpVarInfo );

                        Sup.LogTraceInfoMessage( $"CeckAllVariablesInThisSetOfCharts: Keyword: {tmpVarInfo.KeywordName}; Plotvar: {tmpVarInfo.TypeName}; Datafile: {tmpVarInfo.Datafile}" );
                    }
                    else
                        found = false;
                }

            found = false;

            foreach ( ChartDef c in theseCharts )
                foreach ( Plotvar p in c.PlotVars )
                    if ( p.Equation == null )
                    {
                        if ( p.GraphType == "columnrange" )
                        {
                            Sup.LogTraceInfoMessage( $"CeckAllVariablesInThisSetOfCharts: ColumnRange var {p.Keyword}" );
                            string pvSuffix = p.PlotVar.Substring( 3 );

                            tmpVarInfo.Datafile = p.Datafile;
                            tmpVarInfo.KeywordName = $"min{pvSuffix}";
                            tmpVarInfo.TypeName = $"min{pvSuffix}";
                            AllVars.Add( tmpVarInfo );
                            Sup.LogTraceInfoMessage( $"CeckAllVariablesInThisSetOfCharts: Keyword: {tmpVarInfo.KeywordName}; Plotvar: {tmpVarInfo.TypeName}; Datafile: {tmpVarInfo.Datafile}" );

                            tmpVarInfo.Datafile = p.Datafile;
                            tmpVarInfo.KeywordName = $"max{pvSuffix}";
                            tmpVarInfo.TypeName = $"max{pvSuffix}";
                            AllVars.Add( tmpVarInfo );
                            Sup.LogTraceInfoMessage( $"CeckAllVariablesInThisSetOfCharts: Keyword: {tmpVarInfo.KeywordName}; Plotvar: {tmpVarInfo.TypeName}; Datafile: {tmpVarInfo.Datafile}" );
                        }
                        else
                            continue;
                    }
                    else
                    {
                        Sup.LogTraceInfoMessage( $"CeckAllVariablesInThisSetOfCharts: Equation var {p.Keyword}" );
                        // In case of an equation with its own var name some info needs to be set for codegen to function correctly
                        //
                        if ( p.PlotvarRange == PlotvarRangeType.All || p.PlotvarRange == PlotvarRangeType.Daily )
                        {
                            PlotvarTypes = PlotvarTypesALL;
                            PlotvarKeyword = PlotvarKeywordALL;
                            Datafiles = DatafilesALL;
                        }
                        else if ( p.PlotvarRange == PlotvarRangeType.Recent ) // rangetype is RECENT
                        {
                            PlotvarTypes = PlotvarTypesRECENT;
                            PlotvarKeyword = PlotvarKeywordRECENT;
                            Datafiles = DatafilesRECENT;
                        }
                        else if ( p.PlotvarRange == PlotvarRangeType.Extra ) // rangetype is EXTRA
                        {
                            PlotvarTypes = PlotvarTypesEXTRA;
                            PlotvarKeyword = PlotvarKeywordEXTRA;
                            Datafiles = DatafilesEXTRA;
                        }
                        else
                        {
                            Sup.LogTraceInfoMessage( $"Error PlovarRangeType for {p.Keyword}: {p.PlotvarRange}" );
                            return null;
                        }

                        foreach ( string pt in PlotvarTypes )
                        {
                            string pk = PlotvarKeyword[ Array.FindIndex( PlotvarTypes, word => word.Equals( pt, CUtils.Cmp ) ) ];
                            string df = Datafiles[ Array.FindIndex( PlotvarTypes, word => word.Equals( pt, CUtils.Cmp ) ) ];

                            if ( p.Equation.Contains( pk, CUtils.Cmp ) )
                            {
                                found = false;

                                foreach ( AllVarInfo a in AllVars )
                                    if ( a.KeywordName.Equals( pk, CUtils.Cmp ) )
                                    {
                                        tmpVarInfo = a;
                                        found = true;
                                        break;
                                    }

                                if ( !found )
                                {
                                    tmpVarInfo.KeywordName = pk;
                                    tmpVarInfo.TypeName = pt;
                                    tmpVarInfo.Datafile = df;
                                    AllVars.Add( tmpVarInfo );
                                }
                                else
                                    found = false;

                                Sup.LogTraceInfoMessage( $"CeckAllVariablesInThisSetOfCharts (found: {found}): Keyword: {tmpVarInfo.KeywordName}; Plotvar: {tmpVarInfo.TypeName}; Datafile: {tmpVarInfo.Datafile}" );

                                p.EqAllVarList.Add( tmpVarInfo );
                            }
                        }
                    }

            return AllVars;
        }

        #endregion

    } // Class DefineCharts
}// Namespace
