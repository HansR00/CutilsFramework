/*
 * ChartsCompiler Parser - Part of CumulusUtils
 *
 * Structural rework. Public surface unchanged: ParseChartDefinitions() keeps
 * its signature and its "return null == fall back to default charts" contract.
 *
 * What changed, and why:
 *   - The three near-identical STATS range-selection blocks and the three
 *     near-identical PLOT range-selection blocks now go through one small
 *     SelecTotvarRange() helper. The tables selected (and the resulting
 *     PlotvarRange) are identical to before.
 *   - The duplicate-STATS lookup previously walked thisChart.PlotVars twice
 *     (once with .Where(...).Count()==1 and again with .Where(...).First());
 *     it is now a single scan (TryFindStatsSource) returning the match.
 *   - Whitespace tokenization reuses the Keywords list (Clear instead of
 *     reallocate) and short-circuits an empty file with an explicit check
 *     rather than reading past the end.
 *
 * Deliberately NOT changed: every log message and its level, the exact set of
 * accepted keywords, the EndChart/Info/Output ordering logic, the elementary
 * ColumnRange and STATS cross-checks, and the try/catch fallback behaviour.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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

        // Selects the axis/types/keyword/datafile/unit tables for a range. The
        // caller advances CurrPosition only when the range keyword was present
        // (matching the original inline blocks).
        private void SelectPlotvarRange( PlotvarRangeType range )
        {
            switch ( range )
            {
                case PlotvarRangeType.All:
                    PlotvarAxis    = PlotvarAxisALL;
                    PlotvarTypes   = PlotvarTypesALL;
                    PlotvarKeyword = PlotvarKeywordALL;
                    Datafiles      = DatafilesALL;
                    PlotvarUnits   = PlotvarUnitsALL;
                    break;

                case PlotvarRangeType.Extra:
                    PlotvarAxis    = PlotvarAxisEXTRA;
                    PlotvarTypes   = PlotvarTypesEXTRA;
                    PlotvarKeyword = PlotvarKeywordEXTRA;
                    Datafiles      = DatafilesEXTRA;
                    PlotvarUnits   = PlotvarUnitsEXTRA;
                    break;

                case PlotvarRangeType.Recent:
                default:
                    PlotvarAxis    = PlotvarAxisRECENT;
                    PlotvarTypes   = PlotvarTypesRECENT;
                    PlotvarKeyword = PlotvarKeywordRECENT;
                    Datafiles      = DatafilesRECENT;
                    PlotvarUnits   = PlotvarUnitsRECENT;
                    break;
            }
        }

        // Single scan for the "Stats variable already declared on this chart with an equation" case.
        private static Plotvar TryFindStatsSource( ChartDef chart, string keyword )
        {
            foreach ( Plotvar p in chart.PlotVars )
                if ( p.Keyword.Equals( keyword ) && !p.Equation.Equals( "" ) )
                    return p;

            return null;
        }

        public List<OutputDef> ParseChartDefinitions()
        {
            // Read definition file and tokenize tokens efficiently without O(N^2) string allocations
            string defPath = $"{Sup.PathUtils}{Sup.CutilsChartsDef}";
            if ( File.Exists( defPath ) )
            {
                Keywords ??= new List<string>( capacity: 1024 );
                Keywords.Clear();

                foreach ( string rawLine in File.ReadLines( defPath, Encoding.UTF8 ) )
                {
                    ReadOnlySpan<char> line = rawLine.AsSpan().Trim();
                    if ( line.IsEmpty || line[ 0 ] == ';' )
                        continue;

                    // Tokenize whitespace-separated words
                    int tokenStart = -1;
                    for ( int i = 0; i < line.Length; i++ )
                    {
                        if ( char.IsWhiteSpace( line[ i ] ) )
                        {
                            if ( tokenStart != -1 )
                            {
                                Keywords.Add( line.Slice( tokenStart, i - tokenStart ).ToString() );
                                tokenStart = -1;
                            }
                        }
                        else if ( tokenStart == -1 )
                        {
                            tokenStart = i;
                        }
                    }
                    if ( tokenStart != -1 )
                    {
                        Keywords.Add( line.Slice( tokenStart ).ToString() );
                    }
                }
            }
            else
                return null;

            if ( Keywords.Count == 0 )
                return null;

            Sup.LogDebugMessage( $"DefineUsercharts: Parsing User charts definitions - starting" );

            try  // Any error condition will fail the parsing and return null, falling back to default charts.
            {
                if ( Keywords[ CurrPosition ].Equals( "Equations", CUtils.Cmp ) )
                {
                    CurrPosition++;

                    if ( !ParseEquationBlock() )
                    {
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
                                    Sup.LogMessage( $"Parsing User Charts Definitions : Duplicate and illegal Chart ID : '{entry.Id}'", TraceLevel.Error );
                                    return null;
                                }
                    }
                    else
                    {
                        // Error condition
                        Sup.LogMessage( $"Parsing User Charts Definitions : Unrecognised keyword '{Keywords[ --CurrPosition ]}' where Chart should be", TraceLevel.Error );
                        return null;
                    }

                    Sup.LogMessage( $"Parsing User Charts Definitions : Chart {thisChart.Id}'", TraceLevel.Info );

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
                        Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Missing keyword 'Title'", TraceLevel.Error );
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
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Skipping illegal ConnectTo '{DasboardPanelNr}'", TraceLevel.Warning );
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : ConnectsTo can only be used in the first - unspecified - output", TraceLevel.Warning );
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

                            // int.TryParse never throws; the previous catch was unreachable.
                            _ = int.TryParse( Keywords[ CurrPosition++ ], out int tmp );
                            thisChart.Zoom = tmp;
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
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Missing BELOW or ABOVE Keyword after WindBarbs", TraceLevel.Error );
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
                                Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Missing WindBarbs Keyword", TraceLevel.Error );
                                return null;
                            }
                        }
                    } // while ConnectsTo, Zoom, Has

                    if ( !( Keywords[ CurrPosition ].Equals( "Plot", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "Stats", CUtils.Cmp ) ) )
                    {
                        // Error condition
                        Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Plot or Stats missing", TraceLevel.Error );
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
                                SelectPlotvarRange( PlotvarRangeType.All );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.All;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Recent", CUtils.Cmp ) )
                            {
                                SelectPlotvarRange( PlotvarRangeType.Recent );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Extra", CUtils.Cmp ) )
                            {
                                SelectPlotvarRange( PlotvarRangeType.Extra );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Extra;

                                CurrPosition++;
                            }
                            else
                            {
                                //No Range specification so: use default : Recent
                                SelectPlotvarRange( PlotvarRangeType.Recent );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                            }

                            // HansR: how to validate a STATS variable for an equation:
                            // 1) Check if it is present in the Plotvar array if true continue immediately
                            // 2) Check if the Keyword for the STATS exists already and has an equation (not an empty string)
                            // 3) NOTE: The STATS line must come AFTER the PLOT line of the equation!!

                            int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) );
                            Plotvar statsSource = index == -1 ? TryFindStatsSource( thisChart, Keywords[ CurrPosition ] ) : null;

                            if ( index != -1 || statsSource is not null )
                            {
                                if ( index == -1 )
                                {
                                    // Get the info on the plotvar with EVAL for which we make the STATS
                                    thisPlotvar.Keyword = statsSource.Keyword;
                                    thisPlotvar.PlotVar = statsSource.PlotVar;
                                    thisPlotvar.Unit = statsSource.Unit;
                                    thisPlotvar.Datafile = statsSource.Datafile;
                                    thisPlotvar.AxisId = statsSource.AxisId;
                                    thisPlotvar.Axis = statsSource.Axis;
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
                                Sup.LogMessage( $"Parsing User Charts: Invalid variable {Keywords[ CurrPosition ]} for statistic in chart '{thisChart.Id}'", TraceLevel.Error );
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
                                Sup.LogMessage( $"Parsing User Charts: No Statistics definition found in STATS line of '{thisChart.Id}'", TraceLevel.Error );
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
                                SelectPlotvarRange( PlotvarRangeType.Recent );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Daily", CUtils.Cmp ) || Keywords[ CurrPosition ].Equals( "All", CUtils.Cmp ) )
                            {
                                SelectPlotvarRange( PlotvarRangeType.All );

                                if ( Keywords[ CurrPosition ].Equals( "Daily", CUtils.Cmp ) )
                                    thisPlotvar.PlotvarRange = PlotvarRangeType.Daily;
                                else
                                    thisPlotvar.PlotvarRange = PlotvarRangeType.All;

                                CurrPosition++;
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "Extra", CUtils.Cmp ) )
                            {
                                SelectPlotvarRange( PlotvarRangeType.Extra );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Extra;
                                CurrPosition++;
                            }
                            else
                            {
                                // No Range specification so: use default : Recent
                                SelectPlotvarRange( PlotvarRangeType.Recent );
                                thisPlotvar.PlotvarRange = PlotvarRangeType.Recent;
                            }

                            // So check if the plotvar Keyword translates to a true CMX data variable
                            int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( Keywords[ CurrPosition ], CUtils.Cmp ) );

                            if ( index != -1 )
                            {
                                // The plot var exists, create the entry for the chart and check the other attributes
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
                                    Sup.LogMessage( $"Parsing User Charts: No Equation found for {thisPlotvar.Keyword}", TraceLevel.Error );
                                    return null;
                                }
                                else
                                    thisPlotvar.EqAllVarList = new List<AllVarInfo>();
                            }
                            else if ( EquationRequired )
                            {
                                Sup.LogMessage( $"Parsing User Charts: No EVAL found for a PLOT statement' for {thisPlotvar.Keyword} when required'", TraceLevel.Error );
                                Sup.LogMessage( $"Parsing User Charts: Equation is required because Plotvariable does not translate to valid JSON variable", TraceLevel.Error );
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
                                            Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AS type '{Keywords[ CurrPosition ]}' for '{thisPlotvar.Keyword}'", TraceLevel.Error );
                                            return null;
                                        }

                                    CurrPosition++;
                                }
                                else
                                {
                                    // Error condition
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AS linetype '{Keywords[ CurrPosition ]}'", TraceLevel.Error );
                                    return null;
                                }
                            }
                            else if ( Keywords[ CurrPosition ].Equals( "As", CUtils.Cmp ) && thisPlotvar.IsStats )
                            {
                                Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AS type '{Keywords[ CurrPosition ]}' for '{thisPlotvar.Keyword}'", TraceLevel.Error );
                                Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Cannot set a plot type for a STATS Plotvariable", TraceLevel.Error );

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
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}", TraceLevel.Error );
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Error around zIndex value of '{thisPlotvar.PlotVar}'", TraceLevel.Error );
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
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}", TraceLevel.Error );
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Error around zIndex value of '{thisPlotvar.PlotVar}'", TraceLevel.Error );
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
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}", TraceLevel.Error );
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Error around LineWidth value of '{thisPlotvar.PlotVar}'", TraceLevel.Error );
                                    return null;
                                }
                            } // End LINEWIDTH

                            // Search for AXIS keyword
                            if ( Keywords[ CurrPosition ].Equals( "Axis", CUtils.Cmp ) )
                            {
                                CurrPosition++; // this one gets us on the Axis specification after the keyword

                                if ( string.IsNullOrEmpty( thisPlotvar.Equation ) )
                                {
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : AXIS specification ignored in absence of (correct) EVAL equation for {thisPlotvar.Keyword}", TraceLevel.Error );
                                    Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Axis specification only relevant for Equations, continuing...", TraceLevel.Error );
                                    CurrPosition++; // this one gets us on the next KeyWord
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
                                            Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Invalid AXIS type '{Keywords[ CurrPosition ]}'", TraceLevel.Error );
                                            return null;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' Exception: {e.Message}", TraceLevel.Error );
                                        Sup.LogMessage( $"Parsing User Charts '{thisChart.Id}' : Error around AXIS spec '{thisPlotvar.PlotVar}'", TraceLevel.Error );
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
                                Sup.LogMessage( $"User Charts '{thisChart.Id}/{thisPlotvar.Keyword}': No Axis was specified or in error. Axistype is set to FREE.", TraceLevel.Error );

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
                                        Sup.LogMessage( $"Parsing User Charts Definitions : Double Info specified on '{thisChart.Id}'.", TraceLevel.Error );
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
                                            Sup.LogMessage( $"Parsing User Charts Definitions : Info specified on '{thisChart.Id}' but no closing quote found.", TraceLevel.Error );
                                            return null;
                                        }
                                    }
                                    else
                                    {
                                        Sup.LogMessage( $"Parsing User Charts Definitions : Info specified on '{thisChart.Id}' but no start quote found.", TraceLevel.Error );
                                        return null;
                                    }
                                }

                                if ( CurrPosition >= Keywords.Count ) break;

                                // Do the possible(!) output
                                if ( Keywords[ CurrPosition ].Equals( "Output", CUtils.Cmp ) )
                                {
                                    if ( OutputDone )
                                    {
                                        Sup.LogMessage( $"Parsing User Charts Definitions : Double Output specified on '{thisChart.Id}'.", TraceLevel.Error );
                                        return null;
                                    }
                                    else OutputDone = true;

                                    CurrPosition++;  // Go to the filename

                                    if ( AllOutputs.Count == 0 && AllCharts.Count == 0 )
                                    {
                                        Sup.LogMessage( $"Parsing User Charts Definitions : Output given for first Chart '{thisChart.Id}'. Cannot specify output for first chart", TraceLevel.Error );
                                    }
                                    else
                                    {
                                        thisOutput.TheseCharts = AllCharts;
                                        AllOutputs.Add( thisOutput );

                                        AllCharts = new List<ChartDef>();
                                        thisOutput = new OutputDef( Keywords[ CurrPosition ] );
                                    }

                                    CurrPosition++;  // Keyword next to the filename
                                }
                            } // While Info or Output (the order does not matter)
                        } // try to detect EOF
                        catch ( Exception e )
                        {
                            Sup.LogMessage( "Parsing User Charts Definitions : Unknown exception while reaching EOF. Incomplete Charts definition.", TraceLevel.Error );
                            Sup.LogMessage( $"Exception found is: {e.Message}", TraceLevel.Error );
                            if ( e.InnerException is not null ) Sup.LogMessage( $"InnerException found is: {e.InnerException}", TraceLevel.Error );
                            return null;
                        }

                        AllCharts.Add( thisChart );
                    }
                    else
                    {
                        // Error condition
                        Sup.LogMessage( $"Parsing User Charts Definitions : Error at EndChart of Chart '{thisChart.Id}'", TraceLevel.Error );
                        Sup.LogMessage( $"Parsing User Charts Definitions : After position '{Keywords[ --CurrPosition ]}'", TraceLevel.Error );
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
                            else { Sup.LogMessage( $"Parsing User Charts Definitions : Illegal use of ColumnRange '{chart.Id}'/'{plotvar.Keyword}' with RECENT", TraceLevel.Error ); return null; }

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
                            if ( !found ) { Sup.LogMessage( $"Parsing User Charts Definitions : STATS variable '{plotvar.Keyword}' not plotted by itself in this CHART", TraceLevel.Error ); return null; }
                        }

                // OK so set the lists and continue
                thisOutput.TheseCharts = AllCharts;
                AllOutputs.Add( thisOutput );

                return AllOutputs;
            }
            catch ( Exception e )
            {
                Sup.LogMessage( $"Error Parsing Exception : {e.Message}", TraceLevel.Error );
                Sup.LogMessage( "Error Parsing Chart definitions - Defaults used.", TraceLevel.Error );
                return null;
            }
        } // ParseChartdefinitions()

        #endregion

    } // Class DefineCharts
}// Namespace
