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

namespace CumulusUtils
{
    public struct DiaryValue
    {
        public DateTime ThisDate;
        public int snowLying;
        public int snowFalling;
        public float snowDepth;
    }

    public class Diary
    {
        readonly CuSupport Sup;
        readonly List<DiaryValue> DiaryValues;

        public Diary( CuSupport s )
        {
            Sup = s;

            Sup.LogTraceInfoMessage( "Diary constructor: starting" );

            DiaryValues = GetDiaryDatabase();

            // After the next call DiaryList contains all Diary fields
            if ( DiaryValues == null )
            {
                Sup.LogTraceInfoMessage( "Diary database: No Data" );
                CUtils.HasDiaryMenu = false;
            }
            else
            {
                CUtils.HasDiaryMenu = true;
            }

            Sup.LogTraceInfoMessage( "Diary constructor: stop" );

            return;
        }

        public void GenerateDiary()
        {
            Sup.LogDebugMessage( "Generating Diary - Starting" );

            using ( StreamWriter rt = new StreamWriter( $"{Sup.PathUtils}{Sup.DiaryOutputFilename}", false, Encoding.UTF8 ) )
            {
                rt.WriteLine( "This will in future contain the diary contents" );
            }

            Sup.LogTraceInfoMessage( "End Generating Diary" );

            return;
        }

        private List<DiaryValue> GetDiaryDatabase()
        {
            List<DiaryValue> tmpList = new();

            //string DataSource = Sup.GetUtilsIniValue( "Diary", "DataSource", "data/diary.db" );
            //string Mode = Sup.GetUtilsIniValue( "Diary", "Mode", "ReadOnly" );

            //Sup.LogTraceInfoMessage( $"Generating Diary connectionstring - DataSource={DataSource}; Mode={Mode};" );

            try
            {
                //using ( SqliteConnection thisConnection = new( $"DataSource={DataSource}; Mode={Mode};" ) )
                using ( SqliteConnection thisConnection = new( $"DataSource=data/diary.db; Mode=ReadOnly;" ) )
                {
                    thisConnection.Open();

                    SqliteCommand command = thisConnection.CreateCommand();
                    command.CommandText = @"SELECT * FROM DiaryData";

                    using ( SqliteDataReader reader = command.ExecuteReader() )
                    {
                        int OrdinalTimestamp = reader.GetOrdinal( "Timestamp" );
                        int OrdinalEntry = reader.GetOrdinal( "entry" );
                        int OrdinalSnowFalling = reader.GetOrdinal( "snowFalling" );
                        int OrdinalSnowLying = reader.GetOrdinal( "snowLying" );
                        int OrdinalSnowDepth = reader.GetOrdinal( "snowDepth" );

                        while ( reader.Read() )
                        {
                            DiaryValue tmp = new()
                            {
                                //ThisDate = DateTimeOffset.FromUnixTimeSeconds( reader.GetInt64( OrdinalTimestamp ) ).DateTime.ToLocalTime(),
                                ThisDate = reader.GetDateTime( OrdinalTimestamp ).ToLocalTime(),
                                snowFalling = reader.GetInt32( OrdinalSnowFalling ),
                                snowLying = reader.GetInt32( OrdinalSnowLying ),
                                snowDepth = reader.GetFloat( OrdinalSnowDepth ),
                            };

                            tmpList.Add( tmp );
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
    } // End class Diary
} // End Namespace 
