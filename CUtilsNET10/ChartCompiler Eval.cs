/*
 * ChartsCompiler Eval - Part of CumulusUtils
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
                    Sup.LogMessage( $"Parsing User Charts: Error in Equation {thisEq.Id}", TraceLevel.Error );
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
                        Sup.LogMessage( $"Parsing User Charts: Invalid Plotvariable Name for Eval function", TraceLevel.Error );
                        Sup.LogMessage( $"Parsing User Charts: Plotvariable Name may not be reserved Keyword: {Id}", TraceLevel.Error );
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

                        Sup.LogMessage( $"Parsing User Charts: Evaluating Expression '{rawExpression}'.", TraceLevel.Info );

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
                        Sup.LogMessage( $"Parsing User Charts: No closing ']' found in Expression in '{Id}'.", TraceLevel.Error );
                        return null;
                    }
                }
                else
                {
                    Sup.LogMessage( $"Parsing User Charts: No opening [ found in Eval in '{Id}'.", TraceLevel.Error );
                    return null;
                }
            }
            else
            {
                Sup.LogMessage( $"Parsing User Charts: No EVAL found for a EQUATION statement' for {Id} when required'", TraceLevel.Error );
                return null;
            }
        } // ParseSingleEval

        #endregion

        #region Expression

        private static readonly System.Collections.Frozen.FrozenSet<string> OperatorSet = System.Collections.Frozen.FrozenSet.ToFrozenSet( [ "+", "-", "*", "/", "," ], StringComparer.Ordinal );
        private static readonly System.Collections.Frozen.FrozenSet<string> FunctionSet = System.Collections.Frozen.FrozenSet.ToFrozenSet( [ "sum", "sqrt", "exp", "ln", "pow", "max", "min" ], StringComparer.OrdinalIgnoreCase );
        readonly string[] Operators = [ "+", "-", "*", "/", "," ];
        readonly string[] Brackets = [ "(", ")" ];
        readonly string[] Functions = [ "sum", "sqrt", "exp", "ln", "pow", "max", "min" ];

        string Expression( string[] Exp, ref bool EquationSubstitution, bool CommaPermitted )
        {
            string tmp = "", tmp1;
            int i = 0;

            Sup.LogMessage( "Expression Start", TraceLevel.Verbose );

            try
            {
                tmp = Term( Exp, ref i, ref EquationSubstitution );

                if ( tmp is not null )
                {
                    while ( i < Exp.Length && OperatorSet.Contains( Exp[ i ] ) )
                    {
                        if ( Exp[ i ] == "," && !CommaPermitted )
                        {
                            Sup.LogMessage( $"ParseExpression : Comma is not permitted at this position ", TraceLevel.Error );
                            return null;
                        }

                        // It is an operator so translate to the javascript equivalent.
                        tmp += Exp[ i++ ];

                        tmp1 = Term( Exp, ref i, ref EquationSubstitution );

                        if ( tmp1 is not null )
                            tmp += tmp1;
                        else
                        {
                            Sup.LogMessage( $"ParseExpression : Error in Expression, operator expected ", TraceLevel.Error );
                            return null;
                        }
                    }
                }
                else if ( i < Exp.Length - 1 )
                {
                    Sup.LogMessage( $"ParseExpression : Error in Expression", TraceLevel.Error );
                    return null;
                }
            } // End Try
            catch ( Exception e ) when ( e is IndexOutOfRangeException )
            {
                Sup.LogMessage( $"ParseExpression : Error in Expression, Most likely forgot a matching bracket '(' or ')' or an operator ", TraceLevel.Error );
                return null;
            }

            return tmp;
        }

        string Term( string[] Exp, ref int i, ref bool EquationSubstitution )
        {
            string tmp = "", tmpTerm = "";

            Sup.LogMessage( "Term Start", TraceLevel.Verbose );

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
                            Sup.LogMessage( $"Term : Error in Term in pos {i}", TraceLevel.Error );
                            return null;
                        }
                    }
                    else if ( char.IsLetter( Exp[ i ][ 0 ] ) )
                    {
                        string tmpWord = Exp[ i ];

                        if ( FunctionSet.Contains( tmpWord ) )
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
                                    Sup.LogMessage( $"Term : Error in Term in pos {i}", TraceLevel.Error );
                                    return null;
                                }
                            }
                            else
                            {
                                Sup.LogMessage( $"Term : Error in Function in pos {tmp}", TraceLevel.Error );
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
                                        Sup.LogMessage( $"Term : {tmpWord} is neither an existing Plotvariable nor a predefined equation.", TraceLevel.Error );
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
                        catch ( Exception e ) { Sup.LogMessage( $"Term : Error in Expression, not a number {tmp} ({e.Message})", TraceLevel.Error ); return null; }
                    }
                    else
                        break;  // Can only be an operator, anything else is an error
                }

                return tmp;
            }
            catch ( Exception e ) when ( e is IndexOutOfRangeException )
            {
                // To prevent extensive error handling we let Exceptions occur, fail and continue
                Sup.LogMessage( $"Term : Error in Expression {Exp}, Most likely forgot a matching bracket '(' or ')' ", TraceLevel.Error );
                return null;
            }
        } // Term

        #endregion

        #region ExpressionSupport

        List<string> PrepareRawExpression( string rawExpression )
        {
            if ( string.IsNullOrWhiteSpace( rawExpression ) )
                return [];

            // Direct span tokenization: avoids string allocation per character
            ReadOnlySpan<char> span = rawExpression.AsSpan();
            var rawExp = new List<string>( capacity: span.Length / 2 + 1 );
            int tokenStart = -1;

            for ( int i = 0; i < span.Length; i++ )
            {
                char c = span[ i ];
                if ( char.IsWhiteSpace( c ) )
                {
                    if ( tokenStart != -1 )
                    {
                        rawExp.Add( span.Slice( tokenStart, i - tokenStart ).ToString() );
                        tokenStart = -1;
                    }
                    continue;
                }

                if ( IsOperatorOrBracket( c ) )
                {
                    if ( tokenStart != -1 )
                    {
                        rawExp.Add( span.Slice( tokenStart, i - tokenStart ).ToString() );
                        tokenStart = -1;
                    }
                    rawExp.Add( c.ToString() );
                }
                else if ( tokenStart == -1 )
                {
                    tokenStart = i;
                }
            }

            if ( tokenStart != -1 )
                rawExp.Add( span.Slice( tokenStart ).ToString() );

            return rawExp;
        }

        private static bool IsOperatorOrBracket( char c ) =>
            c is '+' or '-' or '*' or '/' or ',' or '(' or ')';

        #endregion
    }
}
