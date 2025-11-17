/*
 * ChartsCompiler Eval - Part of CumulusUtils
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

        public bool ParseEquationBlock()
        {
            Equationblock = true;

            // Parse the Equation block
            do
            {
                thisEq.Id = Keywords[ CurrPosition++ ];
                thisEq.Equation = ParseSingleEval( thisEq.Id );

                if ( string.IsNullOrEmpty( thisEq.Equation ) )
                {
                    Sup.LogTraceErrorMessage( $"Parsing User Charts: Error in Equation {thisEq.Id}" );
                    return false;
                }
                else
                    AllEquations.Add( thisEq );
            } while ( !Keywords[ CurrPosition ].Equals( "Chart", CUtils.Cmp ) && CurrPosition < Keywords.Count - 1 );

            Equationblock = false;  // Checked by the parser when substituting

            return true;
        }

        #endregion Equations

        #region ParseSingleEval
        public string ParseSingleEval( string Id ) // Returns the expression parsed or empty
        {
            // Do the EVAL:
            if ( Keywords[ CurrPosition ].Equals( "Eval", CUtils.Cmp ) )
            {
                if ( !Equationblock )
                    if ( Array.Exists( PlotvarKeyword, word => word.Equals( Id, CUtils.Cmp ) ) )
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
                        tmp = Expression( Exp: rawExp.ToArray(), EquationSubstitution: ref EquationSubstitution, CommaPermitted: false );

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

                if ( tmp is not null )
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

                        if ( tmp1 is not null )
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

                        if ( tmpTerm is not null )
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

                        if ( Array.Exists( Functions, word => word.Equals( tmpWord, CUtils.Cmp ) ) )
                        {
                            // It is a function so translate to the javascript equivalent. To do so we must know its argument so we continue in Term
                            // Expecting ( and ) with an expression in between
                            tmp += Functions[ Array.FindIndex( Functions, word => word.Equals( tmpWord, CUtils.Cmp ) ) ];

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

                                bool commaPermitted = "pow".Equals( tmpWord, CUtils.Cmp ) || "max".Equals( tmpWord, CUtils.Cmp ) || "min".Equals( tmpWord, CUtils.Cmp );
                                tmpTerm = Expression( subExpArr, ref EquationSubstitution, commaPermitted );

                                if ( tmpTerm is not null )
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
                                if ( Array.Exists( PlotvarKeyword, word => word.Equals( tmpWord, CUtils.Cmp ) ) )
                                {
                                    int index = Array.FindIndex( PlotvarKeyword, word => word.Equals( tmpWord, CUtils.Cmp ) );
                                    tmp += PlotvarKeyword[ index ];
                                }
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

                        try { nmbr = Convert.ToDouble( tmp, CUtils.Inv ); }
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

            rawExpression = CuSupport.StringRemoveWhiteSpace( rawExpression, " " );

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
