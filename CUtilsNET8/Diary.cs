/*
 * Diary - Part of CumulusUtils
 *
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
 */

/*
 * Because of:
 *   - https://cumulus.hosiene.co.uk/viewtopic.php?t=20796&hilit=snow+log
 *   - https://cumulus.hosiene.co.uk/viewtopic.php?p=176371&hilit=diary#p176371
 *   - see also: https://cumulus.hosiene.co.uk/search.php?keywords=diary
 *   - https://www.komokaweather.com/weather/snowfall-log.pdf
 */

using System.Collections.Generic;
using System;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;
using System.Linq;

namespace CumulusUtils
{
    public struct DiaryValue
    {
        public DateTime ThisDate;
        public float? snow24h;
        public float? snowDepth;
    }

    public class Diary
    {
        const int FieldWidth = 20;

        readonly CuSupport Sup;
        readonly List<DiaryValue> DiaryValues;

        public Diary( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "Diary constructor: starting" );

            DiaryValues = LoadDiaryDatabase();

            // After the next call DiaryList contains all Diary fields except where only the entry is filled
            if ( DiaryValues == null )
            {
                Sup.LogTraceInfoMessage( "Diary database: No Data" );
                CUtils.HasDiaryMenu = false;
            }
            else
            {
                Sup.LogTraceInfoMessage( $"Diary database: {DiaryValues.Count} records" );
                CUtils.HasDiaryMenu = true;
            }

            Sup.LogTraceInfoMessage( "Diary constructor: stop" );

            return;
        }

        // This function needs to run when not thrifty
        public void GenerateDiaryDisplay()
        {
            Sup.LogDebugMessage( "Generating Diary module - Starting" );

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}{Sup.DiaryOutputFilename}", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CuSupport.CopyrightForGeneratedFiles() );

                of.WriteLine( $"{CuSupport.GenjQueryIncludestring()}" );

                of.WriteLine( "<script>" );
                of.WriteLine( "console.log('Module DIARY...');" );
                of.WriteLine( "$(function() {" );
                of.WriteLine( "  $('#year').change(function() {" );
                of.WriteLine( "    loadDIARYreport('Diary' + $('#year').val() + '.txt');" );
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRNext').click(function() {" );
                of.WriteLine( "    if ( $('#year option:selected').val() != $('#year option').last().val() ) {" );
                of.WriteLine( "      $('#year option:selected').next('option').prop('selected', 'selected');" );
                of.WriteLine( "      $('#year').trigger('change');" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );

                of.WriteLine( "  $('#YRPrev').click(function() {" );
                of.WriteLine( "    if ( $('#year option:selected').val() != $('#year option').first().val() ) {" );
                of.WriteLine( "      $('#year option:selected').prev('option').prop('selected', 'selected');" );
                of.WriteLine( "      $('#year').trigger('change');" );
                of.WriteLine( "    };" ); // else do nothing
                of.WriteLine( "  });" );
                of.WriteLine( "  $('#year').trigger('change');" );
                of.WriteLine( "});" );

                of.WriteLine( "function loadDIARYreport( filename )" );
                of.WriteLine( "{" );
                of.WriteLine( "  $.ajax({" );
                of.WriteLine( "    url: filename," );
                of.WriteLine( "    timeout: 2000," );
                of.WriteLine( "    cache: false," );
                of.WriteLine( "    headers:{'Access-Control-Allow-Origin': '*'}," );
                of.WriteLine( "    crossDomain: true" );
                of.WriteLine( "  }).fail( function (jqXHR, textStatus, errorThrown) {" );
                of.WriteLine( "      console.log( 'loadDIARYreport: ' + textStatus + ' : ' + errorThrown );" );
                of.WriteLine( "  }).done( function (response, responseStatus) {" );
                of.WriteLine( "      $( '#DIARYplaceholder' ).html( response );" );
                of.WriteLine( "  });" );
                of.WriteLine( "};" );
                of.WriteLine( "</script>" );


