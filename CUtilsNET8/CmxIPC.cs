/* 
 * CmxIPC - Part of CumulusUtils
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

            string CMXport = Sup.GetUtilsIniValue( "General", "CMXport", "8998" );

#if !RELEASE
            CmxBaseURL = $"http://192.168.178.2:{CMXport}";
#else
            CmxBaseURL = $"http://localhost:{CMXport}";
#endif
        }

        public async Task<InfoFromCMX> GetCMXInfoAsync()
        {
            InfoFromCMX thisInfo;

            string CMXinfoURL = $"{CmxBaseURL}/api/tags/process.json?version&build&ProgramUpTime&NewBuildAvailable&NewBuildNumber&CpuCount&CPUTemp";
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

        public async Task<string> GetCMXGraphdataAsync( string thisGraphDef )
        {
            // Base function without a startdate gets full json
            string GraphDataUrl = $"{CmxBaseURL}/api/graphdata/{thisGraphDef}";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( GraphDataUrl ) );

            return JSONstring;
        }

        public async Task<string> GetCMXGraphdataAsync( string thisGraphDef, DateTime thisTime )
        {
            // Overload woith a startdate
            // Everything according to the spec in the Marks email of 16/03/2023 17h22 :
            // 1) (@17h22)
            // Would it help your CUtils if you could do something like this when fetching the graph data…
            // /api/graphdata/tempdata.json?start=1678983491
            // Where start = unix timestamp to start the graph data from ?
            // 2) (@18h10)
            // No parameter = full data set from Graph Hours
            // Start ts = real Unix timestamp, no fudging of local time.
            // Graph data sets remain the same with pseudo TS.
            //
            //string GraphDataUrl = $"{CmxBaseURL}/api/graphdata/{thisGraphDef}?start={CuSupport.DateTimeToUnixUTC( thisTime )}";

            // The above spec was invalidated while making v8 Not sure if this is going to be used again.

            string GraphDataUrl = $"{CmxBaseURL}/api/graphdata/{thisGraphDef}?start={thisTime:yyyy-MM-dd}";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( GraphDataUrl ) );

            return JSONstring;
        }

        private async Task<string> GetSingleWebtagValueFromCMXAsync( string tagName )
        {
            string retval;

            string SingleWebtagURL = $"{CmxBaseURL}/api/tags/process.json?{tagName}";
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
                if ( JSONstring[ 0 ] == '}' ) return "";

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

            string MultipleWebtagURL = $"{CmxBaseURL}/api/tags/process.txt";
            retval = await Isup.PostUrlDataAsync( new Uri( MultipleWebtagURL ), content );

            return retval;
        } // End ReplaceWebtags

    } // End Class CmxIPC
}// End Namespace