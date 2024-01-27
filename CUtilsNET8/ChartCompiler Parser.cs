/*
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
 * Literature:  https://github.com/jstat/jstat
 *              https://jstat.github.io/all.html
 *              https://www.highcharts.com/docs/chart-and-series-types/chart-types
 *              https://jsfiddle.net/laff/WaEBc/
 *              https://www.highcharts.com/docs/stock/technical-indicator-series
 *              https://jsfiddle.net/gh/get/library/pure/highcharts/highcharts/tree/master/samples/stock/indicators/sma/
 *     
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Helpers;

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

        public List<OutputDef> ParseChartDefinitions()
        {
            // Make the whole file into one string with meaningful contents separated by singles space
            if ( File.Exists( $"{Sup.PathUtils}{Sup.CutilsChartsDef}" ) )
            {
                string[] DefLinesArray;
                string DefContents = "";

                DefLinesArray = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CutilsChartsDef}", Encoding.UTF8 );

                foreach ( string line in DefLinesArray )
                    if ( line.IsBlank() || line[ 0 ] == ';' ) continue;
                    else
                        DefContents += line + ' ';

                DefContents = Regex.Replace( DefContents, @"\s+", " " );

                // The where clause takes care of (trailing) empty lines. 
                // Have to review this I don't understand why they don't just get replaced by space.
                char[] charSeparators = new char[] { ' ' };
                Keywords = DefContents.Split( charSeparators ).Where( s => !string.IsNullOrWhiteSpace( s ) ).ToList();
            }
            else
                return null;

            Sup.LogDebugMessage( $"DefineUsercharts: Parsing User charts definitions - start" );

            try  // Any error condition will fail the parsing and return null, falling back to default charts. Elaborate later with error messages
            {
                if ( Keywords[ CurrPosition ].Equals( "Equations", CUtils.Cmp ) )
                {
                    CurrPosition++;

                    if ( !ParseEquationBlock() )
                    {
                        //Sup.LogTraceErrorMessage( $"Parsing User Charts: Error in Equations Block." );
                        // ParseEquationBlock has its own error messaging
                        return null;
                    }
                }

                do  // while not end of input
                {
                    if ( Keywords[ CurrPosition++ ].Equals( "Chart", CUtils.Cmp ) )
                    {
                        thisChart = new ChartDef( "", "" )
                        {
                            Id = Keywords[ CurrPosition++ ]
                        };

                        if ( AllCharts.Count > 0 )
                            foreach ( ChartDef entry in AllCharts )
                                if ( thisChart.Id.Equals( entry.Id, CUtils.Cmp ) )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Duplicate and illegal Chart ID : '{entry.Id}'" );
                                    return null;
                                }
                    }
                    else
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Unrecognised keyword '{Keywords[ --CurrPosition ]}' where Chart should be" );
                        return null;
                    }

                    Sup.LogTraceInfoMessage( $"Parsing User Charts Definitions : Chart {thisChart.Id}'" );

                    if ( Keywords[ CurrPosition++ ].Equals( "Title", CUtils.Cmp ) )
                    {
                        thisChart.Title = Keywords[ CurrPosition++ ];

                        while ( !Keywords[ CurrPosition ].Equals( "Plot", CUtils.Cmp ) && !Keywords[ CurrPosition ].Equals( "ConnectsTo", CUtils.Cmp ) &&
                                                                                   !Keywords[ CurrPosition ].Equals( "Zoom", CUtils.Cmp ) &&
                                                                                   !Keywords[ CurrPosition ].Equals( "Has", CUtils.Cmp ) )
                        {
                            thisChart.Title += " " + Keywords[ CurrPosition++ ];
                        }
                    }
                    else
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Missing keyword 'Title'" );
                        return null;
                    }

                    while ( Keywords[ CurrPosition ].Equals( "ConnectsTo", CUtils.Cmp ) ||
                            Keywords[ CurrPosition ].Equals( "Zoom", CUtils.Cmp ) ||
                            Keywords[ CurrPosition ].Equals( "Has", CUtils.Cmp ) )
                    {
                        if ( Keywords[ CurrPosition ].Equals( "ConnectsTo", CUtils.Cmp ) )
                        {
                            CurrPosition++;

                            while ( int.TryParse( Keywords[ CurrPosition ], out int DasboardPanelNr ) )
                            {
                                CurrPosition++;

                                if ( AllOutputs.Count > 0 )
                                {
                                    Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : Skipping illegal ConnectTo '{DasboardPanelNr}'" );
                                    Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : ConnectsTo can only be used in the first - unspecified - output" );
                                    continue; // Only have Connects to from cumuluscharts.txt
                                }

                                thisChart.ConnectsToDashboardPanel.Add( DasboardPanelNr );
                                ClickEvents[ DasboardPanelNr - 1 ] = thisChart.Id;
                            }
                        }

                        // Search for Zoom keyword
                        if ( Keywords[ CurrPosition ].Equals( "Zoom", CUtils.Cmp ) )
                        {
                            CurrPosition++;

                            try
                            {
                                _ = int.TryParse( Keywords[ CurrPosition++ ], out int tmp );
                                thisChart.Zoom = tmp;
                            }
                            catch ( Exception e )
                            {
                                Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around Zoom value of '{thisChart.Id}'" );
                            }
                        } // End ZOOM

                        if ( Keywords[ CurrPosition ].Equals( "Has", CUtils.Cmp ) )
                        {
                            CurrPosition++;

                            if ( Keywords[ CurrPosition ].Equals( "WindBarbs", CUtils.Cmp ) )
                            {
                                CurrPosition++;
                                thisChart.HasWindBarbs = true;

                                if ( Keywords[ CurrPosition ].Equals( "Above", CUtils.Cmp ) )
                                {
                                    CurrPosition++;
                                    thisChart.WindBarbsBelow = false;
                                }
                                else if ( Keywords[ CurrPosition ].Equals( "Below", CUtils.Cmp ) )
                                {
                                    CurrPosition++;
                                    thisChart.WindBarbsBelow = true;
                                }
                                else
                                {
                                    // Error condition
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Missing BELOW or ABOVE Keyword after WindBarbs" );
                                    return null;
                                }

                                if ( Keywords[ CurrPosition ].Equals( "Colour", CUtils.Cmp ) )
                                {
                                    CurrPosition++;

                                    thisChart.WindBarbColor = Keywords[ CurrPosition++ ];
                                }
                            }
                            else
                            {
                                // Error condition
                                Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Missing WindBarbs Keyword" );
                                return null;
                            }
                        }
                    } // while ConnectsTo, Zoom, Has

                    if ( !( Keywords[ CurrPosition ].Equals( "Plot", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "Stats", CUtils.Cmp ) ) )
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Plot or Stats missing" );
                        return null;
                    }

                    do // Must be PLOT block or a STATS block
                    {
                        Plotvar thisPlotvar = new Plotvar();

                        if ( Keywords[ CurrPosition ].Equals( "Stats", CUtils.Cmp ) )
                        {
                            CurrPosition++;

                            // Do the STATS specific block
                            thisPlotvar.IsStats = true;

                            if ( Keywords[ CurrPosition ].Equals( "Daily", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "All", CUtils.Cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisALL;
                                PlotvarTypes = PlotvarTypesALL;
                                PlotvarKeyword = PlotvarKeywordALL;
                                Datafiles = DatafilesALL;
                                PlotvarUnits = PlotvarUnitsALL;
                                thisPlotvar.PlotvarRange = PlotvarRangeType.All;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Recent", CUtils.Cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisRECENT;
                                PlotvarTypes = PlotvarTypesRECENT;
                                PlotvarKeyword = PlotvarKeywordRECENT;
                                Datafiles = DatafilesRECENT;
                                PlotvarUnits = PlotvarUnitsRECENT;
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Extra", CUtils.Cmp ) )
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

                            // HansR: how to validate and STATS variable for an equation:
                            // 1) Check if it is present in the Plotvar array if true continue immediately
                            // 2) Check if the Keyword for the STATS exists already and has an equation (not an empty string)
                            // 3) NOTE: The STATS line must come AFTER the PLOT line of the equation!!

                            if ( Array.Exists( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) ) ||
                                 ( thisChart.PlotVars.Where( p => p.Keyword.Equals( Keywords[ CurrPosition ]) && !p.Equation.Equals("") ).Count() == 1 ) )
                            {
                                // The plot var exists, create the entry for the chart and check the other attributes
                                int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) );

                                if ( index == -1 )
                                {
                                    // Get the info on the plotvar with EVAL for which we make the STATS
                                    Plotvar tmp = thisChart.PlotVars.Where( p => p.Keyword.Equals( Keywords[ CurrPosition ] ) && !p.Equation.Equals( "" ) ).FirstOrDefault();

                                    thisPlotvar.Keyword = tmp.Keyword;
                                    thisPlotvar.PlotVar = tmp.PlotVar;
                                    thisPlotvar.Unit = tmp.Unit;
                                    thisPlotvar.Datafile = tmp.Datafile;
                                    thisPlotvar.AxisId = tmp.AxisId;
                                    thisPlotvar.Axis = tmp.Axis;
                                    thisChart.Axis |= thisPlotvar.Axis;

                                }
                                else
                                {
                                    // This is a regular plotvar from the known ones so just set the info as known

                                    thisPlotvar.Keyword = PlotvarKeyword[ index ];
                                    thisPlotvar.PlotVar = PlotvarTypes[ index ];
                                    thisPlotvar.Unit = PlotvarUnits[ index ];
                                    thisPlotvar.Datafile = Datafiles[ index ];
                                    thisPlotvar.AxisId = $"{PlotvarAxis[ index ]}";
                                    thisPlotvar.Axis = PlotvarAxis[ index ];
                                    thisChart.Axis |= thisPlotvar.Axis;
                                }

                                CurrPosition++;
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"Parsing User Charts: Invalid variable {Keywords[ CurrPosition ]} for statistic in chart '{thisChart.Id}'" );
                                return null;
                            }

                            if ( Array.Exists( StatsTypeKeywords, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) ) )
                            {
                                // atm only SMA is valid. For more statistic functions we need to expand this section

                                thisPlotvar.GraphType = Keywords[ CurrPosition ].ToLowerInvariant();
                                CurrPosition++;

                                if ( Keywords[ CurrPosition ].Equals( "Period", CUtils.Cmp ) )
                                {
                                    thisPlotvar.Period = Convert.ToInt32( Keywords[ ++CurrPosition ], CUtils.Inv );
                                    CurrPosition++;
                                }
                                else
                                {
                                    // No period, give use the default
                                    thisPlotvar.Period = Convert.ToInt32( Sup.GetUtilsIniValue( "Compiler", "SmaPeriod", "5" ) );
                                }
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"Parsing User Charts: No Statistics definition found in STATS line of '{thisChart.Id}'" );
                                return null;
                            }
                        }
                        else if ( Keywords[ CurrPosition ].Equals( "Plot", CUtils.Cmp ) )
                        {
                            bool EquationRequired = false;

                            //do the PLOTS specific block
                            thisPlotvar.IsStats = false;

                            CurrPosition++;

                            if ( Keywords[ CurrPosition ].Equals( "Recent", CUtils.Cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisRECENT;
                                PlotvarTypes = PlotvarTypesRECENT;
                                PlotvarKeyword = PlotvarKeywordRECENT;
                                Datafiles = DatafilesRECENT;
                                PlotvarUnits = PlotvarUnitsRECENT;

                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Daily", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "All", CUtils.Cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisALL;
                                PlotvarTypes = PlotvarTypesALL;
                                PlotvarKeyword = PlotvarKeywordALL;
                                Datafiles = DatafilesALL;
                                PlotvarUnits = PlotvarUnitsALL;

                                if ( Keywords[ CurrPosition ].Equals( "Daily", CUtils.Cmp ) )
                                    thisPlotvar.PlotvarRange = PlotvarRangeType.Daily;
                                else
                                    thisPlotvar.PlotvarRange = PlotvarRangeType.All;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Extra", CUtils.Cmp ) )
                            {
                                PlotvarAxis = PlotvarAxisEXTRA; //PlotvarAxisEXTRA;
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
                            if ( Array.Exists( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) ) )
                            {
                                // The plot var exists, create the entry for the chart and check the other attributes
                                int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) );

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

                            if ( Keywords[ CurrPosition ].Equals( "Eval", CUtils.Cmp ) )
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
                        thisPlotvar.Visible = true;

                        // Use the range of the last plotvar assuming the user does not use RECENT mixed with ALL or DAILY
                        // Doing so would be an error!
                        thisChart.Range = thisPlotvar.PlotvarRange;

                        do
                        {
                            // Check if the line must be shown at initialisation
                            if ( Keywords[ CurrPosition ].Equals( "InVisible", CUtils.Cmp ) )
                            {
                                CurrPosition++;
                                thisPlotvar.Visible = false;
                            }

                            // Search for AS keyword
                            if ( Keywords[ CurrPosition ].Equals( "As", CUtils.Cmp ) && !thisPlotvar.IsStats )  // Not for STATS variable because that must always be a line
                            {
                                CurrPosition++;

                                if ( Array.Exists( LinetypeKeywords, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) ) )
                                {
                                    thisPlotvar.GraphType = Keywords[ CurrPosition ].ToLowerInvariant();

                                    if ( thisPlotvar.GraphType == "columnrange" )
                                        if ( !Array.Exists( ValidColumnRangeVars, word => word.Equals( thisPlotvar.Keyword, CUtils.Cmp ) ) )
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
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "As", CUtils.Cmp ) && thisPlotvar.IsStats )
                            {
                                Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AS type '{Keywords[ CurrPosition ]}' for '{thisPlotvar.Keyword}'" );
                                Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : Cannot set a plot type for a STATS Plotvariable" );

                                CurrPosition++;
                                CurrPosition++; // Skip over ' AS [LinetypeKeyword] '
                            }// End AS

                            // Search for OPACITY keyword
                            if ( Keywords[ CurrPosition ].Equals( "Opacity", CUtils.Cmp ) )
                            {
                                CurrPosition++;

                                try
                                {
                                    thisPlotvar.Opacity = Convert.ToDouble( Keywords[ CurrPosition++ ], CUtils.Inv );

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
                            if ( Keywords[ CurrPosition ].Equals( "Colour", CUtils.Cmp ) )
                            {
                                CurrPosition++;

                                thisPlotvar.Color = Keywords[ CurrPosition++ ];
                            } // End COLOUR

                            // Search for ZINDEX keyword
                            if ( Keywords[ CurrPosition ].Equals( "zIndex", CUtils.Cmp ) )
                            {
                                CurrPosition++;

                                try
                                {
                                    thisPlotvar.zIndex = Convert.ToInt32( Keywords[ CurrPosition++ ], CUtils.Inv );
                                }
                                catch ( Exception e )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around zIndex value of '{thisPlotvar.PlotVar}'" );
                                    return null;
                                }
                            } // End ZINDEX

                            // Search for LINEWIDTH keyword
                            if ( Keywords[ CurrPosition ].Equals( "LineWidth", CUtils.Cmp ) )
                            {
                                CurrPosition++;

                                try
                                {
                                    thisPlotvar.LineWidth = Convert.ToInt32( Keywords[ CurrPosition++ ], CUtils.Inv );
                                }
                                catch ( Exception e )
                                {
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}" );
                                    Sup.LogTraceErrorMessage( $"Parsing User Charts '{thisChart.Id}' : Error around LineWidth value of '{thisPlotvar.PlotVar}'" );
                                    return null;
                                }
                            } // End LINEWIDTH

                            // Search for AXIS keyword
                            if ( Keywords[ CurrPosition ].Equals( "Axis", CUtils.Cmp ) )
                            {
                                CurrPosition++; // this one gets us on the Axis specification after the keyword

                                if ( string.IsNullOrEmpty( thisPlotvar.Equation ) )
                                {
                                    Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : AXIS specification ignored in absence of (correct) EVAL equation for {thisPlotvar.Keyword}" );
                                    Sup.LogTraceWarningMessage( $"Parsing User Charts '{thisChart.Id}' : Axis specification only relevant for Equations, continuing..." );
                                    CurrPosition++; // this  one gets us on the next KeyWord
                                }
                                else
                                {
                                    try
                                    {
                                        if ( Array.Exists( AxisKeywords, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) ) )
                                        {
                                            thisPlotvar.AxisId = Keywords[ CurrPosition ];
                                            _ = Enum.TryParse( Keywords[ CurrPosition ], out thisPlotvar.Axis );
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

                        } while ( Keywords[ CurrPosition ].Equals( "As", CUtils.Cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "Colour", CUtils.Cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "zIndex", CUtils.Cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "Opacity", CUtils.Cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "Axis", CUtils.Cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "InVisible", CUtils.Cmp ) ||
                                    Keywords[ CurrPosition ].Equals( "LineWidth", CUtils.Cmp ) );

                        if ( thisPlotvar.GraphType == "scatter" )
                            thisChart.HasScatter = true;

                        if ( !string.IsNullOrEmpty( thisPlotvar.Equation ) ) // Check for an Axis
                        {
                            if ( thisPlotvar.Axis == AxisType.None )
                            {
                                Sup.LogTraceWarningMessage( $"User Charts '{thisChart.Id}/{thisPlotvar.Keyword}': No Axis was specified or in error. Axistype is set to FREE." );

                                thisPlotvar.AxisId = "Free";
                                thisPlotvar.Axis = AxisType.Free;
                                thisChart.Axis |= thisPlotvar.Axis;
                            }
                        }

                        thisChart.PlotVars.Add( thisPlotvar );

                    } while ( Keywords[ CurrPosition ].Equals( "Plot", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "Stats", CUtils.Cmp ) );  // End while if PLOT keyword

                    if ( Keywords[ CurrPosition++ ].Equals( "EndChart", CUtils.Cmp ) )
                    {
                        // thisChart still has the value for the current chart for which we just read the EndChart keyword.
                        // This means the Info and Output keywords which may follow the EndChart still refer to the chart for which those are valid!!

                        bool OutputDone = false;

                        try
                        {
                            while ( CurrPosition < Keywords.Count - 1 && ( Keywords[ CurrPosition ].Equals( "Info", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "Output", CUtils.Cmp ) ) )
                            {
                                if ( Keywords[ CurrPosition ].Equals( "Info", CUtils.Cmp ) )
                                {
                                    if ( thisChart.HasInfo )
                                    {
                                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Double Info specified on '{thisChart.Id}'." );
                                        return null;
                                    }
                                    else thisChart.HasInfo = true;

                                    CurrPosition++;

                                    if ( Keywords[ CurrPosition++ ].Equals( "\"", CUtils.Cmp ) )
                                    {
                                        try
                                        {
                                            while ( !Keywords[ CurrPosition ].Equals( "\"", CUtils.Cmp ) )
                                            {
                                                thisChart.InfoText += " " + Keywords[ CurrPosition++ ];
                                            }

                                            CurrPosition++;  // Keyword next to the quote
                                        }
                                        catch ( Exception e ) when ( e is IndexOutOfRangeException )
                                        {
                                            Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Info specified on '{thisChart.Id}' but no closing quote found." );
                                            return null;
                                        }
                                    }
                                    else
                                    {
                                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Info specified on '{thisChart.Id}' but no start quote found." );
                                        return null;
                                    }
                                }

                                if ( CurrPosition >= Keywords.Count ) break;

                                // Do the possible(!) output
                                if ( Keywords[ CurrPosition ].Equals( "Output", CUtils.Cmp ) )
                                {
                                    if ( OutputDone )
                                    {
                                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Double Output specified on '{thisChart.Id}'." );
                                        return null;
                                    }
                                    else OutputDone = true;

                                    CurrPosition++;  // Go to the filename

                                    if ( AllOutputs.Count == 0 && AllCharts.Count == 0 )
                                    {
                                        Sup.LogTraceWarningMessage( $"Parsing User Charts Definitions : Output given for first Chart '{thisChart.Id}'. Cannot specify output for first chart" );
                                    }
                                    else
                                    {
                                        thisOutput.TheseCharts = AllCharts;
                                        AllOutputs.Add( thisOutput );

                                        AllCharts = new List<ChartDef>();
                                        thisOutput = new OutputDef
                                        {
                                            Filename = Keywords[ CurrPosition ]
                                        };
                                    }

                                    CurrPosition++;  // Keyword next to the filename
                                }
                            } // While Info or Output (the order does not matter)
                        } // try to detect EOF
                        catch ( Exception e )
                        {
                            Sup.LogTraceErrorMessage( "Parsing User Charts Definitions : Unknown exception while reaching EOF. Incomplete Charts definition." );
                            Sup.LogTraceErrorMessage( $"Exception found is: {e.Message}" );
                            if ( e.InnerException != null ) Sup.LogTraceErrorMessage( $"InnerException found is: {e.InnerException}" );
                            return null;
                        }

                        AllCharts.Add( thisChart );
                    }
                    else
                    {
                        // Error condition
                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Error at EndChart of Chart '{thisChart.Id}'" );
                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : After position '{Keywords[ --CurrPosition ]}'" );
                        return null;
                    }

                } while ( CurrPosition < Keywords.Count - 1 );

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
                                if ( plotvar2.PlotVar == plotvar.PlotVar && !plotvar2.IsStats ) { found = true; break; }  // The STATS plotvar is also plotted for itself in this chart
                                else
                                    continue;
                            if ( !found ) { Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : STATS variable '{plotvar.Keyword}' not plotted by itself in this CHART" ); return null; }
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
