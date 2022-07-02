/*
 * ChartsCompiler CodeGen - Part of CumulusUtils
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
 * Literature:  https://github.com/jstat/jstat
 *              https://jstat.github.io/all.html
 *              https://www.highcharts.com/docs/chart-and-series-types/chart-types
 *              https://stackoverflow.com/questions/1232040/how-do-i-empty-an-array-in-javascript
 *              
 *              
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CumulusUtils
{
    partial class ChartsCompiler
    {

        #region CodeGenerator

        internal void GenerateUserDefinedCharts( List<ChartDef> theseCharts, string filename, int UniqueOutputId )
        {
            if ( theseCharts.Count == 0 )
                return;

            StringBuilder Html = new StringBuilder();
            StringBuilder MenuJavascript = new StringBuilder();
            StringBuilder AjaxJavascript = new StringBuilder();
            StringBuilder AddSeriesJavascript = new StringBuilder();
            StringBuilder GenericJavascript = new StringBuilder();
            StringBuilder TheCharts = new StringBuilder();

            Sup.LogDebugMessage( $"Compiler - CodeGen: {filename}" );

            int HoursInGraph = Convert.ToInt32( Sup.GetCumulusIniValue( "Graphs", "GraphHours", "" ), ci );

            List<AllVarInfo> AllVars = CheckAllVariablesInThisSetOfCharts( theseCharts );
            GenerateSeriesVariables( GenericJavascript, AllVars );

            List<string> theseDatafiles = AllVars.Where( p => p.Datafile != "" ).Select( p => p.Datafile ).Distinct().ToList();
            bool TheseChartsUseWindBarbs = theseCharts.Where( p =>p.HasWindBarbs ).Any();

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{filename}", false, Encoding.UTF8 ) )
            {
                bool UseHighchartsBoostModule = Sup.GetUtilsIniValue( "Graphs", "UseHighchartsBoostModule", "true" ).Equals( "true", cmp );

                Html.AppendLine( "<!--" );
                Html.AppendLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                Html.AppendLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                Html.AppendLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                Html.AppendLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                Html.AppendLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                Html.AppendLine( "-->" );

                Html.AppendLine( Sup.GenjQueryIncludestring() );

                if ( !CMXutils.DoWebsite && CMXutils.DoLibraryIncludes )
                {
                    Html.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                }

                Html.AppendLine( "<style>" );
                Html.AppendLine( "#report{" );
                Html.AppendLine( "  font-family: arial;" );
                Html.AppendLine( "  border-radius: 15px;" );
                Html.AppendLine( "  border-spacing: 0;" );
                Html.AppendLine( "  border: 1px solid #b0b0b0;" );
                Html.AppendLine( "}" );
                Html.AppendLine( "</style>" );

                Html.AppendLine( "<div><p style='text-align:center;'>" );
                Html.AppendLine( $"<select id='graph{UniqueOutputId}'>" );

                MenuJavascript.AppendLine( $"$('#graph{UniqueOutputId}').change(function(){{CurrentChart{UniqueOutputId}=document.getElementById('graph{UniqueOutputId}').value; handleChange{UniqueOutputId}();}});" );
                MenuJavascript.AppendLine( $"function handleChange{UniqueOutputId}() {{" );
                MenuJavascript.AppendLine( $"  var w1 = document.getElementById('graph{UniqueOutputId}').value = CurrentChart{UniqueOutputId};" );

                GenericJavascript.AppendLine( "var chart, config, freezing;" );

                // Change defaults of the Charts Page / Home button in the Document Init function but only if NOT website
                GenericJavascript.Append( "$( function(){  " );

                // This complex conditional must make sure the initialisation of the chart is done and is done only once.
                //   1) !website means we are doing compileonly (modular) but that maybe for use in the CUtils website or really for another website
                //   2) CMXutils.DojQueryInclude || CMXutils.DoLibraryIncludes is used to determine compileonly is used for another website
                //   3) If UniqueOutputId > 0 the Init is not called by the runtime system and needs to be done here
                //   4) If the condition is true, the chart needs the initialisation on itself
                //      --- this must be a rewrite: initialisation should always be done from the runtime such that we know what the last chart is and that the timer can 
                //      refresh whatever chart is loaded
                if ( ( !CMXutils.DoWebsite && ( CMXutils.DojQueryInclude || CMXutils.DoLibraryIncludes ) ) || UniqueOutputId > 0 )
                    GenericJavascript.Append( "InitCumulusCharts();" );

                GenericJavascript.AppendLine( " } );" );

                // Generate InitCumulusCharts
                GenericJavascript.Append( "function InitCumulusCharts() {" );
                GenericJavascript.Append( "  ChartsType = 'compiler';" );

                if ( CMXutils.DoWebsite && UniqueOutputId == 0 )
                    GenericJavascript.AppendLine( $"  if (CurrentChart{UniqueOutputId} === 'Temp') {{" );   // Temp is the default when no compiler is used

                GenericJavascript.Append( "    ClickEventChart = [" );
                for ( int i = 0; i < 24; i++ )
                    GenericJavascript.Append( $"'{ClickEvents[ i ]}'," );
                GenericJavascript.Remove( GenericJavascript.Length - 1, 1 );
                GenericJavascript.AppendLine( "];" );
                GenericJavascript.AppendLine( "     console.log('Cumuluscharts Compiler version has been initialised');" );
                GenericJavascript.AppendLine( $"    CurrentChart{UniqueOutputId} = '{theseCharts[ 0 ].Id}';" );

                if ( CMXutils.DoWebsite && UniqueOutputId == 0 )
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
                       $"  s => {{s[ 1 ] = s[ 1 ] * {Sup.StationWind.Convert( Sup.StationWind.Dim, WindDim.ms, 1 ).ToString( "F5", CultureInfo.InvariantCulture )} }} );" +
                        "  return" +
                        "}\n" );

                GenericJavascript.AppendLine( "var compassP = function (deg) {" );
                GenericJavascript.AppendLine( "  var a = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'];" );
                GenericJavascript.AppendLine( "  return a[Math.floor((deg + 22.5) / 45) % 8];" );
                GenericJavascript.AppendLine( "};" );

                GenericJavascript.AppendLine( "function GraphconfigAjax(){" );
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
                        if ( df.StartsWith( "CUserdata" ) || df.StartsWith( "extrasensors" ) )
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
                    MenuJavascript.AppendLine( $"  if (w1=='{thisChart.Id}') {{do{thisChart.Id}()}}" );

                    // AlignTicks must be true!! (is the default of HighCharts) this also makes softMax superfluous
                    TheCharts.AppendLine( $"function do{thisChart.Id}() {{" );
                    TheCharts.AppendLine( $"  console.log('Creating chart: {thisChart.Title}');" );

                    TheCharts.AppendLine( "  chart = Highcharts.StockChart('chartcontainer', { title: {" );
                    TheCharts.Append( $" text: '{thisChart.Title}'" );
                    if ( thisChart.HasWindBarbs && !thisChart.WindBarbsBelow ) TheCharts.Append( ", margin: 35" );
                    TheCharts.AppendLine("},");

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
                        TheCharts.AppendLine( "      plotOptions: { scatter: {cursor: 'pointer',enableMouseTracking: false,boostThreshold: 200,marker:{states:{hover:{enabled: false},select:{enabled: false},normal:{enabled: false}}}," +
                          "shadow: false,label:{enabled: false}}}," );
                    else
                        TheCharts.AppendLine( "      plotOptions: { series: {turboThreshold: 0, states: { hover: { halo: { size: 5,opacity: 0.25} } },marker: { enabled: false, states: { hover: { enabled: true, radius: 0.1} } } }, }," );

                    TheCharts.AppendLine( "      tooltip: {shared: true,split: true, valueDecimals: 1, xDateFormat: '%A, %b %e, %H:%M'}," );
                    TheCharts.AppendLine( "      series:[]," );

                    if ( thisChart.Range == PlotvarRangeType.Recent || thisChart.Range == PlotvarRangeType.Extra )
                    {
                        TheCharts.AppendLine( "      rangeSelector:{" );

                        if ( thisChart.HasWindBarbs && !thisChart.WindBarbsBelow ) TheCharts.AppendLine( "    floating: true, y: -50," );

                        TheCharts.AppendLine( "      buttons:[{" );
                        TheCharts.AppendLine( $"       count: {HoursInGraph / 4},type: 'hour',text: '{HoursInGraph / 4}h'}}, {{" );
                        TheCharts.AppendLine( $"       count: {HoursInGraph / 2},type: 'hour',text: '{HoursInGraph / 2}h'}}, {{" );
                        TheCharts.AppendLine( "        type: 'all',text: 'All'}]," );
                        TheCharts.AppendLine( "      inputEnabled: false}" );
                    }
                    else
                    {
                        if ( thisChart.Range == PlotvarRangeType.Daily )
                            TheCharts.AppendLine( "      rangeSelector:{allButtonsEnabled: true, selected: 0}" );
                        else
                            TheCharts.AppendLine( "      rangeSelector:{allButtonsEnabled: true, selected: 4}" );
                    }

                    TheCharts.AppendLine( "  });" );
                    TheCharts.AppendLine( "  chart.showLoading();" );
                    //TheCharts.AppendLine( $"  chart.setTitle();" );

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
                                    if ( sumExpr.Contains( avi.KeywordName, cmp ) )
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
                                    if ( tmpEquation.Contains( avi.KeywordName, cmp ) )
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
                            if ( thisPlotvar.PlotvarRange == PlotvarRangeType.Extra )
                            {
                                string tmp = Sup.GetCUstringValue( "Compiler", thisPlotvar.Keyword, "", true );
                                AddSeriesJavascript.AppendLine( $"    name:'{Sup.GetCUstringValue( "Compiler", thisPlotvar.Keyword, thisPlotvar.Keyword, true )}'," );
                            }
                            else
                                AddSeriesJavascript.AppendLine( $"    name:'{Sup.GetCUstringValue( "Compiler", thisPlotvar.Keyword, thisPlotvar.Keyword, true )}'," );

                            AddSeriesJavascript.AppendLine( $"    id:'{thisPlotvar.Keyword}'," );
                            AddSeriesJavascript.AppendLine( $"    data: {thisPlotvar.Keyword}," );
                        }

                        if ( thisPlotvar.GraphType == "area" )
                            AddSeriesJavascript.AppendLine( $"    fillOpacity: '{thisPlotvar.Opacity.ToString( "F1", ci )}'," );


                        AddSeriesJavascript.AppendLine( $"    color: '{thisPlotvar.Color}'," );
                        AddSeriesJavascript.AppendLine( $"    yAxis: '{thisPlotvar.AxisId}'," );
                        AddSeriesJavascript.AppendLine( $"    type: '{thisPlotvar.GraphType}'," );
                        AddSeriesJavascript.AppendLine( $"    lineWidth: {thisPlotvar.LineWidth}," );
                        AddSeriesJavascript.AppendLine( $"    zIndex: {thisPlotvar.zIndex}," );
                        AddSeriesJavascript.AppendLine( $"    tooltip:{{valueSuffix: ' {thisPlotvar.Unit}'}}" );
                        AddSeriesJavascript.AppendLine( "   }, false);" );

                        Sup.LogTraceInfoMessage( $"Compiler - CodeGen: {filename} Written the Series {thisPlotvar.Keyword}" );

                    } // Loop over all plotvars within the chart

                    if ( thisChart.HasWindBarbs )
                    {
                        AddSeriesJavascript.AppendLine( "  thisChart.addSeries({ " );
                        AddSeriesJavascript.AppendLine( "    name: 'WindBarbs'," );
                        AddSeriesJavascript.AppendLine( "    xAxis: 1," );
                        AddSeriesJavascript.AppendLine( $"    color: '{thisChart.WindBarbColor}'," );
                        AddSeriesJavascript.AppendLine( "    type: 'windbarb'," );
                        AddSeriesJavascript.AppendLine( "    visible: true," );
                        AddSeriesJavascript.AppendLine( "    tooltip:{valueSuffix: ' m/s'}," );
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
                Html.AppendLine( $"<div id='chartcontainer'  " +
                    $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'> </div>" );
                Html.AppendLine( $" <p style='text-align:center;font-size:11px;'>Generated with the ChartsCompiler {CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                Html.AppendLine( "</div>" ); // #Report
                Html.AppendLine( "<script>" );

                Html.AppendLine( MenuJavascript.ToString() );
                Html.AppendLine( GenericJavascript.ToString() );
                Html.AppendLine( AjaxJavascript.ToString() );
                Html.AppendLine( AddSeriesJavascript.ToString() );
                Html.AppendLine( TheCharts.ToString() );

#if !RELEASE
                of.WriteLine( Html );
#else
                of.WriteLine( CuSupport.StringRemoveWhiteSpace( Html.ToString(), " " ) );
#endif

                of.WriteLine( "</script>" );

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

            if ( AxisSet.Equals( AxisType.None ) )
                NoClosingAddAxis = true;

            // Check if Rain/RainRate are the only Plotvars in this chart in which case the horizontal gridlines will be set for that axis
            // If any other Plotvar is present, the horizontal gridlines will be invisible (width = 0)
            // The reason of this detail is that the rain value for the inches is so small that the max value of 1 for the axis can not be done automatically
            // and the "endOnTick: false, showLastLabel: true," have to be used. This confuses the gridlines such that they have to be turned off UNLESS these are the only axis
            // in the chart **SIGH, what a world**
            // If there will be more of this type of shit I will have to move it to a dedicated function with the strings as in/out parameters
            bool RainInChart = false, RRateInChart = false;
            string RainGridLinesVisble = "", RRateGridLinesVisible = "";

            foreach ( Plotvar thisPlotvar in thisChart.PlotVars )
            {
                if ( !thisPlotvar.Axis.HasFlag( AxisType.Rain ) && !thisPlotvar.Axis.HasFlag( AxisType.Rrate ) )
                {
                    RainGridLinesVisble = "gridLineWidth:0, minorGridLineWidth:0";
                    RRateGridLinesVisible = "gridLineWidth:0, minorGridLineWidth:0";
                    RainInChart = RRateInChart = false;

                    break;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Rain ) ) { RainInChart = true; }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Rrate ) ) { RRateInChart = true; }
            }

            // We only have rain data in the chart either Rain or RRate
            if ( RainInChart || RRateInChart )
            {
                if ( RainInChart && RRateInChart )                                                          // Both Rain and RRate
                {
                    RainGridLinesVisble = "gridLineWidth:1, minorGridLineWidth:1";
                    RRateGridLinesVisible = "gridLineWidth:0, minorGridLineWidth:0";
                }
                else if ( RainInChart ) RainGridLinesVisble = "gridLineWidth:1, minorGridLineWidth:1";     // Just Rain in the chart
                else if ( RRateInChart ) RRateGridLinesVisible = "gridLineWidth:1, minorGridLineWidth:1";  // Just RRate in the chart
            }

            // Now do  the actual Axis settings
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
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2" : "labels:{align: 'right',x: -5, y: -2" )}" +
                      $",formatter: function() {{return '<span style=\"fill: ' + (this.value <= freezing ? 'blue' : 'red') + '; \">' + this.value + '</span>';}} }}," );
                    buf.Append( "plotLines:[{value: freezing,color: 'rgb(0, 0, 180)',width: 1,zIndex: 2}]" );
                    AxisSet |= AxisType.Temp;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Pressure ) && !AxisSet.HasFlag( AxisType.Pressure ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Pressure", "Pressure", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"softMin: {MinPressure}, softMax: {MaxPressure}," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Pressure;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Rain ) && !AxisSet.HasFlag( AxisType.Rain ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Rain", "Rain", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"endOnTick: false, showLastLabel: true, {RainGridLinesVisble}, softMax: 1,min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Rain;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Rrate ) && !AxisSet.HasFlag( AxisType.Rrate ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Rainrate", "Rain Rate", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"endOnTick: false, showLastLabel: true, {RRateGridLinesVisible}, softMax: 1,min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Rrate;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Wind ) && !AxisSet.HasFlag( AxisType.Wind ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Wind", "Wind", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Wind;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Direction ) && !AxisSet.HasFlag( AxisType.Direction ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Direction", "Direction", true )} (Compass / degrees)'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "min: 0, max: 360," );
                    buf.Append( "tickInterval: 45," ); // align: 'right',x: -5,
                    buf.Append( $"labels: {{ {( opposite ? "align: 'left',x: 5,y: -2" : "align: 'right',x: -5, y: -2" )}, formatter: function() {{return compassP(this.value);}} }}," );
                    buf.Append( "allowDecimals: false" );
                    AxisSet |= AxisType.Direction;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.UV ) && !AxisSet.HasFlag( AxisType.UV ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "UVindex", "UV index", true )}'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.UV;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Solar ) && !AxisSet.HasFlag( AxisType.Solar ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "SolarRadiation", "Solar Radiation", true )} (W/m²)'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"softMax: {ApproximateSolarMax() + 150},min: 0," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Solar;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Humidity ) && !AxisSet.HasFlag( AxisType.Humidity ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Humidity", "Humidity", true )} (%)'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "min: 0, max: 100," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Humidity;
                } // End of block generatiing the Exis info
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Hours ) && !AxisSet.HasFlag( AxisType.Hours ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "General", "Hours", "Hours", true )}'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( "min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}" : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Hours;
                } // End of block generatiing the Exis info
                else if ( thisPlotvar.Axis.HasFlag( AxisType.EVT ) && !AxisSet.HasFlag( AxisType.EVT ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "Evapotranspiration", "Evapotranspiration", true )} ({thisPlotvar.Unit})'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 1,min: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.EVT;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.DegreeDays ) && !AxisSet.HasFlag( AxisType.DegreeDays ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Website", "DegreeDays", "DegreeDays", true )}'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 10,softMin: 0," );
                    buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.DegreeDays;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.Free ) && !AxisSet.HasFlag( AxisType.Free ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", $"{thisChart.Id}Dimensionless", "Dimensionless", true )}'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 10,softMin: 0," );
                    //buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.Free;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.AQ ) && !AxisSet.HasFlag( AxisType.AQ ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", "ParticulateMatter", "Particulate Matter", true )} (μg/m3)'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 30,softMin: 0," );
                    //buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.AQ;
                }
                else if ( thisPlotvar.Axis.HasFlag( AxisType.ppm ) && !AxisSet.HasFlag( AxisType.ppm ) )
                {
                    buf.Append( $"title:{{text:'{Sup.GetCUstringValue( "Compiler", "PartsPerMillion", "Parts Per Million", true )} (ppm)'}}," );
                    buf.Append( $"opposite: { opposite.ToString().ToLowerInvariant()}," );
                    buf.Append( $"softMax: 500,softMin: 0," );
                    //buf.Append( "allowDecimals: false," );
                    buf.Append( $"{( opposite ? "labels:{align: 'left',x: 5,y: -2}," : "labels:{align: 'right',x: -5, y: -2}" )}" );
                    AxisSet |= AxisType.ppm;
                }

                if ( UseAddAxisCall )
                {
                    buf.AppendLine( "}, false, false );" );
                }
                else
                    break; // Skip the other plotvars, first var now has its axis

            } // For loop over all plotvars

            if ( thisChart.Axis.CountFlags() == 1 && !NoClosingAddAxis )
            {
                buf.Append( "  chart.addAxis({linkedTo: 0, gridLineWidth: 0," );

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
        internal void GenerateUserAskedData( List<ChartDef> thisList )
        {
            Sup.LogDebugMessage( $"Generating Compiler UserAskedData: Start" );

            if ( thisList?.Any() != true )
            {
                Sup.LogTraceInfoMessage( $"Generating UserAskedData: Nothing to do" );
                return;
            }

            // From AirlinkLog:
            // This is shared with GraphSolar... should become some common function to get this interval from the Cumulus.ini file
            // Also shared with ChartsCompiler... have to clean this up and do a shared function somewhere
            // Will do at some time.
            int[] PossibleIntervals = { 1, 5, 10, 15, 20, 30 };

            int LogIntervalInMinutes = PossibleIntervals[ Convert.ToInt32( Sup.GetCumulusIniValue( "Station", "DataLogInterval", "" ), CultureInfo.InvariantCulture ) ];
            int FTPIntervalInMinutes = Convert.ToInt32( Sup.GetCumulusIniValue( "FTP site", "UpdateInterval", "" ) );
            double HoursInGraph = Convert.ToDouble( Sup.GetCumulusIniValue( "Graphs", "GraphHours", "" ) );
            int DaysInGraph = Convert.ToInt32( Sup.GetCumulusIniValue( "Graphs", "ChartMaxDays", "" ) );

            // From AirlinkLog:
            // Take the FTP frequency or the LogInterval (whichever is the largest) and use the minute value being a multiple of that one cycle below the now time as the end time
            // Then go the hours in Graphs back to complete the full cycle. 
            // So with a 10 min FTP cycle and Now = 08h09 the endtime must be 08h00 -> the minute value MOD FTP frequency
            // This should give it the same starttime as the CMX JSONS, this is relevant for the wind addition later on.
            // This is also shared with the ChartsCompiler -> make some shared function for start and endtime related to the intervals.
            //
            DateTime Now = DateTime.Now;
            DateTime timeEnd = Now.AddMinutes( -Now.Minute % Math.Max( FTPIntervalInMinutes, LogIntervalInMinutes ) );
            DateTime timeStart = timeEnd.AddHours( -HoursInGraph );

            StringBuilder Recent = new StringBuilder( "{" );
            StringBuilder Daily = new StringBuilder( "{" );
            StringBuilder All = new StringBuilder( "{" );

            // Make the partial MonthFilelist for the RECENT variable
            Monthfile thisMonthlist = new Monthfile( Sup );
            List<MonthfileValue> RoughList = thisMonthlist.ReadPartialMonthlyLogs( timeStart, timeEnd );
            List<MonthfileValue> MonthlyListToWriteOut = RoughList.Where( b => b.ThisDate <= timeEnd ).Where( a => a.ThisDate >= timeStart ).ToList();
            thisMonthlist.Dispose();

            // Do the ALL/DAILY only once a day

            DateTime.TryParse( Sup.GetUtilsIniValue( "Compiler", "DoneToday", $"{Now.AddDays( -1 ):dd/MM/yy}" ), out DateTime DoneToday );
            bool DoDailyAndAll = !Sup.DateIsToday( DoneToday );

            if ( DoDailyAndAll )
            {
                Sup.LogTraceInfoMessage( $"Generate UserAskedData: Must generate the ALL Range." );
                Sup.SetUtilsIniValue( "Compiler", "DoneToday", $"{Now:dd/MM/yy}" );
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
                                    Recent.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.Evt.ToString( "F1", ci )}]," );
                                Recent.Remove( Recent.Length - 1, 1 );
                                Recent.Append( $"]," );

                                break;

                            case PlotvarRangeType.Daily:
                            case PlotvarRangeType.All:
                                if ( !DoDailyAndAll )
                                    break;

                                All.Append( $"\"{thisVar.PlotVar}\":[" );

                                if ( thisVar.PlotVar.Equals( "heatingdegreedays" ) )
                                    foreach ( DayfileValue entry in CMXutils.MainList )
                                        All.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.HeatingDegreeDays.ToString( "F1", ci )}]," );

                                else if ( thisVar.PlotVar.Equals( "coolingdegreedays" ) )
                                    foreach ( DayfileValue entry in CMXutils.MainList )
                                        All.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.CoolingDegreeDays.ToString( "F1", ci )}]," );

                                else if ( thisVar.PlotVar.Equals( "evapotranspiration" ) )
                                    foreach ( DayfileValue entry in CMXutils.MainList )
                                        All.Append( $"[{CuSupport.DateTimeToJS( entry.ThisDate )},{entry.EvapoTranspiration.ToString( "F1", ci )}]," );

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
                            if ( p.Keyword.Equals( avi.KeywordName, cmp ) )
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
                            string pk = PlotvarKeyword[ Array.FindIndex( PlotvarTypes, word => word.Equals( pt, cmp ) ) ];
                            string df = Datafiles[ Array.FindIndex( PlotvarTypes, word => word.Equals( pt, cmp ) ) ];

                            if ( p.Equation.Contains( pk, cmp ) )
                            {
                                found = false;

                                foreach ( AllVarInfo a in AllVars )
                                    if ( a.KeywordName.Equals( pk, cmp ) )
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

            //foreach ( AllVarInfo v in AllVars )
            //{
            //    Sup.LogTraceInfoMessage( $"AllVars KeywordName: {v.KeywordName}" );
            //    Sup.LogTraceInfoMessage( $"AllVars TypeName: {v.TypeName}" );
            //    Sup.LogTraceInfoMessage( $"AllVars Datafile: {v.Datafile}" );
            //}

            return AllVars;
        }

        #endregion

    } // Class DefineCharts
}// Namespace
