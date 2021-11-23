/*
 * ChartsCompiler Eval - Part of CumulusUtils
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

namespace CumulusUtils
{
    partial class ChartsCompiler
    {

        #region Equations
        bool Equationblock = false;

        internal bool ParseEquationBlock()
        {
            Equationblock = true;

            // Parse the Equation block
            do
            {
                thisEq.Id = Keywords[ CurrPosition++ ];
                thisEq.Equation = ParseSingleEval( thisEq.Id );

                if ( string.IsNullOrEmpty( thisEq.Equation ) )
                    return false;
                else
                    AllEquations.Add( thisEq );
            } while ( !Keywords[ CurrPosition ].Equals( "Chart", cmp ) && CurrPosition < Keywords.Count - 1 );

            Equationblock = false;  // Checked by the parser when substituting

            return true;
        }

        #endregion Equations

        #region ParseSingleEval
        internal string ParseSingleEval( string Id ) // Returns the expression parsed or empty
        {
            // Do the EVAL:
            if ( Keywords[ CurrPosition ].Equals( "Eval", cmp ) )
            {
                if ( !Equationblock )
                    if ( Array.Exists( PlotvarKeyword, word => word.Equals( Id, cmp ) ) )
                    {
                        Sup.LogTraceInfoMessage( $"Parsing User Charts: Invalid Plotvariable Name for Eval function" );
                        Sup.LogTraceInfoMessage( $"Parsing User Charts: Plotvariable Name may not be reserved Keyword: {Id}" );
                        return null;
                    }

                string rawExpression = "";

                CurrPosition++;

                if ( Keywords[ CurrPosition++ ] == "[" )
                {
                    do
                    {
                        rawExpression += Keywords[ CurrPosition++ ];
                    } while ( Keywords[ CurrPosition ] != "]" && CurrPosition < Keywords.Count - 1 );

                    if ( Keywords[ CurrPosition++ ] == "]" )
                    {
                        string tmp = "";
                        List<string> rawExp = new List<string>();

                        rawExp = PrepareRawExpression( rawExpression );

                        Sup.LogTraceInfoMessage( $"Parsing User Charts: Evaluating Expression '{rawExpression}'." );

                        bool EquationSubstitution = false;
                        tmp = Expression( rawExp.ToArray(), ref EquationSubstitution, false );

                        // Repeated substitution is possible so loop while all nested substitutions are done
                        //
                        while ( EquationSubstitution && !string.IsNullOrEmpty( tmp ) )
                        {
                            EquationSubstitution = false;
                            rawExp = PrepareRawExpression( tmp );
                            tmp = Expression( rawExp.ToArray(), ref EquationSubstitution, false );
                        }

                        return tmp;
                    }
                    else
                    {
                        Sup.LogTraceErrorMessage( $"Parsing User Charts: No closing ']' found in Expression in '{Id}'." );
                        return null;
                    }
                }
                else
                {
                    Sup.LogTraceErrorMessage( $"Parsing User Charts: No opening [ found in Eval in '{Id}'." );
                    return null;
                }
            }
            else
            {
                Sup.LogTraceErrorMessage( $"Parsing User Charts: No EVAL found for a EQUATION statement' for {Id} when required'" );
                return null;
            }
        } // ParseSingleEval

        #endregion

        #region Expression

        readonly string[] Operators = { "+", "-", "*", "/", "," }; // The comma is not really an operator but is needed for the pow function
                                                                   // and possibly other parameters for other functions in future
        readonly string[] Brackets = { "(", ")" };
        readonly string[] Functions = { "sum", "sqrt", "exp", "ln", "pow", "max", "min" };

        string Expression( string[] Exp, ref bool EquationSubstitution, bool CommaPermitted )
        {
            string tmp = "", tmp1;
            int i = 0;

            Sup.LogTraceVerboseMessage( "Expression Start" );

            try
            {
                tmp = Term( Exp, ref i, ref EquationSubstitution );

                if ( tmp != null )
                {
                    while ( i < Exp.Length && Array.Exists( Operators, word => word.Equals( Exp[ i ] ) ) )
                    {
                        if ( Exp[ i ] == "," && !CommaPermitted )
                        {
                            Sup.LogTraceErrorMessage( $"ParseExpression : Comma is not permitted at this position " );
                            return null;
                        }

                        // It is an operator so translate to the javascript equivalent.
                        tmp += Exp[ i++ ];

                        tmp1 = Term( Exp, ref i, ref EquationSubstitution );

                        if ( tmp1 != null )
                            tmp += tmp1;
                        else
                        {
                            Sup.LogTraceErrorMessage( $"ParseExpression : Error in Expression, operator expected " );
                            return null;
                        }
                    }
                }
                else if ( i < Exp.Length - 1 )
                {
                    Sup.LogTraceErrorMessage( $"ParseExpression : Error in Expression" );
                    return null;
                }
            } // End Try
            catch ( Exception e ) when ( e is IndexOutOfRangeException )
            {
                Sup.LogTraceErrorMessage( $"ParseExpression : Error in Expression, Most likely forgot a matching bracket '(' or ')' or an operator " );
                return null;
            }

            return tmp;
        }

        string Term( string[] Exp, ref int i, ref bool EquationSubstitution )
        {
            string tmp = "", tmpTerm = "";

            Sup.LogTraceVerboseMessage( "Term Start" );

            try
            {
                for ( ; i < Exp.Length; i++ )
                {
                    List<string> subExp = new List<string>();
                    string[] subExpArr;

                    if ( string.IsNullOrEmpty( Exp[ i ] ) )
                        continue;

                    if ( Exp[ i ] == "(" )
                    {
                        int b = 0;

                        while ( true )
                        {
                            i++;

                            if ( Exp[ i ] == "(" )
                                b++;
                            else if ( Exp[ i ] == ")" && b > 0 )
                                b--;
                            else if ( Exp[ i ] == ")" )
                                break;

                            subExp.Add( Exp[ i ] );
                        }

                        subExpArr = subExp.ToArray();

                        tmpTerm = Expression( subExpArr, ref EquationSubstitution, false );

                        if ( tmpTerm != null )
                            tmp += "(" + tmpTerm + ")";
                        else
                        {
                            Sup.LogTraceErrorMessage( $"Term : Error in Term in pos {i}" );
                            return null;
                        }
                    }
                    else if ( char.IsLetter( Exp[ i ][ 0 ] ) )
                    {
                        string tmpWord = Exp[ i ];

                        if ( Array.Exists( Functions, word => word.Equals( tmpWord, cmp ) ) )
                        {
                            // It is a function so translate to the javascript equivalent. To do so we must know its argument so we continue in Term
                            // Expecting ( and ) with an expression in between
                            tmp += Functions[ Array.FindIndex( Functions, word => word.Equals( tmpWord, cmp ) ) ];

                            i++;
                            if ( Exp[ i ] == "(" )
                            {
                                int b = 0;

                                while ( true )
                                {
                                    i++;

                                    if ( Exp[ i ] == "(" )
                                        b++;
                                    else if ( Exp[ i ] == ")" && b > 0 )
                                        b--;
                                    else if ( Exp[ i ] == ")" )
                                        break;

                                    subExp.Add( Exp[ i ] );
                                }

                                subExpArr = subExp.ToArray();

                                bool commaPermitted = "pow".Equals( tmpWord, cmp ) || "max".Equals( tmpWord, cmp ) || "min".Equals( tmpWord, cmp );
                                tmpTerm = Expression( subExpArr, ref EquationSubstitution, commaPermitted );

                                if ( tmpTerm != null )
                                    tmp += "(" + tmpTerm + ")";
                                else
                                {
                                    Sup.LogTraceErrorMessage( $"Term : Error in Term in pos {i}" );
                                    return null;
                                }
                            }
                            else
                            {
                                Sup.LogTraceErrorMessage( $"Term : Error in Function in pos {tmp}" );
                                return null;
                            }
                        }
                        else
                        {
                            // not a function so must be a variable and we're done, return to expression
                            if ( !Equationblock )
                            {
                                if ( Array.Exists( PlotvarKeyword, word => word.Equals( tmpWord, cmp ) ) )
                                    tmp += tmpWord;
                                else
                                {
                                    bool EqExists = false;

                                    foreach ( EqDef Eq in AllEquations )
                                    {
                                        if ( tmpWord == Eq.Id )
                                        {
                                            tmp += Eq.Equation;
                                            EqExists = true;
                                            EquationSubstitution = true;
                                            break;
                                        }
                                    }

                                    if ( !EqExists )
                                    {
                                        Sup.LogTraceErrorMessage( $"Term : {tmpWord} is neither an existing Plotvariable nor a predefined equation." );
                                        return null;
                                    }
                                }
                            }
                            else
                                tmp += tmpWord;
                        }
                    }
                    else if ( char.IsDigit( Exp[ i ][ 0 ] ) )
                    {
                        double nmbr;

                        // How about decimal point??? Decimal point does not separate so 5.5 gets here as one element. But 5.B as well
                        // This means I have to  parse the number. If it parses OK it is a number, if it does not: error
                        tmp += Exp[ i ];

                        try { nmbr = Convert.ToDouble( tmp, ci ); }
                        catch ( Exception e ) { Sup.LogTraceErrorMessage( $"Term : Error in Expression, not a number {tmp} ({e.Message})" ); return null; }
                    }
                    else
                        break;  // Can only be an operator, anything else is an error
                }

                return tmp;
            }
            catch ( Exception e ) when ( e is IndexOutOfRangeException )
            {
                // To prevent extensive error handling we let Exceptions occur, fail and continue
                Sup.LogTraceErrorMessage( $"Term : Error in Expression {Exp}, Most likely forgot a matching bracket '(' or ')' " );
                return null;
            }
        } // Term

        #endregion

        #region ExpressionSupport

        List<string> PrepareRawExpression( string rawExpression )
        {
            string tmp = "";
            List<string> rawExp = new List<string>();

            rawExpression = Sup.StringRemoveWhiteSpace( rawExpression );

            for ( int i = 0; i < rawExpression.Length; i++ )
                if ( Array.Exists( Operators, word => word.Equals( "" + rawExpression[ i ] ) ) ||
                     Array.Exists( Brackets, word => word.Equals( "" + rawExpression[ i ] ) ) )
                {
                    if ( !string.IsNullOrEmpty( tmp ) )
                        rawExp.Add( tmp );
                    rawExp.Add( "" + rawExpression[ i ] );
                    tmp = "";
                }
                else
                    tmp += rawExpression[ i ];

            if ( !string.IsNullOrEmpty( tmp ) )
                rawExp.Add( tmp );

            return rawExp;
        }

        #endregion
    } // Class DefineCharts
}// Namespace
