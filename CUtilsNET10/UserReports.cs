/*
 * UserReports - Part of CumulusUtils
 *
 */

using System.Diagnostics;
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

            Sup.LogDebugMessage( "USerReports: Starting" );

            string[] files = Directory.GetFiles( $"{Sup.PathUtils}", $"{ReportPrefix}*.txt" );
            string FileContents, ContentsWithWebtagReplacements;

            foreach ( string file in files )
            {
                Sup.LogMessage( $"USerReports: Doing file {file}", TraceLevel.Info );

                // Prepare and call
                FileContents = File.ReadAllText( file );

                // For any CU webtags defined (currently only version) replace the webtag by its value
                if ( FileContents.Contains( "<#CUversion>" ) )
                {
                    FileContents = FileContents.Replace( "<#CUversion>", CuSupport.UnformattedVersion() );
                }

                // Do the CMX webtag replacement
                ContentsWithWebtagReplacements = await thisIPC.ReplaceWebtagsPostAsync( FileContents );

                Sup.LogMessage( $"USerReports: After the async call", TraceLevel.Info );

                string bareFilename = file.Substring( Sup.PathUtils.Length + ReportPrefix.Length );
                File.WriteAllText( $"{Sup.PathUtils}{bareFilename}", ContentsWithWebtagReplacements, Encoding.UTF8 );

                // Always upload, they're user reports so the user wants them there
                Sup.LogMessage( $"USerReports: Uploading {bareFilename}", TraceLevel.Info );
                await Isup.UploadFileAsync( bareFilename, Sup.PathUtils + bareFilename );
            }

            return;
        }

    }


}
