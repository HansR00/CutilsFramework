using System;
using System.IO;
using System.Net;

namespace CheckTaglist
{
    internal class CheckTaglist
    {

        static void Main()
        {
            try
            {
                int count = 0;

                string[] ExistingTags = File.ReadAllLines( "./WebTags.txt" );

                WebClient webClient = new WebClient();
                webClient.DownloadFile( "https://cumuluswiki.org/a/Full_list_of_Webtags", @"./WikiWebtags.txt" );
                string WikiTaglist = File.ReadAllText( "./WikiWebtags.txt" );

                using ( StreamWriter sw = new StreamWriter( "TagsToDo.txt" ) )
                {
                    sw.WriteLine( $"Missing the following tags in the Full List in the CumulusMX Wiki:\n" );

                    foreach ( string TagName in ExistingTags )
                    {
                        if ( !WikiTaglist.Contains( $"&lt;#{TagName}" ) )
                        {
                            if ( TagName.Contains( "AirLink" ) ) continue;                  // known to be there but complex because of [IN|OUT] addition
                            if ( char.IsDigit( TagName[ TagName.Length - 1 ] ) ) continue;  // Skip all Extra sensor tags (ending with a digit)

                            sw.WriteLine( $"Missing {TagName} in Wiki." );
                            Console.Write( $"Missing {count++} TagName descriptions in Wiki.\r" );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                Console.WriteLine( $"CheckTaglist Exception occurred: {e.Message}" );
                Console.WriteLine( $"CheckTaglist Exiting" );
            }

            Console.WriteLine( "\nPlease press any key to continue..." );
            Console.ReadKey();
        }
    }
}
