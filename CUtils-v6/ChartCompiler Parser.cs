/*
 * ChartsCompiler Parser - Part of CumulusUtils
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
 *              https://jsfiddle.net/laff/WaEBc/
 *              https://www.highcharts.com/docs/stock/technical-indicator-series
 *              https://jsfiddle.net/gh/get/library/pure/highcharts/highcharts/tree/master/samples/stock/indicators/sma/
 *              
 * Test Charts:
 *     Chart MyDewPoint Title Dewpoint calculations in CDL
 *       Plot all minBarometer 
 *       Plot all maxBarometer
 *       Plot All ActualVapourPressure Eval [ MinHumidity/100 * 6.112 * EXP(17.62*AverageTemp/(243.12+AverageTemp)) ]
 *       Plot All ThisDewPoint EVAL [ (243.12 * LN(ActualVapourPressure) - 440.1) / (19.43 - LN(ActualVapourPressure)) ]
 *     EndChart
 *     
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CumulusUtils
{
    partial class ChartsCompiler
    {

        #region Parser

        List<string> Keywords;
        int CurrPosition = 0;

        List<ChartDef> AllCharts = new List<ChartDef>();
        ChartDef thisChart = new ChartDef( "", "" );

        readonly List<EqDef> AllEquations = new List<EqDef>();
        EqDef thisEq = new EqDef();

        readonly List<OutputDef> AllOutputs = new List<OutputDef>();
        OutputDef thisOutput = new OutputDef( "cumuluscharts.txt" );

        internal List<OutputDef> ParseChartDefinitions()
        {
            // Make the whole file into one string with meaningful contents separated by singles space
            if ( File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                string[] DefLinesArray;
                string DefContents = "";

                DefLinesArray = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", Encoding.UTF8 );

                foreach ( string line in DefLinesArray )
                    if ( string.IsNullOrEmpty( line ) || line[ 0 ] == ';' )
                        continue;
                    else
                        DefContents += line + ' ';

                DefContents = Regex.Replace( DefContents, @"\s+", " " );

                // The wehere clause takes care of (trailing) empty lines. 
                // Have to review this I don't understand why they don't just get replaced by space.
                Keywords = DefContents.Split( ' ' ).Where( s => !string.IsNullOrWhiteSpace( s ) ).ToList();
            }
            else
                return null;

            Sup.LogDebugMessage( $"DefineUsercharts: Parsing User charts definitions - start" );

            try  // Any error condition will fail the parsing and return null, falling back to default charts. Elaborate later with error messages
            {
                if ( Keywords[ CurrPosition ].Equals( "Equations", cmp ) )
                {
                    CurrPosition++;

                    if ( !ParseEquationBlock() )
                    {
                        Sup.LogTraceErrorMessage( $"Parsing User Charts: Error in Equations Block." );
                        return null;
                    }
                }

                do  // while not end of input
                {
                    if ( Keywords[ CurrPosition++ ].Equals( "Chart", cmp ) )
                    {
                        thisChart = new ChartDef( "", "" )
                        {
                            Id = Keywords[ CurrPosition++ ]
                        };

                        if ( AllCharts.Count > 0 )
                            foreach ( ChartDef entry in AllCharts )
                                if ( thisChart.Id.Equals( entry.Id, cmp ) )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Duplicate and illegal ID '{entry.Id}'" );
                                    return null;
                                }
                    }
                    else
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : No Chart at position '{CurrPosition}'" );
                        return null;
                    }

                    Sup.LogTraceInfoMessage( $"Parsing User Charts Definitions : Chart {thisChart.Id}'" );

                    if ( Keywords[ CurrPosition++ ].Equals( "Title", cmp ) )
                    {
                        thisChart.Title = Keywords[ CurrPosition++ ];

                        while ( !Keywords[ CurrPosition ].Equals( "Plot", cmp ) && !Keywords[ CurrPosition ].Equals( "ConnectsTo", cmp ) )
                        {
                            thisChart.Title += " " + Keywords[ CurrPosition++ ];
                        }
                    }
                    else
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error at Title" );
                        return null;
                    }

                    if ( Keywords[ CurrPosition ].Equals( "ConnectsTo", cmp ) )
                    {
                        CurrPosition++;

                        while ( int.TryParse( Keywords[ CurrPosition ], out int DasboardPanelNr ) )
                        {
                            CurrPosition++;

                            if ( AllOutputs.Count > 0 )
                            {
                                Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : Skipping illegal ConnectTo '{DasboardPanelNr}'" );
                                continue; // Only have Connects to from cumuluscharts.txt
                            }

                            thisChart.ConnectsToDashboardPanel.Add( DasboardPanelNr );
                            ClickEvents[ DasboardPanelNr - 1 ] = thisChart.Id;
                        }
                    }

                    if ( !( Keywords[ CurrPosition ].Equals( "Plot", cmp ) || Keywords[ CurrPosition ].Equals( "Stats", cmp ) ) )
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Plot or Stats missing" );
                        return null;
                    }

                    do // Must be PLOT block or a STATS block
                    {
                        Plotvar thisPlotvar = new Plotvar();

                        if ( Keywords[ CurrPosition ].Equals( "Stats", cmp ) )
                        {
                            CurrPosition++;

                            // Do the STATS specific block
                            thisPlotvar.IsStats = true;

                            if ( Keywords[ CurrPosition ].Equals( "All", cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisALL;
                                PlotvarTypes = PlotvarTypesALL;
                                PlotvarKeyword = PlotvarKeywordALL;
                                Datafiles = DatafilesALL;
                                PlotvarUnits = PlotvarUnitsALL;
                                thisPlotvar.PlotvarRange = PlotvarRangeType.All;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Recent", cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisRECENT;
                                PlotvarTypes = PlotvarTypesRECENT;
                                PlotvarKeyword = PlotvarKeywordRECENT;
                                Datafiles = DatafilesRECENT;
                                PlotvarUnits = PlotvarUnitsRECENT;
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;

                                CurrPosition++;
                            }
                            else
                            {
                                // No Range specification so: use default : Recent
                                PlotvarAxis = PlotvarAxisRECENT;
                                PlotvarTypes = PlotvarTypesRECENT;
                                PlotvarKeyword = PlotvarKeywordRECENT;
                                Datafiles = DatafilesRECENT;
                                PlotvarUnits = PlotvarUnitsRECENT;
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                            }

                            if ( Array.Exists( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], cmp ) ) )
                            {
                                // The plot var exists, create the entry for the chart and check the other attributes
                                int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], cmp ) );

                                thisPlotvar.Keyword = PlotvarKeyword[ index ];
                                thisPlotvar.PlotVar = PlotvarTypes[ index ];
                                thisPlotvar.Unit = PlotvarUnits[ index ];
                                thisPlotvar.Datafile = Datafiles[ index ];
                                thisPlotvar.AxisId = $"{PlotvarAxis[ index ]}";
                                thisPlotvar.Axis = PlotvarAxis[ index ];
                                thisChart.Axis |= thisPlotvar.Axis;

                                CurrPosition++;
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"Parsing User Charts: Invalid variable for statistic '{thisChart.Id}'" );
                                return null;
                            }

                            if ( Array.Exists( StatsTypeKeywords, word => word.Equals( Keywords[ CurrPosition ], cmp ) ) )
                            {
                                thisPlotvar.GraphType = Keywords[ CurrPosition ].ToLowerInvariant();
                                CurrPosition++;
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"Parsing User Charts: No Statitics definition found in STATS line of '{thisChart.Id}'" );
                                return null;
                            }
                        }

                        if ( Keywords[ CurrPosition ].Equals( "Plot", cmp ) )
                        {
                            bool EquationRequired = false;

                            //do the PLOTS specific block
                            thisPlotvar.IsStats = false;

                            CurrPosition++;

                            if ( Keywords[ CurrPosition ].Equals( "Recent", cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisRECENT;
                                PlotvarTypes = PlotvarTypesRECENT;
                                PlotvarKeyword = PlotvarKeywordRECENT;
                                Datafiles = DatafilesRECENT;
                                PlotvarUnits = PlotvarUnitsRECENT;

                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Daily", cmp ) || Keywords[ CurrPosition ].Equals( "All", cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisALL;
                                PlotvarTypes = PlotvarTypesALL;
                                PlotvarKeyword = PlotvarKeywordALL;
                                Datafiles = DatafilesALL;
                                PlotvarUnits = PlotvarUnitsALL;

                                if ( Keywords[ CurrPosition ].Equals( "Daily", cmp ) )
                                    thisPlotvar.PlotvarRange = PlotvarRangeType.Daily;
                                else
                                    thisPlotvar.PlotvarRange = PlotvarRangeType.All;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Extra", cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisEXTRA;
                                PlotvarTypes = PlotvarTypesEXTRA;
                                PlotvarKeyword = PlotvarKeywordEXTRA;
                                Datafiles = DatafilesEXTRA;
                                PlotvarUnits = PlotvarUnitsEXTRA;

                                thisPlotvar.PlotvarRange = PlotvarRangeType.Extra;
                                CurrPosition++;
                            }
                            else
                            {
                                // No Range specification so: use default : Recent
                                PlotvarAxis = PlotvarAxisRECENT;
                                PlotvarTypes = PlotvarTypesRECENT;
                                PlotvarKeyword = PlotvarKeywordRECENT;
                                Datafiles = DatafilesRECENT;
                                PlotvarUnits = PlotvarUnitsRECENT;
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                            }

                            // So check if the plotvar Keyword translates to a true CMX data  variable
                            if ( Array.Exists( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], cmp ) ) )
                            {
                                // The plot var exists, create the entry for the chart and check the other attributes
                                int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], cmp ) );

                                thisPlotvar.Keyword = PlotvarKeyword[ index ];
                                thisPlotvar.PlotVar = PlotvarTypes[ index ];
                                thisPlotvar.Unit = PlotvarUnits[ index ];
                                thisPlotvar.Datafile = Datafiles[ index ];
                                thisPlotvar.AxisId = $"{PlotvarAxis[ index ]}";
                                thisPlotvar.Axis = PlotvarAxis[ index ];
                                thisChart.Axis |= thisPlotvar.Axis;

                            }
                            else
                            {
                                EquationRequired = true;

                                thisPlotvar.Keyword = Keywords[ CurrPosition ];
                                thisPlotvar.PlotVar = "";
                                thisPlotvar.Unit = "";
                                thisPlotvar.Datafile = "";
                                thisPlotvar.AxisId = "";
                                thisPlotvar.Axis = AxisType.None;
                                thisChart.Axis |= thisPlotvar.Axis;
                            }

                            CurrPosition++;

                            if ( Keywords[ CurrPosition ].Equals( "Eval", cmp ) )
                            {
                                thisPlotvar.Equation = ParseSingleEval( thisPlotvar.Keyword );

                                if ( string.IsNullOrEmpty( thisPlotvar.Equation ) )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts: No Equation found for {thisPlotvar.Keyword}" );
                                    return null;
                                }
                                else
                                    thisPlotvar.EqAllVarList = new List<AllVarInfo>();
                            }
                            else if ( EquationRequired )
                            {
                                Sup.LogTraceErrorMessage( $"Parsing User Charts: No EVAL found for a PLOT statement' for {thisPlotvar.Keyword} when required'" );
                                Sup.LogTraceErrorMessage( $"Parsing User Charts: Equation is required because Plotvariable does not translate to valid JSON variable" );
                                return null;
                            }

                            // This needs to be on this level to prevent the STATS GraphType to be overwritten
                            thisPlotvar.GraphType = "spline";

                        } // Plot specific

                        // Create the other defaults for the attributes of PlotVar
                        thisPlotvar.zIndex = 5;
                        thisPlotvar.Color = "";
                        thisPlotvar.LineWidth = 2;
                        thisPlotvar.Opacity = 1.0;

                        // Use the range of the last plotvar assuming the user does not use RECENT mixed with ALL or DAILY
                        // Doing so would be an error!
                        thisChart.Range = thisPlotvar.PlotvarRange;

                        do
                        {
                            // Search for AS keyword
                            if ( Keywords[ CurrPosition ].Equals( "As", cmp ) )
                            {
                                CurrPosition++;

                                if ( Array.Exists( LinetypeKeywords, word => word.Equals( Keywords[ CurrPosition ], cmp ) ) )
                                {
                                    thisPlotvar.GraphType = Keywords[ CurrPosition ].ToLowerInvariant();

                                    if ( thisPlotvar.GraphType == "columnrange" )
                                        if ( !Array.Exists( ValidColumnRangeVars, word => word.Equals( thisPlotvar.Keyword, cmp ) ) )
                                        {
                                            // Error condition
                                            Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AS type '{Keywords[ CurrPosition ]}' for '{thisPlotvar.Keyword}'" );
                                            return null;
                                        }

                                    CurrPosition++;
                                }
                                else
                                {
                                    // Error condition
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AS linetype '{Keywords[ CurrPosition ]}'" );
                                    return null;
                                }
                            } // End AS

                            // Search for OPACITY keyword
                            if ( Keywords[ CurrPosition ].Equals( "Opacity", cmp ) )
                            {
                                CurrPosition++;

                                try
                                {
                                    thisPlotvar.Opacity = Convert.ToDouble( Keywords[ CurrPosition++ ], ci );

                                    if ( thisPlotvar.Opacity < 0 || thisPlotvar.Opacity > 1 )
                                        thisPlotvar.Opacity = 1.0;
                                }
                                catch ( Exception e )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around zIndex value of '{thisPlotvar.PlotVar}'" );
                                    return null;
                                }
                            } // End OPACITY

                            // Search for COLOUR keyword
                            if ( Keywords[ CurrPosition ].Equals( "Colour", cmp ) )
                            {
                                CurrPosition++;

                                thisPlotvar.Color = Keywords[ CurrPosition++ ];
                            } // End COLOUR

                            // Search for ZINDEX keyword
                            if ( Keywords[ CurrPosition ].Equals( "zIndex", cmp ) )
                            {
                                CurrPosition++;

                                try
                                {
                                    thisPlotvar.zIndex = Convert.ToInt32( Keywords[ CurrPosition++ ], ci );
                                }
                                catch ( Exception e )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around zIndex value of '{thisPlotvar.PlotVar}'" );
                                    return null;
                                }
                            } // End ZINDEX

                            // Search for LINEWIDTH keyword
                            if ( Keywords[ CurrPosition ].Equals( "LineWidth", cmp ) )
                            {
                                CurrPosition++;

                                try
                                {
                                    thisPlotvar.LineWidth = Convert.ToInt32( Keywords[ CurrPosition++ ], ci );
                                }
                                catch ( Exception e )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around LineWidth value of '{thisPlotvar.PlotVar}'" );
                                    return null;
                                }
                            } // End LINEWIDTH

                            // Search for AXIS keyword
                            if ( Keywords[ CurrPosition ].Equals( "Axis", cmp ) )
                            {
                                CurrPosition++; // this one gets us on the Axis specification after the keyword

                                if ( string.IsNullOrEmpty( thisPlotvar.Equation ) )
                                {
                                    Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : AXIS definition ignored in absence of correct EVAL equation for {thisPlotvar.Keyword}" );
                                    CurrPosition++; // this  one gets us on the next KeyWord
                                }
                                else
                                {
                                    try
                                    {
                                        if ( Array.Exists( AxisKeywords, word => word.Equals( Keywords[ CurrPosition ], cmp ) ) )
                                        {
                                            thisPlotvar.AxisId = Keywords[ CurrPosition ];
                                            Enum.TryParse( Keywords[ CurrPosition ], out thisPlotvar.Axis );
                                            thisChart.Axis |= thisPlotvar.Axis;

                                            CurrPosition++;
                                        }
                                        else
                                        {
                                            // Error condition
                                            Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AXIS type '{Keywords[ CurrPosition ]}'" );
                                            return null;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                        Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around AXIS spec '{thisPlotvar.PlotVar}'" );
                                        return null;
                                    }
                                }
                            } // End AXIS

                        } while ( Keywords[ CurrPosition ].Equals( "As", cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "Colour", cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "zIndex", cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "Opacity", cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "Axis", cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "LineWidth", cmp ) );

                        thisChart.PlotVars.Add( thisPlotvar );

                        if ( thisPlotvar.GraphType == "scatter" )
                            thisChart.HasScatter = true;

                    } while ( Keywords[ CurrPosition ].Equals( "Plot", cmp ) || Keywords[ CurrPosition ].Equals( "Stats", cmp ) );  // End while if PLOT keyword

                    if ( Keywords[ CurrPosition++ ].Equals( "EndChart", cmp ) )
                    {
                        if ( thisChart.HasScatter )
                        {
                            foreach ( Plotvar pv in thisChart.PlotVars )
                            {
                                if ( pv.GraphType != "scatter" )
                                {
                                    // Error condition
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : A Scatter graph has other Plotvar Types '{pv.Keyword}' as '{pv.GraphType}'" );
                                    return null;
                                }
                            }
                        }

                        // Do the possible(!) output
                        if ( CurrPosition < Keywords.Count - 1 && Keywords[ CurrPosition ].Equals( "Output", cmp ) )
                        {
                            CurrPosition++;  // Go to the filename

                            if ( AllOutputs.Count == 0 && AllCharts.Count == 0 )
                            {
                                Sup.LogTraceWarningMessage( $"Parsing User Charts Definitions : Output given for first Chart '{thisChart.Id}'. Cannot specify output for first chart" );
                                return null;
                            }

                            thisOutput.TheseCharts = AllCharts;
                            AllOutputs.Add( thisOutput );

                            AllCharts = new List<ChartDef>();
                            thisOutput = new OutputDef
                            {
                                Filename = Keywords[ CurrPosition++ ]
                            };
                        }

                        AllCharts.Add( thisChart );
                    }
                    else
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Error at EndChart of Chart '{thisChart.Id}'" );
                        return null;
                    }

                } while ( CurrPosition < Keywords.Count - 1 /* && Keywords[CurrPosition++].Equals("EndChart", cmp)*/ );  // While chart definition properties

                // Do some elementary checks
                // Check for ColumnRange to have a parameter plottable as such
                foreach ( ChartDef chart in AllCharts )
                    foreach ( Plotvar plotvar in chart.PlotVars )
                        if ( plotvar.GraphType == "ColumnRange" )
                            if ( plotvar.PlotvarRange == PlotvarRangeType.Daily || plotvar.PlotvarRange == PlotvarRangeType.All )
                            {
                                // It is OK
                            }
                            else { Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Illegal use of ColumnRange '{chart.Id}'/'{plotvar.Keyword}' with RECENT" ); return null; }

                // Check for STATS to have the same variable regularly plotted in the same chart
                foreach ( ChartDef chart in AllCharts )
                    foreach ( Plotvar plotvar in chart.PlotVars )
                        if ( plotvar.IsStats )
                        {
                            bool found = false;
                            foreach ( Plotvar plotvar2 in chart.PlotVars )
                                if ( plotvar2.PlotVar == plotvar.PlotVar ) { found = true; break; }  // The STATS plotvar is also plotted for itself in this chart
                                else
                                    continue;
                            if ( !found ) { Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : STATS variable '{plotvar.Keyword}' not plotted in this CHART" ); return null; }
                        }

                // OK so set the lists and continue
                thisOutput.TheseCharts = AllCharts;
                AllOutputs.Add( thisOutput );

                return AllOutputs;
            }
            catch ( Exception e )
            {
                Sup.LogTraceErrorMessage( $"Error Parsing Exception : {e.Message}" );
                Sup.LogTraceErrorMessage( "Error Parsing Chart definitions - Defaults used." );
                return null;
            }
        } // ParseChartdefinitions()

        #endregion

    } // Class DefineCharts
}// Namespace
