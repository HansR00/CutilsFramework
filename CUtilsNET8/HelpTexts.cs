/*
 * GraphWind - Part of CumulusUtils
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Helpers;

namespace CumulusUtils
{
    public class HelpTexts
    {
        readonly CuSupport Sup;
        readonly Dictionary<string, string> Helptexts = new Dictionary<string, string>();

        #region Constructor
        public HelpTexts( CuSupport s )
        {
            Sup = s;

            Sup.LogDebugMessage( "HelpTexts Contructor: start" );

            if ( !File.Exists( $"{Sup.PathUtils}{Sup.CUhelptexts}" ) )
            {
                using ( StreamWriter ht = new StreamWriter( $"{Sup.PathUtils}{Sup.CUhelptexts}" ) )
                {
                    // Create the slots for the fixed climate charts to be filled in by the user
                    // The slots for the compiler will be made on the fly when required by the user
                    ht.WriteLine( "HT_DailyRain = \" \"" );
                    ht.WriteLine( "HT_MonthlyRain = \" \"" );
                    ht.WriteLine( "HT_YearlyRainStats = \" \"" );
                    ht.WriteLine( "HT_YearlyMonthlyRainStats = \" \"" );
                    ht.WriteLine( "HT_RAINvsEVT = \" \"" );

                    ht.WriteLine( "HT_MonthlyTemp = \" \"" );
                    ht.WriteLine( "HT_YearlyTempStats = \" \"" );
                    ht.WriteLine( "HT_YearlyMonthlyTempStats = \" \"" );
                    ht.WriteLine( "HT_WarmerDays = \" \"" );
                    ht.WriteLine( "HT_HeatMap = \" \"" );

                    ht.WriteLine( "HT_WindRose = \" \"" );
                    ht.WriteLine( "HT_WindRun = \" \"" );

                    ht.WriteLine( "HT_YearlySolarHRSstats = \" \"" );
                    ht.WriteLine( "HT_YearlyMonthlySolarHRSstats = \" \"" );
                    ht.WriteLine( "HT_YearlyInsolationStats = \" \"" );
                    ht.WriteLine( "HT_YearlyMonthlyInsolationStats = \" \"" );

                    ht.WriteLine( "HT_TempSum = \" \"" );
                    ht.WriteLine( "HT_GrowingDegreeDays = \" \"" );
                    ht.WriteLine( "HT_ThermalSeasons = \" \"" );
                    ht.WriteLine( "HT_DailyEVT = \" \"" );
                    ht.WriteLine( "HT_MonthlyEVT = \" \"" );
                    ht.WriteLine( "HT_ClashOfAverages = \" \"" );
                }
            }

            // Now we are sure the file exists
            // Read the file and fill the dictionary
            string Contents = "", Key = "";
            string[] LinesArray;
            List<string> Keywords;

            LinesArray = File.ReadAllLines( $"{Sup.PathUtils}{Sup.CUhelptexts}", Encoding.UTF8 );

            foreach ( string line in LinesArray )
                if ( line.IsBlank() || line[ 0 ] == ';' ) continue;
                else
                    Contents += line + ' ';

            Contents = Regex.Replace( Contents, @"\s+", " " );

            Keywords = Contents.Split( ' ' ).Where( tmp => !tmp.IsBlank() ).ToList();
            int i = 0;

            while ( i < Keywords.Count )
            {
                Key = Keywords[ i++ ];

                if ( Keywords[ i++ ] == "=" )
                {
                    string thisText = "";

                    // Read all subsequent text between quotes
                    try
                    {
                        if ( Keywords[ i++ ] == "\"" )
                        {
                            while ( !Keywords[ i ].Equals( "\"" ) ) thisText += " " + Keywords[ i++ ];

                            Helptexts.Add( Key, thisText );
                            i++;
                        }
                        else { Sup.LogTraceErrorMessage( $"HelpTexts Constructor Error near {Key}: \" expected" ); break; }
                    }
                    catch ( Exception e ) when ( e is IndexOutOfRangeException )
                    {
                        Sup.LogTraceErrorMessage( $"Parsing User Charts Definitions : Info specified on '{Key}' but no closing quote found." );
                    }
                }
                else { Sup.LogTraceErrorMessage( $"HelpTexts Constructor Error near {Key}: '=' expected" ); break; }
            } // While loop: fall through when in error or when file is exhausted

            Sup.LogTraceInfoMessage( "HelpTexts Contructor: stop" );

            return;
        }

        #endregion

        #region GetHelptext

        public string GetHelpText( string key )
        {
            string retval;

            if ( Helptexts.ContainsKey( key ) ) retval = Helptexts[ key ];
            else
            {
                Sup.LogTraceWarningMessage( $"HelpTexts unknown {key}, returning empty string" );
                retval = "";
            }

            return retval;
        }

        #endregion
    }
}