                of.WriteLine( "<style>" );
                of.WriteLine( "#report{" );
                of.WriteLine( "  font-family: arial;" );
                of.WriteLine( "  border-radius: 15px;" );
                of.WriteLine( "  border-spacing: 0;" );
                of.WriteLine( "  border: 1px solid #b0b0b0;" );
                of.WriteLine( "}" );
                of.WriteLine( ".diary_rep_container {" );
                of.WriteLine( "  font-family: courier new,courier,monospace;" );
                of.WriteLine( "  width: 1050px;" );
                of.WriteLine( "  margin: 0 auto;" );
                of.WriteLine( "}" );
                of.WriteLine( ".diary_rep_container pre {" );
                of.WriteLine( $"  color: {Sup.GetUtilsIniValue( "Diary", "ColorDiaryText", "Black" )};" );
                of.WriteLine( $"  background-color: {Sup.GetUtilsIniValue( "NOAA", "ColorDiaryBackground", "#f9f8EB" )};" );
                of.WriteLine( "  font-family: monospace;" );
                of.WriteLine( "  font-size: 9pt;" );
                of.WriteLine( "  font-weight: normal;" );
                of.WriteLine( "  text-align: left;" );
                of.WriteLine( "  border: 1px solid #000000;" );
                of.WriteLine( "  border-radius: 10px 10px 10px 10px;" );
                of.WriteLine( "  padding: 20px 0px 25px 20px;" );
                of.WriteLine( "}" );
                of.WriteLine( ".buttonFat {border-radius: 4px; margin-right:10px; margin-left:10px; }" );
                of.WriteLine( ".buttonSlim {border-radius: 4px;}" );
                of.WriteLine( "</style>" );

                of.WriteLine( "<div id=report><br/>" );
                of.WriteLine( "<p style='text-align:center;'>" );
                of.WriteLine( $"<input type='button' class=buttonFat id='YRPrev' value='{Sup.GetCUstringValue( "General", "PrevYear", "Prev Year", false )}'>" );
                of.WriteLine( "<select id='year'>" );

                for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
                {
                    if ( i < CUtils.YearMax )
                        of.WriteLine( $"<option value='{i}'>{i}</option>" );
                    else
                        of.WriteLine( $"<option value='{i}' selected>{i}</option>" );
                }

                of.WriteLine( "</select>" );

                of.WriteLine( $"<input type='button' class=buttonFat id='YRNext' value='{Sup.GetCUstringValue( "General", "NextYear", "Next Year", false )}'>" );

                of.WriteLine( "</p>" );

                of.WriteLine( "<div class='diary_rep_container'>" ); // Format for table
                of.WriteLine( "<pre id='DIARYplaceholder'></pre>" );
                of.WriteLine( "</div>" ); // Format for table

                if ( !CUtils.DoWebsite )
                {
                    of.WriteLine( $"<p style='text-align:center;font-size: 12px;'>{CuSupport.FormattedVersion()} - {CuSupport.Copyright()}</p>" );
                }

                of.WriteLine( "</div>" ); // from div report
            } // End of the  module

            Sup.LogTraceInfoMessage( "End Generating Diary" );

