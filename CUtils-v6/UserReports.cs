/*
 * UserReports - Part of CumulusUtils
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
 */
using System.IO;
using System.Text;
using System.Threading.Tasks;

// 
// Api description : https://cumulus.hosiene.co.uk/viewtopic.php?f=40&t=18334
//

namespace CumulusUtils
{
    class UserReports
    {
        readonly CuSupport Sup;
        readonly InetSupport Isup;
        readonly CmxIPC thisIPC;

        public UserReports( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;
            thisIPC = new CmxIPC( Sup, Isup );
        }

        public async Task DoUserReports()
        {
            const string ReportPrefix = "CURPT";

            Sup.LogDebugMessage( "USerReports: Start" );

            string[] files = Directory.GetFiles( $"{Sup.PathUtils}", $"{ReportPrefix}*.txt" );
            string FileContents, ContentsWithWebtagReplacements;

            foreach ( string file in files )
            {
                Sup.LogTraceInfoMessage( $"USerReports: Doing file {file}" );

                // Prepare and call
                FileContents = File.ReadAllText( file );

                // For any CU webtags defined (currently only version) replace the webtag by its value
                if ( FileContents.Contains( "<#CUversion>" ) )
                {
                    FileContents = FileContents.Replace( "<#CUversion>", CuSupport.UnformattedVersion() );
                }

                // Do the CMX webtag replacement
                ContentsWithWebtagReplacements = await thisIPC.ReplaceWebtagsPostAsync( FileContents );

                Sup.LogTraceInfoMessage( $"USerReports: After the async call" );

                string bareFilename = file.Substring( Sup.PathUtils.Length + ReportPrefix.Length );
                File.WriteAllText( $"{Sup.PathUtils}{bareFilename}", ContentsWithWebtagReplacements, Encoding.UTF8 );

                // Always upload, they're user reports so the user wants them there
                Sup.LogTraceInfoMessage( $"USerReports: Uploading {bareFilename}" );
                Isup.UploadFile( bareFilename, Sup.PathUtils + bareFilename );
            }

            return;
        }

    }


}
