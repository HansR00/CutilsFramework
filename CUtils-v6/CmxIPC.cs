/* 
 * CmxIPC - Part of CumulusUtils
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
 * Module for serialising / deserialising JSON structures and files 
 * Started with the possibility of IPC with CumulusMX over the HTTP with WebTags in CMX 3.7.0
 * Usage can not be with lower versions of CMX so this is means a new CumulusUtils branch.
 * 
 * Newtonsoft JSON library is uses as this is already in the CMX directory ready for use.
 * Documentation:  https://www.newtonsoft.com/json/help/html/Introduction.htm
 * 
 * Well: changed to ServiceStack.Text.dll
 *   
 * (and for the Microsoft version of the JSON interface - much harder)
 * On JSON handling because of the new interface with CumulusMX:
 *   https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
 *   https://www.codementor.io/@andrewbuchan/how-to-parse-json-into-a-c-object-4ui1o0bx8
 *   https://www.softwaretestinghelp.com/create-json-structure-using-c/
 *   Search : https://www.google.com/search?client=firefox-b-d&q=reading+json+formatted+data+into+variables+c%23
 * 
 */

using System;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace CumulusUtils
{
    public class InfoFromCMX
    {
        public string Version { get; set; }
        public string Build { get; set; }
        public string ProgramUpTime { get; set; }
        public string NewBuildAvailable { get; set; }  // gives 0 or 1
        public string NewBuildNumber { get; set; }  // gives 0 or 1
        public string CpuCount { get; set; }
        public string CpuTemp { get; set; }
    }

    public class CmxIPC
    {
        public string CmxBaseURL { get; }

        readonly CuSupport Sup;
        readonly InetSupport Isup;

        public CmxIPC( CuSupport s, InetSupport i )
        {
            Sup = s;
            Isup = i;

            string CMXport = Sup.GetUtilsIniValue( "SysInfo", "CMXport", "8998" );

#if RELEASE
            CmxBaseURL = $"http://localhost:{CMXport}/api/tags/";
#else
            CmxBaseURL = $"http://localhost:{CMXport}/api/tags/";
            //CmxBaseURL = $"http://192.168.178.144:{CMXport}/api/tags/";
#endif
        }

        public async Task<InfoFromCMX> GetCMXInfoAsync()
        {
            InfoFromCMX thisInfo;

            string CMXinfoURL = $"{CmxBaseURL}process.json?version&build&ProgramUpTime&NewBuildAvailable&NewBuildNumber&CpuCount&CPUTemp";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( CMXinfoURL ) );

            if ( string.IsNullOrEmpty( JSONstring ) )
                thisInfo = new InfoFromCMX();
            else
                thisInfo = JsonSerializer.DeserializeFromString<InfoFromCMX>( JSONstring );

            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : version: {thisInfo.Version}" );
            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : build: {thisInfo.Build}" );
            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : ProgramUpTime: {thisInfo.ProgramUpTime}" );
            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : NewBuildAvailable: {thisInfo.NewBuildAvailable}" );
            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : NewBuildNumber: {thisInfo.NewBuildNumber}" );
            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : CpuCount: {thisInfo.CpuCount}" );
            Sup.LogTraceVerboseMessage( $"GetCMXInfo API : CpuTemp: {thisInfo.CpuTemp}" );

            return thisInfo;
        }

        private async Task<string> GetSingleWebtagValueFromCMXAsync( string tagName )
        {
            string retval;

            string SingleWebtagURL = $"{CmxBaseURL}process.json?{tagName}";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( SingleWebtagURL ) );

            // Well, there may be some double testing on the validity of the JSON/Webtag validity
            if ( string.IsNullOrEmpty( JSONstring ) )
            {
                Sup.LogTraceVerboseMessage( $"SingleWebtagFromCMX API : Error on fetching JSON: {JSONstring}" );
                retval = "";
            }
            else
            {
                // https://stackoverflow.com/questions/21600968/using-servicestack-text-to-deserialize-a-json-string-to-object
                //
                if ( JSONstring[ 0 ] == '}' )
                    return "";

                var o = JsonObject.Parse( JSONstring );

                retval = o.Get<string>( tagName );               //(tagName, StringComparison.InvariantCulture).Name == tagName)
            } // End nonempty JSONstring

            return retval;
        } // End GetSingleWebtagValueFromCMX

        public async Task<string> ReplaceWebtagsGetAsync( string thisString )
        {
            int i, j;
            string Webtag = "";
            string retval = "";

            Sup.LogTraceInfoMessage( $"ReplaceWebtag start: {thisString}" );

            if ( string.IsNullOrEmpty( thisString ) )
                return "";

            for ( i = 0; i < thisString.Length; i++ )
            {
                if ( thisString[ i ] == '<' && thisString[ i + 1 ] == '#' ) // is a webtag
                {
                    // So webtag  name starts at i+2
                    for ( j = i + 2; j < thisString.Length && thisString[ j ] != '>'; j++ )
                    {
                        Webtag += thisString[ j ];
                    }

                    if ( j == thisString.Length )
                    {
                        Sup.LogTraceVerboseMessage( $"ReplaceWebtag: Illegal  syntax : '{thisString}'" );
                        break;
                    }

                    // So we have the webtag  here, get the value and add it to the return value.
                    retval += await GetSingleWebtagValueFromCMXAsync( Webtag );
                    Webtag = ""; // reinitialise for a next Webtag in the same string
                    i = j; // Let the outer for loop take over again
                }
                else
                {
                    retval += thisString[ i ];
                }
            }

            return retval;
        } // End ReplaceWebtags

        public async Task<string> ReplaceWebtagsPostAsync( string content )
        {
            string retval;

            Sup.LogTraceInfoMessage( $"ReplaceWebtagsPostAsync start:" );

            string MultipleWebtagURL = $"{CmxBaseURL}process.txt";
            retval = await Isup.PostUrlDataAsync( new Uri( MultipleWebtagURL ), content );

            Sup.LogTraceVerboseMessage( $"ReplaceWebtagsPostAsync End. Returning {retval}" );

            return retval;
        } // End ReplaceWebtags

    } // End Class CmxIPC
}// End Namespace