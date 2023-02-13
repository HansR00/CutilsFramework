using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Test
    {
        public static async Task Main( string[] args )
        {
            Console.WriteLine( "TEST: Start" );

            string FileToSend = "Test-CNETFramenwork-HTTPclient";

            using ( StreamWriter of = new StreamWriter( $"{FileToSend}", false, Encoding.UTF8 ) )
            {
                string Website = "https://meteo-wagenborgen.nl";
                of.WriteLine( $"Test content for .NET HTTPclient transfer...to {Website}" );
            }

            string thisContent = $"filename#{FileToSend}&";
            thisContent += "filecontent#" + File.ReadAllText( FileToSend, Encoding.UTF8 );
            _ = await PostUrlDataAsync( new Uri( "https://meteo-wagenborgen.nl/CMX4/upload.php" ), thisContent );

            Console.WriteLine( $"TEST : Success" );

            return;
        }

        static async Task<string> PostUrlDataAsync( Uri thisURL, string data )
        {
            string retval;

            // Note: I use 'using' because it is easier and it gets only called for UserReports so 
            //       there is no risk - I don't see a risk - of socket exhaustion
            // Prevent issues with OpenSSL so bypass the certificate for the CGI
            // https://stackoverflow.com/questions/52939211/the-ssl-connection-could-not-be-established

            // This does no longer seem necessary:
            //HttpClientHandler clientHandler = new HttpClientHandler
            //{
            //    ServerCertificateCustomValidationCallback = ( sender, cert, chain, sslPolicyErrors ) => { return true; }
            //};

            //using ( HttpClient PostClient = new HttpClient( clientHandler, true ) )
            using ( HttpClient PostClient = new HttpClient() )
            {
                Console.WriteLine( $"PostUrlData Calling PostAsync" );

                try
                {
                    using ( StringContent requestData = new StringContent( data, Encoding.UTF8 ) )
                    {
                        using ( HttpResponseMessage response = await PostClient.PostAsync( thisURL, requestData ) )
                        {
                            if ( response.IsSuccessStatusCode )
                            {
                                retval = await response.Content.ReadAsStringAsync();
                                Console.WriteLine( $"PostUrlData success response : {response.StatusCode} - {response.ReasonPhrase}" );
                            }
                            else
                            {
                                Console.WriteLine( $"PostUrlData : Error: {response.StatusCode} - {response.ReasonPhrase}" );
                                retval = "";
                            }
                        } // End using response -> dispose
                    } // End using requestData -> dispose
                }
                catch ( Exception e ) when ( e is HttpRequestException )
                {
                    Console.WriteLine( $"PostUrlData : Exception - {e.Message}" );
                    if ( e.InnerException != null )
                        Console.WriteLine( $"PostUrlData: Inner Exception: {e.InnerException}" );
                    retval = "";
                }
                catch ( Exception e )
                {
                    Console.WriteLine( $"PostUrlData : General exception - {e.Message}" );
                    retval = "";
                }
            }

            Console.ReadKey();

            return retval;
        }
    }
}
