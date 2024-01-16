/*
 * UserReports - Part of CumulusUtils
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
                await Isup.UploadFileAsync( bareFilename, Sup.PathUtils + bareFilename );
            }

            return;
        }

    }


}