            return;
        }

        // =================================================================================================================================
        // Below are the generation 
        // This function needs to run always when calling 
        public void GenerateDiaryReport()   // Generate the file to actually load into display
        {
            float Latitude = Convert.ToSingle( Sup.GetCumulusIniValue( "Station", "Latitude", "" ), CUtils.Inv );
            bool NorthernHemisphere = Latitude >= 0;

            Sup.LogTraceInfoMessage( $"Start Generating Diary data" );

            if (NorthernHemisphere)
            {
                for ( int i = CUtils.YearMin; i <= CUtils.YearMax; i++ )
                {
                    if ( CUtils.Thrifty && i < CUtils.YearMax ) continue; // Under thrifty we only generate the current winterseason

                    Sup.LogTraceInfoMessage( $"Start Generating Diary data for {i}" );

                    generateDiaryForThisYear( i );

                } // Loop over all years
            }
            else
            {
                Sup.LogTraceInfoMessage( $"GenerateDiaryReport: No snow report  for the southern hemisphere has been implemented yet." );
                Sup.LogTraceInfoMessage( $"GenerateDiaryReport: Please request when required." );
            }

            Sup.LogTraceInfoMessage( $"Start Generating Diary data" );
            return;
        }

        private void generateDiaryForThisYear(int thisYear )
        {
            string thisLine2 = "";

            using ( StreamWriter of = new StreamWriter( $"{Sup.PathUtils}Diary{thisYear}.txt", false, Encoding.UTF8 ) )
            {
                of.WriteLine( CString( $"{thisYear}/{thisYear + 1} Seasonal snowfall in {Sup.GetCumulusIniValue( "Station", "LocName", "" )}", 15 * FieldWidth/2 ) );
                of.WriteLine( CString( $"Measured each day at about " +
                    $"{Sup.GetCumulusIniValue( "Station", "SnowDepthHour", "" )} hr for the previous 24 hrs " +
                    $"(in {( Sup.GetCumulusIniValue( "Station", "SnowDepthUnit", "" ) == "0" ? "cm" : "in" )})", 15 * FieldWidth/2 ) );
                of.WriteLine();

                of.WriteLine( CString( "Day", FieldWidth / 2 ) +
                    CString( Months.Oct.ToString(), FieldWidth ) + CString( Months.Nov.ToString(), FieldWidth ) +
                    CString( Months.Dec.ToString(), FieldWidth ) + CString( Months.Jan.ToString(), FieldWidth ) +
                    CString( Months.Feb.ToString(), FieldWidth ) + CString( Months.Mar.ToString(), FieldWidth ) + CString( Months.Apr.ToString(), FieldWidth ) );

                of.WriteLine( $"{"",FieldWidth / 2}" + 
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) +
                    CString( "24 hrs", FieldWidth / 2 ) + CString( "Snow", FieldWidth / 2 ) );

                of.WriteLine( CString( "----------------------------------------------------------------------------------------------------------------------------------------------------", 150 ) );

                for ( int thisDay = 1; thisDay <= 31; thisDay++ )
                {
                    thisLine2 = "";

                    thisLine2 += createLineForDay( thisYear, thisDay );
                    of.WriteLine( $"{thisLine2}" );

                } // Loop over all days
            } // User the current year as the start of the season

        }

        private string createLineForDay( int thisYear, int thisDay )
        {
            string thisLine = "";
            float? SnowDepth, Snow24h;
            string StrSnowDepth = "";
            string StrSnow24h = "";

            DiaryValue thisValue;

            thisLine += CString( thisDay.ToString(), FieldWidth/2);

            for ( int thisMonth = 10; thisMonth <= 12; thisMonth++ ) // Loop over months
            {
                try
                {
                    thisValue = DiaryValues.Where( x => x.ThisDate.Year == thisYear )
                                        .Where( x => x.ThisDate.Month == thisMonth )
                                        .Where( x => x.ThisDate.Day == thisDay ).First();

                    SnowDepth = thisValue.snowDepth; 
                    Snow24h = thisValue.snow24h;

                    StrSnowDepth = $"{( SnowDepth == null || SnowDepth == 0.0 ? "---" : SnowDepth ):F1}";
                    StrSnow24h = $"{( Snow24h == null || Snow24h == 0.0 ? "---" : Snow24h ):F1}";
                    thisLine += CString( StrSnow24h, FieldWidth / 2 ) + CString( StrSnowDepth, FieldWidth / 2 );
                }
                catch ( Exception )
                {
                    thisLine += CString( "---", FieldWidth );
                }
            }  // Loop over months

            for ( int thisMonth = 1; thisMonth <= 4; thisMonth++ ) // Loop over months
            {
                try
                {
                    thisValue = DiaryValues.Where( x => x.ThisDate.Year == thisYear + 1)
                                        .Where( x => x.ThisDate.Month == thisMonth )
                                        .Where( x => x.ThisDate.Day == thisDay ).First();

                    SnowDepth = thisValue.snowDepth;
                    Snow24h = thisValue.snow24h;

                    StrSnowDepth = $"{( SnowDepth == null || SnowDepth == 0.0 ? "---" : SnowDepth ):F1}";
                    StrSnow24h = $"{( Snow24h == null || Snow24h == 0.0 ? "---" : Snow24h ):F1}";
                    thisLine += CString( StrSnow24h, FieldWidth / 2 ) + CString( StrSnowDepth, FieldWidth / 2 );
                }
                catch ( Exception )
                {
                    thisLine += CString( "---", FieldWidth );
                }
            }  // Loop over months
            return thisLine;
        }
        private List<DiaryValue> loadDiaryDatabase()
        {
            List<DiaryValue> tmpList = new();

            try
            {
                using ( SqliteConnection thisConnection = new( $"DataSource=data/diary.db; Mode=ReadOnly;" ) )
                {
                    thisConnection.Open();

                    SqliteCommand command = thisConnection.CreateCommand();
                    command.CommandText = @"SELECT * FROM DiaryData";

                    using ( SqliteDataReader reader = command.ExecuteReader() )
                    {
                        int OrdinalTimestamp = reader.GetOrdinal( "Date" );
                        int OrdinalEntry = reader.GetOrdinal( "Entry" );
                        int OrdinalSnow24h = reader.GetOrdinal( "Snow24h" );
                        int OrdinalSnowDepth = reader.GetOrdinal( "SnowDepth" );

                        while ( reader.Read() )
                        {
                            if ( reader.IsDBNull( OrdinalSnow24h ) && reader.IsDBNull( OrdinalSnowDepth ) )
                            {
                                continue; // Only records with snow height count. The Entry is ignored
                            }
                            else
                            {
                                DiaryValue tmp = new()
                                {
                                    //ThisDate = DateTimeOffset.FromUnixTimeSeconds( reader.GetInt64( OrdinalTimestamp ) ).DateTime.ToLocalTime(),
                                    ThisDate = reader.GetDateTime( OrdinalTimestamp ),
                                    snow24h = reader.IsDBNull( OrdinalSnow24h ) ? 0 : reader.GetFloat( OrdinalSnow24h ),
                                    snowDepth = reader.IsDBNull( OrdinalSnowDepth ) ? 0 : reader.GetFloat( OrdinalSnowDepth )
                                };

                                tmpList.Add( tmp );
                                Sup.LogTraceVerboseMessage( $"Value - Date: {tmp.ThisDate} Snow24h: {tmp.snow24h} SnowDepth: {tmp.snowDepth}" );
                            }

                        } // Loop over the records
                    } // using: Execute the command

                    thisConnection.Close();
                } // using: Connection

                if ( tmpList.Count > 0 ) return tmpList;
                else
                {
                    Sup.LogDebugMessage( "Generating Diary - No Data" );
                    return null;
                }
            }
            catch ( SqliteException e )
            {
                Console.WriteLine( $"ReadDiaryFromSQL: Exception - {e.ErrorCode} - {e.Message}" );
                Console.WriteLine( $"ReadDiaryFromSQL: No Diary data" );
                return null;
            }
        } // End GetDiaryDatabase()


        // Center the string on the width of the container
        private static string CString( string s, int width )
        {
            if ( s.Length >= width )
            {
                return s;
            }

            int leftPadding = ( width - s.Length ) / 2;
            int rightPadding = width - s.Length - leftPadding;

            return new string( ' ', leftPadding ) + s + new string( ' ', rightPadding );
        }
    } // End class Diary
} // End Namespace 
