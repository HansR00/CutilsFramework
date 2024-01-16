/*
 * Graphs - Part of CumulusUtils
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#if TIMING
using System.Diagnostics;
#endif

namespace CumulusUtils
{
    partial class Graphx : IDisposable
    {
        #region Declarations

        private readonly bool GraphDailyRain,
            GraphMonthlyTemperature,
            GraphMonthlyRain,
            GraphHeatmap,
            GraphYearTempStats,
            GraphYearMonthTempStats,
            GraphYearRainStats,
            GraphYearMonthRainStats,
            GraphWarmerDays,
            GraphWindRose,
            GraphWindrun,
            GraphSolarHours,
            GraphYearMonthSolarHoursStats,
            GraphSolarEnergy,
            GraphYearMonthSolarEnergyStats,
            GraphTempSum,
            GraphGrowingDegreeDays,
            GraphSeasons,
            GraphDailyEVT,
            GraphMonthlyEVT,
            GraphRAINvsEVT,
            GraphAverageClash;

        private enum Months : int { Jan = 1, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec };

        // Is this one really necessary???
        private readonly string[] m = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };  //Culture independent, just strings to compare in the menu

        private enum DayType
        { Zero, Plus25Day, Plus30Day, Plus35Day, Plus40Day };

        // source : 
        // Version 2 : private const string graphColors = "['#4363d8', '#e6194B', '#f58231', '#ffe119', '#bfef45', '#3cb44b',  '#42d4f4', '#911eb4','#f032e6', '#fabed4', '#469990', '#dcbeff', '#9A6324', '#800000', '#aaffc3', '#808000', '#ffd8b1', '#000075']";
        // Version 1 : private const string graphColors = "['darkcyan', 'crimson', 'cyan', 'blue', 'green', 'yellow', 'red', 'blueviolet', 'chartreuse', 'coral', 'cornflowerblue', 'darkblue', 'darkgreen']";
        // Version 3 : private const string graphColors = "['#FF0000', '#00FF00', '#0000FF', '#FFFF00', '#FF00FF', '#00FFFF', '#000000', '#800000', '#008000', '#000080', '#808000', '#800080', '#008080', '#808080', " +
        //  "'#C00000', '#00C000', '#0000C0', '#C0C000', '#C000C0', '#00C0C0', '#C0C0C0', '#400000', '#004000', '#000040', '#404000', '#400040', '#004040', '#404040', '#200000', '#002000', '#000020', " +
        //  "'#202000', '#200020', '#002020', '#202020', '#600000', '#006000', '#000060', '#606000', '#600060', '#006060', '#606060', '#A00000', '#00A000', '#0000A0', '#A0A000', '#A000A0', '#00A0A0', " +
        //  "'#A0A0A0', '#E00000', '#00E000', '#0000E0', '#E0E000', '#E000E0', '#00E0E0', '#E0E0E0']";
        // Version 4: private const string graphColors = "['#058DC7', '#50B432', '#ED561B', '#DDDF00', '#24CBE5', '#64E572', '#FF9655', '#FFF263', '#6AF9C4']";
        // Below are the defaults from the template and for the time being that will be default from the app as well 
        private const string graphColors = "['#058DC7', '#50B432', '#ED561B', '#ed9b00', '#24CBE5', '#64E572', '#FF9655', '#FFF263', '#6AF9C4']";

        // make sure the utils dir exists
        private readonly CuSupport Sup;
        private readonly float maxTemp;
        private readonly float minTemp;
        private readonly float maxRain;

        public static bool UseHighchartsBoostModule;

        private bool StationNormal, StationAverage;
        private string NormalUsage;
        private float NOAARainNormYearAv = 0.0F;
        private float StationRainYearAv = 0.0F;
        private float MaxYearlyRainAlltime = 0.0F;
        private readonly int YearMax;
        private readonly int YearMin;
        private readonly bool SplitHeatmapPages;
        private readonly int HeatmapNrOfYearsPerPage;

        private readonly int maxNrOfSeriesVisibleInGraph;

        // Define the monthlist here 
        //   initialised in the Graphx initialiser 
        //   to be used in any graph when required
        //   disposed when Graphx is disposed
        //
        readonly Monthfile thisMonthfile;

        #endregion

        #region Constructor
        public Graphx( List<DayfileValue> thisList, CuSupport s )
        {
            Sup = s;

            #region Graph & Menu Parameters

            GraphDailyRain = Sup.GetUtilsIniValue( "Graphs", "DailyRain", "true" ).Equals( "true", CUtils.Cmp );
            GraphMonthlyRain = Sup.GetUtilsIniValue( "Graphs", "MonthlyRain", "true" ).Equals( "true", CUtils.Cmp );
            GraphYearRainStats = Sup.GetUtilsIniValue( "Graphs", "YearRainstats", "true" ).Equals( "true", CUtils.Cmp );
            GraphYearMonthRainStats = Sup.GetUtilsIniValue( "Graphs", "YearMonthRainstats", "true" ).Equals( "true", CUtils.Cmp );
            GraphRAINvsEVT = Sup.GetUtilsIniValue( "Graphs", "RAINvsEVT", "true" ).Equals( "true", CUtils.Cmp );

            CUtils.HasRainGraphMenu = GraphDailyRain || GraphMonthlyRain || GraphYearMonthRainStats || GraphYearRainStats;

            GraphMonthlyTemperature = Sup.GetUtilsIniValue( "Graphs", "MonthlyTemp", "true" ).Equals( "true", CUtils.Cmp );
            GraphYearTempStats = Sup.GetUtilsIniValue( "Graphs", "YearTempstats", "true" ).Equals( "true", CUtils.Cmp );
            GraphYearMonthTempStats = Sup.GetUtilsIniValue( "Graphs", "YearMonthTempstats", "true" ).Equals( "true", CUtils.Cmp );
            GraphWarmerDays = Sup.GetUtilsIniValue( "Graphs", "WarmerDays", "true" ).Equals( "true", CUtils.Cmp );
            GraphHeatmap = Sup.GetUtilsIniValue( "Graphs", "HeatMap", "true" ).Equals( "true", CUtils.Cmp );

            CUtils.HasTempGraphMenu = GraphMonthlyTemperature || GraphYearTempStats || GraphYearMonthTempStats || GraphHeatmap || GraphWarmerDays;

            GraphWindrun = Sup.GetUtilsIniValue( "Graphs", "Windrun", "true" ).Equals( "true", CUtils.Cmp );
            GraphWindRose = Sup.GetUtilsIniValue( "Graphs", "WindRose", "true" ).Equals( "true", CUtils.Cmp );

            CUtils.HasWindGraphMenu = GraphWindRose || GraphWindrun;

            if ( CUtils.HasSolar )
            {
                GraphSolarHours = Sup.GetUtilsIniValue( "Graphs", "SolarHours", "true" ).Equals( "true", CUtils.Cmp );
                GraphYearMonthSolarHoursStats = Sup.GetUtilsIniValue( "Graphs", "SolarHoursYearMonth", "true" ).Equals( "true", CUtils.Cmp );
                GraphSolarEnergy = Sup.GetUtilsIniValue( "Graphs", "SolarEnergy", "true" ).Equals( "true", CUtils.Cmp );
                GraphYearMonthSolarEnergyStats = Sup.GetUtilsIniValue( "Graphs", "SolarEnergyYearMonth", "true" ).Equals( "true", CUtils.Cmp );

                CUtils.HasSolarGraphMenu = GraphSolarHours || GraphSolarEnergy || GraphYearMonthSolarHoursStats || GraphYearMonthSolarEnergyStats;
            }
            else
            {
                GraphSolarHours = GraphSolarEnergy = GraphYearMonthSolarHoursStats = GraphYearMonthSolarEnergyStats = CUtils.HasSolarGraphMenu = false;
            }

            GraphTempSum = Sup.GetUtilsIniValue( "Graphs", "TempSum", "true" ).Equals( "true", CUtils.Cmp );
            GraphGrowingDegreeDays = Sup.GetUtilsIniValue( "Graphs", "GrowingDegreeDays", "true" ).Equals( "true", CUtils.Cmp );
            GraphSeasons = Sup.GetUtilsIniValue( "Graphs", "Seasons", "false" ).Equals( "true", CUtils.Cmp );
            GraphDailyEVT = Sup.GetUtilsIniValue( "Graphs", "DailyEVT", "true" ).Equals( "true", CUtils.Cmp );
            GraphMonthlyEVT = Sup.GetUtilsIniValue( "Graphs", "MonthlyEVT", "true" ).Equals( "true", CUtils.Cmp );
            GraphAverageClash = Sup.GetUtilsIniValue( "Graphs", "AverageClash", "false" ).Equals( "true", CUtils.Cmp ); // So default is false!!!

            CUtils.HasMiscGraphMenu = GraphTempSum || GraphGrowingDegreeDays || GraphSeasons || GraphDailyEVT || GraphMonthlyEVT || GraphRAINvsEVT || GraphAverageClash;

            #endregion

            #region Chart global values init

            YearMax = thisList.Select( x => x.ThisDate.Year ).Max();
            YearMin = thisList.Select( x => x.ThisDate.Year ).Min();
            maxTemp = thisList.Select( x => x.MaxTemp ).Max();
            minTemp = thisList.Select( x => x.MinTemp ).Min();
            maxRain = thisList.Select( x => x.TotalRainThisDay ).Max();

            maxNrOfSeriesVisibleInGraph = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "MaxNrOfSeriesVisibileInGraph", "2" ), CUtils.Inv );

            GenerateNOAAparameters( thisList );

            HeatmapNrOfYearsPerPage = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "HeatmapNumberOfYearsPerPage", "10" ), CUtils.Inv );
            SplitHeatmapPages = ( YearMax - YearMin + 1 ) > HeatmapNrOfYearsPerPage;

            // For windrose
            NrOfCompassSectors = Convert.ToInt32( Sup.GetCumulusIniValue( "Display", "NumWindRosePoints", "" ), CUtils.Inv );
            CompassSector = (float) ( 360.0 / (float) NrOfCompassSectors );
            HalfCompassSector = (float) ( CompassSector / 2.0 );

            NrOfWindforceClasses = Convert.ToSingle( Sup.GetUtilsIniValue( "Graphs", "WindRoseNrOfWindforceClasses", "6" ), CUtils.Inv );
            SpeedForRoseInterval = Convert.ToSingle( Sup.GetUtilsIniValue( "Graphs", "WindRoseMaxWindSpeed", "60" ), CUtils.Inv ) / NrOfWindforceClasses;

            // For WindRun
            WindrunClassWidth = Convert.ToInt32( Sup.GetUtilsIniValue( "Graphs", "WindrunClassWidth", "75" ), CUtils.Inv );

            MaxWindrun = (int) thisList.Select( x => x.TotalWindRun ).Max();
            NrofWindrunClasses = MaxWindrun / WindrunClassWidth + 1;
            WindrunClasses = new int[ NrofWindrunClasses ];   // { 100, 200, 300, 400, 500, 600, 700}; // in km, other unist need conversion

            for ( int i = 0; i < NrofWindrunClasses; i++ )
                WindrunClasses[ i ] = WindrunClassWidth + WindrunClassWidth * i;

            // For scatter graph
            UseHighchartsBoostModule = Sup.GetUtilsIniValue( "Graphs", "UseHighchartsBoostModule", "true" ).Equals( "true", CUtils.Cmp );
            if ( UseHighchartsBoostModule ) Sup.LogTraceInfoMessage( "Graphx: Using Highcharts Boost Module!" );

            // Just the initialisation. The reading is done once when the list is asked.
            thisMonthfile = new Monthfile( Sup );

            #endregion

            return;
        }

        #endregion


        public void GenerateGraphx( List<DayfileValue> ThisList )
        {
            Sup.LogDebugMessage( "CumulusUtils : starting Graphx" );

            StringBuilder thisBuffer = new StringBuilder();

            #region Rain
            if ( CUtils.HasRainGraphMenu && ( !CUtils.Thrifty || ThisList.Last().TotalRainThisDay > 0.0 || CUtils.RunStarted.DayOfYear % CUtils.ThriftyRainGraphsPeriod == 0 ) )
            {
                // Rain has been detected so it is worth to update the Rain graphs
                CUtils.ThriftyRainGraphsDirty = true;

                Sup.LogTraceInfoMessage( "Graphs : Start Rain section" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.GraphsRainOutputFilename}", false, Encoding.UTF8 ) )
                {
                    thisBuffer.Clear();

                    int GraphNr = 1, GraphNrForYearMonthRainStats = 0;

                    of.WriteLine( "<!--" );
                    of.WriteLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                    of.WriteLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                    of.WriteLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                    of.WriteLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                    of.WriteLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                    of.WriteLine( "-->" );

                    thisBuffer.AppendLine( $"{Sup.GenjQueryIncludestring()}" );

                    if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                    {
                        thisBuffer.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                    }

                    thisBuffer.AppendLine( "<script>" );

                    thisBuffer.AppendLine( "$(function() {" );
                    thisBuffer.AppendLine( "  $('#graph').change(function() {" );
                    thisBuffer.AppendLine( "    handleChange();" );
                    thisBuffer.AppendLine( "  });" );
                    thisBuffer.AppendLine( "  handleChange();" );
                    thisBuffer.AppendLine( "});" );

                    thisBuffer.AppendLine( "function handleChange()" );
                    thisBuffer.AppendLine( "{" );
                    thisBuffer.AppendLine( "  $('[id*=\"YMR\"]').hide();" );
                    thisBuffer.AppendLine( "  var w1 = document.getElementById(\"graph\").value;" );
                    thisBuffer.AppendLine( $"  if (w1 == 'DailyRain') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'MonthlyRain') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'YearRainstatistics') {{ graph{GraphNr++}(); }}" );
                    GraphNrForYearMonthRainStats = GraphNr;
                    thisBuffer.AppendLine( $"  if (w1 == 'YearMonthRainstatistics') {{ $('[id*=\"YMR\"]').show(); graph{GraphNr++}{CUtils.RunStarted.Month}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'RAINvsEVT') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( "<style>" );
                    thisBuffer.AppendLine( "#report{" );
                    thisBuffer.AppendLine( "  text-align: center;" );
                    thisBuffer.AppendLine( "  font-family: arial;" );
                    thisBuffer.AppendLine( "  border-radius: 15px;" );
                    thisBuffer.AppendLine( "  border-spacing: 0;" );
                    thisBuffer.AppendLine( "  border: 1px solid #b0b0b0;" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( $"{Sup.HighchartsAllowBackgroundImage()}" );
                    thisBuffer.AppendLine( "</style>" );

                    thisBuffer.AppendLine( "<div>" ); // class=\"content\"
                    thisBuffer.AppendLine( "<p>" );
                    thisBuffer.AppendLine( "  <select id='graph'>" );

                    if ( GraphDailyRain )
                        thisBuffer.AppendLine( $"    <option value='DailyRain' selected>{Sup.GetCUstringValue( "Graphs", "DRMenuText", "Daily Rain", false )}</option>" );
                    if ( GraphMonthlyRain )
                        thisBuffer.AppendLine( $"    <option value='MonthlyRain'>{Sup.GetCUstringValue( "Graphs", "MRMenuText", "Monthly Rain", false )}</option>" );
                    if ( GraphYearRainStats )
                        thisBuffer.AppendLine( $"    <option value='YearRainstatistics'>{Sup.GetCUstringValue( "Graphs", "YSRMenuText", "Yearly Rain statistics", false )}</option>" );
                    if ( GraphYearMonthRainStats )
                        thisBuffer.AppendLine( $"    <option value='YearMonthRainstatistics'>{Sup.GetCUstringValue( "Graphs", "YMSRMenuText", "Yearly Rain statistics per Month", false )}</option>" );
                    if ( GraphRAINvsEVT )
                        thisBuffer.AppendLine( $"    <option value='RAINvsEVT'>{Sup.GetCUstringValue( "Graphs", "RainvsEVTMenuText", "Rain vs EVT", false )}</option>" );

                    thisBuffer.AppendLine( " </select>" );
                    thisBuffer.AppendLine( "</p>" );
                    thisBuffer.AppendLine( "</div>" );

                    thisBuffer.AppendLine( "<div id='report'>" );
                    thisBuffer.AppendLine( "<br/>" );

                    thisBuffer.AppendLine( "<p>" );
                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        thisBuffer.AppendLine( $"<input type='button' id='YMR{thisMonth}' value='{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( thisMonth )}' onclick='graph{GraphNrForYearMonthRainStats}{thisMonth}()'>" );
                    thisBuffer.AppendLine( "</p>" );

                    thisBuffer.AppendLine( $"  <div id='chartcontainer' " +
                        $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'></div>" );

                    if ( !CUtils.DoWebsite )
                    {
                        thisBuffer.AppendLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                    }

                    GenerateLogarithmicHandlerScript( thisBuffer );

                    GraphNr = 1; // reset the numbering for the generation of the code

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphDailyRain )
                    {
                        GenDailyRainGraphData( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "DailyRain" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "DailyRain", Title: Sup.GetCUstringValue( "Graphs", "DRTitle", "Daily Rain", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphMonthlyRain )
                    {
                        GenMonthlyRainvsNOAAGraphData( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "MonthlyRain" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "MonthlyRain", Title: Sup.GetCUstringValue( "Graphs", "MRTitle", "Daily Rain", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphYearRainStats )
                    {
                        GenerateYearRainStatistics( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyRainStats" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyRainStats", Title: Sup.GetCUstringValue( "Graphs", "YRSTitle", "Daily Rain", true ) ) );

                    // GenerateYearMonthTempStatistics counts as one graph so GraphNr will not be incremented
                    // Only when the whole series has been generated, so after the for-loop
                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                    {
                        thisBuffer.AppendLine( "<script>" );
                        thisBuffer.AppendLine( $"function graph{GraphNr}{thisMonth}()" );
                        thisBuffer.AppendLine( "{" );
                        if ( GraphYearMonthRainStats )
                        {
                            GenerateYearMonthRainStatistics( ThisList, (Months) thisMonth, thisBuffer );
                            thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyMonthlyRainStats" ) );
                        }
                        thisBuffer.AppendLine( "}" );
                        thisBuffer.AppendLine( "</script>" );
                    }
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyMonthlyRainStats", Title: Sup.GetCUstringValue( "Graphs", "YMRSTitle", "Daily Rain", true ) ) );

                    GraphNr++;

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphRAINvsEVT )
                    {
                        GenDailyRAINvsEVT( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "RAINvsEVT" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "RAINvsEVT", Title: Sup.GetCUstringValue( "Graphs", "RAINvsEVTTitle", "RAIN vs EVT", true ) ) );

                    thisBuffer.AppendLine( "<br/>" );
                    thisBuffer.AppendLine( "</div>" ); // #report

#if !RELEASE
                    of.WriteLine( thisBuffer );
#else
                    of.WriteLine( CuSupport.StringRemoveWhiteSpace( thisBuffer.ToString(), " " ) );
#endif

                } // Using RainGraphsOutputFile
            } // Rain Graphs
            #endregion rain

            #region Temp

            if ( CUtils.HasTempGraphMenu && ( !CUtils.Thrifty || CUtils.RunStarted.DayOfYear % CUtils.ThriftyTempGraphsPeriod == 0 ) )
            {
                // Temp graphs are written
                CUtils.ThriftyTempGraphsDirty = true;

                Sup.LogTraceInfoMessage( "Graphs : Start Temp section" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.GraphsTempOutputFilename}", false, Encoding.UTF8 ) )
                {
                    thisBuffer.Clear();

                    int GraphNr = 1, GraphNrForYearMonthTempStats = 0;

                    of.WriteLine( "<!--" );
                    of.WriteLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                    of.WriteLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                    of.WriteLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                    of.WriteLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                    of.WriteLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                    of.WriteLine( "-->" );

                    of.WriteLine( $"{Sup.GenjQueryIncludestring()}" );

                    if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                    {
                        thisBuffer.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                    }

                    thisBuffer.AppendLine( "<script>" );

                    //Sup.LogTraceInfoMessage( "GraphsTest : Temp graphs after the libraries" );
                    //Sup.LogTraceInfoMessage( $"GraphsTest : Starting the main chart script. DoWebsite = {CUtils.DoWebsite}" );

                    if ( !CUtils.DoWebsite )
                    {
                        //Sup.LogTraceInfoMessage( "GraphsTest : starting with DayNumber2Date" );
                        // When DoWebsite==true this function is in cumulusutils.js
                        thisBuffer.AppendLine(
                                  "function DayNumber2Date(dayNumber, year){" +
                                  "  const date = new Date( year, 0, dayNumber );" +
                                  $"  return date.toLocaleDateString('{Sup.Locale}');" +
                                  "}" );
                    }

                    thisBuffer.AppendLine( "$(function() {" );
                    thisBuffer.AppendLine( "  $('#graph').change(function() {" );
                    thisBuffer.AppendLine( "    handleChange();" );
                    thisBuffer.AppendLine( "  });" );
                    thisBuffer.AppendLine( "  handleChange();" );
                    thisBuffer.AppendLine( "});" );

                    thisBuffer.AppendLine( "function handleChange()" );
                    thisBuffer.AppendLine( "{" );
                    thisBuffer.AppendLine( "  $('[id*=\"YMT\"]').hide(); $('[id^=\"Heatmap\"]').hide();" );
                    thisBuffer.AppendLine( "  var w1 = document.getElementById(\"graph\").value;" );
                    thisBuffer.AppendLine( $"  if (w1 == 'MonthlyTemp') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'YearTempstatistics') {{ graph{GraphNr++}(); }}" );
                    GraphNrForYearMonthTempStats = GraphNr;
                    thisBuffer.AppendLine( $"  if (w1 == 'YearMonthTempstatistics') {{ $('[id*=\"YMT\"]').show(); graph{GraphNr++}{CUtils.RunStarted.Month}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'WarmerDays') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'Heatmap') {{ $('[id*=\"Heatmap\"]').show(); graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( "<style>" );
                    thisBuffer.AppendLine( "#report{" );
                    thisBuffer.AppendLine( "  text-align: center;" );
                    thisBuffer.AppendLine( "  font-family: arial;" );
                    thisBuffer.AppendLine( "  border-radius: 15px;" );
                    thisBuffer.AppendLine( "  border-spacing: 0;" );
                    thisBuffer.AppendLine( "  border: 1px solid #b0b0b0;" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( $"{Sup.HighchartsAllowBackgroundImage()}" );
                    thisBuffer.AppendLine( "</style>" );

                    thisBuffer.AppendLine( "<div>" ); // class=\"content\"
                    thisBuffer.AppendLine( "<br/>" );
                    thisBuffer.AppendLine( "<p>" );
                    thisBuffer.AppendLine( "  <select id='graph'>" );

                    if ( GraphMonthlyTemperature )
                        thisBuffer.AppendLine( $"    <option value='MonthlyTemp' selected>{Sup.GetCUstringValue( "Graphs", "MTMenuText", "Monthly Temperatures", false )}</option>" );
                    if ( GraphYearTempStats )
                        thisBuffer.AppendLine( $"    <option value='YearTempstatistics'>{Sup.GetCUstringValue( "Graphs", "YSTMenuText", "Yearly Temperature statistics", false )}</option>" );
                    if ( GraphYearMonthTempStats )
                        thisBuffer.AppendLine( $"    <option value='YearMonthTempstatistics'>{Sup.GetCUstringValue( "Graphs", "YMSTMenuText", "Yearly Temperature statistics per Month", false )}</option>" );
                    if ( GraphWarmerDays )
                        thisBuffer.AppendLine( $"    <option value='WarmerDays'>{Sup.GetCUstringValue( "Graphs", "WDMenuText", "Warmer Days", false )}</option>" );
                    if ( GraphHeatmap )
                        thisBuffer.AppendLine( $"    <option value='Heatmap'>{Sup.GetCUstringValue( "Graphs", "HMMenuText", "Heat Map", false )}</option>" );

                    thisBuffer.AppendLine( " </select>" );
                    thisBuffer.AppendLine( "</p>" );
                    thisBuffer.AppendLine( "</div>" );

                    thisBuffer.AppendLine( "<div id='report'>" );

                    thisBuffer.AppendLine( "<p>" );
                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                        thisBuffer.AppendLine( $"<input type='button' id='YMT{thisMonth}' value='{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( thisMonth )}' onclick='graph{GraphNrForYearMonthTempStats}{thisMonth}()'>" );

                    if ( SplitHeatmapPages )
                    {
                        thisBuffer.AppendLine( $"<input type='button' id='HeatmapPrev' value='Prev page' onclick='HeatmapPrev()'>" );
                        thisBuffer.AppendLine( $"<input type='button' id='HeatmapAll' value='All Years' onclick='HeatmapAll()'>" );
                        thisBuffer.AppendLine( $"<input type='button' id='HeatmapNext' value='Next page' onclick='HeatmapNext()'>" );
                    }

                    thisBuffer.AppendLine( "</p>" );

                    thisBuffer.AppendLine( $"  <div id='chartcontainer' " +
                        $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'></div>" );

                    if ( !CUtils.DoWebsite )
                    {
                        thisBuffer.AppendLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                    }

                    GraphNr = 1; // reset the numbering for the generation of the code

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphMonthlyTemperature )
                    {
                        GenMonthlyTempvsNOAAGraphData( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "MonthlyTemp" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "MonthlyTemp", Title: Sup.GetCUstringValue( "Graphs", "MTTitle", "Monthly Average Temperature", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphYearTempStats )
                    {
                        GenerateYearTempStatistics( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyTempStats" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyTempStats", Title: Sup.GetCUstringValue( "Graphs", "YTSTitle", "Year Temperature Statistics", true ) ) );

                    // GenerateYearMonthTempStatistics counts as one graph so GraphNr will not be incremented
                    // Only when the whole series has been generated, so after the for-loop
                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                    {
                        thisBuffer.AppendLine( "<script>" );
                        thisBuffer.AppendLine( $"function graph{GraphNr}{thisMonth}()" );
                        thisBuffer.AppendLine( "{" );
                        if ( GraphYearMonthTempStats )
                        {
                            GenerateYearMonthTempStatistics( ThisList, (Months) thisMonth, thisBuffer );
                            thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyMonthlyTempStats" ) );
                        }
                        thisBuffer.AppendLine( "}" );
                        thisBuffer.AppendLine( "</script>" );
                    }
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyMonthlyTempStats", Title: Sup.GetCUstringValue( "Graphs", "YMTSTitle", "Year Temperature Statistics for", true ) ) );

                    GraphNr++;

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphWarmerDays )
                    {
                        GenStackedWarmDaysGraphData( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "WarmerDays" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "WarmerDays", Title: Sup.GetCUstringValue( "Graphs", "WDTitle", "Warmer Days", true ) ) );

                    thisBuffer.AppendLine( "<script>" );

                    if ( SplitHeatmapPages )
                    {
                        thisBuffer.AppendLine( "var thisHeatmap;" );
                        thisBuffer.AppendLine( "function HeatmapPrev() {" );
                        thisBuffer.AppendLine( "  let minValue = thisHeatmap.yAxis[0].min;" );

                        thisBuffer.AppendLine( $"  if (minValue - {HeatmapNrOfYearsPerPage} <= {YearMin}) minValue = {YearMin};" );
                        thisBuffer.AppendLine( $"  else minValue -= {HeatmapNrOfYearsPerPage};" );
                        thisBuffer.AppendLine( $"  let maxValue = minValue + {HeatmapNrOfYearsPerPage};" );

                        thisBuffer.AppendLine( "   thisHeatmap.yAxis[0].update({min: minValue - 0.5, max: maxValue + 0.5}, true);" );
                        thisBuffer.AppendLine( "}\n" );

                        thisBuffer.AppendLine( "function HeatmapAll() {" );
                        thisBuffer.AppendLine( $"   thisHeatmap.yAxis[0].update({{min: {YearMin} - 0.5, max: {YearMax} + 0.5}}, true);" );
                        thisBuffer.AppendLine( "}\n" );

                        thisBuffer.AppendLine( "function HeatmapNext() {" );
                        thisBuffer.AppendLine( "  let maxValue = thisHeatmap.yAxis[0].max;" );

                        thisBuffer.AppendLine( $"  if (maxValue + {HeatmapNrOfYearsPerPage} >= {YearMax}) maxValue = {YearMax};" );
                        thisBuffer.AppendLine( $"  else maxValue += {HeatmapNrOfYearsPerPage};" );
                        thisBuffer.AppendLine( $"  let minValue = maxValue - {HeatmapNrOfYearsPerPage};" );

                        thisBuffer.AppendLine( "   thisHeatmap.yAxis[0].update({min: minValue - 0.5, max: maxValue + 0.5}, true);" );
                        thisBuffer.AppendLine( "}\n" );
                    }

                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphHeatmap )
                    {
                        GenerateHeatMap( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "HeatMap" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "HeatMap", Title: Sup.GetCUstringValue( "Graphs", "HMTitle", "Heat Map", true ) ) );

                    thisBuffer.AppendLine( "<br/>" );
                    thisBuffer.AppendLine( "</div>" ); // #report

#if !RELEASE
                    of.WriteLine( thisBuffer );
#else
                    of.WriteLine( CuSupport.StringRemoveWhiteSpace( thisBuffer.ToString(), " " ) );
#endif

                } // Using TempGraphsOutputFile
            } // Temp Graphs
            #endregion

            #region Wind
            if ( CUtils.HasWindGraphMenu && ( !CUtils.Thrifty || CUtils.RunStarted.DayOfYear % CUtils.ThriftyWindGraphsPeriod == 0 ) )
            {
                // Temp graphs are written
                CUtils.ThriftyWindGraphsDirty = true;

                Sup.LogTraceInfoMessage( "Graphs : Start Wind section" );

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.GraphsWindOutputFilename}", false, Encoding.UTF8 ) )
                {
                    thisBuffer.Clear();

                    int GraphNr = 1;

                    of.WriteLine( "<!--" );
                    of.WriteLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                    of.WriteLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                    of.WriteLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                    of.WriteLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                    of.WriteLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                    of.WriteLine( "-->" );

                    thisBuffer.AppendLine( $"{Sup.GenjQueryIncludestring()}" );

                    if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                    {
                        thisBuffer.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                    }

                    thisBuffer.AppendLine( "<script>" );

                    thisBuffer.AppendLine( "$(function() {" );
                    thisBuffer.AppendLine( "  $('#graph').change(function() {" );
                    thisBuffer.AppendLine( "    handleChange();" );
                    thisBuffer.AppendLine( "  });" );
                    thisBuffer.AppendLine( $"  {( GraphWindRose ? "$('#WindRun').hide(); $('#WindRose').show();" : "$('#WindRun').show(); $('#WindRose').hide();" )}" );
                    thisBuffer.AppendLine( "  handleChange();" );
                    thisBuffer.AppendLine( "});" );

                    thisBuffer.AppendLine( "function handleChange()" );
                    thisBuffer.AppendLine( "{" );
                    thisBuffer.AppendLine( "  var w1 = $('#graph option:selected').val();" );
                    thisBuffer.AppendLine( "  if (w1 == 'WindRose') { $('#WindRun').hide(); $('#WindRose').show(); $('#yearRose').trigger('change');}" );
                    thisBuffer.AppendLine( "  if (w1 == 'Windrun') { $('#WindRose').hide(); $('#WindRun').show(); $('#yearRun').trigger('change');}" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( "<style>" );
                    thisBuffer.AppendLine( "#report{" );
                    thisBuffer.AppendLine( "  text-align: center;" );
                    thisBuffer.AppendLine( "  font-family: arial;" );
                    thisBuffer.AppendLine( "  border-radius: 15px;" );
                    thisBuffer.AppendLine( "  border-spacing: 0;" );
                    thisBuffer.AppendLine( "  border: 1px solid #b0b0b0;" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( $"{Sup.HighchartsAllowBackgroundImage()}" );
                    thisBuffer.AppendLine( "</style>" );

                    thisBuffer.AppendLine( "<div>" ); // class=\"content\"
                    thisBuffer.AppendLine( "  <select id='graph'>" );

                    if ( GraphWindRose )
                        thisBuffer.AppendLine( $"    <option value='WindRose' selected>{Sup.GetCUstringValue( "Graphs", "WindRoseMenuText", "WindRose", false )}</option>" );
                    if ( GraphWindrun )
                        thisBuffer.AppendLine( $"    <option value='Windrun' {( GraphWindRose ? "" : "selected" )}>{Sup.GetCUstringValue( "Graphs", "WindRunMenuText", "Windrun", false )}</option>" );

                    thisBuffer.AppendLine( "  </select>" );
                    thisBuffer.AppendLine( "</div>" );

                    Sup.LogTraceInfoMessage( "Graphs : Start writing HTML Style and Menu." );

                    thisBuffer.AppendLine( "<div id='report'>" );
                    thisBuffer.AppendLine( "<br/>" );

                    // do this conditional to prevent issues with WindRun
                    if ( GraphWindRose )
                    {
                        thisBuffer.AppendLine( "<div id='WindRose'>" );
                        GenerateWindRosePrevNextYearMonthMenu( ThisList, thisBuffer );
                        thisBuffer.AppendLine( "</div>" );
                    }

                    thisBuffer.AppendLine( "<div id='WindRun'>" );
                    GenerateWindRunPrevNextYearMenu( thisBuffer );
                    thisBuffer.AppendLine( "</div>" );

                    thisBuffer.AppendLine( $"<div id='chartcontainer' " +
                        $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'></div>" );

                    if ( !CUtils.DoWebsite )
                    {
                        thisBuffer.AppendLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                    }

                    GraphNr = 1;

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );

                    if ( GraphWindRose )  // The setting in the inifile
                    {
                        List<MonthfileValue> thisList;

#if TIMING
                        Stopwatch watch = Stopwatch.StartNew();
#endif

                        // The MonthfileMainlist is created atr the start of graphs
                        // Check in the Monthfile Class itself whether the list has already been created because it may have been asked
                        // already. If the list exists, just pass it on, otherwise really physically read the files
                        // Don't dispose of the Monthlist, it may be usefull somewhere else
                        thisList = thisMonthfile.ReadMonthlyLogs();
                        GenerateWindRose( thisList, thisBuffer );
                        //thisBuffer.AppendLine( ActivateChartInfo( chartId: "WindRose" ) );  // This has to be done somewhere else.... Later

#if TIMING
                        watch.Stop();
                        Sup.LogTraceInfoMessage( $"Timing of WindRose generation = {watch.ElapsedMilliseconds} ms" );
#endif
                    }
                    else
                    {
                        thisBuffer.AppendLine( "}" );
                        Sup.LogTraceWarningMessage( $"RealMain: No Windrose generation because of cumulusutils.ini value of Windrose: {Sup.GetUtilsIniValue( "Graphs", "WindRose", "true" )}" );
                    }

                    // 1-9-2020 : Revised the whole of the Wind Graphs and the following is one of the consequences.
                    // In this specific case the closing accolade has already been written by the WindRose function because of the data arrays folliwng the function
                    // Maybe I'll put the data arrays before the function to make sure the  generation is consistent over all functions.

                    thisBuffer.AppendLine( "</script>" );

                    // Wait for the Info text event procedure to be placed correctly.
                    //thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "WindRose", Title: Sup.GetCUstringValue( "Graphs", "WindRose", "Wind Rose", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graphAllYears()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphWindrun )
                    {
                        GenerateWindrunStatistics( ThisList, thisBuffer, 0 );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "WindRun" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "WindRun", Title: Sup.GetCUstringValue( "Graphs", "WindRunTitle", "Wind Run", true ) ) );

                    for ( int i = YearMin; i <= YearMax; i++ )
                    {
                        thisBuffer.AppendLine( "<script>" );
                        thisBuffer.AppendLine( $"function graph{i}()" );
                        thisBuffer.AppendLine( "{" );
                        if ( GraphWindrun )
                        {
                            GenerateWindrunStatistics( ThisList, thisBuffer, i );
                            thisBuffer.AppendLine( ActivateChartInfo( chartId: "WindRun" ) );
                        }
                        thisBuffer.AppendLine( "}" );
                        thisBuffer.AppendLine( "</script>" );
                    }

                    thisBuffer.AppendLine( "<br/>" );
                    thisBuffer.AppendLine( "</div>" );  // of the #report style

#if !RELEASE
                    of.WriteLine( thisBuffer );
#else
                    of.WriteLine( CuSupport.StringRemoveWhiteSpace( thisBuffer.ToString(), " " ) );
#endif

                }
            } // Using WindGraphsOutputFile

            #endregion

            #region Solar

            if ( CUtils.HasSolar && CUtils.HasSolarGraphMenu && ( !CUtils.Thrifty || CUtils.RunStarted.DayOfYear % CUtils.ThriftySolarGraphsPeriod == 0 ) )
            {
                // Solar graphs are written
                CUtils.ThriftySolarGraphsDirty = true;
                List<MonthfileValue> thisMonthList;

                Sup.LogDebugMessage( "Graphs : Start Solar section" );

                // The MonthfileMainlist is created at the start of graphs
                // Check in the Monthfile Class itself whether the list has already been created because it may have been asked
                // already. If the list exists, just pass it on, otherwise really physically read the files
                // Don't dispose of the Monthlist, it may be usefull somewhere else
                thisMonthList = thisMonthfile.ReadMonthlyLogs();

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.GraphsSolarOutputFilename}", false, Encoding.UTF8 ) )
                {
                    thisBuffer.Clear();

                    int GraphNr = 1;
                    int GraphNrForYearMonthSolarHoursStats = 0;
                    int GraphNrForYearMonthSolarEnergyStats = 0;

                    of.WriteLine( "<!--" );
                    of.WriteLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                    of.WriteLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                    of.WriteLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                    of.WriteLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                    of.WriteLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                    of.WriteLine( "-->" );

                    thisBuffer.AppendLine( $"{Sup.GenjQueryIncludestring()}" );

                    if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                    {
                        thisBuffer.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                    }

                    thisBuffer.AppendLine( "<script>" );

                    thisBuffer.AppendLine( "$(function() {" );
                    thisBuffer.AppendLine( "  $('#graph').change(function() {" );
                    thisBuffer.AppendLine( "    handleChange();" );
                    thisBuffer.AppendLine( "  });" );
                    thisBuffer.AppendLine( "  handleChange();" );
                    thisBuffer.AppendLine( "});" );

                    thisBuffer.AppendLine( "function handleChange()" );
                    thisBuffer.AppendLine( "{" );
                    thisBuffer.AppendLine( "  $('[id*=\"YMSH\"]').hide();" );
                    thisBuffer.AppendLine( "  $('[id*=\"YMSE\"]').hide();" );
                    thisBuffer.AppendLine( "  var w1 = $('#graph option:selected').val();" );
                    thisBuffer.AppendLine( $"  if (w1 == 'SolarHoursStats') {{ graph{GraphNr++}(); }}" );
                    GraphNrForYearMonthSolarHoursStats = GraphNr;
                    thisBuffer.AppendLine( $"  if (w1 == 'YearMonthSolarHoursStats') {{ $('[id*=\"YMSH\"]').show(); graph{GraphNr++}{CUtils.RunStarted.Month}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'InsolationStats') {{ graph{GraphNr++}(); }}" );
                    GraphNrForYearMonthSolarEnergyStats = GraphNr;
                    thisBuffer.AppendLine( $"  if (w1 == 'YearMonthInsolationStats') {{ $('[id*=\"YMSE\"]').show(); graph{GraphNr++}{CUtils.RunStarted.Month}(); }}" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( "<style>" );
                    thisBuffer.AppendLine( "#report{" );
                    thisBuffer.AppendLine( "  text-align: center;" );
                    thisBuffer.AppendLine( "  font-family: arial;" );
                    thisBuffer.AppendLine( "  border-radius: 15px;" );
                    thisBuffer.AppendLine( "  border-spacing: 0;" );
                    thisBuffer.AppendLine( "  border: 1px solid #b0b0b0;" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( $"{Sup.HighchartsAllowBackgroundImage()}" );
                    thisBuffer.AppendLine( "</style>" );

                    thisBuffer.AppendLine( "<div>" ); // class=\"content\"
                    thisBuffer.AppendLine( "<p> " );
                    thisBuffer.AppendLine( "  <select id='graph'>" );

                    if ( GraphSolarHours )
                        thisBuffer.AppendLine( $"    <option value='SolarHoursStats' selected>{Sup.GetCUstringValue( "Graphs", "YSHStatsMenuText", "Solar Hours Statistics", false )}</option>" );
                    if ( GraphYearMonthSolarHoursStats )
                        thisBuffer.AppendLine( $"    <option value='YearMonthSolarHoursStats'>{Sup.GetCUstringValue( "Graphs", "YMSHStatsMenuText", "Monthly Solar Hours Statistics per year", false )}</option>" );
                    if ( GraphSolarEnergy )
                        thisBuffer.AppendLine( $"    <option value='InsolationStats'>{Sup.GetCUstringValue( "Graphs", "YSEStatsMenuText", "Insolation Statistics", false )}</option>" );
                    if ( GraphYearMonthSolarEnergyStats )
                        thisBuffer.AppendLine( $"    <option value='YearMonthInsolationStats'>{Sup.GetCUstringValue( "Graphs", "YMSEStatsMenuText", "Monthly Insolation Statistics per year", false )}</option>" );

                    thisBuffer.AppendLine( "  </select>" );
                    thisBuffer.AppendLine( "</p>" );
                    thisBuffer.AppendLine( "</div>" );

                    Sup.LogDebugMessage( "Graphs : Start writing HTML Style and Menu." );

                    thisBuffer.AppendLine( "<div id='report'>" );
                    thisBuffer.AppendLine( "<br/>" );

                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                    {
                        thisBuffer.AppendLine( $"<input type='button' id='YMSH{thisMonth}' value='{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( thisMonth )}' onclick='graph{GraphNrForYearMonthSolarHoursStats}{thisMonth}()'>" );
                        thisBuffer.AppendLine( $"<input type='button' id='YMSE{thisMonth}' value='{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( thisMonth )}' onclick='graph{GraphNrForYearMonthSolarEnergyStats}{thisMonth}()'>" );
                    }

                    thisBuffer.AppendLine( $"<div id='chartcontainer' " +
                        $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'></div>" );

                    if ( !CUtils.DoWebsite )
                    {
                        thisBuffer.AppendLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                    }

                    CreateListsFromMonthlyLogs( thisMonthList );  // Only once and reused

                    GraphNr = 1;

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphSolarHours )
                    {
                        GenerateYearSolarHoursStatistics( thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlySolarHRSstats" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlySolarHRSstats", Title: Sup.GetCUstringValue( "Graphs", "YSHSTitle", "Solar Hours Statistics per year", true ) ) );

                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                    {
                        thisBuffer.AppendLine( "<script>" );
                        thisBuffer.AppendLine( $"function graph{GraphNr}{thisMonth}()" );
                        thisBuffer.AppendLine( "{" );
                        if ( GraphYearMonthSolarHoursStats )
                        {
                            GenerateYearMonthSolarHoursStatistics( (Months) thisMonth, thisBuffer );
                            thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyMonthlySolarHRSstats" ) );
                        }
                        thisBuffer.AppendLine( "}" );
                        thisBuffer.AppendLine( "</script>" );
                    }
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyMonthlySolarHRSstats", Title: Sup.GetCUstringValue( "Graphs", "YMSHSTitle", "Yearly Solar Hours Statistics per month for", true ) ) );

                    GraphNr++;

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphSolarEnergy )
                    {
                        GenerateYearSolarEnergyStatistics( thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyInsolationStats" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyInsolationStats", Title: Sup.GetCUstringValue( "Graphs", "YSESTitle", "Insolation Statistics per year", true ) ) );

                    for ( int thisMonth = 1; thisMonth <= 12; thisMonth++ )
                    {
                        thisBuffer.AppendLine( "<script>" );
                        thisBuffer.AppendLine( $"function graph{GraphNr}{thisMonth}()" );
                        thisBuffer.AppendLine( "{" );
                        if ( GraphYearMonthSolarEnergyStats )
                        {
                            GenerateYearMonthSolarEnergyStatistics( (Months) thisMonth, thisBuffer );
                            thisBuffer.AppendLine( ActivateChartInfo( chartId: "YearlyMonthlyInsolationStats" ) );
                        }
                        thisBuffer.AppendLine( "}" );
                        thisBuffer.AppendLine( "</script>" );
                    }
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "YearlyMonthlyInsolationStats", Title: Sup.GetCUstringValue( "Graphs", "YMSESTitle", "Monthly Insolation Statistics per year for", true ) ) );

                    thisBuffer.AppendLine( "<br/>" );
                    thisBuffer.AppendLine( "</div>" );  // of the #report style

#if !RELEASE
                    of.WriteLine( thisBuffer );
#else
                    of.WriteLine( CuSupport.StringRemoveWhiteSpace( thisBuffer.ToString(), " " ) );
#endif

                }
            } // Using SolarGraphsOutputFile

            #endregion

            #region Misc

            if ( CUtils.HasMiscGraphMenu && ( !CUtils.Thrifty || CUtils.RunStarted.DayOfYear % CUtils.ThriftyMiscGraphsPeriod == 0 ) )
            {
                // Temp graphs are written
                CUtils.ThriftyMiscGraphsDirty = true;

                using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.GraphsMiscOutputFilename}", false, Encoding.UTF8 ) )
                {
                    thisBuffer.Clear();

                    int GraphNr = 1;

                    of.WriteLine( "<!--" );
                    of.WriteLine( $" This file is generated as part of CumulusUtils - {DateTime.Now}" );
                    of.WriteLine( " This header must not be removed and the user must comply to the Creative Commons 4.0 license" );
                    of.WriteLine( " The license conditions imply the non-commercial use of HighCharts for which the user is held responsible" );
                    of.WriteLine( $" © Copyright 2019 - {DateTime.Now:yyyy} Hans Rottier <hans.rottier@gmail.com>" );
                    of.WriteLine( " See also License conditions of CumulusUtils: https://meteo-wagenborgen.nl/" );
                    of.WriteLine( "-->" );

                    thisBuffer.AppendLine( $"{Sup.GenjQueryIncludestring()}" );

                    if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                    {
                        thisBuffer.AppendLine( Sup.GenHighchartsIncludes().ToString() );
                    }

                    thisBuffer.AppendLine( "<script>" );

                    if ( !CUtils.DoWebsite )
                    {
                        // When DoWebsite==true this function is in cumulusutils.js
                        thisBuffer.AppendLine(
                                  "function DayNumber2Date(dayNumber, year){" +
                                  "  const date = new Date( year, 0, dayNumber );" +
                                  $"  return date.toLocaleDateString('{Sup.Locale}');" +
                                  "}" );
                    }

                    thisBuffer.AppendLine( "$(function() {" );
                    thisBuffer.AppendLine( "  $('#graph').change(function() {" );
                    thisBuffer.AppendLine( "    handleChange();" );
                    thisBuffer.AppendLine( "  });" );
                    thisBuffer.AppendLine( "  handleChange();" );
                    thisBuffer.AppendLine( "});" );

                    thisBuffer.AppendLine( "function handleChange()" );
                    thisBuffer.AppendLine( "{" );
                    thisBuffer.AppendLine( "  var w1 = document.getElementById(\"graph\").value;" );
                    thisBuffer.AppendLine( $"  if (w1 == 'TempSum') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'GrowingDegreeDays') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'Seasons') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'DailyEVT') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'MonthlyEVT') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( $"  if (w1 == 'AverageClash') {{ graph{GraphNr++}(); }}" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( "<style>" );
                    thisBuffer.AppendLine( "#report{" );
                    thisBuffer.AppendLine( "  text-align: center;" );
                    thisBuffer.AppendLine( "  font-family: arial;" );
                    thisBuffer.AppendLine( "  border-radius: 15px;" );
                    thisBuffer.AppendLine( "  border-spacing: 0;" );
                    thisBuffer.AppendLine( "  border: 1px solid #b0b0b0;" );
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( $"{Sup.HighchartsAllowBackgroundImage()}" );
                    thisBuffer.AppendLine( "</style>" );

                    thisBuffer.AppendLine( "<div>" ); // class=\"content\"
                    thisBuffer.AppendLine( "<p>" );
                    thisBuffer.AppendLine( "  <select id='graph'>" );

                    if ( GraphTempSum )
                        thisBuffer.AppendLine( $"    <option value=\"TempSum\">{Sup.GetCUstringValue( "Graphs", "TempSumMenuText", "Temperature Sum", false )}</option>" );
                    if ( GraphGrowingDegreeDays )
                        thisBuffer.AppendLine( $"    <option value=\"GrowingDegreeDays\">{Sup.GetCUstringValue( "Graphs", "GrowingdegreedaysMenuText", "Growing Degree Days", false )}</option>" );
                    if ( GraphSeasons )
                        thisBuffer.AppendLine( $"    <option value=\"Seasons\">{Sup.GetCUstringValue( "Graphs", "SeasonsMenuText", "Seasons", false )}</option>" );
                    if ( GraphDailyEVT )
                        thisBuffer.AppendLine( $"    <option value=\"DailyEVT\">{Sup.GetCUstringValue( "Graphs", "DEVTMenuText", "Daily EVT", false )}</option>" );
                    if ( GraphMonthlyEVT )
                        thisBuffer.AppendLine( $"    <option value=\"MonthlyEVT\">{Sup.GetCUstringValue( "Graphs", "MEVTMenuText", "Monthly EVT", false )}</option>" );
                    if ( GraphAverageClash )
                        thisBuffer.AppendLine( $"    <option value=\"AverageClash\">{Sup.GetCUstringValue( "Graphs", "ACMenuText", "Clash of Averages", false )}</option>" );

                    thisBuffer.AppendLine( " </select>" );
                    thisBuffer.AppendLine( "</p>" );
                    thisBuffer.AppendLine( "</div>" );

                    thisBuffer.AppendLine( "<div id='report'>" );
                    thisBuffer.AppendLine( "<br/>" );

                    thisBuffer.AppendLine( $"<div id='chartcontainer' " +
                        $"style='min-height:{Convert.ToInt32( Sup.GetUtilsIniValue( "General", "ChartContainerHeight", "650" ) )}px;margin-top: 10px;margin-bottom: 5px;'></div>" );

                    if ( !CUtils.DoWebsite )
                    {
                        thisBuffer.AppendLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                    }

                    GraphNr = 1; // reset the numbering for the generation of the code

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphTempSum )
                    {
                        GenTempSum( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "TempSum" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "TempSum", Title: Sup.GetCUstringValue( "Graphs", "TempSumTitle", "Temperature Sum", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphGrowingDegreeDays )
                    {
                        GenGrowingDegreeDays( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "GrowingDegreeDays" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "GrowingDegreeDays", Title: Sup.GetCUstringValue( "Graphs", "GrowingDegreeDaysTitle", "Growing Degree Days", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphSeasons )
                    {
                        YearlySeasons( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "ThermalSeasons" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "ThermalSeasons", Title: Sup.GetCUstringValue( "Graphs", "YearlySeasonsTitle", "Yearly Thermal Seasons", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphDailyEVT )
                    {
                        GenDailyEVTGraphData( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "DailyEVT" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "DailyEVT", Title: Sup.GetCUstringValue( "Graphs", "EVTTitle", "Daily EVT", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphMonthlyEVT )
                    {
                        GenMonthlyEVTGraphData( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "MonthlyEVT" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "MonthlyEVT", Title: Sup.GetCUstringValue( "Graphs", "MEVTTitle", "Monthly EVT", true ) ) );

                    thisBuffer.AppendLine( "<script>" );
                    thisBuffer.AppendLine( $"function graph{GraphNr++}()" );
                    thisBuffer.AppendLine( "{" );
                    if ( GraphAverageClash )
                    {
                        GenerateClashOfAverages( ThisList, thisBuffer );
                        thisBuffer.AppendLine( ActivateChartInfo( chartId: "ClashOfAverages" ) );
                    }
                    thisBuffer.AppendLine( "}" );
                    thisBuffer.AppendLine( "</script>" );
                    thisBuffer.AppendLine( GenerateChartInfoModal( chartId: "ClashOfAverages", Title: Sup.GetCUstringValue( "Graphs", "ClashOfAverages", "Clash of Averages", true ) ) );

                    thisBuffer.AppendLine( "<br/>" );
                    thisBuffer.AppendLine( "</div>" ); // #report

#if !RELEASE
                    of.WriteLine( thisBuffer );
#else
                    of.WriteLine( CuSupport.StringRemoveWhiteSpace( thisBuffer.ToString(), " " ) );
#endif
                } // Using MiscGraphsOutputFile
            } // Misc Graphs

            #endregion

            #region ChartInfoFuncs

            string ActivateChartInfo( string chartId )
            {
                chartId = "HT_" + chartId;

                if ( string.IsNullOrEmpty( CUtils.ChartHelp.GetHelpText( chartId ) ) )
                {
                    return ""; // No helptext present so do nothing
                }

                StringBuilder tmp = new StringBuilder();
                string Info = $"{Sup.GetCUstringValue( "General", "Info", "Info", true )}";

                tmp.AppendLine( "chart.update({" );
                tmp.AppendLine( "  chart:{events:{render() {const {x,y,width} = this.exportingGroup.getBBox();" );

                tmp.AppendLine( "  if ( !this.customText ){" ); // Create a customText if it doesn't exist
                tmp.AppendLine( $"    this.customText = this.renderer.text( '{Info}', x - width - 15, y + 15 )" );
                tmp.AppendLine( "      .add()" +
                    ".css({color: this.title.styles.color})" +
                    ".css({cursor: 'pointer'})" +
                    $".on('click', () => $('#{chartId}').modal( 'show') );" );
                tmp.AppendLine( "  } else {" ); // Update the label position on render event (i.e on window resize)
                tmp.AppendLine( "    this.customText.attr({x: x - width - 15, y: y + 15}); } } } } });" );

                return tmp.ToString();
            }

            string GenerateChartInfoModal( string chartId, string Title )
            {
                chartId = "HT_" + chartId;

                if ( string.IsNullOrEmpty( CUtils.ChartHelp.GetHelpText( chartId ) ) )
                {
                    return ""; // No helptext present so do nothing
                }

                StringBuilder tmp = new StringBuilder();

                if ( !CUtils.DoWebsite && CUtils.DoLibraryIncludes )
                {
                    // Use the jQuery modal, by setting the DoLibraryIncludes to false the user has control whether or not to use the 
                    // supplied includes or do it all by her/himself
                    tmp.AppendLine(
                        $"<div class='modal' id='{chartId}' style='font-family: Verdana, Geneva, Tahoma, sans-serif;font-size: 120%;'>" +
                        "      <div>" +
                        $"        <h5 class='modal-title'>{Title}</h5>" +
                        "      </div>" +
                        "      <div style='text-align: left;'>" +
                        $"        {CUtils.ChartHelp.GetHelpText( chartId )}" +
                        "      </div>" +
                        "</div>" );
                }
                else
                {
                    // Use the bootstrap modal --- tabindex='-1' 
                    tmp.AppendLine( $"<div class='modal fade' id='{chartId}' role='dialog' aria-hidden='true'>" +
                    "  <div class='modal-dialog modal-dialog-centered modal-dialog modal-lg' role='document'>" +
                    "    <div class='modal-content'>" +
                    "      <div class='modal-header'>" +
                    $"        <h5 class='modal-title'>{Title}</h5>" +
                    "        <button type='button' class='close' data-bs-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" +
                    "      </div>" +
                    "      <div class='modal-body text-start'>" +
                    $"       {CUtils.ChartHelp.GetHelpText( chartId )}" +
                    "      </div>" +
                    "      <div class='modal-footer'>" +
                    $"       <button type='button' class='btn btn-secondary' data-bs-dismiss='modal'>{Sup.GetCUstringValue( "Website", "Close", "Close", false )}</button>" +
                    "      </div>" +
                    "    </div>" +
                    "  </div>" +
                    "</div>" );
                }

                return tmp.ToString();
            }

            #endregion

            return;
        }

        #region Set Normal values

        private void GenerateNOAAparameters( List<DayfileValue> ThisList )
        {
            // Now we have to determine the reference values for the total rainfall. A lot of work for two value :)
            //

            List<float> YearlyValues = new List<float>();

            NormalUsage = Sup.GetUtilsIniValue( "Graphs", "UseNormalRainReference", "Normal" );

            if ( NormalUsage.Equals( "Normal", CUtils.Cmp ) || NormalUsage.Equals( "Both", CUtils.Cmp ) )
            {
                StationNormal = true;

                for ( int i = (int) Months.Jan; i <= (int) Months.Dec; i++ )
                {
                    string iniKeyName = "NOAARainNorm" + Enum.GetNames( typeof( Months ) )[ i - 1 ];
                    string iniResult = Sup.GetCumulusIniValue( "NOAA", iniKeyName, "0.0" );
                    if ( iniResult.IndexOf( ',' ) > 0 )
                        iniResult = iniResult.Replace( ',', '.' );
                    NOAARainNormYearAv += (float) Convert.ToDouble( iniResult, CUtils.Inv );
                }

                //YearlyValues.Add( NOAARainNormYearAv );

                Sup.LogTraceInfoMessage( $" GenerateNOAAparameters: NOAARainNormYearAv {NOAARainNormYearAv:F1}" );
            }

            // Use station Average
            if ( NormalUsage.Equals( "StationAverage", CUtils.Cmp ) || NormalUsage.Equals( "Both", CUtils.Cmp ) )
            {
                List<float> tmp = new List<float>();
                //Calendar Cal = new GregorianCalendar();

                StationAverage = true;

                for ( int i = YearMin; i <= YearMax; i++ )
                {
                    if ( ThisList.Where( x => x.ThisDate.Year == i ).Count() < 364 /* Cal.GetDaysInYear(i) */ ) continue; // Incomplete year - have to reset to nr of days per year 
                    tmp.Add( ThisList.Where( x => x.ThisDate.Year == i ).Select( x => x.TotalRainThisDay ).Sum() );
                }

                // Second pass to determine the average and StdDev
                if ( tmp.Any() )
                    StationRainYearAv = tmp.Average();

                Sup.LogTraceInfoMessage( $" GenerateNOAAparameters : StationRainYearAv {StationRainYearAv}" );
            }

            //  Now get the highest year rainfall ever
            for ( int i = YearMin; i <= YearMax; i++ )
                MaxYearlyRainAlltime = Math.Max( ThisList.Where( x => x.ThisDate.Year == i ).Select( x => x.TotalRainThisDay ).Sum(), MaxYearlyRainAlltime );



            return;
        }

        #endregion

        #region Logarithmic Axis

        private void GenerateLogarithmicHandlerScript( StringBuilder thisBuffer )
        {
            // Take care of possible negative values on the logarithmixc scale!!
            // https://www.highcharts.com/blog/snippets/alternative-maths-plotting-negative-values-logarithmic-axis/
            //

            thisBuffer.AppendLine( "<script>" );
            thisBuffer.AppendLine( "(function(H) {" );
            thisBuffer.AppendLine( "  H.addEvent(H.Axis, 'afterInit', function() {" );
            thisBuffer.AppendLine( "    const logarithmic = this.logarithmic;" );

            thisBuffer.AppendLine( "    if (logarithmic && this.options.custom.allowNegativeLog)" );
            thisBuffer.AppendLine( "    {" );
            thisBuffer.AppendLine( "      this.positiveValuesOnly = false;" );  // Avoid errors on negative numbers on a log axis

            thisBuffer.AppendLine( "      logarithmic.log2lin = num => {" );  // Override the converter functions
            thisBuffer.AppendLine( "        const isNegative = num < 0;" );
            thisBuffer.AppendLine( "        let adjustedNum = Math.abs(num);" );
            thisBuffer.AppendLine( "        if (adjustedNum < 10)" );
            thisBuffer.AppendLine( "        {" );
            thisBuffer.AppendLine( "          adjustedNum += (10 - adjustedNum) / 10;" );
            thisBuffer.AppendLine( "        }" );
            thisBuffer.AppendLine( "        const result = Math.log(adjustedNum) / Math.LN10;" );
            thisBuffer.AppendLine( "        return isNegative ? -result : result;" );
            thisBuffer.AppendLine( "      };" );

            thisBuffer.AppendLine( "      logarithmic.lin2log = num => {" );
            thisBuffer.AppendLine( "        const isNegative = num < 0;" );
            thisBuffer.AppendLine( "        let result = Math.pow(10, Math.abs(num));" );
            thisBuffer.AppendLine( "        if (result < 10)" );
            thisBuffer.AppendLine( "        {" );
            thisBuffer.AppendLine( "          result = (10 * (result - 1)) / (10 - 1);" );
            thisBuffer.AppendLine( "        }" );
            thisBuffer.AppendLine( "        return isNegative ? -result : result;" );
            thisBuffer.AppendLine( "      };" );
            thisBuffer.AppendLine( "    }" ); // if (logarithmic && this.options.custom.allowNegativeLog)

            thisBuffer.AppendLine( "  });" );
            thisBuffer.AppendLine( "} (Highcharts));" );
            thisBuffer.AppendLine( "</script>" );

            return;
        }
        #endregion

        #region IDisposable

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                    thisMonthfile.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Graphx()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( false );
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( true );
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable
    }
}