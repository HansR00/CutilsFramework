/* 
 * CmxIPC - Part of CumulusUtils
 *
 */

using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CumulusUtils
{
    public class InfoFromCMX
    {
        public string version { get; set; } = string.Empty;
        public string build { get; set; } = string.Empty;
        public string ProgramUpTime { get; set; } = string.Empty;
        public string NewBuildAvailable { get; set; } = string.Empty;  // gives 0 or 1
        public string NewBuildNumber { get; set; } = string.Empty;  // gives 0 or 1
        public string CpuCount { get; set; } = string.Empty;
        public string CPUTemp { get; set; } = string.Empty;
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

            try
            {
                string CMXinfoURL = $"{CmxBaseURL}/api/tags/process.json?version&build&ProgramUpTime&NewBuildAvailable&NewBuildNumber&CpuCount&CPUTemp";
                string JSONstring = await Isup.GetUrlDataAsync( new Uri( CMXinfoURL ) );

                if ( string.IsNullOrEmpty( JSONstring ) )
                    thisInfo = new InfoFromCMX();
                else
                    thisInfo = JsonSerializer.Deserialize<InfoFromCMX>( JSONstring );

                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : version: {thisInfo.version}" );
                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : build: {thisInfo.build}" );
                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : ProgramUpTime: {thisInfo.ProgramUpTime}" );
                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : NewBuildAvailable: {thisInfo.NewBuildAvailable}" );
                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : NewBuildNumber: {thisInfo.NewBuildNumber}" );
                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : CpuCount: {thisInfo.CpuCount}" );
                Sup.LogTraceVerboseMessage( $"GetCMXInfo API : CpuTemp: {thisInfo.CPUTemp}" );
            }
            catch ( Exception ex )
            {
                Sup.LogTraceErrorMessage( $"GetCMXInfo API : Exception on fetching or deserializing JSON: {ex.Message}" );
                thisInfo = new InfoFromCMX();
            }

            return thisInfo;
        }

        public async Task<string> GetCMXGraphdataAsync( string thisGraphDef )
        {
            // Base function without a startdate gets full json - in CUtils only used for Graphconfig.
            string GraphDataUrl = $"{CmxBaseURL}/api/graphdata/{thisGraphDef}";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( GraphDataUrl ) );

            return JSONstring;
        }

        public async Task<string> GetCMXGraphdataAsync( string thisGraphDef, DateTime thisTime )
        {
            // Overload woith a startdate
            // Everything according to the spec in the Marks email of 16/03/2023 17h22 and the change in email of 6/12/2025 20h50:
            // 1) (@17h22)
            // Would it help your CUtils if you could do something like this when fetching the graph data…
            // /api/graphdata/tempdata.json?start=1678983491
            // Where start = unix timestamp to start the graph data from ?
            // 2) (@18h10)
            // No parameter = full data set from Graph Hours
            // Start ts = real Unix timestamp, no fudging of local time.
            // Graph data sets remain the same with pseudo TS.
            //   string GraphDataUrl = $"{CmxBaseURL}/api/graphdata/{thisGraphDef}?start={CuSupport.DateTimeToUnixUTC( thisTime )}";
            //   The above spec was invalidated while making v8 Not sure if this is going to be used again.

            // 3) (6/12/2025 @20h50):
            // It is an easy change for me in the API parameter parser to add a date/time.
            // from:
            //    start = DateTime.ParseExact( Request.QueryString.Get( "start" ), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal );

            // to:
            //    start = DateTime.ParseExact( Request.QueryString.Get( "start" ), [ "yyyy-MM-dd", "yyyy-MM-dd HH:mm" ], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal );
            //
            //    Consider it done for the next beta build.


            string GraphDataUrl = $"{CmxBaseURL}/api/graphdata/{thisGraphDef}?start={thisTime:yyyy-MM-dd HH:mm}";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( GraphDataUrl ) );

            return JSONstring;
        }

        private async Task<string> GetSingleWebtagValueFromCMXAsync( string tagName )
        {
            string retval = "";

            string SingleWebtagURL = $"{CmxBaseURL}/api/tags/process.json?{tagName}";
            string JSONstring = await Isup.GetUrlDataAsync( new Uri( SingleWebtagURL ) );

            using ( JsonDocument document = JsonDocument.Parse( JSONstring ) )
            {
                JsonElement root = document.RootElement;

                // Check if the root is an object
                if ( root.ValueKind == JsonValueKind.Object )
                {
                    // Enumerate the properties
                    foreach ( var property in root.EnumerateObject() )
                    {
                        if ( property.Value.ValueKind == JsonValueKind.String )
                        {
                            retval = property.Value.GetString()!;
                        }

                        // Possible values for JsonValueKind
                        // Value,     Description,                                                 JSON Example
                        // Object,    A complex JSON object, represented by curly braces.          "{"key": "value"}"
                        // Array,     An array of JSON values, represented by square brackets.     "["item1", "item2"]"
                        // String,    A sequence of Unicode characters, enclosed in double quotes. "hello world"
                        // Number,    A JSON number( integer or floating point ).                  123, 123.45, 1e-6 
                        // True,      The JSON boolean value for true.                             true
                        // False,     The JSON boolean value for false.                            false
                        // Null,      The JSON literal for null                                    null
                        // Undefined, Internal use only.This value is never produced when parsing a JSON document and is typically used as a default state
                        //            for a JsonElement that has not been initialized.You should generally not expect to encounter this value in parsed data.
                    }
                }
            }

            return retval; // when error , empty string 
        } // End GetSingleWebtagValueFromCMX


        public string ReplaceWebtagsGet( string thisString )
        {
            int i, j;
            string Webtag = "";
            string retval = "";

            Sup.LogTraceInfoMessage( $"ReplaceWebtag start: {thisString}" );

            if ( string.IsNullOrEmpty( thisString ) ) return "";

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
                    // retval += await GetSingleWebtagValueFromCMXAsync( Webtag );
                    Task<string> AsyncTask = GetSingleWebtagValueFromCMXAsync( Webtag );
                    AsyncTask.Wait();
                    retval = AsyncTask.Result;

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